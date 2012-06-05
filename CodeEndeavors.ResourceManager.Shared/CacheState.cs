using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using CodeEndeavors.ResourceManager.DomainObjects;
using System.Web.Caching;
using CodeEndeavors.Extensions;
using System.Collections.Concurrent;

namespace CodeEndeavors.ResourceManager
{
    public class CacheState
    {
        //this is the meta data entry for all cache items
        //private static SafeDictionary<string, CacheItem> _cacheItems = new SafeDictionary<string, CacheItem>();
        private static ConcurrentDictionary<string, CacheItem> _cacheItems = new ConcurrentDictionary<string, CacheItem>();

        public static System.Web.Caching.Cache Cache
        {
            get
            {
                if (HttpContext.Current != null)
                    return HttpContext.Current.Cache;
                return HttpRuntime.Cache;
            }
        }

        public static void RemoveCacheItem(string key)
        {
            CacheItem item = null;
            _cacheItems.TryRemove(key, out item);
        }

        public static CacheItem GetCacheItem(string key)
        {
            CacheItem item;
            _cacheItems.TryGetValue(key, out item);  
            if (item == null)
            {
                item = new CacheItem(key);
                _cacheItems[key] = item;
            }
            return item;
        }

        public static T GetState<T>(string key, T defaultValue)
        {
            object obj = Cache[key];
            if (obj == null)
                return defaultValue;
            return obj.ToType<T>();
        }

        public static void SetState(string key, object value, TimeSpan duration, CacheItemPriority priority)
        {
            SetState(key, value, duration, priority, null);
        }

        public static void SetState(string key, object value, TimeSpan duration, System.Web.Caching.CacheItemPriority priority, string dependencyKey)
        {
            SetState(key, value, duration, priority, dependencyKey, null);
        }

        public static void SetState(string key, object value, TimeSpan duration, System.Web.Caching.CacheItemPriority priority, string dependencyKey, string dependencyFileName)
        {
            DateTime exp = Cache.NoAbsoluteExpiration;
            if (duration < TimeSpan.MaxValue)
                exp = DateTime.Now.Add(duration);

            SetState(key, value, exp, priority, dependencyKey, dependencyFileName);
        }

        public static void SetState(string key, object value, DateTime expiryTime, System.Web.Caching.CacheItemPriority priority)
        {
            SetState(key, value, expiryTime, priority, null, null);
        }

        public static void SetState(string key, object value, DateTime expiryTime, System.Web.Caching.CacheItemPriority priority, string dependencyKey, string dependencyFileName)
        {
            if (value != null)
            {
                CacheItem item = GetCacheItem(key);

                item.LastUpdate = DateTime.Now;
                item.AbsoluteExpiration = expiryTime;
                item.Priority = priority;

                CacheDependency dependency = null;
                string[] keys = null;
                string[] dependencyFileNames = null;
                if (string.IsNullOrEmpty(dependencyKey) == false)
                    keys = dependencyKey.Split('|');
                if (dependencyFileName != null)
                    dependencyFileNames = dependencyFileName.Split('|');
                
                if (keys != null || dependencyFileNames != null)
                    dependency = new CacheDependency(dependencyFileNames, keys);

                item.DependencyKey = dependencyKey;
                item.DependencyFileName = dependencyFileName;

                Cache.Insert(key, value, dependency, item.AbsoluteExpiration, Cache.NoSlidingExpiration, priority, null);
            }
            else
                RemoveCache(key);
        }

        public static void SetState(string key, object value, string fileName)
        {
            if (value != null)
            {
                CacheItem item = GetCacheItem(key);
                item.LastUpdate = DateTime.Now;
                item.DependencyFileName = fileName;
                Cache.Insert(key, value, new CacheDependency(fileName));
            }
            else
                RemoveCache(key);
        }

        public static List<string> GetCacheKeys()
        {
            return _cacheItems.Keys.ToList();
        }

        public static void ExpireCache()
        {
            _cacheItems.Keys.ToList().ForEach(RemoveCache);
        }

        public static void ExpireCache(string key)
        {
            _cacheItems.Keys.ToList().FindAll(k => k == key || k.StartsWith(key + "~")).ForEach(RemoveCache);
        }

        public static void RemoveCache(string key)
        {
            Cache.Remove(key);
            RemoveCacheItem(key);
        }

        #region PullCache Methods
        public delegate T PullCacheData<T>();

        public static T PullCache<T>(string key, bool useCache, PullCacheData<T> pullFunc, CacheItemPriority priority, TimeSpan CacheTime)
        {
            return PullCache<T>(key, useCache, pullFunc, priority, CacheTime, null);
        }

        public static T PullCache<T>(string key, bool useCache, PullCacheData<T> pullFunc, string DependencyFileName)
        {
            return PullCache<T>(key, useCache, pullFunc, CacheItemPriority.Normal, TimeSpan.MaxValue, DependencyFileName);
        }
        
        public static T PullCache<T>(string key, bool useCache, PullCacheData<T> pullFunc, CacheItemPriority priority, TimeSpan CacheTime, string DependencyFileName)
        {
            T ret = default(T);
            string cacheKey = GetKey(key, typeof(T));

            if (useCache)
            {
                ret = GetState<T>(cacheKey, default(T));   
                if (ret == null) 
                {
                    var cacheItem = GetCacheItem(cacheKey);
                    lock (cacheItem)  //lock to make sure only one request occurs for this key/suffix combo
                    {
                        ret = GetState<T>(cacheKey, default(T));     //try and get value from cache within lock
                        if (ret == null) 
                        {
                            try
                            {
                                cacheItem.HasPendingRequest = true;
                                LogCache(string.Format("PullCache<{0}> not found in cache[{1}]. Retrieving from service.", typeof(T).ToString(), key.ToString()), "PullCache: " + key.ToString(), "");
                                //TimeSpan timeSpan = TimeSpan.MinValue;
                                ret = pullFunc(); //pull items
                                if (CacheTime != TimeSpan.MinValue)
                                    SetState(cacheKey, ret, CacheTime, priority, null, DependencyFileName);
                            }
                            catch
                            {
                                throw;
                            }
                            finally
                            {
                                cacheItem.HasPendingRequest = false;
                            }
                        }
                    }
                }
            }
            else
            {
                LogCache(string.Format("PullCache<{0}> not USING cache[{1}}]. Retrieving from service.", typeof(T).ToString(), key.ToString()), "PullCache: " + key.ToString(), "");
                //TimeSpan timeSpan = TimeSpan.MinValue;
                ret = pullFunc();
                if (CacheTime != TimeSpan.MinValue)
                    SetState(cacheKey, ret, CacheTime, priority, null, DependencyFileName);
            }

            return ret;
        }

        #endregion


        #region Misc Helpers
        //todo: kind of a hack here!
        public static string GetDependencyKey(string key, Type valueType)
        {
            return GetKey(key, valueType);
        }

        public static string GetKey(string key, Type valueType)
        {
            return string.Format("{0}~{1}", key, valueType);
        }

        private static void LogCache(string logMsg, string counter, string counterItem)
        {
            //Log.Info(logMsg);
            //Count.Increment("Cache", counter, counterItem);
        }
        #endregion
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Caching;
using CodeEndeavors.Extensions;
using System.Collections.Concurrent;
using System.Configuration;
using System.Web;

namespace CodeEndeavors.ResourceManager.File
{
    public class FileRepository : IRepository, IDisposable
    {
        private Dictionary<string, object> _connection = null;  //connection is directory
        private string _resourceDir = null;
        private string _cacheType = null;
        public delegate void SaveFunc();
        
        //todo: static or instance...  concurrency will bite us either way!
        private static ConcurrentDictionary<Type, SaveFunc> _pendingUpdates = new ConcurrentDictionary<Type, SaveFunc>(); 

        public FileRepository()
        {
        }

        public void Initialize(Dictionary<string, object> connection)
        {
            //_connection = ConfigurationManager.AppSettings.GetSetting(Connection, @"~\FileDb");     
            _connection = connection;
            _resourceDir = _connection.GetSetting("resourceDir", "");
            _cacheType = _connection.GetSetting("cacheType", "web");
            
            if (string.IsNullOrEmpty(_resourceDir))
                throw new Exception("ResourceDir key not found in connection");
            
            //if (System.Web.HttpContext.Current != null)
            //    _resourceDir = System.Web.HttpContext.Current.Server.MapPath(_resourceDir);
            //else
            //    _resourceDir = _resourceDir.Replace(@"~/", HttpRuntime.AppDomainAppPath);
            if (_resourceDir.StartsWith("~/"))
            {
                if (System.Web.Hosting.HostingEnvironment.MapPath(_resourceDir) != null)
                    _resourceDir = System.Web.Hosting.HostingEnvironment.MapPath(_resourceDir);
                else
                    _resourceDir = _resourceDir.Replace("~/", Environment.CurrentDirectory + "/").Replace("/", "\\");
            }
            //return HttpContext.Current.Server.MapPath(path);

            if (!Directory.Exists(_resourceDir))
                Directory.CreateDirectory(_resourceDir);
        }

        public void Dispose()
        {
        }

        public DomainObjects.Resource<T> GetResource<T>(string id)
        {
            var dict = AllDict<T>();
            if (!string.IsNullOrEmpty(id) && dict.ContainsKey(id))
                return dict[id];
            return null;
            //return AllResources<T>().SingleOrDefault(d => d.Id == Id);
        }

        public List<T> Find<T>(Func<T, bool> predicate)
        {
            return All<T>().Where(predicate).ToList();
        }

        public List<DomainObjects.Resource<T>> FindResources<T>(Func<DomainObjects.Resource<T>, bool> predicate)
        {
            return AllResources<T>().Where(predicate).ToList();
        }

        public List<DomainObjects.Resource<T>> AllResources<T>()
        {
            return AllDict<T>().Values.ToList();    //todo:  perf problem????
        }

        public List<T> All<T>()
        {
            var fileName = GetJsonFileName<T>();

            CodeEndeavors.Cache.CacheState.PullCacheData<List<T>> getDelegate = delegate()
                    {
                        var resource = new List<T>();
                        var json = GetJsonFileContents<T>();
                        if (json != null)
                            resource = json.ToObject<List<T>>();
                        return resource;
                    };

            if (_cacheType == "request")
                return Cache.CacheState.PullRequestCache("FileRepository." + fileName, getDelegate);
            else
                return Cache.CacheState.PullCache("FileRepository." + fileName, true, getDelegate, fileName);
        }

        public ConcurrentDictionary<string, DomainObjects.Resource<T>> AllDict<T>()
        {
            var fileName = GetJsonFileName<DomainObjects.Resource<T>>();

            CodeEndeavors.Cache.CacheState.PullCacheData<ConcurrentDictionary<string, DomainObjects.Resource<T>>> getDelegate = delegate()
                {
                    var resource = new List<DomainObjects.Resource<T>>();
                    var json = GetJsonFileContents<DomainObjects.Resource<T>>();
                    if (json != null)
                        resource = json.ToObject<List<DomainObjects.Resource<T>>>();
                    return new ConcurrentDictionary<string, DomainObjects.Resource<T>>(resource.ToDictionary(r => r.Id));
                };

            if (_cacheType == "request")
                return Cache.CacheState.PullRequestCache("FileRepository." + fileName, getDelegate);
            else
                return Cache.CacheState.PullCache("FileRepository." + fileName, true, getDelegate, fileName);
        }

        public void Store<T>(T item)
        {
            //if (string.IsNullOrEmpty(item.Id))
            //{
            //    item.Id = Guid.NewGuid().ToString();    //todo: use another resource for this...
            //    try
            //    {
            //        ((dynamic)item.Data).Id = item.Id;  //todo: require Id on object?
            //    }
            //    catch { }
            //    //if (item.Data is CodeEndeavors.ResourceManager.IId)
            //    //    (item.Data as CodeEndeavors.ResourceManager.IId).Id = item.Id;
            //}
            //var dict = AllDict<T>();
            //dict[item.Id] = item;

            //var key = item.GetType();
            //if (!_pendingUpdates.ContainsKey(key))
            //    _pendingUpdates[key] = () => WriteJsonFile<T>();
            ////_pendingUpdates.AddOrUpdate(item.GetType(), 1, (key, oldValue) => oldValue + 1);

        }

        public void Store<T>(DomainObjects.Resource<T> item)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = Guid.NewGuid().ToString();    //todo: use another resource for this...
                try
                {
                    ((dynamic)item.Data).Id = item.Id;  //todo: require Id on object?
                }
                catch { }
                //if (item.Data is CodeEndeavors.ResourceManager.IId)
                //    (item.Data as CodeEndeavors.ResourceManager.IId).Id = item.Id;
            }
            var dict = AllDict<T>();
            dict[item.Id] = item;

            var key = item.GetType();
            if (!_pendingUpdates.ContainsKey(key))
                _pendingUpdates[key] = () => WriteJsonFile<T>();
            //_pendingUpdates.AddOrUpdate(item.GetType(), 1, (key, oldValue) => oldValue + 1);

        }

        public void Delete<T>(DomainObjects.Resource<T> item)
        {
            DomainObjects.Resource<T> resource = null;
            var dict = AllDict<T>();
            dict.TryRemove(item.Id, out resource);

            if (resource != null)
            {
                var key = item.GetType();
                if (!_pendingUpdates.ContainsKey(key))
                    _pendingUpdates[key] = () => WriteJsonFile<T>();
                //_pendingUpdates.AddOrUpdate(item.GetType(), 1, (key, oldValue) => oldValue + 1);
            }
        }

        public void Delete<T>(T item)
        {
            //T resource = null;
            //var dict = AllDict<T>();
            //dict.TryRemove(item.Id, out resource);

            //if (resource != null)
            //{
            //    var key = item.GetType();
            //    if (!_pendingUpdates.ContainsKey(key))
            //        _pendingUpdates[key] = () => WriteJsonFile<T>();
            //    //_pendingUpdates.AddOrUpdate(item.GetType(), 1, (key, oldValue) => oldValue + 1);
            //}
        }

        public void Save()
        {
            foreach (var type in _pendingUpdates.Keys)
                _pendingUpdates[type]();
            _pendingUpdates.Clear();
        }

        public void DeleteAll<T>()
        {
            var dict = AllDict<T>();
            dict.Clear();
            //todo:  save?
        }

        private string GetJsonFileName<T>()
        {
            return Path.Combine(_resourceDir, typeof(T).ToString()) + ".json";
        }

        private string GetJsonFileContents<T>()
        {
            var fileName = GetJsonFileName<T>();
            if (System.IO.File.Exists(fileName))
                return fileName.GetFileContents();
            return null;
        }

        private void WriteJsonFile<T>()
        {
            var fileName = GetJsonFileName<DomainObjects.Resource<T>>();
            var json = AllResources<T>().ToJson(true, "db");  //pretty?
            if (System.IO.File.Exists(fileName))    //todo: concurrent access dies!
                System.IO.File.Delete(fileName);
            json.WriteText(fileName);
            
        }

    }
}
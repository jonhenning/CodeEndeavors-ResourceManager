using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CodeEndeavors.Extensions;
using System.Collections.Concurrent;
using System.Configuration;
using System.Web;

namespace CodeEndeavors.ResourceManager.File
{
    public class FileRepository : IRepository, IDisposable
    {
        private Dictionary<string, object> _connection = null;  
        private Dictionary<string, object> _cacheConnection = null;  
        private string _resourceDir = null;
        //private string _cacheType = null;
        //public delegate void SaveFunc();
        
        //todo: static or instance...  concurrency will bite us either way!
        //private static ConcurrentDictionary<Type, Action> _pendingUpdates = new ConcurrentDictionary<Type, Action>();
        private string _cacheName;
        private bool _useFileMonitor;

        public FileRepository()
        {
        }

        public void Initialize(Dictionary<string, object> connection, Dictionary<string, object> cacheConnection)
        {
            _connection = connection;
            _cacheConnection = cacheConnection;
            _resourceDir = _connection.GetSetting("resourceDir", "");
            _cacheName = _cacheConnection.GetSetting("cacheName", "");
            _useFileMonitor = _connection.GetSetting("useFileMonitor", false);
            
            if (string.IsNullOrEmpty(_resourceDir))
                throw new Exception("ResourceDir key not found in connection");
            
            if (_resourceDir.StartsWith("~/"))
                _resourceDir = _resourceDir.Replace("~/", AppDomain.CurrentDomain.BaseDirectory + "/").Replace("/", "\\");

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
            return AllDict<T>().Values.Select(v => v.Data).ToList();
        }

        public ConcurrentDictionary<string, DomainObjects.Resource<T>> AllDict<T>()
        {
            var fileName = GetJsonFileName<DomainObjects.Resource<T>>();

            Func<ConcurrentDictionary<string, DomainObjects.Resource<T>>> getDelegate = delegate()
                {
                    var resource = new List<DomainObjects.Resource<T>>();
                    var json = GetJsonFileContents<DomainObjects.Resource<T>>();
                    if (json != null)
                        resource = json.ToObject<List<DomainObjects.Resource<T>>>();
                    return new ConcurrentDictionary<string, DomainObjects.Resource<T>>(resource.ToDictionary(r => r.Id));
                };

            return CodeEndeavors.Distributed.Cache.Client.Service.GetCacheEntry(_cacheName, fileName, getDelegate, getMonitorOptions(fileName));
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

            WriteJsonFile(dict.Values); //write file right away now...  someone else could have expired cache and it got reloaded from disk without our changes!
        }

        public void Delete<T>(DomainObjects.Resource<T> item)
        {
            DomainObjects.Resource<T> resource = null;
            var dict = AllDict<T>();
            dict.TryRemove(item.Id, out resource);

            if (resource != null)
                WriteJsonFile(dict.Values); //write file right away now...  someone else could have expired cache and it got reloaded from disk without our changes!
        }

        public void Save()
        {
            //foreach (var type in _pendingUpdates.Keys)
            //    _pendingUpdates[type]();
            //_pendingUpdates.Clear();
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

        private void WriteJsonFile<T>(IEnumerable<T> data)
        {
            var fileName = GetJsonFileName<T>();
            var json = data.ToJson(true, "db");  //pretty?
            if (System.IO.File.Exists(fileName))    //todo: concurrent access dies!
                System.IO.File.Delete(fileName);
            json.WriteText(fileName);
            expireCacheEntry(fileName);
        }

        private void expireCacheEntry(string key)
        {
            CodeEndeavors.Distributed.Cache.Client.Service.ExpireCacheEntry(_cacheName, key);
        }

        private object getMonitorOptions(string fileName)
        {
            if (_useFileMonitor)
                return new { monitorType = "CodeEndeavors.Distributed.Cache.Client.File.FileMonitor", fileName = fileName, uniqueProperty = "fileName" };
            return null; 
        }


    }
}
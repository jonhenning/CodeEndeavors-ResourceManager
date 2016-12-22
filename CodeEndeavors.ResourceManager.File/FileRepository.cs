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
        private string _namespace = null;
        
        private ConcurrentDictionary<string, Action> _pendingUpdates = new ConcurrentDictionary<string, Action>();

        //repository instance lifespan is a single request (for videre) - not ideal as we really don't want to dictate how resourcemanager is used in relation to its lifespan
        private ConcurrentDictionary<string, object> _pendingDict = new ConcurrentDictionary<string, object>();

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
            _namespace = _connection.GetSetting<string>("namespace", null);

            _cacheName = _cacheConnection.GetSetting("cacheName", "");
            _useFileMonitor = _connection.GetSetting("useFileMonitor", false);
            
            if (string.IsNullOrEmpty(_resourceDir))
                throw new Exception("ResourceDir key not found in connection");
            
            if (_resourceDir.StartsWith("~/"))
                _resourceDir = _resourceDir.Replace("~/", AppDomain.CurrentDomain.BaseDirectory + "/").Replace("/", "\\");

            if (!Directory.Exists(_resourceDir))
                Directory.CreateDirectory(_resourceDir);

            Logging.Log(Logging.LoggingLevel.Minimal, "File Repository Initialized {0}", _resourceDir);

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
            var resourceType = getResourceType<T>();

            if (_pendingDict.ContainsKey(resourceType))
            {
                Logging.Log(Logging.LoggingLevel.Verbose, "Pulled {0} from _pendingDict", resourceType);
                return _pendingDict[resourceType] as ConcurrentDictionary<string, DomainObjects.Resource<T>>;
            }

            var fileName = GetJsonFileName<DomainObjects.Resource<T>>();

            Func<ConcurrentDictionary<string, DomainObjects.Resource<T>>> getDelegate = delegate()
                {
                    var resource = new List<DomainObjects.Resource<T>>();
                    var json = GetJsonFileContents<DomainObjects.Resource<T>>();
                    if (json != null)
                        resource = json.ToObject<List<DomainObjects.Resource<T>>>();
                    return new ConcurrentDictionary<string, DomainObjects.Resource<T>>(resource.ToDictionary(r => r.Id));
                };

            var dict = CodeEndeavors.Distributed.Cache.Client.Service.GetCacheEntry(_cacheName, fileName, getDelegate, getMonitorOptions(fileName));
            _pendingDict[resourceType] = dict;
            return dict;
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
            }
            var dict = AllDict<T>();    
            dict[item.Id] = item;   //item is inserted into cache here

            Logging.Log(Logging.LoggingLevel.Detailed, "Stored {0}:{1} in _pendingDict", typeof(T).Name, item.Id);

            //keeping deferred writes intact - we are storing the dictionary in a member variable (_pendingDict) so even if 
            //item is expelled from cache we will still have our changes
            //of course, this leads to the potential problem with concurrent writes but we would have that anyways
            var resourceType = getResourceType<T>();
            if (!_pendingUpdates.ContainsKey(resourceType))
                _pendingUpdates[resourceType] = () => WriteJsonFile<T>();
        }

        public void Delete<T>(DomainObjects.Resource<T> item)
        {
            DomainObjects.Resource<T> resource = null;
            var dict = AllDict<T>();
            dict.TryRemove(item.Id, out resource);

            Logging.Log(Logging.LoggingLevel.Detailed, "Removed {0}:{1} in _pendingDict", typeof(T).Name, item.Id);

            if (resource != null)
            {
                var resourceType = getResourceType<T>();
                if (!_pendingUpdates.ContainsKey(resourceType))
                    _pendingUpdates[resourceType] = () => WriteJsonFile<T>();
            }
        }

        public void Save()
        {
            Logging.Log(Logging.LoggingLevel.Minimal, "Save called with {0} pending updates", _pendingUpdates.Keys.Count);

            foreach (var resourceType in _pendingUpdates.Keys)
                _pendingUpdates[resourceType]();
            _pendingUpdates.Clear();
        }

        public void DeleteAll<T>()
        {
            var dict = AllDict<T>();
            dict.Clear();

            var resourceType = getResourceType<T>();
            if (!_pendingUpdates.ContainsKey(resourceType))
                _pendingUpdates[resourceType] = () => WriteJsonFile<T>();
        }

        private string GetJsonFileName<T>()
        {
            var fileName = typeof(T).ToString();
            if (!string.IsNullOrEmpty(_namespace))
                fileName = _namespace + "." + fileName;
            return Path.Combine(_resourceDir, fileName) + ".json";
        }

        private string GetJsonFileContents<T>()
        {
            var fileName = GetJsonFileName<T>();
            if (System.IO.File.Exists(fileName))
            {
                var contents = fileName.GetFileContents();
                Logging.Log(Logging.LoggingLevel.Minimal, "Retrieved data from file {0} (length={1})", fileName, contents.Length);
                return contents;
            }
            return null;
        }

        private string getResourceType<T>()
        {
            return typeof(T).ToString();
        }

        private void WriteJsonFile<T>()
        {
            var fileName = GetJsonFileName<DomainObjects.Resource<T>>();
            var resources = AllResources<T>();
            var json = resources.ToJson(true, "db");  //pretty?
            if (System.IO.File.Exists(fileName))    //todo: concurrent access dies!
                System.IO.File.Delete(fileName);
            json.WriteText(fileName);

            Logging.Log(Logging.LoggingLevel.Minimal, "Wrote Json File {0} with {1} resources", fileName, resources.Count);

            var resourceType = getResourceType<T>();
            object temp;
            var success = _pendingDict.TryRemove(resourceType, out temp);
            Logging.Log(Logging.LoggingLevel.Minimal, "Removed {0} from pendingDict ({1})", resourceType, success);

            expireCacheEntry(fileName);
        }

        private void expireCacheEntry(string key)
        {
            Logging.Log(Logging.LoggingLevel.Minimal, "Calling Expire on {0}", key);
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
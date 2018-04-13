using System;
using System.Collections.Generic;
using System.Linq;
using CodeEndeavors.Extensions;
using CodeEndeavors.ResourceManager.Extensions;

namespace CodeEndeavors.ResourceManager
{
    public class ResourceRepository : IDisposable 
    {
       
        private IRepository _repository;
        private Dictionary<string, object> _connectionDict = null;
        private static string _clientId;    //static since we want it the same every time if we assign a guid
        private int _auditHistorySize;

        public int PendingUpdates { get; set; }

        public ResourceRepository(string connection)
        {
            _connectionDict = connection.ToObject<Dictionary<string, object>>();

            //backwards compatibility hack
            var backType = _connectionDict.GetSetting("type", "File");
            if (backType == "File")
                backType = "CodeEndeavors.ResourceManager.File.FileRepository";

            var type = string.Format(backType);

            var cacheConnection = _connectionDict.GetSetting("cacheConnection", new Newtonsoft.Json.Linq.JObject()).ToJson().ToObject<Dictionary<string, object>>();
            var cacheName = cacheConnection.GetSetting("cacheName", "ResourceManager.File");
            var notifierName = cacheConnection.GetSetting("notifierName", "");
            _auditHistorySize = _connectionDict.GetSetting("auditHistorySize", 10);

            if (_connectionDict.ContainsKey("notifierConnection"))
            {
                var notifierConnectionJson = _connectionDict.GetSetting("notifierConnection", new Newtonsoft.Json.Linq.JObject()).ToJson();
                //var notifierConnection = notifierConnectionJson.ToObject<Dictionary<string, object>>();
                Distributed.Cache.Client.Service.RegisterNotifier(notifierName, notifierConnectionJson);
            }

            if (!string.IsNullOrEmpty(_clientId))
                _clientId = _connectionDict.GetSetting("clientId", cacheConnection.GetSetting("clientId", Guid.NewGuid().ToString()));

            try
            {
                Distributed.Cache.Client.Service.RegisterCache(cacheName, cacheConnection.ToJson());
                _connectionDict["cacheName"] = cacheName;
                //Logging.Log(Logging.LoggingLevel.Minimal, "Cache {0} Configured: {1}", cacheName, cacheConnection.ToJson());
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LoggingLevel.Minimal, ex.Message);
                Logging.Log(Logging.LoggingLevel.Minimal, "Using default cache");
                cacheConnection = new Dictionary<string, object>();
            }

            _repository = type.GetInstance<IRepository>();
            _repository.Initialize(_connectionDict, cacheConnection);
        }

        public List<DomainObjects.Resource<T>> GetResources<T>()
        {
            return _repository.AllResources<T>();
        }

        public List<T> Find<T>(Func<T, bool> predicate)
        {
            return _repository.Find(predicate).ToList();
        }

        public List<DomainObjects.Resource<T>> FindResources<T>(Func<DomainObjects.Resource<T>, bool> predicate)
        {
            //return Get<T>().Where(predicate).ToList();
            return _repository.FindResources(predicate).ToList();
        }

        public List<T> Get<T>()
        {
            return _repository.All<T>();
        }

        public DomainObjects.Resource<T> GetResourceById<T>(string id)
        {
            //return Get<T>().Where(x => x.Id == Id).SingleOrDefault();
            return _repository.GetResource<T>(id);
        }

        public List<DomainObjects.Resource<T>> GetResources<T>(string type, string key = null) 
        {
            return FindResources<T>(r => r.Type == type && (key == null || r.Key == key));
        }
        public List<DomainObjects.Resource<T>> GetResources<T>(string type, string key, Func<DomainObjects.Resource<T>, dynamic> statement, bool bestMatch = true)
        {
            return GetResources<T>(GetResources<T>(type, key), new List<DomainObjects.Query<DomainObjects.Resource<T>>>() { new DomainObjects.Query<DomainObjects.Resource<T>>(statement, 1) }, bestMatch);
        }
        public List<DomainObjects.Resource<T>> GetResources<T>(string type, string key, List<DomainObjects.Query<DomainObjects.Resource<T>>> queries, bool bestMatch = true)
        {
            return GetResources<T>(GetResources<T>(type, key), queries, bestMatch);
        }
        public List<DomainObjects.Resource<T>> GetResources<T>(string type, Func<DomainObjects.Resource<T>, dynamic> statement, bool bestMatch = false)
        {
            return GetResources<T>(GetResources<T>(type), new List<DomainObjects.Query<DomainObjects.Resource<T>>>() { new DomainObjects.Query<DomainObjects.Resource<T>>(statement, 1) }, bestMatch);
        }
        public List<DomainObjects.Resource<T>> GetResources<T>(string type, List<DomainObjects.Query<DomainObjects.Resource<T>>> queries, bool bestMatch = true)
        {
            return GetResources<T>(GetResources<T>(type), queries, bestMatch);
        }
        public List<DomainObjects.Resource<T>> GetResources<T>(List<DomainObjects.Resource<T>> resources, List<DomainObjects.Query<DomainObjects.Resource<T>>> queries, bool bestMatch = true)
        {
            var groupedItems = resources.GroupBy(i => i.GroupKey);
            var items = new List<DomainObjects.Resource<T>>();
            foreach (var group in groupedItems)
            {
                var matches = queries.GetMatches<T>(group, bestMatch);
                if (matches.Count > 0)
                {
                    if (bestMatch)
                        items.Add(matches[0]);
                    else 
                        items.AddRange(matches);
                }
            }
            return items;
        }

        public T GetResourceData<T>(string type, string key, Func<dynamic, dynamic> statement, T defaultValue)
        {
            return GetResourceData(type, key, new List<DomainObjects.Query<DomainObjects.Resource<T>>>() { new DomainObjects.Query<DomainObjects.Resource<T>>(statement, 1) }, defaultValue);
        }
        public T GetResourceData<T>(string type, string key, List<DomainObjects.Query<DomainObjects.Resource<T>>> queries, T defaultValue)
        {
            var items = GetResources<T>(type, key, queries, true);
            return (items.Count > 0 ? items[0].Data : defaultValue);
        }
        public T GetResourceData<T>(string type, Func<DomainObjects.Resource<T>, dynamic> statement, T defaultValue)
        {
            return GetResourceData(type, new List<DomainObjects.Query<DomainObjects.Resource<T>>>() { new DomainObjects.Query<DomainObjects.Resource<T>>(statement, 1) }, defaultValue);
        }
        public T GetResourceData<T>(string type, List<DomainObjects.Query<DomainObjects.Resource<T>>> queries, T defaultValue)
        {
            return GetResourceData<T>(type, null, queries, defaultValue);
        }
        public T GetResourceData<T>(List<DomainObjects.Resource<T>> resources, List<DomainObjects.Query<DomainObjects.Resource<T>>> queries, T defaultValue)
        {
            var items = GetResources<T>(resources, queries, true);
            return (items.Count > 0 ? items[0].Data : defaultValue);
        }

        public DomainObjects.Resource<T> StoreResource<T>(string type, string key, T data, string userId)
        {
            string id = null;
            try
            {
                id = ((dynamic)data).Id; //todo: better trapping of errors if data doesn't support?!
            }
            catch { }
            var resource = id != null ? GetResourceById<T>(id) : null;
            if (resource == null)
                resource = new DomainObjects.Resource<T>(type, key, null, data);
            else
                resource.Data = data;
            if (_auditHistorySize > 0)
            {
                resource.Audit.Add(new DomainObjects.Audit(userId, DateTimeOffset.UtcNow, "Save"));
                resource.Audit = resource.Audit.OrderByDescending(a => a.Date).Take(_auditHistorySize).ToList();   
            }
            _repository.Store(resource);
            PendingUpdates++;
            return resource;
        }
        public DomainObjects.Resource<T> StoreResource<T>(DomainObjects.Resource<T> resource, string userId)
        {
            if (_auditHistorySize > 0)
            {
                resource.Audit.Add(new DomainObjects.Audit(userId, DateTimeOffset.UtcNow, "Save"));
                resource.Audit = resource.Audit.OrderByDescending(a => a.Date).Take(_auditHistorySize).ToList();
            }
            _repository.Store(resource);
            PendingUpdates++;
            return resource;
        }

        public void SaveChanges()
        {
            _repository.Save();
            PendingUpdates = 0;
        }

        public void DeleteAll<T>(string type)
        {
            var resources = GetResources<T>(type);
            foreach (var resource in resources)
                Delete<T>(resource);

            SaveChanges();
        }
        public void DeleteAll<T>()
        {
            _repository.DeleteAll<T>();
            //SaveChanges();
        }

        public void Delete<T>(DomainObjects.Resource<T> resource)
        {
            _repository.Delete(resource);
            PendingUpdates++;
            //_repository.Save();
        }

        public DomainObjects.Resource<T> ExpireResource<T>(DomainObjects.Resource<T> resource, string userId) 
        {
            resource.Audit.Add(new DomainObjects.Audit(userId, DateTimeOffset.UtcNow, "Delete"));
            resource.ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(-1);
            _repository.Store(resource);
            //_repository.Save();
            return resource;
        }

        public string ObtainLock(string source, string ns)
        {
            return _repository.ObtainLock(source, ns);
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
         
    }
}
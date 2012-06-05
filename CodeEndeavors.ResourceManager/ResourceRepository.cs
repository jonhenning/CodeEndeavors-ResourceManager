using System;
using System.Collections.Generic;
using System.Linq;
using CodeEndeavors.Extensions;
using CodeEndeavors.ResourceManager.Extensions;
using StructureMap;

namespace CodeEndeavors.ResourceManager
{
    public class ResourceRepository : IDisposable 
    {
        private IRepository _repository;
        private Dictionary<string, object> _connectionDict = null;
        
        public int PendingUpdates { get; set; }

        public ResourceRepository(string connection)
        {
            _connectionDict = connection.ToObject<Dictionary<string, object>>();
            string assemblyName = _connectionDict.GetSetting("assembly", string.Format("CodeEndeavors.ResourceManager.{0}", _connectionDict.GetSetting("type", "File")));

            ObjectFactory.Configure(x =>
                x.Scan(scan =>
                {
                    scan.Assembly(assemblyName);
                    scan.AddAllTypesOf<IRepository>();
                }));

            _repository = ObjectFactory.GetInstance<IRepository>();
            _repository.Initialize(_connectionDict);
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
            var resource = GetResourceById<T>(id);
            if (resource == null)
                resource = new DomainObjects.Resource<T>(type, key, null, data);
            else
                resource.Data = data;
            resource.Audit.Add(new DomainObjects.Audit(userId, DateTime.UtcNow, "Save"));
            resource.Audit = resource.Audit.OrderByDescending(a => a.Date).Take(10).ToList();   //only keep 10 audits - todo: make this configurable
            _repository.Store(resource);
            PendingUpdates++;
            return resource;
        }
        public DomainObjects.Resource<T> StoreResource<T>(DomainObjects.Resource<T> resource, string userId)
        {
            resource.Audit.Add(new DomainObjects.Audit(userId, DateTime.UtcNow, "Save"));
            resource.Audit = resource.Audit.OrderByDescending(a => a.Date).Take(10).ToList();   //only keep 10 audits - todo: make this configurable
            _repository.Store(resource);
            PendingUpdates++;
            return resource;
        }

        public T Store<T>(T resource, string userId)
        {
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

        public void Delete<T>(T resource)
        {
            _repository.Delete(resource);
            PendingUpdates++;
            //_repository.Save();
        }
        public void Delete<T>(DomainObjects.Resource<T> resource)
        {
            _repository.Delete(resource);
            PendingUpdates++;
            //_repository.Save();
        }

        public DomainObjects.Resource<T> ExpireResource<T>(DomainObjects.Resource<T> resource, string userId) 
        {
            resource.Audit.Add(new DomainObjects.Audit(userId, DateTime.UtcNow, "Delete"));
            resource.ExpirationDate = DateTime.UtcNow.AddMinutes(-1);
            _repository.Store(resource);
            //_repository.Save();
            return resource;
        }

        public void Dispose()
        {
            _repository.Dispose();
        }

    }
}
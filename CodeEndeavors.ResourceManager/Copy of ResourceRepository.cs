using System;
using System.Linq;
using System.Collections.Generic;
using StructureMap;
using System.Web.Caching;
using CodeEndeavors.Extensions;
using CodeEndeavors.ResourceManager.Extensions;

namespace CodeEndeavors.ResourceManager
{
    public class ResourceRepository : IDisposable 
    {
        private IRepository _repository;
        private Dictionary<string, object> _connectionDict = null;
        
        //public enum RepositoryType
        //{
        //    RavenDb,
        //    File,
        //    AzureBlob
        //}

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

        //public ResourceRepository(string connection, RepositoryType type)
        //{
        //    this.Configure(connection, "CodeEndeavors.ResourceManager." + type.ToString());        
        //}

        //public ResourceRepository(string connection, string assemblyName)
        //{
        //    this.Configure(connection, assemblyName);
        //}

        //private void Configure(string connection, string assemblyName)
        //{
        //    ObjectFactory.Configure(x => 
        //        x.Scan(scan => {
        //            scan.Assembly(assemblyName);
        //            scan.AddAllTypesOf<IRepository>();
        //        }));

        //    _repository = ObjectFactory.GetInstance<IRepository>();
        //    _repository.Initialize(connection);
        //}

        public void ClearResources<T>(string Type)
        {
            var resources = Get<T>(Type).Data;
            foreach (var resource in resources)
                Purge<T>(resource);

            SaveChanges();
        }

        public void DeleteAll<T>()
        {
            _repository.DeleteAll<T>();
            SaveChanges();
        }

        public RepositoryResult<List<DomainObjects.Resource<T>>> Get<T>()
        {
            return RepositoryRequest.Execute<List<DomainObjects.Resource<T>>>(r =>
            {
                r.Data = _repository.All<T>();
            });

        }

        public RepositoryResult<List<DomainObjects.Resource<T>>> Find<T>(Func<DomainObjects.Resource<T>, bool> predicate)
        {
            return RepositoryRequest.Execute<List<DomainObjects.Resource<T>>>(r =>
            {
                //r.Data = Get<T>().Data.Where(predicate).ToList();
                r.Data = _repository.Find(predicate);
            });
        }

        public RepositoryResult<DomainObjects.Resource<T>> GetById<T>(string id)
        {
            return RepositoryRequest.Execute<DomainObjects.Resource<T>>(r =>
            {
                //r.Data = Get<T>().Data.Where(x => x.Id == Id).SingleOrDefault();
                r.Data = _repository.Get<T>(id);
            });
        }

        public RepositoryResult<List<DomainObjects.Resource<T>>> Get<T>(string type) 
        {
            return RepositoryRequest.Execute<List<DomainObjects.Resource<T>>>(r =>
            {
                //r.Data = Find<T>(x => x.Type == type);
                r.Data = _repository.Find<T>(x => x.Type == type);
            });
        }

        public RepositoryResult<List<DomainObjects.Resource<T>>> Get<T>(string type, Func<dynamic, dynamic> statement, bool bestMatch = false)
        {
            return RepositoryRequest.Execute<List<DomainObjects.Resource<T>>>(r =>
            {
                r.Data = Get<T>(Get<T>(type).Data, new List<DomainObjects.Query>() { new DomainObjects.Query(statement, 1) }, bestMatch);   //todo: inner messages will be lost
            });
        }

        public RepositoryResult<List<DomainObjects.Resource<T>>> Get<T>(string type, List<DomainObjects.Query> queries, bool bestMatch = true)
        {
            return RepositoryRequest.Execute<List<DomainObjects.Resource<T>>>(r =>
            {
                r.Data = Get<T>(Get<T>(type).Data, queries, bestMatch); //todo: inner messages will be lost
            });
        }

        public RepositoryResult<List<DomainObjects.Resource<T>>> Get<T>(string type, string key)
        {
            return RepositoryRequest.Execute<List<DomainObjects.Resource<T>>>(r =>
            {
                //return Find<T>(r => r.Type == type && r.Key == key);
                r.Data = _repository.Find<T>(x => x.Type == type && x.Key == key);
            });
        }

        public RepositoryResult<List<DomainObjects.Resource<T>>> Get<T>(string type, string key, Func<dynamic, dynamic> statement, bool bestMatch = true)
        {
            return RepositoryRequest.Execute<List<DomainObjects.Resource<T>>>(r =>
            {
                r.Data = Get<T>(Get<T>(type, key).Data, new List<DomainObjects.Query>() { new DomainObjects.Query(statement, 1) }, bestMatch);  //todo: inner messages will be lost
            });
        }

        public RepositoryResult<List<DomainObjects.Resource<T>>> Get<T>(string type, string key, List<DomainObjects.Query> queries, bool bestMatch = true)
        {
            return RepositoryRequest.Execute<List<DomainObjects.Resource<T>>>(r =>
            {
                r.Data = Get<T>(Get<T>(type, key).Data, queries, bestMatch);    //todo: inner messages will be lost
            });
        }

        public List<DomainObjects.Resource<T>> Get<T>(List<DomainObjects.Resource<T>> resources, List<DomainObjects.Query> queries, bool bestMatch = true)
        {
            var groupedItems = resources.GroupBy(i => i.GroupKey);
            var items = new List<DomainObjects.Resource<T>>();
            foreach (var group in groupedItems)
            {
                var matches = queries.GetMatches<DomainObjects.Resource<T>>(group, bestMatch);
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

        public RepositoryResult<T> Get<T>(string type, Func<dynamic, dynamic> statement, T defaultValue) //where T: new()
        {
            return Get(type, new List<DomainObjects.Query>() { new DomainObjects.Query(statement, 1) }, defaultValue);
        }

        public RepositoryResult<T> Get<T>(string type, List<DomainObjects.Query> queries, T defaultValue) //where T : new()
        {
            return Get<T>(type, null, queries, defaultValue);
        }

        public RepositoryResult<T> Get<T>(string type, string key, Func<dynamic, dynamic> statement, T defaultValue) //where T :new()
        {
            return Get(type, key, new List<DomainObjects.Query>() { new DomainObjects.Query(statement, 1) }, defaultValue);
        }

        public RepositoryResult<T> Get<T>(string type, string key, List<DomainObjects.Query> queries, T defaultValue) //where T: new()
        {
            return RepositoryRequest.Execute<T>(r =>
            {
                var items = Get<T>(type, key, queries, true).Data;
                r.Data = (items.Count > 0 ? items[0].Data : defaultValue);
            });
        }

        public RepositoryResult<T> Get<T>(List<DomainObjects.Resource<T>> resources, List<DomainObjects.Query> queries, T defaultValue) //where T : new()
        {
            return RepositoryRequest.Execute<T>(r =>
            {
                var items = Get<T>(resources, queries, true);
                r.Data = (items.Count > 0 ? items[0].Data : defaultValue);
            });
        }

        public RepositoryResult<DomainObjects.Resource<T>> Store<T>(string type, string key, T data, string UserId)
        {
            return RepositoryRequest.Execute<DomainObjects.Resource<T>>(r =>
            {
                string id = ((dynamic)data).Id; //todo: better trapping of errors if data doesn't support?!

                var resource = GetById<T>(id).Data;
                if (resource == null)
                    resource = new DomainObjects.Resource<T>(type, key, null, data);
                else
                    resource.Data = data;
                resource.Audit.Add(new DomainObjects.Audit(UserId, DateTime.UtcNow, "Save"));
                _repository.Store(resource);
                PendingUpdates++;
                r.Data = resource;
            });
        }

        public RepositoryResult<DomainObjects.Resource<T>> Store<T>(DomainObjects.Resource<T> resource, string userId)
        {
            return RepositoryRequest.Execute<DomainObjects.Resource<T>>(r =>
            {
                resource.Audit.Add(new DomainObjects.Audit(userId, DateTime.UtcNow, "Save"));
                _repository.Store(resource);
                PendingUpdates++;
                r.Data = resource;
            });
        }

        public void SaveChanges()
        {
            _repository.Save();
            PendingUpdates = 0;
        }

        public RepositoryResult<DomainObjects.Resource<T>> Delete<T>(DomainObjects.Resource<T> resource, string userId) 
        {
            return RepositoryRequest.Execute<DomainObjects.Resource<T>>(r =>
            {
                resource.Audit.Add(new DomainObjects.Audit(userId, DateTime.UtcNow, "Delete"));
                resource.ExpirationDate = DateTime.UtcNow;
                _repository.Store(resource);
                //_repository.Save();
                r.Data = resource;
            });
        }

        public void Purge<T>(DomainObjects.Resource<T> resource) 
        {
            _repository.Delete(resource);
            PendingUpdates++;
            //_repository.Save();
        }

        public void Dispose()
        {
            _repository.Dispose();
        }

    }
}
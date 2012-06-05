using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Caching;
using CodeEndeavors.Extensions;
using System.Collections.Concurrent;
using System.Configuration;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System.Diagnostics;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using CodeEndeavors.Extensions;

namespace CodeEndeavors.ResourceManager.MongoDb
{
    public class MongoDbRepository : IRepository, IDisposable
    {
        private Dictionary<string, object> _connection = null;

        private MongoServer _server;
        private MongoDatabase _database;
        
        public MongoDbRepository()
        {
        }

        public void Initialize(Dictionary<string, object> connection)
        {
            _connection = connection;
            var mongoUrl = connection.GetSetting("mongoUrl", "mongodb://localhost:27017");
            var dbName = connection.GetSetting("mongoDbName", "ResourceManager");
            _server = MongoServer.Create(mongoUrl);
            _database = _server.GetDatabase(dbName, SafeMode.True);

            Trace.WriteLine(string.Format("MongoDb setup: {0} - {1}", mongoUrl, dbName));
        }

        public void Dispose()
        {
        }

        public DomainObjects.Resource<T> GetResource<T>(string id)
        {
            return (from r in GetCollectionResourceQuery<T>()
                   where r.Id == id
                   select r).SingleOrDefault();
            //var dict = AllDict<T>();
            //if (dict.ContainsKey(id))
            //    return dict[id];
            //return null;
        }

        public List<T> Find<T>(Func<T, bool> predicate)
        {
            return GetCollectionQuery<T>().Where(predicate).ToList();
            //return AllResources<T>().Where(predicate).ToList();
        }

        public List<T> All<T>()
        {
            return GetCollectionQuery<T>().ToList();
            //return AllDict<T>().Values.ToList();    //todo:  perf problem????
        }

        public List<DomainObjects.Resource<T>> FindResources<T>(Func<DomainObjects.Resource<T>, bool> predicate)
        {
            return GetCollectionResourceQuery<T>().Where(predicate).ToList();
            //return AllResources<T>().Where(predicate).ToList();
        }

        public List<DomainObjects.Resource<T>> AllResources<T>()
        {
            return GetCollectionResourceQuery<T>().ToList();
            //return AllDict<T>().Values.ToList();    //todo:  perf problem????
        }

        //public ConcurrentDictionary<string, DomainObjects.Resource<T>> AllDict<T>()
        //{
        //    //var tableName = GetJsontableName<T>();
        //    //var resource = GetCollection<T>().FindAll().ToList();
        //    return GetCollectionQuery<T>().ToDictionary(r => r.Id);

        //    var dict = new ConcurrentDictionary<string, DomainObjects.Resource<T>>(resource.ToDictionary(r => r.Id));
        //    return dict;
        //    //    },
        //    //    tableName);
        //}

        public void Store<T>(T item)
        {
            var resource = GetResourceCollection<T>();
            //if (string.IsNullOrEmpty(item.Id))
            //{
            //    item.Id = ObjectId.GenerateNewId().ToString(); //Guid.NewGuid().ToString();    //todo: use another resource for this...
            //    //item.Id = Guid.NewGuid().ToString();
            //    try
            //    {
            //        ((dynamic)item.Data).Id = item.Id;  //todo: require Id on object?
            //    }
            //    catch { }
            //    //resource.Insert(item);
            //}
            //else
            //{
            //    //var query = Query.EQ("_id", item.Id);

            //    //resource.Update(
            //    //todo:

            //    //resource.Update(query,  
            //}
            resource.Save(item);
        }

        public void Store<T>(DomainObjects.Resource<T> item)
        {
            var resource = GetResourceCollection<T>();
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = ObjectId.GenerateNewId().ToString(); //Guid.NewGuid().ToString();    //todo: use another resource for this...
                //item.Id = Guid.NewGuid().ToString();
                try
                {
                    ((dynamic)item.Data).Id = item.Id;  //todo: require Id on object?
                }
                catch { }
                //resource.Insert(item);
            }
            else
            {
                //var query = Query.EQ("_id", item.Id);
                
                //resource.Update(
                //todo:
                
                //resource.Update(query,  
            }
            resource.Save(item);
        }

        private IQueryable<T> GetCollectionQuery<T>()
        {
            //http://stackoverflow.com/questions/9953180/asp-net-webapi-iqueryable-support-with-mongodb-official-c-sharp-driver
            return GetCollection<T>().AsQueryable<T>().Select(r => r);
        }

        private IQueryable<DomainObjects.Resource<T>> GetCollectionResourceQuery<T>()
        {
            //http://stackoverflow.com/questions/9953180/asp-net-webapi-iqueryable-support-with-mongodb-official-c-sharp-driver
            return GetResourceCollection<T>().AsQueryable<DomainObjects.Resource<T>>().Select(r => r);
        }

        private MongoCollection<T> GetCollection<T>()
        {
            var tableName = GetTableName<T>();
            return _database.GetCollection<T>(tableName);
        }

        private MongoCollection<DomainObjects.Resource<T>> GetResourceCollection<T>()
        {
            var tableName = GetTableName<DomainObjects.Resource<T>>();
            return _database.GetCollection<DomainObjects.Resource<T>>(tableName);
        }

        public void Delete<T>(DomainObjects.Resource<T> item)
        {
            var query = Query.EQ("_id", item.Id);
            var resource = GetResourceCollection<T>();
            resource.Remove(query);
        }

        public void Delete<T>(T item)
        {
            //var query = Query.EQ("_id", item.Id);
            //var resource = GetResourceCollection<T>();
            //resource.Remove(query);
        }

        public void Save()
        {
            //all logic done already
        }

        public void DeleteAll<T>()
        {
            var resource = GetResourceCollection<T>();
            resource.RemoveAll();
        }

        private string GetTableName<T>()
        {
            return typeof(T).ToString();
        }

    }
}

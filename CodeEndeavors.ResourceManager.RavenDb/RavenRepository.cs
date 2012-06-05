using System;
using System.Linq;
using Raven.Client;
using Raven.Client.Embedded;
using System.Collections.Generic;
using Raven.Abstractions.Data;

namespace CodeEndeavors.ResourceManager.RavenDb
{
    public class RavenRepository : IRepository, IDisposable  
    {
        private IDocumentSession _session;
        private string _connection = null;


        public RavenRepository()
        {
        }

        public void Initialize(string Connection)
        {
            _connection = Connection;
            _session = Instance(Connection).OpenSession();
        }

        public void Dispose()
        {
            if (_session != null)
            {
                _session.Dispose();
            }
        }

        public DomainObjects.Resource<T> GetResource<T>(string Id) 
        {
            return _session.Query<DomainObjects.Resource<T>>()
                .Customize(d => d.WaitForNonStaleResults())
                .SingleOrDefault(d => d.Id == Id);
        }

        public List<DomainObjects.Resource<T>> FindResources<T>(Func<DomainObjects.Resource<T>, bool> predicate) 
        {
            return _session.Query<DomainObjects.Resource<T>>()
                .Customize(d => d.WaitForNonStaleResults())
                .Where(predicate).ToList();
        }

        public List<DomainObjects.Resource<T>> AllResources<T>() 
        {
            return _session.Query<DomainObjects.Resource<T>>()
                .Customize(d => d.WaitForNonStaleResults())
                .Take(Int32.MaxValue)
                .ToList();
        }

        public void Store<T>(DomainObjects.Resource<T> item)
        {
            _session.Store(item);
        }

        public void Delete<T>(DomainObjects.Resource<T> item)
        {
            _session.Delete(item);
        }

        public void Save()
        {
            _session.SaveChanges();
            //_session.Dispose();
            //_session = Instance(_connection).OpenSession();
        }

        public void DeleteAll<T>()
        {
            _session.Advanced.DatabaseCommands.DeleteByIndex("Auto/ResourcesOf" + typeof(T).Name + "s", new IndexQuery() { Query = "" }, true);

        }

        private static Dictionary<string, IDocumentStore> _instances = new Dictionary<string,IDocumentStore>();
        private static IDocumentStore Instance(string Connection)
        {
            if (!_instances.ContainsKey(Connection))
            {
                _instances[Connection] = new EmbeddableDocumentStore 
                { 
                    ConnectionStringName = Connection,
                    UseEmbeddedHttpServer = true    //todo:  verify - http://old.ravendb.net/faq/embedded-with-http
                };
                _instances[Connection].Conventions.IdentityPartsSeparator = "-"; //???
                _instances[Connection].Initialize();
                try
                {
                    Raven.Database.Server.NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(80);  //todo: make configurable
                }
                catch (Exception ex)
                {

                }
                //throw new InvalidOperationException("IDocumentStore has not been initialized.");
            }
            return _instances[Connection];
        }

    }
}
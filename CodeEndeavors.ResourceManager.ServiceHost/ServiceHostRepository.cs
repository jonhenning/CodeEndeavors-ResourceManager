using CodeEndeavors.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RepositoryDomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;
using DomainObjects = CodeEndeavors.ResourceManager.DomainObjects;
using System.Collections.Concurrent;
using CodeEndeavors.ResourceManager;
using CodeEndeavors.Services.ResourceManager.Client;
using CodeEndeavors.ServiceHost.Common.Services;

namespace CodeEndeavors.ResourceManager.ServiceHost
{
    public class ServiceHostRepository : IRepository, IDisposable
    {
        private Dictionary<string, object> _connection = null;
        private Dictionary<string, object> _cacheConnection = null;

        //private string _cacheName;
        private string _namespace;
        //private bool _useFileMonitor;
        private int _auditHistorySize;
        private bool _enableAudit;

        private ConcurrentDictionary<string, List<RepositoryDomainObjects.Resource>> _pendingResourceUpdates = new ConcurrentDictionary<string, List<RepositoryDomainObjects.Resource>>();
        private ConcurrentDictionary<string, List<RepositoryDomainObjects.ResourceAudit>> _pendingAuditUpdates = new ConcurrentDictionary<string, List<RepositoryDomainObjects.ResourceAudit>>();

        //repository instance lifespan is a single request (for videre) - not ideal as we really don't want to dictate how resourcemanager is used in relation to its lifespan
        private ConcurrentDictionary<string, object> _pendingDict = new ConcurrentDictionary<string, object>();

        public ServiceHostRepository()
        {
        }

        public void Initialize(Dictionary<string, object> connection, Dictionary<string, object> cacheConnection)
        {
            _connection = connection;
            _cacheConnection = cacheConnection;
            var cacheName = _cacheConnection.GetSetting("cacheName", "");
            _namespace = _connection.GetSetting("namespace", "");
            //_useFileMonitor = _connection.GetSetting("useFileMonitor", false);
            _auditHistorySize = _connection.GetSetting("auditHistorySize", 10); //todo: how is this getting passed up the chain?
            _enableAudit = _auditHistorySize > 0;

            var url = _connection.GetSetting("url", "");
            var requestTimeout = _connection.GetSetting("requestTimeout", 600000);
            var httpUser = _connection.GetSetting("httpUser", "");
            var httpPassword = _connection.GetSetting("httpPassword", "");
            var authenticationType = _connection.GetSetting("authenticationType", "None");
            var logLevel = _connection.GetSetting("logLevel", "Info");

            if (string.IsNullOrEmpty(url))
                throw new Exception("url key not found in connection");

            if (authenticationType != "None")
                ServiceLocator.Register<RepositoryService>(url, requestTimeout, httpUser, httpPassword, authenticationType.ToType<AuthenticationType>());
            else
                ServiceLocator.Register<RepositoryService>(url, requestTimeout);

            //todo: setaquireuserid?!?!?!
            ServiceLocator.Resolve<RepositoryService>().SetAquireUserIdDelegate(() => { return "5"; }); //FIX;
            ServiceLocator.Resolve<RepositoryService>().ConfigureLogging("ResourceManager", logLevel, (string level, string message) =>
                {
                    Logging.Log(Logging.LoggingLevel.Minimal, message); //todo: map log levels
                });
            ServiceLocator.Resolve<RepositoryService>().ConfigureCache(cacheName, _cacheConnection.ToJson());

            //Logging.Log(Logging.LoggingLevel.Minimal, "ServiceHost Repository Initialized");
        }


        public DomainObjects.Resource<T> GetResource<T>(string id)
        {
            var dict = AllDict<T>();
            if (!string.IsNullOrEmpty(id) && dict.ContainsKey(id))
                return dict[id];
            return null;
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

            Func<ConcurrentDictionary<string, DomainObjects.Resource<T>>> getDelegate = delegate()
            {
                var resource = new List<DomainObjects.Resource<T>>();
                var sr = RepositoryService.Resolve().GetResources(resourceType, _enableAudit, _namespace);

                if (sr.Success)
                {
                    foreach (var resourceRow in sr.Data)
                    {
                        resource.Add(toResource<T>(resourceRow));
                    }
                    //todo: handle Audit
                    return new ConcurrentDictionary<string, DomainObjects.Resource<T>>(resource.ToDictionary(r => r.Id));
                }
                else
                    throw new Exception(sr.ToString());
            };

            //var dict = CodeEndeavors.Distributed.Cache.Client.Service.GetCacheEntry(_cacheName, resourceType, getDelegate, null); //getMonitorOptions(fileName));
            var dict = getDelegate();
            _pendingDict[resourceType] = dict;
            return dict;
        }

        public void Store<T>(DomainObjects.Resource<T> item)
        {
            var resourceType = getResourceType<T>();
            var rowState = RepositoryDomainObjects.RowStateEnum.Modified;

            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = Guid.NewGuid().ToString();    //todo: use another resource for this...
                try
                {
                    ((dynamic)item.Data).Id = item.Id;  //todo: require Id on object?
                }
                catch { }
                rowState = RepositoryDomainObjects.RowStateEnum.Added;
            }

            var dict = AllDict<T>();
            dict[item.Id] = item;   //item is inserted into "local" cache here, we need to add it to Redis if we have it!

            //_cacheName, "Table", TimeSpan.FromHours(1), new List<string> { resourceType }, resourceType, resourceType

            //Storing updated dictionary directly before update!!!!!
            //we cache the DomainObjects.Resource, not the direct object
            //RepositoryService.Resolve().SetCacheEntry(resourceType, null, resourceType, dict.Values.Select(d => toRepositoryResource(d, Services.ResourceManager.Shared.DomainObjects.RowStateEnum.Unchanged)).ToList());
            RepositoryService.Resolve().SetResource(resourceType, null, toRepositoryResource(item, Services.ResourceManager.Shared.DomainObjects.RowStateEnum.Unchanged), _namespace);

            if (!_pendingResourceUpdates.ContainsKey(resourceType))
                _pendingResourceUpdates[resourceType] = new List<RepositoryDomainObjects.Resource>();
            if (!_pendingAuditUpdates.ContainsKey(resourceType))
                _pendingAuditUpdates[resourceType] = new List<RepositoryDomainObjects.ResourceAudit>();

            _pendingResourceUpdates[resourceType].Add(toRepositoryResource(item, rowState));
            if (item.Audit.Count > 0)
                _pendingAuditUpdates[resourceType].Add(toRepositoryAudit(item.Id, item.Audit.FirstOrDefault()));
        }

        public void Save()
        {
            foreach (var resourceType in _pendingResourceUpdates.Keys)
            {
                if (_pendingResourceUpdates[resourceType].Count > 0)
                {
                    if (_pendingAuditUpdates.ContainsKey(resourceType))
                        _pendingResourceUpdates[resourceType].ForEach(r => r.ResourceAudits = _pendingAuditUpdates[resourceType].Where(a => a.ResourceId == r.Id).ToList());

                    var sr = RepositoryService.Resolve().SaveResources(_pendingResourceUpdates[resourceType], _namespace);
                    if (!sr.Success)
                        throw new Exception(sr.ToString());

                    //expireCacheEntry(resourceType);
                }
            }
            _pendingResourceUpdates = new ConcurrentDictionary<string, List<RepositoryDomainObjects.Resource>>();
            _pendingAuditUpdates = new ConcurrentDictionary<string, List<RepositoryDomainObjects.ResourceAudit>>();
        }

        public void Delete<T>(DomainObjects.Resource<T> item)
        {
            var resourceType = getResourceType<T>();

            if (!_pendingResourceUpdates.ContainsKey(resourceType))
                _pendingResourceUpdates[resourceType] = new List<RepositoryDomainObjects.Resource>();
            _pendingResourceUpdates[resourceType].Add(toRepositoryResource(item, RepositoryDomainObjects.RowStateEnum.Deleted));
        }

        public void DeleteAll<T>()
        {
            var resourceType = getResourceType<T>();
            var sr = RepositoryService.Resolve().DeleteAll(resourceType, "", _namespace);
            if (sr.Success)
            {
                //expireCacheEntry(resourceType);
                List<RepositoryDomainObjects.Resource> res;
                List<RepositoryDomainObjects.ResourceAudit> resAudit;
                _pendingResourceUpdates.TryRemove(resourceType, out res);
                _pendingAuditUpdates.TryRemove(resourceType, out resAudit);
            }
            else
                throw new Exception(sr.ToString());
        }

        private string getResourceType<T>()
        {
            return typeof(T).ToString();
        }

        //private void expireCacheEntry(string key)
        //{
        //    CodeEndeavors.Distributed.Cache.Client.Service.ExpireCacheEntry(_cacheName, key);
        //}

        private object getMonitorOptions(string fileName)
        {
            //if (_useFileMonitor)
            //    return new { monitorType = "CodeEndeavors.Distributed.Cache.Client.File.FileMonitor", fileName = fileName, uniqueProperty = "fileName" };
            return null;
        }

        private DomainObjects.Resource<T> toResource<T>(RepositoryDomainObjects.Resource resource)
        {
            var ret = new DomainObjects.Resource<T>();
            ret.Id = resource.Id;
            ret.Key = resource.Key;
            ret.Type = resource.Type;
            ret.EffectiveDate = resource.EffectiveDate;
            ret.ExpirationDate = resource.ExpirationDate;
            var json = resource.Data;
            if (!string.IsNullOrEmpty(json))
                ret.Data = json.ToObject<T>();
            json = resource.Scope;
            if (!string.IsNullOrEmpty(json))
                ret.Scope = json.ToObject<dynamic>();

            foreach (var audit in resource.ResourceAudits)
            {
                ret.Audit.Add(new DomainObjects.Audit()
                {
                    Action = audit.Action,
                    UserId = audit.UserId,
                    Date = audit.AuditDate
                });
            }
            return ret;
        }

        private RepositoryDomainObjects.Resource toRepositoryResource<T>(DomainObjects.Resource<T> resource, RepositoryDomainObjects.RowStateEnum rowState)
        {
            return new RepositoryDomainObjects.Resource()
                {
                    Id = resource.Id,
                    ResourceType = getResourceType<T>(),
                    Key = resource.Key,
                    Type = resource.Type,
                    Sequence = resource.Sequence,

                    EffectiveDate = resource.EffectiveDate,
                    ExpirationDate = resource.ExpirationDate,
                    Data = resource.Data != null ? resource.Data.ToJson(false, "db") : null,
                    Scope = resource.Scope != null ? ((object)resource.Scope).ToJson() : null,
                    RowState = rowState
                };
        }

        private RepositoryDomainObjects.ResourceAudit toRepositoryAudit(string resourceId, DomainObjects.Audit audit)
        {
            return new RepositoryDomainObjects.ResourceAudit()
            {
                ResourceId = resourceId,
                UserId = audit.UserId,
                Action = audit.Action
            };
        }

        public void Dispose()
        {

        }

    }
}

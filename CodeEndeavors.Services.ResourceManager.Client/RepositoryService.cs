using System.Linq;
using CodeEndeavors.Extensions;
using CodeEndeavors.ServiceHost.Common.Client;
using CodeEndeavors.ServiceHost.Common.Services;
using System;
using System.Collections.Generic;
using DomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;
//using DomainObjects = CodeEndeavors.ResourceManager.DomainObjects;
using Logger = CodeEndeavors.ServiceHost.Common.Services.Logging;
using CacheService = CodeEndeavors.Distributed.Cache.Client.Service;

namespace CodeEndeavors.Services.ResourceManager.Client
{
    public class RepositoryService : BaseClient
    {
        private IRepositoryService _service;

        public RepositoryService()
        {
            Helpers.HandleAssemblyResolve();
            //_service = new Stubs.Repository();
        }

        public RepositoryService(string httpServiceUrl, int requestTimeout, string restfulServerExtension)
        {
            Helpers.HandleAssemblyResolve();
            _service = new Http.Repository(httpServiceUrl, requestTimeout, restfulServerExtension);
        }
        public RepositoryService(string httpServiceUrl, int requestTimeout, string restfulServerExtension, string httpUser, string httpPassword, string authenticationType)
        { 
            Helpers.HandleAssemblyResolve();
            _service = new Http.Repository(httpServiceUrl, requestTimeout, restfulServerExtension, httpUser, httpPassword, authenticationType);
        }

        public ClientCommandResult<List<DomainObjects.Resource>> GetResources(string resourceType, bool includeAudit)
        {
            return GetResources(resourceType, includeAudit, null);
        }
        public ClientCommandResult<List<DomainObjects.Resource>> GetResources(string resourceType, bool includeAudit, string ns)
        {
            return Cache.Execute<List<DomainObjects.Resource>>(_cacheName, "Table", TimeSpan.FromHours(1), new List<string> { resourceType }, "Query:GetResources", resourceType, () =>
            {
                return _service.GetResources(resourceType, includeAudit, ns);
            });
        }

        public ClientCommandResult<bool> SaveResources(List<DomainObjects.Resource> resources)
        {
            return SaveResources(resources, null);
        }
        public ClientCommandResult<bool> SaveResources(List<DomainObjects.Resource> resources, string ns)
        {
            return ClientCommandResult<bool>.Execute(result =>
            {
                resources.ForEach(r => r.Namespace = ns);
                result.ReportResult(_service.SaveResources(resources), true);
                var resourceTypes = resources.Select(r => r.ResourceType).Distinct().ToList();
                foreach (var resourceType in resourceTypes)
                    CacheService.ExpireCacheDependencies(_cacheName, "Table", resourceType);
            });
        }
        public ClientCommandResult<bool> DeleteAll(string resourceType, string type)
        {
            return DeleteAll(resourceType, type, null);
        }
        public ClientCommandResult<bool> DeleteAll(string resourceType, string type, string ns)
        {
            return ClientCommandResult<bool>.Execute(result =>
            {
                result.ReportResult(_service.DeleteAll(resourceType, type, ns), true);
                CacheService.ExpireCacheDependencies(_cacheName, "Table", resourceType);
            });
        }
        public ClientCommandResult<bool> DeleteAll(string resourceType)
        {
            return ClientCommandResult<bool>.Execute(result =>
            {
                result.ReportResult(_service.DeleteAll(resourceType, "", null), true);
                CacheService.ExpireCacheDependencies(_cacheName, "Table", resourceType);
            });
        }

        #region Common Client Methods

        [Obsolete]
        public static void Register(string url, int requestTimeout)
        {
            ServiceLocator.Register<Client.RepositoryService>(url, requestTimeout);
        }

        [Obsolete]
        public static void Register(string url, int requestTimeout, string httpUser, string httpPassword, string authenticationType)
        {
            ServiceLocator.Register<Client.RepositoryService>(url, requestTimeout, httpUser, httpPassword, authenticationType.ToType<AuthenticationType>());
        }

        public override void SetAquireUserIdDelegate(Func<string> func)
        {
            _service.SetAquireUserIdDelegate(func);
        }

        private string _cacheName = null;
        public void ConfigureCache(string cacheName, string connection)
        {
            ConfigureCache(cacheName, connection, Distributed.Cache.Client.Logging.LoggingLevel.Minimal);
        }

        public void ConfigureCache(string cacheName, string connection, CodeEndeavors.Distributed.Cache.Client.Logging.LoggingLevel logLevel)
        {
            _cacheName = cacheName;
            if (!string.IsNullOrEmpty(cacheName))
            {
                CacheService.RegisterCache(cacheName, connection);

                if (CodeEndeavors.Distributed.Cache.Client.Logging.LoggingHandlerCount == 0)
                {
                    CodeEndeavors.Distributed.Cache.Client.Logging.LogLevel = logLevel;
                    CodeEndeavors.Distributed.Cache.Client.Logging.OnLoggingMessage += (message) =>
                    {
                        Logging.Log(Logging.LoggingLevel.Debug, message);
                    };
                }
            }
        }

        [Obsolete]
        public static Client.RepositoryService Resolve()
        {
            return ServiceLocator.Resolve<Client.RepositoryService>();
        }

        #endregion

    }
}

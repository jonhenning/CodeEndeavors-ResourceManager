using CodeEndeavors.Extensions;
using CodeEndeavors.ServiceHost.Common.Services;
using System;
using System.Collections.Generic;
using DomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;
//using DomainObjects = CodeEndeavors.ResourceManager.DomainObjects;
using Logger = CodeEndeavors.ServiceHost.Common.Services.Logging;

namespace CodeEndeavors.Services.ResourceManager.Client
{
    public class RepositoryService
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
            return ClientCommandResult<List<DomainObjects.Resource>>.Execute(result =>
            {
                result.ReportResult(_service.GetResources(resourceType, includeAudit, ns), true);
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
            });
        }
        public ClientCommandResult<bool> DeleteAll(string resourceType)
        {
            return ClientCommandResult<bool>.Execute(result =>
            {
                result.ReportResult(_service.DeleteAll(resourceType, "", null), true);
            });
        }

        #region Common Client Methods
        public void SetAquireUserIdDelegate(Func<string> func)
        {
            _service.SetAquireUserIdDelegate(func);
        }

        public void ConfigureLogging(string logLevel, Action<string, string> onLoggingMessage)
        {
            Logger.LogLevel = logLevel.ToType<Logger.LoggingLevel>();
            Logger.OnLoggingMessage += (Logger.LoggingLevel level, string message) =>
            {
                if (onLoggingMessage != null)
                    onLoggingMessage(level.ToString(), message);
            };
        }

        public static void Register(string url, int requestTimeout)
        {
            ServiceLocator.Register<Client.RepositoryService>(url, requestTimeout);
        }
        public static void Register(string url, int requestTimeout, string httpUser, string httpPassword, string authenticationType)
        {
            ServiceLocator.Register<Client.RepositoryService>(url, requestTimeout, httpUser, httpPassword, authenticationType.ToType<AuthenticationType>());
        }

        public static Client.RepositoryService Resolve()
        {
            return ServiceLocator.Resolve<Client.RepositoryService>();
        }
        #endregion

    }
}

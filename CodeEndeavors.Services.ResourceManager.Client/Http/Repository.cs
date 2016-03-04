using System;
using System.Collections.Generic;
using CodeEndeavors.ServiceHost.Common.Services;
//using CodeEndeavors.ServiceHost.Common.Services.LoggingServices;
using DomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;

namespace CodeEndeavors.Services.ResourceManager.Client.Http
{
    public class Repository : CodeEndeavors.ServiceHost.Common.Services.BaseClientHttpService, IRepositoryService
    {
        public Repository(string httpServiceUrl, int requestTimeout, string restfulServerExtension)
            : base("Repository", httpServiceUrl, requestTimeout, restfulServerExtension)
        {
        }

        public Repository(string httpServiceUrl, int requestTimeout, string restfulServerExtension, string httpUser, string httpPassword, AuthenticationType authenticationType)
            : base("Repository", httpServiceUrl, requestTimeout, restfulServerExtension, httpUser, httpPassword, authenticationType)
        {
        }

        public void SetAquireUserIdDelegate(Func<string> func)
        {
            base.AquireUserIdDelegate = func;
        }


        public ServiceResult<List<DomainObjects.Resource>> GetResources(string resourceType, bool includeAudits)
        {
            return base.GetHttpRequestObject<ServiceResult<List<DomainObjects.Resource>>>(base.RequestUrl("ResourcesGet"), new { resourceType = resourceType, includeAudits = includeAudits });
        }

        public ServiceResult<bool> SaveResources(List<DomainObjects.Resource> resources)
        {
            return base.GetHttpRequestObject<ServiceResult<bool>>(base.RequestUrl("ResourcesSave"), resources);
        }

        public ServiceResult<bool> DeleteAll(string resourceType, string type)
        {
            return base.GetHttpRequestObject<ServiceResult<bool>>(base.RequestUrl("DeleteAll"), new { resourceType = resourceType, type = type });
        }

    }
}

using CodeEndeavors.Extensions;
using CodeEndeavors.ServiceHost;
using CodeEndeavors.ServiceHost.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using DomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;

namespace CodeEndeavors.Services.ResourceManager.Controllers
{
    public class RepositoryController : BaseController
    {
        private static Repository _repositoryService;

        private Repository RepositoryService
        {
            get
            {
                if (_repositoryService == null)
                    _repositoryService = new Repository();
                return _repositoryService;
            }
        }

        [HttpPost]
        public ServiceResult<List<DomainObjects.Resource>> ResourcesGet(string userId, [FromBody]Dictionary<string, object> arguments)
        {
            return RepositoryService.GetResources(arguments.GetSetting("resourceType", ""), arguments.GetSetting("includeAudits", false));
        }

        [HttpPost]
        public ServiceResult<bool> ResourcesSave(string userId, [FromBody]List<DomainObjects.Resource> resources)
        {
            return RepositoryService.SaveResources(resources, userId);
        }

        [HttpPost]
        public ServiceResult<bool> ResourcesDeleteAll(string userId, [FromBody]Dictionary<string, object> arguments)
        {
            return RepositoryService.DeleteAll(arguments.GetSetting("resourceType", ""), arguments.GetSetting("type", ""));
        }

        //[HttpPost]
        //public ServiceResult<bool> CustomerSave(string userId, [FromBody]DomainObjects.Resource customer)
        //{
        //    return RepositoryService.CustomerSave(customer, userId);
        //}


    }
}

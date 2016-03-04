using CodeEndeavors.ServiceHost.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;

namespace CodeEndeavors.Services.ResourceManager.Client
{
    public interface IRepositoryService
    {
        ServiceResult<List<DomainObjects.Resource>> GetResources(string resourceType, bool includeAudits);
        ServiceResult<bool> SaveResources(List<DomainObjects.Resource> resources);
        ServiceResult<bool> DeleteAll(string resourceType, string type);
        
        void SetAquireUserIdDelegate(Func<string> func);


    }
}

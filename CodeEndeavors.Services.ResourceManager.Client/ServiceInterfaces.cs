﻿using CodeEndeavors.ServiceHost.Common.Services;
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
        ServiceResult<List<DomainObjects.Resource>> GetResources(string resourceType, bool includeAudits, string ns);
        ServiceResult<bool> SaveResources(List<DomainObjects.Resource> resources);
        ServiceResult<bool> DeleteAll(string resourceType, string type, string ns);
        ServiceResult<string> ObtainLock(string source, string ns);
        ServiceResult<bool> RemoveLock(string source, string ns);
        void SetAquireUserIdDelegate(Func<string> func);


    }
}

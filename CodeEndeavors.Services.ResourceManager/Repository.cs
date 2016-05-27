using System.Data.Entity;
using CodeEndeavors.ServiceHost;
using CodeEndeavors.ServiceHost.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;

namespace CodeEndeavors.Services.ResourceManager
{

    public class Repository : BaseService
    {
        private string _connection;

        public Repository()
        {
            base.Configure();
            _connection = GetConnectionString("resourcemanagerConnection", "");
            Schema.EnsureSchema(_connection);
        }

        public ServiceResult<List<DomainObjects.Resource>> GetResources(string resourceType, bool includeAudit, string ns)
        {
            return this.ExecuteServiceResult<List<DomainObjects.Resource>>(result =>
            {
                using (var db = new Data.resourcemanagerContext(_connection))
                {
                    db.Configuration.ProxyCreationEnabled = false;
                    var resources = db.Resources.Where(r => r.ResourceType.Equals(resourceType, StringComparison.InvariantCultureIgnoreCase) && (string.IsNullOrEmpty(ns) || r.Namespace.Equals(ns, StringComparison.InvariantCultureIgnoreCase))).AsNoTracking().ToList();
                    if (includeAudit)
                    {
                        var ids = resources.Select(r => r.Id).ToList();
                        var audits = db.ResourceAudits.Where(a => ids.Contains(a.ResourceId)).AsNoTracking().ToList();
                        resources.ForEach(r => r.ResourceAudits = audits.Where(a => a.ResourceId == r.Id).ToList());
                    }
                    result.ReportResult(resources, true);
                }
            });
        }

        public ServiceResult<bool> SaveResources(List<DomainObjects.Resource> resources, string userId)
        {
            return this.ExecuteServiceResult<bool>(result =>
            {
                using (var db = new Data.resourcemanagerContext(_connection))
                {
                    var ids = resources.Where(r => !string.IsNullOrEmpty(r.Id)).Select(r => r.Id).ToList();
                    var existingResources = db.Resources.Where(r => ids.Contains(r.Id)).ToList();

                    foreach (var resource in resources)
                    {
                        if (resource.RowState == DomainObjects.RowStateEnum.Modified)
                        {
                            var existing = existingResources.Where(r => r.Id == resource.Id).FirstOrDefault();
                            if (existing != null)
                                db.Entry(existing).CurrentValues.SetValues(resource);
                            else
                            {
                                db.Resources.Add(resource);
                                existingResources.Add(resource);    //add this here as well, if it is updated later in batch
                            }
                        }
                        else if (resource.RowState == DomainObjects.RowStateEnum.Added)
                        {
                            db.Resources.Add(resource);
                            existingResources.Add(resource);    //add this here as well, if it is updated later in batch
                        }
                        else if (resource.RowState == DomainObjects.RowStateEnum.Deleted)
                        {
                            var existing = existingResources.Where(r => r.Id == resource.Id).FirstOrDefault();
                            var audits = db.ResourceAudits.Where(r => r.ResourceId == resource.Id).ToList();
                            if (existing != null)
                            {
                                audits.ForEach(a => db.ResourceAudits.Remove(a));
                                db.Resources.Remove(existing);
                            }
                        }
                    }
                    db.SaveChanges();
                    result.ReportResult(true, true);
                }
            });
        }

        public ServiceResult<bool> DeleteAll(string resourceType, string type, string ns)
        {
            return this.ExecuteServiceResult<bool>(result =>
            {
                using (var db = new Data.resourcemanagerContext(_connection))
                {
                    var resources = db.Resources.Where(r => r.ResourceType.Equals(resourceType, StringComparison.InvariantCultureIgnoreCase) && (string.IsNullOrEmpty(type) || r.Type.Equals(type, StringComparison.InvariantCultureIgnoreCase)) && (string.IsNullOrEmpty(ns) || r.Namespace.Equals(ns, StringComparison.InvariantCultureIgnoreCase))).ToList();
                    var ids = resources.Select(r => r.Id).ToList();
                    var audits = db.ResourceAudits.Where(r => ids.Contains(r.ResourceId)).ToList();
                    foreach (var audit in audits)
                        db.ResourceAudits.Remove(audit);
                    foreach (var resource in resources)
                        db.Resources.Remove(resource);
                    db.SaveChanges();
                    result.ReportResult(true, true);
                }
            });
        }
    }
}

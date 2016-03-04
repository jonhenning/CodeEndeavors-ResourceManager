using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;

namespace CodeEndeavors.Services.ResourceManager.Data.Mapping
{
    internal partial class ResourceMap : EntityTypeConfiguration<DomainObjects.Resource>
    {
        partial void InitializePartial()
        {
            Ignore(x => x.RowState);
        }
    }
}

// ReSharper disable RedundantUsingDirective
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable RedundantNameQualifier
// TargetFrameworkVersion = 4.51
#pragma warning disable 1591    //  Ignore "Missing XML Comment" warning

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEndeavors.Services.ResourceManager.Shared.DomainObjects
{
    // Resource
    [GeneratedCodeAttribute("EF.Reverse.POCO.Generator", "2.13.1.0")]
    public partial class Resource
    {
        public string Id { get; set; } // Id (Primary key)
        public string ResourceType { get; set; } // ResourceType
        public string Key { get; set; } // Key
        public string Type { get; set; } // Type
        public int? Sequence { get; set; } // Sequence
        public DateTimeOffset? EffectiveDate { get; set; } // EffectiveDate
        public DateTimeOffset? ExpirationDate { get; set; } // ExpirationDate
        public string Scope { get; set; } // Scope
        public string Data { get; set; } // Data
        public string Namespace { get; set; } // Namespace

        // Reverse navigation
        public virtual ICollection<ResourceAudit> ResourceAudits { get; set; } // ResourceAudit.FK_ResourceAudit_Resource
        
        public Resource()
        {
            ResourceAudits = new List<ResourceAudit>();
            InitializePartial();
        }

        partial void InitializePartial();
    }

}

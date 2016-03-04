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
    // ResourceAudit
    public partial class ResourceAudit
    {
        public int Id { get; set; } // Id (Primary key)
        public string ResourceId { get; set; } // ResourceId
        public string UserId { get; set; } // UserId
        public DateTimeOffset AuditDate { get; set; } // AuditDate
        public string Action { get; set; } // Action

        // Foreign keys
        public virtual Resource Resource { get; set; } // FK_ResourceAudit_Resource
    }

}

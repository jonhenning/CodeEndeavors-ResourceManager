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
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data;
using System.Data.SqlClient;
using CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;
using CodeEndeavors.Services.ResourceManager.Data.Mapping;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEndeavors.Services.ResourceManager.Data
{
    public partial class ResourceLock_ObtainLockReturnModel
    {
        public String Source { get; set; }
        public String Namespace { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

}

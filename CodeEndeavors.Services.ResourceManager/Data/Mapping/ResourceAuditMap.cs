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
using System.Data.Entity.ModelConfiguration;
using CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;
using CodeEndeavors.Services.ResourceManager.Data;
using System.Threading;
using System.Threading.Tasks;
using DatabaseGeneratedOption = System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption;

namespace CodeEndeavors.Services.ResourceManager.Data.Mapping
{
    // ResourceAudit
    internal partial class ResourceAuditMap : EntityTypeConfiguration<CodeEndeavors.Services.ResourceManager.Shared.DomainObjects.ResourceAudit>
    {
        public ResourceAuditMap(string schema = "dbo")
        {
            ToTable(schema + ".ResourceAudit");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("Id").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.ResourceId).HasColumnName("ResourceId").IsRequired().IsUnicode(false).HasMaxLength(100);
            Property(x => x.UserId).HasColumnName("UserId").IsOptional().IsUnicode(false).HasMaxLength(100);
            Property(x => x.AuditDate).HasColumnName("AuditDate").IsRequired();
            Property(x => x.Action).HasColumnName("Action").IsRequired().IsUnicode(false).HasMaxLength(20);

            // Foreign keys
            HasRequired(a => a.Resource).WithMany(b => b.ResourceAudits).HasForeignKey(c => c.ResourceId); // FK_ResourceAudit_Resource
            InitializePartial();
        }
        partial void InitializePartial();
    }

}

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
    // Resource
    internal partial class ResourceMap : EntityTypeConfiguration<CodeEndeavors.Services.ResourceManager.Shared.DomainObjects.Resource>
    {
        public ResourceMap(string schema = "dbo")
        {
            ToTable(schema + ".Resource");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("Id").IsRequired().IsUnicode(false).HasMaxLength(100).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.ResourceType).HasColumnName("ResourceType").IsOptional().IsUnicode(false).HasMaxLength(200);
            Property(x => x.Key).HasColumnName("Key").IsOptional().IsUnicode(false).HasMaxLength(200);
            Property(x => x.Type).HasColumnName("Type").IsOptional().IsUnicode(false).HasMaxLength(200);
            Property(x => x.Sequence).HasColumnName("Sequence").IsOptional();
            Property(x => x.EffectiveDate).HasColumnName("EffectiveDate").IsOptional();
            Property(x => x.ExpirationDate).HasColumnName("ExpirationDate").IsOptional();
            Property(x => x.Scope).HasColumnName("Scope").IsOptional().HasMaxLength(500);
            Property(x => x.Data).HasColumnName("Data").IsOptional().HasMaxLength(1073741823);
            Property(x => x.Namespace).HasColumnName("Namespace").IsOptional().IsUnicode(false).HasMaxLength(200);
            InitializePartial();
        }
        partial void InitializePartial();
    }

}

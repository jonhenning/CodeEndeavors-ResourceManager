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
    public partial class resourcemanagerContext : DbContext, IresourcemanagerContext
    {
        public IDbSet<CodeEndeavors.Services.ResourceManager.Shared.DomainObjects.Resource> Resources { get; set; } // Resource
        public IDbSet<CodeEndeavors.Services.ResourceManager.Shared.DomainObjects.ResourceAudit> ResourceAudits { get; set; } // ResourceAudit
        
        static resourcemanagerContext()
        {
            Database.SetInitializer<resourcemanagerContext>(null);
        }

        public resourcemanagerContext()
            : base("Name=CodeEndeavors.Services.ResourceManager.Properties.Settings.resourcemanagerConnection")
        {
            InitializePartial();
        }

        public resourcemanagerContext(string connectionString) : base(connectionString)
        {
            InitializePartial();
        }

        public resourcemanagerContext(string connectionString, System.Data.Entity.Infrastructure.DbCompiledModel model) : base(connectionString, model)
        {
            InitializePartial();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Configurations.Add(new ResourceMap());
            modelBuilder.Configurations.Add(new ResourceAuditMap());

            OnModelCreatingPartial(modelBuilder);
        }

        public static DbModelBuilder CreateModel(DbModelBuilder modelBuilder, string schema)
        {
            modelBuilder.Configurations.Add(new ResourceMap(schema));
            modelBuilder.Configurations.Add(new ResourceAuditMap(schema));
            return modelBuilder;
        }

        partial void InitializePartial();
        partial void OnModelCreatingPartial(DbModelBuilder modelBuilder);

		//custom conversion handlers
		private SqlParameter applyDBNull(SqlParameter param)
		{
			if (param.Value == null)
				param.Value = DBNull.Value;
			return param;
		}

		private SqlParameter unapplyDBNull(SqlParameter param)
		{
			if (param.Value == DBNull.Value)
				param.Value = null;
			return param;
		}

        
        // Stored Procedures
        public List<ResourceLock_ObtainLockReturnModel> ResourceLockObtainLock(string ns, string source, int? timeoutMinutes, out int procResult)
        {
            var nsParam = applyDBNull(new SqlParameter { ParameterName = "@ns", SqlDbType = SqlDbType.VarChar, Direction = ParameterDirection.Input, Value = ns, Size = 50 });
            var sourceParam = applyDBNull(new SqlParameter { ParameterName = "@source", SqlDbType = SqlDbType.VarChar, Direction = ParameterDirection.Input, Value = source, Size = 50 });
            var timeoutMinutesParam = applyDBNull(new SqlParameter { ParameterName = "@timeout_minutes", SqlDbType = SqlDbType.Int, Direction = ParameterDirection.Input, Value = timeoutMinutes.HasValue ? (object)timeoutMinutes : DBNull.Value });
            var procResultParam = new SqlParameter { ParameterName = "@procResult", SqlDbType = SqlDbType.Int, Direction = ParameterDirection.Output };
 
            var procResultData = Database.SqlQuery<ResourceLock_ObtainLockReturnModel>("EXEC @procResult = [dbo].[ResourceLock_ObtainLock] @ns, @source, @timeout_minutes", nsParam, sourceParam, timeoutMinutesParam, procResultParam).ToList();
 
            procResult = (int) procResultParam.Value;
            return procResultData;
        }

    }
}

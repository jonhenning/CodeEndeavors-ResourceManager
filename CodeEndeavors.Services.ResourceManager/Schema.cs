using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Services.ResourceManager
{
    public class Schema
    {
        //just using what is in the SQLRepository as it was easier to keep in sync instead of using EntityFramework or Mig#

        private static bool _initialized = false;

        public static void EnsureSchema(string connection)
        {
            if (!_initialized)
            {
                migrateSchema(connection);
                _initialized = true;
            }
        }

        private static DataTable getData(string sql, SqlConnection connection)
        {
            return getData(sql, null, connection);
        }
        private static DataTable getData(string sql, Dictionary<string, object> parameters, SqlConnection connection)
        {
            var dt = new DataTable();
            using (var da = new SqlDataAdapter(sql, connection))
            {
                if (parameters != null)
                    parameters.Keys.ToList().ForEach(key => da.SelectCommand.Parameters.AddWithValue(key, parameters[key]));
                da.Fill(dt);
            }

            //Logging.Log(Logging.LoggingLevel.Detailed, "getData {0} ({1})", sql, parameters.ToJson());  //perf

            return dt;
        }


        private static int executeSql(string sql, SqlConnection connection)
        {
            return executeSql(sql, null, connection);
        }

        private static int executeSql(string sql, Dictionary<string, object> parameters, SqlConnection connection)
        {
            using (var cmd = new SqlCommand(sql, connection))
            {
                if (parameters != null)
                    parameters.Keys.ToList().ForEach(key => cmd.Parameters.AddWithValue(key, parameters[key]));
                var ret = cmd.ExecuteNonQuery();

                //Logging.Log(Logging.LoggingLevel.Detailed, "executeSql {0} ({1})", sql, parameters.ToJson());  //perf

                return ret;
            }
        }

        private static void ensureSchema(SqlConnection connection)
        {
            var versionTable = "if not exists (select 1 from sysobjects where name = '__resmgr_schema_version') CREATE TABLE __resmgr_schema_version(version int)";
            var versionSeed = "if not exists(select 1 from __resmgr_schema_version) INSERT INTO __resmgr_schema_version (version) VALUES (0)";

            executeSql(versionTable, connection);
            executeSql(versionSeed, connection);
        }

        private static void migrateSchema(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                ensureSchema(connection);

                var currentVersion = getSchemaVersion(connection);
                var maxVersion = _dbScripts.Keys.Max();
                var scripts = getScripts(currentVersion, maxVersion);
                scripts.ForEach(s => executeSql(s, connection));
                updateSchemaVersion(maxVersion, connection);
            }
        }

        private static Dictionary<int, List<string>> _dbScripts = new Dictionary<int, List<string>>()
            {
                {1, new List<string>()  //version 1 scripts
                {
                    "if not exists (SELECT 1 FROM sysobjects where name = 'Resource') BEGIN CREATE TABLE Resource(Id varchar(100) NOT NULL, ResourceType varchar(200) NULL, [Key] varchar(200) NULL,Type varchar(200) NULL,Sequence int NULL,EffectiveDate datetimeoffset(7) NULL,ExpirationDate datetimeoffset(7) NULL,Scope nvarchar(500) NULL,Data ntext NULL)  ON [PRIMARY] TEXTIMAGE_ON [PRIMARY] ALTER TABLE Resource ADD CONSTRAINT PK_Resource PRIMARY KEY CLUSTERED ( Id ) CREATE NONCLUSTERED INDEX IX_ResourceType ON Resource (ResourceType) END",
                    "if not exists (SELECT 1 FROM sysobjects where name = 'ResourceAudit') BEGIN CREATE TABLE [ResourceAudit]([Id] [int] IDENTITY(1,1) NOT NULL, [ResourceId] varchar(100) NOT NULL, [UserId] [varchar](100) NULL, [AuditDate] [datetimeoffset](7) NOT NULL, [Action] [varchar](20) NOT NULL, CONSTRAINT [PK_ResourceAudit] PRIMARY KEY CLUSTERED (	[Id] ASC )) ALTER TABLE [dbo].[ResourceAudit]  WITH CHECK ADD  CONSTRAINT [FK_ResourceAudit_Resource] FOREIGN KEY([ResourceId]) REFERENCES [dbo].[Resource] ([Id]) ALTER TABLE [dbo].[ResourceAudit] CHECK CONSTRAINT [FK_ResourceAudit_Resource] END"
                }}
            };

        private static List<string> getScripts(int fromVersion, int toVersion)
        {
            var ret = new List<string>();
            for (var i = fromVersion; i <= toVersion; i++)
            {
                if (_dbScripts.ContainsKey(i))
                    ret.AddRange(_dbScripts[i]);
            }
            return ret;
        }

        private static int getSchemaVersion(SqlConnection connection)
        {
            var dt = getData("SELECT version from __resmgr_schema_version", connection);
            if (dt.Rows.Count > 0)
                return int.Parse(dt.Rows[0]["version"].ToString());
            return 0;
        }
        private static void updateSchemaVersion(int version, SqlConnection connection)
        {
            executeSql("UPDATE __resmgr_schema_version SET Version = @Version", new Dictionary<string, object>() { { "@Version", version } }, connection);
        }


    }
}

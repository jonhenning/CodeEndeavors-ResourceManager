using CodeEndeavors.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace CodeEndeavors.ResourceManager.SQLServer
{
    public class SQLRepository : IRepository, IDisposable
    {
        private Dictionary<string, object> _connection = null;
        private Dictionary<string, object> _cacheConnection = null;
        private string _dataConnection = null;
        private static bool _initialized = false;

        private string _cacheName;
        private string _namespace = null;
        //private bool _useFileMonitor;
        private int _auditHistorySize;
        private bool _enableAudit;

        private ConcurrentDictionary<string, DataTable> _pendingResourceUpdates = new ConcurrentDictionary<string, DataTable>();
        private ConcurrentDictionary<string, DataTable> _pendingAuditUpdates = new ConcurrentDictionary<string, DataTable>();

        //repository instance lifespan is a single request (for videre) - not ideal as we really don't want to dictate how resourcemanager is used in relation to its lifespan
        private ConcurrentDictionary<string, object> _pendingDict = new ConcurrentDictionary<string, object>();

        public SQLRepository()
        {
        }

        public void Initialize(Dictionary<string, object> connection, Dictionary<string, object> cacheConnection)
        {
            _connection = connection;
            _cacheConnection = cacheConnection;
            _dataConnection = _connection.GetSetting("dataConnection", "");
            _cacheName = _cacheConnection.GetSetting("cacheName", "");
            _namespace = _connection.GetSetting<string>("namespace", null);
            //_useFileMonitor = _connection.GetSetting("useFileMonitor", false);
            _auditHistorySize = _connection.GetSetting("auditHistorySize", 10);
            _enableAudit = _auditHistorySize > 0;    

            if (string.IsNullOrEmpty(_dataConnection))
                throw new Exception("dataConnection key not found in connection");

            if (!_initialized)
            {
                migrateSchema();
                _initialized = true;
            }

            Logging.Log(Logging.LoggingLevel.Minimal, "SQL Repository Initialized");
        }

        public void Dispose()
        {
        }

        public DomainObjects.Resource<T> GetResource<T>(string id)
        {
            var dict = AllDict<T>();
            if (!string.IsNullOrEmpty(id) && dict.ContainsKey(id))
                return dict[id];
            return null;
        }

        public List<T> Find<T>(Func<T, bool> predicate)
        {
            return All<T>().Where(predicate).ToList();
        }

        public List<DomainObjects.Resource<T>> FindResources<T>(Func<DomainObjects.Resource<T>, bool> predicate)
        {
            return AllResources<T>().Where(predicate).ToList();
        }

        public List<DomainObjects.Resource<T>> AllResources<T>()
        {
            return AllDict<T>().Values.ToList();    //todo:  perf problem????
        }

        public List<T> All<T>()
        {
            return AllDict<T>().Values.Select(v => v.Data).ToList();
        }

        public ConcurrentDictionary<string, DomainObjects.Resource<T>> AllDict<T>()
        {
            var resourceType = getResourceType<T>();

            if (_pendingDict.ContainsKey(resourceType))
            {
                Logging.Log(Logging.LoggingLevel.Verbose, "Pulled {0} from _pendingDict", resourceType);
                return _pendingDict[resourceType] as ConcurrentDictionary<string, DomainObjects.Resource<T>>;
            }

            Func<ConcurrentDictionary<string, DomainObjects.Resource<T>>> getDelegate = delegate()
            {
                var resource = new List<DomainObjects.Resource<T>>();
                using (var connection = new SqlConnection(_dataConnection))
                {
                    connection.Open();
                    var parameters = new Dictionary<string, object> { { "@resourceType", resourceType } };
                    var sql = "SELECT * FROM Resource WHERE ResourceType = @resourceType";
                    if (!string.IsNullOrEmpty(_namespace))
                    {
                        parameters["@namespace"] = _namespace;
                        sql += " AND [Namespace] = @namespace";
                    }
                    
                    var data = getData(sql, parameters, connection);
                    DataTable auditData = null;
                    if (_enableAudit)
                    {
                        parameters = new Dictionary<string, object> { { "@resourceType", resourceType } };
                        sql = "SELECT ra.* FROM ResourceAudit ra JOIN Resource r ON ra.ResourceId = r.Id WHERE r.ResourceType = @resourceType";

                        if (!string.IsNullOrEmpty(_namespace))
                        {
                            parameters["@namespace"] = _namespace;
                            sql += " AND r.[Namespace] = @namespace";
                        }
                        auditData = getData(sql, parameters, connection);
                    }

                    foreach (DataRow dr in data.Rows)
                    {
                        var auditRows = auditData != null ? auditData.Select("ResourceId = '" + dr["Id"].ToString() + "'") : null;
                        resource.Add(toResource<T>(dr, auditRows));
                    }
                    //todo: handle Audit
                }
                return new ConcurrentDictionary<string, DomainObjects.Resource<T>>(resource.ToDictionary(r => r.Id));
            };

            var dict = CodeEndeavors.Distributed.Cache.Client.Service.GetCacheEntry<ConcurrentDictionary<string, DomainObjects.Resource<T>>>(_cacheName, resourceType, getDelegate, null); //getMonitorOptions(fileName));
            _pendingDict[resourceType] = dict;
            return dict;
        }

        public void Store<T>(DomainObjects.Resource<T> item)
        {
            var resourceType = getResourceType<T>();
            var rowState = DataRowState.Modified;

            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = Guid.NewGuid().ToString();    //todo: use another resource for this...
                item.Namespace = _namespace;
                try
                {
                    ((dynamic)item.Data).Id = item.Id;  //todo: require Id on object?
                }
                catch { }
                rowState = DataRowState.Added;
            }

            var dict = AllDict<T>();
            dict[item.Id] = item;

            if (!_pendingResourceUpdates.ContainsKey(resourceType))
                _pendingResourceUpdates[resourceType] = getResourceDataTable();
            if (!_pendingAuditUpdates.ContainsKey(resourceType))
                _pendingAuditUpdates[resourceType] = getResourceAuditDataTable();

            addResourceDataRow(_pendingResourceUpdates[resourceType], item, rowState);
            addResourceAuditDataRow(_pendingAuditUpdates[resourceType], item.Id, item.Audit.FirstOrDefault());
        }

        public void Save()
        {
            foreach (var resourceType in _pendingResourceUpdates.Keys)
            {
                if (_pendingResourceUpdates[resourceType].Rows.Count > 0)
                {
                    batchUpdateResource(_pendingResourceUpdates[resourceType]);
                    if (_pendingAuditUpdates.ContainsKey(resourceType))
                        batchUpdateResourceAudit(_pendingAuditUpdates[resourceType]);
                    expireCacheEntry(resourceType);
                }
            }
            _pendingResourceUpdates = new ConcurrentDictionary<string, DataTable>();
            _pendingAuditUpdates = new ConcurrentDictionary<string, DataTable>();
        }

        public void Delete<T>(DomainObjects.Resource<T> item)
        {
            var resourceType = getResourceType<T>();

            if (!_pendingResourceUpdates.ContainsKey(resourceType))
                _pendingResourceUpdates[resourceType] = getResourceDataTable();
            addResourceDataRow(_pendingResourceUpdates[resourceType], item, DataRowState.Deleted);
        }

        public void DeleteAll<T>()
        {
            var resourceType = getResourceType<T>();
            using (var connection = new SqlConnection(_dataConnection))
            {
                connection.Open();
                
                var parameters = new Dictionary<string, object>() { { "@ResourceType", resourceType } };
                var sql = "DELETE FROM ResourceAudit WHERE ResourceId IN (SELECT ResourceId FROM Resource WHERE ResourceType = @ResourceType); DELETE FROM Resource WHERE ResourceType = @ResourceType";

                if (!string.IsNullOrEmpty(_namespace))
                {
                    parameters["@namespace"] = _namespace;
                    sql += " AND [Namespace] = @namespace";
                }

                executeSql(sql, parameters, connection);
            }
            expireCacheEntry(resourceType);
            DataTable dt;
            _pendingResourceUpdates.TryRemove(resourceType, out dt);
            _pendingAuditUpdates.TryRemove(resourceType, out dt);

        }

        private string getResourceType<T>()
        {
            return typeof(T).ToString();
        }

        private void expireCacheEntry(string key)
        {
            CodeEndeavors.Distributed.Cache.Client.Service.ExpireCacheEntry(_cacheName, key);
        }

        private object getMonitorOptions(string fileName)
        {
            //if (_useFileMonitor)
            //    return new { monitorType = "CodeEndeavors.Distributed.Cache.Client.File.FileMonitor", fileName = fileName, uniqueProperty = "fileName" };
            return null;
        }

        private DomainObjects.Resource<T> toResource<T>(DataRow dr, DataRow[] audits)
        {
            var ret = new DomainObjects.Resource<T>();
            ret.Id = getDataValue(dr, "Id", "");
            ret.Key = getDataValue<string>(dr, "Key", null);
            ret.Type = getDataValue<string>(dr, "Type", null);
            ret.Namespace = getDataValue<string>(dr, "Namespace", null);
            ret.EffectiveDate = getDataValue<DateTimeOffset?>(dr, "EffectiveDate", null);
            ret.ExpirationDate = getDataValue<DateTimeOffset?>(dr, "ExpirationDate", null);
            var json = getDataValue<string>(dr, "Data", null);
            if (!string.IsNullOrEmpty(json))
                ret.Data = json.ToObject<T>();
            json = getDataValue<string>(dr, "Scope", null);
            if (!string.IsNullOrEmpty(json))
                ret.Scope = json.ToObject<dynamic>(); 

            if (audits != null)
            {
                foreach (var audit in audits)
                {
                    ret.Audit.Add(new DomainObjects.Audit()
                    {
                        Action = getDataValue<string>(audit, "Action", null),
                        UserId = getDataValue<string>(audit, "UserId", null),
                        Date = getDataValue(audit, "AuditDate", DateTimeOffset.Now)
                    });
                }
            }

            return ret;
        }

        private void addResourceDataRow<T>(DataTable dt, DomainObjects.Resource<T> resource, DataRowState rowState)
        {
            var dr = dt.NewRow();
            dr["Id"] = resource.Id;
            dr["ResourceType"] = getResourceType<T>();
            dr["Key"] = getParameterValue(resource.Key, DBNull.Value);
            dr["Type"] = getParameterValue(resource.Type, DBNull.Value);
            dr["Namespace"] = getParameterValue(resource.Namespace, DBNull.Value);
            dr["Sequence"] = getParameterValue(resource.Sequence, DBNull.Value);
            dr["EffectiveDate"] = getParameterValue(resource.EffectiveDate, DBNull.Value);
            dr["ExpirationDate"] = getParameterValue(resource.ExpirationDate, DBNull.Value);
            dr["Data"] = resource.Data != null ? resource.Data.ToJson(false, "db") : (object)DBNull.Value;
            dr["Scope"] = resource.Scope != null ? ((object)resource.Scope).ToJson() : (object)DBNull.Value;
            dt.Rows.Add(dr);
            
            dr.AcceptChanges();
            if (rowState == DataRowState.Added)
                dr.SetAdded();
            else if (rowState == DataRowState.Modified)
                dr.SetModified();
            else if (rowState == DataRowState.Deleted)
                dr.Delete();
            else
                throw new Exception("Rowstate not supported: " + rowState.ToString());
        }

        private void addResourceAuditDataRow(DataTable dt, string resourceId, DomainObjects.Audit audit)
        {
            if (audit != null)
            {
                var dr = dt.NewRow();
                dr["ResourceId"] = resourceId;
                dr["UserId"] = getParameterValue(audit.UserId, DBNull.Value);
                dr["AuditDate"] = audit.Date;
                dr["Action"] = getParameterValue(audit.Action, DBNull.Value);
                dt.Rows.Add(dr);
            }
        }

        private DataTable getResourceDataTable()
        {
            var dt = new DataTable("Resource");
            dt.Columns.Add(new DataColumn("Id", typeof(string)));
            dt.Columns.Add(new DataColumn("ResourceType", typeof(string)));
            dt.Columns.Add(new DataColumn("Key", typeof(string)));
            dt.Columns.Add(new DataColumn("Type", typeof(string)));
            dt.Columns.Add(new DataColumn("Namespace", typeof(string)));
            dt.Columns.Add(new DataColumn("Sequence", typeof(int)));
            dt.Columns.Add(new DataColumn("EffectiveDate", typeof(DateTimeOffset)));
            dt.Columns.Add(new DataColumn("ExpirationDate", typeof(DateTimeOffset)));
            dt.Columns.Add(new DataColumn("Data", typeof(string)));
            dt.Columns.Add(new DataColumn("Scope", typeof(string)));
            return dt;
        }

        private DataTable getResourceAuditDataTable()
        {
            var dt = new DataTable("ResourceAudit");
            dt.Columns.Add(new DataColumn("Id", typeof(int)));
            dt.Columns.Add(new DataColumn("ResourceId", typeof(string)));
            dt.Columns.Add(new DataColumn("UserId", typeof(string)));
            dt.Columns.Add(new DataColumn("AuditDate", typeof(DateTimeOffset)));
            dt.Columns.Add(new DataColumn("Action", typeof(string)));
            return dt;
        }

        private object getParameterValue<T>(T value, object defaultValue)
        {
            var type = typeof(T);
            if (type == typeof(string))
            {
                if (value == null || value.ToString() == "")
                    return DBNull.Value;
                return value;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null)
                    return DBNull.Value;
            }
            return value;
        }

        private T getDataValue<T>(DataRow dr, string columnName, T defaultValue)
        {
            if (dr[columnName] != DBNull.Value)
                return dr[columnName].ToType<T>();
            return defaultValue;
        }

        private void batchUpdateResource(DataTable dt)
        {
            using (var connection = new SqlConnection(_dataConnection))
            {
                var adapter = new SqlDataAdapter();
                adapter.InsertCommand = new SqlCommand("INSERT Resource (Id, ResourceType, Type, [Namespace], [Key], Sequence, EffectiveDate, ExpirationDate, Scope, Data) VALUES (@Id, @ResourceType, @Type, @Namespace, @Key, @Sequence, @EffectiveDate, @ExpirationDate, @Scope, @Data)", connection);
                adapter.InsertCommand.Parameters.AddRange(getResourceParameters().ToArray());
                adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
                adapter.UpdateCommand = new SqlCommand("UPDATE Resource SET ResourceType = @ResourceType, Type = @Type, [Namespace] = @Namespace, [Key] = @Key, Sequence = @Sequence, EffectiveDate = @EffectiveDate, ExpirationDate = @ExpirationDate, Scope = @Scope, Data = @Data WHERE Id = @Id", connection);
                adapter.UpdateCommand.Parameters.AddRange(getResourceParameters().ToArray());
                adapter.UpdateCommand.UpdatedRowSource = UpdateRowSource.None;
                adapter.DeleteCommand = new SqlCommand("DELETE FROM ResourceAudit WHERE ResourceId = @Id; DELETE FROM Resource WHERE Id = @Id", connection);
                adapter.DeleteCommand.Parameters.Add(new SqlParameter("@Id", SqlDbType.VarChar, 100, "Id"));
                adapter.DeleteCommand.UpdatedRowSource = UpdateRowSource.None;

                adapter.UpdateBatchSize = 0; //largest possible size
                adapter.Update(dt);

                Logging.Log(Logging.LoggingLevel.Minimal, "Updated {0} Resource rows in batch update", dt.Rows.Count);
            }
        }
        private void batchUpdateResourceAudit(DataTable dt)
        {
            using (var connection = new SqlConnection(_dataConnection))
            {
                var adapter = new SqlDataAdapter();
                
                var archiveAuditSQL = "";
                if (_enableAudit)
                    archiveAuditSQL = ";DELETE FROM ResourceAudit WHERE Id IN (SELECT Id FROM (select ROW_NUMBER() OVER (ORDER BY [AuditDate] desc) as rownumber, Id from resourceaudit where ResourceId = @ResourceId) a WHERE rownumber > " + _auditHistorySize + ")";    

                adapter.InsertCommand = new SqlCommand("INSERT ResourceAudit (ResourceId, UserId, AuditDate, Action) VALUES (@ResourceId, @UserId, @AuditDate, @Action)" + archiveAuditSQL, connection);
                adapter.InsertCommand.Parameters.AddRange(getResourceAuditParameters().ToArray());
                adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
                adapter.DeleteCommand = new SqlCommand("DELETE FROM ResourceAudit WHERE Id = @Id", connection);
                adapter.DeleteCommand.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int, 4, "Id"));
                adapter.DeleteCommand.UpdatedRowSource = UpdateRowSource.None;
                adapter.UpdateBatchSize = 0; //largest possible size
                adapter.Update(dt);

                Logging.Log(Logging.LoggingLevel.Minimal, "Updated {0} Resource Audit rows in batch update", dt.Rows.Count);
            }
        }

        private List<SqlParameter> getResourceParameters()
        {
            return new List<SqlParameter>() 
                {
                    new SqlParameter ("@Id", SqlDbType.VarChar, 100, "Id"),
                    new SqlParameter("@ResourceType", SqlDbType.VarChar, 200, "ResourceType"),
                    new SqlParameter("@Key", SqlDbType.VarChar, 200, "Key"),
                    new SqlParameter("@Type", SqlDbType.VarChar, 200, "Type"),
                    new SqlParameter("@Namespace", SqlDbType.VarChar, 200, "Namespace"),
                    new SqlParameter("@Sequence", SqlDbType.Int, 4, "Sequence"),
                    new SqlParameter("@EffectiveDate", SqlDbType.DateTimeOffset, 8, "EffectiveDate"),
                    new SqlParameter("@ExpirationDate", SqlDbType.DateTimeOffset, 8, "ExpirationDate"),
                    new SqlParameter("@Scope", SqlDbType.NVarChar, 500, "Scope"),
                    new SqlParameter("@Data", SqlDbType.NText, 8000, "Data")    //size???
                };
        }
        private List<SqlParameter> getResourceAuditParameters()
        {
            return new List<SqlParameter>() 
                {
                    new SqlParameter("@ResourceId", SqlDbType.VarChar, 100, "ResourceId"),
                    new SqlParameter("@UserId", SqlDbType.VarChar, 100, "UserId"),
                    new SqlParameter("@AuditDate", SqlDbType.DateTimeOffset, 8, "AuditDate"),
                    new SqlParameter("@Action", SqlDbType.VarChar, 20, "Action")
                };
        }

        private int executeSql(string sql, SqlConnection connection)
        {
            return executeSql(sql, null, connection);
        }

        private int executeSql(string sql, Dictionary<string, object> parameters, SqlConnection connection)
        {
            using (var cmd = new SqlCommand(sql, connection))
            {
                if (parameters != null)
                    parameters.Keys.ToList().ForEach(key => cmd.Parameters.AddWithValue(key, parameters[key]));
                var ret = cmd.ExecuteNonQuery();

                Logging.Log(Logging.LoggingLevel.Detailed, "executeSql {0} ({1})", sql, parameters.ToJson());  //perf

                return ret;
            }
        }

        private DataTable getData(string sql, SqlConnection connection)
        {
            return getData(sql, null, connection);
        }
        private DataTable getData(string sql, Dictionary<string, object> parameters, SqlConnection connection)
        {
            var dt = new DataTable();
            using (var da = new SqlDataAdapter(sql, connection))
            {
                if (parameters != null)
                    parameters.Keys.ToList().ForEach(key => da.SelectCommand.Parameters.AddWithValue(key, parameters[key]));
                da.Fill(dt);
            }

            Logging.Log(Logging.LoggingLevel.Detailed, "getData {0} ({1})", sql, parameters.ToJson());  //perf

            return dt;
        }

        #region Schema

        //should I just use Mig#?
        //or keep it plain and simple
        private void ensureSchema(SqlConnection connection)
        {
            var versionTable = "if not exists (select 1 from sysobjects where name = '__resmgr_schema_version') CREATE TABLE __resmgr_schema_version(version int)";
            var versionSeed = "if not exists(select 1 from __resmgr_schema_version) INSERT INTO __resmgr_schema_version (version) VALUES (0)";

            executeSql(versionTable, connection);
            executeSql(versionSeed, connection);
        }

        private void migrateSchema()
        {
            using (var connection = new SqlConnection(_dataConnection))
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

        private Dictionary<int, List<string>> _dbScripts = new Dictionary<int, List<string>>()
            {
                {1, new List<string>()  //version 1 scripts
                {
                    "if not exists (SELECT 1 FROM sysobjects where name = 'Resource') BEGIN CREATE TABLE Resource(Id varchar(100) NOT NULL, ResourceType varchar(200) NULL, [Key] varchar(200) NULL,Type varchar(200) NULL,Sequence int NULL,EffectiveDate datetimeoffset(7) NULL,ExpirationDate datetimeoffset(7) NULL,Scope nvarchar(500) NULL,Data ntext NULL)  ON [PRIMARY] TEXTIMAGE_ON [PRIMARY] ALTER TABLE Resource ADD CONSTRAINT PK_Resource PRIMARY KEY CLUSTERED ( Id ) CREATE NONCLUSTERED INDEX IX_ResourceType ON Resource (ResourceType) END",
                    "if not exists (SELECT 1 FROM sysobjects where name = 'ResourceAudit') BEGIN CREATE TABLE [ResourceAudit]([Id] [int] IDENTITY(1,1) NOT NULL, [ResourceId] varchar(100) NOT NULL, [UserId] [varchar](100) NULL, [AuditDate] [datetimeoffset](7) NOT NULL, [Action] [varchar](20) NOT NULL, CONSTRAINT [PK_ResourceAudit] PRIMARY KEY CLUSTERED (	[Id] ASC )) ALTER TABLE [dbo].[ResourceAudit]  WITH CHECK ADD  CONSTRAINT [FK_ResourceAudit_Resource] FOREIGN KEY([ResourceId]) REFERENCES [dbo].[Resource] ([Id]) ALTER TABLE [dbo].[ResourceAudit] CHECK CONSTRAINT [FK_ResourceAudit_Resource] END"
                }},
                {2, new List<string>()  //version 2 scripts
                {
                    "IF NOT EXISTS(SELECT * from syscolumns where name = 'Namespace' and id = (select id from sysobjects where name = 'Resource' and xtype = 'U')) BEGIN ALTER TABLE Resource ADD [Namespace] varchar(200) NULL END"
                }}
            };

        private List<string> getScripts(int fromVersion, int toVersion)
        {
            var ret = new List<string>();
            for (var i = fromVersion; i <= toVersion; i++)
            {
                if (_dbScripts.ContainsKey(i))
                    ret.AddRange(_dbScripts[i]);
            }
            return ret;
        }

        private int getSchemaVersion(SqlConnection connection)
        {
            var dt = getData("SELECT version from __resmgr_schema_version", connection);
            if (dt.Rows.Count > 0)
                return int.Parse(dt.Rows[0]["version"].ToString());
            return 0;
        }
        private void updateSchemaVersion(int version, SqlConnection connection)
        {
            executeSql("UPDATE __resmgr_schema_version SET Version = @Version", new Dictionary<string, object>() { { "@Version", version } }, connection);
        }

        #endregion

    }
}

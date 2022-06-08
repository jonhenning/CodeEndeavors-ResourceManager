using CodeEndeavors.Extensions;
using CodeEndeavors.ServiceHost.Common.Client;
using CodeEndeavors.ServiceHost.Common.Services;
using CodeEndeavors.ServiceHost.Common.Services.Profiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheService = CodeEndeavors.Distributed.Cache.Client.Service;

namespace CodeEndeavors.Services.ResourceManager.Client
{
    public class Cache
    {
        public static string GetCacheClientId()
        {
            return string.Format("{0}:{1}", Environment.MachineName, System.Reflection.Assembly.GetCallingAssembly().FullName);
        }

        public static string GetInMemoryConnection()
        {
            return string.Format("{{'cacheType': 'CodeEndeavors.Distributed.Cache.Client.InMemory.InMemoryCache', 'clientId': '{0}', 'absoluteExpiration': '\"00:10:00\"' }}", GetCacheClientId());
        }

        public static void ConfigureCache(string cacheName, string connection, CodeEndeavors.Distributed.Cache.Client.Logging.LoggingLevel logLevel = CodeEndeavors.Distributed.Cache.Client.Logging.LoggingLevel.Minimal)
        {
            if (!string.IsNullOrEmpty(cacheName))
            {
                CacheService.RegisterCache(cacheName, connection);

                if (CodeEndeavors.Distributed.Cache.Client.Logging.LoggingHandlerCount == 0)
                {
                    CodeEndeavors.Distributed.Cache.Client.Logging.LogLevel = logLevel;
                    CodeEndeavors.Distributed.Cache.Client.Logging.OnLoggingMessage += (message) =>
                    {
                        Logging.Log(Logging.LoggingLevel.Debug, message);
                    };
                }
            }
        }

        public static ClientCommandResult<TData> Execute<TData>(string cacheName, string cacheKey, object cacheItemObject, Func<ServiceResult<TData>> codeFunc)
        {
            return Execute<TData>(cacheName, cacheKey, null, cacheItemObject, codeFunc);
        }
        public static ClientCommandResult<TData> Execute<TData>(string cacheName, string cacheKey, TimeSpan? absoluteExpiration, object cacheItemObject, Func<ServiceResult<TData>> codeFunc)
        {
            if (!string.IsNullOrEmpty(cacheName))
            {
                return ClientCommandResult<TData>.Execute(result =>
                {
                    using (var capture = Timeline.Capture("ResourceManager.Client.Cache.Execute"))
                    {
                        var serviceCalled = false;
                        string profilerResults = null;
                        var serviceResult = CacheService.GetCacheEntry(cacheName, cacheKey, ToMD5(cacheItemObject.ToJson()), () =>
                        {
                            serviceCalled = true;
                            var sr = codeFunc.Invoke();
                            if (sr.Success)
                            {
                                profilerResults = sr.ProfilerResults;
                                return sr.Data;
                            }
                            else
                                throw new Exception(sr.ToString());
                        });
                        if (serviceCalled)
                        {
                            result.ProfilerResults = profilerResults;
                            result.ReportResult(serviceResult, true);
                        }
                        else
                            result.ReportResult(serviceResult, true);
                    }
                });
            }
            else
            {
                using (var capture = Timeline.Capture("ResourceManager.Client.Cache.Execute (nocache)"))
                {
                    return ClientCommandResult<TData>.Execute(codeFunc);
                }
            }
        }

        public static ClientCommandResult<TData> Execute<TData>(string cacheName, string cacheTypeDependency, List<string> cacheTypeDependencyKeys, string cacheKey, object cacheItemObject, Func<ServiceResult<TData>> codeFunc)
        {
            return Execute<TData>(cacheName, cacheTypeDependency, null, cacheTypeDependencyKeys, cacheKey, cacheItemObject, codeFunc);
        }


        //DO NOT CACHE SERVICERESULT, WE NEED TO INSERT A NEW RESULT
        public static ClientCommandResult<TData> Execute<TData>(string cacheName, string cacheTypeDependency, TimeSpan? absoluteExpiration, List<string> cacheTypeDependencyKeys, string cacheKey, object cacheItemObject, Func<ServiceResult<TData>> codeFunc)
        {
            if (!string.IsNullOrEmpty(cacheName))
            {
                return ClientCommandResult<TData>.Execute(result =>
                {
                    using (var capture = Timeline.Capture("ResourceManager.Client.Cache.Execute w/ deps"))
                    {
                        var serviceCalled = false;
                        string profilerResults = null;
                        var serviceResult = CacheService.GetCacheEntry(cacheName, absoluteExpiration, cacheKey, ToMD5(cacheItemObject.ToJson()), () =>
                        {
                            serviceCalled = true;
                            var sr = codeFunc.Invoke();
                            if (sr.Success)
                            {
                                profilerResults = sr.ProfilerResults;
                                return sr.Data;
                            }
                            else
                                throw new Exception(sr.ToString());
                        });
                        if (serviceCalled)
                        {
                            if (cacheTypeDependencyKeys != null)
                            {
                                foreach (var key in cacheTypeDependencyKeys)
                                    CacheService.AddCacheDependency(cacheName, cacheTypeDependency, key, cacheKey); //dependencies have no expiration (unless specified on connection)
                            }
                            result.ProfilerResults = profilerResults;
                            result.ReportResult(serviceResult, true);
                        }
                        else
                            result.ReportResult(serviceResult, true);
                    }
                });
            }
            else
                return ClientCommandResult<TData>.Execute(codeFunc);
        }

        public static bool SetCacheEntry<TData>(string cacheName, string cacheKey, TimeSpan? absoluteExpiration, object cacheItemObject, TData data)
        {
            if (!string.IsNullOrEmpty(cacheName))
            {
                var itemKey = ToMD5(cacheItemObject.ToJson());
                CacheService.SetCacheEntry(cacheName, absoluteExpiration, cacheKey, itemKey, data, null);
                return true;
            }
            else
                return true;
        }


        private static string ToMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }

        }

    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Security;
using CodeEndeavors.Extensions;
using Microsoft.ApplicationServer.Caching;
using System.Diagnostics;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using CodeEndeavors.Cache;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace CodeEndeavors.ResourceManager.AzureBlob
{
    public class Cache
    {
        private DataCache _dataCache = null;

        private TokenProvider _queueTokenProvider = null;
        private Uri _queueUri = null;
        private MessagingFactory _queueFactory = null;
        //private MessageSender _queueSender = null;
        private NamespaceManager _namespaceManager = null;
        private QueueClient _queueClient = null;
        private string _queueName = null;

        public Cache(Dictionary<string, object> connection)
        {
            //http://msdn.microsoft.com/en-us/library/windowsazure/gg618003.aspx
            var cacheACSKey = connection.GetSetting("azureCacheACSKey", "");
            _queueName = connection.GetSetting("azureQueueName", "");

            if (!string.IsNullOrEmpty(cacheACSKey))
            {
                // Setup DataCacheSecurity configuration.
                var secureACSKey = new SecureString();
                foreach (char a in cacheACSKey)
                    secureACSKey.AppendChar(a);
                secureACSKey.MakeReadOnly();
                var factorySecurity = new DataCacheSecurity(secureACSKey);

                // Setup the DataCacheFactory configuration.
                var factoryConfig = new DataCacheFactoryConfiguration();
                factoryConfig.Servers = new DataCacheServerEndpoint[1] {new DataCacheServerEndpoint(
                    connection.GetSetting("azureCacheServiceUrl", ""), 
                    connection.GetSetting("azureCacheServicePort", 22233))};
                factoryConfig.SecurityProperties = factorySecurity;

                // Create a configured DataCacheFactory object.
                var cacheFactory = new DataCacheFactory(factoryConfig);

                Trace.TraceInformation("Azure Blob Cache setup: {0}:{1}", connection.GetSetting("azureCacheServiceUrl", ""), connection.GetSetting("azureCacheServicePort", 22233));

                // Get a cache client for the default cache.
                _dataCache = cacheFactory.GetDefaultCache();
                Clear();

            }
            else if (!string.IsNullOrEmpty(_queueName))
            {
                _queueTokenProvider = TokenProvider.CreateSharedSecretTokenProvider(connection.GetSetting("azureQueueIssuer", ""), connection.GetSetting("azureQueueKey", ""));
                _queueUri = ServiceBusEnvironment.CreateServiceUri("sb", connection.GetSetting("azureQueueNamespace", ""), string.Empty); //todo: always sb???
                _namespaceManager = new NamespaceManager(_queueUri, _queueTokenProvider);

                //if (_namespaceManager.GetQueues().Where(q => q.Path.Equals(_queueName, StringComparison.InvariantCultureIgnoreCase)).Count() == 0)
                //    _namespaceManager.CreateQueue(_queueName);
                _queueFactory = MessagingFactory.Create(_queueUri, _queueTokenProvider);
                _queueClient = _queueFactory.CreateQueueClient(_queueName);
                //_queueSender = _queueFactory.CreateMessageSender(_queueName);
            }

            else
                Trace.TraceInformation("No Azure Blob Cache settings found.");
        }

        public T GetCache<T>(string key, T defaultValue) where T : class
        {
            if (_queueClient != null)
            {
                var ret = CacheState.GetState<T>(key, System.Web.HttpContext.Current.Items.GetSetting<T>(key, null));
                if (ret == null)
                    Trace.TraceInformation("Item not found in cache: {0}", key);
                //else
                //    Trace.TraceInformation("Item found in cache: {0}", key);
                return ret;
            }
            else if (_dataCache != null)
            {
                var cacheItem = System.Web.HttpContext.Current.Items.GetSetting<T>(key, null);   //more efficient pulling from current request
                if (cacheItem == null)
                {
                    var item = _dataCache.GetCacheItem(key);
                    if (item != null)
                    {
                        cacheItem = item.Value.ToString().ToObject<T>();
                        System.Web.HttpContext.Current.Items[key] = cacheItem;  //store on current request for even quicker lookups
                    }
                }
                //return item.Value as T;
                return cacheItem;
            }
            else
                return System.Web.HttpContext.Current.Items.GetSetting<T>(key, defaultValue);
        }

        private string getSafeKey(string key)
        {
            var newKey = Regex.Replace(key, @"[^A-Za-z0-9]+", "-");
            return newKey.Substring(newKey.Length > 50 ? newKey.Length - 50 : 0);   //todo: REAL HACK!
        }

        public void SetCache<T>(string key, T value)
        {
            if (_queueClient != null)
            {
                var safeKey = getSafeKey(key);
                var roleInstanceId = getSafeKey(RoleEnvironment.CurrentRoleInstance.Id); //Guid.NewGuid().ToString();
                var crc = value.ToJson().ComputeHash();
                var time = DateTime.UtcNow;
                TopicDescription topic = null;
                if (!_namespaceManager.TopicExists(safeKey))
                    topic = _namespaceManager.CreateTopic(safeKey);
                else
                    topic = _namespaceManager.GetTopic(safeKey);

                if (!_namespaceManager.SubscriptionExists(topic.Path, roleInstanceId))
                    _namespaceManager.CreateSubscription(topic.Path, roleInstanceId);

                var subscriptionClient = _queueFactory.CreateSubscriptionClient(topic.Path, roleInstanceId);//todo:  key both topic and name???
                Trace.TraceInformation("QueueCacheDependency setup: ==> {0} <==: {1} {2}", roleInstanceId, safeKey, crc);
                var dependency = new QueueCacheDependency(safeKey, subscriptionClient, crc);

                ////_queueClient.Send(new BrokeredMessage((new Dictionary<string, object>() { { "key", key }, { "guid", guid }, { "time", time } }).ToJson()));
                ////var dependency = new QueueCacheDependency(key, guid, _queueTokenProvider, _queueUri, _queueName);
                //CacheState.ExpireCache(key);

                CacheState.SetState(key, value, dependency);

                Trace.TraceInformation("Sending Expire Notification: ==> {0} <==: {1} {2}", roleInstanceId, safeKey, crc);
                var topicClient = _queueFactory.CreateTopicClient(topic.Path);
                topicClient.Send(new BrokeredMessage(crc));

            }
            else if (_dataCache != null)
            {
                //_dataCache.Put(key, value);
                //System.Web.HttpContext.Current.Items[key] = value;  //store on current request for even quicker lookups
                _dataCache.Put(key, value.ToJson(false, "db"), TimeSpan.FromMinutes(1));    //datacontractserializer sucks! so we will simply use json and store it in our cache as a string
            }
            //else
            System.Web.HttpContext.Current.Items[key] = value; //store on current request for even quicker lookups
        }

        public void Clear()
        {
            if (_dataCache != null)
            {
                //var items = _dataCache.GetObjectsInRegion();
                //var keys = items.Select(i => i.Key).ToList();
                //foreach (var key in keys)
                //    _dataCache.Remove(key);
            }
        }

        public void Clear(string key)
        {
            if (_dataCache != null)
            {
                _dataCache.Remove(key);
            }
        }


    }
}
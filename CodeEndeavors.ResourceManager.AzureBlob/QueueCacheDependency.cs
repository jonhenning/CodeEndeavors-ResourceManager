using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;
using Microsoft.ServiceBus;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using System.Diagnostics;
using CodeEndeavors.Extensions;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace CodeEndeavors.ResourceManager.AzureBlob
{
    public class QueueCacheDependency : CacheDependency
    {
        //private TokenProvider _tokenProvider = null;
        //private Uri _uri = null;
        //private MessagingFactory _factory = null;
        //private MessageReceiver _receiver = null;
        private string _key = null;
        //private string _guid = null;
        private string _crc = null;
        //private SubscriptionClient _subscriptionClient = null;
        //private string _entityPath = null;
        //public QueueCacheDependency(string key, string guid, TokenProvider tokenProvider, Uri uri, string queueName)
        //{
        //    _key = key;
        //    _guid = guid;
        //    //_tokenProvider = tokenProvider;
        //    //_uri = uri;
        //    //_entityPath = entityPath;

        //    var factory = MessagingFactory.Create(uri, tokenProvider);
        //    _receiver = factory.CreateMessageReceiver(queueName);

        //    WaitCallback callback = new WaitCallback(WaitForMessage);
        //    ThreadPool.QueueUserWorkItem(callback);
        //}

        public QueueCacheDependency(string key, SubscriptionClient subscriptionClient, string crc)
        {
            Trace.TraceInformation("QueueCacheDependency created: {0}", key);
            //_subscriptionClient = subscriptionClient;
            //_guid = guid;
            _key = key;
            _crc = crc;

            subscriptionClient.BeginReceive(
                TimeSpan.MaxValue, 
                ReceiveDone,
                subscriptionClient
            );
        }

        private void ReceiveDone(IAsyncResult result)
        {
            var subscriptionClient = result.AsyncState as SubscriptionClient;
            if (subscriptionClient != null && !subscriptionClient.IsClosed)
            {
                var message = subscriptionClient.EndReceive(result);
                //var messageRoleInstanceId = message.GetBody<string>();
                //var currentRoleInstanceId = RoleEnvironment.CurrentRoleInstance.Id;
                var crc = message.GetBody<string>();

                Trace.TraceInformation("QueueCacheDependency message received on instance ==> {0} <==: {1} currentCrc: {2}  messageCrc: {3}", subscriptionClient.Name, _key, _crc, crc);
                if (_crc != crc)     //ensure message is not same as one that created cache (stop instant expiring
                {
                    Trace.TraceInformation("* Expiring Cache ==> {0} <==: {1}", subscriptionClient.Name, _key);
                    base.NotifyDependencyChanged(this, EventArgs.Empty);
                    message.Complete();
                    //subscriptionClient.Close();
                }
                else
                {
                    Trace.TraceInformation("Instant expire stopped ==> {0} <==: {1}", subscriptionClient.Name, _crc);
                    message.Complete();
                }


                //subscriptionClient.BeginReceive(
                //    TimeSpan.MaxValue,
                //    ReceiveDone,
                //    subscriptionClient
                //);
            }
            else
            {
                //something went wrong, expire cache
                Trace.TraceInformation("*** MESSAGE FAILURE *** Expiring Cache ==> {0} <==: {1}", subscriptionClient.Name, _key);
                base.NotifyDependencyChanged(this, EventArgs.Empty);

            }


        }

        //private void WaitForMessage(object state)
        //{
        //    var message = _subscriptionClient.Receive();
        //    Trace.TraceInformation("QueueCacheDependency message received: {0}", message.MessageId);
        //    try
        //    {
        //        if (_guid != message.GetBody<string>())     //ensure message is not same as one that created cache (stop instant expiring
        //            base.NotifyDependencyChanged(this, EventArgs.Empty);
        //        else
        //            Trace.TraceInformation("Instant expire stopped", _guid);
        //        message.Complete();
        //    }
        //    catch (Exception ex)
        //    {
        //        message.Abandon();
        //    }

        //}

        //private void WaitForMessageOld(object state)
        //{
        //    var message = _receiver.Receive(TimeSpan.MaxValue);
        //    var jsonDict = message.GetBody<string>();
        //    var dict = jsonDict.ToObject<Dictionary<string, object>>();
        //    var changedKey = dict.GetSetting("key", "");
        //    var changedGuid = dict.GetSetting("guid", "");
        //    var time = dict.GetSetting<DateTime?>("time", null);

        //    Trace.TraceInformation("QueueCacheDependency message received: {0} = {1}", changedKey, _key);
        //    if (changedKey == _key && changedGuid != _guid)
        //    {
        //        base.NotifyDependencyChanged(this, EventArgs.Empty);
        //        Trace.TraceInformation("QueueCacheDependency cache invalidated: {0}", _key);
        //    }
        //    else
        //    {
        //        Trace.TraceInformation("QueueCacheDependency cache NOT invalidated, waiting some more: {0}", _key);
        //        WaitCallback callback = new WaitCallback(WaitForMessageOld);
        //        ThreadPool.QueueUserWorkItem(callback);
        //    }

        //    if (!time.HasValue || (DateTime.UtcNow - time.Value) > TimeSpan.FromSeconds(30))
        //        message.Complete();
        //}

    }
}

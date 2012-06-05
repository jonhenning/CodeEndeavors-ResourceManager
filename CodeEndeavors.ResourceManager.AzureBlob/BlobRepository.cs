using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Caching;
using CodeEndeavors.Extensions;
using System.Collections.Concurrent;
using System.Configuration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.ApplicationServer.Caching;
using System.Security;
using System.Diagnostics;

namespace CodeEndeavors.ResourceManager.AzureBlob
{
    public class BlobRepository : IRepository, IDisposable
    {
        private Dictionary<string, object> _connection = null;
        private string _azureBlobStorage = null;
        private CloudStorageAccount _storageAccount = null;
        private CloudBlobClient _blobClient = null;
        private CloudBlobContainer _blobContainer = null;

        //private Cache _cache = null;

        public delegate void SaveFunc();

        //todo: static or instance...  concurrency will bite us either way!
        private static ConcurrentDictionary<Type, SaveFunc> _pendingUpdates = new ConcurrentDictionary<Type, SaveFunc>();

        public BlobRepository()
        {
        }

        public void Initialize(Dictionary<string, object> connection)
        {
            _connection = connection;
            _azureBlobStorage = _connection.GetSetting("azureBlobStorage", "");
            if (string.IsNullOrEmpty(_azureBlobStorage))
                throw new Exception("azureBlobStorage key not found in connection");

            _storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue(_azureBlobStorage));
            _blobClient = _storageAccount.CreateCloudBlobClient();
            _blobContainer = _blobClient.GetContainerReference(_connection.GetSetting("azureBlobContainer", "codeendeavors-resourcerepository"));
            _blobContainer.CreateIfNotExist();

            Trace.WriteLine(string.Format("Azure Blob setup: {0}:{1}", _azureBlobStorage, _connection.GetSetting("azureBlobContainer", "codeendeavors-resourcerepository"))); 

            //_cache = new Cache(connection);
        }

        public void Dispose()
        {
        }

        public DomainObjects.Resource<T> GetResource<T>(string id)
        {
            var dict = AllResourceDict<T>();
            if (dict.ContainsKey(id))
                return dict[id];
            return null;
            //return AllResources<T>().SingleOrDefault(d => d.Id == Id);
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
            return AllResourceDict<T>().Values.ToList();    //todo:  perf problem????
        }

        public List<T> All<T>()
        {
            var fileName = GetJsonFileName<T>();
            return Cache.CacheState.PullCache("FileRepository." + fileName, true,
                delegate()
                {
                    var resource = new List<T>();
                    var blob = _blobContainer.GetBlobReference(fileName);
                    if (blob != null)
                    {
                        try
                        {
                            var json = blob.DownloadText();
                            resource = json.ToObject<List<T>>();
                            Trace.TraceInformation("~~~~~~~~~~~~~~~~ Loaded {0} items from blob: {1}", resource.Count, fileName);
                        }
                        catch (StorageClientException ex)
                        {
                            //todo:  really... only way to detect existance is an exception???
                            Trace.TraceInformation("Cache not found: {0}", fileName);
                        }
                    }
                    return resource;
                },
                fileName);
        }

        private ConcurrentDictionary<string, DomainObjects.Resource<T>> AllResourceDict<T>()
        {
            var fileName = GetJsonFileName<DomainObjects.Resource<T>>();
            return Cache.CacheState.PullCache("FileRepository." + fileName, true,
                delegate()
                {
                    var resource = new List<DomainObjects.Resource<T>>();
                    var blob = _blobContainer.GetBlobReference(fileName);
                    if (blob != null)
                    {
                        try
                        {
                            var json = blob.DownloadText();
                            resource = json.ToObject<List<DomainObjects.Resource<T>>>();
                            Trace.TraceInformation("~~~~~~~~~~~~~~~~ Loaded {0} items from blob: {1}", resource.Count, fileName);
                        }
                        catch (StorageClientException ex)
                        {
                            //todo:  really... only way to detect existance is an exception???
                            Trace.TraceInformation("Cache not found: {0}", fileName);
                        }
                    }
                    return new ConcurrentDictionary<string, DomainObjects.Resource<T>>(resource.ToDictionary(r => r.Id));
                },
                fileName);
        }

        //private ConcurrentDictionary<string, DomainObjects.Resource<T>> AllResourceDict<T>()
        //{
        //    var fileName = GetJsonFileName<T>();
        //    var dict = _cache.GetCache<ConcurrentDictionary<string, DomainObjects.Resource<T>>>(fileName, null);
        //    //return Cache.CacheState.PullCache("FileRepository." + fileName, true,
        //    //    delegate()
        //    //    {
        //    if (dict == null)
        //    {
        //        var resource = new List<DomainObjects.Resource<T>>();
        //        var blob = _blobContainer.GetBlobReference(fileName);
        //        if (blob != null)
        //        {
        //            try
        //            {
        //                var json = blob.DownloadText();
        //                resource = json.ToObject<List<DomainObjects.Resource<T>>>();
        //                Trace.TraceInformation("~~~~~~~~~~~~~~~~ Loaded {0} items from blob: {1}", resource.Count, fileName);
        //            }
        //            catch (StorageClientException ex)
        //            {
        //                //todo:  really... only way to detect existance is an exception???
        //                Trace.TraceInformation("Cache not found: {0}", fileName);
        //            }

        //        }
        //        dict = new ConcurrentDictionary<string, DomainObjects.Resource<T>>(resource.ToDictionary(r => r.Id));
        //        _cache.SetCache(fileName, dict);
        //    }
        //    return dict;
        //    //    },
        //    //    fileName);
        //}

        public void Store<T>(DomainObjects.Resource<T> item)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = Guid.NewGuid().ToString();    //todo: use another resource for this...
                ((dynamic)item.Data).Id = item.Id;  //todo: require Id on object?
            }
            var dict = AllResourceDict<T>();
            dict[item.Id] = item;

            var key = item.GetType();
            if (!_pendingUpdates.ContainsKey(key))
                _pendingUpdates[key] = () => WriteJsonFile<T>();

        }

        public void Delete<T>(DomainObjects.Resource<T> item)
        {
            DomainObjects.Resource<T> resource = null;
            var dict = AllResourceDict<T>();
            dict.TryRemove(item.Id, out resource);

            if (resource != null)
            {
                var key = item.GetType();
                if (!_pendingUpdates.ContainsKey(key))
                    _pendingUpdates[key] = () => WriteJsonFile<T>();
            }
        }

        public void Save()
        {
            foreach (var type in _pendingUpdates.Keys)
                _pendingUpdates[type]();
            _pendingUpdates.Clear();
        }

        public void DeleteAll<T>()
        {
            var dict = AllResourceDict<T>();
            dict.Clear();
            //todo:  save?
        }

        private string GetJsonFileName<T>()
        {
            return Path.Combine(_azureBlobStorage, typeof(DomainObjects.Resource<T>).ToString()) + ".json";
        }

        private string GetRealJsonFileName<T>()
        {
            return Path.Combine(_azureBlobStorage, typeof(DomainObjects.Resource<T>).ToString()) + ".json";
        }

        private void WriteJsonFile<T>()
        {
            var fileName = GetJsonFileName<T>();
            var list = AllResources<T>();
            var json = list.ToJson(true, "db");  //pretty?
            var blob = _blobContainer.GetBlobReference(fileName);
            blob.UploadText(json);

            var dict = new ConcurrentDictionary<string, DomainObjects.Resource<T>>(list.ToDictionary(r => r.Id));
            //_cache.SetCache(fileName, dict);
            //_cache.Clear<T>(fileName);

            Trace.TraceInformation("Storing {0} chars json data into cache: {1}", json.Length, fileName);
        }

        public void Delete<T>(T item)
        {
            throw new NotImplementedException();
        }

        public void Store<T>(T item)
        {
            throw new NotImplementedException();
        }
    }
}
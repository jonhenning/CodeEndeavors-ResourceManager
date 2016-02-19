using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeEndeavors.ResourceManager
{
    //public enum StorageProvider
    //{
    //    RavenDb
    //}

    public interface IRepository
    {
        List<T> Find<T>(Func<T, bool> predicate);
        List<T> All<T>();
        DomainObjects.Resource<T> GetResource<T>(string id);
        List<DomainObjects.Resource<T>> FindResources<T>(Func<DomainObjects.Resource<T>, bool> predicate);
        List<DomainObjects.Resource<T>> AllResources<T>();
        void Initialize(Dictionary<string, object> connection);
        void Store<T>(DomainObjects.Resource<T> item);
        void Delete<T>(DomainObjects.Resource<T> item);
        void DeleteAll<T>();
        void Save();
        void Dispose();
    }
}

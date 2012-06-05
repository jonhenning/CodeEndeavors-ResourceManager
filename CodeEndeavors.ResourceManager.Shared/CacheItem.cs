using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;


namespace CodeEndeavors.ResourceManager
{
    public class CacheItem
    {
        private DateTime _absoluteExpiration = Cache.NoAbsoluteExpiration;

        public CacheItem(string key)
        {
            Key = key;
            LastUpdate = DateTime.MinValue;
            AbsoluteExpiration = Cache.NoAbsoluteExpiration;
        }

        public string Key { get; set; }
        public DateTime LastUpdate { get; set; }
        public DateTime AbsoluteExpiration
        {
            get
            {
                //if (UsesStale)
                //    return Cache.NoAbsoluteExpiration;

                return _absoluteExpiration;
            }
            set
            {
                _absoluteExpiration = value;
            }
        }

        public CacheItemPriority Priority { get; set; }
        public string DependencyKey { get; set; }
        public string DependencyFileName { get; set; }

        //public bool UsesStale { get; set; }
        //public StaleCacheEvaluator StaleEvaluator { get; set; }

        public bool HasPendingRequest { get; set; }

        //public bool HasStaleData
        //{
        //    get
        //    {
        //        if (UsesStale)
        //        {
        //            //if (StaleEvaluator != null)
        //            //{
        //            //    var result = StaleEvaluator(this, CacheState.GetState<object>(Key, null));
        //            //    if (result.Cancel)
        //            //        return result.IsStale;
        //            //}
        //            return StaleExpiration < DateTime.Now;
        //        }
        //        return false;
        //    }
        //}

        //public DateTime StaleExpiration
        //{
        //    get
        //    {
        //        if (UsesStale)
        //            return _absoluteExpiration; // can't access it via property here, or we'll get wrong answer
        //        return DateTime.MaxValue;
        //    }
        //}

    }
}

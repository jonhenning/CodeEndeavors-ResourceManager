using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Collections;
using Newtonsoft.Json;

namespace CodeEndeavors.ResourceManager.DomainObjects
{
    public class Resource<T> //: IDomainObject 
    {
        public Resource()
        {
            //Scope = new Dictionary<string, List<string>>();
            Audit = new List<Audit>();
        }


        //public Resource(string Type, string Key, int? Sequence, T Data, string ScopeName, string Item)
        //    : this(null, Type, Key, Sequence, Data, new ExpandoObject())
        //    //: this(Type, Key, Sequence, Data, new Dictionary<string, List<string>>() { { ScopeName, new List<string>() {Item} } })
        //{
        //    (this.Scope as IDictionary<string, object>)[ScopeName] = Item;
        //}

        //public Resource(string Id, string Type, string Key, int? Sequence, T Data, string ScopeName, string Item)
        //    : this(Id, Type, Key, Sequence, Data, new ExpandoObject())
        //    //: this(Id, Type, Key, Sequence, Data, new Dictionary<string, List<string>>() { { ScopeName, new List<string>() {Item} } })
        //{
        //    (this.Scope as IDictionary<string, object>)[ScopeName] = Item;
        //}

        public Resource(string Type, string Key, int? Sequence, T Data)
            : this(null, Type, Key, Sequence, Data, new ExpandoObject())
        {

        }

        public Resource(string Type, string Key, int? Sequence, T Data, dynamic Scope)
            : this(null, Type, Key, Sequence, Data)
        {
            this.Scope = Scope;
        }

        public Resource(string Id, string Type, string Key, int? Sequence, T Data, dynamic Scope)
            : this(Id, Type, Key, Sequence, Data)
        {
            this.Scope = Scope;
        }

        public Resource(string Id, string Type, string Key, int? Sequence, T Data)
            : this()
        {
            this.Id = Id;
            this.Key = Key;
            this.Type = Type;
            this.Sequence = Sequence;
            this.Data = Data;
        }

        public string Id { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }
        public int? Sequence { get; set; }
        public DateTimeOffset? EffectiveDate { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public T Data { get; set; }
        //public Dictionary<string, List<string>> Scope { get; set; }
        public dynamic Scope { get; set; }
        //public ExpandoObject Scope { get; set; }
        public List<Audit> Audit { get; set; }
        public bool Deleted
        {
            get
            {
                return ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
            }
        }

        public bool Effective
        {
            get
            {
                return Deleted == false && (!EffectiveDate.HasValue || EffectiveDate.Value < DateTime.UtcNow);
            }
        }

        public int GetMatchScore(List<Query<DomainObjects.Resource<T>>> Queries)
        {
            var score = 0;
            //var itemScore = 0;
            //foreach (var key in MatchScope.Keys)
            //var dict = Scope as IDictionary<string, object>;
            //var o = Scope as IEnumerable<KeyValuePair<string, object>>;

            if (Effective)
            {
                //object scopeObject = Scope as object;
                var scopeObject = this;
                foreach (var q in Queries)
                {
                    //itemScore = 0;
                    try
                    {
                        //if (scopeObject.Where(q.Statement).Count() > 0)
                        if (q.Statement(scopeObject))
                            score += q.Score;
                    }
                    catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
                    {
                        //ignore
                    }
                }
            }
            return score;
        }

        //public int GetMatchScore(Dictionary<string, ScopeMatch> MatchScope)
        //{
        //    var score = 0;
        //    var itemScore = 0;
        //    //foreach (var key in MatchScope.Keys)
        //    foreach (var key in MatchScope.Keys)
        //    {
        //        itemScore = 0;
        //        if (Scope.ContainsKey(key))
        //            itemScore = MatchScope[key].MatchScore(Scope[key]);

        //        if (MatchScope[key].Required && itemScore == 0)
        //            return 0;
                
        //        score += itemScore;

        //    }
        //    return score;
        //}

        //public bool HasScope(string Key, string Value)
        //{
        //    return Scope.ContainsKey(Key) && (Value + "").Equals(Scope[Key].Value + "", StringComparison.OrdinalIgnoreCase);
        //}

        public string GroupKey
        {
            get
            {
                return string.Format("{0}~{1}~{2}", Type, Key, Sequence.HasValue ? Sequence.ToString() : "");
            }
        }

    }
}

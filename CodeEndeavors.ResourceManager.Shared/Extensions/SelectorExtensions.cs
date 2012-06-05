using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace CodeEndeavors.ResourceManager.Extensions
{
    public static class Selector
    {
        public static List<T> GetMatches<T>(this List<DomainObjects.Query<T>> Queries, IEnumerable<T> Items, bool BestMatch)
        {
            var matchedItems = new List<T>();
            //todo: perf... double match score calculation
            var matches = Items.Where(i => Queries.GetMatchScore(i) > 0)
                    .OrderByDescending(i => Queries.GetMatchScore(i))
                    .ToList();
            if (matches.Count > 0)
            {
                if (BestMatch)
                    matchedItems.Add(matches[0]);
                else
                    matchedItems.AddRange(matches);
            }
            return matchedItems;
        }

        public static List<DomainObjects.Resource<T>> GetMatches<T>(this List<DomainObjects.Query<DomainObjects.Resource<T>>> Queries, IEnumerable<DomainObjects.Resource<T>> Items, bool BestMatch)
        {
            var matchedItems = new List<DomainObjects.Resource<T>>();
            //todo: perf... double match score calculation
            var matches = Items.Where(i => i.Effective && Queries.GetMatchScore(i) > 0)
                    .OrderByDescending(i => Queries.GetMatchScore(i))
                    .ThenByDescending(i => i.EffectiveDate.HasValue ? i.EffectiveDate.Value : DateTime.MinValue)
                    .ToList();
            if (matches.Count > 0)
            {
                if (BestMatch)
                    matchedItems.Add(matches[0]);
                else
                    matchedItems.AddRange(matches);
            }
            return matchedItems;
        }

        public static int GetMatchScore<T>(this List<DomainObjects.Query<T>> Queries, T scopeObject)
        {
            var score = 0;
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
            return score;
        }



    }
}
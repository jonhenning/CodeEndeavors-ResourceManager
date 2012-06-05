using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeEndeavors.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> getKey)
        {
            var dict = new Dictionary<TKey, TSource>();

            foreach (var item in source)
            {
                var key = getKey(item);
                if (!dict.ContainsKey(key))
                    dict[key] = item;
            }
            return dict.Select(item => item.Value);
        }
    }
}

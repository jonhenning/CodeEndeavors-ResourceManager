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

        //http://blog.mutable.net/post/2008/05/23/using-linq-to-objects-for-recursion.aspx
        public static IEnumerable<T> Descendants<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> DescendBy)
        {
            foreach (T value in source)
            {
                yield return value;

                foreach (T child in DescendBy(value).Descendants<T>(DescendBy))
                {
                    yield return child;
                }
            }
        }

    }

}

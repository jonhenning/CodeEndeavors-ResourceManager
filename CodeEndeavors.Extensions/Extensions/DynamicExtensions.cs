using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeEndeavors.ResourceManager.Extensions
{
    static class DynamicExtensions
    {
        public static IEnumerable<dynamic> Where(this object source, Func<dynamic, dynamic> predicate)
        {
            if (predicate(source))
                yield return source;

            //foreach (dynamic item in source as dynamic)
            //{
            //    if (predicate(item))
            //        yield return item;
            //}

        }
    }
}

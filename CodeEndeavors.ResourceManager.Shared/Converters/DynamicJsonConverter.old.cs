using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;


namespace CodeEndeavors.ResourceManager.Shared.Converters
{
    public sealed class DynamicJsonConverter : JavaScriptConverter
    {
        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            var result = ToExpando(dictionary);

            return type == typeof(object) ? result : null;
        }

        private static ExpandoObject ToExpando(IDictionary<string, object> dictionary)
        {
            var result = new ExpandoObject();
            var dic = result as IDictionary<String, object>;

            foreach (var item in dictionary)
            {
                var valueAsDic = item.Value as IDictionary<string, object>;
                if (valueAsDic != null)
                {
                    dic.Add(item.Key, ToExpando(valueAsDic));
                    continue;
                }
                var arrayList = item.Value as ArrayList;
                if (arrayList != null && arrayList.Count > 0)
                {
                    dic.Add(item.Key, ToExpando(arrayList));
                    continue;
                }

                dic.Add(item.Key, item.Value);
            }
            return result;
        }

        private static ArrayList ToExpando(ArrayList obj)
        {
            ArrayList result = new ArrayList();

            foreach (var item in obj)
            {
                var valueAsDic = item as IDictionary<string, object>;
                if (valueAsDic != null)
                {
                    result.Add(ToExpando(valueAsDic));
                    continue;
                }

                var arrayList = item as ArrayList;
                if (arrayList != null && arrayList.Count > 0)
                {
                    result.Add(ToExpando(arrayList));
                    continue;
                }

                result.Add(item);
            }
            return result;
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            return obj as IDictionary<string, object>;
        }

        public override IEnumerable<Type> SupportedTypes
        {
            get { return new ReadOnlyCollection<Type>(new List<Type>(new[] { typeof(object) })); }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
//using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CodeEndeavors.ResourceManager.Converters
{
    public sealed class DynamicJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return ((objectType == typeof(IDictionary<System.String, Newtonsoft.Json.Linq.JToken>)) || (objectType == typeof(object)));
            //return ((objectType == typeof(IDictionary<System.String, Newtonsoft.Json.Linq.JToken>)));
        }

        //public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = JContainer.ReadFrom(reader);
            var dict = value as IDictionary<System.String, Newtonsoft.Json.Linq.JToken>;
            if (dict != null)
                return ToExpando(dict);
            return null;
        }

        private static dynamic ToExpando(IDictionary<System.String, Newtonsoft.Json.Linq.JToken> dictionary)
        {
            dynamic result = new ExpandoObject();
            var dic = result as IDictionary<String, object>;

            foreach (var item in dictionary)
            {
                var valueAsDic = item.Value as IDictionary<System.String, Newtonsoft.Json.Linq.JToken>;
                if (valueAsDic != null)
                {
                    dic.Add(item.Key, ToExpando(valueAsDic));
                    continue;
                }
                //var arrayList = item.Value as ArrayList;
                var arrayList = item.Value as JArray;
                if (arrayList != null && arrayList.Count > 0)
                {
                    dic.Add(item.Key, ToExpando<string>(arrayList));    //todo: HACK!  ALWAYS ASSUMING STRING
                    continue;
                }

                var token = item.Value;
                if (token.Type == JTokenType.Integer)
                    dic.Add(item.Key, token.Value<int>());
                else
                    dic.Add(item.Key, token.Value<string>());   //todo: NOT ALWAYS STRING!

                //dic.Add(item.Key, item.Value);
            }
            return result;
        }

        private static List<T> ToExpando<T>(JArray obj)
        {
            var result = new List<T>();

            foreach (JToken item in obj)
            {
                var valueAsDic = item as IDictionary<System.String, Newtonsoft.Json.Linq.JToken>;
                if (valueAsDic != null)
                {
                    result.Add(ToExpando(valueAsDic));
                    continue;
                }

                var arrayList = item as JArray;
                if (arrayList != null && arrayList.Count > 0)
                {
                    result.AddRange(ToExpando<T>(arrayList));
                    continue;
                }
                result.Add(item.Value<T>());
            }
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        //public override IEnumerable<Type> SupportedTypes
        //{
        //    get { return new ReadOnlyCollection<Type>(new List<Type>(new[] { typeof(object) })); }
        //}
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;

namespace CodeEndeavors.ResourceManager.Converters
{
    //public class JsonDynamicConverter : JsonConverter
    //{
    //    // Methods
    //    public override bool CanConvert(Type objectType)
    //    {
    //        //return ((objectType == typeof(DynamicJsonObject)) || (objectType == typeof(object)));
    //        return ((objectType == typeof(JContainer)));
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        var token = JToken.ReadFrom(reader);
    //        var value2 = token as JValue;
    //        if (value2 != null)
    //        {
    //            return value2.Value;
    //        }
    //        var source = token as JArray;
    //        if (source != null)
    //        {
    //            //var obj2 = new DynamicJsonObject(new JObject());
    //            var obj2 = new JContainer(new JObject());
    //            return new DynamicList(source.Select<JToken, object>(new Func<JToken, object>(obj2, (IntPtr)this.TransformToValue)).ToArray<object>());
    //        }
    //        string typeName = token.Value<string>("$type");
    //        if (typeName != null)
    //        {
    //            Type type = Type.GetType(typeName, false);
    //            if (type != null)
    //            {
    //                return serializer.Deserialize(new JTokenReader(token), type);
    //            }
    //        }
    //        return new DynamicJsonObject((JObject)((JObject)token).CloneToken());
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        var metaObject = ((IDynamicMetaObjectProvider)value).GetMetaObject(Expression.Constant(value));
    //        writer.WriteStartObject();
    //        foreach (string str in metaObject.GetDynamicMemberNames())
    //        {
    //            writer.WritePropertyName(str);
    //            object valueDynamically = DynamicUtil.GetValueDynamically(value, str);
    //            if (((valueDynamically == null) || (valueDynamically is ValueType)) || (valueDynamically is string))
    //            {
    //                writer.WriteValue(valueDynamically);
    //            }
    //            else
    //            {
    //                serializer.Serialize(writer, valueDynamically);
    //            }
    //        }
    //        writer.WriteEndObject();
    //    }
    //}

}

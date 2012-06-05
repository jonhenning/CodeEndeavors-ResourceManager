using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
//using Newtonsoft.Json;

namespace CodeEndeavors.ResourceManager.Extensions
{
    public static class Converter
    {
    //    public delegate object ItemConvert(object item);

    //    public static string ToFormatted(this DateTime? date, string nullDisplay)
    //    {
    //        return ToFormatted(date, "MM/dd/yy h:mm tt", nullDisplay);
    //    }

    //    public static string ToFormatted(this DateTime? date, string format, string nullDisplay)
    //    {
    //        if (date.HasValue)
    //            return ((DateTime)date).ToString(format);
    //        else
    //            return nullDisplay;
    //    }

    //    public static T ToEnum<T>(this string s)
    //    {
    //        return (T)Enum.Parse(typeof(T), s);
    //    }

    //    public static List<NewType> ToList<OldType, NewType>(this IEnumerable collection, ItemConvert convertDelegate)
    //    {
    //        List<NewType> ret = new List<NewType>();
    //        foreach (OldType item in collection)
    //        {
    //            ret.Add((NewType)convertDelegate(item));
    //        }
    //        return ret;
    //    }

    //    public static List<T> ToList<T>(this ArrayList arrayList)
    //    {
    //        //better way to do this?
    //        List<T> list = new List<T>();
    //        foreach (object item in arrayList)
    //            list.Add((T)item);
    //        return list;
    //    }

    //    public static List<TElement> ToList<TSource, TElement>(this IEnumerable<TSource> source, Func<TSource, TElement> itemSelector)
    //    {
    //        List<TElement> list = new List<TElement>();
    //        foreach (TSource local in source)
    //        {
    //            list.Add(itemSelector(local));
    //        }
    //        return list;
    //    }

    //    public static Dictionary<TKey, List<TSource>> ToListDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    //    {
    //        var dictionary = new Dictionary<TKey, List<TSource>>();
    //        foreach (TSource local in source)
    //        {
    //            var key = keySelector(local);
    //            if (dictionary.ContainsKey(key) == false)
    //                dictionary[key] = new List<TSource>();
    //            dictionary[key].Add(local);
    //        }
    //        return dictionary;
    //    }

    //    public static string JoinToString(this IEnumerable collection, string separator)
    //    {
    //        List<string> items = collection.ToList<object, string>(obj => obj.ToString());
    //        return string.Join(separator, items.ToArray());
    //    }

    //    public static int[] ToIntArray(this string s)
    //    {
    //        bool valid = true;
    //        int[] digits = Array.ConvertAll<char, int>(s.ToCharArray(), delegate(char c)
    //        {
    //            int i;
    //            if (int.TryParse(c.ToString(), out i) == false)
    //                valid = false;

    //            return i;

    //        });
    //        if (valid == false)
    //            return null;
    //        return digits;
    //    }

        public static T ToType<T>(this object obj)
        {
            int intConversion;

            //if Enum and we have a string that should really be a number, make it so
            if (typeof(T).IsEnum)
            {
                if (int.TryParse(obj.ToString(), out intConversion))
                    return (T)Enum.ToObject(typeof(T), intConversion);
                else
                    return (T)Enum.Parse(typeof(T), obj.ToString());
            }
            if (obj is IConvertible)
                return (T)ChangeType(obj, typeof(T));   //use fixed method until MS fixes framework to handle nullable types
            //return (T)Convert.ChangeType(obj, typeof(T));
            return (T)obj;
        }

    //    //public static Stream ToStream(this string s)
    //    //{
    //    //    return new MemoryStream(System.Text.ASCIIEncoding.Default.GetBytes(s));
    //    //}

    //    public static Dictionary<string, object> ToDictionary(this string json)
    //    {
    //        return ToObject<Dictionary<string, object>>(json);
    //    }

    //    public static Dictionary<T, V> ToEnumDictionary<T, V>(this Dictionary<string, V> dict)
    //    {
    //        Dictionary<T, V> ret = new Dictionary<T, V>();
    //        foreach (var key in dict.Keys)
    //        {
    //            ret[key.ToEnum<T>()] = dict[key];
    //        }
    //        return ret;
    //    }

    //    public static Dictionary<string, V> ToStringDictionary<T, V>(this Dictionary<T, V> dict)
    //    {
    //        var ret = new Dictionary<string, V>();
    //        if (dict != null)
    //        {
    //            foreach (var key in dict.Keys)
    //                ret[key.ToString()] = dict[key].ToType<V>(); //perf???
    //        }

    //        return ret;
    //    }

    //    public static string ToSortableString(this DateTime d)
    //    {
    //        return d.ToString("yyyyMMdd");
    //    }


    //    // Public property allows callers to specify maximum length of JSON
    //    // strings to be serialized and deserialized
    //    private static int _maxJsonLength = int.MaxValue; // default to maximum possible
    //    public static int MaxJsonLength
    //    {
    //        get { return _maxJsonLength; }
    //        set { _maxJsonLength = value; }
    //    }

    //    private static JavaScriptSerializer JsonSerializer
    //    {
    //        get
    //        {
    //            // we're creating a new object each time--would it be safe to reuse a single instance?
    //            var ser = new JavaScriptSerializer { MaxJsonLength = MaxJsonLength };
    //            ser.RegisterConverters(new JavaScriptConverter[] { new JSONDateTimeConverter() });
    //            return ser;
    //        }
    //    }

        //public static T ToObject<T>(this string json)
        //{
        //    //return JsonSerializer.Deserialize<T>(json);
        //    return JsonConvert.DeserializeObject<T>(json);
        //}

        //public static T ToObject<T>(this string json, bool useConverter)
        //{
        //    if (useConverter)
        //        return ToObject<T>(json);

        //    var ser = new JavaScriptSerializer { MaxJsonLength = MaxJsonLength };
        //    return ser.Deserialize<T>(json);
        //}

        //public static string ToJson(this object obj)
        //{
        //    //return JsonSerializer.Serialize(obj);
        //    return JsonConvert.SerializeObject(obj);
        //}

    //    public static string ToJson(this object obj, bool useConverter)
    //    {
    //        if (useConverter)
    //            return ToJson(obj);

    //        var ser = new JavaScriptSerializer { MaxJsonLength = MaxJsonLength };
    //        return ser.Serialize(obj);
    //    }

    //    public static T ToTypeByJson<T>(this object obj)
    //    {
    //        var json = ToJson(obj);
    //        return ToObject<T>(json);
    //    }

    //    public static T GetValue<T>(this Dictionary<string, object> dict, string key, T defaultVal)
    //    {
    //        if (dict.ContainsKey(key) && dict[key] != null)
    //        {
    //            if (dict[key] is Dictionary<string, object>)
    //                return ((Dictionary<string, object>)dict[key]).ToObject<T>();
    //            return dict[key].ToType<T>();
    //        }
    //        return defaultVal;
    //    }

    //    public static T GetValue<T>(this Dictionary<string, T> dict, string key, T defaultVal)
    //    {
    //        if (dict.ContainsKey(key) && dict[key] != null)
    //            return dict[key];
    //        return defaultVal;
    //    }

    //    //todo: research if this will break things as it relies on system configuration
    //    //public static T GetValue<T>(this System.Configuration.Configuration config, string key, T defaultValue)
    //    //{
    //    //    var setting = config.AppSettings.Settings[key];
    //    //    if (setting != null)
    //    //        return setting.Value.ToType<T>();
    //    //    return defaultValue;
    //    //}

    //    public static T ToObject<T>(this Dictionary<string, object> dict)
    //    {
    //        Type type = typeof(T);
    //        object item = Activator.CreateInstance(type);
    //        PropertyInfo pi;
    //        FieldInfo fi;
    //        foreach (KeyValuePair<string, object> pair in dict)
    //        {
    //            fi = type.GetField(pair.Key);
    //            pi = type.GetProperty(pair.Key);
    //            if (fi != null)
    //                type.InvokeMember(pair.Key, System.Reflection.BindingFlags.SetField, null, item, new Object[] { pair.Value });
    //            else if (pi != null && pi.CanWrite && pair.Value != null)
    //                type.InvokeMember(pair.Key, System.Reflection.BindingFlags.SetProperty, null, item, new Object[] { pair.Value });
    //        }
    //        return item.ToType<T>();
    //    }

    //    public static DateTime? ToKind(this DateTime? date, DateTimeKind kind)
    //    {
    //        if (date.HasValue)
    //            return ToKind((DateTime)date, kind);
    //        return date;
    //    }

    //    public static DateTime ToKind(this DateTime date, DateTimeKind kind)
    //    {
    //        if (date.Kind != kind)
    //            return new DateTime(date.Ticks, kind);
    //        return date;
    //    }


    //    /// <summary>
    //    /// Returns an Object with the specified Type and whose value is equivalent to the specified object.
    //    /// </summary>
    //    /// <param name="value">An Object that implements the IConvertible interface.</param>
    //    /// <param name="conversionType">The Type to which value is to be converted.</param>
    //    /// <returns>An object whose Type is conversionType (or conversionType's underlying type if conversionType
    //    /// is Nullable&lt;&gt;) and whose value is equivalent to value. -or- a null reference, if value is a null
    //    /// reference and conversionType is not a value type.</returns>
    //    /// <remarks>
    //    /// This method exists as a workaround to System.Convert.ChangeType(Object, Type) which does not handle
    //    /// nullables as of version 2.0 (2.0.50727.42) of the .NET Framework. The idea is that this method will
    //    /// be deleted once Convert.ChangeType is updated in a future version of the .NET Framework to handle
    //    /// nullable types, so we want this to behave as closely to Convert.ChangeType as possible.
    //    /// This method was written by Peter Johnson at:
    //    /// http://aspalliance.com/author.aspx?uId=1026.
    //    /// </remarks>
        private static object ChangeType(object value, Type conversionType)
        {
            // Note: This if block was taken from Convert.ChangeType as is, and is needed here since we're
            // checking properties on conversionType below.
            if (conversionType == null)
            {
                throw new ArgumentNullException("conversionType");
            } // end if

            // If it's not a nullable type, just pass through the parameters to Convert.ChangeType

            if (conversionType.IsGenericType &&
              conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                // It's a nullable type, so instead of calling Convert.ChangeType directly which would throw a
                // InvalidCastException (per http://weblogs.asp.net/pjohnson/archive/2006/02/07/437631.aspx),
                // determine what the underlying type is
                // If it's null, it won't convert to the underlying type, but that's fine since nulls don't really
                // have a type--so just return null
                // Note: We only do this check if we're converting to a nullable type, since doing it outside
                // would diverge from Convert.ChangeType's behavior, which throws an InvalidCastException if
                // value is null and conversionType is a value type.
                if (value == null)
                {
                    return null;
                } // end if

                // It's a nullable type, and not null, so that means it can be converted to its underlying type,
                // so overwrite the passed-in conversion type with this underlying type
                var nullableConverter = new NullableConverter(conversionType);
                conversionType = nullableConverter.UnderlyingType;
            } // end if

            // Now that we've guaranteed conversionType is something Convert.ChangeType can handle (i.e. not a
            // nullable type), pass the call on to Convert.ChangeType
            return Convert.ChangeType(value, conversionType);
        }
    //    public static string ToSafeJS(this string s)
    //    {
    //        return s.Replace("\n", "\\n").Replace("\r", "\\r").Replace("'", "\\'");
    //    }

        public static string PathCombine(this string path1, string path2)
        {
            return PathCombine(path1, path2, "/");
        }

        public static string PathCombine(this string path1, string path2, string delimiter)
        {
            if (path1.EndsWith(delimiter) && path2.StartsWith(delimiter))
                return path1.Substring(0, path1.Length - 1) + path2;
            else if (!path1.EndsWith(delimiter) && !path2.StartsWith(delimiter))
                return path1 + delimiter + path2;
            return path1 + path2;
        }

    }
}
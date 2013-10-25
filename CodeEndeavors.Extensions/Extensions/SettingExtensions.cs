using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace CodeEndeavors.Extensions
{
    public static class SettingExtensions
    {
        public static T GetSetting<T>(this System.Web.Caching.Cache cache, string key, T defaultValue, bool addEntry = false)
        {
            var obj = cache[key];
            if (obj == null)
                return defaultValue;
            if (addEntry)
                cache[key] = defaultValue;
            return obj.ToType<T>();
        }

        public static T GetSetting<T>(this System.Collections.IDictionary dictionary, string key, T defaultValue, bool addEntry = false)
        {
            if (dictionary.Contains(key))
                return dictionary[key].ToType<T>();
            if (addEntry)
                dictionary[key] = defaultValue;
            return defaultValue;
        }

        public static T GetSetting<T>(this NameValueCollection collection, string key, T defaultValue, bool addEntry = false)
        {
            if (collection[key] != null)
                return collection[key].ToType<T>();
            if (addEntry)
                collection[key] = defaultValue.ToString();
            return defaultValue;
        }

        public static void SetSetting<T>(this System.Collections.IDictionary dictionary, string Key, T Value)
        {
            if (!dictionary.Contains(Key))
                dictionary.Add(Key, Value);
            else
                dictionary[Key] = Value;
        }

        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> source1, Dictionary<TKey, TValue> source2)
        {
            return Merge(source1, source2, true);   //defaulting to true for backward compat
        }

        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> source1, Dictionary<TKey, TValue> source2, bool newDict)
        {
            //todo: use linq with SelectMany?
            Dictionary<TKey, TValue> result;
            result = newDict ? new Dictionary<TKey, TValue>() : source1;
            foreach (var x in source1)
                result[x.Key] = x.Value;
            foreach (var x in source2)
                result[x.Key] = x.Value;
            return result;
        }
    }
}

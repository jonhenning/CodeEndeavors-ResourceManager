using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Converters;

namespace CodeEndeavors.Extensions
{
    public static class Converter
    {
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

        public static T JsonClone<T>(this T obj)
        {
            return ToObject<T>(ToJson(obj));
        }

        public static T ToObject<T>(this string json, Type type)
        {
            var settings = new JsonSerializerSettings();
            return JsonConvert.DeserializeObject(json, type).ToType<T>();
        }

        public static T ToObject<T>(this string json)
        {
            //return JsonSerializer.Deserialize<T>(json);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IsoDateTimeConverter() { DateTimeStyles = System.Globalization.DateTimeStyles.AdjustToUniversal });
            //settings.Converters.Add(new CodeEndeavors.ResourceManager.Converters.DynamicJsonConverter());
            //settings.Converters
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static string ToJson(this object obj, bool pretty = false, string ignoreType = null, bool preserveObjectReferences = false)
        {
            //return JsonSerializer.Serialize(obj);
            var format = pretty ? Formatting.Indented : Formatting.None;
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IsoDateTimeConverter() { DateTimeStyles = System.Globalization.DateTimeStyles.AdjustToUniversal });
            if (!string.IsNullOrEmpty(ignoreType))
                settings.ContractResolver = new Serialization.SerializeIgnoreContractResolver(ignoreType);
            if (preserveObjectReferences)
                settings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            return JsonConvert.SerializeObject(obj, format, settings);
        }

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

        public static string ComputeHash(this string value)
        {
            //http://support.microsoft.com/kb/307020
            var tmpSource = ASCIIEncoding.ASCII.GetBytes(value);
            var tmpNewHash = new MD5CryptoServiceProvider().ComputeHash(tmpSource);
            return ByteArrayToString(tmpNewHash);
        }

        static string ByteArrayToString(byte[] arrInput)
        {
            int i;
            var sOutput = new StringBuilder(arrInput.Length);
            for (i = 0; i < arrInput.Length - 1; i++)
            {
                sOutput.Append(arrInput[i].ToString("X2"));
            }
            return sOutput.ToString();
        }

        public static int? ToInt(this string s)
        {
            int? i = null;
            int i2;
            if (int.TryParse(s, out i2))
                i = i2;
            return i;
        }

    }
}
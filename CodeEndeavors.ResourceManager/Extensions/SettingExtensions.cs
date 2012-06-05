using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeEndeavors.ResourceManager.Extensions;

namespace CodeEndeavors.ResourceManager.Extensions
{
    public static class SettingExtensions
    {
        //public static GetSetting<T>(ByVal AppSettings As NameValueCollection, ByVal Key As String, ByVal DefaultValue As T) As T
        //    Return GetSetting<T>(AppSettings, Key, "", DefaultValue)
        //End Function

        //public static GetSetting<T>(ByVal AppSettings As KeyValueConfigurationCollection, ByVal Key As String, ByVal DefaultValue As T) As T
        //    Return GetSetting<T>(AppSettings, Key, "", DefaultValue)
        //End Function

        //public static GetSetting<T>(ByVal AppSettings As NameValueCollection, ByVal Key As String, ByVal Suffix As String, ByVal DefaultValue As T) As T
        //    Dim theKey As String = Key + Suffix
        //    Dim obj As Object = AppSettings(theKey)
        //    If obj Is Nothing Then Return DefaultValue

        //    Return NNR.Extensions.ConversionExtensions.ToType<T>(obj)
        //End Function

        //public static GetSetting<T>(ByVal AppSettings As KeyValueConfigurationCollection, ByVal Key As String, ByVal Suffix As String, ByVal DefaultValue As T) As T
        //    Dim theKey As String = Key + Suffix
        //    Dim obj As KeyValueConfigurationElement = AppSettings(theKey)
        //    If obj Is Nothing Then Return DefaultValue

        //    Return NNR.Extensions.ConversionExtensions.ToType<T>(obj.Value)
        //End Function

        public static T GetSetting<T>(this System.Web.Caching.Cache Cache, string Key, T DefaultValue)
        {
            return GetSetting<T>(Cache, Key, "", DefaultValue);
        }

        public static T GetSetting<T>(this System.Web.Caching.Cache Cache, string Key, string Suffix, T DefaultValue)
        {
            var theKey = Key + Suffix;
            var obj = Cache[theKey];
            if (obj == null)
                return DefaultValue;
            return obj.ToType<T>();
        }

        public static T GetSetting<T>(this System.Collections.IDictionary Dictionary, string Key, T DefaultValue)
        {
            if (Dictionary.Contains(Key))
                return Dictionary[Key].ToType<T>();
            return DefaultValue;
        }

        public static T GetSetting<T>(this System.Collections.Specialized.NameValueCollection Collection, string Key, T DefaultValue)
        {
            if (Collection[Key] != null)
                return Collection[Key].ToType<T>();
            return DefaultValue;
        }

    }
}

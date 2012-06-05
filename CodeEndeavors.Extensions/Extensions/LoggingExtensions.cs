using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using System.Diagnostics;

namespace CodeEndeavors.Extensions
{
    public static class LoggingExtensions
    {
        private static string _logKey = "CodeEndeavors.Extensions.LoggingExtensions.Key";
        public static void LogOnRequest(this string text, params string args)
        {
            Trace.TraceInformation(text, args);
            

            var list = HttpContext.Current.Items.GetSetting(_logKey, new List<string>());
            if (list.Count == 0)
                HttpContext.Current.Items[_logKey] = list;

            list.Add(string.Format(text, args));
        }

        public static string GetLogRequestText(this HttpContext ctx, string delimiter = "\r\n")
        {
            var list = ctx.Items.GetSetting(_logKey, new List<string>());
            return String.Join(delimiter, list); 
        }

    }
}

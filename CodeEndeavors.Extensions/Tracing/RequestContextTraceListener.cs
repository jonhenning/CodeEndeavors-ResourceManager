using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Web;

namespace CodeEndeavors.Extensions.Tracing
{
    public class RequestContextTraceListener : TraceListener
    {
        private static string _logKey = "CodeEndeavors.Extensions.Tracing.RequestContextTraceListener";

        //protected override string[] GetSupportedAttributes() { return new[] { }; }

        public override void Write(string message, string category)
        {
            if (HttpContext.Current != null)
            {
                var list = HttpContext.Current.Items.GetSetting(_logKey, new List<string>());
                if (list.Count == 0)
                    HttpContext.Current.Items[_logKey] = list;

                list.Add(string.Format("[{0}]: {1}", category, message));
            }
        }
        public override void WriteLine(string message, string category) { Write(message + "\n", category); }
        public override void Write(string message) { Write(message, null); }
        public override void WriteLine(string message) { Write(message + "\n"); }

        public static string GetTraceText(string delimiter = "\r\n")
        {
            var list = HttpContext.Current.Items.GetSetting(_logKey, new List<string>());
            return (String.Join(delimiter, list)); 
        }
    }
}

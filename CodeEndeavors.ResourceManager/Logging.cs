using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeEndeavors.ResourceManager
{
    public class Logging
    {
        public enum LoggingLevel
        {
            None = 0,
            Minimal = 1,
            Detailed = 2,
            Verbose = 3
        }

        public static LoggingLevel LogLevel { get; set; }

        public static event Action<string> OnLoggingMessage;


        public static void Log(LoggingLevel level, string msg)
        {
            Log(level, msg, "");
        }
        public static void Log(LoggingLevel level, string msg, params object[] args)
        {
            if ((int)level <= (int)LogLevel && OnLoggingMessage != null)
                 OnLoggingMessage(level.ToString() + ":" + (msg.IndexOf("{0}") > -1 ? string.Format(msg, args) : msg));
        }


    }
}

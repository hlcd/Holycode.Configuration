using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Holycode.Configuration.Serilog.Filters
{
    public class LogContextFilter : ILogEventFilter
    {
        private string contextName;
        private LogEventLevel minLevel;
        public LogContextFilter(string contextName, LogEventLevel minLevel)
        {
            this.contextName = contextName;
            this.minLevel = minLevel;
        }
        public bool IsEnabled(LogEvent logEvent)
        {
            return !(ExcludeLogByContext(logEvent, contextName, minLevel));
        }


        private static bool ExcludeLogByContext(LogEvent ev, string name, LogEventLevel minLevel)
        {
            LogEventPropertyValue ctx;
            if (ev.Properties.TryGetValue("SourceContext", out ctx))
            {
                return ((ScalarValue)ctx).Value.ToString().StartsWith(name) && ev.Level < minLevel;
            }
            return false;
        }
    }
    public class IpFilter : ILogEventFilter
    {
        private LoggingLevelSwitch logLevel;
        public string Ip { get; set; }

        public IpFilter(LoggingLevelSwitch logLevel, string ip)
        {
            this.logLevel = logLevel;
            this.Ip = ip;
        }
        public bool IsEnabled(LogEvent ev)
        {
            if (Ip != null)
            {
                LogEventPropertyValue ctx = null;
                if (ev?.Properties?.TryGetValue("ClientIp", out ctx) ?? false)
                {
                    if (((ScalarValue)ctx)?.Value?.ToString() == Ip) return true;
                }
            }
            return ev != null && ev.Level >= logLevel.MinimumLevel;
        }
    }
}

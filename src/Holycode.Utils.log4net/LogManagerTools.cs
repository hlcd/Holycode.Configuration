using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Filter;
using log4net.Layout;
using log4net.Util;
using log4net.Repository.Hierarchy;
using System.Reflection;

namespace log4net
{
    public static class LogManagerTools
    {
        static ILoggerRepository repository => LogManager.GetRepository(typeof(LogManagerTools).GetTypeInfo().Assembly);
        public static void FlushBuffers()
        {
            foreach (IAppender appender in repository.GetAppenders())
            {
                var buffered = appender as BufferingAppenderSkeleton;
                if (buffered != null)
                {
                    buffered.Flush();
                }
            }
        }

        public static void AddAppender(AppenderSkeleton appender)
        {
            appender.ActivateOptions();
            AddAppender((IAppender)appender);
        }

        public static void AddAppender<TAppender>(Func<TAppender> factoryMethod, Action<TAppender> configure)
            where TAppender : IAppender
        {
            var app = factoryMethod();
            configure?.Invoke(app);
            AddGlobalAppender(app);
        }

        public static void AddAppender(IAppender appender) => AddGlobalAppender(appender);

        internal static void AddGlobalAppender(IAppender appender) => AddGlobalAppender(repository, appender);

        internal static void AddGlobalAppender(ILoggerRepository repository, IAppender appender)
        {
            if (repository.GetAppenders().Any(a => a.Name == appender.Name)) return;
            log4net.Config.BasicConfigurator.Configure(repository, appender);
        }

        public static void AddTraceAppender() => AddGlobalAppender(AppenderFactory.CreateTraceAppender());

        public static void AddTapAppender() => AddAppender(AppenderFactory.CreateTapAppender());

        public static void AddConsoleAppender(Action<ConsoleAppender> configure) => AddAppender(AppenderFactory.CreateConsoleAppender, configure);
        public static void AddConsoleAppender() => AddAppender(AppenderFactory.CreateConsoleAppender());

        public static void AddConsoleAppenderColored() => AddAppender(AppenderFactory.CreateConsoleAppenderColored());

        public static void AddTerminalAppenderColored() => AddAppender(AppenderFactory.CreateAnsiColorTerminalAppender());

        public static void AddFileAppender(string filename, string appenderName = "RollingFileAppender", bool minimalLock = true,
        Action<RollingFileAppender> config = null) 
            => AddAppender(AppenderFactory.CreateFileAppender(filename, appenderName, minimalLock: minimalLock, config: config));


        public static void AddCallbackAppender(Action<string, LoggingEvent> callback)
            => AddAppender(AppenderFactory.CreateCallbackAppender(callback));

        public static void AddCallbackAppender(Action<string> callback)
            => AddAppender(AppenderFactory.CreateCallbackAppender(callback));

       
    }
}

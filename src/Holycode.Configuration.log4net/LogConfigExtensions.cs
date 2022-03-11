using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Microsoft.Extensions.Configuration
{
    public static class LogConfigExtensions
    {
        private static ILog debugLog = LogManager.GetLogger(typeof (LogConfigExtensions));
        private static Assembly rootAssembly = typeof (LogConfigExtensions).GetTypeInfo().Assembly;
        
        private static ILog rootLog => LogManager.GetLogger(rootAssembly, "root");
        
        public static IConfiguration ConfigureLog4net(this IConfiguration config, string appName, ILog log = null, string logRootPath = null, bool internalDebug = false)
        {
            if (internalDebug)
            {
                log4net.Util.LogLog.InternalDebugging = true;                
            }


            log = log ?? rootLog;
            bool isRoot = log.Logger.Name == "root";

            var rootLevel = config.Get("log4net:level") ?? "Debug";
            log.SetLevel(rootLevel);
            if (isRoot)
            {
                ((Logger)log.Logger).Hierarchy.Root.SetLevel(rootLevel);
            }

            var showStackTrace = config.GetNullable<bool>("log4net:showStackTrace") ?? true;
            var innerLevel = config.GetNullable<int>("log4net:innerExceptionLevel");
            if (showStackTrace)
            {
                log.Logger.Repository.AddExceptionRenderer(level: innerLevel, showStackTrace: showStackTrace);
            }

            var env = config.Get("application:env") ?? config.Get("ASPNET_ENV") ?? "Development";

            //var section = config.GetSection("log4net:appenders");

            ConfigureFileLog(config, appName, env, logRootPath, log);
            ConfigureTraceLog(config, log);
            ConfigureConsoleLog(config, log);

            ConfigureLoggers(config);

            return config;
        }

        private static void ConfigureLoggers(IConfiguration config)
        {
            var loggers = config.GetSection("log4net:loggers");
            foreach (var ch in loggers.GetChildren())
            {
                var loggername = ch.Key;
                var level = ch.Get("level");
                if (level != null)
                {
                    var logger = LogManager.GetLogger(rootAssembly, loggername);
                    ((Logger)logger.Logger).SetLevel(level);
                    if (ch.Get("test") != null)
                    {
                        logger.Debug($"testing logger {loggername} level {level}");
                        logger.Info($"testing logger {loggername} level {level}");
                        logger.Warn($"testing logger {loggername} level {level}");
                        logger.Error($"testing logger {loggername} level {level}");
                    }
                }
            }
        }

        private static void ConfigureConsoleLog(IConfiguration config, ILog log)
        {
            var consoleEnabled = config.GetNullable<bool>("log4net:appenders:console:enabled") ?? true;
            if (consoleEnabled)
            {
                log.AddConsoleAppenderColored();
            }
        }

        private static void ConfigureTraceLog(IConfiguration config, ILog log)
        {
            var traceEnabled = config.GetNullable<bool>("log4net:appenders:trace:enabled") ?? true;
            if (traceEnabled && Debugger.IsAttached)
            {
                log.AddTraceAppender();
            }
        }

        private const string StatsLoggerName = "req-stats";
        private const string ReqLoggerName = "req";
        public static ILog ConfigureStatsLogs(this IConfiguration config, string appName, string logdir)
        {
            var statsLog = log4net.LogManager.GetLogger(rootAssembly, StatsLoggerName);
            statsLog.SetLevel(Level.Info);
            statsLog.SetAdditivity(false);
            statsLog.AddFileAppender($"{logdir}/stats/{appName}-stats.csv", "stats", minimalLock: false);

            var reqLog = log4net.LogManager.GetLogger(rootAssembly, ReqLoggerName);
            reqLog.SetAdditivity(false);
            reqLog.AddFileAppender($"{logdir}/reqs/{appName}-req.log", "reqs", minimalLock: false);
            return statsLog;
        }


       

        private static void ConfigureFileLog(IConfiguration config, string appName, string env, string logRootPath, ILog log, bool minimalLock = true)
        {
            var logFilename = config.GetConnectionStringValue("log4net:appenders:file");
            var fileEnabled = (config.GetNullable<bool>("log4net:appenders:file:enabled") ?? true);

            if (logFilename?.Equals("True") ?? false)
            {
                logFilename = null;
                fileEnabled = true;
            }
            if (logFilename?.Equals("False") ?? false)
            {
                fileEnabled = false;
            }

            if (fileEnabled)
            {
                logRootPath = EnsureLogPath(config, logRootPath);
                logFilename = logFilename ?? $"log\\{appName}-{env}.log";
                var logfile = $"{logRootPath}\\{logFilename}";
                log.AddFileAppender(logfile, minimalLock: minimalLock);
                log.DebugFormat($"configured file appender. logfile={logfile}");

                ConfigureStatsLogs(config, appName, logRootPath);
            }
        }

        private static string EnsureLogPath(IConfiguration config, string logRootPath)
        {
            if (logRootPath == null)
            {
                logRootPath = config.Get("application:wwwroot")
                                ?? config.AppBasePath()
                                ?? ".";
                logRootPath += "/log";
                if (logRootPath.StartsWith("/")) logRootPath = "." + logRootPath;
            }            
            return logRootPath;
        }
    }
}

namespace log4net
{
    public static class LogConfigExtensions
    {
        public static ILog Configure(this ILog log, IConfiguration config, string appName)
        {
            config.ConfigureLog4net(appName, log: log);
            return log;
        }
    }
    
}
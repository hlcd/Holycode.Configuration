using System;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;

namespace log4net
{
    public static class AppenderFactory
    {
        public static string DefaultLayoutPattern =
            @"%date [%-5.5thread] %-5level: [ %-30.30logger ] %-100message%newline";

        public static string DefaultTracceLayoutPattern =
            @"%date [%-2.2thread] %-5level: [%-10.10logger] %-100message";

        public static string DefaultConsoleLayoutPattern = @"%date [%-2thread] %-5level: [%-10.10logger] %message%newline";

        internal static AzureTraceAppender CreateTraceAppender() => new AzureTraceAppender()
        {
            Layout = CreateLayout(DefaultTracceLayoutPattern),
            Name = "TraceAppender",
            ImmediateFlush = true,
        };

        internal static LogTapAppender CreateTapAppender() => new LogTapAppender()
        {
            Name = "LogTapAppender",
        };

        internal static ConsoleAppender CreateConsoleAppender() => new ConsoleAppender()
        {
            Layout = CreateLayout(DefaultConsoleLayoutPattern),
            Threshold = Level.Debug,
            Name = "ConsoleAppender",

        };

        internal static AppenderSkeleton CreateConsoleAppenderColored()
        {
            var appender = new ColoredConsoleAppender()
            {
                Layout = CreateLayout(DefaultConsoleLayoutPattern),
                Threshold = Level.Debug,
                Name = "ConsoleAppender",
            };
            appender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Debug,
                ForeColor = ColoredConsoleAppender.Colors.Green
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Info,
                ForeColor = ColoredConsoleAppender.Colors.White
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Warn,
                ForeColor = ColoredConsoleAppender.Colors.Yellow
            });
            appender.AddMapping(new ColoredConsoleAppender.LevelColors()
            {
                Level = Level.Error,
                ForeColor = ColoredConsoleAppender.Colors.Red
            });
            return appender;
        }

        internal static AnsiColorTerminalAppender CreateAnsiColorTerminalAppender()
        {
            var appender = new AnsiColorTerminalAppender()
            {
                Layout = CreateLayout(AppenderFactory.DefaultConsoleLayoutPattern),
                Threshold = Level.Debug,
                Name = "TerminalAppender",
            };
            appender.AddMapping(new AnsiColorTerminalAppender.LevelColors()
            {
                Level = Level.Debug,
                ForeColor = AnsiColorTerminalAppender.AnsiColor.Green
            });
            appender.AddMapping(new AnsiColorTerminalAppender.LevelColors()
            {
                Level = Level.Info,
                ForeColor = AnsiColorTerminalAppender.AnsiColor.White
            });
            appender.AddMapping(new AnsiColorTerminalAppender.LevelColors()
            {
                Level = Level.Warn,
                ForeColor = AnsiColorTerminalAppender.AnsiColor.Yellow
            });
            appender.AddMapping(new AnsiColorTerminalAppender.LevelColors()
            {
                Level = Level.Error,
                ForeColor = AnsiColorTerminalAppender.AnsiColor.Red
            });
            return appender;
        }

        public static RollingFileAppender CreateFileAppender(string filename,
            string appenderName = "RollingFileAppender",
            bool minimalLock = true,
            Action<RollingFileAppender> config = null)
        {
            var layout = CreateLayout(DefaultLayoutPattern);
            var appender = new RollingFileAppender()
            {
                Layout = layout,
                Threshold = Level.Debug,
                ImmediateFlush = true,
                AppendToFile = true,
                RollingStyle = RollingFileAppender.RollingMode.Composite,
                PreserveLogFileNameExtension = true,
                File = filename,
                Name = appenderName,
                LockingModel = minimalLock ? (FileAppender.LockingModelBase)new FileAppender.MinimalLock() : new FileAppender.ExclusiveLock(),
                MaxSizeRollBackups = -1,
                CountDirection = 1,
                StaticLogFileName = true,
            };

            if (config != null)
                config(appender);

            return appender;
        }

        internal static ILayout CreateLayout(string layout)
        {
            var l = new PatternLayout(layout) { IgnoresException = true };
            l.ActivateOptions();
            return l;
        }
        
        internal static CallbackAppender CreateCallbackAppender(Action<string> callback)
        {
            return new CallbackAppender(callback)
            {
                Name = "CallbackAppender"
            };
        }

        internal static CallbackAppender CreateCallbackAppender(Action<string, LoggingEvent> callback)
        {
            return new Appender.CallbackAppender(callback)
            {
                Name = "CallbackAppender"
            };
        }
    }
}
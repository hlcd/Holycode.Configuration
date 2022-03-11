using Holycode.Configuration.Serilog;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using log4net;
using log4net.Config;

namespace Holycode.Configuration.Tests
{
    [TestClass]
    public class serilog_test
    {
        [TestMethod]
        public void manual_sinks_graylog()
        {
            var url = "net.udp://localhost:12201";
            var configuration = new ConfigurationBuilder().Build();
            var builder = new SerilogConfiguration(configuration, "test", ".");

            // default docker config
            builder.UseGraylog(url);

            global::Serilog.Log.Logger = builder.CreateLogger();

            global::Serilog.Log.Information($"testing graylog from .NET using url {url} at {DateTime.Now}");
            var ms = new Random().Next() % 1000;
            global::Serilog.Log.Information("generated time: {@time}", new { time = ms });

            global::Serilog.Log.CloseAndFlush();

            Console.WriteLine($"graylog message sent to {url}");
            // aaand... check graylog!
        }

        [TestMethod]
        public void log4net_with_seriog_file_sink()
        {
            var log = LogManager.GetLogger(typeof(serilog_test));
            var repo = LogManager.GetRepository(typeof(serilog_test).Assembly);
            BasicConfigurator.Configure(repo);

            var configuration = new ConfigurationBuilder().Build();
            var builder = new SerilogConfiguration(configuration, "test", ".");
            builder.UseFileSink();
            global::Serilog.Log.Logger = builder.CreateLogger();

            global::log4net.Appender.Serilog.Configuration.Configure(repo);

            try
            {
                InnerThrowing();
            } catch (Exception ex)
            {
                // stack trace points to rethrow statement and cointains wrapped inner exception
                log.Error("caugth exception", ex);
            }

            global::Serilog.Log.CloseAndFlush();
        }


        private void InnerThrowing()
        {
            var log = LogManager.GetLogger(typeof(serilog_test));
            try
            {
                try
                {
                    throw new Exception("this is most inner exception");
                }
                catch(Exception ex)
                {                    
                    // stack trace points to original exception
                    log.Error("rethrowing exception", ex);
                    throw;
                }
            } catch (Exception ex)
            {
                /// stack trace ponts to rethrow statement
                log.Error("wrapping exception", ex);
                throw new Exception("this is inner exception wrapper", ex);
            }
        }
    }
}

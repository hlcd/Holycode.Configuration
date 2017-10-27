using Holycode.Configuration.Serilog;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Should;

namespace Holycode.Configuration.Tests
{
    [TestClass]
    public class logging_config_test
    {
        [TestMethod]
        public void no_logging_config_should_not_throw()
        {
            string cfg = @"{
            }
            ";

            System.IO.File.WriteAllText("config.json", cfg);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false)
                .Build();
            var builder = new SerilogConfiguration(configuration, "test", ".");

            // this should not throw
            builder.ConfigureSinks();
        }

        [TestMethod]
        public void file_sink_config()
        {
            string cfgfile = @"{
                'Logging': {
                  'Sinks': {
                    'File': true
                  }
                }
            }
            ";

            System.IO.File.WriteAllText("config.json", cfgfile);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false)
                .Build();
            var builder = new SerilogConfiguration(configuration, "test", ".");

            // this should not throw
            builder.ConfigureSinks();

            builder.IsSinkEnabled("File").ShouldBeTrue();
            builder.IsSinkEnabled("Serilog").ShouldBeFalse();

        }
    }
}

using Holycode.Configuration.Serilog;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.IO;

namespace common.configuration.tests
{
    public class logging_config_test
    {
        [Fact]
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

        [Fact]
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

            Assert.True(builder.IsSinkEnabled("File"));
            Assert.False(builder.IsSinkEnabled("Serilog"));

        }
    }
}

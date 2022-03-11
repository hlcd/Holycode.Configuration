using Holycode.Configuration.Serilog.Filters;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.RollingFileAlternate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Sinks.Graylog;

namespace Holycode.Configuration.Serilog
{
    public partial class SerilogConfiguration 
    {
        public const string DefaultMessageTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{ProcessId}:{ThreadId}] {Level}:  [{SourceContext}] ({ClientIp}) {Message}{NewLine}{Exception}";
        public const string DefaultIndexFormat = "logstash-{0}-{1}";
        //public static string DefaultLayoutPattern =
        // @"%date [%-5.5thread] %-5level: [ %-30.30logger ] %-100message%newline";
        public LoggerConfiguration UseFileSink(string template = null)
        {
            template = template ?? configuration["Logging:Sinks:File:MessageTemplate"] ?? DefaultMessageTemplate;
            var logBuilder = this;

            logBuilder.WriteTo.Logger(inner =>
                inner
                    .Filter.With(new LogContextFilter("SerilogWeb.Classic.ApplicationLifecycleModule", LogEventLevel.Warning))
                    .WriteTo.RollingFileAlternate($"{LogDir}",
                    prefix: $"{appname}-{env}-{System.Diagnostics.Process.GetCurrentProcess().Id}",
                    outputTemplate: template,
                    retainedFileCountLimit: 100, fileSizeLimitBytes: (5 << 20)
                )
            );

            return this;
        }

        public LoggerConfiguration UseElasticSink(Action<ElasticsearchSinkOptions> configureElastic = null)
        {
            var elasticSink = configuration["Logging:Sinks:Elastic"] ?? configuration["Logging:Sinks:Elastic:ConnectionString"];
            elasticSink = elasticSink ?? "http://localhost:9200";
            var indexFormat = configuration["Logging:Sinks:Elastic:IndexFormat"] ?? DefaultIndexFormat;
                        
            indexFormat = string.Format(indexFormat, prefix, env, appname);

            LogInternal($"elastic sink: {elasticSink} indexformat={indexFormat}");

            this.WriteTo.Logger(inner =>
            {
                var elasticOpts = new ElasticsearchSinkOptions(new Uri(elasticSink))
                {
                    IndexFormat = $"{indexFormat.ToLowerInvariant()}-{{0:yyyy.MM.dd}}"
                };
                if (configureElastic != null) configureElastic(elasticOpts);

                inner.WriteTo.Elasticsearch(elasticOpts);
            });

            return this;
        }

        public LoggerConfiguration UseGraylog(string graylogConnectionString = null)
        {
            graylogConnectionString = graylogConnectionString ?? configuration["Logging:Sinks:Graylog"] ?? configuration["Logging:Sinks:Graylog:ConnectionString"];
            if (string.IsNullOrEmpty(graylogConnectionString))
            {
                throw new ArgumentNullException("graylogConnectionString", "pass graylogConnectionString or set Logging:Sinks:Graylog:ConnectionString in configuration");
            }

            var uri = new Uri(graylogConnectionString);
            var opts = new GraylogSinkOptions()
            {
                Host = uri.Host,
                Port = uri.Port,
            };

            this.WriteTo.Logger(inner => inner.WriteTo.Graylog(opts));

            return this;
        }


    }
}

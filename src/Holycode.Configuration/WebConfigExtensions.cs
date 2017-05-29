﻿#if NETFX

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Configuration
{
    public static class WebConfigExtensions
    {
        /// <summary>
        /// Adds the config values from web.config AppSettings and ConnectionStrings to IConfiguration, replacing '.' separator with ':'
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        public static IConfigurationBuilder AddWebConfig(this IConfigurationBuilder configuration)
        {
            var keys = ConfigurationManager.AppSettings.AllKeys;
            var dict = new Dictionary<string, string>();

            foreach (var k in keys)
            {
                var val = ConfigurationManager.AppSettings[k];
                dict[k.Replace(".", ":")] = val;
                var check = configuration.Get(k.Replace(".", ":"));
            }



            var connectionStrings = ConfigurationManager.ConnectionStrings;
            foreach (ConnectionStringSettings c in connectionStrings)
            {
                dict[$"connectionStrings:{c.Name}:connectionString"] = c.ConnectionString;
                dict[$"connectionStrings:{c.Name}:provider"] = c.ProviderName;
            }

            configuration.AddInMemoryCollection(dict);
            return configuration;
        }

        public static IConfiguration AddWebConfig(this IConfiguration configuration)
        {
            var keys = ConfigurationManager.AppSettings.AllKeys;
            var dict = new Dictionary<string, string>();

            foreach (var k in keys)
            {
                var val = ConfigurationManager.AppSettings[k];
                configuration.Set(k.Replace(".", ":"), val);
                var check = configuration.Get(k.Replace(".", ":"));
            }



            var connectionStrings = ConfigurationManager.ConnectionStrings;
            foreach (ConnectionStringSettings c in connectionStrings)
            {
                configuration.Set($"connectionStrings:{c.Name}:connectionString",c.ConnectionString);
                configuration.Set($"connectionStrings:{c.Name}:provider", c.ProviderName);
            }

            return configuration;
        }

        public static IConfiguration Fill(this System.Collections.Specialized.NameValueCollection collection, IConfiguration configuration)
        {
            configuration.Traverse((key, val) => { collection[key.Replace(':', '.')] = val; });
            return configuration;
        }

        public static IConfiguration FillAppSettings(this IConfiguration configuration)
        {
            configuration.Traverse((key, val) => { ConfigurationManager.AppSettings[key.Replace(':', '.')] = val; });
            return configuration;
        }
    }
}

#endif
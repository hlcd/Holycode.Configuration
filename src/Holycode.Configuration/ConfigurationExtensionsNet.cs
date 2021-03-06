﻿#if NETFX
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensionsNet
    {
        public static IConfigurationBuilder AddEnvJson(this IConfigurationBuilder src, bool optional = true, string environment = null)
        {
            var path = src.AppBasePath();
            if (path == null)
            {
                path = Assembly.GetCallingAssembly().CodeBase.Substring("file:///".Length);
                path = Path.GetDirectoryName(path);
            }
            return src.AddEnvJson(path, optional: optional, environment: environment);
        }

        public static IConfigurationBuilder WithCallingAssemblyBasePath(
           this IConfigurationBuilder cfg)
        {
            var path = Assembly.GetCallingAssembly().CodeBase.Substring("file:///".Length);
            cfg.SetAppBasePath(path);
            return cfg;
        }
    }
}


#endif
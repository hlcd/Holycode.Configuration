﻿#if !NETFX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration
{
    public static class ConfigurationExtensionsVnext
    {



        //public static IConfigurationBuilder AddEnvJson(this IConfigurationBuilder src, IApplicationEnvironment env)
        //{
        //    var envPath = env.ApplicationBasePath;
        //    return src.AddEnvJson(envPath);
        //}

        public static IConfigurationBuilder AddEnvJson(this IConfigurationBuilder src)
        {
            var envPath = src.GetBasePath();
            return src.AddEnvJson(envPath);
        }

        public static IConfigurationBuilder AddEnvJson(this IConfigurationBuilder src, bool optional = true)
        {
            var envPath = src.GetBasePath();
            return src.AddEnvJson(envPath, optional: optional);
        }

    }
}

#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Holycode.Configuration;
using Holycode.Configuration.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensions
    {
        internal const string ApplicationBasePathKey = "application:basePath";
        

        
        public static IConfigurationBuilder AddEnvJson(this IConfigurationBuilder src, string applicationBasePath)
        {
            return AddEnvJson(src, applicationBasePath, optional: true);
        }

        /// <summary>
        /// Looks for env.json and env.{environment}.json and adds them to config
        /// </summary>
        /// <param name="src">configuration builder</param>
        /// <param name="applicationBasePath">application base path</param>
        /// <param name="optional">is env.json optional</param>
        /// <param name="environment">force specific environment (overwrites  value provided by env.json)[optional]</param>
        /// <returns></returns>
        public static IConfigurationBuilder AddEnvJson(this IConfigurationBuilder src, string applicationBasePath, bool optional, string environment = null)
        {
            try {
                if (src.AppBasePath() == null)
                    src.SetAppBasePath(applicationBasePath);
                applicationBasePath = src.AppBasePath();

               
                var conventions = DefaultConvention.Get(applicationBasePath, optional, environment);
                var resolver = new ConfigSourceResolver(conventions, stopOnFirstMatch: true);

                var cfgSources = resolver.GetConfigSources();
                foreach(var cfgSrc in cfgSources) {
                    src.Add(cfgSrc);
                }

                src.AddIncludeFiles();

                return src;
            } catch(Exception ex) {
                throw new Exception(ex.Message + "\r\n" + src.GetConfigTrace(), ex);
            }
        }

        /// Set will always override other existing entries
        public static IConfigurationBuilder Set(this IConfigurationBuilder builder, string key, string value)
        {
            //builder.Properties[key] = value;
          
            foreach(MemoryConfigurationSource other in builder.Sources.Where(s => s is MemoryConfigurationSource)) {
                if (other.InitialData == null) continue;
                var data = other.InitialData.ToList();
                data.RemoveAll(d => d.Key == key);
                other.InitialData = data;
            }

            var mem = builder.Sources.LastOrDefault() as MemoryConfigurationSource;
            if (mem == null)
            {
                builder.AddInMemoryCollection();
                mem = builder.Sources.LastOrDefault() as MemoryConfigurationSource;
            }
            if (mem != null)
            {
                var data = mem.InitialData?.ToList() ?? new List<KeyValuePair<string, string>>();
                data.RemoveAll(d => d.Key == key);
                data.Add(new KeyValuePair<string, string>(key, value));
                mem.InitialData = data;
            } else {
                throw new InvalidOperationException("couldn't find MemoryConfigurationSource on builder");
            }
            return builder;
        }

        public static string Get(this IConfigurationBuilder builder, string key)
        {
            //return builder.Build().Get(key);
            // object val;
            // if (builder.Properties.TryGetValue(key, out val))
            // {
            //     return val.ToString();
            // }
            //foreach (var src in builder.Sources)
            //{
            //    string strVal;

            //    if (src.Build(builder).TryGet(key, out strVal))
            //    {
            //        return strVal;
            //    }
            //}
            try {
                return builder.Build().Get(key);
             } catch(Exception ex) {
                throw new Exception(ex.Message + "\r\n" + builder.GetConfigTrace(), ex);
            }
        }

        public static T? GetNullable<T>(this IConfiguration cfg, string key, Func<string, T> convert = null)
          where T : struct
        {
            var v = (cfg.Get(key));
            if (string.IsNullOrEmpty(v)) return null;
            if (v.Equals("false", StringComparison.CurrentCultureIgnoreCase) && typeof(T) != typeof(bool))
            {
                return null;
            }
            else
            {
                if (convert != null) return convert(v);
                return (T)Convert.ChangeType(v, typeof(T));
            }
        }

        public static Dictionary<string, string> GetDictionary(this IConfiguration cfg, string key)
        {
            var subkeys = cfg.GetSection(key).GetChildren();
            if (subkeys == null) return null;
            var d = new Dictionary<string, string>();
            foreach (var pair in subkeys)
            {
                d.Add(pair.Key, pair.Value);
            }
            return d;
        }

        public static T TryGet<T>(this IConfiguration cfg, string key)
           where T : struct
        {
            var v = (cfg.Get(key));
            if (v == null) return default(T);
            else return cfg.Get<T>(key);
        }

        public static string Get(this IConfiguration configuration, string key)
        {
            return configuration[key];
        }

        public static void Set(this IConfiguration configuration, string key, string value)
        {
            configuration[key] = value;
        }

        public static T Get<T>(this IConfiguration configuration, string key)
        {
            return (T)Convert.ChangeType(configuration[key], typeof(T));
        }


        public static void Traverse(this IConfiguration configuration, Action<string, string> action, string rootNs = "")
        {
            var keys = configuration.GetChildren();

            string val = null;
            if (configuration is IConfigurationSection)
            {
                val = ((IConfigurationSection)configuration).Value;
            }
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    Traverse(key, action, rootNs + ":" + key.Key);
                }
            }
            if (val != null)
            {
                var key = rootNs.TrimStart(':');
                action(key, val);
            }
        }

        public static TResult Aggregate<TResult>(this IConfiguration configuration,
            TResult seed,
            Func<TResult, string, object, TResult> action, string rootNs = "")
        {
            return Aggregate(configuration, action, rootNs);
        }
        private static TResult Aggregate<TResult>(this IConfiguration configuration, Func<TResult, string, object, TResult> action, string rootNs = "")
        {
            var keys = configuration.GetChildren();


            string val = null;

            TResult ag = default(TResult);
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    val = key.Value;
                    if (val != null)
                    {
                        ag = action(ag, rootNs + ":" + key.Key, val);
                    }
                    else
                    {
                        var subag = Aggregate(key, action, rootNs + ":" + key.Key);
                        ag = action(ag, rootNs + ":" + key.Key, subag);
                    }
                }
            }


            return ag;
        }



        public static Dictionary<string, string> AsDictionaryPlain(this IConfiguration config)
        {
            var dict = new Dictionary<string, string>();
            config.Traverse((s, s1) =>
            {
                dict.Add(s, s1);
            });

            return dict;
        }

        public static Dictionary<string, object> AsDictionaryNested(this IConfiguration config)
        {
            return config.Aggregate(new Dictionary<string, object>(),
                (dict, key, val) =>
                {
                    if (dict == null) dict = new Dictionary<string, object>();
                    key = key.Split(':').Last();
                    dict.Add(key, val);
                    return dict;
                });
        }


        /// <summary>
        /// Gets the secure service URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="baseUrl">The base URL.</param>
        /// <returns></returns>
        public static string GetSecureServiceUrl(this IConfiguration config, string url, string baseUrl = null, int? sslPort = null)
        {
            if (url == null) return null;
            url = config.GetServiceUrl(url, baseUrl);
            if (sslPort == null)
            {
                var uri = new Uri(url);
                if (uri.Port == 80) sslPort = 443;
                else throw new ArgumentException($"base url uses non-standard port {uri.Port}. Cannot determine ssl port - no sslPort given and 'sslPort' key not found in config");
            }

            if (!url.StartsWith("https://"))
            {
                // do not use https for localhost development
                if (url.StartsWith("http://localhost"))
                {
                    return url;
                }
                url = url.Replace("http://", "https://");
                url = Regex.Replace(url, @"(\:[0-9]+)/", $":{sslPort.Value}/");
            }

            return url;
        }

        public static string GetServiceUrl(this IConfiguration config, string url, string baseUrl = null)
        {
            if (baseUrl == null) baseUrl = config.Get("baseUrl");
            if (baseUrl == null) throw new ArgumentException("no baseUrl given and 'baseUrl' key not found in config");
            if (url == null) return null;
            /// is url relative?
            if (url.StartsWith("/"))
            {
                return (baseUrl.TrimEnd('/') ?? "") + url;
            }
            else
            {
                return url;
            }
        }

        public static string EnvironmentName(this IConfiguration cfg)
        {
            return cfg.Get(EnvJsonConvention.EnvironmentNameKey);
        }

        public static string EnvironmentName(this IConfigurationBuilder cfg)
        {
            return cfg.Get(EnvJsonConvention.EnvironmentNameKey);
        }

        public static string AppBasePath(this IConfiguration cfg)
        {

            return cfg.Get(ApplicationBasePathKey);
        }

        public static void SetAppBasePath(this IConfiguration cfg, string path)
        {
            cfg.Set(ApplicationBasePathKey, path);
        }

        public static string AppBasePath(this IConfigurationBuilder cfg)
        {
            return cfg.Get(ApplicationBasePathKey);
        }

        public static void SetAppBasePath(this IConfigurationBuilder cfg, string path)
        {
            cfg.Set(ApplicationBasePathKey, path);
        }


        public static string EnvJsonPath(this IConfiguration cfg) {
            return cfg.Get(EnvJsonConvention.EnvConfigPathKey);
        }

        public static string EnvJsonPath(this IConfigurationBuilder cfg)
        {
            return cfg.Get(EnvJsonConvention.EnvConfigPathKey);
        }

        public static IEnumerable<string> GetConfigFiles(this IEnumerable<IConfigurationSource> sources)
        {
            foreach (var src in sources)
            {
                if (src is FileConfigurationSource)
                {
                    var filesrc = src as FileConfigurationSource;
                    var providerPath = (filesrc.FileProvider as Microsoft.Extensions.FileProviders.PhysicalFileProvider)?.Root ?? "";
                    yield return Path.Combine(providerPath, filesrc.Path);
                }
            }
        }

        public static string GetConfigTrace(this IConfigurationBuilder cfgBuilder)
        {
            var cfgTrace = new System.Text.StringBuilder();
            cfgTrace.AppendLine();
            cfgTrace.AppendLine(" === Config Sources: ===");
            foreach (var src in cfgBuilder.Sources)
            {
                cfgTrace.Append("  - ");
                if (src is FileConfigurationSource)
                {
                    var filesrc = src as FileConfigurationSource;
                    var providerPath = (filesrc.FileProvider as Microsoft.Extensions.FileProviders.PhysicalFileProvider)?.Root ?? "";

                    cfgTrace.Append($"{providerPath} {filesrc.Path}");
                    if (filesrc.Optional) cfgTrace.Append(" (optional)");
                }
                else if (src is MemoryConfigurationSource)
                {
                    var memsrc = src as MemoryConfigurationSource;
                    var topKeys = memsrc.InitialData.Take(4).Select(kvp => kvp.Key).ToList();
                    if (memsrc.InitialData.Count() > topKeys.Count) topKeys.Add("...");
                    var sample = "[" + string.Join(",", topKeys) + "]";
                    cfgTrace.Append($"{src.GetType().Name} {sample}");
                }
                else
                {
                    cfgTrace.Append($"{src.GetType().Name}");
                }
                cfgTrace.AppendLine();
            }
            cfgTrace.AppendLine(" === END Config      ===");
            return cfgTrace.ToString();
        }

        public static IEnumerable<string> GetIncludePaths(this IConfigurationBuilder cfgBuilder, string key = "include") {
            var includeFiles = cfgBuilder.Get(key);
            if (includeFiles != null)
            {
                foreach (var split in includeFiles.Split(';',','))
                {
                    var path = split;
                    if (!Path.IsPathRooted(path))
                    {
                        var envpath = cfgBuilder.Get(EnvJsonConvention.EnvConfigPathKey);
                        if (envpath != null && !path.StartsWith("{basePath}"))
                        {
                            path = Path.Combine(Path.GetDirectoryName(envpath), path);
                        }
                    }
                    path = Path.GetFullPath(path);

                    yield return path;
                    
                }
            }
        }

        public static IConfigurationBuilder AddIncludeFiles(this IConfigurationBuilder cfgBuilder, IEnumerable<string> paths) {
            foreach(var path in paths) {
                if (path.EndsWith(".config") || path.EndsWith(".xml")) {
                    cfgBuilder.AddXmlAppSettings(path, optional: false, reloadOnChange: true);
                }
                else if (path.EndsWith(".json")) {
                    cfgBuilder.AddJsonFile(path, optional: false, reloadOnChange: true);
                } else {
                    throw new ArgumentException($"unrecognized include file '{path}'");
                }
            }

            return cfgBuilder;
        }

        public static IConfigurationBuilder AddIncludeFiles(this IConfigurationBuilder cfgBuilder) 
            => cfgBuilder.AddIncludeFiles(cfgBuilder.GetIncludePaths());
        public static IConfigurationBuilder AddIncludeFiles(this IConfigurationBuilder cfgBuilder, string key) 
            => cfgBuilder.AddIncludeFiles(cfgBuilder.GetIncludePaths(key));
    }
}

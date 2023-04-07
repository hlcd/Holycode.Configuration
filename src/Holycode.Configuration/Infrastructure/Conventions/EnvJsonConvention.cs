using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;


namespace Holycode.Configuration.Conventions
{

    public class EnvJsonConvention : IConfigSourceConvention
    {
        internal const string EnvironmentNameKey = "ASPNET_ENV";
        internal const string DefaultEnvironment = "development";
        
        private const string BuildKey = "build";
        private const string DefaultBuild = "local";
        
        public string MainConfigFile = "env.json";

        public bool IsMainConfigOptional = false;
        public bool IsEnvSpecificConfigOptional = false;

        internal const string EnvConfigFoundKey = "env:config:found";
        internal const string EnvConfigPathKey = "env:config:path";

        private readonly string baseDir;
        private readonly string envName;
        public EnvJsonConvention(string appBaseDir, string environmentName = null)
        {
            baseDir = appBaseDir;
            envName = environmentName;
        }
        public IEnumerable<IConfigurationSource> GetConfigSources()
        {
            var configFiles = FindConfigFiles();

            return GetConfigSources(configFiles);
        }

        private IEnumerable<ConfigPathSource> FindConfigFiles()
        {
            var toSearch = MainConfigFile;
            if (envName != null && IsMainConfigOptional) 
            {                 
                // if env.json is optional, search for env.{_envName}.json
                var searchFiles = new List<string>();
                foreach(var split in MainConfigFile.Split('|')) 
                {
                    string mainConfigExt = split.Substring(split.LastIndexOf('.'));
                    string mainConfigPathNoExt = split.Substring(0, split.LastIndexOf('.'));
                    searchFiles.Add(mainConfigPathNoExt + $".{envName}" + mainConfigExt);
                }
                toSearch = string.Join("|", searchFiles);
            }
            var configFiles = new ConfigFileFinder().Find(baseDir, toSearch, stopOnFirstMatch: true);
            return configFiles;
        }

        private IEnumerable<IConfigurationSource> GetConfigSources(IEnumerable<ConfigPathSource> configFiles)
        {
            var builder = new ConfigurationBuilder();
            foreach (var file in configFiles)
            {
                string path = file.Source;
                string dir = Path.GetDirectoryName(path);
                string mainConfigNameNoExt = Path.GetFileNameWithoutExtension(path);

                if (mainConfigNameNoExt.EndsWith(".default"))
                {
                    mainConfigNameNoExt = mainConfigNameNoExt.Substring(0, mainConfigNameNoExt.Length - ".default".Length);
                }
                if (mainConfigNameNoExt.EndsWith($".{envName}"))
                {
                    mainConfigNameNoExt = mainConfigNameNoExt.Substring(0, mainConfigNameNoExt.Length - $".{envName}".Length);
                }
                

                try
                {
                    builder.AddEnvironmentVariables();

                    AddDefaultFiles(dir, mainConfigNameNoExt, builder);

                    AddMainFile(path, builder, IsMainConfigOptional);

                    string env = GetEnvName(builder);
                    builder.Set(EnvironmentNameKey, env);

                    var tempBuilder = new ConfigurationBuilder();
                    AddEnvFiles(dir, mainConfigNameNoExt, env, tempBuilder, IsEnvSpecificConfigOptional);

                    string envBuild = GetBuildName(tempBuilder);
                    builder.Set(BuildKey, envBuild);

                    AddBuildEnvFile(dir, mainConfigNameNoExt, envBuild, builder);

                    AddEnvFiles(dir, mainConfigNameNoExt, env, builder, IsEnvSpecificConfigOptional);
                    AddOverrideFiles(dir, mainConfigNameNoExt, env, builder);
                }
                catch (Exception ex)
                {
                    throw new FileLoadException($"Failed to load config file {path}: {ex.Message}", ex);
                }
            }

            return builder.Sources;
        }

        private string GetEnvName(ConfigurationBuilder builder)
        {
            string env = envName;
            if (string.IsNullOrWhiteSpace(env))
            {
                env = builder.Get(EnvironmentNameKey);
            }

            if (string.IsNullOrWhiteSpace(env))
            {
                env = DefaultEnvironment;
            }

            return env;
        }

        private string GetBuildName(ConfigurationBuilder builder)
        {
            string build = builder.Get(BuildKey);

            if (string.IsNullOrWhiteSpace(build))
            {
                build = DefaultBuild;
            }

            return build;
        }

        private void AddMainFile(string path, ConfigurationBuilder builder, bool optional)
        {
            // if MainConfigFile contains folder, it will be already included in dir. strip it from mainconfigfile
            if (!File.Exists(path) && !optional)
            {
                throw new FileLoadException($"Failed to load main config file {path}", path);
            }
            builder.AddJsonFile(path, optional: optional);

            builder.Set(EnvConfigFoundKey, "true");
            builder.Set(EnvConfigPathKey, path);
        }

        private void AddDefaultFiles(string dir, string mainConfigNameNoExt, ConfigurationBuilder builder)
        {
            builder.AddJsonFile(Path.Combine(dir, $"{mainConfigNameNoExt}.default.json"), optional: true);
        }

        private void AddBuildEnvFile(string dir, string mainConfigNameNoExt, string buildName, ConfigurationBuilder builder)
        {
            builder.AddJsonFile(Path.Combine(dir, $"{mainConfigNameNoExt}.{buildName}.json"), optional: true);
        }

        private void AddEnvFiles(string dir, string mainConfigNameNoExt, string env, ConfigurationBuilder builder, bool optional)
        {
            builder.AddJsonFile(Path.Combine(dir, $"{mainConfigNameNoExt}.{env}.json"), optional: optional);
        }
        private void AddOverrideFiles(string dir, string mainConfigNameNoExt, string env, ConfigurationBuilder builder)
        {
            builder.AddJsonFile(Path.Combine(dir, $"{mainConfigNameNoExt}.override.json"), optional: true);
            builder.AddJsonFile(Path.Combine(dir, $"{mainConfigNameNoExt}.{env}.override.json"), optional: true);
        }
    }
}
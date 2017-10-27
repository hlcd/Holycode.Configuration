using System;
using Should;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Holycode.Configuration.Conventions;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Memory;
using static Holycode.Configuration.Tests.TestHelpers;

namespace Holycode.Configuration.Tests
{
    [TestClass]
    public class env_json_test
    {
        [TestMethod]
        public void global_env_json_should_be_found()
        {

            var src = ConfigFactory.CreateConfigSource(GetPath("."));
            src.AddEnvJson(optional: false);
        }

        [TestMethod]
        public void should_find_env_json_root_folder_with_absolute_path()
        {
            var envPaths = new ConfigFileFinder().Find(GetPath(@"input\reporoot1\subfolder\projectfolder\"), "env.json");

            envPaths.Count().ShouldBeGreaterThan(0);
            envPaths.Last().Directory.ShouldEqual(GetPath(@"input\reporoot1"));
        }

        [TestMethod]
        public void should_find_env_json_root_in_config_dir()
        {
            var envPaths = new ConfigFileFinder().Find(GetPath(@"input\reporoot1\subfolder\projectfolder\"), "config/env.json");

            envPaths.Count().ShouldBeGreaterThan(0);
            envPaths.Last().Directory.ShouldEqual(GetPath(@"input\reporoot1\config"));
        }


        [TestMethod]
        public void should_find_env_json_root_folder_with_relative_path()
        {
            var d = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(GetPath("."));
                var envPaths = new ConfigFileFinder().Find(@"input\reporoot1\subfolder\projectfolder\", "env.json");

                envPaths.Count().ShouldBeGreaterThan(0);
                envPaths.Last().Directory.ShouldEqual(@"input\reporoot1");
            } finally
            {
                Directory.SetCurrentDirectory(d);
            }
        }

        [TestMethod]
        public void should_include_env_specific_config()
        {
            var src = ConfigFactory.CreateConfigSource(GetPath(@"input\reporoot1\subfolder\projectfolder\"));
            src.AddEnvJson(optional: false);

            var val = src.Get("test_devel");

            src.EnvironmentName().ShouldEqual("development");
            val.ShouldNotBeNull();
            val.ShouldEqual("test_from_devel");
        }

        [TestMethod]
        public void should_load_specified_env()
        {
            var src = ConfigFactory.CreateConfigSource(GetPath(@"input\reporoot1\subfolder\projectfolder\"));
            src.AddEnvJson(optional: false, environment: "test");

            var val = src.Get("test_local");
            var isTest = src.Get("is_test_env");

            val.ShouldNotBeNull();
            val.ShouldEqual("test_from_local");
            isTest.ShouldNotBeNull();
            isTest.ShouldEqual("true");
        }

        [TestMethod]
        public void should_set_basepath_in_ctor()
        {
            var p = GetPath(@"input\reporoot1\subfolder\projectfolder\");
            var src = ConfigFactory.CreateConfigSource(p);

            var basepath = src.AppBasePath();

            basepath.ShouldNotBeNull();
            basepath.ShouldEqual(p);
        }

        [TestMethod]
        public void should_include_override_config()
        {
            var src = ConfigFactory.CreateConfigSource(GetPath(@"input\reporoot1\subfolder\projectfolder\"));
            src.AddEnvJson(optional: false, environment: "test");

            var val = src.Get("test_local");

            val.ShouldNotBeNull();
            val.ShouldEqual("test_from_local");
        }

        [TestMethod]
        public void in_mem_collection_order()
        {
            var src = new ConfigurationBuilder();

            src.AddInMemoryCollection(new Dictionary<string, string>() {
                { "key1", "val1" }
            });

            var val = src.Build().Get("key1");
            val.ShouldEqual("val1");

            src.AddInMemoryCollection(new Dictionary<string, string>() {
                { "key1", "val2" }
            });

            val = src.Build().Get("key1");
            val.ShouldEqual("val2");
        }

        [TestMethod]
        public void in_mem_collection_replace()
        {
            var src = new ConfigurationBuilder();

            src.AddInMemoryCollection(new Dictionary<string, string>() {
                { "key1", "val1" }
            });

            var val = src.Build().Get("key1");
            val.ShouldEqual("val1");

            var mem = src.Sources.LastOrDefault() as MemoryConfigurationSource;

            var data = mem.InitialData?.ToList() ?? new List<KeyValuePair<string, string>>();
            data.RemoveAll(d => d.Key == "key1");
            data.Add(new KeyValuePair<string, string>("key1", "val2"));
            mem.InitialData = data;

            val = src.Build().Get("key1");
            val.ShouldEqual("val2");
        }

        [TestMethod]
        public void set_should_override_previous_set_values()
        {
            var src = ConfigFactory.CreateConfigSource(GetPath(@"input\reporoot1\subfolder\projectfolder\"));

            src.Set("some_key", "from code");
            var val = src.Get("some_key");
            val.ShouldEqual("from code");

            src.Set("some_key", "from code 1");
            val = src.Get("some_key");
            val.ShouldEqual("from code 1");
        }

        [TestMethod]
        public void set_should_override_previous_file_values()
        {
            var src = ConfigFactory.CreateConfigSource(GetPath(@"input\reporoot1\subfolder\projectfolder\"));

            src.Set("some_key", "from code");
            var val = src.Get("some_key");
            val.ShouldEqual("from code");

            src.Set("some_key", "from code 1");
            val = src.Get("some_key");
            val.ShouldEqual("from code 1");

            src.AddEnvJson(optional: false, environment: "test");

            val = src.Get("some_key");
            val.ShouldEqual("from env.test.json");

            src.Set("some_key", "from code 2");
            val = src.Get("some_key");
            val.ShouldEqual("from code 2");


        }


        [TestMethod]
        public void should_include_env_override_config()
        {
            var src = ConfigFactory.CreateConfigSource(GetPath(@"input\reporoot1\subfolder\projectfolder\"));
            src.AddEnvJson(optional: false, environment: "development");

            var val = src.Get("test_local");
            var devVal = src.Get("test_development_local");
            var prodVal = src.Get("test_prod_local");
            var envVal = src.Get("env_override");

            val.ShouldNotBeNull();
            val.ShouldEqual("test_development_local");
            devVal.ShouldNotBeNull();
            devVal.ShouldEqual("test_development_local");
            prodVal.ShouldBeNull();

            envVal.ShouldNotBeNull();
        }

     
        [TestMethod]
        public void should_include_env_default_config()
        {
            var src = ConfigFactory.CreateConfigSource(GetPath(@"input\reporoot2\subfolder"));
            src.AddEnvJson(optional: false);
    
            var trace = src.GetConfigTrace();
            Console.WriteLine(trace);

            var val = src.EnvironmentName();
            
            val.ShouldEqual("default-env");
        }

        [TestMethod]
        public void should_resolve_nearest_env_json_before_config_dir()
        {
            var applicationBasePath = GetPath(@"input\reporoot1");
            string environment = null;

            var resolver = new ConfigSourceResolver(new[] {
                new EnvJsonConvention(applicationBasePath, environmentName: environment) {
                    MainConfigFile = "env.json"
                },
                new EnvJsonConvention(applicationBasePath, environmentName: environment) {
                    MainConfigFile = "config/env.json"
                },
            }, stopOnFirstMatch: true);

            var cfgFiles = resolver.GetConfigSources().GetConfigFiles().ToArray();

            var expected = new[] {
                GetPath(@"input\reporoot1\env.default.json"),
                GetPath(@"input\reporoot1\env.json"),
                GetPath(@"input\reporoot1\env.development.json"),
                GetPath(@"input\reporoot1\env.override.json"),
                GetPath(@"input\reporoot1\env.development.override.json"),
            };

            for (int i = 0; i < cfgFiles.Length && i < expected.Length; i++)
            {
                cfgFiles[i].ShouldEqual(expected[i]);
            }
        }      

        /*
         [TestMethod]
        public void should_resolve_nearest_env_json_for_specific_env()
        {
            var applicationBasePath = GetPath(@"input\reporoot1");
            string environment = "from_config_dir";

            var resolver = new ConfigSourceResolver(new[] {
                new EnvJsonConvention(applicationBasePath, environmentName: environment) {
                    MainConfigFile = "env.json",
                    IsMainConfigOptional = environment != null,
                },
                new EnvJsonConvention(applicationBasePath, environmentName: environment) {
                    MainConfigFile = "config/env.json",
                    IsMainConfigOptional = environment != null,
                },
            }, stopOnFirstMatch: true);

            var cfgFiles = resolver.GetConfigSources().GetConfigFiles().ToArray();

            var expected = new[] {
                GetPath(@"input\reporoot1\config\env.default.json"),
                GetPath(@"input\reporoot1\config\env.json"),
                GetPath(@"input\reporoot1\config\env.from_config_dir.json"),
                GetPath(@"input\reporoot1\config\env.override.json"),
                GetPath(@"input\reporoot1\config\env.from_config_dir.override.json"),
            };

            for (int i = 0; i < cfgFiles.Length && i < expected.Length; i++)
            {
                cfgFiles[i].ShouldEqual(expected[i]);
            }
        }     
        */ 
    }
}

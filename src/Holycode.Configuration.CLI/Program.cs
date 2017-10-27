using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Holycode.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Xml;

namespace Holycode.Configuration.CLI
{
    public class Program
    {
        private static bool verbose = false;
        private static bool noEnvVar = false;
        private static bool fullOutput = false;

        public static int Main(string[] args)
        {
            //Console.WriteLine($"args={string.Join(" ", args)}");

            try
            {
                string env = null;
                string cmd = null;
                string cfgPath = null;
                string diskPath = ".";
                List<string> nodashArgs = new List<string>();
                for (int i = 0; i < args.Length; i++)
                {
                    if (!args[i].StartsWith("-"))
                    {
                        nodashArgs.Add(args[i]);
                        continue;
                    }
                    if (args[i].Equals("--version", StringComparison.OrdinalIgnoreCase) 
                        || args[i].Equals("-v", StringComparison.OrdinalIgnoreCase))
                    {
                        PrintVersion();
                        return 0;
                    }
                       if (args[i].Equals("--help", StringComparison.OrdinalIgnoreCase) 
                            || args[i].Equals("-h", StringComparison.OrdinalIgnoreCase)
                            || args[i].Equals("-?", StringComparison.OrdinalIgnoreCase))
                    {
                        PrintUsage();
                        return 0;
                    }

                    if (args[i].Equals("--env", StringComparison.OrdinalIgnoreCase) ||
                        args[i].Equals("-env", StringComparison.OrdinalIgnoreCase))
                    {
                        if (i == args.Length - 1 || args[i + 1].StartsWith("-"))
                        {
                            throw new ArgumentException("arg --env requires a value!");
                        }
                        env = args[i + 1];
                        i++;
                        continue;
                    }
                    if (args[i].Equals("--verbose", StringComparison.OrdinalIgnoreCase))
                    {
                        verbose = true;
                        continue;
                    }
                    if (args[i].Equals("--no-env-var", StringComparison.OrdinalIgnoreCase))
                    {
                        noEnvVar = true;
                        continue;
                    }
                     if (args[i].Equals("--full", StringComparison.OrdinalIgnoreCase))
                    {
                        fullOutput = true;
                        continue;
                    }
                }

                if (nodashArgs.Count > 0)
                {
                    var diskPathIdx = 0;
                    var cmdIdx = 1;
                    var cfgPathIdx = 2;
                    if (nodashArgs.Count < 3)
                    {
                        if (System.IO.File.Exists(nodashArgs[0]) || System.IO.Directory.Exists(nodashArgs[0]) || nodashArgs[0].Contains("/") || nodashArgs.Contains("\\"))
                        {
                            // first argument seems like a disk path                                                       
                        }
                        else
                        {
                            // first argument seems like a cmd
                            diskPathIdx = -1;
                            cmdIdx = 0;
                            cfgPathIdx = 1;
                        }
                    }

                    diskPath = diskPathIdx >= 0 && diskPathIdx < nodashArgs.Count ? nodashArgs[diskPathIdx] : ".";
                    cmd = cmdIdx >= 0 && cmdIdx < nodashArgs.Count ? nodashArgs[cmdIdx] : null;
                    cfgPath = cfgPathIdx >= 0 && cfgPathIdx < nodashArgs.Count ? nodashArgs[cfgPathIdx] : null;
                }

                //Console.WriteLine($"looking for env.config... environment={env}");
                if (cmd == null) cmd = "tree";

                var builder = GetBuilder(diskPath, env);

                //builder.AddJsonFile("config.json", optional: true);
                //builder.AddJsonFile($"config.{builder.EnvironmentName()}.json", optional: true);

                var conf = builder.Build();

                if (cmd.Equals("info", StringComparison.OrdinalIgnoreCase)
                    || cmd.Equals("full", StringComparison.OrdinalIgnoreCase))
                {
                    fullOutput = true;
                }

                if (cmd == "get")
                {
                    if (cfgPath == null)
                    {
                        ListConfigValues(conf, builder);
                    }
                    else
                    {
                        //cfgPath = cfgPath.Replace('.',':');
                        var val = conf.Get(cfgPath);
                        if (val == null)
                        {
                            var sub = conf.GetSection(cfgPath);

                            if (sub != null)
                            {
                                ListConfigValues(sub, builder);
                            }
                            else
                            {
                                return 1;
                            }
                        }
                        else
                        {
                            Console.WriteLine(val);
                        }
                    }

                    return 0;
                }
                else if (cmd == "list")
                {
                    System.Console.WriteLine($"base path: {conf.AppBasePath()}");
                    System.Console.WriteLine();
                    ListConfigKeys(conf, builder);
                    return 0;
                }
                else if (cmd.Equals("set-env", StringComparison.OrdinalIgnoreCase)
                || cmd.Equals("setenv", StringComparison.OrdinalIgnoreCase))
                {
                    if (cfgPath == null)
                    {
                        Console.WriteLine("Error: expected environment name!");
                        return -1;
                    }
                    else
                    {
                        SetEnv(builder, cfgPath);

                        // reload config
                        builder = GetBuilder(diskPath, env);

                        System.Console.WriteLine($"base path:   {conf.AppBasePath()}");
                        System.Console.WriteLine($"environment: {conf.EnvironmentName()}");
                        System.Console.WriteLine();
                        ShowConfigTree(builder);
                    }

                    return 0;
                }
                else if (cmd.Equals("connstr", StringComparison.OrdinalIgnoreCase)
                || cmd.Equals("getconnstr", StringComparison.OrdinalIgnoreCase)
                || cmd.Equals("conn", StringComparison.OrdinalIgnoreCase))
                {
                    PrintConnectionStrings(conf, cfgPath);
                    return 0;
                }
                else if (cmd.Equals("tree", StringComparison.OrdinalIgnoreCase)
                    || cmd.Equals("show", StringComparison.OrdinalIgnoreCase)
                    || cmd.Equals("info", StringComparison.OrdinalIgnoreCase)
                    || cmd.Equals("full", StringComparison.OrdinalIgnoreCase))
                {
                    System.Console.WriteLine($"base path:   {conf.AppBasePath()}");
                    System.Console.WriteLine($"environment: {conf.EnvironmentName()}");
                    System.Console.WriteLine();
                    ShowConfigTree(builder);
                    if (fullOutput) {
                        System.Console.WriteLine();
                        System.Console.WriteLine("Connection strings:");
                        System.Console.WriteLine();
                        PrintConnectionStrings(conf, null);
                    }
                }
                else
                {
                    System.Console.WriteLine($"Unknown command: '{cmd}'");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message} {ex.StackTrace}");
                return -1;
            }

            return 0;
        }



        private static IConfigurationBuilder GetBuilder(string diskPath, string env)
        {
            var builder = ConfigFactory.CreateConfigSource(Path.GetFullPath(diskPath));
            builder.AddEnvJson(environment: env, optional: false);
            builder.AddIncludeFiles();

            return builder;
        }

        private static void PrintUsage()
        {
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine("hfcg [path] <command> [switch]");
            System.Console.WriteLine();
            Console.WriteLine("Available commands:");
            Console.WriteLine(" tree|show        shows config tree");
            Console.WriteLine(" info|full        shows full config info (including connectionstrings)");
            Console.WriteLine(" get              returns whole configuration, serialized as JSON");
            Console.WriteLine(" list             list configuration keys");
            Console.WriteLine(" get {key}        returns config value for key {key}");
            Console.WriteLine(" connstr [{name}] returns connection string with name {name}");
            Console.WriteLine(" set-env {name}   modify env.json to use environment named {name}");
            Console.WriteLine();
            Console.WriteLine("Switches:");
            Console.WriteLine(" --verbose           show verbose log");
            Console.WriteLine(" --full              show full output");
            Console.WriteLine(" --help              show this help page");
            Console.WriteLine(" --env {environment} use specific environment");
            Console.WriteLine(" --no-env-var        do not include ENV variables in result");
        }

        private static void PrintVersion()
        {
            var asm = typeof(Program).GetTypeInfo().Assembly;
            //var ver = asm.GetName().Version
            #if !CORECLR
            var ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion;
            #else 
            var ver = "?";
            #endif
            System.Console.WriteLine($"hcfg version {ver}");
        }

        private static void ListConfigKeys(IConfiguration conf, IConfigurationBuilder builder)
        {
            var dict = conf.AsDictionaryPlain();
            foreach (var k in dict.Keys)
            {
                Console.WriteLine(k);
            }
        }

        private static Dictionary<TKey, TVal> BuildAnonymousDict<TKey, TVal>(Func<TVal> factory)
        {
            return new Dictionary<TKey, TVal>();
        }
        private static Dictionary<string, TVal> BuildAnonymousDict<TVal>(Func<TVal> factory)
        {
            return new Dictionary<string, TVal>();
        }
        private static Dictionary<TKey, TVal> BuildDict<TKey, TVal>(IEnumerable<KeyValuePair<TKey, TVal>> list)
        {
            return list.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }


        class ConfigSourceInfo
        {
            public string Path;
            public string Name;
            public string FullPath;
        }
        private static ConfigSourceInfo GetConfigSourceInfo(IConfigurationSource src)
        {
            var srcName = src.ToString();
            if (src is FileConfigurationSource)
            {
                var file = (src as FileConfigurationSource);
                return new ConfigSourceInfo()
                {
                    Name = $"{file.Path} ({file.FileProvider.GetFileInfo(file.Path).PhysicalPath})",
                    Path = file.Path,
                    FullPath = file.FileProvider.GetFileInfo(file.Path).PhysicalPath
                };
            }
            else if (src is EnvironmentVariablesConfigurationSource)
            {
                return new ConfigSourceInfo()
                {
                    Name = "ENV"
                };
            }
            else if (src is Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource)
            {
                return new ConfigSourceInfo()
                {
                    Name = "in-mem"
                };
            }
            else
            {
                return new ConfigSourceInfo()
                {
                    Name = src.ToString()
                };
            }

        }

        private static void SetEnv(IConfigurationBuilder builder, string name)
        {
            string envJson = null;
            foreach (var src in builder.Sources)
            {
                var srcInfo = GetConfigSourceInfo(src);
                if (srcInfo.FullPath != null)
                {
                    var fn = Path.GetFileName(srcInfo.FullPath);
                    if (fn == "env.default.json")
                    {
                        envJson = srcInfo.FullPath;
                        envJson = Path.Combine(Path.GetDirectoryName(envJson), "env.json");
                    }
                    if (fn == "env.json")
                    {
                        envJson = srcInfo.FullPath;
                        break;
                    }
                }
            }

            if (envJson == null) throw new Exception("no valid env.json or env.default.json found!");


            if (name == "default" || name == "auto")
            {
                var bak = Path.Combine(Path.GetDirectoryName(envJson), "_" + Path.GetFileName(envJson));

                if (!File.Exists(envJson))
                {
                    System.Console.WriteLine($"'{envJson}' not found - already using defaults.");
                    return;
                }
                System.Console.WriteLine($"Reverting environment to default settings: moving '{envJson}' to '{bak}'");

                if (File.Exists(bak)) File.Delete(bak);
                File.Move(envJson, bak);

                return;
            }

            var cfgLine = $@"""ASPNET_ENV"": ""{name}""";
            var cfgText = new string[] { $@"{{
  {cfgLine}
}}" };
            if (File.Exists(envJson))
            {
                cfgText = File.ReadAllLines(envJson);
                for (int i = 0; i < cfgText.Length; i++)
                {
                    if (cfgText[i].Contains("ASPNET_ENV"))
                    {
                        cfgText[i] = cfgLine;
                    }
                }
            }

            System.Console.WriteLine($"setting {envJson} to environment: '{name}'");
            File.WriteAllLines(envJson, cfgText, Encoding.UTF8);

        }
        
        private static void PrintConnectionStrings(IConfiguration conf, string cfgPath = null) {
            if (cfgPath == null)
            {
                var connStrs = conf.GetSection("connectionStrings");
                foreach (var cs in connStrs.GetChildren())
                {
                    var val = conf.GetConnectionStringValue(cs.Key);
                    Console.WriteLine($"  {cs.Key}={val}");
                }
            }
            else
            {
                var val = conf.GetConnectionStringValue(cfgPath);
                Console.WriteLine($"  {cfgPath}={val}");
            }
        }
        private static void ShowConfigTree(IConfigurationBuilder builder)
        {

            Stack<ConfigSourceInfo> parents = new Stack<ConfigSourceInfo>();

            foreach (var src in builder.Sources)
            {
                var srcInfo = GetConfigSourceInfo(src);
                if (srcInfo.Path != null)
                {
                    var fn = Path.GetFileNameWithoutExtension(srcInfo.Path);
                    while (parents.Count > 0)
                    {
                        var p = parents.Peek();
                        var pfn = Path.GetFileNameWithoutExtension(p.Path);
                        if (fn.StartsWith(pfn))
                        {
                            // fn is a child of pfn
                            break;
                        }
                        else
                        {
                            parents.Pop();
                        }
                    }

                    System.Console.Write("  ");
                    if (File.Exists(srcInfo.FullPath))
                    {
                        System.Console.Write(System.Console.OutputEncoding == System.Text.Encoding.UTF8 ? "[√]" : "[v]");
                    }
                    else
                    {
                        System.Console.Write("[×]");
                    }
                    System.Console.Write("  ");

                    int padding = 40;
                    if (parents.Count > 0)
                    {
                        System.Console.Write((System.Console.OutputEncoding == System.Text.Encoding.UTF8 ? "└╴" : "└-").PadLeft(parents.Count * 2));
                        padding -= 2;
                    }

                    System.Console.Write(srcInfo.Path.PadRight(padding));
                    System.Console.Write(" ");
                    System.Console.Write($"({srcInfo.FullPath})");

                    System.Console.WriteLine();
                    parents.Push(srcInfo);
                }
            }
            System.Console.WriteLine();
        }

        private static void ListConfigValues(IConfiguration conf, IConfigurationBuilder builder)
        {
            var result = BuildAnonymousDict(() => new { Value = "", Source = "" });
            var dict = conf.AsDictionaryPlain();

            foreach (var src in builder.Sources.Reverse())
            {
                var srcName = GetConfigSourceInfo(src).Name;

                var count = 0;
                //Console.WriteLine($"examining source '{srcName}'");
                var srcCfg = src.Build(builder);
                srcCfg.Load();
                foreach (var k in dict.Keys)
                {
                    var key = k;
                    if (conf is IConfigurationSection)
                    {
                        key = (conf as IConfigurationSection).Path + ":" + key;
                    }
                    if (srcCfg.TryGet(key, out string v))
                    {
                        count++;
                        if (!result.ContainsKey(key))
                        {
                            result.Add(key, new { Value = v, Source = srcName });
                        }
                    }
                }
                //Console.WriteLine($"source '{srcName}' has {count} entries");
            }

            if (noEnvVar)
            {
                result = BuildDict(result.Where(kvp => kvp.Value.Source != "ENV"));
            }

            string serialized = null;
            if (verbose)
            {
                serialized = Newtonsoft.Json.JsonConvert.SerializeObject(result,
                    Newtonsoft.Json.Formatting.Indented);
            }
            else
            {
                var r2 = BuildDict(result.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.Value)));
                serialized = Newtonsoft.Json.JsonConvert.SerializeObject(r2,
                    Newtonsoft.Json.Formatting.Indented);
            }
            Console.WriteLine(serialized);
        }

    }
}

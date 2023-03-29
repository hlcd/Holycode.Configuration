using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Holycode.Configuration.Converter
{
    class Program
    {
        /// <summary>
        /// Perform transformation from xml to json file format.
        /// </summary>
        /// <param name="input">input file</param>
        /// <param name="output">output file</param>
        /// <param name="inputNode">the node in input file which will be converted, given as XPath. If not given, entire input is converted.</param>
        /// <param name="outputNode">
        /// the node in output file where converted items will be placed, given as JsonPath. If not given, entire output overwrites entire document - must not be be mixed with <c>append</c> option in this case.
        /// </param>
        /// <param name="diffBase">
        /// The file used as diff. Output file with only contain diff between <c>diffBase</c> and generated output.
        /// If <paramref name="outputNode"/> is specified, it used to for diffBase file too.
        /// </param>
        /// <param name="asPatch">
        /// If <c>true</c> and <paramref name="diffBase"/> was used too, generated configuration node will be stored
        /// as json patch. This is required to properly handle scenarios where xml configuration node contains
        /// arrays.
        /// </param>
        static void Main(FileInfo input, FileInfo output,
            string? inputNode = null, string? outputNode = null, FileInfo? diffBase = null, bool asPatch = false)
        {
            string? error = Validate(input, output, inputNode, outputNode, diffBase);
            if (error != null)
            {
                Console.WriteLine($"ERROR: {error}");
            }

            const string dir = "C:\\_lgm.core\\config";
            string[] prodFiles = new[]
            {
                "prod/prod.pl.config",
                "prod/prod.de.config",
                "prod/prod.vmss.pl.config",
                "prod/prod-internal-net.pl.config"
            };

            string[] devFiles = new[]
            {   
                "alpha.pl.config",
                "alpha.pl.tests.config",
                "beta.pl.config",
                "beta.pl.tests.config",
                "alpha.de.config",
                "dynamic.pl.config",
                "dynamic-tests.pl.config",
            };

            string[] localFiles = new[]
            {
                "local.pl.config",
                "local.de.config",
                "local.beta.pl.config",
            };

            string commonDevFile = "same-dev.xml";
            CompareAppConfigFiles(devFiles, dir, commonDevFile);
            string commonLocalFile = "same-local.xml";
            CompareAppConfigFiles(localFiles, dir, commonLocalFile);
            string commonProdFile = "same-prod.xml";
            CompareAppConfigFiles(prodFiles, dir, commonProdFile);


            string[] envCommonSettings = new[]
            {
                commonProdFile,
                commonDevFile,
                commonLocalFile
            };
            string globalCommonFile = "same-all.xml";
            CompareAppConfigFiles(envCommonSettings, AppDomain.CurrentDomain.BaseDirectory, globalCommonFile);

            CleanupConfig(globalCommonFile, envCommonSettings);
            CleanupConfig(globalCommonFile,
                localFiles.Select(l => Path.Combine(dir, l)).ToArray(), updateInPlace: true);
            CleanupConfig(commonLocalFile + CleanedUpSuffix, 
                localFiles.Select(l => Path.Combine(dir, l)).ToArray(), updateInPlace: true);

            CleanupConfig(globalCommonFile,
                devFiles.Select(l => Path.Combine(dir, l)).ToArray(), updateInPlace: true);
            CleanupConfig(commonDevFile + CleanedUpSuffix, 
                devFiles.Select(l => Path.Combine(dir, l)).ToArray(), updateInPlace: true);


            CleanupConfig(globalCommonFile,
                prodFiles.Select(l => Path.Combine(dir, l)).ToArray(), updateInPlace: true);
            CleanupConfig(commonProdFile + CleanedUpSuffix, 
                prodFiles.Select(l => Path.Combine(dir, l)).ToArray(), updateInPlace: true);

            return;

            try
            {
                FileMode outputMode = outputNode != null ? FileMode.OpenOrCreate : FileMode.Create;
                string newContent;
                using (Stream inputStream = input.OpenRead())
                using (FileStream outputStream = output.Open(outputMode, FileAccess.ReadWrite, FileShare.Read))
                {
                    newContent = FromXml.Convert(inputStream, outputStream, inputNode, outputNode, diffBase, asPatch);
                }

                using (Stream saveResult = output.Open(FileMode.Create))
                {
                    var writer = new StreamWriter(saveResult, Encoding.UTF8);
                    writer.Write(newContent);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }
        }

        private static Dictionary<string, string> LoadSettingsFromFile(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                var xml = XDocument.Load(stream);

                return xml.Descendants("add")
                    ?.Select(node => new
                    {
                        Key = node.Attribute("key")?.Value,
                        Value = node.Attribute("value")?.Value
                    })
                    .Where(x => x.Key != null)
                    .ToDictionary(x => x.Key!, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase)!;
            }
        }

        private const string CleanedUpSuffix = ".updated";

        /// <summary>
        /// Loads entries from <paramref name="commonSettingsFile"/>.
        /// All loaded entries are removed from each file from <paramref name="files"/> collection
        /// </summary>
        /// <param name="commonSettingsFile">common settings file</param>
        /// <param name="files">files to update</param>
        /// <param name="updateInPlace">
        /// If <c>true</c>, <paramref name="files"/> are updated in place.
        /// Otherwise, new file is saved with <c>.updated</c> suffix in its name.
        /// </param>
        private static void CleanupConfig(string commonSettingsFile, string[] files, 
            bool updateInPlace = false)
        {
            string[] filesToClean = files;
            var commonOpts = LoadSettingsFromFile(commonSettingsFile);
            foreach (string file in filesToClean)
            {
                XDocument xml;
                using (var stream = File.OpenRead(file))
                {
                    xml = XDocument.Load(stream);
                }

                foreach (var setting in commonOpts)
                {
                    XElement? node = xml.XPathSelectElement($"/appSettings/add[@key='{setting.Key}']");
                    if (node != null)
                    {
                        node.Remove();
                    }
                }

                string outFile = updateInPlace
                    ? file
                    : file + CleanedUpSuffix;
                FileMode mode = updateInPlace
                    ? FileMode.Truncate
                    : FileMode.CreateNew;
                using (var stream = File.Open(outFile, mode, FileAccess.ReadWrite))
                {
                    xml.Save(stream);
                }
            }
        }

        /// <summary>
        /// Stores all settings which are the same in single xml file
        /// </summary>
        /// <param name="files">files to process</param>
        /// <param name="configTopDir">directory where <paramref name="files"/> are placed</param>
        /// <param name="outputFile">
        /// result file. This file will contain settings which are the same in every
        /// file in <paramref name="files"/> parameter
        /// </param>
        private static void CompareAppConfigFiles(string[] files, string configTopDir, string outputFile)
        {
            Dictionary<string, int> allConfigs = new();
            Dictionary<string, HashSet<string>> allKeyValues = new(StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                string path = Path.Combine(configTopDir, file);
                Dictionary<string, string> optsMap = LoadSettingsFromFile(path);
                foreach (var kvp in optsMap)
                {
                    if (allKeyValues.TryGetValue(kvp.Key, out var valueSet))
                    {
                        valueSet.Add(kvp.Value!);
                    }
                    else
                    {
                        allKeyValues[kvp.Key] = new HashSet<string>() { kvp.Value! };
                    }

                    if (allConfigs.TryGetValue(kvp.Key, out int count))
                    {
                        allConfigs[kvp.Key] = count + 1;
                    }
                    else
                    {
                        allConfigs[kvp.Key] = 1;
                    }
                }
            }

            var sameKeys = allKeyValues
                .Where(kvp => kvp.Value.Count == 1 && allConfigs[kvp.Key] == files.Length)
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"<add key=\"{kvp.Key}\" value=\"{kvp.Value.First()}\" />");

            var config = @$"<appSettings>
{string.Join(Environment.NewLine, sameKeys)}
</appSettings>";
            File.WriteAllText(outputFile, config);
        }

        private static string? Validate(FileInfo? input, FileInfo? output, 
            string? inputNode, string? outputNode, FileInfo? diff)
        {
            if (input == null)
            {
                return "no input specified";
            }
            if (output == null)
            {
                return "no output specified";
            }

            if (!input.Exists)
            {
                return $"input file was not found: {input.FullName}";
            }
            if (outputNode != null && !output.Exists)
            {
                return $"cannot append, because output file was not found: {output.FullName}";
            }

            if (diff != null && !diff.Exists)
            {
                return $"diff base file {diff.FullName} was not found";
            }

            return null;
        }
    }
}

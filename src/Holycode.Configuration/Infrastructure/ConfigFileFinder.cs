using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Holycode.Configuration
{
    ///<summary>
    /// env.json convention
    ///</summary>
    public class ConfigFileFinder
    {
        public ConfigFileFinder()
        {
        }

        public IEnumerable<ConfigPathSource> Find(string rootDir, string filePattern, bool stopOnFirstMatch = false) 
            => FindConfigFilesUpwards(rootDir, filePattern, stopOnFirstMatch);

        private IEnumerable<ConfigPathSource> FindConfigFilesUpwards(string rootDir, string filePattern, bool stopOnFirstMatch = false)
        {
            List<ConfigPathSource> names = new List<ConfigPathSource>();
            int levelUp = 0; // 0 for a path to a file and 1 for directory          

            for (string dir = rootDir; !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir), levelUp++)
            {
                var dirname = Path.GetFileName(dir);
                var found = GetConfigFilesInDir(dir, filePattern);
                if (found != null && found.Any())
                {
                    if (stopOnFirstMatch)
                    {
                        names.Add(found.First());
                    }
                    else
                    {
                        names.AddRange(found);
                    }
                    break;
                }

            }

            return names;
        }

        private IEnumerable<ConfigPathSource> GetConfigFilesInDir(string directory, string filePattern)
        {
            // stop at first env.json
            var dirname = Path.GetFileName(directory);

            filePattern = filePattern.Replace("\\", "/");
            foreach(var split in filePattern.Split('|')) 
            {
                var pattern = split; 
                var patternDir = Path.GetDirectoryName(pattern);
                if (!string.IsNullOrEmpty(patternDir))
                {
                    directory = Path.Combine(directory, patternDir);
                    pattern = Path.GetFileName(pattern);
                }
                if (System.IO.Directory.Exists(directory))
                {
                    // search for env.json                
                    var files = System.IO.Directory.GetFiles(directory, pattern);
                    if (files.Length > 0)
                    {
                        foreach (var f in files)
                        {
                            var s = new ConfigPathSource(f, $"[{this.GetType().Name}] contains {pattern}");
                            //Console.WriteLine($"found config file: {s}");
                            yield return s;
                        }
                        // do not process other directories
                    }
                }
            }
            
        }

    }

}
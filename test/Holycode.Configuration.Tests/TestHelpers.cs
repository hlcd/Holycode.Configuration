using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Holycode.Configuration.Tests
{
    static class TestHelpers
    {
        public static string GetPath(string v)
        {
            var asm = typeof(TestHelpers).Assembly;// Assembly.GetExecutingAssembly();
            var cb = asm.CodeBase.Replace("file:///", "");
            cb = Path.GetDirectoryName(cb);

            //var i = cb.LastIndexOf("\\bin\\");
            //if (i > 0)
            //{
            //    cb = cb.Substring(0, i);
            //}
            return Path.Combine(cb, v);
        }

    }

    class TestDir : IDisposable
    {
        public string BasePath { get; }

        public TestDir()
        {
            this.BasePath = System.IO.Path.GetTempPath() + "/" + Guid.NewGuid().ToString("n");
            Directory.CreateDirectory(this.BasePath);
        }


        public void Dispose()
        {
            if (Directory.Exists(BasePath)) Directory.Delete(BasePath, recursive: true);
        }

        public string CreateFile(string path, string content)
        {
            var fullpath = Path.Combine(BasePath, path);
            File.WriteAllText(fullpath, content);

            return fullpath;
        }

        internal string CreateAppSettingsFile(string path, string content) => CreateFile(path, "<appSettings>" + content + "</appSettings>");

    }
}

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

            var i = cb.LastIndexOf("\\bin\\");
            if (i > 0)
            {
                cb = cb.Substring(0, i);
            }
            return Path.Combine(cb, v);
        }

    }
}

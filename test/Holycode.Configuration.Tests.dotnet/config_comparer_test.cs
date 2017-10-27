using Holycode.Configuration.Generator;
using Microsoft.Extensions.Configuration;
using Should;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Holycode.Configuration.Tests.dotnet
{
    /*
      
        1. adding value to one env should raise warnings about missing values in other envs
        2. basic scenario: add config value to single file
        3. advanced scenario: add variable and single/multiple config values based on that variable
        3. advanced scenario: share variables between envs
        
        
        # 'explode' config values from variables
    */

    public class config_comparer_test
    {


        [Fact]
        public void compare_config_different_values()
        {
            var cfg1 = Helpers.LoadXMLConfigString(@"<appSettings><add key=""key1"" value=""val1""/></appSettings>");
            var cfg2 = Helpers.LoadXMLConfigString(@"<appSettings><add key=""key1"" value=""val2""/></appSettings>");

            var diff = new ConfigComparer().Compare(cfg1, cfg2);

            diff.AreEqual.ShouldBeFalse();
            diff.Diff.Count.ShouldEqual(1);
            var d = diff.Diff.First();

            d.Key.ShouldEqual("key1");
            d.LeftValue.ShouldEqual("val1");
            d.RightValue.ShouldEqual("val2");
        }

        [Fact]
        public void compare_config_missing_value()
        {
            var cfg1 = Helpers.LoadXMLConfigString(@"<appSettings><add key=""key1"" value=""val1""/></appSettings>");
            var cfg2 = Helpers.LoadXMLConfigString(@"<appSettings></appSettings>");

            var diff = new ConfigComparer().Compare(cfg1, cfg2);

            diff.AreEqual.ShouldBeFalse();
            diff.Diff.Count.ShouldEqual(1);
            var d = diff.Diff.First();

            d.Key.ShouldEqual("key1");
            d.LeftValue.ShouldEqual("val1");
            d.RightValue.ShouldBeNull();
        }

        [Fact]
        public void compare_config_missing_subkey()
        {
            var cfg1 = Helpers.LoadXMLConfigString(@"<appSettings><add key=""key1.subkey"" value=""val1""/></appSettings>");
            var cfg2 = Helpers.LoadXMLConfigString(@"<appSettings></appSettings>");

            var diff = new ConfigComparer().Compare(cfg1, cfg2);

            diff.AreEqual.ShouldBeFalse();
            diff.Diff.Count.ShouldEqual(1);
            var d = diff.Diff.First();

            d.Key.ShouldEqual("key1:subkey");
            d.LeftValue.ShouldEqual("val1");
            d.RightValue.ShouldBeNull();
        }

        [Fact]
        public void compare_config_files()
        {
            var p1 = Path.GetFullPath(@"input\compare\dev-1.config");
            var p2 = Path.GetFullPath(@"input\compare\dev-2.config");

            var cfg1 = new ConfigurationBuilder().AddXmlAppSettings(p1).Build();
            var cfg2 = new ConfigurationBuilder().AddXmlAppSettings(p2).Build();

            var diff = new ConfigComparer().Compare(cfg1, cfg2);

            diff.AreEqual.ShouldBeTrue();
        }
    }
   

}

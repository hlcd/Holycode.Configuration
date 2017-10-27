using Holycode.Configuration.Generator;
using Should;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Holycode.Configuration.Tests.TestHelpers;

namespace Holycode.Configuration.Tests
{
    [TestClass]
    public class generator_test
    {
        [TestMethod]
        public void generate_config()
        {
            var generator = new ConfigGenerator();

            var cfg = generator.GenerateConfig(GetPath("input/config-gen/config/source/beta-pl/beta-pl.config"), "beta-pl");

            Assert.Inconclusive("don't know how to verify generated config");
        }

        [TestMethod]
        public void process_vars_directive()
        {
            var generator = new ConfigGenerator();

            using (var dir = new TestDir())
            {
                dir.CreateAppSettingsFile("_vars.xml", @"
                    <add key=""key1"" value=""val1"" />
                ");
                var cfgFile = dir.CreateAppSettingsFile("beta.config", @"
                    [[Vars _vars.xml]]
                    <add key=""key2"" value=""{key1}"" />
                ");

                var expected = Helpers.LoadXMLConfigLines(@"
                    <add key=""key2"" value=""val1"" />
                ");

                var r = generator.ProcessFile(cfgFile);
                var processed = Helpers.LoadXMLConfigLines(r);
                var diff = new ConfigComparer().Compare(expected, processed);


                r.ShouldNotContain("[[Vars");
                diff.AreEqual.ShouldBeTrue(diff.ToString());                
            }
        }

        [TestMethod]
        public void process_line_simple()
        {
            var generator = new ConfigGenerator();

            var input = @"<add key=""key1"" value=""val1"" />";
            var expected = @"<add key=""key1"" value=""val1"" />";

            var processed = generator.ProcessLine(input);

            processed.ShouldEqual(expected);

        }

        [TestMethod]
        public void process_line_simple_var()
        {
            var generator = new ConfigGenerator();

            var input = @"
                <add key=""key1"" value=""val1"" />
                <add key=""key2"" value=""{key1}"" />
            ";

            var expected = Helpers.LoadXMLConfigLines(@"
                <add key=""key1"" value=""val1"" />
                <add key=""key2"" value=""val1"" />
            ");

            var r = generator.ProcessContent(input.Split("/r/n"), "xml");
            var processed = Helpers.LoadXMLConfigLines(r);

            var diff = new ConfigComparer().Compare(expected, processed);


            diff.AreEqual.ShouldBeTrue(diff.ToString());            
        }

        [TestMethod]
        public void process_line_hierarchical_var()
        {
            var generator = new ConfigGenerator();

            var input = @"
                <add key=""master.slave1"" value=""slaveval"" />
                <add key=""slave2"" value=""{master.slave1}"" />
            ";

            var expected = Helpers.LoadXMLConfigLines(@"
                <add key=""master.slave1"" value=""slaveval"" />
                <add key=""slave2"" value=""slaveval"" />
            ");

            var r = generator.ProcessContent(input.Split("/r/n"), "xml");
            var processed = Helpers.LoadXMLConfigLines(r);

            var diff = new ConfigComparer().Compare(expected, processed);


            diff.AreEqual.ShouldBeTrue(diff.ToString());
        }

        [TestMethod]
        public void process_line_nested()
        {
            var generator = new ConfigGenerator();

            var input = @"
                <add key=""master.slave1"" value=""slaveval"" />
                <add key=""slave2"" value=""{master.slave1}"" />
                <add key=""slave3"" value=""{slave2}"" />
            ";

            var expected = Helpers.LoadXMLConfigLines(@"
                <add key=""master.slave1"" value=""slaveval"" />
                <add key=""slave2"" value=""slaveval"" />
                <add key=""slave3"" value=""slaveval"" />
            ");

            var r = generator.ProcessContent(input.Split("/r/n"), "xml");
            var processed = Helpers.LoadXMLConfigLines(r);

            var diff = new ConfigComparer().Compare(expected, processed);


            diff.AreEqual.ShouldBeTrue(diff.ToString());
        }


        [TestMethod]
        public void variables_replacement_simple()
        {
            var vars = new Dictionary<string, string>() {
                { "my:config:value", "World" },
                { "somevalue", "val1" }
            };

            ConfigGenerator.ReplaceVariables("hello, {my.config.value}!", vars)
                .ShouldEqual("hello, World!");
            ConfigGenerator.ReplaceVariables("{my.config.value}, hello, {my.config.value}!", vars)
                .ShouldEqual("World, hello, World!");
            ConfigGenerator.ReplaceVariables("{somevalue}, hello, {my.config.value}!", vars)
                .ShouldEqual("val1, hello, World!");
        }

        [TestMethod]
        public void variables_replacement_nested()
        {
            var vars = new Dictionary<string, string>() {
                { "my:config:value", "{somevalue}" },
                { "somevalue", "World" }
            };

            ConfigGenerator.ReplaceVariables("hello, {my.config.value}!", vars)
                .ShouldEqual("hello, World!");
            ConfigGenerator.ReplaceVariables("{my.config.value}, hello, {my.config.value}!", vars)
                .ShouldEqual("World, hello, World!");
            ConfigGenerator.ReplaceVariables("{somevalue}, hello, {my.config.value}!", vars)
                .ShouldEqual("World, hello, World!");
        }
    }
}

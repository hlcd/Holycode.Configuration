using Holycode.Configuration.Generator;
using Should;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Holycode.Configuration.Tests.dotnet
{
    public class generator_test
    {
        //[Fact]
        public void generate_config()
        {
            var generator = new ConfigGenerator();

            var cfg = generator.GenerateConfig("config-gen/config/beta-pl.config", "beta-pl");

            Assert.True(false, "don't know how to verify generated config");
        }

        [Fact]
        public void process_line_simple()
        {
            var generator = new ConfigGenerator();

            var input = @"<add key=""key1"" value=""val1"" />";
            var expected = @"<add key=""key1"" value=""val1"" />";

            var processed = generator.ProcessLine(input);

            processed.ShouldEqual(expected);

        }

        [Fact]
        public void process_line_variable()
        {
            var generator = new ConfigGenerator();

            var input = @"<add key=""key1"" value=""val1"" />
                         <add key=""key2"" value=""{key1}"" />";

            var expected = Helpers.LoadXMLConfigLines(
                        @"<add key=""key1"" value=""val1"" />
                         <add key=""key2"" value=""val1"" />");

            var r = generator.ProcessContent(input.Split("/r/n"), "xml");
            var processed = Helpers.LoadXMLConfigLines(r);

            var diff = new ConfigComparer().Compare(expected, processed);


            diff.AreEqual.ShouldBeTrue(diff.ToString());            
        }

        [Fact]
        public void variables_replacement_simple()
        {
            var vars = new Dictionary<string, string>() {
                { "my.config.value", "World" },
                { "somevalue", "val1" }
            };

            ConfigGenerator.ReplaceVariables("hello, {my.config.value}!", vars)
                .ShouldEqual("hello, World!");
            ConfigGenerator.ReplaceVariables("{my.config.value}, hello, {my.config.value}!", vars)
                .ShouldEqual("World, hello, World!");
            ConfigGenerator.ReplaceVariables("{somevalue}, hello, {my.config.value}!", vars)
                .ShouldEqual("val1, hello, World!");
        }

        [Fact]
        public void variables_replacement_nested()
        {
            var vars = new Dictionary<string, string>() {
                { "my.config.value", "{somevalue}" },
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

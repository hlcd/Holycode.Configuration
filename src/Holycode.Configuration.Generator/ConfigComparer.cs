using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Holycode.Configuration.Generator
{
    public class ConfigValueDifference
    {
        public string Key { get; set; }
        public string LeftValue { get; set; }
        public string RightValue { get; set; }

        public ConfigValueDifference(string key, string left, string right)
        {
            this.Key = key;
            this.LeftValue = left;
            this.RightValue = right;
        }
    }
    public class CompareResult
    {
        public bool AreEqual => Diff?.Count == 0;

        public List<ConfigValueDifference> Diff { get; }

        public CompareResult(List<ConfigValueDifference> diff)
        {
            this.Diff = diff;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach(var d in Diff)
            {
                if (d.RightValue == null) sb.AppendLine($"- {d.Key}={d.LeftValue}");
                else if (d.LeftValue == null) sb.AppendLine($"+ {d.Key}={d.LeftValue}");
                else sb.AppendLine($"- {d.Key}={d.LeftValue} +{d.Key}={d.RightValue}");
            }

            return sb.ToString();
        }
    }
    public class ConfigComparer
    {
        public CompareResult Compare(IConfiguration left, IConfiguration right)
        {
            var diff = new List<ConfigValueDifference>();
            left.Traverse((key, leftVal) =>
            {
                var rightVal = right.Get(key);

                if (rightVal != leftVal)
                {
                    diff.Add(new ConfigValueDifference(key, leftVal, rightVal));
                }                
            });

            right.Traverse((key, rightVal) =>
            {
                var leftVal = left.Get(key);

                if (rightVal != null && leftVal == null)
                {
                    diff.Add(new ConfigValueDifference(key, leftVal, rightVal));
                }
            });

            return new CompareResult(diff);
        }
    }
}

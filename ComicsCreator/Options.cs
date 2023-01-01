using System;
using System.Collections.Generic;
using CommandLine;

namespace ComicsCreator
{
    public class Options
    {
        [Option("comics", HelpText = "Path to the comics folder", Required = true)]
        public IEnumerable<string> ComicsFolder { get; set; } = Array.Empty<string>();

        [Option("data", HelpText = "Path to the data folder", Required = false)]
        public string DataFolder { get; set; } = string.Empty;

        [Option("output", Required = true, HelpText = "Output folder")]
        public string Output { get; set; } = string.Empty;

        [Option("benchmark", HelpText = "Run Benchmark?", Required = false)]
        public bool Benchmark { get; set; } = false;
    }
}

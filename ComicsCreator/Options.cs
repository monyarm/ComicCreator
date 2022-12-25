using System;
using System.Collections.Generic;
using CommandLine;

namespace ComicsCreator
{
    public class Options
    {
        [Option("comics", HelpText = "Path to the comics folder", Required = true)]
        public IEnumerable<string> ComicsFolder { get; set; } = Array.Empty<string>();

        [Option("data", HelpText = "Path to the data folder", Required = true)]
        public string DataFolder { get; set; } = string.Empty;

        [Option("output", Required = true, HelpText = "Output merge folder")]
        public string Output { get; set; } = string.Empty;
    }
}

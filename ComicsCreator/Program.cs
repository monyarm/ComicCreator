using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using CommandLine;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using SixLabors.ImageSharp;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using Slugify;
using System.Diagnostics;
using Noggog;
using BenchmarkDotNet.Running;
using static ComicsCreator.Utils;

namespace ComicsCreator
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(Run);
        }

        private static async Task Run(Options options)
        {
            if(options.Benchmark) {
                _ = BenchmarkRunner.Run<CCBenchmark>();
                return;
            }
            if (Directory.Exists(options.Output))
            {
                Directory.Delete(Path.Join(options.Output, "Materials"), true);
                Directory.Delete(Path.Join(options.Output, "Textures"), true);
                File.Delete(Path.Join(options.Output, "ComicsCreator.esp"));
            }
            Directory.CreateDirectory(options.Output);
            Directory.CreateDirectory(Path.Join(options.Output, "Textures"));
            Directory.CreateDirectory(Path.Join(options.Output, "Materials"));
            Directory.CreateDirectory(Path.Join(options.Output, "Textures", "CustomComics"));
            Directory.CreateDirectory(Path.Join(options.Output, "Materials", "CustomComics"));
            Directory.CreateDirectory(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics"));

            if (options.Game == GameRelease.Fallout4) {
                Fallout4.Parse(options);
            }
            else if (options.Game != GameRelease.Oblivion)
            {
                //Skyrim.Parse(options);
            }

            Directory.Delete(temp, true);
        }
    }
}

using System;
using System.IO;
using System.Text;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using BenchmarkDotNet.Attributes;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Noggog;
using System.Linq;

namespace ComicsCreator
{
    [MemoryDiagnoser]
    public class CCBenchmark
    {
        // public static string GetMD5Hash(string filename)
        // {
        //     using var md5 = System.Security.Cryptography.MD5.Create();
        //     using var stream = File.OpenRead(filename);
        //     var hash = md5.ComputeHash(stream);
        //     return BitConverter.ToString(hash).Replace("-", "");
        // }
        // [Benchmark]
        // public void MD5Hash()
        // {
        //     for (int i = 0; i < 1000; i++)
        //     {
        //         GetMD5Hash("/home/monyarm/Documents/Github/ComicCreator/Comics2/The Liberty Brigade #1.jpg");
        //     }
        // }
        // [Benchmark]
        // public void MurmurHash()
        // {
        //     for (int i = 0; i < 1000; i++)
        //     {
        //         Program.GetHash("/home/monyarm/Documents/Github/ComicCreator/Comics2/The Liberty Brigade #1.jpg");
        //     }
        // }

        // [Benchmark]
        // public void GenerateCoverImageSharp()
        // {
        //     using Image<Rgba32> image = Image<Rgba32>.Load<Rgba32>("/home/monyarm/Documents/Github/ComicCreator/Comics2/The Liberty Brigade #1.jpg");
        //         image.Mutate(x => x
        //              .Resize(image.Height, 476 / 617 * image.Height)
        //              .Resize(new ResizeOptions()
        //              {
        //                  Mode = ResizeMode.Pad,
        //                  Size = new SixLabors.ImageSharp.Size(image.Height),
        //                  PadColor = Color.Transparent,
        //                  Position = AnchorPositionMode.Right
        //              })
        //              .Resize(1024, 1024));

        //         BcEncoder encoder = new();

        //         encoder.OutputOptions.GenerateMipMaps = true;
        //         encoder.OutputOptions.Quality = CompressionQuality.Fast;
        //         encoder.OutputOptions.Format = CompressionFormat.Bc3;
        //         encoder.OutputOptions.FileFormat = OutputFileFormat.Dds;
        //         using MemoryStream stream = new();
        //         encoder.EncodeToStream(image, stream);
        //         stream.Dispose();
        // }
    }
}
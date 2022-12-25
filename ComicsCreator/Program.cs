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

namespace ComicsCreator
{
    public static class Program
    {
        public static string GetMD5Checksum(string filename)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = System.IO.File.OpenRead(filename);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }
        private static readonly string[] imageExtensions = new[] { "*.png", "*.jpg", ".jpeg" };

        private static readonly List<Task> taskList = new();
        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(Run);
        }

        private static readonly SlugHelper helper = new();
        private static readonly string temp = GetTemporaryDirectory();
        private static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
        private static readonly String CoverBGSM = @"{  'sDiffuseTexture': 'replaceMe',  'sNormalTexture': 'Props\\ComicsAndMagazinesHighRes\\ComicNormalSpec_n.DDS',  'sSmoothSpecTexture': 'Props\\ComicsAndMagazinesHighRes\\ComicNormalSpec_s.DDS',  'fRimPower': 2.0,  'fSubsurfaceLightingRolloff': 0.3,  'bSpecularEnabled': true,  'cSpecularColor': '#ffffff',  'fSpecularMult': 1.0,  'sRootMaterialPath': '',  'bCastShadows': true,  'fGrayscaleToPaletteScale': 0.5019608,  'bTileU': true,  'bTileV': true,  'fAlphaTestRef': 127}".Replace("'", "\"");
        private static readonly FormKey MiscMagazine = FormKey.Factory("1BB354:Fallout4.esm");
        private static readonly FormKey FeaturedItem = FormKey.Factory("1B3FAC:Fallout4.esm");
        private static readonly FormKey PerkMagKeyword = FormKey.Factory("1D4A70:Fallout4.esm");

        private static readonly FormKey Container_Loot_Trunk_Prewar_Boss = FormKey.Factory("0192FA:Fallout4.esm");
        private static readonly FormKey Container_Loot_Safe_Prewar = FormKey.Factory("06D4AF:Fallout4.esm");
        private static readonly FormKey Container_Loot_Raider_Boss = FormKey.Factory("06D4B0:Fallout4.esm");

        private static readonly FormKey Container_Loot_Trunk_Prewar = FormKey.Factory("06D4B7:Fallout4.esm");

        private static readonly FormKey Container_Loot_Trunk_Raider_Boss = FormKey.Factory("06D4B8:Fallout4.esm");

        private static readonly FormKey Container_Loot_Trunk = FormKey.Factory("0D6E66:Fallout4.esm");

        private static readonly FormKey Container_Loot_Raider_Safe = FormKey.Factory("1B8812:Fallout4.esm");
        private static readonly FormKey RealComicsSta = FormKey.Factory("001734:RealComics.esp");
        private static readonly ModKey outputModKey = ModKey.FromFileName("ComicsCreator.esp");
        private static readonly Fallout4Mod outputMod = new(outputModKey);
        private static async Task Run(Options options)
        {
            if (Directory.Exists(options.Output)) Directory.Delete(options.Output, true);
            Directory.CreateDirectory(options.Output);
            Directory.CreateDirectory(Path.Join(options.Output, "Textures", "CustomComics"));
            Directory.CreateDirectory(Path.Join(options.Output, "Materials", "CustomComics"));
            Directory.CreateDirectory(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics"));

            outputMod.UsingLocalization = false;
            var env = GameEnvironmentBuilder<IFallout4Mod, IFallout4ModGetter>.Create(GameRelease.Fallout4).
            WithTargetDataFolder(options.DataFolder).
            WithOutputMod(outputMod).Build();
            var LLComicsCreator = new LeveledItem(outputMod.GetNextFormKey())
            {
                Entries = new Noggog.ExtendedList<LeveledItemEntry>(),
                ChanceNone = 95
            };

            var CBZ = options.ComicsFolder.SelectMany(x => Directory.EnumerateFiles(x, "*.cbz"));
            var IMG = options.ComicsFolder.SelectMany(x => imageExtensions.SelectMany(y => Directory.EnumerateFiles(x, y)));
            foreach (var comic in CBZ)
            {
                var item = AddCBZComic(comic, options);
                LLComicsCreator.Entries.Add(new LeveledItemEntry()
                {
                    Data = new LeveledItemEntryData()
                    {
                        Reference = item.ToNullableLink(),
                        Level = 1,
                        Count = 1
                    }
                });
            }
            foreach (var comic in IMG)
            {
                var item = AddIMGComic(comic, options);
                LLComicsCreator.Entries.Add(new LeveledItemEntry()
                {
                    Data = new LeveledItemEntryData()
                    {
                        Reference = item.ToNullableLink(),
                        Level = 1,
                        Count = 1
                    }
                });
            }
            outputMod.LeveledItems.Add(LLComicsCreator);
            var LLComicsEntry = new LeveledItemEntry()
            {
                Data = new LeveledItemEntryData()
                {
                    Reference = LLComicsCreator.ToNullableLink(),
                    Level = 1,
                    Count = 1
                }
            };

            var leveledLists = new List<FormKey>
            {
                Container_Loot_Trunk_Prewar_Boss,
                Container_Loot_Safe_Prewar,
                Container_Loot_Raider_Boss,
                Container_Loot_Trunk_Prewar,
                Container_Loot_Trunk_Raider_Boss,
                Container_Loot_Trunk,
                Container_Loot_Raider_Safe
            };

            Parallel.ForEach(env.LoadOrder.PriorityOrder.WinningOverrideContexts<IFallout4Mod, IFallout4ModGetter, ILeveledItem, ILeveledItemGetter>(env.LinkCache).Where(x => leveledLists.Contains(x.Record.FormKey)).ToList(), x => x.GetOrAddAsOverride(outputMod).Entries!.Add(LLComicsEntry));

            await Task.WhenAll(taskList);

            outputMod.WriteToBinary(Path.Join(options.Output, "ComicsCreator.esp"));

            Directory.Delete(temp, true);
        }
        private static Book GenerateComic(string comic, Options options, bool isCBZ = true)
        {
            var comicName = Path.GetFileNameWithoutExtension(comic);

            Directory.CreateDirectory(Path.Join(options.Output, "Materials", "CustomComics", comicName));
            Directory.CreateDirectory(Path.Join(options.Output, "Textures", "CustomComics", comicName));

            var comicItem = new Book(outputMod.GetNextFormKey());
            comicItem.PreviewTransform.SetTo(MiscMagazine);
            comicItem.Name = comicName;
            comicItem.Model = new Model
            {
                File = @"Props\GrognarComic\Comic_GrognarMar_Prewar.nif"
            };

            var comicMaterialSwap = new MaterialSwap(outputMod.GetNextFormKey())
            {
                EditorID = helper.GenerateSlug(comicName + "_SWAP")
            };
            comicMaterialSwap.Substitutions.Add(new MaterialSubstitution()
            {
                OriginalMaterial = @"props\comicsandmagazines\grognak\grognakaprilprewar.bgsm",
                ReplacementMaterial = $@"CustomComics\{comicName}\Cover.BGSM"
            });
            if (isCBZ)
            {
                comicMaterialSwap.Substitutions.Add(new MaterialSubstitution()
                {
                    OriginalMaterial = @"Props\Grognak\GrognakAprilBackPrewar.BGSM",
                    ReplacementMaterial = $@"CustomComics\{comicName}\BackCover.BGSM"
                });
            }
            outputMod.MaterialSwaps.Add(comicMaterialSwap);
            comicItem.Model.MaterialSwap = comicMaterialSwap.ToNullableLink();
            GenerateBGSM(comicName, options);

            comicItem.Keywords = new Noggog.ExtendedList<IFormLinkGetter<IKeywordGetter>>
            {
                FeaturedItem,
                PerkMagKeyword
            };
            comicItem.Value = 100;
            comicItem.InventoryArt.SetTo(RealComicsSta);

            comicItem.EditorID = helper.GenerateSlug(comicName);
            return comicItem;
        }
        private static Book AddCBZComic(string comic, Options options)
        {
            var comicName = Path.GetFileNameWithoutExtension(comic);
            Book comicItem = GenerateComic(comic, options);
            string description = "";
            var pageCount = HandlePages(comic, comicName, options);
            for (int i = 0; i < pageCount; i++)
            {
                description += $"<img src='img://CustomComics/{comicName}/{i:D3}.dds' height='431' width='415'>\n";
                description += "[pagebreak]\n";
            }
            comicItem.BookText = description;

            outputMod.Books.Add(comicItem);
            return comicItem;
        }
        private static Book AddIMGComic(string comic, Options options)
        {
            var comicName = Path.GetFileNameWithoutExtension(comic);
            Book comicItem = GenerateComic(comic, options, false);
            taskList.Add(Task.Run(() => HandleCover(comic, comic, comicName, options, "Cover_d.dds")));

            outputMod.Books.Add(comicItem);
            return comicItem;
        }

        private static int HandlePages(string comic, string comicName, Options options)
        {
            var pageCount = 0;
            using (ZipArchive zip = ZipFile.Open(comic, ZipArchiveMode.Read))
            {
                var zipEntries = zip.Entries.ToList();
                zipEntries.Sort(Comparer<ZipArchiveEntry>.Create((x, y) =>
                {
                    var xName = int.Parse(Path.GetFileNameWithoutExtension(x.Name));
                    var yName = int.Parse(Path.GetFileNameWithoutExtension(y.Name));
                    return xName > yName ? 1 : xName < yName ? -1 : 0;
                }));

                pageCount = zip.Entries.Count;

                if (!File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", comicName, GetMD5Checksum(comic) + "_0.dds")))
                {
                    ProcessStartInfo pro = new()
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "7z",
                        Arguments = "x \"" + comic + "\" -o\"" + Path.Join(temp, comicName) + "\"",
                        RedirectStandardOutput = true
                    };
                    Process? x = Process.Start(pro);
                    x!.WaitForExit();
                }

                taskList.Add(Task.Run(() => HandleCover(Path.Join(temp, comicName, zipEntries[0].Name), comic, comicName, options, "Cover_d.dds")));
                taskList.Add(Task.Run(() => HandleCover(Path.Join(temp, comicName, zipEntries.Last().Name), comic, comicName, options, "BackCover_d.dds")));

                Parallel.For(0, pageCount, i =>
                {
                    taskList.Add(Task.Run(() =>
                    HandlePage(Path.Join(temp, comicName, zipEntries[i].Name), comic, comicName, options, i)));
                });
            }
            return pageCount;
        }

        private static void HandlePage(string entry, string comic, string comicName, Options options, int i)
        {
            var hash = GetMD5Checksum(comic) + "_" + i;
            var hashFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", comicName, hash + ".dds");
            Directory.CreateDirectory(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", comicName));
            string output = Path.Join(options.Output, "Textures", "CustomComics", comicName, i.ToString("D3") + ".dds");
            if (File.Exists(hashFile))
            {
                File.Copy(hashFile, output);
                return;
            }
            using Image<Rgba32> image = Image<Rgba32>.Load<Rgba32>(entry);
            image.Mutate(x => x
                 .Resize(new ResizeOptions()
                 {
                     Mode = ResizeMode.Pad,
                     Size = new SixLabors.ImageSharp.Size(image.Height),
                     PadColor = Color.Transparent,
                     Position = AnchorPositionMode.Left
                 })
                 .Resize(1024, 1024));
            ConvertToDDS(image, output);
            File.Copy(output, hashFile);
        }

        private static void HandleCover(string coverPage, string comic, string comicName, Options options, string fileName)
        {
            var hash = GetMD5Checksum(comic) + "_" + fileName;
            var hashFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", comicName, hash + ".dds");
            Directory.CreateDirectory(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", comicName));
            string output = Path.Join(options.Output, "Textures", "CustomComics", comicName, fileName);
            if (File.Exists(hashFile))
            {
                File.Copy(hashFile, output);
                return;
            }
            using Image<Rgba32> image = Image<Rgba32>.Load<Rgba32>(coverPage);
            image.Mutate(x => x
                 .Resize(new ResizeOptions()
                 {
                     Mode = ResizeMode.Pad,
                     Size = new SixLabors.ImageSharp.Size(image.Height),
                     PadColor = Color.Black,
                     Position = AnchorPositionMode.Right
                 })
                 .Resize(1024, 1024));

            string path = Path.Join(Path.GetDirectoryName(coverPage), "cover_" + Path.GetFileName(coverPage));
            ConvertToDDS(image, output);
            File.Copy(output, hashFile);
        }

        private static void ConvertToDDS(Image<Rgba32> input, string output)
        {
            // Image<Rgba32> coverPNG = Image.Load<Rgba32>(input);
            BcEncoder encoder = new();

            encoder.OutputOptions.GenerateMipMaps = true;
            encoder.OutputOptions.Quality = CompressionQuality.Balanced;
            encoder.OutputOptions.Format = CompressionFormat.Bc3;
            encoder.OutputOptions.FileFormat = OutputFileFormat.Dds; //Change to Dds for a dds file.
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            using FileStream fs = File.OpenWrite(output);
            encoder.EncodeToStream(input, fs);
        }

        private static void GenerateBGSM(string comicName, Options options)
        {
            var cover = CoverBGSM.Replace("replaceMe", @"CustomComics\\" + comicName + @"\\Cover_d.dds");
            File.WriteAllText(Path.Join(options.Output, "Materials", "CustomComics", comicName, "Cover.BGSM"), cover);
            var backCover = CoverBGSM.Replace("replaceMe", @"CustomComics\\" + comicName + @"\\BackCover_d.dds");
            File.WriteAllText(Path.Join(options.Output, "Materials", "CustomComics", comicName, "BackCover.BGSM"), backCover);
        }
    }
}

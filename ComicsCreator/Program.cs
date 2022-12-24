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
        static List<Task> taskList = new List<Task>();
        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(Run);
        }
        static SlugHelper helper = new SlugHelper();
        static string temp = GetTemporaryDirectory();
        private static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
        private static String CoverBGSM = @"{  'sDiffuseTexture': 'replaceMe',  'sNormalTexture': 'Props\\ComicsAndMagazinesHighRes\\ComicNormalSpec_n.DDS',  'sSmoothSpecTexture': 'Props\\ComicsAndMagazinesHighRes\\ComicNormalSpec_s.DDS',  'fRimPower': 2.0,  'fSubsurfaceLightingRolloff': 0.3,  'bSpecularEnabled': true,  'cSpecularColor': '#ffffff',  'fSpecularMult': 1.0,  'sRootMaterialPath': '',  'bCastShadows': true,  'fGrayscaleToPaletteScale': 0.5019608,  'bTileU': true,  'bTileV': true,  'fAlphaTestRef': 127}".Replace("'", "\"");
        private static FormKey MiscMagazine = FormKey.Factory("1BB354:Fallout4.esm");
        private static FormKey FeaturedItem = FormKey.Factory("1B3FAC:Fallout4.esm");
        private static FormKey PerkMagKeyword = FormKey.Factory("1D4A70:Fallout4.esm");

        private static FormKey Container_Loot_Trunk_Prewar_Boss = FormKey.Factory("0192FA:Fallout4.esm");
        private static FormKey Container_Loot_Safe_Prewar = FormKey.Factory("06D4AF:Fallout4.esm");
        private static FormKey Container_Loot_Raider_Boss = FormKey.Factory("06D4B0:Fallout4.esm");

        private static FormKey Container_Loot_Trunk_Prewar = FormKey.Factory("06D4B7:Fallout4.esm");

        private static FormKey Container_Loot_Trunk_Raider_Boss = FormKey.Factory("06D4B8:Fallout4.esm");

        private static FormKey Container_Loot_Trunk = FormKey.Factory("0D6E66:Fallout4.esm");

        private static FormKey Container_Loot_Raider_Safe = FormKey.Factory("1B8812:Fallout4.esm");
        private static FormKey RealComicsSta = FormKey.Factory("001734:RealComics.esp");


        private static async Task Run(Options options)
        {
            if (Directory.Exists(options.Output)) Directory.Delete(options.Output, true);
            Directory.CreateDirectory(options.Output);
            Directory.CreateDirectory(Path.Join(options.Output, "Textures", "CustomComics"));
            Directory.CreateDirectory(Path.Join(options.Output, "Materials", "CustomComics"));

            var outputModKey = ModKey.FromFileName("ComicsCreator.esp");
            var outputMod = new Fallout4Mod(outputModKey);
            var env = GameEnvironmentBuilder<IFallout4Mod, IFallout4ModGetter>.Create(GameRelease.Fallout4).
            WithTargetDataFolder(options.DataFolder).
            WithOutputMod(outputMod).Build();
            var LLComicsCreator = new LeveledItem(outputMod.GetNextFormKey());

            LLComicsCreator.Entries = new Noggog.ExtendedList<LeveledItemEntry>();
            LLComicsCreator.ChanceNone = 95;

            var CBZ = Directory.EnumerateFiles(options.ComicsFolder, "*.cbz");
            Parallel.ForEach(CBZ, comic => {

                var item = env.AddComic(comic, outputMod, options);
                LLComicsCreator.Entries.Add(new LeveledItemEntry()
                {
                    Data = new LeveledItemEntryData()
                    {
                        Reference = item.ToNullableLink(),
                        Level = 1,
                        Count = 1
                    }
                });
            });
            // foreach (var comic in CBZ)
            // {
            //     var item = env.AddComic(comic, outputMod, options);
            //     LLComicsCreator.Entries.Add(new LeveledItemEntry()
            //     {
            //         Data = new LeveledItemEntryData()
            //         {
            //             Reference = item.ToNullableLink(),
            //             Level = 1,
            //             Count = 1
            //         }
            //     });
            // }
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
        private static IBook AddComic(this IGameEnvironment env, string comic, Fallout4Mod mod, Options options)
        {
            var comicName = Path.GetFileNameWithoutExtension(comic);

            Directory.CreateDirectory(Path.Join(options.Output, "Materials", "CustomComics", comicName));
            Directory.CreateDirectory(Path.Join(options.Output, "Textures", "CustomComics", comicName));

            var comicItem = new Book(mod.GetNextFormKey());
            comicItem.PreviewTransform.SetTo(MiscMagazine);
            comicItem.Name = comicName;
            comicItem.Model = new Model();
            comicItem.Model.File = @"Props\GrognarComic\Comic_GrognarMar_Prewar.nif";

            var comicMaterialSwap = new MaterialSwap(mod.GetNextFormKey());
            comicMaterialSwap.EditorID = helper.GenerateSlug(comicName + "_SWAP");
            comicMaterialSwap.Substitutions.Add(new MaterialSubstitution()
            {
                OriginalMaterial = @"props\comicsandmagazines\grognak\grognakaprilprewar.bgsm",
                ReplacementMaterial = @"CustomComics\" + comicName + @"\Cover.BGSM"
            });
            comicMaterialSwap.Substitutions.Add(new MaterialSubstitution()
            {
                OriginalMaterial = @"Props\Grognak\GrognakAprilBackPrewar.BGSM",
                ReplacementMaterial = @"CustomComics\" + comicName + @"\BackCover.BGSM"
            });
            mod.MaterialSwaps.Add(comicMaterialSwap);
            comicItem.Model.MaterialSwap = comicMaterialSwap.ToNullableLink();
            GenerateBGSM(comic, comicName, options);

            comicItem.Keywords = new Noggog.ExtendedList<IFormLinkGetter<IKeywordGetter>>();
            comicItem.Keywords.Add(FeaturedItem);
            comicItem.Keywords.Add(PerkMagKeyword);

            string description = "";
            var pageCount = HandlePages(comic, comicName, options);
            for (int i = 0; i < pageCount; i++)
            {
                description += "<img src='img://CustomComics/" + comicName + "/" + i.ToString("D3") + ".dds" + "' height='431' width='415'>\n";
                description += "[pagebreak]\n";
            }
            comicItem.BookText = description;

            comicItem.Value = 100;
            comicItem.InventoryArt.SetTo(RealComicsSta);

            comicItem.EditorID = helper.GenerateSlug(comicName);
            mod.Books.Add(comicItem);
            return comicItem;
        }

        private static int HandlePages(string comic, string comicName, Options options)
        {
            var pageCount = 0;
            using (ZipArchive zip = ZipFile.Open(comic, ZipArchiveMode.Read))
            {
                var zipEntries = zip.Entries.ToList();
                zipEntries.Sort(Comparer<ZipArchiveEntry>.Create((x, y) => {
                    var xName = int.Parse(Path.GetFileNameWithoutExtension(x.Name));
                    var yName = int.Parse(Path.GetFileNameWithoutExtension(y.Name));
                    return xName > yName ? 1 : xName < yName ? -1 : 0;
                }));

                pageCount = zip.Entries.Count;

                try
                {
                    ProcessStartInfo pro = new ProcessStartInfo();
                    pro.WindowStyle = ProcessWindowStyle.Hidden;
                    pro.FileName = "7z";
                    pro.Arguments = "x \"" + comic + "\" -o\"" + Path.Join(temp, comicName) + "\"";
                    pro.RedirectStandardOutput = true;
                    Process x = Process.Start(pro);
                    x.WaitForExit();
                }
                catch (System.Exception Ex)
                {
                }
                taskList.Add(Task.Run(() => HandleCover(Path.Join(temp, comicName, zipEntries.First().Name), comic, comicName, options, "Cover_d.dds")));
                taskList.Add(Task.Run(() => HandleCover(Path.Join(temp, comicName, zipEntries.Last().Name), comic, comicName, options, "BackCover_d.dds")));

                Parallel.For(0,pageCount,i => {
                    taskList.Add(Task.Run(() => 
                    HandlePage(Path.Join(temp, comicName, zipEntries[i].Name), comic, comicName, options, i)));
                });
            }
            return pageCount;
        }

        private static void HandlePage(string entry, string comic, string comicName, Options options, int i)
        {
            using (Image image = Image.Load(entry))
            {
                image.Mutate(x => x
                     .Resize(new ResizeOptions()
                     {
                         Mode = ResizeMode.Pad,
                         Size = new SixLabors.ImageSharp.Size(image.Height),
                         PadColor = Color.Transparent,
                         Position = AnchorPositionMode.Left
                     })
                     .Resize(1024, 1024));
                string path = Path.Join(Path.GetDirectoryName(entry), "page_" + Path.GetFileName(entry));
                image.Save(path);
                ConvertToDDS(path, Path.Join(options.Output, "Textures", "CustomComics", comicName, i.ToString("D3") + ".dds"));
            }
        }

        private static void HandleCover(string coverPage, string comic, string comicName, Options options, string fileName)
        {
            using (Image image = Image.Load(coverPage))
            {
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
                image.Save(path);
                ConvertToDDS(path, Path.Join(options.Output, "Textures", "CustomComics", comicName, fileName));

            }
        }

        private static void ConvertToDDS(string input, string output)
        {
            Image<Rgba32> coverPNG = Image.Load<Rgba32>(input);
            BcEncoder encoder = new BcEncoder();

            encoder.OutputOptions.GenerateMipMaps = true;
            encoder.OutputOptions.Quality = CompressionQuality.Balanced;
            encoder.OutputOptions.Format = CompressionFormat.Bc3;
            encoder.OutputOptions.FileFormat = OutputFileFormat.Dds; //Change to Dds for a dds file.
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            using FileStream fs = File.OpenWrite(output);
            encoder.EncodeToStream(coverPNG, fs);
        }

        private static void GenerateBGSM(string comic, string comicName, Options options)
        {
            var cover = CoverBGSM.Replace("replaceMe", @"CustomComics\\" + comicName + @"\\Cover_d.dds");
            File.WriteAllText(Path.Join(options.Output, "Materials", "CustomComics", comicName, "Cover.BGSM"), cover);
            var backCover = CoverBGSM.Replace("replaceMe", @"CustomComics\\" + comicName + @"\\BackCover_d.dds");
            File.WriteAllText(Path.Join(options.Output, "Materials", "CustomComics", comicName, "BackCover.BGSM"), backCover);
        }
    }
}

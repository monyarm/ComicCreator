using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Numerics;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static ComicsCreator.Utils;

namespace ComicsCreator
{
    static partial class Fallout4
    {
        private static List<Action> actionList = new();
        private static readonly string CoverBGSM = @"{  'sDiffuseTexture': 'replaceMe',  'sNormalTexture': 'Props\\ComicsAndMagazinesHighRes\\ComicNormalSpec_n.DDS',  'sSmoothSpecTexture': 'Props\\ComicsAndMagazinesHighRes\\ComicNormalSpec_s.DDS',  'fRimPower': 2.0,  'fSubsurfaceLightingRolloff': 0.3,  'bSpecularEnabled': true,  'cSpecularColor': '#ffffff',  'fSpecularMult': 1.0,  'sRootMaterialPath': '',  'bCastShadows': true,  'fGrayscaleToPaletteScale': 0.5019608,  'bTileU': true,  'bTileV': true,  'fAlphaTestRef': 127}".Replace("'", "\"");
        private static readonly string BOS_SWAP = @"[Forms]\n;ComicBurnt01\n0x132974~Fallout4.esm|replaceMe|NONE|chanceR(10)\n\n;ComicBurnt02\n0x132977~Fallout4.esm|replaceMe|NONE|chanceR(10)\n\n;ComicBurnt03\n0x132979~Fallout4.esm|replaceMe|NONE|chanceR(10)";
        private static readonly FormKey MiscMagazine = FormKey.Factory("1BB354:Fallout4.esm");
        private static readonly FormKey MiscMagazine_Fix = FormKey.Factory("1E2EAD:Fallout4.esm");
        private static readonly FormKey FeaturedItem = FormKey.Factory("1B3FAC:Fallout4.esm");
        private static readonly FormKey PerkMagKeyword = FormKey.Factory("1D4A70:Fallout4.esm");

        private static readonly FormKey Container_Loot_Trunk_Prewar_Boss = FormKey.Factory("0192FA:Fallout4.esm");
        private static readonly FormKey Container_Loot_Safe_Prewar = FormKey.Factory("06D4AF:Fallout4.esm");
        private static readonly FormKey Container_Loot_Raider_Boss = FormKey.Factory("06D4B0:Fallout4.esm");

        private static readonly FormKey Container_Loot_Trunk_Prewar = FormKey.Factory("06D4B7:Fallout4.esm");

        private static readonly FormKey Container_Loot_Trunk_Raider_Boss = FormKey.Factory("06D4B8:Fallout4.esm");

        private static readonly FormKey Container_Loot_Trunk = FormKey.Factory("0D6E66:Fallout4.esm");

        private static readonly FormKey Container_Loot_Raider_Safe = FormKey.Factory("1B8812:Fallout4.esm");
        private static FormKey RealComicsSta = FormKey.Factory("001734:RealComics.esp");
        private static Image<Rgba32> template = Image.Load<Rgba32>(
            File.ReadAllBytes(System.AppContext.BaseDirectory + "/template.png"));
        private static readonly ModKey outputModKey = ModKey.FromFileName("ComicsCreator.esp");
        private static readonly Fallout4Mod outputMod = new(outputModKey, Fallout4Release.Fallout4);

        private static readonly List<FormKey> LLFormKeys = new List<FormKey>
            {
                Container_Loot_Trunk_Prewar_Boss,
                Container_Loot_Safe_Prewar,
                Container_Loot_Raider_Boss,
                Container_Loot_Trunk_Prewar,
                Container_Loot_Trunk_Raider_Boss,
                Container_Loot_Trunk,
                Container_Loot_Raider_Safe
            };

        public static List<String> ComicList { get; private set; } = new();

        public static void Parse(Options options)
        {
            outputMod.UsingLocalization = false;
            var env = options.DataFolder.Length > 0 ? GameEnvironmentBuilder<IFallout4Mod, IFallout4ModGetter>.Create(GameRelease.Fallout4).
            WithTargetDataFolder(options.DataFolder).
            WithOutputMod(outputMod).Build() : GameEnvironmentBuilder<IFallout4Mod, IFallout4ModGetter>.Create(GameRelease.Fallout4).
            WithOutputMod(outputMod).Build();
            var LLComicsCreator = new LeveledItem(outputMod.GetNextFormKey(), Fallout4Release.Fallout4)
            {
                EditorID = "ComicsCreatorLL",
                Entries = new ExtendedList<LeveledItemEntry>(),
                ChanceNone = 95
            };

            var CBZ = options.ComicsFolder.SelectMany(x => Directory.EnumerateFiles(x, "*.cbz", SearchOption.AllDirectories));
            var IMG = options.ComicsFolder.SelectMany(x => imageExtensions.SelectMany(y => Directory.EnumerateFiles(x, y, SearchOption.AllDirectories)));
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
                ComicList.Add(item.EditorID);
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
                ComicList.Add(item.EditorID);
            }

            try
            {
                Parallel.Invoke(actionList.ToArray());
            }
            catch (Exception)
            {
                throw;
            }

            AddToLeveledLists(env, LLComicsCreator, LLFormKeys);
            SplitLeveledList(LLComicsCreator, outputMod);
            GenerateSWAP(options.Output);

            // await Task.WhenAll(taskList);

            outputMod.WriteToBinary(Path.Join(options.Output, "ComicsCreator.esp"));
        }
        private static void AddToLeveledLists(IGameEnvironment<IFallout4Mod, IFallout4ModGetter> env, LeveledItem LLComicsCreator, List<FormKey> leveledLists)
        {
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
            foreach (var ll in env.LoadOrder.PriorityOrder.WinningOverrideContexts<IFallout4Mod, IFallout4ModGetter, ILeveledItem, ILeveledItemGetter>(env.LinkCache).Where(x => leveledLists.Contains(x.Record.FormKey)).ToList())
            {
                ll.GetOrAddAsOverride(outputMod).Entries!.Add(LLComicsEntry);
            }
        }

        private static void SplitLeveledList(LeveledItem LLComicsCreator, IFallout4Mod outputMod)
        {
            if (LLComicsCreator.Entries.Count > 255)
            {
                var entries = new ExtendedList<LeveledItemEntry>();
                var count = LLComicsCreator.Entries.Count;
                for (var n = 0; count > 255; n++)
                {
                    count -= 255;

                    var last = LLComicsCreator.Entries.TakeLast(255);
                    LLComicsCreator.Entries.RemoveRange(count, 255);

                    LeveledItem record = new(outputMod.GetNextFormKey(), Fallout4Release.Fallout4)
                    {
                        EditorID = "ComicsCreatorLL" + n,
                        Entries = new ExtendedList<LeveledItemEntry>(last)
                    };
                    entries.Add(new LeveledItemEntry()
                    {
                        Data = new LeveledItemEntryData()
                        {
                            Reference = record.ToNullableLink(),
                            Level = 1,
                            Count = 1,
                            ChanceNone = new Percent(95)
                        }
                    });
                    outputMod.LeveledItems.Add(record);
                }
                LLComicsCreator.Entries = entries;
            }
        }

        private static Book GenerateComic(string comic, Options options, bool isCBZ = true)
        {
            var comicName = Path.GetFileNameWithoutExtension(comic);
            Console.WriteLine($"Processing {comicName}");
            var slug = helper.GenerateSlug(comicName);

            Directory.CreateDirectory(Path.Join(options.Output, "Materials", "CustomComics", slug));
            Directory.CreateDirectory(Path.Join(options.Output, "Textures", "CustomComics", slug));

            var comicItem = new Book(outputMod.GetNextFormKey(), Fallout4Release.Fallout4);
            comicItem.PreviewTransform.SetTo(MiscMagazine);
            comicItem.Name = comicName;
            comicItem.TextOffsetX = 0;
            comicItem.TextOffsetY = 7;
            comicItem.ObjectBounds = new ObjectBounds()
            {
                First = new P3Int16()
                {
                    X = -9,
                    Y = -7,
                    Z = 0
                },
                Second = new P3Int16()
                {
                    X = 10,
                    Y = 8,
                    Z = 0
                }
            };
            comicItem.Model = new Model
            {
                File = @"Props\GrognakComic\Comic_GrognakFeb.nif"
            };

            var comicMaterialSwap = new MaterialSwap(outputMod.GetNextFormKey(), Fallout4Release.Fallout4)
            {
                EditorID = slug + "_SWAP"
            };
            comicMaterialSwap.Substitutions.Add(new MaterialSubstitution()
            {
                OriginalMaterial = @"Props\ComicsAndMagazines\Grognak\GrognakFeb.bgsm",
                ReplacementMaterial = $@"CustomComics\{slug}\Cover.BGSM"
            });
            if (isCBZ)
            {
                comicMaterialSwap.Substitutions.Add(new MaterialSubstitution()
                {
                    OriginalMaterial = @"Props\ComicsAndMagazines\Backside\ComicBackGreen.BGSM",
                    ReplacementMaterial = $@"CustomComics\{slug}\BackCover.BGSM"
                });
            }
            outputMod.MaterialSwaps.Add(comicMaterialSwap);
            comicItem.Model.MaterialSwap = comicMaterialSwap.ToNullableLink();
            GenerateBGSM(slug, options);

            comicItem.Keywords = new ExtendedList<IFormLinkGetter<IKeywordGetter>>
            {
                FeaturedItem,
                PerkMagKeyword
            };
            comicItem.Value = 100;
            comicItem.InventoryArt.SetTo(RealComicsSta);

            comicItem.EditorID = slug;
            return comicItem;
        }

        private static void GenerateSWAP(string outDir)
        {
            var contents = BOS_SWAP.Replace("replaceMe", string.Join(',', ComicList));
            File.WriteAllText(Path.Join(outDir, "ComicsCreator_SWAP.ini"), contents);
        }

        private static Book AddCBZComic(string comic, Options options)
        {
            var comicName = Path.GetFileNameWithoutExtension(comic);
            var slug = helper.GenerateSlug(comicName);
            Book comicItem = GenerateComic(comic, options);
            string description = "";
            var pageCount = HandlePages(comic, slug, options);
            for (int i = 0; i < pageCount; i++)
            {
                description += $"<img src='img://CustomComics/{slug}/{i:D3}.dds' height='431' width='415'>\n";
                if (i != pageCount - 1)
                    description += "[pagebreak]\n";
            }
            comicItem.BookText = description;

            outputMod.Books.Add(comicItem);
            return comicItem;
        }
        private static Book AddIMGComic(string comic, Options options)
        {
            var comicName = Path.GetFileNameWithoutExtension(comic);
            var slug = helper.GenerateSlug(comicName);
            Book comicItem = GenerateComic(comic, options, false);
            actionList.Add(() => HandleCover(comic, GetHash(comic), slug, options, "Cover_d.dds"));

            outputMod.Books.Add(comicItem);
            return comicItem;
        }

        private static int HandlePages(string comic, string slug, Options options)
        {
            var pageCount = 0;
            using (ZipArchive zip = ZipFile.Open(comic, ZipArchiveMode.Read))
            {

                pageCount = zip.Entries.Count;

                var hash = GetHash(comic);

                if (!File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", slug, hash + "_0.dds")))
                {
                    ProcessStartInfo pro = new()
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "7z",
                        Arguments = "x \"" + comic + "\" -o\"" + Path.Join(temp, slug) + "\"",
                        RedirectStandardOutput = true
                    };
                    Process? x = Process.Start(pro);
                    x!.WaitForExit();
                }
                else
                {
                    for (int i = 0; i < pageCount; i++)
                    {
                        var index = i;
                        actionList.Add(() => CopyFromCache(hash, slug, options, index));
                    }
                    actionList.Add(() => CopyFromCache(hash, slug, options, -1, true, "Cover_d.dds"));
                    actionList.Add(() => CopyFromCache(hash, slug, options, -1, true, "BackCover_d.dds"));
                
                    return pageCount;
                }

                var zipEntries = zip.Entries.ToList();

                zipEntries.Sort(Comparer<ZipArchiveEntry>.Create((x, y) =>
                {
                    var xName = int.Parse(DigitSuffixRegex().Match(Path.GetFileNameWithoutExtension(x.Name)).Value);
                    var yName = int.Parse(DigitSuffixRegex().Match(Path.GetFileNameWithoutExtension(y.Name)).Value);
                    return xName > yName ? 1 : xName < yName ? -1 : 0;
                }));

                actionList.Add(() => HandleCover(Path.Join(temp, slug, zipEntries[0].Name), hash, slug, options, "Cover_d.dds"));
                actionList.Add(() => HandleCover(Path.Join(temp, slug, zipEntries.Last().Name), hash, slug, options, "BackCover_d.dds"));
                for (int i = 0; i < pageCount; i++)
                {
                    var name = zipEntries[i].Name;
                    var entry = Path.Join(temp, slug, name);
                    var index = i;
                    var pageHash = hash;
                    var comicSlug = slug;
                    actionList.Add(() => HandlePage(entry, pageHash, comicSlug, options, index));
                }
            }
            return pageCount;
        }

        private static void CopyFromCache(string pageHash, string comicSlug, Options options, int index, bool isPage = true, string coverName = "")
        {

            var hash = pageHash + "_" + (isPage ? index.ToString() : coverName);
            var hashFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", comicSlug, hash + ".dds");
            string output = Path.Join(options.Output, "Textures", "CustomComics", comicSlug, isPage ? index.ToString("D3") + ".dds" : coverName);
            if (File.Exists(hashFile))
            {
                //File.Copy(hashFile, output);
                File.CreateSymbolicLink(output, hashFile);
                return;
            }
        }

        private static void HandlePage(string entry, string _hash, string slug, Options options, int i)
        {
            var hash = _hash + "_" + i;
            var hashFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", slug, hash + ".dds");
            string output = Path.Join(options.Output, "Textures", "CustomComics", slug, i.ToString("D3") + ".dds");
            CopyFromCache(_hash, slug, options, i);
            Console.WriteLine("Processing Page " + i + " of " + slug);
            Image<Rgba32> image = Image.Load<Rgba32>(entry);
            var power = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(image.Height, image.Width));
            image.Mutate(x => x
             .Resize(new ResizeOptions()
             {
                 Mode = ResizeMode.Stretch,
                 Size = new SixLabors.ImageSharp.Size((int)((730f / 1024f) * image.Height), image.Height)
             })
             .Resize(new ResizeOptions()
             {
                 Mode = ResizeMode.Pad,
                 Size = new SixLabors.ImageSharp.Size(image.Height),
                 PadColor = Color.Black,
                 Position = AnchorPositionMode.Left
             })
             .Resize(power, power));
            ConvertToDDS(image, output);
            File.Copy(output, hashFile);
        }

        private static void HandleCover(string coverPage, string _hash, string slug, Options options, string fileName)
        {
            var hash = _hash + "_" + fileName;
            var hashFile = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", slug, hash + ".dds");
            Directory.CreateDirectory(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomComics", slug));
            string output = Path.Join(options.Output, "Textures", "CustomComics", slug, fileName);
            if (File.Exists(hashFile))
            {
                //File.Copy(hashFile, output);
                File.CreateSymbolicLink(output, hashFile);
                return;
            }
            Console.WriteLine("Processing Cover of " + slug);
            Image<Rgba32> image = Image.Load<Rgba32>(coverPage);
            var power = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(image.Height, image.Width));
            image.Mutate(x => x
             .Resize(new ResizeOptions()
             {
                 Mode = ResizeMode.Stretch,
                 Size = new SixLabors.ImageSharp.Size((int)((1670f / 2048f * image.Height)), image.Height)
             })
             .Resize(new ResizeOptions()
             {
                 Mode = ResizeMode.Pad,
                 Size = new SixLabors.ImageSharp.Size(image.Height),
                 PadColor = Color.White,
                 Position = AnchorPositionMode.Right
             })
             .Resize(power, power)
             .DrawImage(template, 1)
             );
            string path = Path.Join(Path.GetDirectoryName(coverPage), "Cover_" + Path.GetFileName(coverPage));
            ConvertToDDS(image, output);
            File.Copy(output, hashFile);
        }

        private static void ConvertToDDS(Image<Rgba32> input, string output)
        {
            // Image<Rgba32> coverPNG = Image.Load<Rgba32>(input);
            BcEncoder encoder = new();

            encoder.OutputOptions.GenerateMipMaps = false;
            encoder.OutputOptions.Quality = CompressionQuality.Balanced;
            encoder.OutputOptions.Format = CompressionFormat.Bc1;
            encoder.OutputOptions.FileFormat = OutputFileFormat.Dds; //Change to Dds for a dds file.
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            using FileStream fs = File.OpenWrite(output);
            encoder.EncodeToStream(input, fs);
        }

        private static void GenerateBGSM(string slug, Options options)
        {
            var cover = CoverBGSM.Replace("replaceMe", @"CustomComics\\" + slug + @"\\Cover_d.dds");
            File.WriteAllText(Path.Join(options.Output, "Materials", "CustomComics", slug, "Cover.BGSM"), cover);
            var backCover = CoverBGSM.Replace("replaceMe", @"CustomComics\\" + slug + @"\\BackCover_d.dds");
            File.WriteAllText(Path.Join(options.Output, "Materials", "CustomComics", slug, "BackCover.BGSM"), backCover);
        }

        [GeneratedRegex("\\d*$")]
        private static partial Regex DigitSuffixRegex();
    }
}
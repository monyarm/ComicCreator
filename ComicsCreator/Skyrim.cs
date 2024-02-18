using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static ComicsCreator.Utils;
using VersOne.Epub;
using Mutagen.Bethesda.Strings;
using System.Xml;
using System.Xml.Linq;

namespace ComicsCreator
{
    static class Skyrim
    {
        public static readonly List<Action> actionList = new();
        private static readonly ModKey outputModKey = ModKey.FromFileName("ComicsCreator.esp");
        private static readonly SkyrimMod outputMod = new(outputModKey, SkyrimRelease.SkyrimSE);

        private static readonly List<FormKey> LLFormKeys = new()
        {
        };

        public static void Parse(Options options)
        {
            outputMod.UsingLocalization = false;
            var env = options.DataFolder.Length > 0 ? GameEnvironmentBuilder<ISkyrimMod, ISkyrimModGetter>.Create(GameRelease.SkyrimSE).
            WithTargetDataFolder(options.DataFolder).
            WithOutputMod(outputMod).Build() : GameEnvironmentBuilder<ISkyrimMod, ISkyrimModGetter>.Create(GameRelease.SkyrimSE).
            WithOutputMod(outputMod).Build();
            var LLComicsCreator = new LeveledItem(outputMod.GetNextFormKey(), SkyrimRelease.SkyrimSE)
            {
                EditorID = "ComicsCreatorLL",
                Entries = new Noggog.ExtendedList<LeveledItemEntry>(),
                ChanceNone = 95
            };

            var EPUB = options.ComicsFolder.SelectMany(x => Directory.EnumerateFiles(x, "*.epub"));
            foreach (var book in EPUB)
            {
                var item = AddEPUBBook(book, options);
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
            AddToLeveledLists(env, LLComicsCreator, LLFormKeys);
            SplitLeveledList(LLComicsCreator, outputMod);

            // await Task.WhenAll(taskList);
            Parallel.Invoke(actionList.ToArray());

            outputMod.WriteToBinary(Path.Join(options.Output, "ComicsCreator.esp"));
        }

        private static Book AddEPUBBook(string book, Options options)
        {
            var epubBook = EpubReader.ReadBook(book);
            var bookName = epubBook.Title;
            Console.WriteLine($"Processing {bookName}");
            var slug = helper.GenerateSlug(bookName);

            Directory.CreateDirectory(Path.Join(options.Output, "Materials", "CustomComics", slug));
            Directory.CreateDirectory(Path.Join(options.Output, "Textures", "CustomComics", slug));

            var bookItem = new Book(outputMod.GetNextFormKey(), SkyrimRelease.SkyrimSE);
            bookItem.Name = bookName;
            bookItem.EditorID = slug;
            bookItem.Description = epubBook.Description;
            bookItem.BookText = ParseContent(epubBook);

            return bookItem;
        }

        private static string ParseContent(EpubBook epubBook)
        {
            var content = "";
            foreach (var item in epubBook.Content.Html.Local)
            {
                XDocument doc = XDocument.Parse(item.Content.ToString());
                foreach (var element in doc.Descendants())
                {
                    element.ReplaceWith(TransformNode(element));
                }
                content += doc.ToString() + "\n[pagebreak]\n";
            }
            return content;
        }

        private static XNode TransformNode(XElement node)
        {
            switch (node.Name.LocalName)
            {
                case "b":
                case "i":
                case "u":
                case "br":
                    // Fully Supported
                    break;
                case "a":
                case "body":
                case "head":
                case "html":
                case "link":
                case "meta":
                case "title":
                case "blockquote":
                    // return new XText(node.Value);
                    break;
                case "em":
                    node.Name = "i";
                    break;
                case "dl":
                    node.Name = "ul";
                    break;
                case "dt":
                    node.Name = "li";
                    break;
                case "dd":
                    node.Name = "li";
                    node.Value = "    " + node.Value;
                    break;
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                    // TODO: Font Size tags (h1/h2/h3/..) are not currently supported, and will be ignored.
                    break;
                case "p":
                case "div":
                    node.Attributes().ForEach(
                        x => {
                            switch (x.Name.LocalName)
                            {
                                case "align":
                                    // Do nothing, attribute is fully supported by skyrim.
                                    break;
                                case "class":
                                    // TODO: Read CSS and grab necessary data (font, color, size, italig, boldd, underline, etc.)
                                    break;
                                default:
                                    Console.WriteLine($"Attribute {x.Name.LocalName} is not supported (Will be Ignored)");
                                    goto case "_remove";
                                case "_remove":
                                case "id":
                                    node.Attribute(node.GetAttribute(x.Name.LocalName)?? "")?.Remove();
                                    break;
                            }
                        } 
                    );
                    break;
                default:
                    Console.WriteLine($"Element {node.Name.LocalName} is not supported");
                    // throw new NotImplementedException($"Element {node.Name.LocalName} is not supported");
                    break;
            }
            return node;
        }

        private static void AddToLeveledLists(IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env, LeveledItem LLComicsCreator, List<FormKey> leveledLists)
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
            foreach (var ll in env.LoadOrder.PriorityOrder.WinningOverrideContexts<ISkyrimMod, ISkyrimModGetter, ILeveledItem, ILeveledItemGetter>(env.LinkCache).Where(x => leveledLists.Contains(x.Record.FormKey)).ToList())
            {
                ll.GetOrAddAsOverride(outputMod).Entries!.Add(LLComicsEntry);
            }
        }

        private static void SplitLeveledList(LeveledItem LLComicsCreator, ISkyrimMod outputMod)
        {
            if (LLComicsCreator.Entries.Count > 255)
            {
                var entries = new Noggog.ExtendedList<LeveledItemEntry>();
                var count = LLComicsCreator.Entries.Count;
                for (var n = 0; count > 255; n++)
                {
                    count -= 255;

                    var last = LLComicsCreator.Entries.TakeLast(255);
                    LLComicsCreator.Entries.RemoveRange(count, 255);

                    LeveledItem record = new(outputMod.GetNextFormKey(), SkyrimRelease.SkyrimSE)
                    {
                        EditorID = "ComicsCreatorLL" + n,
                        Entries = new Noggog.ExtendedList<LeveledItemEntry>(last)
                    };
                    entries.Add(new LeveledItemEntry()
                    {
                        Data = new LeveledItemEntryData()
                        {
                            Reference = record.ToNullableLink(),
                            Level = 1,
                            Count = 1
                        }
                    });
                    outputMod.LeveledItems.Add(record);
                }
                LLComicsCreator.Entries = entries;
            }
        }
    }
}
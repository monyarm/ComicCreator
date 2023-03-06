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

namespace ComicsCreator {
    static class Skyrim {
        public static readonly List<Action> actionList = new();
        private static readonly ModKey outputModKey = ModKey.FromFileName("ComicsCreator.esp");
        private static readonly SkyrimMod outputMod = new(outputModKey,SkyrimRelease.SkyrimSE);

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
            foreach (var comic in EPUB)
            {
                // var item = AddCBZComic(comic, options);
                // LLComicsCreator.Entries.Add(new LeveledItemEntry()
                // {
                //     Data = new LeveledItemEntryData()
                //     {
                //         Reference = item.ToNullableLink(),
                //         Level = 1,
                //         Count = 1
                //     }
                // });
                
            }
            // AddToLeveledLists(env, LLComicsCreator, LLFormKeys);
            // SplitLeveledList(LLComicsCreator, outputMod);

            // await Task.WhenAll(taskList);
            Parallel.Invoke( actionList.ToArray());

            outputMod.WriteToBinary(Path.Join(options.Output, "ComicsCreator.esp"));
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
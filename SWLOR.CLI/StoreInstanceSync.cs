using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SWLOR.CLI
{
    internal class StoreInstanceSync
    {
        private static readonly HashSet<string> StoreBlueprintOnlyKeys = new()
        {
            "__data_type",
            "Comment",
            "ID",
        };

        private static readonly HashSet<string> ItemBlueprintOnlyKeys = new()
        {
            "__data_type",
            "Comment",
            "PaletteID",
        };

        private static readonly HashSet<string> StoreInstanceKeys = new()
        {
            "__struct_id",
            "XOrientation",
            "XPosition",
            "YOrientation",
            "YPosition",
            "ZPosition",
        };

        private static readonly HashSet<string> ItemInstanceKeys = new()
        {
            "__struct_id",
            "Infinite",
            "Repos_PosX",
            "Repos_Posy",
            "XOrientation",
            "XPosition",
            "YOrientation",
            "YPosition",
            "ZPosition",
        };

        private static readonly JObject DefaultInfinite = Field("byte", 1);
        private static readonly JObject DefaultRepoPosition = Field("word", 0);
        private static readonly JObject DefaultXOrientation = Field("float", 0.0);
        private static readonly JObject DefaultXPosition = Field("float", -1.0);
        private static readonly JObject DefaultYOrientation = Field("float", 1.0);
        private static readonly JObject DefaultYPosition = Field("float", -1.0);
        private static readonly JObject DefaultZPosition = Field("float", -1.0);

        public bool Process(string moduleRoot, bool dryRun, bool createMissingStoreBlueprints = false)
        {
            if (string.IsNullOrWhiteSpace(moduleRoot))
                moduleRoot = "./Module";

            moduleRoot = Path.GetFullPath(moduleRoot);
            var gitPath = Path.Combine(moduleRoot, "git");
            var utiPath = Path.Combine(moduleRoot, "uti");
            var utmPath = Path.Combine(moduleRoot, "utm");

            if (!Directory.Exists(gitPath) || !Directory.Exists(utiPath) || !Directory.Exists(utmPath))
            {
                throw new DirectoryNotFoundException(
                    $"Module root '{moduleRoot}' must contain git, uti, and utm directories.");
            }

            Console.WriteLine(createMissingStoreBlueprints
                ? "Creating missing store blueprints..."
                : dryRun
                    ? "Checking placed store instances..."
                    : "Syncing placed store instances...");
            Console.WriteLine($"Module root: {moduleRoot}");

            var itemBlueprints = LoadItemBlueprints(utiPath);
            var storeBlueprints = LoadStoreBlueprints(utmPath);
            var createdStoreBlueprints = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            var report = new StoreInstanceSyncReport();
            var changedFiles = new List<string>();
            var createdBlueprintFiles = new List<string>();

            foreach (var gitFile in Directory.GetFiles(gitPath, "*.git.json").OrderBy(x => x))
            {
                var git = ReadJson(gitFile);
                var stores = GetArray(git, "StoreList");
                var fileChanged = false;

                for (var storeIndex = 0; storeIndex < stores.Count; storeIndex++)
                {
                    if (stores[storeIndex] is not JObject placedStore)
                        continue;

                    report.TotalStores++;
                    var storeResRef = GetFieldValue(placedStore, "ResRef");
                    if (!string.IsNullOrWhiteSpace(storeResRef) &&
                        storeBlueprints.TryGetValue(storeResRef, out var storeBlueprint))
                    {
                        report.SourceBackedStores++;
                        var expectedStore = BuildExpectedStore(placedStore, storeBlueprint, itemBlueprints, report, gitFile);

                        if (!JToken.DeepEquals(placedStore, expectedStore))
                        {
                            report.OutOfDateStores++;
                            AddExample(report.OutOfDateStoreExamples, gitFile, storeResRef, storeIndex);

                            if (!dryRun)
                            {
                                stores[storeIndex] = expectedStore;
                                fileChanged = true;
                            }
                        }
                    }
                    else
                    {
                        report.StoresMissingBlueprint++;
                        AddExample(report.MissingStoreExamples, gitFile, storeResRef, storeIndex);

                        if (createMissingStoreBlueprints && !string.IsNullOrWhiteSpace(storeResRef))
                        {
                            var generatedStoreBlueprint = BuildStoreBlueprintFromPlacedStore(placedStore);
                            if (createdStoreBlueprints.TryGetValue(storeResRef, out var existingGeneratedStoreBlueprint))
                            {
                                if (!JToken.DeepEquals(existingGeneratedStoreBlueprint, generatedStoreBlueprint))
                                {
                                    throw new InvalidDataException(
                                        $"Missing store blueprint '{storeResRef}' has multiple placed instances that do not match. " +
                                        "Create separate resrefs or choose a canonical store inventory first.");
                                }
                            }
                            else
                            {
                                var blueprintFile = Path.Combine(utmPath, $"{storeResRef}.utm.json");
                                if (File.Exists(blueprintFile))
                                    throw new IOException($"Cannot create '{blueprintFile}' because a file already exists.");

                                WriteJson(blueprintFile, generatedStoreBlueprint);
                                createdStoreBlueprints[storeResRef] = generatedStoreBlueprint;
                                createdBlueprintFiles.Add(blueprintFile);
                            }
                        }

                        if (SyncItemsOnly(placedStore, itemBlueprints, report, gitFile, dryRun))
                            fileChanged = true;
                    }
                }

                if (fileChanged)
                {
                    WriteJson(gitFile, git);
                    changedFiles.Add(gitFile);
                }
            }

            PrintReport(report, changedFiles, createdBlueprintFiles, dryRun, createMissingStoreBlueprints);
            return report.OutOfDateStores > 0 || report.OutOfDateItems > 0;
        }

        private static Dictionary<string, JObject> LoadItemBlueprints(string utiPath)
        {
            var results = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(utiPath, "*.uti.json").OrderBy(x => x))
            {
                var item = ReadJson(file);
                var resRef = GetFieldValue(item, "TemplateResRef");
                if (string.IsNullOrWhiteSpace(resRef))
                    resRef = GetFieldValue(item, "Tag");
                if (string.IsNullOrWhiteSpace(resRef))
                    resRef = Path.GetFileName(file).Replace(".uti.json", string.Empty);

                results[resRef] = item;
            }

            return results;
        }

        private static Dictionary<string, JObject> LoadStoreBlueprints(string utmPath)
        {
            var results = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(utmPath, "*.utm.json").OrderBy(x => x))
            {
                var store = ReadJson(file);
                var resRef = GetFieldValue(store, "ResRef");
                if (string.IsNullOrWhiteSpace(resRef))
                    resRef = Path.GetFileName(file).Replace(".utm.json", string.Empty);

                results[resRef] = store;
            }

            return results;
        }

        private static JObject BuildExpectedStore(
            JObject placedStore,
            JObject storeBlueprint,
            IReadOnlyDictionary<string, JObject> itemBlueprints,
            StoreInstanceSyncReport report,
            string gitFile)
        {
            var expectedStore = new JObject();
            expectedStore["__struct_id"] = Clone(placedStore["__struct_id"] ?? new JValue(0));

            foreach (var property in storeBlueprint.Properties())
            {
                if (StoreBlueprintOnlyKeys.Contains(property.Name))
                    continue;

                expectedStore[property.Name] = property.Name == "StoreList"
                    ? BuildExpectedStoreList(placedStore, storeBlueprint, itemBlueprints, report, gitFile)
                    : Clone(property.Value);
            }

            foreach (var key in StoreInstanceKeys.Where(x => x != "__struct_id"))
            {
                if (placedStore.TryGetValue(key, out var value))
                    expectedStore[key] = Clone(value);
            }

            return expectedStore;
        }

        private static JObject BuildExpectedStoreList(
            JObject placedStore,
            JObject storeBlueprint,
            IReadOnlyDictionary<string, JObject> itemBlueprints,
            StoreInstanceSyncReport report,
            string gitFile)
        {
            var sourcePanes = GetArray(storeBlueprint, "StoreList");
            var expectedPanes = new JArray();
            var existingItems = BuildExistingItemLookup(placedStore);

            foreach (var sourcePaneToken in sourcePanes)
            {
                if (sourcePaneToken is not JObject sourcePane)
                    continue;

                var expectedPane = new JObject
                {
                    ["__struct_id"] = Clone(sourcePane["__struct_id"] ?? new JValue(0)),
                };

                var sourceItems = sourcePane["ItemList"]?["value"] as JArray;
                if (sourceItems != null)
                {
                    var expectedItems = new JArray();
                    foreach (var sourceItemToken in sourceItems)
                    {
                        if (sourceItemToken is not JObject sourceItem)
                            continue;

                        var itemResRef = GetFieldValue(sourceItem, "InventoryRes");
                        var existingItem = TakeExistingItem(existingItems, itemResRef);

                        if (itemBlueprints.TryGetValue(itemResRef, out var itemBlueprint))
                        {
                            var expectedItem = BuildExpectedItem(itemBlueprint, sourceItem, existingItem);
                            expectedItems.Add(expectedItem);
                            report.SourceBackedItems++;

                            if (existingItem != null && !JToken.DeepEquals(existingItem, expectedItem))
                            {
                                report.OutOfDateItems++;
                                AddExample(report.OutOfDateItemExamples, gitFile, itemResRef, null);
                            }
                        }
                        else if (existingItem != null)
                        {
                            var preservedItem = PreserveExistingItemWithSourceSlot(existingItem, sourceItem);
                            expectedItems.Add(preservedItem);
                            report.ItemsMissingBlueprint++;
                            AddExample(report.MissingItemExamples, gitFile, itemResRef, null);
                        }
                        else
                        {
                            report.ItemsMissingBlueprint++;
                            AddExample(report.MissingItemExamples, gitFile, itemResRef, null);
                        }
                    }

                    expectedPane["ItemList"] = new JObject
                    {
                        ["type"] = "list",
                        ["value"] = expectedItems,
                    };
                }

                expectedPanes.Add(expectedPane);
            }

            return new JObject
            {
                ["type"] = "list",
                ["value"] = expectedPanes,
            };
        }

        private static JObject BuildStoreBlueprintFromPlacedStore(JObject placedStore)
        {
            var storeBlueprint = new JObject
            {
                ["__data_type"] = "UTM ",
            };
            var insertedBlueprintMetadata = false;

            foreach (var property in placedStore.Properties())
            {
                if (StoreInstanceKeys.Contains(property.Name))
                    continue;

                if (!insertedBlueprintMetadata && property.Name == "IdentifyPrice")
                {
                    storeBlueprint["Comment"] = Field("cexostring", string.Empty);
                    storeBlueprint["ID"] = Field("byte", 5);
                    insertedBlueprintMetadata = true;
                }

                storeBlueprint[property.Name] = property.Name == "StoreList"
                    ? BuildStoreBlueprintListFromPlacedStore(placedStore)
                    : Clone(property.Value);
            }

            if (!insertedBlueprintMetadata)
            {
                storeBlueprint["Comment"] = Field("cexostring", string.Empty);
                storeBlueprint["ID"] = Field("byte", 5);
            }

            return storeBlueprint;
        }

        private static JObject BuildStoreBlueprintListFromPlacedStore(JObject placedStore)
        {
            var blueprintPanes = new JArray();

            foreach (var placedPaneToken in GetArray(placedStore, "StoreList"))
            {
                if (placedPaneToken is not JObject placedPane)
                    continue;

                var blueprintPane = new JObject
                {
                    ["__struct_id"] = Clone(placedPane["__struct_id"] ?? new JValue(0)),
                };

                var placedItems = placedPane["ItemList"]?["value"] as JArray;
                if (placedItems != null)
                {
                    var blueprintItems = new JArray();
                    foreach (var placedItemToken in placedItems)
                    {
                        if (placedItemToken is not JObject placedItem)
                            continue;

                        var itemResRef = GetItemResRef(placedItem);
                        if (string.IsNullOrWhiteSpace(itemResRef))
                            itemResRef = GetFieldValue(placedItem, "InventoryRes");

                        blueprintItems.Add(new JObject
                        {
                            ["__struct_id"] = Clone(placedItem["__struct_id"] ?? new JValue(0)),
                            ["Infinite"] = Clone(placedItem["Infinite"] ?? DefaultInfinite),
                            ["InventoryRes"] = Field("resref", itemResRef),
                            ["Repos_PosX"] = Clone(placedItem["Repos_PosX"] ?? DefaultRepoPosition),
                            ["Repos_Posy"] = Clone(placedItem["Repos_Posy"] ?? DefaultRepoPosition),
                        });
                    }

                    blueprintPane["ItemList"] = new JObject
                    {
                        ["type"] = "list",
                        ["value"] = blueprintItems,
                    };
                }

                blueprintPanes.Add(blueprintPane);
            }

            return new JObject
            {
                ["type"] = "list",
                ["value"] = blueprintPanes,
            };
        }

        private static bool SyncItemsOnly(
            JObject placedStore,
            IReadOnlyDictionary<string, JObject> itemBlueprints,
            StoreInstanceSyncReport report,
            string gitFile,
            bool dryRun)
        {
            var changed = false;

            foreach (var item in EnumeratePlacedItems(placedStore))
            {
                report.ItemsInStoresWithoutStoreBlueprint++;
                var itemResRef = GetItemResRef(item);
                if (!itemBlueprints.TryGetValue(itemResRef, out var itemBlueprint))
                {
                    report.ItemsMissingBlueprint++;
                    AddExample(report.MissingItemExamples, gitFile, itemResRef, null);
                    continue;
                }

                report.SourceBackedItems++;
                var expectedItem = BuildExpectedItem(itemBlueprint, null, item);
                if (!JToken.DeepEquals(item, expectedItem))
                {
                    report.OutOfDateItems++;
                    AddExample(report.OutOfDateItemExamples, gitFile, itemResRef, null);

                    if (!dryRun)
                    {
                        item.Replace(expectedItem);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static JObject BuildExpectedItem(
            JObject itemBlueprint,
            JObject sourceStoreItem,
            JObject existingItem)
        {
            var metadata = BuildItemMetadata(sourceStoreItem, existingItem);
            var expectedItem = new JObject
            {
                ["__struct_id"] = metadata.StructId,
            };
            var insertedInfinite = false;
            var insertedRepositoryPosition = false;

            foreach (var property in itemBlueprint.Properties())
            {
                if (ItemBlueprintOnlyKeys.Contains(property.Name))
                    continue;

                if (!insertedInfinite && property.Name == "LocalizedName")
                {
                    expectedItem["Infinite"] = Clone(metadata.Infinite);
                    insertedInfinite = true;
                }

                if (!insertedRepositoryPosition && property.Name == "StackSize")
                {
                    expectedItem["Repos_PosX"] = Clone(metadata.ReposPosX);
                    expectedItem["Repos_Posy"] = Clone(metadata.ReposPosY);
                    insertedRepositoryPosition = true;
                }

                expectedItem[property.Name] = Clone(property.Value);

                if (!insertedInfinite && property.Name == "Identified")
                {
                    expectedItem["Infinite"] = Clone(metadata.Infinite);
                    insertedInfinite = true;
                }

                if (!insertedRepositoryPosition && property.Name == "PropertiesList")
                {
                    expectedItem["Repos_PosX"] = Clone(metadata.ReposPosX);
                    expectedItem["Repos_Posy"] = Clone(metadata.ReposPosY);
                    insertedRepositoryPosition = true;
                }
            }

            if (!insertedInfinite)
                expectedItem["Infinite"] = Clone(metadata.Infinite);
            if (!insertedRepositoryPosition)
            {
                expectedItem["Repos_PosX"] = Clone(metadata.ReposPosX);
                expectedItem["Repos_Posy"] = Clone(metadata.ReposPosY);
            }

            expectedItem["XOrientation"] = Clone(metadata.XOrientation);
            expectedItem["XPosition"] = Clone(metadata.XPosition);
            expectedItem["YOrientation"] = Clone(metadata.YOrientation);
            expectedItem["YPosition"] = Clone(metadata.YPosition);
            expectedItem["ZPosition"] = Clone(metadata.ZPosition);

            return expectedItem;
        }

        private static JObject PreserveExistingItemWithSourceSlot(JObject existingItem, JObject sourceStoreItem)
        {
            var metadata = BuildItemMetadata(sourceStoreItem, existingItem);
            var preserved = (JObject)existingItem.DeepClone();

            preserved["__struct_id"] = metadata.StructId;
            preserved["Infinite"] = Clone(metadata.Infinite);
            preserved["Repos_PosX"] = Clone(metadata.ReposPosX);
            preserved["Repos_Posy"] = Clone(metadata.ReposPosY);

            return preserved;
        }

        private static ItemInstanceMetadata BuildItemMetadata(JObject sourceStoreItem, JObject existingItem)
        {
            return new ItemInstanceMetadata
            {
                StructId = Clone(sourceStoreItem?["__struct_id"] ?? existingItem?["__struct_id"] ?? new JValue(0)),
                Infinite = Clone(sourceStoreItem?["Infinite"] ?? existingItem?["Infinite"] ?? DefaultInfinite),
                ReposPosX = Clone(sourceStoreItem?["Repos_PosX"] ?? existingItem?["Repos_PosX"] ?? DefaultRepoPosition),
                ReposPosY = Clone(sourceStoreItem?["Repos_Posy"] ?? existingItem?["Repos_Posy"] ?? DefaultRepoPosition),
                XOrientation = Clone(existingItem?["XOrientation"] ?? DefaultXOrientation),
                XPosition = Clone(existingItem?["XPosition"] ?? DefaultXPosition),
                YOrientation = Clone(existingItem?["YOrientation"] ?? DefaultYOrientation),
                YPosition = Clone(existingItem?["YPosition"] ?? DefaultYPosition),
                ZPosition = Clone(existingItem?["ZPosition"] ?? DefaultZPosition),
            };
        }

        private static Dictionary<string, Queue<JObject>> BuildExistingItemLookup(JObject store)
        {
            var results = new Dictionary<string, Queue<JObject>>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in EnumeratePlacedItems(store))
            {
                var resRef = GetItemResRef(item);
                if (string.IsNullOrWhiteSpace(resRef))
                    continue;

                if (!results.TryGetValue(resRef, out var queue))
                {
                    queue = new Queue<JObject>();
                    results[resRef] = queue;
                }

                queue.Enqueue(item);
            }

            return results;
        }

        private static JObject TakeExistingItem(Dictionary<string, Queue<JObject>> lookup, string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return null;

            return lookup.TryGetValue(resRef, out var queue) && queue.Count > 0
                ? queue.Dequeue()
                : null;
        }

        private static IEnumerable<JObject> EnumeratePlacedItems(JObject store)
        {
            foreach (var pane in GetArray(store, "StoreList").OfType<JObject>())
            {
                var items = pane["ItemList"]?["value"] as JArray;
                if (items == null)
                    continue;

                foreach (var item in items.OfType<JObject>())
                    yield return item;
            }
        }

        private static JArray GetArray(JObject obj, string propertyName)
        {
            return obj[propertyName]?["value"] as JArray ?? new JArray();
        }

        private static string GetItemResRef(JObject item)
        {
            var resRef = GetFieldValue(item, "TemplateResRef");
            if (string.IsNullOrWhiteSpace(resRef))
                resRef = GetFieldValue(item, "Tag");

            return resRef;
        }

        private static string GetFieldValue(JObject obj, string fieldName)
        {
            return obj[fieldName]?["value"]?.ToString() ?? string.Empty;
        }

        private static JObject ReadJson(string path)
        {
            return JObject.Parse(ReadText(path));
        }

        private static string ReadText(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length >= 3 &&
                bytes[0] == 0xEF &&
                bytes[1] == 0xBB &&
                bytes[2] == 0xBF)
            {
                bytes = bytes.Skip(3).ToArray();
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.Latin1.GetString(bytes);
            }
        }

        private static void WriteJson(string path, JObject json)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false))
            {
                NewLine = "\n",
            };
            using var jsonWriter = new JsonTextWriter(writer)
            {
                Formatting = Formatting.Indented,
                Indentation = 2,
            };

            json.WriteTo(jsonWriter);
            writer.WriteLine();
        }

        private static JToken Clone(JToken token)
        {
            return token?.DeepClone();
        }

        private static JObject Field(string type, object value)
        {
            return new JObject
            {
                ["type"] = type,
                ["value"] = JToken.FromObject(value),
            };
        }

        private static void AddExample(List<string> examples, string gitFile, string resRef, int? storeIndex)
        {
            if (examples.Count >= 25)
                return;

            var fileName = Path.GetFileName(gitFile);
            var label = string.IsNullOrWhiteSpace(resRef) ? "<missing resref>" : resRef;
            examples.Add(storeIndex.HasValue
                ? $"{fileName} store[{storeIndex.Value}] {label}"
                : $"{fileName} {label}");
        }

        private static void PrintReport(
            StoreInstanceSyncReport report,
            List<string> changedFiles,
            List<string> createdBlueprintFiles,
            bool dryRun,
            bool createMissingStoreBlueprints)
        {
            Console.WriteLine();
            Console.WriteLine("Store instance sync report");
            Console.WriteLine($"  Mode: {(createMissingStoreBlueprints ? "create missing UTM blueprints" : dryRun ? "check" : "sync")}");
            Console.WriteLine($"  Stores scanned: {report.TotalStores}");
            Console.WriteLine($"  Source-backed stores: {report.SourceBackedStores}");
            Console.WriteLine($"  Stores missing UTM blueprint: {report.StoresMissingBlueprint}");
            Console.WriteLine($"  UTM blueprints created: {createdBlueprintFiles.Count}");
            Console.WriteLine($"  Out-of-date stores: {report.OutOfDateStores}");
            Console.WriteLine($"  Source-backed items: {report.SourceBackedItems}");
            Console.WriteLine($"  Out-of-date items: {report.OutOfDateItems}");
            Console.WriteLine($"  Items missing UTI blueprint: {report.ItemsMissingBlueprint}");
            Console.WriteLine($"  Items checked in stores without UTM blueprint: {report.ItemsInStoresWithoutStoreBlueprint}");
            Console.WriteLine($"  Files changed: {changedFiles.Count}");

            PrintExamples("Out-of-date stores", report.OutOfDateStoreExamples);
            PrintExamples("Out-of-date items", report.OutOfDateItemExamples);
            PrintExamples("Missing UTM store blueprints", report.MissingStoreExamples);
            PrintExamples("Missing UTI item blueprints", report.MissingItemExamples);

            if (!dryRun && changedFiles.Count > 0)
            {
                Console.WriteLine("Changed files:");
                foreach (var file in changedFiles)
                    Console.WriteLine($"  {file}");
            }

            if (createdBlueprintFiles.Count > 0)
            {
                Console.WriteLine("Created UTM blueprints:");
                foreach (var file in createdBlueprintFiles)
                    Console.WriteLine($"  {file}");
            }
        }

        private static void PrintExamples(string title, IReadOnlyCollection<string> examples)
        {
            if (examples.Count == 0)
                return;

            Console.WriteLine($"{title} examples:");
            foreach (var example in examples)
                Console.WriteLine($"  {example}");
        }

        private class ItemInstanceMetadata
        {
            public JToken StructId { get; set; }
            public JToken Infinite { get; set; }
            public JToken ReposPosX { get; set; }
            public JToken ReposPosY { get; set; }
            public JToken XOrientation { get; set; }
            public JToken XPosition { get; set; }
            public JToken YOrientation { get; set; }
            public JToken YPosition { get; set; }
            public JToken ZPosition { get; set; }
        }

        private class StoreInstanceSyncReport
        {
            public int TotalStores { get; set; }
            public int SourceBackedStores { get; set; }
            public int StoresMissingBlueprint { get; set; }
            public int OutOfDateStores { get; set; }
            public int SourceBackedItems { get; set; }
            public int OutOfDateItems { get; set; }
            public int ItemsMissingBlueprint { get; set; }
            public int ItemsInStoresWithoutStoreBlueprint { get; set; }
            public List<string> OutOfDateStoreExamples { get; } = new();
            public List<string> OutOfDateItemExamples { get; } = new();
            public List<string> MissingStoreExamples { get; } = new();
            public List<string> MissingItemExamples { get; } = new();
        }
    }
}

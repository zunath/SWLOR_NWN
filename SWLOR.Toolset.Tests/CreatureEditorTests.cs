using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Creatures;

namespace SWLOR.Toolset.Tests
{
    [NonParallelizable]
    public class CreatureEditorTests
    {
        [Test]
        [NonParallelizable]
        public void EveryCreature_ConstructsWithoutChangingItsBytes()
        {
            var files = Directory.EnumerateFiles(
                Path.Combine(CorpusLocator.ModuleDirectory, "utc"), "*.utc.json").ToList();
            files.Should().HaveCount(938);

            var failures = new List<string>();
            foreach (var file in files)
            {
                var original = File.ReadAllBytes(file);
                using var session = DocumentSession.Open(file);
                try
                {
                    using var editor = new CreatureEditorViewModel(
                        session.Document.Root,
                        file,
                        Path.GetFileName(file).Split('.')[0],
                        (description, mutation) =>
                        {
                            session.Execute(description, mutation);
                            return true;
                        },
                        null,
                        null,
                        null,
                        null,
                        _ => null,
                        null);
                    session.ToBytes().Should().Equal(original);

                    var customVariables = editor.Variables.Rows
                        .Select(row => row.Name)
                        .ToHashSet(StringComparer.Ordinal);
                    var hiddenVariables = new CreatureValueStore(session.Document.Root).Locals
                        .Select(entry => entry.Name)
                        .Where(name => !customVariables.Contains(name));
                    foreach (var name in hiddenVariables)
                    {
                        if (!IsDedicatedCreatureVariable(name))
                            failures.Add($"{Path.GetFileName(file)}: hidden local '{name}' has no editor surface");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"{Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            failures.Should().BeEmpty(string.Join(Environment.NewLine, failures.Take(10)));
        }

        private static bool IsDedicatedCreatureVariable(string name)
        {
            if (name.StartsWith("PERK_LEVEL_", StringComparison.Ordinal))
                return true;
            if (System.Text.RegularExpressions.Regex.IsMatch(name, "^LOOT_TABLE_[0-9]+$"))
                return true;
            return name is
                "QUEST_NPC_GROUP_ID" or "CONVERSATION" or "GUILD_ID" or
                "STORE_TAG_RANK_1" or "STORE_TAG_RANK_2" or "STORE_TAG_RANK_3" or
                "STORE_TAG_RANK_4" or "STORE_TAG_RANK_5" or "BEAST_TYPE" or
                "PERMANENT_VFX_ID" or "PARALYZE" or "DAZE" or "AI_PROFILE" or "AI_FLAGS";
        }

        [Test]
        public void EditingMissingStatSkin_CreatesLinkedItem_AndUndoRestoresUtcBytes()
        {
            var root = Path.Combine(Path.GetTempPath(), "swlor-creature-editor-" + Guid.NewGuid().ToString("N"));
            var utcDirectory = Path.Combine(root, "utc");
            var utiDirectory = Path.Combine(root, "uti");
            Directory.CreateDirectory(utcDirectory);
            Directory.CreateDirectory(utiDirectory);
            var path = Path.Combine(utcDirectory, "test_beast.utc.json");
            File.WriteAllBytes(path, BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "test_beast", "Test Beast"));

            try
            {
                var original = File.ReadAllBytes(path);
                using var session = DocumentSession.Open(path);
                using var editor = new CreatureEditorViewModel(
                    session.Document.Root,
                    path,
                    "test_beast",
                    (description, mutation) =>
                    {
                        session.Execute(description, mutation);
                        return true;
                    },
                    null,
                    null,
                    null,
                    null,
                    _ => null,
                    null);

                editor.Stats.Vitals.Single(cell => cell.Label == "NPC Level").Number = 7;

                var store = new CreatureValueStore(session.Document.Root);
                var skinResRef = store.EquippedResRef(CreaturePropertyCatalog.StatSkinSlot);
                skinResRef.Should().NotBeNullOrWhiteSpace();
                editor.Equipment.ForSlot(CreaturePropertyCatalog.StatSkinSlot)!.Store
                    .GetPropertyValue(CreaturePropertyCatalog.Level, -1).Should().Be(7);

                session.Undo();
                editor.ReloadFromDocument();
                store.EquippedResRef(CreaturePropertyCatalog.StatSkinSlot).Should().BeNull();
                session.ToBytes().Should().Equal(original);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [TestCase(-100, 200)]
        [TestCase(-10, 110)]
        [TestCase(0, 0)]
        [TestCase(100, 100)]
        public void ResistanceEncoding_RoundTrips(int authored, int stored)
        {
            CreaturePropertyCatalog.EncodeResistance(authored).Should().Be(stored);
            CreaturePropertyCatalog.DecodeResistance(stored).Should().Be(authored);
        }

        [Test]
        public void LootWrite_ClampsAndRenumbersContiguously()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_test", "Loot Test"));
            var store = new CreatureValueStore(document.Root);
            store.Locals.SetString("LOOT_TABLE_1", "FIRST,50,2");
            store.Locals.SetString("LOOT_TABLE_3", "THIRD,900,0");

            var read = store.ReadLoot(out var hasGap);
            hasGap.Should().BeTrue();
            store.WriteLoot(read);

            store.Locals.GetString("LOOT_TABLE_1").Should().Be("FIRST,50,2");
            store.Locals.GetString("LOOT_TABLE_2").Should().Be("THIRD,100,1");
            store.Locals.GetString("LOOT_TABLE_3").Should().BeNull();
        }

        [Test]
        public void LootWrite_PreservesMalformedAndDeprecatedLocals()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_test", "Loot Test"));
            var store = new CreatureValueStore(document.Root);
            store.Locals.SetString("LOOT_TABLE_1", "FIRST,50,2");
            store.Locals.SetString("LOOT_TABLE_ID", "LEGACY");
            store.Locals.SetString("LOOT_TABLE_1`", "TYPO");

            store.WriteLoot(new[] { new CreatureLootEntry("SECOND", 100, 1) });

            store.Locals.GetString("LOOT_TABLE_1").Should().Be("SECOND,100,1");
            store.Locals.GetString("LOOT_TABLE_ID").Should().Be("LEGACY");
            store.Locals.GetString("LOOT_TABLE_1`").Should().Be("TYPO");
        }

        [Test]
        public void PropertyCoverage_DeclaresEveryCreatureSpecificSurface()
        {
            CreaturePropertyCatalog.SurfacedSkinProperties.Should().BeEquivalentTo(new[]
            {
                91, 92, 94, 96, 99, 111, 112, 117, 118, 125, 133
            });
            CreaturePropertyCatalog.SurfacedWeaponProperties.Should().BeEquivalentTo(new[]
            {
                93, 98, 103, 134
            });
        }

        [Test]
        public void LinkedCreatureItems_OnlyContainSurfacedOrExplicitlyPreservedProperties()
        {
            var observedSkin = new HashSet<int>();
            var observedWeapons = new HashSet<int>();
            foreach (var file in Directory.EnumerateFiles(
                         Path.Combine(CorpusLocator.ModuleDirectory, "utc"), "*.utc.json"))
            {
                var creature = JsonGffDocument.Load(file).Root;
                foreach (var equipment in creature.GetListOrEmpty("Equip_ItemList"))
                {
                    var slot = (int)(equipment.StructId ?? 0);
                    if (slot != CreaturePropertyCatalog.StatSkinSlot &&
                        slot != CreaturePropertyCatalog.MainWeaponSlot &&
                        slot != CreaturePropertyCatalog.OffWeaponSlot &&
                        slot != CreaturePropertyCatalog.CreatureWeaponSlot)
                    {
                        continue;
                    }

                    var resRef = equipment.GetStringOrNull("EquippedRes");
                    var itemPath = Path.Combine(CorpusLocator.ModuleDirectory, "uti", resRef + ".uti.json");
                    if (string.IsNullOrWhiteSpace(resRef) || !File.Exists(itemPath))
                        continue;

                    var properties = new ItemValueStore(JsonGffDocument.Load(itemPath).Root).Properties;
                    var target = slot == CreaturePropertyCatalog.StatSkinSlot ? observedSkin : observedWeapons;
                    foreach (var property in properties)
                        target.Add(property.PropertyId);
                }
            }

            observedSkin.Should().BeSubsetOf(CreaturePropertyCatalog.SurfacedSkinProperties
                .Concat(CreaturePropertyCatalog.PreservedSkinProperties));
            observedWeapons.Should().BeSubsetOf(CreaturePropertyCatalog.SurfacedWeaponProperties
                .Concat(CreaturePropertyCatalog.PreservedWeaponProperties));
        }

        [Test]
        public void RemoteReferenceArtwork_UsesTheSharedChoiceGallery()
        {
            var choice = new BehaviorChoice(42, "Visual effect", imageUrl: "https://example.test/vfx.png");

            new BehaviorChoiceViewModel(choice).HasArtwork.Should().BeTrue();
        }

        [Test]
        public void UnknownLootTables_AreFlaggedButDoNotRequestNumberingRepair()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_test", "Loot Test"));
            var store = new CreatureValueStore(document.Root);
            store.Locals.SetString("LOOT_TABLE_1", "REMOVED_TABLE,100,1");

            var viewModel = new CreatureLootViewModel(store, (_, mutation) =>
            {
                mutation();
                return true;
            }, Array.Empty<CreatureLootTableInfo>());

            viewModel.HasWarning.Should().BeTrue();
            viewModel.NeedsNormalization.Should().BeFalse();
        }

        [Test]
        public void LootDefinitionLink_UsesTheRegisteredDefinitionType()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_test", "Loot Test"));
            var store = new CreatureValueStore(document.Root);
            store.Locals.SetString("LOOT_TABLE_1", "TEST_TABLE,100,1");
            string? opened = null;
            var table = new CreatureLootTableInfo(
                "TEST_TABLE", "Test Table", false, Array.Empty<CreatureLootTableItemInfo>(), "TestLootDefinition");
            var viewModel = new CreatureLootViewModel(store, (_, mutation) =>
            {
                mutation();
                return true;
            }, new[] { table }, typeName => opened = typeName);

            viewModel.OpenDefinitionCommand.Execute(null);

            opened.Should().Be("TestLootDefinition");
        }

        [Test]
        public void SoundSetPreview_ResolvesARepresentativeAudioResource()
        {
            var root = Path.Combine(Path.GetTempPath(), "swlor-soundset-preview-" + Guid.NewGuid().ToString("N"));
            var twoDaDirectory = Path.Combine(root, "2da");
            var resourceDirectory = Path.Combine(root, "resources");
            Directory.CreateDirectory(twoDaDirectory);
            Directory.CreateDirectory(resourceDirectory);
            File.WriteAllText(Path.Combine(twoDaDirectory, "soundset.2da"),
                "2DA V2.0\n\nLABEL RESREF STRREF GENDER TYPE\n0 Test c_test 0 0 4\n");
            File.WriteAllBytes(Path.Combine(resourceDirectory, "c_test.ssf"), SoundSetBytes("sample_voice"));

            try
            {
                var resources = new ResourceIndex(null,
                    new[] { new ResourceIndex.HakLayer("test", resourceDirectory) });
                var resolver = new CreatureSoundSetPreviewResolver(new TwoDaService(twoDaDirectory), resources);

                resolver.Resolve(0).Should().Be("sample_voice");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [AvaloniaTest]
        public void CreatureEditorView_RendersItsSevenTabSurface()
        {
            var root = Path.Combine(Path.GetTempPath(), "swlor-creature-view-" + Guid.NewGuid().ToString("N"));
            var utcDirectory = Path.Combine(root, "utc");
            Directory.CreateDirectory(utcDirectory);
            Directory.CreateDirectory(Path.Combine(root, "uti"));
            var path = Path.Combine(utcDirectory, "render_test.utc.json");
            File.WriteAllBytes(path, BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "render_test", "Render Test"));

            try
            {
                using var session = DocumentSession.Open(path);
                using var editor = new CreatureEditorViewModel(
                    session.Document.Root,
                    path,
                    "render_test",
                    (_, mutation) =>
                    {
                        mutation();
                        return true;
                    },
                    null, null, null, null, _ => null, null);
                var view = new CreatureEditorView { DataContext = editor };
                var window = new Window { Width = 1280, Height = 800, Content = view };

                window.Show();
                Dispatcher.UIThread.RunJobs();
                view.GetVisualDescendants().Should().NotBeEmpty();
                view.FindControl<TabControl>("CreatureTabs").Should().NotBeNull();

                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static byte[] SoundSetBytes(string resRef)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
            writer.Write(Encoding.ASCII.GetBytes("SSF V1.0"));
            writer.Write((uint)1);
            writer.Write((uint)40);
            writer.Write(new byte[24]);
            writer.Write((uint)44);
            var fixedResRef = new byte[16];
            var encoded = Encoding.ASCII.GetBytes(resRef);
            encoded.AsSpan(0, Math.Min(fixedResRef.Length, encoded.Length)).CopyTo(fixedResRef);
            writer.Write(fixedResRef);
            writer.Write(uint.MaxValue);
            writer.Flush();
            return stream.ToArray();
        }

        [Test]
        public void BasicLayout_UsesReadOnlyResRefAndRequiredBuilderFields()
        {
            CreatureEditorLayout.Basic.Single(field => field.Name == "TemplateResRef")
                .IsReadOnly.Should().BeTrue();
            CreatureEditorLayout.Basic.Should().Contain(field => field.Name == "PaletteID");
            CreatureEditorLayout.Basic.Should().Contain(field => field.Name == "Race");
            CreatureEditorLayout.Basic.Should().Contain(field => field.Name == "FactionID");
            CreatureEditorLayout.Basic.Should().NotContain(field => field.Name.Contains("Script"));
            CreatureEditorLayout.GuildMaster.Single(field => field.Name == "GUILD_ID")
                .ChoicesKey.Should().Be(CreatureChoiceKeys.Guilds);
        }

        [Test]
        public void PerkCatalog_BuildsWithoutAHeadlessNwnRuntime()
        {
            var catalog = CreaturePerkCatalog.Build();

            catalog[(int)SWLOR.Game.Server.Service.PerkService.PerkType.Tame]
                .MaximumLevel.Should().Be(5);
            catalog[(int)SWLOR.Game.Server.Service.PerkService.PerkType.CombatAnalyzer]
                .MaximumLevel.Should().Be(4);
        }
    }
}

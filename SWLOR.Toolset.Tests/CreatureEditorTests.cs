using System.Numerics;
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
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Appearance;
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

                editor.Stats.HasStatSkin.Should().BeFalse();
                editor.Stats.Vitals.Single(cell => cell.Label == "NPC Level").Number = 7;

                var store = new CreatureValueStore(session.Document.Root);
                var skinResRef = store.EquippedResRef(CreaturePropertyCatalog.StatSkinSlot);
                skinResRef.Should().NotBeNullOrWhiteSpace();
                editor.Stats.HasStatSkin.Should().BeTrue();
                editor.Equipment.ForSlot(CreaturePropertyCatalog.StatSkinSlot)!.Store
                    .GetPropertyValue(CreaturePropertyCatalog.Level, -1).Should().Be(7);

                session.Undo();
                editor.ReloadFromDocument();
                store.EquippedResRef(CreaturePropertyCatalog.StatSkinSlot).Should().BeNull();
                editor.Stats.HasStatSkin.Should().BeFalse();
                session.ToBytes().Should().Equal(original);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void StatsTab_DoesNotExposeStatSkinImplementation()
        {
            var view = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));

            view.ToLowerInvariant().Should().NotContain("stat skin");
            view.Should().NotContain("CreateStatSkinCommand");
            view.Should().NotContain("SkinStatus");
            view.Should().NotContain("IsStatSkinMissing");
        }

        [Test]
        public void AbilitiesTab_DoesNotExposePreservedEngineFeats()
        {
            var view = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));

            view.ToLowerInvariant().Should().NotContain("engine feat");
            view.Should().NotContain("PreservedSummary");
            view.Should().NotContain("HasPreservedFeats");
        }

        [Test]
        public void RegisteredAbility_CanBeAddedRankedAndRemoved()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "ability_test", "Ability Test"));
            var store = new CreatureValueStore(document.Root);
            var ability = new CreatureAbilityInfo(42, "Test Ability", "Test description", 7, "Test Perk");
            var viewModel = new CreatureAbilitiesViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                [ability],
                new Dictionary<int, CreaturePerkInfo>
                {
                    [7] = new CreaturePerkInfo(7, "Test Perk", 4)
                });

            viewModel.AddCommand.Execute(ability);

            var assigned = viewModel.Assigned.Should().ContainSingle().Which;
            store.Feats.Should().ContainSingle().Which.Should().Be(42);
            assigned.EffectiveLevel.Should().Be(4);

            assigned.EffectiveLevel = 2;
            store.Locals.GetInt("PERK_LEVEL_7").Should().Be(2);

            assigned.RemoveCommand.Execute(null);
            store.Feats.Should().BeEmpty();
            viewModel.Assigned.Should().BeEmpty();
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
        public void LootTables_ReuseTheSharedProgressivePicker()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_picker", "Loot Picker"));
            var store = new CreatureValueStore(document.Root);
            store.Locals.SetString("LOOT_TABLE_1", "TABLE_400,75,2");
            var tables = Enumerable.Range(0, 500)
                .Select(index => new CreatureLootTableInfo(
                    $"TABLE_{index:D3}",
                    $"Table {index:D3}",
                    false,
                    Array.Empty<CreatureLootTableItemInfo>()))
                .ToList();
            using var viewModel = new CreatureLootViewModel(store, (_, mutation) =>
            {
                mutation();
                return true;
            }, tables);

            var row = viewModel.Entries.Should().ContainSingle().Which;
            var picker = row.TablePicker;
            picker.Should().BeAssignableTo<BehaviorRowViewModel>();
            picker.AreChoicesLoaded.Should().BeFalse();
            picker.FilteredChoices.Should().BeEmpty();
            picker.SelectedChoiceDisplay.Should().Be("Table 400 (TABLE_400)");

            picker.OpenSearchCommand.Execute(null);

            picker.AreChoicesLoaded.Should().BeTrue();
            picker.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize + 1,
                "the stored table remains visible above the first progressive page");
            picker.ChoiceSearchText = "TABLE_321";
            var match = picker.FilteredChoices.Should().ContainSingle().Which;

            picker.PickChoiceCommand.Execute(match);

            store.Locals.GetString("LOOT_TABLE_1").Should().Be("TABLE_321,75,2");
            row.Table.Should().BeSameAs(tables[321]);
            viewModel.SelectedEntry.Should().BeSameAs(row);
            viewModel.PreviewTitle.Should().Be("Table 321");
            picker.IsSearchExpanded.Should().BeFalse();
            picker.FilteredChoices.Should().BeEmpty();
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
        public void CreatureEditorView_RendersItsEightTabSurface()
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
                    (description, mutation) =>
                    {
                        session.Execute(description, mutation);
                        return true;
                    },
                    null, null, null, null,
                    _ => new AppearanceRow(0, "DYNAMIC_TEST", "Dynamic Test", "P", "H", null),
                    null,
                    appearanceOptions: Enumerable.Range(0, 50)
                        .Select(index => new AppearanceOption(
                            index.ToString(), $"Appearance {index}", $"row {index}",
                            CreatureAppearanceId: index))
                        .ToList());
                var view = new CreatureEditorView { DataContext = editor };
                var window = new Window { Width = 1280, Height = 800, Content = view };

                window.Show();
                Dispatcher.UIThread.RunJobs();
                view.GetVisualDescendants().Should().NotBeEmpty();
                var tabs = view.FindControl<TabControl>("CreatureTabs");
                tabs.Should().NotBeNull();
                tabs!.Items.Cast<TabItem>().Select(tab => tab.Header?.ToString()).Should().Equal(
                    "Basic", "Behavior", "Appearance", "Equipment", "Stats", "Abilities", "AI", "Loot");
                tabs!.SelectedIndex = 2;
                Dispatcher.UIThread.RunJobs();

                var appearanceSections = view.FindControl<TabControl>("CreatureAppearanceSections");
                appearanceSections.Should().NotBeNull();
                appearanceSections!.Items.Cast<TabItem>().Select(tab => tab.Header?.ToString()).Should().Equal(
                    "Model", "Details", "Body");
                for (var index = 0; index < 3; index++)
                {
                    appearanceSections!.SelectedIndex = index;
                    Dispatcher.UIThread.RunJobs();
                }

                tabs.SelectedIndex = 3;
                Dispatcher.UIThread.RunJobs();
                var equipmentSlots = view.FindControl<ListBox>("CreatureEquipmentSlots");
                equipmentSlots.Should().NotBeNull();
                equipmentSlots!.ItemCount.Should().Be(4);
                equipmentSlots.SelectedItem.Should().BeSameAs(editor.VisibleEquipment.SelectedSlot);

                tabs.SelectedIndex = 5;
                Dispatcher.UIThread.RunJobs();
                var abilityButton = view.GetVisualDescendants()
                    .OfType<Button>()
                    .FirstOrDefault(button => button.DataContext is CreatureAbilityInfo);
                abilityButton.Should().NotBeNull("registered abilities must render as actionable choices");
                abilityButton!.IsEnabled.Should().BeTrue();
                abilityButton.Command.Should().NotBeNull();
                abilityButton.CommandParameter.Should().BeOfType<CreatureAbilityInfo>();
                var selectedAbility = (CreatureAbilityInfo)abilityButton.CommandParameter!;

                abilityButton.Command!.Execute(abilityButton.CommandParameter);
                Dispatcher.UIThread.RunJobs();
                editor.Abilities.Assigned.Should().ContainSingle(entry => entry.FeatId == selectedAbility.FeatId);

                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void CreaturePreview_FacesTheDefaultCameraAndAlwaysPublishesAnimationSegments()
        {
            using var editor = OpenPreviewEditor(new RenderModel { Name = "facing_test" });

            editor.PreviewScene.Should().NotBeNull();
            editor.PreviewScene!.Instances.Single().Orientation.Should().Be(new Vector2(-1f, 0f));
            editor.PreviewAnimations.Select(option => option.Display)
                .Should().Equal("Idle", "Walk", "Attack");
        }

        [Test]
        public void CreaturePreview_MapsNamedAnimationsToSegments()
        {
            using var editor = OpenPreviewEditor(new RenderModel
            {
                Name = "facing_test",
                DefaultAnimationName = "pause1",
                Animations =
                [
                    new RenderAnimation { Name = "pause1", Length = 1f, IsPlayable = true },
                    new RenderAnimation { Name = "walk", Length = 1f, IsPlayable = true },
                    new RenderAnimation { Name = "1hslashl", Length = 1f, IsPlayable = true }
                ]
            });

            editor.PreviewAnimations.Select(option => option.AnimationName)
                .Should().Equal("pause1", "walk", "1hslashl");
        }

        private static CreatureEditorViewModel OpenPreviewEditor(RenderModel model)
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "facing_test", "Facing Test"));
            return new CreatureEditorViewModel(
                document.Root,
                Path.Combine(CorpusLocator.ModuleDirectory, "utc", "facing_test.utc.json"),
                "facing_test",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                null,
                null,
                _ => model,
                _ => null,
                null);
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
        public void DirectFields_AreGroupedByTheTabsThatOwnThem()
        {
            CreatureEditorLayout.Basic.Single(field => field.Name == "TemplateResRef")
                .IsReadOnly.Should().BeTrue();
            CreatureEditorLayout.Basic.Should().Contain(field => field.Name == "PaletteID");
            var movedFields = new[]
            {
                "Race", "FactionID", "Conversation", "Plot",
                "IsImmortal", "NoPermDeath", "Disarmable"
            };
            CreatureEditorLayout.Basic.Should().NotContain(field => movedFields.Contains(field.Name));
            CreatureEditorLayout.Appearance.Should().Contain(field => field.Name == "Race");
            CreatureEditorLayout.Appearance.Should().NotContain(field => field.Name == "Appearance_Type",
                "the model uses the shared paged appearance gallery instead of a generic choice row");
            CreatureEditorLayout.Ai.Should().ContainSingle(field => field.Name == "FactionID");
            CreatureEditorLayout.DialogRole.Should().Contain(field => field.Name == "Conversation");
            CreatureEditorLayout.CombatRules.Select(field => field.Name).Should().Equal(
                "Plot", "IsImmortal", "NoPermDeath", "Disarmable");
            CreatureEditorLayout.Basic.Should().NotContain(field => field.Name.Contains("Script"));
            CreatureEditorLayout.GuildMaster.Single(field => field.Name == "GUILD_ID")
                .ChoicesKey.Should().Be(CreatureChoiceKeys.Guilds);
        }

        [Test]
        public async Task CreatureAppearance_UsesTheSharedPagedGalleryAndDefersBodyPartRows()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "appearance_test", "Appearance Test"));
            var options = Enumerable.Range(0, 500)
                .Select(index => new AppearanceOption(
                    index.ToString(),
                    $"Appearance {index}",
                    $"row {index}",
                    CreatureAppearanceId: index))
                .ToList();
            using var editor = new CreatureEditorViewModel(
                document.Root,
                Path.Combine(CorpusLocator.ModuleDirectory, "utc", "appearance_test.utc.json"),
                "appearance_test",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                null,
                null,
                null,
                _ => null,
                null,
                appearanceOptions: options);

            editor.AppearanceGallery.Should().NotBeNull();
            editor.AppearanceGallery!.Tiles.Should().HaveCount(48,
                "the shared gallery only publishes one visual page at a time");
            editor.BodyParts.IsLoaded.Should().BeFalse(
                "opening an editor must not scan the model catalog for a hidden body section");

            var target = editor.AppearanceGallery.Tiles[7];
            editor.AppearanceGallery.Highlighted = target;

            new CreatureValueStore(document.Root)
                .GetInteger(BehaviorFieldStorage.Field, "Appearance_Type").Should().Be(7);

            editor.SelectedAppearanceSectionIndex = 2;
            await editor.BodyParts.EnsureLoadedAsync();
            editor.BodyParts.IsLoaded.Should().BeTrue(
                "the body rows are published when their section is actually opened");
        }

        [Test]
        public async Task CreatureBodyParts_AreIndependentUntilTheBuilderEnablesMirroring()
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "utc", "nw_blozeatiato.utc.json");
            var document = JsonGffDocument.Load(path);
            var store = new CreatureValueStore(document.Root);
            using var editor = new CreatureEditorViewModel(
                document.Root,
                path,
                "nw_blozeatiato",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                null,
                null,
                null,
                id => id == 10001
                    ? new AppearanceRow(id, "DYNAMIC_CHISS", "Dynamic Chiss", "P", "H", null)
                    : null,
                null);

            await editor.BodyParts.EnsureLoadedAsync();

            editor.BodyParts.MirrorRightFromLeft.Should().BeFalse(
                "matching stored values do not imply that the builder opted into mirroring");
            editor.BodyParts.Limbs.Select(pair => pair.Label).Should().Equal(
                "Shoulder", "Bicep", "Forearm", "Hand", "Thigh", "Shin", "Foot");
            editor.BodyParts.Limbs.Should().OnlyContain(pair => pair.Right.IsEnabled);
            editor.BodyParts.Structure.Should().Contain(cell => cell.Label == "Belt");
            editor.BodyParts.Colors.Select(color => color.Label).Should().Equal(
                "Skin", "Hair", "Body Color 1", "Body Color 2");
            editor.BodyParts.Colors.Should().OnlyContain(color =>
                !color.AllowsNumericFallback && !color.HasNumericFallback,
                "creature colors always use the shared palette picker, never a number control");

            var bicep = editor.BodyParts.Limbs.Single(pair => pair.Label == "Bicep");
            bicep.Left.Number = 2;
            store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_LBicep").Should().Be(2);
            store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_RBicep").Should().Be(1);
            store.GetInteger(BehaviorFieldStorage.Field, "xBodyPart_LBicep").Should().Be(2);

            bicep.Right.Number = 3;
            store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_LBicep").Should().Be(2);
            store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_RBicep").Should().Be(3);

            editor.BodyParts.MirrorRightFromLeft = true;
            bicep.Right.IsReadOnly.Should().BeTrue();
            bicep.Right.Number.Should().Be(2,
                "enabling mirroring immediately copies the left side to the right");

            bicep.Left.Number = 4;
            store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_LBicep").Should().Be(4);
            store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_RBicep").Should().Be(4);

            editor.BodyParts.MirrorRightFromLeft = false;
            bicep.Right.IsEnabled.Should().BeTrue();
            bicep.Left.Number = 5;
            store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_LBicep").Should().Be(5);
            store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_RBicep").Should().Be(4);

            var foot = editor.BodyParts.Limbs.Single(pair => pair.Label == "Foot");
            foot.Right.Number = 2;
            store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_RFoot").Should().Be(2,
                "the creature format's unusual right-foot field must remain wired correctly");
        }

        [Test]
        public void VisibleEquipment_LoadsOneSharedProgressivePickerAtATime()
        {
            var catalogLoads = 0;
            IReadOnlyList<CreatureEquipmentChoice> Equipment()
            {
                catalogLoads++;
                return Enumerable.Range(0, 500)
                    .Select(index => new CreatureEquipmentChoice(
                        $"armor_{index:D3}", $"Armor {index:D3}", 16))
                    .ToList();
            }

            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "equipment_test", "Equipment Test"));
            using var editor = new CreatureEditorViewModel(
                document.Root,
                Path.Combine(CorpusLocator.ModuleDirectory, "utc", "equipment_test.utc.json"),
                "equipment_test",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                null,
                null,
                null,
                _ => null,
                null,
                equipmentChoices: Equipment);

            catalogLoads.Should().Be(0);
            editor.VisibleEquipment.SelectedSlot.Should().BeSameAs(
                editor.VisibleEquipment.Slots.Single(slot => slot.Label == "Armor"));
            editor.VisibleEquipment.Slots.Should().OnlyContain(slot => !slot.AreChoicesLoaded);
            editor.VisibleEquipment.Slots.Should().OnlyContain(slot => slot.FilteredChoices.Count == 0);

            var armor = editor.VisibleEquipment.Slots.Single(slot => slot.Label == "Armor");
            armor.OpenSearchCommand.Execute(null);

            catalogLoads.Should().Be(1);
            armor.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize);
            editor.VisibleEquipment.Slots.Where(slot => slot.Label != "Armor")
                .Should().OnlyContain(slot => !slot.AreChoicesLoaded);

            var mainHand = editor.VisibleEquipment.Slots.Single(slot => slot.Label == "Main Hand");
            editor.VisibleEquipment.SelectedSlot = mainHand;
            armor.IsSearchExpanded.Should().BeFalse(
                "moving between compact slot summaries closes and releases the old list");
            armor.FilteredChoices.Should().BeEmpty();
            mainHand.AreChoicesLoaded.Should().BeFalse(
                "selecting a slot is cheap; its catalog loads only when Choose is opened");

            editor.VisibleEquipment.SelectedSlot = armor;
            armor.OpenSearchCommand.Execute(null);

            armor.PickChoiceCommand.Execute(armor.FilteredChoices[3]);
            new CreatureValueStore(document.Root).EquippedResRef(2).Should().Be("armor_003");
            armor.CanClearChoice.Should().BeTrue();

            armor.ClearChoiceCommand.Execute(null);
            new CreatureValueStore(document.Root).EquippedResRef(2).Should().BeNull();
            armor.CanClearChoice.Should().BeFalse();
        }

        [Test]
        public void BehaviorChoices_LoadOnePickerAtATimeAndRoleRowsAreReused()
        {
            var guildStoreLoads = 0;
            IReadOnlyList<BehaviorChoice> Resolve(string key)
            {
                if (key == CreatureChoiceKeys.GuildStores)
                {
                    guildStoreLoads++;
                    return Enumerable.Range(0, 120)
                        .Select(index => new BehaviorChoice($"store_{index}", $"Store {index}"))
                        .ToList();
                }

                if (key == CreatureChoiceKeys.Guilds)
                    return new[] { new BehaviorChoice(1, "Hunters Guild") };
                return Array.Empty<BehaviorChoice>();
            }

            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "lazy_roles", "Lazy Roles"));
            using var editor = new CreatureEditorViewModel(
                document.Root,
                Path.Combine(CorpusLocator.ModuleDirectory, "utc", "lazy_roles.utc.json"),
                "lazy_roles",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                Resolve,
                null,
                null,
                _ => null,
                null);

            var guildMaster = CreatureRoleCatalog.All.Single(role => role.Id == "guild_master");
            editor.ChooseRoleCommand.Execute(guildMaster);
            guildStoreLoads.Should().Be(0,
                "switching behaviors must create field shells without resolving their catalogs");

            var firstStore = editor.RoleRows.Single(row => row.Definition.Name == "STORE_TAG_RANK_1");
            editor.RoleRows.Where(row => row.Definition.Name.StartsWith("STORE_TAG_RANK_"))
                .Should().OnlyContain(row => !row.AreChoicesLoaded);

            firstStore.OpenSearchCommand.Execute(null);

            guildStoreLoads.Should().Be(1);
            firstStore.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize);
            editor.RoleRows.Where(row => row.Definition.Name.StartsWith("STORE_TAG_RANK_") &&
                                         row.Definition.Name != "STORE_TAG_RANK_1")
                .Should().OnlyContain(row => !row.AreChoicesLoaded,
                    "opening one rank must not initialize the other four pickers");

            var presentation = CreatureRoleCatalog.All.Single(role => role.Id == "presentation");
            editor.ChooseRoleCommand.Execute(presentation);
            editor.ChooseRoleCommand.Execute(guildMaster);

            editor.RoleRows.Single(row => row.Definition.Name == "STORE_TAG_RANK_1")
                .Should().BeSameAs(firstStore, "revisiting a behavior reuses its already loaded rows");
            guildStoreLoads.Should().Be(1);
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

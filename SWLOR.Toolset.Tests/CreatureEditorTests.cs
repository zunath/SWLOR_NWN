using System.Collections.Specialized;
using System.Numerics;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Appearance;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Creatures;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

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
                "PERMANENT_VFX_ID" or "PARALYZE" or "DAZE" or "AI_PROFILE" or
                "AI_PROFILE_ID" or "AI_FLAGS";
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
        public void NaturalWeaponCheckbox_CreatesAndUnlinksTheHiddenBackingItem()
        {
            var root = Path.Combine(Path.GetTempPath(), "swlor-creature-weapon-" + Guid.NewGuid().ToString("N"));
            var utcDirectory = Path.Combine(root, "utc");
            Directory.CreateDirectory(utcDirectory);
            Directory.CreateDirectory(Path.Combine(root, "uti"));
            var path = Path.Combine(utcDirectory, "weapon_toggle.utc.json");
            File.WriteAllBytes(path, BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "weapon_toggle", "Weapon Toggle"));

            try
            {
                using var session = DocumentSession.Open(path);
                using var editor = new CreatureEditorViewModel(
                    session.Document.Root,
                    path,
                    "weapon_toggle",
                    (description, mutation) =>
                    {
                        session.Execute(description, mutation);
                        return true;
                    },
                    null, null, null, null, _ => null, null);
                var primary = editor.EquipmentSlots.NaturalWeapons.Single(weapon =>
                    weapon.Label == "Primary Natural Weapon");
                var store = new CreatureValueStore(session.Document.Root);

                primary.IsEnabled.Should().BeFalse();
                primary.IsEnabled = true;

                var createdResRef = store.EquippedResRef(CreaturePropertyCatalog.MainWeaponSlot);
                createdResRef.Should().NotBeNullOrWhiteSpace();
                editor.Equipment.ForSlot(CreaturePropertyCatalog.MainWeaponSlot)!.Session.Document.Root
                    .GetIntOrNull("BaseItem").Should().Be(69);

                primary.Damage.Number = 12;
                primary.IsEnabled = false;
                store.EquippedResRef(CreaturePropertyCatalog.MainWeaponSlot).Should().BeNull();
                primary.IsEnabled.Should().BeFalse();

                primary.IsEnabled = true;
                store.EquippedResRef(CreaturePropertyCatalog.MainWeaponSlot).Should().Be(createdResRef,
                    "re-enabling relinks the same hidden item instead of creating duplicates");
                primary.Damage.Number.Should().Be(12);
                primary.DamageStat.Options.Select(option => option.Value).Should().Equal(
                    Enum.GetValues<AbilityType>()
                        .Where(value => value != AbilityType.Invalid)
                        .Select(value => (int)value));
                primary.DamageStat.Selected = primary.DamageStat.Options.Single(option =>
                    option.Value == (int)AbilityType.Might);
                editor.Equipment.ForSlot(CreaturePropertyCatalog.MainWeaponSlot)!.Store.Properties
                    .Single(property => property.PropertyId == CreaturePropertyCatalog.DamageStat)
                    .SubtypeId.Should().Be((int)AbilityType.Might);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void EquipmentTab_UsesNaturalWeaponCheckboxesInsteadOfCreationButtons()
        {
            var view = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));

            view.Should().NotContain("Create weapon");
            view.Should().NotContain("EnsureExistsCommand");
            view.Should().Contain("ItemsSource=\"{Binding EquipmentSlots.NaturalWeapons}\"");
            view.Should().NotContain("ItemsSource=\"{Binding Stats.Weapons}\"");
            view.Should().Contain("IsChecked=\"{Binding IsEnabled, Mode=TwoWay}\"");
            view.Should().Contain("IsVisible=\"{Binding IsEnabled}\"",
                "disabled natural weapons should not fill Equipment with inactive fields");
        }

        [Test]
        public void NaturalWeaponSelectors_KeepTheirOptionsReadable()
        {
            var view = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));
            var optionTemplateStart = view.IndexOf(
                "DataType=\"creatures:CreatureOptionCellViewModel\"", StringComparison.Ordinal);
            var optionTemplateEnd = view.IndexOf("</DataTemplate>", optionTemplateStart, StringComparison.Ordinal);
            var optionTemplate = view[optionTemplateStart..optionTemplateEnd];

            optionTemplate.Should().Contain("ColumnDefinitions=\"*,240\"");
            optionTemplate.Should().Contain("MinWidth=\"240\"");
            optionTemplate.Should().Contain("HorizontalAlignment=\"Stretch\"");
            optionTemplate.Should().Contain("Text=\"{Binding Display}\"");
        }

        [Test]
        public void CreatureBodyPartSelectors_ReserveSpaceForThreeDigitVariants()
        {
            var view = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));
            var templateStart = view.IndexOf(
                "x:Key=\"CreaturePartValueTemplate\"", StringComparison.Ordinal);
            var templateEnd = view.IndexOf("</DataTemplate>", templateStart, StringComparison.Ordinal);
            var template = view[templateStart..templateEnd];

            template.Should().Contain("MinWidth=\"96\"",
                "three-digit body-part variants must remain visible beside the popup scrollbar");
            template.Should().Contain("HorizontalAlignment=\"Stretch\"");
        }

        [Test]
        public void StatsTab_UsesAStableEditingOrderInsteadOfIndependentColumns()
        {
            var view = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));
            var orderedCards = new[]
            {
                "CreatureVitalsCard",
                "CreatureAttributesCard",
                "CreatureOffenseCard",
                "CreatureDefenseCard"
            };

            var positions = orderedCards
                .Select(name => view.IndexOf(name, StringComparison.Ordinal))
                .ToList();
            positions.Should().OnlyContain(index => index >= 0);
            positions.Should().BeInAscendingOrder(
                "the Stats tab should read from core values through combat tuning");

            var statsStart = view.IndexOf("<TabItem Header=\"Stats\">", StringComparison.Ordinal);
            var statsEnd = view.IndexOf("</TabItem>", statsStart, StringComparison.Ordinal);
            var resistancesStart = view.IndexOf("<TabItem Header=\"Resistances\">", StringComparison.Ordinal);
            var resistancesCard = view.IndexOf("CreatureResistancesCard", StringComparison.Ordinal);
            var abilitiesStart = view.IndexOf("<TabItem Header=\"Abilities\"", StringComparison.Ordinal);
            var resistancesMarkup = view[resistancesStart..abilitiesStart];
            resistancesMarkup.Should().Contain("<UniformGrid Columns=\"1\" />",
                "each resistance should have a full-width row so its label cannot crowd another field's controls");
            resistancesMarkup.Should().NotContain("<UniformGrid Columns=\"2\" />");
            view.Should().NotContain("Skill Overrides");
            view.Should().NotContain("Stats.AddSkillOverrideCommand");

            resistancesStart.Should().BeGreaterThan(statsEnd,
                "resistances should be removed from the long Stats form");
            resistancesCard.Should().BeGreaterThan(resistancesStart);
            resistancesCard.Should().BeLessThan(abilitiesStart);

            var equipmentStart = view.IndexOf("CreatureEquipmentSections", StringComparison.Ordinal);
            var naturalWeapons = view.IndexOf("CreatureNaturalWeapons", StringComparison.Ordinal);
            equipmentStart.Should().BeGreaterThanOrEqualTo(0);
            naturalWeapons.Should().BeGreaterThan(equipmentStart);
            naturalWeapons.Should().BeLessThan(statsStart,
                "natural weapons belong to Equipment rather than Stats");
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
            store.Locals.GetInt("PERK_LEVEL_7").Should().BeNull(
                "removing the last dependent ability must not leave a hidden perk override");
            viewModel.Assigned.Should().BeEmpty();
        }

        [Test]
        public void RemovingAbility_PreservesPerkOverrideWhileAnotherGrantedFeatDependsOnIt()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "ability_shared", "Shared Perk"));
            var store = new CreatureValueStore(document.Root);
            store.AddFeat(99);
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
                    [7] = new CreaturePerkInfo(7, "Test Perk", 4, new HashSet<int> { 42, 99 })
                });

            viewModel.AddCommand.Execute(ability);
            viewModel.Assigned.Single().EffectiveLevel = 2;
            viewModel.Assigned.Single().RemoveCommand.Execute(null);

            store.Feats.Should().Equal(99);
            store.Locals.GetInt("PERK_LEVEL_7").Should().Be(2,
                "the remaining perk-granted feat still makes the explicit perk level intentional");
        }

        [Test]
        public void AbilityFilters_UseAudienceAndSkillMetadata()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "ability_filters", "Ability Filters"));
            var store = new CreatureValueStore(document.Root);
            var abilities = new[]
            {
                new CreatureAbilityInfo(1, "NPC Device", "NPC device ability", 0, "", 33, "Devices", true),
                new CreatureAbilityInfo(2, "Player Device", "Player device ability", 0, "", 33, "Devices"),
                new CreatureAbilityInfo(3, "Player Rifle", "Player rifle ability", 0, "", 46, "Rifle"),
                new CreatureAbilityInfo(4, "Unskilled", "No associated skill", 0, "")
            };
            var viewModel = new CreatureAbilitiesViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                abilities,
                new Dictionary<int, CreaturePerkInfo>());

            viewModel.SelectedAudienceFilter.Value.Should().Be(CreatureAbilityAudience.Npc,
                "creature builders should start with NPC-intended abilities");
            viewModel.Matching.Should().ContainSingle(info => info.Name == "NPC Device");

            viewModel.SelectedSkillFilter = viewModel.SkillFilters.Single(filter => filter.Label == "Devices");
            viewModel.Matching.Should().ContainSingle(info => info.Name == "NPC Device");

            viewModel.SelectedAudienceFilter = viewModel.AudienceFilters.Single(
                filter => filter.Value == CreatureAbilityAudience.Player);
            viewModel.Matching.Should().ContainSingle(info => info.Name == "Player Device");

            viewModel.SelectedAudienceFilter = viewModel.AudienceFilters[0];
            viewModel.SelectedSkillFilter = viewModel.SkillFilters.Single(filter => filter.Label == "No skill");
            viewModel.Matching.Should().ContainSingle(info => info.Name == "Unskilled");
        }

        [Test]
        public void AbilityFilters_TolerateTransientNullComboBoxSelections()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "ability_null", "Ability Null Filter"));
            var store = new CreatureValueStore(document.Root);
            var npcAbility = new CreatureAbilityInfo(
                1, "Warocas Ability", "Creature ability", 0, "", 0, "", true);
            var playerAbility = new CreatureAbilityInfo(
                2, "Player Ability", "Player ability", 0, "", 33, "Devices");
            var viewModel = new CreatureAbilitiesViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                [npcAbility, playerAbility],
                new Dictionary<int, CreaturePerkInfo>());

            var action = () =>
            {
                viewModel.SelectedSkillFilter = null!;
                viewModel.SelectedAudienceFilter = null!;
            };

            action.Should().NotThrow(
                "Avalonia can briefly clear a ComboBox selection while the Abilities tab is realized");
            viewModel.Matching.Should().ContainSingle().Which.Info.Should().Be(npcAbility,
                "a transient null selection should retain the NPC and all-skills defaults");
        }

        [Test]
        public void AbilityCatalog_UsesConciseGameplayDescriptionsWithoutRecastMetadata()
        {
            var catalog = CreatureAbilityCatalog.Build(CreaturePerkCatalog.Build());

            catalog.Should().NotBeEmpty();
            catalog.Should().OnlyContain(info => !string.IsNullOrWhiteSpace(info.Description));
            catalog.Should().OnlyContain(info =>
                !info.Description.Contains("recast", StringComparison.OrdinalIgnoreCase),
                "the picker should describe what an ability does rather than repeat recast metadata");
            catalog.Should().Contain(info =>
                info.Description.StartsWith("Hits ", StringComparison.Ordinal) ||
                info.Description.StartsWith("Affects ", StringComparison.Ordinal),
                "abilities without a registered perk description should still explain their target or area");
        }

        [Test]
        public async Task AbilityIcons_AreResolvedLazilyAndOnlyOncePerPublishedRow()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "ability_icon", "Ability Icon"));
            var store = new CreatureValueStore(document.Root);
            var resolverCalls = 0;
            var ability = new CreatureAbilityInfo(
                42, "Icon Ability", "A useful effect description.", 0, "", 0, "", true);
            using var viewModel = new CreatureAbilitiesViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                [ability],
                new Dictionary<int, CreaturePerkInfo>(),
                choicePreviews: new ChoicePreviewService(null),
                iconResolver: _ =>
                {
                    resolverCalls++;
                    return null;
                });

            resolverCalls.Should().Be(0,
                "opening the Abilities tab must not synchronously resolve icons for its result page");
            var row = viewModel.Matching.Should().ContainSingle().Which;

            await viewModel.EnsureIconAsync(row);
            await viewModel.EnsureIconAsync(row);

            resolverCalls.Should().Be(1,
                "a realized row should share its single asynchronous icon lookup across repeated loads");
        }

        [Test]
        public void AbilityAssignments_UpdatePublishedPageIncrementally()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "ability_paging", "Ability Paging"));
            var store = new CreatureValueStore(document.Root);
            var abilities = Enumerable.Range(1, 75)
                .Select(id => new CreatureAbilityInfo(
                    id, $"Ability {id:000}", "Test ability", 0, "", 33, "Devices", true))
                .ToArray();
            var viewModel = new CreatureAbilitiesViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                abilities,
                new Dictionary<int, CreaturePerkInfo>());

            viewModel.Matching.Should().HaveCount(40);
            viewModel.CanLoadMore.Should().BeTrue();
            var changes = new List<NotifyCollectionChangedAction>();
            viewModel.Matching.CollectionChanged += (_, change) => changes.Add(change.Action);
            var selected = viewModel.Matching[5];

            viewModel.AddCommand.Execute(selected.Info);

            changes.Should().NotContain(NotifyCollectionChangedAction.Reset,
                "assigning one ability must not reconstruct the published result page");
            viewModel.Matching.Should().HaveCount(40);
            viewModel.Matching.Should().NotContain(selected);
            var assigned = viewModel.Assigned.Should().ContainSingle().Which;

            changes.Clear();
            assigned.RemoveCommand.Execute(null);

            changes.Should().NotContain(NotifyCollectionChangedAction.Reset,
                "removing one assignment must return only its sorted result row");
            viewModel.Matching.Should().Contain(row => row.Info == selected.Info);
            viewModel.LoadMoreCommand.Execute(null);
            viewModel.Matching.Should().HaveCount(75);
            viewModel.CanLoadMore.Should().BeFalse();
        }

        [Test]
        public async Task AbilityAndLootCatalogs_LoadOnlyWhenTheirTabsNeedThem()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "defer_catalogs", "Deferred Catalogs"));
            var store = new CreatureValueStore(document.Root);
            var abilityLoads = 0;
            var lootLoads = 0;
            var ability = new CreatureAbilityInfo(
                42, "Deferred Ability", "Loaded on demand", 0, "", 0, "", true);
            var table = new CreatureLootTableInfo(
                "DEFERRED_TABLE", "Deferred Table", false,
                Array.Empty<CreatureLootTableItemInfo>());
            using var abilities = new CreatureAbilitiesViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalogLoader: () =>
                {
                    abilityLoads++;
                    return Task.FromResult(new CreatureAbilityCatalogData(
                        [ability], new Dictionary<int, CreaturePerkInfo>()));
                });
            using var loot = new CreatureLootViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                tableLoader: () =>
                {
                    lootLoads++;
                    return Task.FromResult<IReadOnlyList<CreatureLootTableInfo>>([table]);
                });

            abilities.IsLoaded.Should().BeFalse();
            loot.IsLoaded.Should().BeFalse();
            abilityLoads.Should().Be(0);
            lootLoads.Should().Be(0);
            var initialSkillSelection = abilities.SelectedSkillFilter;

            await abilities.EnsureLoadedAsync();
            abilityLoads.Should().Be(1);
            abilities.IsLoaded.Should().BeTrue();
            abilities.SelectedSkillFilter.Should().BeSameAs(initialSkillSelection,
                "loading the catalog must not clear the ComboBox's active All skills row");
            abilities.Matching.Should().ContainSingle().Which.Info.Should().Be(ability);
            lootLoads.Should().Be(0, "opening Abilities must not load Loot");

            await loot.EnsureLoadedAsync();
            lootLoads.Should().Be(1);
            loot.IsLoaded.Should().BeTrue();
            loot.Tables.Should().ContainSingle().Which.Should().Be(table);
        }

        [Test]
        public async Task AbilityCatalogLoadFailure_LeavesTheTabRetryableOnTheNextEnsureLoadedCall()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "flaky_abilities", "Flaky Abilities"));
            var store = new CreatureValueStore(document.Root);
            var attempt = 0;
            var ability = new CreatureAbilityInfo(
                42, "Recovered Ability", "Loaded after retry", 0, "", 0, "", true);

            using var abilities = new CreatureAbilitiesViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalogLoader: () =>
                {
                    attempt++;
                    return attempt == 1
                        ? Task.FromException<CreatureAbilityCatalogData>(new InvalidOperationException("boom"))
                        : Task.FromResult(new CreatureAbilityCatalogData(
                            [ability], new Dictionary<int, CreaturePerkInfo>()));
                });

            await abilities.EnsureLoadedAsync();
            abilities.IsLoaded.Should().BeFalse(
                "a faulted load must stay retryable rather than permanently disabling the tab");
            abilities.HasLoadError.Should().BeTrue();

            await abilities.EnsureLoadedAsync();
            attempt.Should().Be(2, "re-entering the tab after a failure must retry the load");
            abilities.IsLoaded.Should().BeTrue();
            abilities.HasLoadError.Should().BeFalse();
            abilities.Matching.Should().ContainSingle().Which.Info.Should().Be(ability);
        }

        [Test]
        public async Task LootCatalogLoadFailure_LeavesTheTabRetryableOnTheNextEnsureLoadedCall()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "flaky_loot", "Flaky Loot"));
            var store = new CreatureValueStore(document.Root);
            var attempt = 0;
            var table = new CreatureLootTableInfo(
                "RECOVERED_TABLE", "Recovered Table", false,
                Array.Empty<CreatureLootTableItemInfo>());

            using var loot = new CreatureLootViewModel(
                store,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                tableLoader: () =>
                {
                    attempt++;
                    return attempt == 1
                        ? Task.FromException<IReadOnlyList<CreatureLootTableInfo>>(new InvalidOperationException("boom"))
                        : Task.FromResult<IReadOnlyList<CreatureLootTableInfo>>([table]);
                });

            await loot.EnsureLoadedAsync();
            loot.IsLoaded.Should().BeFalse(
                "a faulted load must stay retryable rather than permanently disabling the tab");
            loot.HasLoadError.Should().BeTrue();

            await loot.EnsureLoadedAsync();
            attempt.Should().Be(2, "re-entering the tab after a failure must retry the load");
            loot.IsLoaded.Should().BeTrue();
            loot.HasLoadError.Should().BeFalse();
            loot.Tables.Should().ContainSingle().Which.Should().Be(table);
        }

        [AvaloniaTest]
        public async Task CreatureEditor_DefersAppearanceProjectionUntilAppearanceIsSelected()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "defer_appear", "Deferred Appearance"));
            var appearanceLoads = 0;
            var referenceChoiceLoads = new Dictionary<string, int>(StringComparer.Ordinal);
            using var editor = new CreatureEditorViewModel(
                document.Root,
                Path.Combine(Path.GetTempPath(), "utc", "defer_appear.utc.json"),
                "defer_appear",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                key =>
                {
                    referenceChoiceLoads[key] = referenceChoiceLoads.GetValueOrDefault(key) + 1;
                    return Array.Empty<BehaviorChoice>();
                },
                null,
                null,
                _ => null,
                null,
                appearanceOptionsLoader: () =>
                {
                    appearanceLoads++;
                    return
                    [
                        new AppearanceOption("1", "Deferred Model", "row 1", CreatureAppearanceId: 1)
                    ];
                });

            appearanceLoads.Should().Be(0);
            editor.AppearanceGallery!.Tiles.Should().BeEmpty();
            referenceChoiceLoads.Should().NotContainKey(CreatureChoiceKeys.Dialogs);
            referenceChoiceLoads.Should().NotContainKey(CreatureChoiceKeys.DialogDefinitions);
            referenceChoiceLoads.Should().NotContainKey(CreatureChoiceKeys.GuildStores);
            editor.AppearanceRows
                .Where(row => row.Definition.IsSearchable)
                .Should().OnlyContain(row => !row.AreChoicesLoaded,
                    "off-screen appearance choices should resolve only when their picker opens");

            editor.IsAppearanceTabSelected = true;
            await editor.EnsureAppearanceCatalogLoadedAsync();

            appearanceLoads.Should().Be(1);
            editor.AppearanceGallery.Tiles.Should().ContainSingle();
        }

        [AvaloniaTest]
        public async Task AppearanceDetails_ShowTheirProgressivePickersWithoutChooseActions()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "appear_details", "Appearance Details"));
            using var editor = new CreatureEditorViewModel(
                document.Root,
                Path.Combine(Path.GetTempPath(), "utc", "appear_details.utc.json"),
                "appear_details",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                key => key switch
                {
                    CreatureChoiceKeys.Races => [new BehaviorChoice(6, "Human")],
                    CreatureChoiceKeys.Portraits =>
                        [new BehaviorChoice(4, "Human portrait", "po_human")],
                    CreatureChoiceKeys.SoundSets => [new BehaviorChoice(85, "Monodrone")],
                    _ => Array.Empty<BehaviorChoice>()
                },
                null,
                null,
                _ => null,
                null);

            editor.SelectedAppearanceSectionIndex = 1;
            await editor.EnsureAppearanceDetailsLoadedAsync();

            var race = editor.AppearanceRows.Single(row => row.Definition.Name == "Race");
            var portrait = editor.AppearanceRows.Single(row => row.Definition.Name == "PortraitId");
            var soundSet = editor.AppearanceRows.Single(row => row.Definition.Name == "SoundSetFile");
            race.IsInlineSearchChoice.Should().BeTrue();
            race.IsSearchExpanded.Should().BeTrue();
            portrait.IsInlineGallery.Should().BeTrue();
            portrait.IsPopupGallery.Should().BeFalse();
            soundSet.IsInlineSearchChoice.Should().BeTrue();
            soundSet.IsSearchExpanded.Should().BeTrue();
        }

        [Test]
        public void AbilitiesTab_UsesMetadataFiltersAndVirtualizedProgressiveResults()
        {
            var view = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Views", "CreatureEditorView.axaml"));

            view.Should().Contain("AudienceFilters");
            view.Should().Contain("SkillFilters");
            view.Should().Contain("VirtualizingStackPanel");
            view.Should().Contain("OnAbilityScrollChanged");
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
        public void LootRead_PreservesContiguousRowsBeyondNinetyNine()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_many", "Many Loot Rows"));
            var store = new CreatureValueStore(document.Root);
            for (var index = 1; index <= 100; index++)
                store.Locals.SetString($"LOOT_TABLE_{index}", $"TABLE_{index},100,1");

            var read = store.ReadLoot(out var hasGap);

            hasGap.Should().BeFalse();
            read.Should().HaveCount(100);
            read[^1].TableId.Should().Be("TABLE_100");
            store.WriteLoot(read);
            store.Locals.GetString("LOOT_TABLE_100").Should().Be("TABLE_100,100,1");
        }

        [Test]
        public void AiProfile_UsesNumericFallbackAndSynchronizesBothLocals()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "ai_profile", "AI Profile"));
            var store = new CreatureValueStore(document.Root);
            store.Locals.SetString(NPCAI.ProfileLocalVariable, "not-a-profile");
            store.Locals.SetInt(NPCAI.ProfileIdLocalVariable, (int)AIProfileType.BeastCompanion);
            var viewModel = new CreatureAiViewModel(store, (_, mutation) =>
            {
                mutation();
                return true;
            });

            viewModel.Profile.Should().Be(AIProfileType.BeastCompanion.ToString());
            viewModel.Profile = AIProfileType.Generic.ToString();
            store.Locals.GetString(NPCAI.ProfileLocalVariable).Should().BeNull();
            store.Locals.GetInt(NPCAI.ProfileIdLocalVariable).Should().BeNull();

            viewModel.Profile = AIProfileType.DroidCompanion.ToString();
            store.Locals.GetString(NPCAI.ProfileLocalVariable)
                .Should().Be(AIProfileType.DroidCompanion.ToString());
            store.Locals.GetInt(NPCAI.ProfileIdLocalVariable)
                .Should().Be((int)AIProfileType.DroidCompanion);
        }

        [Test]
        public void PermanentCreatureVisuals_IncludeOnlyReferencedDurationEffects()
        {
            var visualEffects = new Dictionary<int, string>
            {
                [1] = "Vfx_Fnf_Burst",
                [2] = "Vfx_Dur_Aura",
                [3] = "Vfx_Dur_Missing_Metadata"
            };
            var references = new Dictionary<int, VisualEffectReferenceInfo>
            {
                [1] = Reference(1, "FNF"),
                [2] = Reference(2, "DUR")
            };

            var choices = CreatureVisualEffectCatalog.Build(visualEffects, references);

            choices.Should().ContainSingle().Which.Value.Should().Be(2);
            choices[0].ImageUrl.Should().Be("https://example.test/2.jpg");
            CreatureVisualEffectCatalog.Build(
                    visualEffects,
                    new Dictionary<int, VisualEffectReferenceInfo>())
                .Should().BeEmpty("missing group metadata must fail closed");

            static VisualEffectReferenceInfo Reference(int id, string group) => new(
                id,
                group,
                $"Vfx_{group}_{id}",
                "test",
                "body",
                "blue",
                "test",
                "https://example.test/source",
                $"https://example.test/{id}.jpg");
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
            row.EditorTitle.Should().Be("Drop 1");
            row.ConfigurationSummary.Should().Be("75% chance \u00B7 2 pulls");
            var picker = row.TablePicker;
            picker.Should().BeAssignableTo<BehaviorRowViewModel>();
            picker.AreChoicesLoaded.Should().BeFalse();
            picker.FilteredChoices.Should().BeEmpty();
            picker.SelectedChoiceDisplay.Should().Be("Table 400");
            picker.SelectedChoiceIdentifier.Should().Be("TABLE_400");

            picker.OpenSearchCommand.Execute(null);

            picker.AreChoicesLoaded.Should().BeTrue();
            picker.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize + 1,
                "the stored table remains visible above the first progressive page");
            picker.ChoiceSearchText = "TABLE_321";
            var match = picker.FilteredChoices.Should().ContainSingle().Which;
            match.Display.Should().Be("Table 321");
            match.Identifier.Should().Be("TABLE_321");

            picker.PickChoiceCommand.Execute(match);

            store.Locals.GetString("LOOT_TABLE_1").Should().Be("TABLE_321,75,2");
            row.Table.Should().BeSameAs(tables[321]);
            viewModel.SelectedEntry.Should().BeSameAs(row);
            viewModel.PreviewTitle.Should().Be("Table 321");
            picker.IsSearchExpanded.Should().BeFalse();
            picker.FilteredChoices.Should().BeEmpty();
        }

        [Test]
        public void AddingALootDrop_WaitsForARealTableSelectionBeforeWriting()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_add", "Loot Add"));
            var store = new CreatureValueStore(document.Root);
            var table = new CreatureLootTableInfo(
                "TEST_TABLE", "Test Table", false, Array.Empty<CreatureLootTableItemInfo>());
            using var viewModel = new CreatureLootViewModel(store, (_, mutation) =>
            {
                mutation();
                return true;
            }, new[] { table });

            viewModel.AddCommand.Execute(null);

            var row = viewModel.Entries.Should().ContainSingle().Which;
            row.IsPending.Should().BeTrue();
            row.TablePicker.IsSearchExpanded.Should().BeTrue();
            store.Locals.GetString("LOOT_TABLE_1").Should().BeNullOrEmpty();

            var selection = row.TablePicker.FilteredChoices.Should().ContainSingle().Which;
            row.TablePicker.PickChoiceCommand.Execute(selection);

            row.IsPending.Should().BeFalse();
            store.Locals.GetString("LOOT_TABLE_1").Should().Be("TEST_TABLE,100,1");
        }

        [Test]
        public void EmptyRegisteredLootTables_AreSurfacedAsInvalidConfiguration()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_empty", "Loot Empty"));
            var store = new CreatureValueStore(document.Root);
            store.Locals.SetString("LOOT_TABLE_1", "EMPTY_TABLE,100,1");
            var table = new CreatureLootTableInfo(
                "EMPTY_TABLE", "Empty Table", false, Array.Empty<CreatureLootTableItemInfo>());
            using var viewModel = new CreatureLootViewModel(store, (_, mutation) =>
            {
                mutation();
                return true;
            }, new[] { table });

            viewModel.HasWarning.Should().BeTrue();
            viewModel.Warning.Should().Contain("has no items");
            viewModel.HasPreviewItems.Should().BeFalse();
        }

        [Test]
        public void PerKillLootEstimate_UsesWeightsQuantitiesAndResolvedItemNames()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "loot_estimate", "Loot Estimate"));
            var store = new CreatureValueStore(document.Root);
            store.Locals.SetString("LOOT_TABLE_1", "TEST_TABLE,50,2");
            var table = new CreatureLootTableInfo(
                "TEST_TABLE",
                "Test Table",
                false,
                new[]
                {
                    new CreatureLootTableItemInfo("item_a", 1, 1, false),
                    new CreatureLootTableItemInfo("item_b", 1, 3, false)
                });
            using var viewModel = new CreatureLootViewModel(store, (_, mutation) =>
            {
                mutation();
                return true;
            }, new[] { table }, resolveItemName: resRef => resRef == "item_a" ? "Item A" : "Item B");

            viewModel.PreviewItems.Select(item => item.DisplayName)
                .Should().Equal("Item A", "Item B");
            viewModel.PreviewItems.Select(item => item.RelativeChance)
                .Should().Equal(new[] { 50d, 50d },
                    "each item shows its weight relative to the selected table's total weight");
            viewModel.PreviewItems.Should().OnlyContain(item => item.WeightDisplay == "Weight 1 · 50%");
            viewModel.ExpectedItems.Should().ContainEquivalentOf(
                new CreatureExpectedLootItemViewModel("Item A", "item_a", 0.5));
            viewModel.ExpectedItems.Should().ContainEquivalentOf(
                new CreatureExpectedLootItemViewModel("Item B", "item_b", 1.0));
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
        public async Task CreatureEditorView_RendersItsConditionalVariablesTab()
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
                        .ToList(),
                    equipmentSearch: (query, slot, skip, take) =>
                        Task.FromResult<IReadOnlyList<CreatureEquipmentChoice>>(slot == 2
                            ? Enumerable.Range(skip, Math.Max(0, Math.Min(take, 60 - skip)))
                                .Select(index => new CreatureEquipmentChoice(
                                    $"armor_{index:D3}", $"Armor {index:D3}", 16, 2))
                                .ToList()
                            : Array.Empty<CreatureEquipmentChoice>()));
                var view = new CreatureEditorView { DataContext = editor };
                var window = new Window { Width = 1280, Height = 800, Content = view };

                window.Show();
                Dispatcher.UIThread.RunJobs();
                view.GetVisualDescendants().Should().NotBeEmpty();
                var tabs = view.FindControl<TabControl>("CreatureTabs");
                tabs.Should().NotBeNull();
                var tabItems = tabs!.Items.Cast<TabItem>().ToList();
                tabItems.Select(tab => tab.Header?.ToString()).Should().Equal(
                    "Basic", "Behavior", "Variables", "Appearance", "Equipment", "Stats", "Resistances", "Abilities", "AI", "Loot");
                var flagsCard = view.FindControl<Border>("CreatureFlagsCard");
                flagsCard.Should().NotBeNull();
                flagsCard!.IsVisible.Should().BeTrue("Flags belong to the initially selected Basic tab");
                FindVisual<TabControl>(view, "CreatureAppearanceSections").Should().BeNull(
                    "the cold-open Basic surface must not realize the hidden Appearance editor");
                FindVisual<ListBox>(view, "CreatureEquipmentSlots").Should().BeNull(
                    "the cold-open Basic surface must not realize hidden equipment pickers");
                FindVisual<Border>(view, "CreatureAvailableAbilities").Should().BeNull(
                    "the cold-open Basic surface must not realize the hidden ability catalog");
                FindVisual<ListBox>(view, "CreatureLootEntries").Should().BeNull(
                    "the cold-open Basic surface must not realize the hidden loot editor");
                var basicTab = tabItems.Single(tab => tab.Header?.ToString() == "Basic");
                basicTab.Content.Should().BeAssignableTo<Control>().Which
                    .GetVisualDescendants().Should().Contain(flagsCard,
                        "the Flags card is content owned by Basic rather than Stats");
                var variablesTab = tabItems.Single(tab => tab.Header?.ToString() == "Variables");
                variablesTab.IsVisible.Should().BeFalse();
                editor.SelectedRole.Id.Should().Be(CreatureRoleCatalog.StandardId);
                var standardRole = editor.RoleList.First(item => item.IsSelectable);
                standardRole.Text.Should().Be("Standard");
                standardRole.IsSelected.Should().BeTrue();

                tabs.SelectedIndex = 1;
                editor.ChooseRoleCommand.Execute(CreatureRoleCatalog.All.Single(
                    role => role.Id == CreatureRoleCatalog.CustomId));
                Dispatcher.UIThread.RunJobs();
                editor.ShowsVariablesTab.Should().BeTrue();
                variablesTab.IsVisible.Should().BeTrue();

                tabs.SelectedItem = variablesTab;
                Dispatcher.UIThread.RunJobs();
                variablesTab.Content.Should().BeOfType<VariablesSectionView>(
                    "Custom variables use the shared editor on their own tab");

                tabs.SelectedItem = tabItems.Single(tab => tab.Header?.ToString() == "Appearance");
                editor.IsAppearanceTabSelected = true;
                Dispatcher.UIThread.RunJobs();

                var appearanceSections = FindVisual<TabControl>(view, "CreatureAppearanceSections");
                appearanceSections.Should().NotBeNull();
                appearanceSections!.Items.Cast<TabItem>().Select(tab => tab.Header?.ToString()).Should().Equal(
                    "Model", "Details", "Body");
                for (var index = 0; index < 3; index++)
                {
                    appearanceSections!.SelectedIndex = index;
                    Dispatcher.UIThread.RunJobs();
                }

                tabs.SelectedIndex = 4;
                Dispatcher.UIThread.RunJobs();
                var equipmentSlots = FindVisual<ListBox>(view, "CreatureEquipmentSlots");
                equipmentSlots.Should().NotBeNull();
                equipmentSlots!.ItemCount.Should().Be(14);
                equipmentSlots.SelectedItem.Should().BeSameAs(editor.EquipmentSlots.SelectedSlot);
                var equipmentPicker = FindVisual<BehaviorRowView>(view, "CreatureEquipmentPicker");
                equipmentPicker.Should().NotBeNull();
                equipmentPicker!.ShowLabel.Should().BeFalse(
                    "the item gallery should extend left into the redundant selected-slot label gutter");
                editor.EquipmentSlots.SelectedSlot!.GalleryChoices.Should()
                    .HaveCount(BehaviorRowViewModel.GalleryPageSize,
                        "the first equipment page is already present when the slot is shown");
                view.GetVisualDescendants().OfType<Button>()
                    .Where(button => button.IsVisible &&
                                     button.Content?.ToString()?.StartsWith("Choose", StringComparison.Ordinal) == true)
                    .Should().BeEmpty("equipment options are the work pane, not a second button behind it");
                FindVisual<Border>(view, "CreatureEquipmentStats").Should().NotBeNull(
                    "the selected item uses the reusable read-only stat summary");
                FindVisual<TextBlock>(view, "CreatureEquipmentNoStats").Should().NotBeNull(
                    "an equipped item with no gameplay properties must say so explicitly");

                tabs.SelectedItem = tabItems.Single(tab => tab.Header?.ToString() == "Abilities");
                await editor.Abilities.EnsureLoadedAsync();
                Dispatcher.UIThread.RunJobs();
                var availableAbilities = FindVisual<Border>(view, "CreatureAvailableAbilities");
                var assignedAbilities = FindVisual<ScrollViewer>(view, "CreatureAssignedAbilities");
                availableAbilities.Should().NotBeNull();
                assignedAbilities.Should().NotBeNull();
                Grid.GetColumn(availableAbilities!).Should().Be(0,
                    "available abilities belong in the left work pane");
                Grid.GetColumn(assignedAbilities!).Should().Be(1,
                    "assigned abilities belong in the right summary pane");
                var audienceFilter = FindVisual<ComboBox>(view, "CreatureAbilityAudienceFilter");
                audienceFilter.Should().NotBeNull();
                audienceFilter!.Bounds.Width.Should().BeGreaterThanOrEqualTo(300,
                    "the longest ability audience option must remain readable in the field and its popup");
                var skillFilter = FindVisual<ComboBox>(view, "CreatureAbilitySkillFilter");
                skillFilter.Should().NotBeNull();
                skillFilter!.Bounds.Width.Should().BeGreaterThanOrEqualTo(300,
                    "the longest ability skill option must remain readable in the field and its popup");
                skillFilter.Bounds.Width.Should().BeApproximately(audienceFilter.Bounds.Width, 0.1,
                    "both ability filters should receive the full card width");
                var abilityButton = view.GetVisualDescendants()
                    .OfType<Button>()
                    .FirstOrDefault(button => button.DataContext is CreatureAbilityChoiceViewModel);
                abilityButton.Should().NotBeNull("registered abilities must render as actionable choices");
                abilityButton!.IsEnabled.Should().BeTrue();
                abilityButton.Command.Should().NotBeNull();
                abilityButton.CommandParameter.Should().BeOfType<CreatureAbilityInfo>();
                var selectedAbility = (CreatureAbilityInfo)abilityButton.CommandParameter!;

                abilityButton.Command!.Execute(abilityButton.CommandParameter);
                Dispatcher.UIThread.RunJobs();
                editor.Abilities.Assigned.Should().ContainSingle(entry => entry.FeatId == selectedAbility.FeatId);

                tabs.SelectedItem = tabItems.Single(tab => tab.Header?.ToString() == "Loot");
                await editor.Loot.EnsureLoadedAsync();
                editor.Loot.AddCommand.Execute(null);
                Dispatcher.UIThread.RunJobs();
                var lootEntry = editor.Loot.Entries.Should().ContainSingle().Which;
                await lootEntry.TablePicker.OpenSearchCommand.ExecuteAsync(null);
                var longestTable = lootEntry.TablePicker.Choices
                    .OrderByDescending(choice => choice.Display.Length)
                    .First();
                lootEntry.TablePicker.PickChoiceCommand.Execute(longestTable);
                Dispatcher.UIThread.RunJobs();
                var lootList = FindVisual<ListBox>(view, "CreatureLootEntries");
                lootList.Should().NotBeNull();
                lootList!.SelectedItem.Should().BeSameAs(lootEntry);
                ScrollViewer.GetHorizontalScrollBarVisibility(lootList).Should().Be(ScrollBarVisibility.Auto,
                    "long loot-table names must be reachable without widening the configured-drop pane");
                var lootListItem = view.GetVisualDescendants().OfType<ListBoxItem>()
                    .SingleOrDefault(item => ReferenceEquals(item.DataContext, lootEntry));
                lootListItem.Should().NotBeNull();
                lootListItem!.Bounds.Height.Should().BeLessThan(80,
                    "opening the loot-table picker must not stretch its summary row");
                var lootScroll = lootList.GetVisualDescendants().OfType<ScrollViewer>().Single();
                lootScroll.Extent.Width.Should().BeGreaterThan(lootScroll.Viewport.Width,
                    "the longest loot-table name must contribute its natural width to the scroll extent");
                var rightmostOffset = lootScroll.Extent.Width - lootScroll.Viewport.Width;
                lootScroll.Offset = new Avalonia.Vector(lootScroll.Extent.Width, lootScroll.Offset.Y);
                Dispatcher.UIThread.RunJobs();
                lootScroll.Offset.X.Should().BeApproximately(rightmostOffset, 0.5,
                    "the horizontal scrollbar must reach the actual end of the loot-table name");
                var lootPicker = view.GetVisualDescendants().OfType<SearchableChoicePickerView>()
                    .SingleOrDefault(picker => ReferenceEquals(picker.DataContext, lootEntry.TablePicker));
                lootPicker.Should().NotBeNull();
                lootPicker!.Bounds.Width.Should().BeGreaterThanOrEqualTo(300,
                    "the shared loot-table picker belongs in the full-width detail pane");

                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [AvaloniaTest]
        public void CreaturePreview_FacesTheDefaultCameraAndAlwaysPublishesAnimationSegments()
        {
            using var editor = OpenPreviewEditor(new RenderModel { Name = "facing_test" });

            editor.PreviewScene.Should().NotBeNull();
            editor.PreviewScene!.Instances.Single().Orientation.Should().Be(new Vector2(-1f, 0f));
            editor.PreviewAnimations.Select(option => option.Display)
                .Should().Equal("Idle", "Walk", "Attack");
        }

        [AvaloniaTest]
        public void CreatureAppearance_HidesBodyTabForNonDynamicModelsAndRepairsSelection()
        {
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "body_visibility", "Body Visibility"));
            var options = new[]
            {
                new AppearanceOption("6", "Dynamic Human", "row 6", CreatureAppearanceId: 6),
                new AppearanceOption("7", "Full Body Droid", "row 7", CreatureAppearanceId: 7)
            };
            using var editor = new CreatureEditorViewModel(
                document.Root,
                Path.Combine(CorpusLocator.ModuleDirectory, "utc", "body_visibility.utc.json"),
                "body_visibility",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                null,
                null,
                null,
                id => id == 6
                    ? new AppearanceRow(6, "DYNAMIC_HUMAN", "Dynamic Human", "P", "H", null)
                    : new AppearanceRow(7, "FULL_BODY_DROID", "Full Body Droid", "S", "c_droid", null),
                null,
                appearanceOptions: options);
            var view = new CreatureEditorView { DataContext = editor };
            var window = new Window { Width = 1280, Height = 800, Content = view };

            window.Show();
            Dispatcher.UIThread.RunJobs();
            var editorTabs = view.FindControl<TabControl>("CreatureTabs");
            editorTabs.Should().NotBeNull();
            editorTabs!.SelectedItem = editorTabs.Items.Cast<TabItem>()
                .Single(tab => tab.Header?.ToString() == "Appearance");
            editor.IsAppearanceTabSelected = true;
            Dispatcher.UIThread.RunJobs();
            var appearanceSections = FindVisual<TabControl>(view, "CreatureAppearanceSections");
            appearanceSections.Should().NotBeNull();
            var bodyTab = appearanceSections!.Items.Cast<TabItem>()
                .Single(tab => tab.Header?.ToString() == "Body");
            bodyTab.IsVisible.Should().BeTrue("the starting appearance is dynamic");

            appearanceSections.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();
            editor.SelectedAppearanceSectionIndex.Should().Be(2);

            editor.AppearanceGallery!.Highlighted = editor.AppearanceGallery.Tiles.Single(tile =>
                tile.Option.CreatureAppearanceId == 7);
            Dispatcher.UIThread.RunJobs();

            bodyTab.IsVisible.Should().BeFalse("fixed models have no editable segmented body parts");
            editor.SelectedAppearanceSectionIndex.Should().Be(0,
                "changing away from a dynamic model cannot leave a hidden section selected");
            appearanceSections.SelectedIndex.Should().Be(0);

            editorTabs!.SelectedItem = editorTabs.Items.Cast<TabItem>()
                .Single(tab => tab.Header?.ToString() == "Equipment");
            Dispatcher.UIThread.RunJobs();
            var equipmentEditor = FindVisual<Grid>(view, "CreatureEquipmentEditor");
            equipmentEditor.Should().NotBeNull();
            equipmentEditor!.IsVisible.Should().BeTrue(
                "UTC equipment slots exist even when the full-body model does not render them");
            FindVisual<ListBox>(view, "CreatureEquipmentSlots")!.ItemCount.Should().Be(14);

            window.Close();
            Dispatcher.UIThread.RunJobs();
        }

        [AvaloniaTest]
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
            var editor = new CreatureEditorViewModel(
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
            DrainUntil(() => !editor.IsModelPreviewLoading);
            return editor;
        }

        private static void DrainUntil(Func<bool> condition)
        {
            for (var attempt = 0; attempt < 200 && !condition(); attempt++)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
            }

            Dispatcher.UIThread.RunJobs();
            condition().Should().BeTrue("the background creature preview should publish promptly");
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
            CreatureEditorLayout.Basic.Should().Contain(field => field.Name == "WalkRate");
            var movedFields = new[] { "Race", "FactionID", "Conversation" };
            CreatureEditorLayout.Basic.Should().NotContain(field => movedFields.Contains(field.Name));
            CreatureEditorLayout.Appearance.Should().Contain(field => field.Name == "Race");
            CreatureEditorLayout.Appearance.Should().NotContain(field => field.Name == "WalkRate");
            CreatureEditorLayout.Appearance.Should().NotContain(field => field.Name == "Appearance_Type",
                "the model uses the shared paged appearance gallery instead of a generic choice row");
            CreatureEditorLayout.Ai.Should().ContainSingle(field => field.Name == "FactionID");
            CreatureEditorLayout.DialogRole.Should().Contain(field => field.Name == "Conversation");
            CreatureEditorLayout.Flags.Select(field => field.Name).Should().Equal(
                "Plot", "IsImmortal", "NoPermDeath", "Disarmable");
            CreatureEditorLayout.Basic.Concat(CreatureEditorLayout.Flags)
                .Should().Contain(field => field.Name == "Plot",
                    "the separate Flags card is part of the Basic tab");
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
        public async Task CreatureBodyParts_AllowOptionalBeltsAndShouldersToBeRemoved()
        {
            var scratch = Path.Combine(
                Path.GetTempPath(), "swlor-creature-parts-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                foreach (var resRef in new[]
                         {
                             "pfh0_belt001", "pfh0_chest001", "pfh0_shol001", "pfh0_shor001",
                             "pfh0_bicepl001", "pfh0_bicepr001"
                         })
                {
                    File.WriteAllBytes(Path.Combine(scratch, resRef + ".mdl"), Array.Empty<byte>());
                }

                var resources = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[] { new ResourceIndex.HakLayer("fixture", scratch) });
                var catalog = new ArmorPartCatalog(resources);
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
                    resources,
                    null,
                    id => id == 10001
                        ? new AppearanceRow(id, "DYNAMIC_CHISS", "Dynamic Chiss", "P", "H", null)
                        : null,
                    catalog);

                await editor.BodyParts.EnsureLoadedAsync();

                var belt = editor.BodyParts.Structure.Single(cell => cell.Label == "Belt");
                belt.Options.Should().Equal(0, 1);
                editor.BodyParts.Structure.Single(cell => cell.Label == "Torso")
                    .Options.Should().Equal(new[] { 1 },
                        "required body segments must not gain a removal choice");

                var shoulder = editor.BodyParts.Limbs.Single(pair => pair.Label == "Shoulder");
                shoulder.Left.Options.Should().Equal(0, 1);
                shoulder.Right.Options.Should().Equal(0, 1);
                editor.BodyParts.Limbs.Single(pair => pair.Label == "Bicep")
                    .Left.Options.Should().Equal(new[] { 1 },
                        "only optional limbs use the engine's no-model value");

                belt.SelectedOption = 0;
                store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_Belt").Should().Be(0);
                store.GetInteger(BehaviorFieldStorage.Field, "xBodyPart_Belt").Should().Be(0);

                editor.BodyParts.MirrorRightFromLeft = true;
                shoulder.Left.SelectedOption = 0;
                store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_LShoul").Should().Be(0);
                store.GetInteger(BehaviorFieldStorage.Field, "BodyPart_RShoul").Should().Be(0);
                store.GetInteger(BehaviorFieldStorage.Field, "xBodyPart_LShoul").Should().Be(0);
                store.GetInteger(BehaviorFieldStorage.Field, "xBodyPart_RShoul").Should().Be(0);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void EquipmentSlots_UseBaseItemMasksAndLoadOneProgressivePickerAtATime()
        {
            var catalogLoads = 0;
            var detailLoads = 0;
            var pageRequests = new List<(string Query, int Slot, int Skip, int Take)>();
            var weaponStats = new[]
            {
                new ItemStatSummaryGroup("Combat", new[]
                {
                    new ItemStatSummaryEntry("DMG", "8")
                })
            };
            var equipment = Enumerable.Range(0, 500)
                .Select(index => new CreatureEquipmentChoice(
                    $"armor_{index:D3}", $"Armor {index:D3}", 16, 2))
                .Append(new CreatureEquipmentChoice(
                    "right_hand_test", "Right Hand Test", 7, 0x30, weaponStats))
                .ToList();

            Task<IReadOnlyList<CreatureEquipmentChoice>> Equipment()
            {
                catalogLoads++;
                return Task.FromResult<IReadOnlyList<CreatureEquipmentChoice>>(equipment);
            }

            CreatureEquipmentChoice? Details(string resRef)
            {
                detailLoads++;
                return equipment.FirstOrDefault(choice => choice.ResRef == resRef);
            }

            Task<IReadOnlyList<CreatureEquipmentChoice>> Search(
                string query,
                int slot,
                int skip,
                int take)
            {
                pageRequests.Add((query, slot, skip, take));
                return Task.FromResult<IReadOnlyList<CreatureEquipmentChoice>>(equipment
                    .Where(choice => (choice.EquipableSlots & slot) != 0)
                    .Where(choice => string.IsNullOrWhiteSpace(query) ||
                                     choice.Display.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                     choice.ResRef.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Skip(skip)
                    .Take(take)
                    .ToList());
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
                equipmentChoices: Equipment,
                equipmentDetails: Details,
                equipmentSearch: Search);

            catalogLoads.Should().Be(0,
                "the page source must not materialize the old all-items catalog");
            pageRequests.Should().BeEmpty(
                "opening a creature must not load equipment before its tab is visited");
            editor.IsEquipmentTabSelected = true;
            pageRequests.Should().ContainSingle().Which.Should().Be(
                (string.Empty, 2, 0, BehaviorRowViewModel.GalleryPageSize + 1));
            editor.EquipmentSlots.Slots.Select(slot => slot.Label).Should().Equal(
                "Armor", "Helmet", "Cloak", "Right Hand", "Left Hand", "Boots", "Arms",
                "Neck", "Belt", "Left Ring", "Right Ring", "Arrows", "Bolts", "Bullets");
            editor.EquipmentSlots.SelectedSlot.Should().BeSameAs(
                editor.EquipmentSlots.Slots.Single(slot => slot.Label == "Armor"));
            editor.EquipmentSlots.SelectedSlot!.AreChoicesLoaded.Should().BeTrue();
            editor.EquipmentSlots.SelectedSlot.IsInlineGallery.Should().BeTrue();
            editor.EquipmentSlots.SelectedSlot.GalleryTileWidth.Should().Be(104,
                "equipment uses the shared compact gallery tiles so the pane fits two columns");
            editor.EquipmentSlots.SelectedSlot.GalleryThumbnailHeight.Should().Be(78);
            editor.EquipmentSlots.SelectedSlot.GalleryChoices.Should()
                .HaveCount(BehaviorRowViewModel.GalleryPageSize);
            editor.EquipmentSlots.Slots.Where(slot => slot.Label != "Armor")
                .Should().OnlyContain(slot => !slot.AreChoicesLoaded);

            var armor = editor.EquipmentSlots.Slots.Single(slot => slot.Label == "Armor");
            catalogLoads.Should().Be(0);
            armor.GalleryChoices.Should().HaveCount(BehaviorRowViewModel.GalleryPageSize);
            armor.GalleryChoices.Should().OnlyContain(choice => choice.HasArtwork);
            armor.CanLoadMoreGallery.Should().BeTrue();
            armor.LoadMoreGalleryCommand.Execute(null);
            armor.GalleryChoices.Should().HaveCount(BehaviorRowViewModel.GalleryPageSize * 2);
            pageRequests.Last().Should().Be(
                (string.Empty, 2, BehaviorRowViewModel.GalleryPageSize,
                    BehaviorRowViewModel.GalleryPageSize + 1));
            editor.EquipmentSlots.Slots.Where(slot => slot.Label != "Armor")
                .Should().OnlyContain(slot => !slot.AreChoicesLoaded);

            var mainHand = editor.EquipmentSlots.Slots.Single(slot => slot.Label == "Right Hand");
            editor.EquipmentSlots.SelectedSlot = mainHand;
            catalogLoads.Should().Be(0);
            pageRequests.Last().Should().Be(
                (string.Empty, 16, 0, BehaviorRowViewModel.GalleryPageSize + 1),
                "selecting a new slot requests only that slot's first page");
            mainHand.AreChoicesLoaded.Should().BeTrue();
            mainHand.GalleryChoices.Select(choice => choice.StringValue)
                .Should().Equal(new[] { "right_hand_test" },
                    "baseitems.2da's equipment mask determines which slot offers an item");
            mainHand.GalleryChoices.Single().Summary.Should().Contain("DMG 8",
                "candidate stats are visible before the builder equips the item");

            editor.EquipmentSlots.SelectedSlot = armor;
            armor.PickChoiceCommand.Execute(armor.GalleryChoices[3]);
            new CreatureValueStore(document.Root).EquippedResRef(2).Should().Be("armor_003");
            armor.CanClearChoice.Should().BeTrue();

            armor.ClearChoiceCommand.Execute(null);
            new CreatureValueStore(document.Root).EquippedResRef(2).Should().BeNull();
            armor.CanClearChoice.Should().BeFalse();

            var detailLoadsBeforeMainHand = detailLoads;
            editor.EquipmentSlots.SelectedSlot = mainHand;
            mainHand.PickChoiceCommand.Execute(mainHand.GalleryChoices.Single());
            detailLoads.Should().Be(detailLoadsBeforeMainHand + 1,
                "only the newly equipped blueprint is loaded for the details pane");
            mainHand.SelectedStatGroups.Should().ContainSingle()
                .Which.Entries.Should().ContainSingle(entry => entry.Label == "DMG" && entry.Value == "8");
        }

        [Test]
        public async Task EquipmentPicker_AwaitsTheCatalogWithoutBlockingItsOpenCommand()
        {
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<IReadOnlyList<CreatureEquipmentChoice>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "equipment_async", "Async Equipment"));
            var picker = new CreatureEquipmentPickerViewModel(
                "Armor",
                2,
                new CreatureValueStore(document.Root),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                async () =>
                {
                    started.TrySetResult(true);
                    return await release.Task.ConfigureAwait(false);
                },
                _ => null,
                () => { });

            var opening = picker.OpenSearchCommand.ExecuteAsync(null);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

            opening.IsCompleted.Should().BeFalse();
            picker.AreChoicesLoaded.Should().BeFalse();
            release.SetResult([
                new CreatureEquipmentChoice("armor_async", "Async Armor", 16, 2)
            ]);
            await opening.WaitAsync(TimeSpan.FromSeconds(2));

            picker.AreChoicesLoaded.Should().BeTrue();
            picker.GalleryChoices.Should().ContainSingle()
                .Which.StringValue.Should().Be("armor_async");
        }

        [Test]
        public async Task EquipmentPicker_AwaitsOnePagedSearchWithoutShowingAChooseControl()
        {
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<IReadOnlyList<CreatureEquipmentChoice>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "equip_page_async", "Paged Equipment"));
            using var picker = new CreatureEquipmentPickerViewModel(
                "Armor",
                2,
                new CreatureValueStore(document.Root),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                _ => null,
                () => { },
                searchChoices: async (query, skip, take) =>
                {
                    started.TrySetResult(true);
                    return await release.Task.ConfigureAwait(false);
                });

            var activation = picker.ActivateAsync();
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

            activation.IsCompleted.Should().BeFalse();
            picker.IsGalleryLoading.Should().BeTrue();
            picker.IsInlineGallery.Should().BeTrue();
            picker.IsSearchableChoice.Should().BeFalse(
                "a paged equipment gallery must never fall back to a Choose button while loading");

            release.SetResult([
                new CreatureEquipmentChoice("armor_page_async", "Paged Armor", 16, 2)
            ]);
            await activation.WaitAsync(TimeSpan.FromSeconds(2));

            picker.IsGalleryLoading.Should().BeFalse();
            picker.GalleryChoices.Should().ContainSingle()
                .Which.StringValue.Should().Be("armor_page_async");
        }

        [AvaloniaTest]
        public void EquipmentSelection_ComposesOnlyTheNewestCreaturePreviewOffTheUiThread()
        {
            using var firstSelectionStarted = new ManualResetEventSlim();
            using var releaseFirstSelection = new ManualResetEventSlim();
            using var autoReleaseCancellation = new CancellationTokenSource();
            Task? autoRelease = null;
            var resolverCalls = 0;
            var document = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc, "equipmentprev", "Equipment Preview"));
            var choices = new[]
            {
                new CreatureEquipmentChoice("armor_a", "Armor A", 16, 2),
                new CreatureEquipmentChoice("armor_b", "Armor B", 16, 2)
            };
            using var editor = new CreatureEditorViewModel(
                document.Root,
                Path.Combine(CorpusLocator.ModuleDirectory, "utc", "equipmentprev.utc.json"),
                "equipmentprev",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                null,
                null,
                creature =>
                {
                    Interlocked.Increment(ref resolverCalls);
                    var equipped = new CreatureValueStore(creature).EquippedResRef(2) ?? "none";
                    if (equipped == "armor_a")
                    {
                        firstSelectionStarted.Set();
                        releaseFirstSelection.Wait();
                    }

                    return new RenderModel { Name = equipped };
                },
                _ => null,
                null,
                equipmentChoices: () => Task.FromResult<IReadOnlyList<CreatureEquipmentChoice>>(choices));

            try
            {
                DrainUntil(() => !editor.IsModelPreviewLoading);
                editor.PreviewScene!.Instances.Single().Model!.Name.Should().Be("none");

                editor.IsEquipmentTabSelected = true;
                var armor = editor.EquipmentSlots.Slots.Single(slot => slot.Label == "Armor");
                DrainUntil(() => armor.GalleryChoices.Count == choices.Length);
                var armorA = armor.GalleryChoices.Single(choice => choice.StringValue == "armor_a");

                // A regressed synchronous selection releases itself after two seconds so the test
                // fails with a useful timing assertion instead of hanging the runner forever.
                autoRelease = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), autoReleaseCancellation.Token);
                        releaseFirstSelection.Set();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
                var elapsed = System.Diagnostics.Stopwatch.StartNew();
                armor.PickChoiceCommand.Execute(armorA);
                elapsed.Stop();
                elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(250),
                    "choosing equipment must not parse and compose the creature model on the UI thread");

                Dispatcher.UIThread.RunJobs();
                firstSelectionStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                var armorB = armor.GalleryChoices.Single(choice => choice.StringValue == "armor_b");
                armor.PickChoiceCommand.Execute(armorB);
                Dispatcher.UIThread.RunJobs();
                releaseFirstSelection.Set();
                DrainUntil(() => !editor.IsModelPreviewLoading);

                resolverCalls.Should().Be(3,
                    "only the initial model, active selection, and newest queued selection should compose");
                editor.PreviewScene!.Instances.Single().Model!.Name.Should().Be("armor_b",
                    "a slower previous equipment preview must not replace the latest selection");
            }
            finally
            {
                autoReleaseCancellation.Cancel();
                releaseFirstSelection.Set();
                autoRelease?.GetAwaiter().GetResult();
            }
        }

        [Test]
        public void BehaviorChoices_LoadVisibleRolePickersAndRoleRowsAreReused()
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
            var storeRows = editor.RoleRows
                .Where(row => row.Definition.Name.StartsWith("STORE_TAG_RANK_"))
                .ToList();
            storeRows.Should().OnlyContain(row => row.IsInlineSearchChoice && row.IsSearchExpanded);
            storeRows.Should().OnlyContain(row => !row.AreChoicesLoaded);

            editor.IsBehaviorTabSelected = true;
            DrainUntil(() => storeRows.All(row => row.AreChoicesLoaded));

            guildStoreLoads.Should().Be(1,
                "rows that use the same catalog must share its background load");
            storeRows.Should().OnlyContain(row =>
                row.FilteredChoices.Count == BehaviorRowViewModel.SearchPageSize);

            var presentation = CreatureRoleCatalog.All.Single(role => role.Id == "presentation");
            editor.ChooseRoleCommand.Execute(presentation);
            editor.ChooseRoleCommand.Execute(guildMaster);

            editor.RoleRows.Single(row => row.Definition.Name == "STORE_TAG_RANK_1")
                .Should().BeSameAs(firstStore, "revisiting a behavior reuses its already loaded rows");
            guildStoreLoads.Should().Be(1);
        }

        [Test]
        public void CustomRole_HasNoDecorativeTagline()
        {
            var custom = CreatureRoleCatalog.All.Single(role => role.Id == CreatureRoleCatalog.CustomId);

            custom.DisplayName.Should().Be("Custom");
            custom.Tagline.Should().BeNull();
        }

        [Test]
        public void CreatureRoles_DefaultToNeutralStandard()
        {
            var standard = CreatureRoleCatalog.Default;

            standard.Id.Should().Be(CreatureRoleCatalog.StandardId);
            standard.DisplayName.Should().Be("Standard");
            standard.Group.Should().BeNull();
            standard.Fields.Should().BeEmpty();
            standard.AllowsVariables.Should().BeFalse();
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

        private static T? FindVisual<T>(Control root, string name) where T : Control =>
            root.GetVisualDescendants().OfType<T>().SingleOrDefault(control => control.Name == name);
    }
}

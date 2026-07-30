using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// The permanent combination-audit gate for the item stat/requirement catalogs: every
    /// (PropertyId, Subtype) pair the Module\uti corpus actually stores must be accounted for by
    /// some catalog, every catalog subtype table must still match its 2da source, every baseitems
    /// row in use must classify the way the toolset expects, and the item editor must construct
    /// (and never mutate) every blueprint in the corpus. Re-derives every fact from the live 2da/
    /// corpus data rather than trusting hardcoded expectations, so drift in either fails loudly.
    /// </summary>
    [TestFixture]
    public class ItemCombinationAuditTests
    {
        private const int CastSpellPropertyId = 15;

        /// <summary>
        /// Dangling (PropertyId, Subtype) pairs the corpus still carries that nothing in the live
        /// game/editor writes anymore - preserved rather than migrated, per the verified audit.
        /// (137, 17) is an additional one found by re-verifying against the corpus rather than the
        /// audit's known list: Module\uti\compbon.uti.json ("Component Bonus: FP Up 4", a single
        /// crafting-container helper blueprint per its DescIdentified text) stores Detection (137)
        /// with Subtype 17 and CostTable 62 - Detection has no subtype table in itempropdef.2da and
        /// every other Detection entry in the corpus uses CostTable 41, so this is a one-off,
        /// preserve-only value rather than a real Detection reading.
        /// </summary>
        private static readonly HashSet<(int PropertyId, int SubtypeId)> AllowlistedPairs = new()
        {
            (94, 3), (94, 4), (94, 6), (131, 0), (137, 17)
        };

        /// <summary>
        /// Property ids that are themselves blank/reserved itempropdef.2da rows (146, 148) - any
        /// subtype the corpus carries for them is preserved-only. 103 (DamageStat), 20
        /// (DamageImmunity), 37 (Immunity), and 51 (Regeneration) were removed from the editor by
        /// owner decision; data preserved - the corpus's entries for all four stay untouched, just
        /// no longer offered by any catalog.
        /// </summary>
        // 61 is UnlimitedAmmo: every ranged weapon here has unlimited ammunition, so the property
        // decides nothing and the editor stopped offering it. Its 228 stored entries stay on disk.
        private static readonly HashSet<int> AllowlistedProperties = new() { 146, 148, 103, 20, 37, 51, 61 };

        private static string Sw2DaDirectory =>
            Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da");

        private static string UtiDirectory =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti");

        // ------------------------------------------------------------------
        // (a) Every stored (PropertyId, Subtype) pair is covered by some catalog.
        // ------------------------------------------------------------------

        [Test]
        public void CorpusPropertyCoverage_EveryStoredPropertySubtypePairIsAccountedFor()
        {
            var statPairs = new HashSet<(int PropertyId, int SubtypeId)>(
                ItemStatCatalog.All.Select(stat => (stat.PropertyId, Normalize(stat.SubtypeId))));
            var requirementPairs = new HashSet<(int PropertyId, int SubtypeId)>(
                ItemRequirementCatalog.All.Select(req => (req.PropertyId, Normalize(req.SubtypeId))));
            var multiEntryProperties = new HashSet<int>(
                ItemMultiEntryCatalog.All.Select(definition => definition.PropertyId));

            var uncovered = new Dictionary<(int PropertyId, int SubtypeId), int>();
            var files = Directory.EnumerateFiles(UtiDirectory, "*.json").ToList();
            files.Should().NotBeEmpty("the uti corpus should be present");

            foreach (var path in files)
            {
                var store = new ItemValueStore(UtiDocument.Load(path).Fields);
                foreach (var property in store.Properties)
                {
                    var pair = (property.PropertyId, property.SubtypeId);

                    if (statPairs.Contains(pair)) continue;
                    if (requirementPairs.Contains(pair)) continue;
                    if (multiEntryProperties.Contains(property.PropertyId)) continue;
                    if (ItemEngineLegacyCatalog.Contains(property.PropertyId)) continue;
                    if (property.PropertyId == CastSpellPropertyId) continue;
                    if (AllowlistedPairs.Contains(pair)) continue;
                    if (AllowlistedProperties.Contains(property.PropertyId)) continue;

                    uncovered[pair] = uncovered.GetValueOrDefault(pair) + 1;
                }
            }

            if (uncovered.Count > 0)
            {
                var report = string.Join("\n", uncovered
                    .OrderByDescending(entry => entry.Value)
                    .Select(entry =>
                        $"  property {entry.Key.PropertyId} subtype {entry.Key.SubtypeId}: {entry.Value} occurrence(s)"));
                Assert.Fail($"{uncovered.Count} uncovered (PropertyId, Subtype) pairs:\n{report}");
            }
        }

        /// <summary>-1 ("no subtype declared") and 0 (what the store actually writes) are the same thing.</summary>
        private static int Normalize(int subtypeId) => subtypeId < 0 ? 0 : subtypeId;

        // ------------------------------------------------------------------
        // (b) Every subtype table the catalogs expand matches its 2da exactly.
        // ------------------------------------------------------------------

        [Test]
        public void SubtypeTablesMatchTwoDa_ResistanceAndDroidStat()
        {
            // DamageStat (103) and WeaponDamageType (134) no longer expand inline in ItemStatCatalog
            // - 103 was removed from the editor by owner decision, and 134 moved to
            // ItemMultiEntryCatalog as an exclusive choice - so neither has a fixed subtype-id list
            // to check here anymore.
            var twoDa = new TwoDaService(Sw2DaDirectory);

            AssertSubtypeIdsMatch(
                twoDa, "iprp_resistance",
                ItemStatCatalog.ByGroup(ItemStatGroup.Resistance)
                    .Where(stat => stat.PropertyId == 133).Select(stat => stat.SubtypeId));

            AssertSubtypeIdsMatch(
                twoDa, "iprp_droidstat",
                ItemStatCatalog.ByGroup(ItemStatGroup.Droid)
                    .Where(stat => stat.PropertyId == 121).Select(stat => stat.SubtypeId));
        }

        [Test]
        public void WeaponDamageTypeExclusiveChoiceSubtypeTableMatchesTwoDaExactly()
        {
            var twoDa = new TwoDaService(Sw2DaDirectory);
            var definition = ItemMultiEntryCatalog.ByPropertyId(134)!;
            definition.IsExclusive.Should().BeTrue();

            var choices = ItemSubtypeChoiceCatalog.Read(twoDa, definition.SubtypeTableResRef, tlk: null);
            var table = twoDa.GetTable(definition.SubtypeTableResRef);
            var labeledRows = new List<int>();
            for (var row = 0; row < table.RowCount; row++)
            {
                if (!string.IsNullOrWhiteSpace(table.GetString(row, "Label")))
                    labeledRows.Add(row);
            }

            choices.Select(choice => (int)choice.Value).Should().BeEquivalentTo(labeledRows);
        }

        private static void AssertSubtypeIdsMatch(
            TwoDaService twoDa, string tableName, IEnumerable<int> catalogSubtypeIds)
        {
            var table = twoDa.GetTable(tableName);
            var labeledRows = new List<int>();
            for (var row = 0; row < table.RowCount; row++)
            {
                if (!string.IsNullOrWhiteSpace(table.GetString(row, "Label")))
                    labeledRows.Add(row);
            }

            catalogSubtypeIds.Should().BeEquivalentTo(labeledRows,
                $"the catalog's subtype ids for {tableName} must match its labeled 2da rows exactly");
        }

        // ------------------------------------------------------------------
        // (c) Every distinct BaseItem value in use classifies as expected.
        // ------------------------------------------------------------------

        /// <summary>
        /// Hand-verified against SWLOR_Haks/sw_2da/baseitems.2da and ItemFamilyClassifier's rules
        /// for every BaseItem value the Module\uti corpus was found to use (80 distinct rows).
        /// A row that appears in the corpus but not here fails loudly rather than silently passing.
        /// </summary>
        private static readonly IReadOnlyDictionary<int, ItemFamily> ExpectedFamilyByBaseItem =
            new Dictionary<int, ItemFamily>
            {
                [0] = ItemFamily.MeleeWeapon, // shortsword
                [1] = ItemFamily.MeleeWeapon, // longsword
                [2] = ItemFamily.MeleeWeapon, // battleaxe
                [3] = ItemFamily.MeleeWeapon, // bastardsword
                [6] = ItemFamily.RangedWeapon, // cannon
                [7] = ItemFamily.RangedWeapon, // rifle
                [10] = ItemFamily.MeleeWeapon, // halberd
                [11] = ItemFamily.RangedWeapon, // pistol
                [12] = ItemFamily.MeleeWeapon, // twobladedsword
                [13] = ItemFamily.MeleeWeapon, // greatsword
                [15] = ItemFamily.Miscellaneous, // torch
                [16] = ItemFamily.Armor, // armor
                [17] = ItemFamily.Helmet, // helmet
                [19] = ItemFamily.Accessory, // amulet
                [21] = ItemFamily.Accessory, // belt
                [22] = ItemFamily.MeleeWeapon, // dagger
                [24] = ItemFamily.Miscellaneous, // miscsmall
                [25] = ItemFamily.Miscellaneous, // bolt
                [26] = ItemFamily.Accessory, // boots
                [27] = ItemFamily.Miscellaneous, // bullet
                [28] = ItemFamily.MeleeWeapon, // club
                [29] = ItemFamily.Miscellaneous, // miscmedium
                [34] = ItemFamily.Miscellaneous, // misclarge
                [36] = ItemFamily.Accessory, // gloves
                [39] = ItemFamily.Miscellaneous, // healerskit
                [44] = ItemFamily.Miscellaneous, // magicrod
                [45] = ItemFamily.Tool, // fishingrod
                [50] = ItemFamily.MeleeWeapon, // quarterstaff
                [52] = ItemFamily.Accessory, // ring
                [56] = ItemFamily.Shield, // largeshield
                [58] = ItemFamily.MeleeWeapon, // shortspear
                [59] = ItemFamily.RangedWeapon, // shuriken
                [65] = ItemFamily.Miscellaneous, // key
                [66] = ItemFamily.Miscellaneous, // largebox
                [69] = ItemFamily.CreatureItem, // cslashweapon
                [70] = ItemFamily.CreatureItem, // cpiercweapon
                [71] = ItemFamily.CreatureItem, // cbludgweapon
                [72] = ItemFamily.CreatureItem, // cslshprcweap
                [73] = ItemFamily.CreatureItem, // creatureitem
                [74] = ItemFamily.Miscellaneous, // book
                [77] = ItemFamily.Miscellaneous, // gem
                [78] = ItemFamily.Accessory, // bracer
                [79] = ItemFamily.Miscellaneous, // miscthin
                [80] = ItemFamily.Cape, // cloak
                [109] = ItemFamily.Miscellaneous, // craftcompbase
                [111] = ItemFamily.MeleeWeapon, // Whip
                [112] = ItemFamily.Miscellaneous, // craftbase
                [205] = ItemFamily.Miscellaneous, // modern_thin
                [206] = ItemFamily.Miscellaneous, // modern_mdm
                [210] = ItemFamily.Tool, // holdable
                [211] = ItemFamily.Tool, // holdable2
                [213] = ItemFamily.RangedWeapon, // d20_smallArms_d6
                [307] = ItemFamily.Miscellaneous, // miscmedium2
                [310] = ItemFamily.MeleeWeapon, // katar
                [311] = ItemFamily.Miscellaneous, // miscsmall2
                [314] = ItemFamily.Tool, // fashionacc
                [325] = ItemFamily.Tool, // Flowers
                [511] = ItemFamily.Lightsaber, // twobladedlightsaber
                [512] = ItemFamily.Lightsaber, // lightsaber
                [513] = ItemFamily.Essence, // ess1
                [514] = ItemFamily.RangedWeapon, // legacy_smallarms
                [515] = ItemFamily.Miscellaneous, // basefuel
                [516] = ItemFamily.Essence, // ess2
                [517] = ItemFamily.Essence, // ess3
                [518] = ItemFamily.Essence, // ess4
                [519] = ItemFamily.Essence, // ess5
                [520] = ItemFamily.Essence, // ess6
                [522] = ItemFamily.Essence, // ess8
                [525] = ItemFamily.MeleeWeapon, // electroblade
                [526] = ItemFamily.Miscellaneous, // miscmedium_stackable
                [527] = ItemFamily.Essence, // ess1_stackable
                [528] = ItemFamily.Essence, // ess2_stackable
                [529] = ItemFamily.Essence, // ess3_stackable
                [530] = ItemFamily.Essence, // ess4_stackable
                [531] = ItemFamily.Essence, // ess5_stackable
                [536] = ItemFamily.Miscellaneous, // miscsmall_stackable
                [537] = ItemFamily.MeleeWeapon, // twinelectroblade
                [538] = ItemFamily.Miscellaneous, // miscthin_stackable
                [539] = ItemFamily.Miscellaneous, // miscsmall2_stackable
                [540] = ItemFamily.Miscellaneous // miscmedium2_stackable
            };

        private static readonly ItemFamily[] EquipmentFamilies =
        {
            ItemFamily.MeleeWeapon, ItemFamily.RangedWeapon, ItemFamily.Lightsaber, ItemFamily.Armor,
            ItemFamily.Helmet, ItemFamily.Cape, ItemFamily.Shield, ItemFamily.Accessory
        };

        [Test]
        public void BaseTypeMatrix_EveryUsedBaseItemClassifiesAsExpected()
        {
            var twoDa = new TwoDaService(Sw2DaDirectory);
            var baseItemRows = new BaseItemRowService(twoDa);

            var usedBaseItems = new HashSet<int>();
            foreach (var path in Directory.EnumerateFiles(UtiDirectory, "*.json"))
            {
                var baseItem = UtiDocument.Load(path).BaseItem;
                if (baseItem.HasValue)
                    usedBaseItems.Add(baseItem.Value);
            }

            usedBaseItems.Should().NotBeEmpty();

            var familyCounts = new Dictionary<ItemFamily, int>();
            foreach (var baseItem in usedBaseItems)
            {
                var row = baseItemRows.GetOrNull(baseItem);
                row.Should().NotBeNull($"BaseItem {baseItem} is used in the corpus but absent from baseitems.2da");

                var family = ItemFamilyClassifier.Classify(row!);

                ExpectedFamilyByBaseItem.TryGetValue(baseItem, out var expected).Should().BeTrue(
                    $"BaseItem {baseItem} (\"{row!.Label}\") is used in the corpus but has no hand-verified " +
                    "expected family in this test - add one rather than letting it pass unverified");
                family.Should().Be(expected, $"BaseItem {baseItem} (\"{row.Label}\")");

                familyCounts[family] = familyCounts.GetValueOrDefault(family) + 1;
            }

            // Equipment families never offer a Behavior-rail role.
            foreach (var family in EquipmentFamilies)
                ItemRoleCatalog.RolesFor(family).Should().BeEmpty($"{family} is equipment, not role-driven");

            // The four role-driven families match the role catalog's own declared sets.
            ItemRoleCatalog.RolesFor(ItemFamily.Miscellaneous).Select(role => role.Id).Should().BeEquivalentTo(new[]
            {
                ItemRoleCatalog.ConsumableId, ItemRoleCatalog.MealId,
                ItemRoleCatalog.DeployedDeviceId, ItemRoleCatalog.DroidPartId,
                ItemRoleCatalog.IncubationSampleId, ItemRoleCatalog.SchematicId,
                ItemRoleCatalog.KeyItemId, ItemRoleCatalog.CustomId
            });
            ItemRoleCatalog.RolesFor(ItemFamily.Essence).Select(role => role.Id).Should().BeEquivalentTo(new[]
            {
                ItemRoleCatalog.ComponentId, ItemRoleCatalog.EnhancementId, ItemRoleCatalog.CustomId
            });
            ItemRoleCatalog.RolesFor(ItemFamily.CreatureItem).Select(role => role.Id).Should().BeEquivalentTo(new[]
            {
                ItemRoleCatalog.CreatureItemId, ItemRoleCatalog.CustomId
            });
            ItemRoleCatalog.RolesFor(ItemFamily.Tool).Select(role => role.Id).Should().BeEquivalentTo(new[]
            {
                ItemRoleCatalog.CustomId
            });

            // Weapon families never show Crafting as a primary group.
            foreach (var family in new[] { ItemFamily.MeleeWeapon, ItemFamily.RangedWeapon, ItemFamily.Lightsaber })
                ItemStatVisibility.PrimaryGroups(family).Should().NotContain(ItemStatGroup.Crafting);

            // Only families that swing something see the weapon-only Combat stats (DMG/Delay) - the
            // three player weapon families plus creature items, whose base types 69-72 are creature
            // weapons. DamageStat (103) was
            // removed from the editor by owner decision and WeaponDamageType (134) moved to
            // ItemMultiEntryCatalog as an exclusive choice, so neither is an ItemStatDefinition here
            // anymore - the ids stay in this list only as documentation of what used to be checked.
            var weaponOnlyPropertyIds = new[] { 93, 98 };
            foreach (var family in Enum.GetValues<ItemFamily>())
            {
                var combatStats = ItemStatVisibility.CombatStatsFor(family);
                if (ItemStatVisibility.CarriesWeaponCombatStats(family))
                    combatStats.Should().Contain(stat => weaponOnlyPropertyIds.Contains(stat.PropertyId));
                else
                    combatStats.Should().NotContain(stat => weaponOnlyPropertyIds.Contains(stat.PropertyId));
            }

            // No family's primaries carry the removed Enhancements group - it belongs only to
            // ItemMultiEntryCatalog's context tagging, never to a family's visible primary set.
            foreach (var family in Enum.GetValues<ItemFamily>())
                ItemStatVisibility.PrimaryGroups(family).Should().NotContain(ItemStatGroup.Enhancements);
        }

        // ------------------------------------------------------------------
        // (d) The item editor constructs over every blueprint without mutating it.
        // ------------------------------------------------------------------

        [Test]
        public void EditorConstructionSweep_EveryBlueprintConstructsAndRoundTripsUntouched()
        {
            var twoDa = new TwoDaService(Sw2DaDirectory);
            var baseItemRows = new BaseItemRowService(twoDa);
            Func<string, Action, bool> runEdit = (_, mutation) => { mutation(); return true; };

            var files = Directory.EnumerateFiles(UtiDirectory, "*.json").ToList();
            files.Should().NotBeEmpty();

            var failures = new ConcurrentBag<string>();
            var processed = 0;

            Parallel.ForEach(files, path =>
            {
                try
                {
                    var document = UtiDocument.Load(path);
                    // Compare the same document serializer before and after construction. Comparing
                    // to the source file conflates editor mutation with harmless canonicalization
                    // of older JSON formatting/order during a normal load/save round trip.
                    var beforeConstruction = document.ToBytes();

                    using (var editor = new ItemEditorViewModel(
                               document.Fields, "test", runEdit, baseItemRows: baseItemRows.GetOrNull))
                    {
                        _ = editor.Family;
                    }

                    var written = document.ToBytes();
                    if (!written.AsSpan().SequenceEqual(beforeConstruction))
                        failures.Add($"{path}: constructing the editor changed the document's bytes");
                }
                catch (Exception ex)
                {
                    failures.Add($"{path}: {ex.GetType().Name}: {ex.Message}");
                }

                Interlocked.Increment(ref processed);
            });

            processed.Should().Be(files.Count);
            failures.Should().BeEmpty(
                $"every uti blueprint must construct without exception and round-trip untouched. " +
                $"{failures.Count} failed. First failures:\n{string.Join("\n", failures.Take(10))}");
        }
    }
}

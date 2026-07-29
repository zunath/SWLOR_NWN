using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Editors.Items;
using System.Text.Json;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// Opening any real blueprint must show every property that blueprint carries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the question <see cref="ItemCombinationAuditTests"/> does NOT ask. That test proves
    /// each stored (property, subtype) pair is known to SOME catalog, which is a statement about
    /// the catalogs; it says nothing about whether a builder opening that particular item can see
    /// or edit the property, because what the Stats tab shows is chosen per family. A property can
    /// be perfectly catalogued and still be invisible on every item that actually carries it - and
    /// 62 family/property pairs were, across roughly 1,500 blueprints, until this test went in.
    /// </para>
    /// <para>
    /// The rule the editor follows, and this test checks, is: a family decides what a blueprint of
    /// that kind USUALLY has, and what the blueprint actually stores decides the rest. Hiding a
    /// stored value is always worse than showing a row the family does not normally want, because
    /// a hidden value is still saved back - it just cannot be seen or corrected.
    /// </para>
    /// <para>
    /// A failure here is not automatically a bug; a family may one day deliberately hide something.
    /// That is what <see cref="AcceptedGaps"/> is for - an entry names the family, the property and
    /// WHY, so an accepted gap is a decision on the record rather than an oversight.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class ItemPropertyReachabilityTests
    {
        /// <summary>
        /// Family/property pairs a corpus item carries that the editor deliberately does not show,
        /// each with the reason it is accepted rather than fixed.
        /// </summary>
        private static readonly IReadOnlyDictionary<(ItemFamily Family, int PropertyId), string> AcceptedGaps =
            new Dictionary<(ItemFamily, int), string>
            {
                // Empty on purpose: every property a blueprint carries is currently reachable on that
                // blueprint. This stays as the escape hatch so a future deliberate omission is a
                // decision on the record with a reason next to it, not a silently missing field.
            };

        /// <summary>
        /// Preserve-only properties no family edits: dangling legacy data the editor keeps but never
        /// offers. Mirrors <see cref="ItemCombinationAuditTests"/>'s own allowlist.
        /// </summary>
        /// <remarks>
        /// UnlimitedAmmo (61) joined them by decision rather than by decay: every ranged weapon in
        /// this game has unlimited ammunition, so the property decides nothing. Stored values stay
        /// on disk; the editor just does not ask a question with one answer.
        /// </remarks>
        private static readonly HashSet<int> PreserveOnlyProperties = new() { 20, 37, 51, 61, 103, 146, 148 };

        private const int CastSpellPropertyId = 15;

        [Test]
        public void EveryPropertyACorpusBlueprintCarriesIsReachableWhenThatBlueprintIsOpened()
        {
            var twoDa = new TwoDaService(
                Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da"));
            var baseItemRows = new BaseItemRowService(twoDa);

            var gaps = new Dictionary<(ItemFamily Family, int PropertyId), (int Count, string Example)>();
            var files = Directory.EnumerateFiles(
                Path.Combine(CorpusLocator.ModuleDirectory, "uti"), "*.uti.json").ToList();
            files.Should().NotBeEmpty("the uti corpus should be present");

            foreach (var path in files)
            {
                UtiDocument document;
                try
                {
                    document = UtiDocument.Load(path);
                }
                catch (Exception)
                {
                    continue; // A malformed blueprint is the corpus loader's problem, not this test's.
                }

                var store = new ItemValueStore(document.Fields);
                var baseItem = (int)(store.GetInteger(BehaviorFieldStorage.Field, "BaseItem") ?? -1);
                var row = baseItem < 0 ? null : baseItemRows.GetOrNull(baseItem);
                var family = row == null ? ItemFamily.Miscellaneous : ItemFamilyClassifier.Classify(row);
                var stored = store.Properties.Select(property => property.PropertyId).ToHashSet();
                var reachable = ReachableProperties(family, ItemRoleCatalog.Classify(store, family).Id, stored);

                foreach (var propertyId in stored)
                {
                    if (reachable.Contains(propertyId) ||
                        ItemEngineLegacyCatalog.Contains(propertyId) ||
                        PreserveOnlyProperties.Contains(propertyId) ||
                        AcceptedGaps.ContainsKey((family, propertyId)))
                    {
                        continue;
                    }

                    var key = (family, propertyId);
                    gaps.TryGetValue(key, out var seen);
                    gaps[key] = (seen.Count + 1, seen.Example ?? Path.GetFileName(path));
                }
            }

            if (gaps.Count == 0)
                return;

            var report = string.Join("\n", gaps
                .OrderByDescending(gap => gap.Value.Count)
                .Select(gap =>
                    $"  {gap.Key.Family}/{gap.Key.PropertyId}: {gap.Value.Count} item(s), e.g. {gap.Value.Example}"));
            Assert.Fail(
                $"{gaps.Count} family/property pair(s) exist on real blueprints but cannot be seen or edited " +
                $"in the editor for that family:\n{report}\n\n" +
                "Either show the property for that family, or add it to AcceptedGaps with the reason.");
        }

        /// <summary>
        /// Top-level GFF fields the editor never offers because nothing should type them by hand:
        /// the engine or the save path owns each one.
        /// </summary>
        /// <remarks>
        /// "Description" is the odd one out - it is the UNIDENTIFIED description, and it is listed
        /// here because nothing reads it. NWScript's GetDescription defaults to the identified
        /// string, and every examine surface in the server takes that default, so DescIdentified is
        /// what players see and what the editor's single Description box binds to. The unidentified
        /// field is preserved exactly as it sits on disk rather than being given a box of its own,
        /// which would be an editable field that could never change anything in game.
        /// </remarks>
        private static readonly IReadOnlyDictionary<string, string> EngineOwnedFields =
            new Dictionary<string, string>
            {
                ["__data_type"] = "the GFF file-type marker",
                ["Comment"] = "a builder's note to itself; dropped from the layout by owner decision",
                ["Description"] = "the unidentified description - dead data, see the note below",
                ["Identified"] = "every blueprint ships identified; the flag is set on creation",
                ["PropertiesList"] = "the Stats and Requirements tabs, checked by the sweep above",
                ["VarTable"] = "the Variables tab",
            };

        [Test]
        public void EveryFieldACorpusBlueprintCarriesIsReachableWhenThatBlueprintIsOpened()
        {
            // The same question as the property sweep, asked of the GFF fields instead. It is how
            // DescIdentified was found: 1,390 blueprints carry the identified description - the text
            // the engine hands to GetDescription and players actually read - and the Basic tab only
            // offered the unidentified one, so on 441 items the only description present was
            // invisible.
            var editable = EditableFieldNames();
            var missing = new Dictionary<string, (int Count, string Example)>();

            foreach (var path in Directory.EnumerateFiles(
                         Path.Combine(CorpusLocator.ModuleDirectory, "uti"), "*.uti.json"))
            {
                JsonDocument parsed;
                try
                {
                    parsed = JsonDocument.Parse(File.ReadAllBytes(path));
                }
                catch (Exception)
                {
                    continue;
                }

                using (parsed)
                {
                    if (parsed.RootElement.ValueKind != JsonValueKind.Object)
                        continue;

                    foreach (var field in parsed.RootElement.EnumerateObject())
                    {
                        if (editable.Contains(field.Name) || EngineOwnedFields.ContainsKey(field.Name))
                            continue;

                        missing.TryGetValue(field.Name, out var seen);
                        missing[field.Name] = (seen.Count + 1, seen.Example ?? Path.GetFileName(path));
                    }
                }
            }

            if (missing.Count == 0)
                return;

            var report = string.Join("\n", missing
                .OrderByDescending(field => field.Value.Count)
                .Select(field => $"  {field.Key}: {field.Value.Count} item(s), e.g. {field.Value.Example}"));
            Assert.Fail(
                $"{missing.Count} GFF field(s) exist on real blueprints but no item-editor surface " +
                $"reads or writes them:\n{report}\n\n" +
                "Either add the field to a tab, or list it in EngineOwnedFields with the reason.");
        }

        /// <summary>Every top-level field name some item-editor surface binds to.</summary>
        private static HashSet<string> EditableFieldNames()
        {
            var names = ItemEditorLayout.Basic.Select(field => field.Name).ToHashSet();

            // The Appearance tab: the simple/composite model parts, the armor parts and their
            // x-twins, and the six dye channels.
            names.Add(ItemAppearanceFieldNames.SimplePart);
            names.Add(ItemAppearanceFieldNames.Middle);
            names.Add(ItemAppearanceFieldNames.Top);
            names.Add("xModelPart1");
            names.Add("xModelPart2");
            names.Add("xModelPart3");

            foreach (var name in new[]
                     {
                         ItemAppearanceFieldNames.Neck, ItemAppearanceFieldNames.NeckTwin,
                         ItemAppearanceFieldNames.Torso, ItemAppearanceFieldNames.TorsoTwin,
                         ItemAppearanceFieldNames.Belt, ItemAppearanceFieldNames.BeltTwin,
                         ItemAppearanceFieldNames.Pelvis, ItemAppearanceFieldNames.PelvisTwin,
                         ItemAppearanceFieldNames.Robe, ItemAppearanceFieldNames.RobeTwin,
                         ItemAppearanceFieldNames.Cloth1Color, ItemAppearanceFieldNames.Cloth2Color,
                         ItemAppearanceFieldNames.Leather1Color, ItemAppearanceFieldNames.Leather2Color,
                         ItemAppearanceFieldNames.Metal1Color, ItemAppearanceFieldNames.Metal2Color
                     })
            {
                names.Add(name);
            }

            foreach (var pair in ItemAppearanceFieldNames.Pairs)
            {
                names.Add(pair.LeftField);
                names.Add(pair.RightField);
                if (pair.LeftTwinField != null)
                    names.Add(pair.LeftTwinField);
                if (pair.RightTwinField != null)
                    names.Add(pair.RightTwinField);
            }

            return names;
        }

        [Test]
        public void AnEssenceCanStillChooseItsWeaponDamageType()
        {
            // 23 corpus essences carry property 134. Filtering it off every non-weapon family (to
            // get it off armor) made those uneditable; only worn gear should drop it.
            var combat = ItemStatVisibility.MultiEntryFor(ItemFamily.Essence, ItemStatGroup.Combat);

            combat.Should().Contain(definition => definition.PropertyId == 134);
            ItemStatVisibility.MultiEntryFor(ItemFamily.Armor, ItemStatGroup.Combat)
                .Should().NotContain(definition => definition.PropertyId == 134, "worn gear has no damage type");
        }

        /// <summary>
        /// Every property the editor can surface for one blueprint: its family's stat groups (plus
        /// whatever its role unlocks, plus any group the blueprint already stores a value in), the
        /// multi-entry lists those groups carry, the Requirements tab, and the role card's spell
        /// picker. Mirrors <c>ItemStatsSectionViewModel.Rebuild</c> - if the two drift, this test is
        /// measuring something the editor no longer does.
        /// </summary>
        private static HashSet<int> ReachableProperties(
            ItemFamily family, string roleId, IReadOnlySet<int> stored)
        {
            var groups = ItemStatVisibility.PrimaryGroups(family).ToList();
            foreach (var unlocked in ItemRoleCatalog.GroupsUnlockedBy(roleId))
            {
                if (!groups.Contains(unlocked))
                    groups.Add(unlocked);
            }

            // Mirrors ItemStatsSectionViewModel: an Essence always sees Enhancements, whatever role
            // it takes, because slotting into other gear is what an Essence IS.
            if (family == ItemFamily.Essence && !groups.Contains(ItemStatGroup.Enhancements))
                groups.Add(ItemStatGroup.Enhancements);

            foreach (var group in StoredGroups(stored))
            {
                if (!groups.Contains(group))
                    groups.Add(group);
            }

            var reachable = new HashSet<int> { CastSpellPropertyId };
            foreach (var group in groups)
            {
                var stats = group == ItemStatGroup.Combat
                    ? ItemStatVisibility.CombatStatsFor(family, stored)
                    : ItemStatCatalog.ByGroup(group);
                foreach (var stat in stats)
                    reachable.Add(stat.PropertyId);

                foreach (var definition in ItemStatVisibility.MultiEntryFor(family, group, stored))
                    reachable.Add(definition.PropertyId);
            }

            // The Requirements tab is family-independent.
            AddRequirements(reachable);

            return reachable;
        }

        /// <summary>Mirrors <c>ItemStatsSectionViewModel.StoredGroups</c>.</summary>
        private static IEnumerable<ItemStatGroup> StoredGroups(IReadOnlySet<int> stored)
        {
            var groups = new List<ItemStatGroup>();
            foreach (var stat in ItemStatCatalog.All)
            {
                if (stored.Contains(stat.PropertyId) && !groups.Contains(stat.Group))
                    groups.Add(stat.Group);
            }

            foreach (var definition in ItemMultiEntryCatalog.All)
            {
                if (definition.IsRequirement || definition.Context is not { } context)
                    continue;

                if (stored.Contains(definition.PropertyId) && !groups.Contains(context))
                    groups.Add(context);
            }

            return groups;
        }

        private static void AddRequirements(HashSet<int> reachable)
        {
            foreach (var requirement in ItemRequirementCatalog.All)
                reachable.Add(requirement.PropertyId);
            foreach (var definition in ItemMultiEntryCatalog.All.Where(entry => entry.IsRequirement))
                reachable.Add(definition.PropertyId);
        }
    }
}

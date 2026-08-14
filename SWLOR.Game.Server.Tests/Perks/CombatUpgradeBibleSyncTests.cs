using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class CombatUpgradeBibleSyncTests
{
    private static readonly HashSet<string> ScopedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Aura",
        "Capstone",
        "Combat",
        "Stance",
        "Toggle",
        "Trait"
    };

    private static readonly HashSet<string> ImplementedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Implemented",
        "Design Added"
    };

    private static readonly HashSet<string> OutOfScopeTabs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Farming",
        "Agriculture",
        "Smithery",
        "Research",
    };

    private static readonly HashSet<string> WeaponTabs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vibroblade",
        "Vibroknife",
        "Lightsaber",
        "Heavy Vibroblade",
        "Spear",
        "Twin Blade",
        "Saberstaff",
        "Katar",
        "Staff",
        "Pistol",
        "Rifle",
        "Throwing"
    };

    private static readonly HashSet<string> WeaponProgressionShapeOutliers = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string[]> WeaponProgressionTypePatternByStyle = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Heavy Vibroblade|Immortal"] = new[]
        {
            "Combat",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Trait",
            "Combat",
            "Stance",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Combat",
            "Trait",
            "Trait",
            "Trait",
            "Trait",
            "Capstone"
        },
        ["Heavy Vibroblade|Berserker"] = new[]
        {
            "Combat",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Trait",
            "Combat",
            "Stance",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Toggle",
            "Trait",
            "Trait",
            "Trait",
            "Trait",
            "Capstone"
        },
        ["Vibroblade|Bulwark"] = new[]
        {
            "Combat",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Trait",
            "Combat",
            "Stance",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Combat",
            "Trait",
            "Combat",
            "Combat",
            "Trait",
            "Capstone"
        },
        ["Katar|Iron Guard"] = new[]
        {
            "Combat",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Trait",
            "Trait",
            "Combat",
            "Trait",
            "Trait",
            "Trait",
            "Combat",
            "Trait",
            "Trait",
            "Combat",
            "Stance",
            "Trait",
            "Capstone"
        }
    };

    private static readonly Dictionary<string, int[]> WeaponProgressionPricePatternByStyle = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Katar|Iron Guard"] = new[]
        {
            2,
            2,
            2,
            2,
            2,
            3,
            3,
            4,
            4,
            4,
            4,
            4,
            3,
            2,
            4,
            5,
            4,
            6
        },
        ["Vibroblade|Bulwark"] = new[]
        {
            2,
            2,
            2,
            2,
            2,
            4,
            3,
            4,
            4,
            4,
            3,
            4,
            3,
            2,
            4,
            5,
            4,
            6
        }
    };

    private static readonly HashSet<PerkCategoryType> DroidInstructionCategories = new()
    {
        PerkCategoryType.General,
        PerkCategoryType.DevicesAssaultGadgets,
        PerkCategoryType.DevicesFieldEngineer,
        PerkCategoryType.DevicesFieldSupport,
        PerkCategoryType.DevicesGrenadier,
        PerkCategoryType.FirstAidCombatPharmacology,
        PerkCategoryType.FirstAidTraumaMedic
    };

    private static readonly Dictionary<string, SkillType> SkillNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Armor"] = SkillType.Armor,
        ["Beast Mastery"] = SkillType.BeastMastery,
        ["Devices"] = SkillType.Devices,
        ["Engineering"] = SkillType.Engineering,
        ["Fabrication"] = SkillType.Fabrication,
        ["First Aid"] = SkillType.FirstAid,
        ["Force"] = SkillType.Force,
        ["Gathering"] = SkillType.Gathering,
        ["Heavy Vibroblade"] = SkillType.HeavyVibroblade,
        ["Katar"] = SkillType.Katar,
        ["Leadership"] = SkillType.Leadership,
        ["Lightsaber"] = SkillType.Lightsaber,
        ["Mimicry"] = SkillType.Mimicry,
        ["Piloting"] = SkillType.Piloting,
        ["Pistol"] = SkillType.Pistol,
        ["Rifle"] = SkillType.Rifle,
        ["Saberstaff"] = SkillType.Saberstaff,
        ["Spear"] = SkillType.Spear,
        ["Staff"] = SkillType.Staff,
        ["Throwing"] = SkillType.Throwing,
        ["Twin Blade"] = SkillType.TwinBlade,
        ["Vibroblade"] = SkillType.Vibroblade,
        ["Vibroknife"] = SkillType.Vibroknife
    };

    [Test]
    public void ForceAndDevices_HaveEqualSkillPointAndAbilityBudgets()
    {
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv");
        var forceRows = rows.Where(row => row.Tab == "Force").ToArray();
        var deviceRows = rows.Where(row => row.Tab == "Devices").ToArray();

        forceRows.Sum(row => ParseWholeNumber(row.Price)).Should().Be(240);
        deviceRows.Sum(row => ParseWholeNumber(row.Price)).Should().Be(240);
        forceRows.Length.Should().Be(deviceRows.Length);
        forceRows.Count(row => row.Type == "Combat")
            .Should().Be(deviceRows.Count(row => row.Type == "Combat"));

        deviceRows
            .GroupBy(row => row.Style)
            .ToDictionary(group => group.Key, group => group.Sum(row => ParseWholeNumber(row.Price)))
            .Should().OnlyContain(pair => pair.Value == 60,
                "each Devices archetype must retain the same 60-SP completion cost");

        AssertTwinPrices(forceRows, deviceRows, "Throw Rock", "Arc Projector");
        AssertTwinPrices(forceRows, deviceRows, "Radiant Lance", "Ion Lance");
    }

    private static void AssertTwinPrices(
        IReadOnlyCollection<BiblePerkRow> forceRows,
        IReadOnlyCollection<BiblePerkRow> deviceRows,
        string forcePerkName,
        string devicePerkName)
    {
        var forcePrices = forceRows
            .Where(row => GetBaseName(row.PerkName).Equals(forcePerkName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => GetExpectedLevel(row.PerkName))
            .Select(row => ParseWholeNumber(row.Price));
        var devicePrices = deviceRows
            .Where(row => GetBaseName(row.PerkName).Equals(devicePerkName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => GetExpectedLevel(row.PerkName))
            .Select(row => ParseWholeNumber(row.Price));

        devicePrices.Should().Equal(forcePrices,
            $"{devicePerkName} must retain the same rank prices as its Force twin {forcePerkName}");
    }

    [Test]
    public void CombatUpgradeBibleManifest_MatchesLivePerkAndAbilityRegistries()
    {
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv")
            .Where(IsScopedImplementedRow)
            .ToArray();
        var perks = BuildPerksWithout2daLookup();
        var abilities = BuildAbilities();
        var failures = new List<string>();
        var scopedPerkTypes = new HashSet<PerkType>();
        var expectedActiveFeats = new HashSet<FeatType>();

        foreach (var row in rows)
        {
            var match = FindMatchingPerk(row, perks, failures);
            if (match == null)
                continue;

            var (perkType, perk, level) = match.Value;
            scopedPerkTypes.Add(perkType);
            AssertPerkRow(row, perk, level, failures);

            if (IsNativeStealthRow(row))
            {
                AssertNativeStealthRow(row, level, root, failures);
            }
            else if (ShouldValidateAsActiveAbility(row, level))
            {
                if (IsTameRow(row))
                {
                    AssertTameRow(row, level, abilities, failures);
                    if (GetExpectedLevel(row.PerkName) == 1)
                    {
                        expectedActiveFeats.Add(FeatType.Tame);
                        expectedActiveFeats.Add(FeatType.CallBeast);
                    }

                    continue;
                }

                AssertActiveAbilityRow(row, perkType, level, abilities, failures);
                foreach (var feat in level.GrantedFeats)
                {
                    expectedActiveFeats.Add(feat);
                }
            }
            else if (IsTraitLikeType(row.Type))
            {
                var nonPassiveIconFeats = level.GrantedFeats
                    .Where(feat => !IsPassiveTraitFeat(feat, abilities))
                    .ToArray();
                if (nonPassiveIconFeats.Length != 0)
                {
                    failures.Add($"{Describe(row)}: bible type is Trait but code grants non-passive feat(s): {string.Join(", ", nonPassiveIconFeats)}.");
                }
            }
        }

        AssertNoExtraScopedPerks(rows, perks, failures);
        AssertNoExtraScopedAbilities(scopedPerkTypes, expectedActiveFeats, abilities, failures);
        AssertAllAbilityDefinitionNamesMatchAbilityNames(abilities, failures);
        AssertDefinitionFormatting(root, failures);
        AssertDefinitionClassNamesMatchFiles(root, failures);
        AssertStatusEffectDefinitionNamesMatchStatusEffectNames(root, failures);
        AssertExplicitCombatImpactScalingDeclarations(root, failures);

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures.Take(200)));
    }

    [Test]
    public void CombatUpgradeBibleManifest_EveryGrantedStatHasAProductionConsumer()
    {
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv")
            .Where(IsScopedImplementedRow)
            .ToArray();
        var scopedCategories = rows
            .SelectMany(GetExpectedCategories)
            .ToHashSet();
        var grantedStats = BuildPerksWithout2daLookup()
            .Where(perk => scopedCategories.Contains(perk.Detail.Category))
            .SelectMany(perk => perk.Detail.PerkLevels.Values)
            .SelectMany(level => level.StatBonuses)
            .Select(bonus => bonus.Stat)
            .Where(stat => stat != StatType.Invalid)
            .Distinct()
            .OrderBy(stat => stat)
            .ToArray();
        var productionRoot = Path.Combine(root.FullName, "SWLOR.Game.Server");
        var consumerCorpus = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(productionRoot, "*.cs", System.IO.SearchOption.AllDirectories)
                .Where(file => !file.Contains(
                    Path.Combine("Feature", "PerkDefinition"),
                    StringComparison.OrdinalIgnoreCase))
                .Where(file => !file.EndsWith(
                    Path.Combine("Service", "StatService", "StatType.cs"),
                    StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));
        var missingConsumers = grantedStats
            .Where(stat => !consumerCorpus.Contains($"StatType.{stat}", StringComparison.Ordinal))
            .Select(stat => stat.ToString())
            .ToArray();

        missingConsumers.Should().BeEmpty(
            "a Bible-scoped perk stat that is never read by production code is an unimplemented gameplay promise:" +
            Environment.NewLine + string.Join(Environment.NewLine, missingConsumers));
    }

    [Test]
    public void CombatUpgradeBibleManifest_MimicryTechniquesMatchLiveAbilities()
    {
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv")
            .Where(row => row.Tab.Equals("Mimicry", StringComparison.OrdinalIgnoreCase))
            .Where(row => row.Style.Equals("Technique", StringComparison.OrdinalIgnoreCase))
            .Where(row => ImplementedStatuses.Contains(row.DevStatus))
            .ToArray();
        var abilities = BuildAbilities()
            .Where(x => x.Value.Detail.IsMimicryTechnique)
            .ToArray();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "feat.2da")));
        var spellRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "spells.2da")));
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "sw_tlk" / "sw_tlk.tlk.json");
        var failures = new List<string>();
        var matchedFeats = new HashSet<FeatType>();

        foreach (var row in rows)
        {
            var matches = abilities
                .Where(x => x.Value.Detail.Name.Equals(row.PerkName, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                failures.Add($"{Describe(row)}: expected one live Mimicry technique ability, found {matches.Length}.");
                continue;
            }

            var (feat, ability) = matches[0];
            matchedFeats.Add(feat);
            var detail = ability.Detail;

            if (!featRows.TryGetValue((int)feat, out var featRow))
            {
                failures.Add($"{Describe(row)}: technique feat {feat} is missing from feat.2da.");
            }
            else
            {
                AssertTlkDescription(row, featRow.GetValueOrDefault("DESCRIPTION"), tlkEntries, failures, $"{feat} feat.2da DESCRIPTION");

                if (featRow.TryGetValue("SPELLID", out var spellIdText) &&
                    int.TryParse(spellIdText, out var spellId) &&
                    spellRows.TryGetValue(spellId, out var spellRow))
                {
                    AssertTlkDescription(row, spellRow.GetValueOrDefault("SpellDesc"), tlkEntries, failures, $"{feat} spells.2da SpellDesc");
                }
            }
            var expectedRequirement = ParseMimicrySkillRequirement(row, failures);
            if (expectedRequirement != null && detail.MimicrySkillRequirement != expectedRequirement.Value)
            {
                failures.Add($"{Describe(row)}: Mimicry skill requirement mismatch. Bible={expectedRequirement}, Code={detail.MimicrySkillRequirement}.");
            }

            var expectedSlots = TryParseWholeNumber(row.Slots);
            if (expectedSlots == null || detail.MimicrySlotCost != expectedSlots.Value)
            {
                failures.Add($"{Describe(row)}: slot cost mismatch. Bible={row.Slots}, Code={detail.MimicrySlotCost}.");
            }

            if (row.Type.Equals("Trait", StringComparison.OrdinalIgnoreCase) != detail.IsMimicryTrait)
            {
                failures.Add($"{Describe(row)}: Trait classification mismatch. Code IsMimicryTrait={detail.IsMimicryTrait}.");
            }

            if (row.Type.Equals("Stance", StringComparison.OrdinalIgnoreCase) != detail.IsMimicryStance)
            {
                failures.Add($"{Describe(row)}: Stance classification mismatch. Code IsMimicryStance={detail.IsMimicryStance}.");
            }

            AssertMimicryTraitPayload(row, detail, failures);

            AssertTargetingDescription(row, detail, failures);

            AssertAbilityCost<AbilityRequirementFP>(row, "FP", row.FP, detail, failures, x => x.RequiredFP);
            AssertAbilityCost<AbilityRequirementStamina>(row, "STM", row.STM, detail, failures, x => x.RequiredSTM);

            var cooldown = TryParseDurationSeconds(row.CooldownTime);
            var actualCooldown = detail.RecastDelay?.Invoke(0);
            if (cooldown != null && (actualCooldown == null || Math.Abs(actualCooldown.Value - cooldown.Value) > 0.001f))
            {
                failures.Add($"{Describe(row)}: recast mismatch. Bible={cooldown.Value}, Code={actualCooldown?.ToString(CultureInfo.InvariantCulture) ?? "-"}.");
            }

            var activationDelay = TryParseActivationSeconds(row.CastingTime);
            if (activationDelay != null)
            {
                var actualDelay = detail.ActivationDelay?.Invoke(0, 0, detail.AbilityLevel) ?? 0f;
                if (Math.Abs(actualDelay - activationDelay.Value) > 0.001f)
                {
                    failures.Add($"{Describe(row)}: activation delay mismatch. Bible={activationDelay.Value}, Code={actualDelay}.");
                }
            }
        }

        foreach (var (feat, ability) in abilities.Where(x => !matchedFeats.Contains(x.Key)))
        {
            failures.Add($"{ability.Detail.Name}/{feat}: live Mimicry technique has no implemented Bible technique row.");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures.Take(200)));
    }

    [Test]
    public void WeaponPerkProgression_UsesNormalizedCostsAndCapstoneRows()
    {
        int[] expectedSkillRanks = { 2, 5, 8, 10, 12, 15, 18, 20, 22, 25, 28, 30, 32, 35, 38, 40, 45, 50 };
        int[] expectedPrices = { 2, 2, 2, 2, 2, 4, 3, 4, 4, 4, 3, 4, 3, 2, 4, 5, 4, 6 };
        string[] expectedTypes =
        {
            "Combat",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Trait",
            "Combat",
            "Stance",
            "Trait",
            "Trait",
            "Combat",
            "Combat",
            "Combat",
            "Trait",
            "Combat",
            "Combat",
            "Trait",
            "Capstone"
        };
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv")
            .Where(IsScopedImplementedRow)
            .Where(row => WeaponTabs.Contains(row.Tab))
            .ToArray();
        var styleGroups = rows
            .GroupBy(row => $"{row.Tab}|{row.Style}", StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var failures = new List<string>();

        if (styleGroups.Length != 24)
            failures.Add($"Expected 24 weapon style progressions, found {styleGroups.Length}.");

        foreach (var group in styleGroups)
        {
            var ordered = group
                .OrderBy(row => TryParseSkillRequirement(row.SkillRequirements)?.Rank ?? int.MaxValue)
                .ThenBy(row => ParseWholeNumber(row.Row))
                .ToArray();
            var tab = ordered[0].Tab;
            var style = ordered[0].Style;
            var total = ordered.Sum(row => ParseWholeNumber(row.Price));
            var capstone = ordered[^1];

            var skillRanks = ordered
                .Select(row => TryParseSkillRequirement(row.SkillRequirements)?.Rank ?? 0)
                .ToArray();
            var prices = ordered
                .Select(row => ParseWholeNumber(row.Price))
                .ToArray();
            var types = ordered
                .Select(row => NormalizeProgressionType(row.Type))
                .ToArray();

            if (!skillRanks.SequenceEqual(expectedSkillRanks))
                failures.Add($"{tab}/{style}: skill-rank pattern should be [{string.Join(", ", expectedSkillRanks)}], found [{string.Join(", ", skillRanks)}].");

            var expectedPricePattern = GetExpectedWeaponProgressionPricePattern(group.Key, expectedPrices);
            if (!prices.SequenceEqual(expectedPricePattern))
                failures.Add($"{tab}/{style}: SP-price pattern should be [{string.Join(", ", expectedPricePattern)}], found [{string.Join(", ", prices)}].");

            var expectedTypePattern = GetExpectedWeaponProgressionTypePattern(group.Key, expectedTypes);
            if (!types.SequenceEqual(expectedTypePattern, StringComparer.OrdinalIgnoreCase))
                failures.Add($"{tab}/{style}: type pattern should be [{string.Join(", ", expectedTypePattern)}], found [{string.Join(", ", types)}].");

            if (!ordered[0].Type.Equals("Combat", StringComparison.OrdinalIgnoreCase))
                failures.Add($"{tab}/{style}: first perk at rank 2 must be an active Combat ability, found {ordered[0].Type} '{ordered[0].PerkName}'.");

            if (!capstone.Type.Equals("Capstone", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{tab}/{style}: skill-50 row '{capstone.PerkName}' should be typed Capstone.");
            }

            if (ParseWholeNumber(capstone.Price) != 6)
            {
                failures.Add($"{tab}/{style}: skill-50 row '{capstone.PerkName}' should cost 6 SP.");
            }

            for (var index = 1; index < ordered.Length; index++)
            {
                var previous = ordered[index - 1];
                var current = ordered[index];
                if (!HasRankSuffix(previous.PerkName) && !HasRankSuffix(current.PerkName))
                    continue;

                if (GetBaseName(previous.PerkName).Equals(GetBaseName(current.PerkName), StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"{tab}/{style}: ranked rows '{previous.PerkName}' and '{current.PerkName}' are adjacent; put a different perk between ranks.");
                }
            }

            if (ordered.Length == 18)
            {
                if (total != 60)
                {
                    failures.Add($"{tab}/{style}: 18-row weapon style should total 60 SP, found {total}.");
                }

                continue;
            }

            if (!WeaponProgressionShapeOutliers.Contains(group.Key))
            {
                failures.Add($"{tab}/{style}: unexpected weapon progression shape with {ordered.Length} rows.");
                continue;
            }

            if (total != 60)
            {
                failures.Add($"{tab}/{style}: known row-count outlier should still total 60 SP, found {total}.");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void CombatUpgradeBibleManifest_UsesRankSuffixOnlyForMultiLevelPerks()
    {
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv")
            .Where(row => !string.IsNullOrWhiteSpace(row.PerkName))
            .ToArray();
        var rankPattern = new Regex(@"\s+(I|II|III|IV|V|VI|VII|VIII|IX|X)$", RegexOptions.IgnoreCase);
        var rankGroups = rows
            .Where(row => rankPattern.IsMatch(row.PerkName))
            .GroupBy(row => GetRankGroupKey(row.Tab, row.Style, GetBaseName(row.PerkName)), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => GetExpectedLevel(row.PerkName))
                    .ToHashSet(),
                StringComparer.OrdinalIgnoreCase);

        var failures = new List<string>();
        foreach (var row in rows)
        {
            var match = rankPattern.Match(row.PerkName);
            if (!match.Success)
                continue;

            var baseName = GetBaseName(row.PerkName);
            var key = GetRankGroupKey(row.Tab, row.Style, baseName);
            if (!rankGroups[key].Any(rank => rank > 1))
            {
                failures.Add($"{Describe(row)}: singleton perk should be named '{baseName}' instead of '{row.PerkName}'.");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void CombatUpgradeBibleManifest_GrantedFeatDescriptionsMatchBibleRows()
    {
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv")
            .Where(IsScopedImplementedRow)
            .ToArray();
        var perks = BuildPerksWithout2daLookup();
        var abilities = BuildAbilities();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "feat.2da")));
        var spellRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "spells.2da")));
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "sw_tlk" / "sw_tlk.tlk.json");
        var failures = new List<string>();

        foreach (var row in rows)
        {
            var match = FindMatchingPerk(row, perks, failures);
            if (match == null)
                continue;

            var level = match.Value.Level;
            foreach (var feat in level.GrantedFeats.Where(feat => feat != FeatType.Invalid))
            {
                if (!featRows.TryGetValue((int)feat, out var featRow))
                {
                    failures.Add($"{Describe(row)}: granted feat {feat} is missing from feat.2da.");
                    continue;
                }

                if (IsAuxiliaryGrantedFeat(row, feat))
                {
                    AssertConcreteTlkDescription(row, featRow.GetValueOrDefault("DESCRIPTION"), tlkEntries, failures, $"{feat} feat.2da DESCRIPTION");
                    continue;
                }

                AssertTlkDescription(row, featRow.GetValueOrDefault("DESCRIPTION"), tlkEntries, failures, $"{feat} feat.2da DESCRIPTION");

                if (!abilities.ContainsKey(feat) ||
                    !featRow.TryGetValue("SPELLID", out var spellIdText) ||
                    spellIdText.Equals("****", StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(spellIdText, out var spellId))
                {
                    continue;
                }

                if (!spellRows.TryGetValue(spellId, out var spellRow))
                {
                    failures.Add($"{Describe(row)}: granted feat {feat} references missing spells.2da row {spellId}.");
                    continue;
                }

                AssertTlkDescription(row, spellRow.GetValueOrDefault("SpellDesc"), tlkEntries, failures, $"{feat} spells.2da SpellDesc");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures.Take(200)));
    }

    [Test]
    public void CombatUpgradeBibleManifest_WritesLineByLineImplementationReview()
    {
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv")
            .ToArray();
        var perks = BuildPerksWithout2daLookup();
        var abilities = BuildAbilities();
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "feat.2da")));
        var spellRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "sw_2da",
            "spells.2da")));
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "sw_tlk" / "sw_tlk.tlk.json");
        var reviewRows = new List<ImplementationReviewRow>();

        foreach (var row in rows)
        {
            var scope = GetReviewScope(row);
            var failures = new List<string>();
            PerkMatch? match = null;
            var codePerk = string.Empty;
            var codePrice = string.Empty;
            var codeDescription = string.Empty;
            var codeRequirements = string.Empty;
            var grantedFeats = string.Empty;
            var abilityEvidence = string.Empty;
            var featDescriptionEvidence = string.Empty;
            var spellDescriptionEvidence = string.Empty;

            if (scope.ShouldValidate)
            {
                if (IsMimicryTechniqueRow(row))
                {
                    var evidence = ReviewMimicryTechniqueRow(
                        row,
                        abilities,
                        featRows,
                        spellRows,
                        tlkEntries,
                        failures);
                    codePerk = evidence.CodePerk;
                    codePrice = evidence.CodePrice;
                    codeDescription = evidence.CodeDescription;
                    codeRequirements = evidence.CodeRequirements;
                    grantedFeats = evidence.GrantedFeats;
                    abilityEvidence = evidence.AbilityEvidence;
                    featDescriptionEvidence = evidence.FeatDescriptionEvidence;
                    spellDescriptionEvidence = evidence.SpellDescriptionEvidence;
                }
                else
                {
                    match = FindMatchingPerk(row, perks, failures);
                    if (match != null)
                    {
                        var (perkType, perk, level) = match.Value;
                        codePerk = $"{perkType}/{perk.Category}/{perk.Name} rank {GetExpectedLevel(row.PerkName)}";
                        codePrice = level.Price.ToString(CultureInfo.InvariantCulture);
                        codeDescription = level.Description;
                        codeRequirements = DescribeRequirements(level);
                        grantedFeats = string.Join("; ", level.GrantedFeats.Where(feat => feat != FeatType.Invalid));
                        abilityEvidence = DescribeAbilityEvidence(level, abilities);
                        if (IsTameRow(row) && level.GrantedFeats.Count == 0)
                        {
                            abilityEvidence = abilities.TryGetValue(FeatType.Tame, out var tameAbility)
                                ? $"Tame rank scales the existing Tame ability; higher Tame ranks grant no new feat by design. Existing ability evidence: {DescribeAbilityEvidence(FeatType.Tame, tameAbility)}"
                                : "Tame rank scales the existing Tame ability; higher Tame ranks grant no new feat by design, but the Tame ability was not found.";
                        }
                        featDescriptionEvidence = DescribeFeatDescriptionEvidence(row, level, featRows, tlkEntries, failures);
                        spellDescriptionEvidence = DescribeSpellDescriptionEvidence(row, level, abilities, featRows, spellRows, tlkEntries, failures);

                        AssertPerkRow(row, perk, level, failures);

                        if (IsNativeStealthRow(row))
                        {
                            AssertNativeStealthRow(row, level, root, failures);
                            abilityEvidence = "Uses NWN's built-in Stealth action; no custom feat or ability definition by design.";
                        }
                        else if (ShouldValidateAsActiveAbility(row, level))
                        {
                            if (IsTameRow(row))
                            {
                                AssertTameRow(row, level, abilities, failures);
                            }
                            else
                            {
                                AssertActiveAbilityRow(row, perkType, level, abilities, failures);
                            }
                        }
                        else if (IsTraitLikeType(row.Type))
                        {
                            var nonPassiveIconFeats = level.GrantedFeats
                                .Where(feat => !IsPassiveTraitFeat(feat, abilities))
                                .ToArray();
                            if (nonPassiveIconFeats.Length != 0)
                            {
                                failures.Add($"{Describe(row)}: bible type is {row.Type} but code grants active feats: {string.Join(", ", nonPassiveIconFeats)}.");
                            }
                        }
                    }
                }
            }

            var verdict = !scope.ShouldValidate
                ? "SKIP"
                : failures.Count == 0
                    ? "PASS"
                    : "FAIL";

            var findings = !scope.ShouldValidate
                ? scope.Description
                : string.Join(" | ", failures.Distinct());

            reviewRows.Add(new ImplementationReviewRow(
                verdict,
                scope.Description,
                row.Tab,
                row.Row,
                row.Style,
                row.Price,
                codePrice,
                row.PerkName,
                row.SkillRequirements,
                codeRequirements,
                row.CharacterType,
                row.Type,
                row.PrimaryStat,
                row.SecondaryStat,
                row.ScalingSource,
                row.CrossSkill,
                row.FP,
                row.STM,
                row.CastingTime,
                row.CooldownTime,
                row.DevStatus,
                row.AdditionalRequirements,
                row.Notes,
                row.Description,
                codeDescription,
                codePerk,
                grantedFeats,
                abilityEvidence,
                featDescriptionEvidence,
                spellDescriptionEvidence,
                findings));
        }

        var outputPath = root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBibleImplementationReview.csv";
        WriteImplementationReview(outputPath.FullName, reviewRows);

        reviewRows.Should().HaveCount(rows.Length);
        reviewRows
            .Where(row => row.Verdict == "FAIL")
            .Select(row => $"{row.Tab}/{row.Style}/{row.BibleRow} {row.PerkName}: {row.Findings}")
            .Should()
            .BeEmpty("the line-by-line implementation review should not contain scoped implemented row failures");
    }

    private static MimicryReviewEvidence ReviewMimicryTechniqueRow(
        BiblePerkRow row,
        IReadOnlyDictionary<FeatType, AbilityRecord> abilities,
        IReadOnlyDictionary<int, Dictionary<string, string>> featRows,
        IReadOnlyDictionary<int, Dictionary<string, string>> spellRows,
        IReadOnlyDictionary<int, string> tlkEntries,
        List<string> failures)
    {
        var matches = abilities
            .Where(x => x.Value.Detail.IsMimicryTechnique)
            .Where(x => x.Value.Detail.Name.Equals(row.PerkName, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            failures.Add($"{Describe(row)}: expected one live Mimicry technique ability, found {matches.Length}.");
            return MimicryReviewEvidence.Empty;
        }

        var (feat, ability) = matches[0];
        var detail = ability.Detail;
        AssertAbilityDefinitionName(row, ability, failures);

        var expectedRequirement = ParseMimicrySkillRequirement(row, failures);
        if (expectedRequirement != null && detail.MimicrySkillRequirement != expectedRequirement.Value)
        {
            failures.Add($"{Describe(row)}: Mimicry skill requirement mismatch. Bible={expectedRequirement}, Code={detail.MimicrySkillRequirement}.");
        }

        var expectedSlots = TryParseWholeNumber(row.Slots);
        if (expectedSlots == null || detail.MimicrySlotCost != expectedSlots.Value)
        {
            failures.Add($"{Describe(row)}: slot cost mismatch. Bible={row.Slots}, Code={detail.MimicrySlotCost}.");
        }

        if (row.Type.Equals("Trait", StringComparison.OrdinalIgnoreCase) != detail.IsMimicryTrait)
        {
            failures.Add($"{Describe(row)}: Trait classification mismatch. Code IsMimicryTrait={detail.IsMimicryTrait}.");
        }

        if (row.Type.Equals("Stance", StringComparison.OrdinalIgnoreCase) != detail.IsMimicryStance)
        {
            failures.Add($"{Describe(row)}: Stance classification mismatch. Code IsMimicryStance={detail.IsMimicryStance}.");
        }

        AssertMimicryTraitPayload(row, detail, failures);

        var scalingMatch = Regex.Match(
            row.Description,
            @"\b(?:Deals|dealing)\s+\d+\s+\w+\s+DMG\s+plus\s+(MGT|PER|VIT|AGI|WIL|SOC)\s+scaling\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (scalingMatch.Success)
        {
            var expectedAbility = scalingMatch.Groups[1].Value.ToUpperInvariant() switch
            {
                "MGT" => AbilityType.Might,
                "PER" => AbilityType.Perception,
                "VIT" => AbilityType.Vitality,
                "AGI" => AbilityType.Agility,
                "WIL" => AbilityType.Willpower,
                "SOC" => AbilityType.Social,
                _ => AbilityType.Invalid
            };
            if (detail.CombatImpactDamageAbility != expectedAbility)
            {
                failures.Add($"{Describe(row)}: damage scaling mismatch. Bible={scalingMatch.Groups[1].Value.ToUpperInvariant()}, Code={detail.CombatImpactDamageAbility}.");
            }
            if (!row.PrimaryStat.Equals(scalingMatch.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{Describe(row)}: Primary Stat must match the description's {scalingMatch.Groups[1].Value.ToUpperInvariant()} scaling, found '{row.PrimaryStat}'.");
            }
            if (!row.ScalingSource.Equals("Combat Formula", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{Describe(row)}: damaging technique Scaling Source should be Combat Formula, found '{row.ScalingSource}'.");
            }
        }
        else
        {
            if (!row.PrimaryStat.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{Describe(row)}: non-damaging technique Primary Stat should be None, found '{row.PrimaryStat}'.");
            }
            if (!row.ScalingSource.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{Describe(row)}: non-damaging technique Scaling Source should be None, found '{row.ScalingSource}'.");
            }
        }

        AssertTargetingDescription(row, detail, failures);
        AssertAbilityCost<AbilityRequirementFP>(row, "FP", row.FP, detail, failures, x => x.RequiredFP);
        AssertAbilityCost<AbilityRequirementStamina>(row, "STM", row.STM, detail, failures, x => x.RequiredSTM);

        var cooldown = TryParseDurationSeconds(row.CooldownTime);
        var actualCooldown = detail.RecastDelay?.Invoke(0);
        if (cooldown != null && (actualCooldown == null || Math.Abs(actualCooldown.Value - cooldown.Value) > 0.001f))
        {
            failures.Add($"{Describe(row)}: recast mismatch. Bible={cooldown.Value}, Code={actualCooldown?.ToString(CultureInfo.InvariantCulture) ?? "-"}.");
        }

        var activationDelay = TryParseActivationSeconds(row.CastingTime);
        if (activationDelay != null)
        {
            var actualDelay = detail.ActivationDelay?.Invoke(0, 0, detail.AbilityLevel) ?? 0f;
            if (Math.Abs(actualDelay - activationDelay.Value) > 0.001f)
            {
                failures.Add($"{Describe(row)}: activation delay mismatch. Bible={activationDelay.Value}, Code={actualDelay}.");
            }
        }

        var featEvidence = "-";
        var spellEvidence = "-";
        if (!featRows.TryGetValue((int)feat, out var featRow))
        {
            failures.Add($"{Describe(row)}: technique feat {feat} is missing from feat.2da.");
            featEvidence = $"{feat}: missing feat.2da row";
        }
        else
        {
            featEvidence = $"{feat}: {DescribeExactTlkReference(row, featRow.GetValueOrDefault("DESCRIPTION"), tlkEntries, failures, $"{feat} feat.2da DESCRIPTION")}";
            if (featRow.TryGetValue("SPELLID", out var spellIdText) &&
                int.TryParse(spellIdText, out var spellId))
            {
                if (spellRows.TryGetValue(spellId, out var spellRow))
                {
                    spellEvidence = $"{feat}: {DescribeExactTlkReference(row, spellRow.GetValueOrDefault("SpellDesc"), tlkEntries, failures, $"{feat} spells.2da SpellDesc")}";
                }
                else
                {
                    failures.Add($"{Describe(row)}: technique feat {feat} references missing spells.2da row {spellId}.");
                    spellEvidence = $"{feat}: missing spells.2da row {spellId}";
                }
            }
        }

        return new MimicryReviewEvidence(
            $"{feat}/{ability.DefinitionType.Name}/Mimicry rank {detail.MimicrySkillRequirement}",
            "-",
            row.Description,
            $"Mimicry rank {detail.MimicrySkillRequirement}; slots {detail.MimicrySlotCost}",
            feat.ToString(),
            $"{DescribeAbilityEvidence(feat, ability)} mimicryRank={detail.MimicrySkillRequirement} slots={detail.MimicrySlotCost} trait={detail.IsMimicryTrait} stance={detail.IsMimicryStance}",
            featEvidence,
            spellEvidence);
    }

    private static void AssertMimicryTraitPayload(
        BiblePerkRow row,
        AbilityDetail detail,
        List<string> failures)
    {
        if (!detail.IsMimicryTrait)
            return;

        var expectedStats = new Dictionary<StatType, int>();
        var expectedResistances = new Dictionary<ResistanceType, int>();
        var procMatch = Regex.Match(
            row.Description,
            @"^Your attacks have an? (?<value>\d+)% chance to inflict (?<status>Bleed|Freezing|Hemorrhage|Poison|Shock|Sunder)\.$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (procMatch.Success)
        {
            var stat = procMatch.Groups["status"].Value.ToLowerInvariant() switch
            {
                "bleed" => StatType.DamageDealtBleedChance,
                "freezing" => StatType.DamageDealtFreezingChance,
                "hemorrhage" => StatType.DamageDealtHemorrhageChance,
                "poison" => StatType.DamageDealtPoisonChance,
                "shock" => StatType.DamageDealtShockChance,
                "sunder" => StatType.DamageDealtSunderChance,
                _ => StatType.Invalid
            };
            expectedStats[stat] = int.Parse(procMatch.Groups["value"].Value, CultureInfo.InvariantCulture);
        }
        else
        {
            var flatStatNames = new Dictionary<string, StatType>(StringComparer.OrdinalIgnoreCase)
            {
                ["Accuracy"] = StatType.AccuracyPercentAdjustment,
                ["Attack"] = StatType.AttackPercentAdjustment,
                ["Critical Rate"] = StatType.CriticalRatePercentAdjustment,
                ["Force Attack"] = StatType.ForceAttackPercentAdjustment,
                ["Force Defense"] = StatType.ForceDefensePercentAdjustment,
                ["Physical Defense"] = StatType.PhysicalDefensePercentAdjustment
            };
            foreach (Match match in Regex.Matches(
                         row.Description,
                         @"(?:Increases your |\+)(?<stat>Accuracy|Attack|Critical Rate|Force Attack|Force Defense|Physical Defense)(?: rating)?(?: by)? (?<value>\d+)%|\+(?<prefixValue>\d+)%(?:\s+)(?<prefixStat>Force Defense|Physical Defense)",
                         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                var statName = match.Groups["stat"].Success
                    ? match.Groups["stat"].Value
                    : match.Groups["prefixStat"].Value;
                var valueText = match.Groups["value"].Success
                    ? match.Groups["value"].Value
                    : match.Groups["prefixValue"].Value;
                expectedStats[flatStatNames[statName]] = int.Parse(valueText, CultureInfo.InvariantCulture);
            }

            var resistanceNames = new Dictionary<string, ResistanceType>(StringComparer.OrdinalIgnoreCase)
            {
                ["Fire"] = ResistanceType.Fire,
                ["Poison"] = ResistanceType.Poison,
                ["Trauma"] = ResistanceType.Trauma
            };
            foreach (Match match in Regex.Matches(
                         row.Description,
                         @"\+(?<value>\d+) (?<resistance>Fire|Poison|Trauma) Resistance",
                         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                expectedResistances[resistanceNames[match.Groups["resistance"].Value]] =
                    int.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
            }
        }

        if (expectedStats.Count == 0 && expectedResistances.Count == 0)
        {
            failures.Add($"{Describe(row)}: unsupported Mimicry trait description; its payload cannot be statically verified: '{row.Description}'.");
            return;
        }

        if (!detail.MimicryTraitStats.OrderBy(x => x.Key).SequenceEqual(expectedStats.OrderBy(x => x.Key)))
        {
            failures.Add(
                $"{Describe(row)}: Mimicry trait stat payload mismatch. " +
                $"Bible=[{string.Join(", ", expectedStats.Select(x => $"{x.Key}={x.Value}"))}] " +
                $"Code=[{string.Join(", ", detail.MimicryTraitStats.Select(x => $"{x.Key}={x.Value}"))}].");
        }

        if (!detail.MimicryTraitResistances.OrderBy(x => x.Key).SequenceEqual(expectedResistances.OrderBy(x => x.Key)))
        {
            failures.Add(
                $"{Describe(row)}: Mimicry trait resistance payload mismatch. " +
                $"Bible=[{string.Join(", ", expectedResistances.Select(x => $"{x.Key}={x.Value}"))}] " +
                $"Code=[{string.Join(", ", detail.MimicryTraitResistances.Select(x => $"{x.Key}={x.Value}"))}].");
        }
    }

    private static bool IsAuxiliaryGrantedFeat(BiblePerkRow row, FeatType feat)
    {
        return IsTameRow(row) && feat == FeatType.CallBeast;
    }

    [Test]
    public void CombatUpgradeBibleManifest_DefinesControlEffectLanguageForConditionalPerks()
    {
        var root = FindRepositoryRoot();
        var rows = ReadManifest(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv")
            .Where(row => IsScopedImplementedRow(row))
            .ToArray();
        var undefinedControlPhrases = new[]
        {
            "controlled target",
            "controlled targets",
            "controlled enemies",
            "debuffed/controlled",
            "debuffed or controlled",
            "becomes controlled"
        };
        var failures = rows
            .Where(row => undefinedControlPhrases.Any(
                phrase => row.Description.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
            .Select(row => $"{Describe(row)}: use 'control effect' wording instead of undefined controlled shorthand.")
            .ToArray();

        var validationMatrix = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Readmes",
            "CombatUpgradeReleaseValidationMatrix.md"));
        validationMatrix.Should().Contain("`Controlled` is a category, not a single status effect.");
        validationMatrix.Should().Contain("A target is controlled while affected by a control effect");
        validationMatrix.Should().Contain("Blind, Confusion, Dazed, Disoriented, Foggy Mind, Force Disruption");

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void DroidInstructionResources_MatchCurrentRecipeDefinitionsAndMigration()
    {
        var root = FindRepositoryRoot();
        var recipes = new DroidInstructionRecipes()
            .BuildRecipes()
            .Where(x => x.Value.Category == RecipeCategoryType.DroidInstruction)
            .ToDictionary(x => x.Key, x => x.Value);
        var recipeResrefs = recipes.Values
            .Select(recipe => recipe.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var recipeTypes = recipes.Keys
            .Select(type => type.ToString())
            .ToHashSet(StringComparer.Ordinal);
        var templates = ReadDroidInstructionTemplates(root);
        var templateResrefs = templates
            .Select(template => template.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var paletteText = File.ReadAllText(Path.Combine(root.FullName, "Module", "itp", "itempalcus.itp.json"));
        var recipeTypeSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "CraftService",
            "RecipeType.cs"));
        var enumRecipeTypes = Regex.Matches(recipeTypeSource, @"(?m)^\s*(Instruction[A-Za-z0-9]+)\s*=")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        var migrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ObsoleteItemMigration.cs"));
        var migrationMaxLevels = ReadCurrentDroidInstructionMaxLevels(migrationSource);
        var perks = BuildPerksWithout2daLookup()
            .ToDictionary(x => x.Type, x => x.Detail);
        var expectedInstructions = GetExpectedDroidInstructions(perks)
            .ToDictionary(x => (x.Perk, x.Level), x => x);
        var templatesByPerkLevel = templates
            .GroupBy(x => (x.Perk, x.Level))
            .ToDictionary(x => x.Key, x => x.ToArray());
        var templateMaxLevels = templates
            .GroupBy(x => x.Perk)
            .ToDictionary(x => x.Key, x => x.Max(y => y.Level));
        var failures = new List<string>();

        foreach (var expectedInstruction in expectedInstructions.Values.OrderBy(x => x.Perk).ThenBy(x => x.Level))
        {
            var aiSlots = perks[expectedInstruction.Perk].PerkLevels[expectedInstruction.Level].DroidAISlots;
            if (aiSlots <= 0)
                failures.Add($"{expectedInstruction.Perk} level {expectedInstruction.Level} is droid-selectable but has no AI slot cost.");
        }

        foreach (var duplicate in recipes.Values
                     .GroupBy(x => x.Resref, StringComparer.OrdinalIgnoreCase)
                     .Where(x => x.Count() > 1)
                     .OrderBy(x => x.Key))
        {
            failures.Add($"Droid instruction recipe resref '{duplicate.Key}' is used by multiple recipes.");
        }

        foreach (var resref in recipeResrefs.OrderBy(x => x))
        {
            if (!templateResrefs.Contains(resref))
                failures.Add($"Droid instruction recipe resref '{resref}' has no UTI template.");

            if (!Regex.IsMatch(paletteText, $@"""value""\s*:\s*""{Regex.Escape(resref)}""", RegexOptions.None))
                failures.Add($"Droid instruction recipe resref '{resref}' is missing from the custom item palette.");
        }

        foreach (var template in templates.OrderBy(x => x.Resref))
        {
            if (!recipeResrefs.Contains(template.Resref))
                failures.Add($"Droid instruction UTI '{template.Resref}' exists without a current recipe definition.");

            if (!perks.TryGetValue(template.Perk, out var perk))
            {
                failures.Add($"Droid instruction UTI '{template.Resref}' references missing perk {template.Perk}.");
            }
            else if (!perk.PerkLevels.ContainsKey(template.Level))
            {
                failures.Add($"Droid instruction UTI '{template.Resref}' references missing {template.Perk} level {template.Level}.");
            }
            else if (!expectedInstructions.TryGetValue((template.Perk, template.Level), out var expectedInstruction))
            {
                failures.Add($"Droid instruction UTI '{template.Resref}' references non-droid-selectable {template.Perk} level {template.Level}.");
            }
            else if (!template.Name.Equals(expectedInstruction.Name, StringComparison.Ordinal))
            {
                failures.Add($"Droid instruction UTI '{template.Resref}' has name '{template.Name}', expected '{expectedInstruction.Name}'.");
            }
        }

        foreach (var expectedInstruction in expectedInstructions.Values.OrderBy(x => x.Perk).ThenBy(x => x.Level))
        {
            if (!templatesByPerkLevel.TryGetValue((expectedInstruction.Perk, expectedInstruction.Level), out var matchingTemplates))
            {
                failures.Add($"Missing droid instruction UTI for {expectedInstruction.Perk} level {expectedInstruction.Level}.");
                continue;
            }

            if (matchingTemplates.Length > 1)
                failures.Add($"{expectedInstruction.Perk} level {expectedInstruction.Level} has multiple droid instruction UTIs: {string.Join(", ", matchingTemplates.Select(x => x.Resref).OrderBy(x => x))}.");
        }

        enumRecipeTypes.Should().BeEquivalentTo(recipeTypes, "droid instruction recipe enum entries should match live recipe definitions");

        foreach (var group in templates.GroupBy(x => x.Perk).OrderBy(x => x.Key))
        {
            var maxLevel = group.Max(x => x.Level);
            if (!migrationMaxLevels.TryGetValue(group.Key, out var migrationMaxLevel))
            {
                failures.Add($"Obsolete item migration missing current droid max level for {group.Key}={maxLevel}.");
                continue;
            }

            if (migrationMaxLevel != maxLevel)
                failures.Add($"Obsolete item migration has current droid max level {group.Key}={migrationMaxLevel}, expected {maxLevel}.");
        }

        foreach (var (perkType, maxLevel) in migrationMaxLevels.OrderBy(x => x.Key))
        {
            if (!templateMaxLevels.TryGetValue(perkType, out var templateMaxLevel))
            {
                failures.Add($"Obsolete item migration permits droid instruction perk {perkType} level {maxLevel}, but no current UTI exists.");
                continue;
            }

            if (templateMaxLevel != maxLevel)
                continue;

            if (!perks.TryGetValue(perkType, out var perk))
            {
                failures.Add($"Obsolete item migration permits missing perk {perkType}.");
                continue;
            }

            for (var level = 1; level <= maxLevel; level++)
            {
                if (!perk.PerkLevels.TryGetValue(level, out var perkLevel))
                {
                    failures.Add($"Obsolete item migration permits missing {perkType} level {level}.");
                    continue;
                }

                if (!expectedInstructions.ContainsKey((perkType, level)))
                    failures.Add($"Obsolete item migration permits non-droid-selectable {perkType} level {level}.");

                if (perkLevel.DroidAISlots <= 0)
                    failures.Add($"Obsolete item migration permits {perkType} level {level}, but it has no AI slot cost.");
            }
        }

        migrationSource.Should().Contain("\"id_concgren3\"");
        migrationSource.Should().Contain("\"id_tranqshot3\"");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static IReadOnlyDictionary<PerkType, int> ReadCurrentDroidInstructionMaxLevels(string source)
    {
        var block = Regex.Match(
            source,
            @"CurrentDroidInstructionMaxLevels\s*=\s*new\(\)\s*\{(?<body>.*?)\};",
            RegexOptions.Singleline);
        if (!block.Success)
            Assert.Fail("Could not find CurrentDroidInstructionMaxLevels in obsolete item migration.");

        var result = new Dictionary<PerkType, int>();
        foreach (Match match in Regex.Matches(
                     block.Groups["body"].Value,
                     @"\{\s*PerkType\.([A-Za-z0-9_]+)\s*,\s*(\d+)\s*\}",
                     RegexOptions.None))
        {
            var perkName = match.Groups[1].Value;
            if (!Enum.TryParse(perkName, out PerkType perkType))
                Assert.Fail($"CurrentDroidInstructionMaxLevels references unknown perk {perkName}.");

            if (!result.TryAdd(perkType, int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)))
                Assert.Fail($"CurrentDroidInstructionMaxLevels contains duplicate perk {perkName}.");
        }

        return result;
    }

    private static IReadOnlyCollection<ExpectedDroidInstruction> GetExpectedDroidInstructions(
        IReadOnlyDictionary<PerkType, PerkDetail> perks)
    {
        var result = new List<ExpectedDroidInstruction>();
        foreach (var (perkType, detail) in perks
                     .Where(x => DroidInstructionCategories.Contains(x.Value.Category))
                     .OrderBy(x => x.Value.Name)
                     .ThenBy(x => x.Key))
        {
            foreach (var (level, perkLevel) in detail.PerkLevels.OrderBy(x => x.Key))
            {
                if (perkLevel.GrantedFeats.Count <= 0 ||
                    perkLevel.GrantedFeats.All(IsPassiveIconTraitFeat))
                    continue;

                var suffix = detail.PerkLevels.Count > 1
                    ? $" {GetRomanNumeral(level)}"
                    : string.Empty;
                result.Add(new ExpectedDroidInstruction(
                    perkType,
                    level,
                    $"Instruction Disc: {detail.Name}{suffix}"));
            }
        }

        return result;
    }

    private static string GetRankGroupKey(string tab, string style, string baseName)
    {
        return $"{tab}\u001F{style}\u001F{baseName}";
    }

    private static void AssertDefinitionFormatting(PathInfo root, List<string> failures)
    {
        foreach (var folder in EnumerateDefinitionFolders(root))
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                var lineNumber = 0;
                foreach (var line in File.ReadLines(file))
                {
                    lineNumber++;
                    if (line.StartsWith(".", StringComparison.Ordinal))
                    {
                        failures.Add($"{Path.GetRelativePath(root.FullName, file)}:{lineNumber}: fluent chain line starts at column 1.");
                    }
                }
            }
        }
    }

    private static void AssertAllAbilityDefinitionNamesMatchAbilityNames(
        IReadOnlyDictionary<FeatType, AbilityRecord> abilities,
        List<string> failures)
    {
        foreach (var (feat, ability) in abilities.OrderBy(x => x.Key))
        {
            var definitionName = ability.DefinitionType.Name;
            const string suffix = "AbilityDefinition";
            if (!definitionName.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            // Mimicry technique classes keep a "Technique" suffix (their FeatType would otherwise
            // collide with the source NPC feat, e.g. ToxicSpitTechnique vs ToxicSpit), while their
            // player-facing name deliberately drops it. Compare against the suffixed form for those.
            var baseName = GetBaseName(ability.Detail.Name);
            var expected = NormalizeName(ability.Detail.IsMimicryTechnique ? baseName + "Technique" : baseName);
            var actual = NormalizeName(definitionName[..^suffix.Length]);
            if (!actual.Equals(expected, StringComparison.Ordinal))
            {
                failures.Add($"{ability.Detail.Name}/{feat}: ability definition '{definitionName}.cs' does not match ability name.");
            }
        }
    }

    private static void AssertDefinitionClassNamesMatchFiles(PathInfo root, List<string> failures)
    {
        foreach (var folder in EnumerateDefinitionFolders(root))
        {
            foreach (var file in Directory
                         .EnumerateFiles(folder, "*.cs", System.IO.SearchOption.AllDirectories)
                         .Where(IsDefinitionClassFile))
            {
                var text = File.ReadAllText(file);
                var match = Regex.Match(text, @"(?m)^\s*(?:public\s+)?(?:sealed\s+|abstract\s+|static\s+|partial\s+)*class\s+([A-Za-z0-9_]+)");
                if (!match.Success)
                {
                    failures.Add($"{Path.GetRelativePath(root.FullName, file)}: no class declaration found.");
                    continue;
                }

                var expected = Path.GetFileNameWithoutExtension(file);
                var actual = match.Groups[1].Value;
                if (!actual.Equals(expected, StringComparison.Ordinal))
                {
                    failures.Add($"{Path.GetRelativePath(root.FullName, file)}: class name '{actual}' does not match file name '{expected}'.");
                }
            }
        }
    }

    private static void AssertStatusEffectDefinitionNamesMatchStatusEffectNames(PathInfo root, List<string> failures)
    {
        var folder = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition");
        foreach (var file in Directory.EnumerateFiles(folder, "*StatusEffect.cs", System.IO.SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var nameMatch = Regex.Match(text, @"public\s+override\s+string\s+Name\s*=>\s*""([^""]+)""");
            if (!nameMatch.Success)
                continue;

            var classMatch = Regex.Match(text, @"(?m)^\s*(?:public\s+)?(?:sealed\s+|abstract\s+|static\s+|partial\s+)*class\s+([A-Za-z0-9_]+)");
            if (!classMatch.Success)
                continue;

            var classBaseName = Regex.Replace(classMatch.Groups[1].Value, "StatusEffect$", "", RegexOptions.None);
            var expectedName = NormalizeName(ConvertRomanSuffixToRankNumber(nameMatch.Groups[1].Value));
            var candidates = GetStatusEffectNameCandidates(classBaseName)
                .Select(NormalizeName)
                .ToHashSet(StringComparer.Ordinal);

            if (!candidates.Contains(expectedName))
            {
                failures.Add($"{Path.GetRelativePath(root.FullName, file)}: status effect class '{classMatch.Groups[1].Value}' does not match display name '{nameMatch.Groups[1].Value}'.");
            }
        }
    }

    private static IEnumerable<string> GetStatusEffectNameCandidates(string classBaseName)
    {
        yield return classBaseName;
        yield return StripOptionalRankNumber(classBaseName);

        var technicalSuffixes = new[]
        {
            "Beast",
            "Damage",
            "DefensePenalty",
            "Healing",
            "MightPenalty",
            "Penalty",
            "Self",
            "VitalityPenalty"
        };

        foreach (var suffix in technicalSuffixes)
        {
            if (!classBaseName.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            var trimmed = classBaseName[..^suffix.Length];
            yield return trimmed;
            yield return StripOptionalRankNumber(trimmed);
        }
    }

    private static string StripOptionalRankNumber(string value)
    {
        return Regex.Replace(value, @"\d+$", "");
    }

    private static IEnumerable<string> EnumerateDefinitionFolders(PathInfo root)
    {
        yield return Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition");
        yield return Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "PerkDefinition");
        yield return Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition");
    }

    private static void AssertExplicitCombatImpactScalingDeclarations(PathInfo root, List<string> failures)
    {
        var folders = new[]
        {
            "Devices",
            "Force",
            "Pistol",
            "Rifle",
            "Throwing"
        };
        var abilityRoot = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition");

        foreach (var folder in folders.Select(x => Path.Combine(abilityRoot, x)).Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*AbilityDefinition.cs", System.IO.SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                if (!Regex.IsMatch(text, @"Apply(?:Telegraphed)?CombatImpact\s*\(", RegexOptions.None))
                    continue;

                if (!text.Contains("CombatImpactDamageAbility", StringComparison.Ordinal) &&
                    !text.Contains("combatImpactDamageAbility:", StringComparison.Ordinal))
                {
                    failures.Add($"{Path.GetRelativePath(root.FullName, file)}: combat-impact ability must declare its scaling ability in the ability definition.");
                }
            }
        }
    }

    private static bool IsDefinitionClassFile(string file)
    {
        var fileName = Path.GetFileName(file);
        return fileName.EndsWith("AbilityDefinition.cs", StringComparison.Ordinal) ||
               fileName.EndsWith("PerkDefinition.cs", StringComparison.Ordinal) ||
               fileName.EndsWith("StatusEffect.cs", StringComparison.Ordinal) ||
               fileName.EndsWith("StatusEffectDefinition.cs", StringComparison.Ordinal);
    }

    private static IReadOnlyCollection<DroidInstructionTemplate> ReadDroidInstructionTemplates(PathInfo root)
    {
        var folder = Path.Combine(root.FullName, "Module", "uti");
        var result = new List<DroidInstructionTemplate>();
        foreach (var file in Directory.EnumerateFiles(folder, "id_*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var rootElement = document.RootElement;
            if (!rootElement.TryGetProperty("PropertiesList", out var propertiesList) ||
                !propertiesList.TryGetProperty("value", out var properties))
            {
                continue;
            }

            foreach (var property in properties.EnumerateArray())
            {
                if (property.GetProperty("PropertyName").GetProperty("value").GetInt32() != 123)
                    continue;

                var resref = rootElement.GetProperty("TemplateResRef").GetProperty("value").GetString()!;
                var perkId = property.GetProperty("Subtype").GetProperty("value").GetInt32();
                if (!Enum.IsDefined(typeof(PerkType), perkId))
                {
                    Assert.Fail($"{Path.GetRelativePath(root.FullName, file)} has unknown droid instruction perk id {perkId}.");
                }

                result.Add(new DroidInstructionTemplate(
                    resref,
                    (PerkType)perkId,
                    property.GetProperty("CostValue").GetProperty("value").GetInt32(),
                    rootElement.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!));
            }
        }

        return result;
    }

    private static void AssertNoExtraScopedPerks(
        IReadOnlyCollection<BiblePerkRow> rows,
        IReadOnlyCollection<PerkRecord> perks,
        List<string> failures)
    {
        var expectedCategories = rows
            .SelectMany(GetExpectedCategories)
            .ToHashSet();
        var expectedRows = rows
            .Select(row => (
                Categories: GetExpectedCategories(row),
                Name: NormalizeName(GetBaseName(row.PerkName)),
                Level: GetExpectedLevel(row.PerkName)))
            .SelectMany(x => x.Categories.Select(category => (Category: category, x.Name, x.Level)))
            .ToHashSet();

        foreach (var perk in perks.Where(x => expectedCategories.Contains(x.Detail.Category)))
        {
            var name = NormalizeName(perk.Detail.Name);
            foreach (var level in perk.Detail.PerkLevels.Keys)
            {
                if (!expectedRows.Contains((perk.Detail.Category, name, level)))
                {
                    failures.Add($"{perk.Detail.Category}/{perk.Detail.Name} {level}: live perk level is in a bible-scoped category but is not present in the implemented bible rows.");
                }
            }
        }
    }

    private static void AssertNoExtraScopedAbilities(
        IReadOnlySet<PerkType> scopedPerkTypes,
        IReadOnlySet<FeatType> expectedActiveFeats,
        IReadOnlyDictionary<FeatType, AbilityRecord> abilities,
        List<string> failures)
    {
        foreach (var (feat, ability) in abilities.OrderBy(x => x.Key))
        {
            if (!scopedPerkTypes.Contains(ability.Detail.EffectiveLevelPerkType) ||
                expectedActiveFeats.Contains(feat))
            {
                continue;
            }

            // Mimicry techniques use Combat Analyzer as their EffectiveLevelPerkType for scaling, but they
            // are granted by equipping a learned technique through the Mimicry system, not by any perk level's granted
            // feats. Skip them here the same way IsTameRow/IsAuxiliaryGrantedFeat carve out other equip/aux-granted feats.
            if (ability.Detail.IsMimicryTechnique)
            {
                continue;
            }

            failures.Add($"{ability.Detail.EffectiveLevelPerkType}/{ability.Detail.Name}/{feat}: live ability is tied to a bible-scoped perk, but no implemented bible active row grants this feat.");
        }
    }

    private static PerkMatch? FindMatchingPerk(
        BiblePerkRow row,
        IReadOnlyCollection<PerkRecord> perks,
        List<string> failures)
    {
        var baseName = GetBaseName(row.PerkName);
        var expectedLevel = GetExpectedLevel(row.PerkName);
        var expectedSkill = TryParseSkillRequirement(row.SkillRequirements);
        var expectedBeastLevel = TryParseBeastLevelRequirement(row);
        var expectedBeastRole = TryParseBeastRoleRequirement(row);
        var expectedCategories = GetExpectedCategories(row);
        var candidates = perks
            .Where(x => NormalizeName(x.Detail.Name) == NormalizeName(baseName))
            .Where(x => x.Detail.PerkLevels.ContainsKey(expectedLevel))
            .ToArray();

        if (expectedCategories.Length > 0)
        {
            candidates = candidates
                .Where(x => expectedCategories.Contains(x.Detail.Category))
                .ToArray();
        }

        if (expectedSkill != null)
        {
            candidates = candidates
                .Where(x => HasSkillRequirement(x.Detail.PerkLevels[expectedLevel], expectedSkill.Value.Skill, expectedSkill.Value.Rank))
                .ToArray();
        }

        if (expectedBeastLevel != null)
        {
            candidates = candidates
                .Where(x => HasBeastLevelRequirement(x.Detail.PerkLevels[expectedLevel], expectedBeastLevel.Value))
                .ToArray();
        }

        if (expectedBeastRole != null)
        {
            candidates = candidates
                .Where(x => HasBeastRoleRequirement(x.Detail.PerkLevels[expectedLevel], expectedBeastRole.Value))
                .ToArray();
        }

        if (candidates.Length == 0)
        {
            failures.Add($"{Describe(row)}: no matching live perk level for '{baseName}' level {expectedLevel}.");
            return null;
        }

        if (candidates.Length > 1)
        {
            failures.Add($"{Describe(row)}: ambiguous live perk match: {string.Join(", ", candidates.Select(x => $"{x.Type}/{x.Detail.Category}"))}.");
            return null;
        }

        var candidate = candidates[0];
        return new PerkMatch(candidate.Type, candidate.Detail, candidate.Detail.PerkLevels[expectedLevel]);
    }

    private static void AssertPerkRow(BiblePerkRow row, PerkDetail perk, PerkLevel level, List<string> failures)
    {
        var expectedPrice = ParseWholeNumber(row.Price);
        if (level.Price != expectedPrice)
        {
            failures.Add($"{Describe(row)}: price mismatch. Bible={expectedPrice}, Code={level.Price}.");
        }

        if (!NormalizeText(level.Description).Equals(NormalizeText(row.Description), StringComparison.Ordinal))
        {
            failures.Add($"{Describe(row)}: description mismatch. Bible='{row.Description}' Code='{level.Description}'.");
        }

        var expectedSkill = TryParseSkillRequirement(row.SkillRequirements);
        if (expectedSkill != null && !HasSkillRequirement(level, expectedSkill.Value.Skill, expectedSkill.Value.Rank))
        {
            failures.Add($"{Describe(row)}: missing skill requirement {expectedSkill.Value.Skill} {expectedSkill.Value.Rank}.");
        }

        var expectedBeastLevel = TryParseBeastLevelRequirement(row);
        if (expectedBeastLevel != null)
        {
            if (!HasBeastLevelRequirement(level, expectedBeastLevel.Value))
            {
                failures.Add($"{Describe(row)}: missing beast level requirement {expectedBeastLevel.Value}.");
            }

            if (level.Requirements.OfType<PerkRequirementSkill>().Any(x => x.Type == SkillType.BeastMastery))
            {
                failures.Add($"{Describe(row)}: beast-owned perk must not use Beast Mastery rank as its level requirement.");
            }
        }
        else if (row.Tab.Equals("Beast Mastery", StringComparison.OrdinalIgnoreCase) &&
                 level.Requirements.OfType<PerkRequirementBeastLevel>().Any())
        {
            failures.Add($"{Describe(row)}: player Beast Mastery perk must not require active beast level.");
        }

        var expectedBeastRole = TryParseBeastRoleRequirement(row);
        if (expectedBeastRole != null && !HasBeastRoleRequirement(level, expectedBeastRole.Value))
        {
            failures.Add($"{Describe(row)}: missing beast role requirement {expectedBeastRole.Value}.");
        }

        var expectedCharacterType = TryParseCharacterType(row.CharacterType);
        if (expectedCharacterType != null && !HasCharacterTypeRequirement(level, expectedCharacterType.Value))
        {
            failures.Add($"{Describe(row)}: missing character type requirement {expectedCharacterType.Value}.");
        }

        if (row.CharacterType.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            level.Requirements.OfType<PerkRequirementCharacterType>().Any())
        {
            failures.Add($"{Describe(row)}: bible character type is All but code has a character type requirement.");
        }
    }

    private static void AssertActiveAbilityRow(
        BiblePerkRow row,
        PerkType perkType,
        PerkLevel level,
        IReadOnlyDictionary<FeatType, AbilityRecord> abilities,
        List<string> failures)
    {
        if (level.GrantedFeats.Count != 1)
        {
            failures.Add($"{Describe(row)}: bible type is {row.Type} but code grants {level.GrantedFeats.Count} feats.");
            return;
        }

        var feat = level.GrantedFeats[0];
        if (!abilities.TryGetValue(feat, out var ability))
        {
            failures.Add($"{Describe(row)}: granted feat {feat} has no live ability definition.");
            return;
        }

        var detail = ability.Detail;
        AssertAbilityDefinitionName(row, ability, failures);

        if (detail.EffectiveLevelPerkType != perkType)
        {
            failures.Add($"{Describe(row)}: ability effective perk mismatch. Bible perk={perkType}, Ability={detail.EffectiveLevelPerkType}.");
        }

        if (!detail.Name.Equals(row.PerkName, StringComparison.Ordinal))
        {
            failures.Add($"{Describe(row)}: ability name mismatch. Bible='{row.PerkName}' Code='{detail.Name}'.");
        }

        if (detail.AbilityLevel != GetExpectedLevel(row.PerkName))
        {
            failures.Add($"{Describe(row)}: ability level mismatch. Bible={GetExpectedLevel(row.PerkName)}, Code={detail.AbilityLevel}.");
        }

        var expectedSkill = TryParseSkillRequirement(row.SkillRequirements);
        if (expectedSkill != null && detail.SkillType != SkillType.Invalid && detail.SkillType != expectedSkill.Value.Skill)
        {
            failures.Add($"{Describe(row)}: ability skill mismatch. Bible={expectedSkill.Value.Skill}, Code={detail.SkillType}.");
        }

        AssertAbilityCost<AbilityRequirementFP>(row, "FP", row.FP, detail, failures, x => x.RequiredFP);
        AssertAbilityCost<AbilityRequirementStamina>(row, "STM", row.STM, detail, failures, x => x.RequiredSTM);

        var cooldown = TryParseDurationSeconds(row.CooldownTime);
        if (cooldown != null)
        {
            if (detail.RecastDelay == null)
            {
                failures.Add($"{Describe(row)}: missing ability recast delay. Bible={row.CooldownTime}.");
            }
            else
            {
                try
                {
                    detail.RecastDelay(0).Should().BeApproximately(cooldown.Value, 0.001f);
                }
                catch (Exception ex)
                {
                    failures.Add($"{Describe(row)}: recast mismatch. Bible={cooldown.Value}, CodeError={ex.Message}");
                }
            }
        }

        var activationDelay = TryParseActivationSeconds(row.CastingTime);
        if (activationDelay != null)
        {
            var actualDelay = detail.ActivationDelay?.Invoke(0, 0, detail.AbilityLevel) ?? 0f;
            if (Math.Abs(actualDelay - activationDelay.Value) > 0.001f)
            {
                failures.Add($"{Describe(row)}: activation delay mismatch. Bible={activationDelay.Value}, Code={actualDelay}.");
            }
        }

        if (row.CastingTime.Equals("Queued", StringComparison.OrdinalIgnoreCase) &&
            detail.ActivationType != AbilityActivationType.Weapon)
        {
            failures.Add($"{Describe(row)}: casting time is Queued but ability activation type is {detail.ActivationType}.");
        }

        AssertTargetingDescription(row, detail, failures);
    }

    private static void AssertTargetingDescription(
        BiblePerkRow row,
        AbilityDetail detail,
        List<string> failures)
    {
        var targeting = detail.Targeting;
        if (targeting == null || targeting.Shape == AbilityTargetingShapeType.None)
            return;

        static string Number(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        var description = row.Description.ToLowerInvariant();
        var sizeX = Number(targeting.SizeX);
        var sizeY = Number(targeting.SizeY);
        var escapedX = Regex.Escape(sizeX);
        var escapedY = Regex.Escape(sizeY);

        var describesTargeting = targeting.Shape switch
        {
            AbilityTargetingShapeType.Sphere or AbilityTargetingShapeType.HSphere =>
                Regex.IsMatch(
                    description,
                    $@"\b{escapedX}\s*m(?:eter)?s?\b",
                    RegexOptions.CultureInvariant),
            AbilityTargetingShapeType.Cone =>
                Regex.IsMatch(
                    description,
                    $@"{escapedX}\s*m\s*(?:x|by)\s*{escapedY}\s*m\s+cone\b",
                    RegexOptions.CultureInvariant),
            AbilityTargetingShapeType.Rect =>
                Regex.IsMatch(
                    description,
                    $@"{escapedX}\s*m\s*(?:x|by)\s*{escapedY}\s*m\s+line\b",
                    RegexOptions.CultureInvariant),
            _ => true
        };

        if (!describesTargeting)
        {
            failures.Add(
                $"{Describe(row)}: description must state the exact {targeting.Shape} targeting size " +
                $"({sizeX}m x {sizeY}m). Text='{row.Description}'.");
        }
    }

    private static void AssertTameRow(
        BiblePerkRow row,
        PerkLevel level,
        IReadOnlyDictionary<FeatType, AbilityRecord> abilities,
        List<string> failures)
    {
        var expectedLevel = GetExpectedLevel(row.PerkName);
        if (expectedLevel == 1)
        {
            if (!level.GrantedFeats.Contains(FeatType.Tame) || !level.GrantedFeats.Contains(FeatType.CallBeast))
            {
                failures.Add($"{Describe(row)}: Tame I must grant Tame and Call Beast.");
                return;
            }
        }
        else if (level.GrantedFeats.Count != 0)
        {
            failures.Add($"{Describe(row)}: Tame ranks above I should scale the Tame ability without granting new feats.");
            return;
        }

        if (!abilities.TryGetValue(FeatType.Tame, out var tame))
        {
            failures.Add($"{Describe(row)}: Tame feat has no live ability definition.");
            return;
        }

        AssertAbilityDefinitionName(row, tame, failures);
        AssertAbilityCost<AbilityRequirementStamina>(row, "STM", row.STM, tame.Detail, failures, x => x.RequiredSTM);

        if (expectedLevel == 1 && abilities.TryGetValue(FeatType.CallBeast, out var callBeast))
        {
            AssertAbilityDefinitionName(row, callBeast, failures);
        }

        var cooldown = TryParseDurationSeconds(row.CooldownTime);
        if (cooldown != null && tame.Detail.RecastDelay != null)
        {
            try
            {
                tame.Detail.RecastDelay(0).Should().BeApproximately(cooldown.Value, 0.001f);
            }
            catch (Exception ex)
            {
                failures.Add($"{Describe(row)}: recast mismatch. Bible={cooldown.Value}, CodeError={ex.Message}");
            }
        }

        var activationDelay = TryParseActivationSeconds(row.CastingTime);
        if (activationDelay != null)
        {
            var actualDelay = tame.Detail.ActivationDelay?.Invoke(0, 0, tame.Detail.AbilityLevel) ?? 0f;
            if (Math.Abs(actualDelay - activationDelay.Value) > 0.001f)
            {
                failures.Add($"{Describe(row)}: activation delay mismatch. Bible={activationDelay.Value}, Code={actualDelay}.");
            }
        }
    }

    private static void AssertAbilityDefinitionName(
        BiblePerkRow row,
        AbilityRecord ability,
        List<string> failures)
    {
        var definitionName = ability.DefinitionType.Name;
        const string suffix = "AbilityDefinition";
        if (!definitionName.EndsWith(suffix, StringComparison.Ordinal))
        {
            failures.Add($"{Describe(row)}: ability definition '{definitionName}' should end with {suffix}.");
            return;
        }

        var expected = NormalizeName(GetBaseName(ability.Detail.Name));
        var actualBaseName = definitionName[..^suffix.Length];
        if (ability.Detail.IsMimicryTechnique && actualBaseName.EndsWith("Technique", StringComparison.Ordinal))
        {
            actualBaseName = actualBaseName[..^"Technique".Length];
        }
        var actual = NormalizeName(actualBaseName);
        if (!actual.Equals(expected, StringComparison.Ordinal))
        {
            failures.Add($"{Describe(row)}: ability definition '{definitionName}.cs' does not match ability name '{ability.Detail.Name}'.");
        }
    }

    private static void AssertAbilityCost<TRequirement>(
        BiblePerkRow row,
        string label,
        string value,
        AbilityDetail ability,
        List<string> failures,
        Func<TRequirement, int> readCost)
        where TRequirement : IAbilityActivationRequirement
    {
        var expected = TryParseWholeNumber(value);
        var requirements = ability.Requirements.OfType<TRequirement>().ToArray();

        if (expected == null)
            return;

        if (requirements.Length != 1)
        {
            failures.Add($"{Describe(row)}: expected one {label} requirement of {expected.Value}, found {requirements.Length}.");
            return;
        }

        var actual = readCost(requirements[0]);
        if (actual != expected.Value)
        {
            failures.Add($"{Describe(row)}: {label} cost mismatch. Bible={expected.Value}, Code={actual}.");
        }
    }

    private static IReadOnlyCollection<PerkRecord> BuildPerksWithout2daLookup()
    {
        var result = new List<PerkRecord>();
        var definitionTypes = typeof(IPerkListDefinition).Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(x))
            .OrderBy(x => x.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = Activator.CreateInstance(definitionType)!;
            foreach (var method in definitionType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0 && !x.Name.Contains('<'))
                         .OrderBy(x => x.MetadataToken))
            {
                method.Invoke(definition, null);
            }

            var builder = definitionType
                .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(definition)!;

            var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(builder)!;

            result.AddRange(perks.Select(x => new PerkRecord(x.Key, x.Value)));
        }

        return result;
    }

    private static IReadOnlyDictionary<FeatType, AbilityRecord> BuildAbilities()
    {
        var result = new Dictionary<FeatType, AbilityRecord>();
        var duplicates = new List<string>();
        var definitionTypes = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && typeof(IAbilityListDefinition).IsAssignableFrom(x))
            .OrderBy(x => x.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                if (!result.TryAdd(feat, new AbilityRecord(ability, definitionType)))
                {
                    duplicates.Add($"{feat} in {definitionType.FullName}");
                }
            }
        }

        duplicates.Should().BeEmpty("ability feat definitions must be unique");
        return result;
    }

    private static IReadOnlyCollection<BiblePerkRow> ReadManifest(PathInfo path)
    {
        using var parser = new TextFieldParser(path.FullName);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.ReadFields();

        var rows = new List<BiblePerkRow>();
        while (!parser.EndOfData)
        {
            var cells = parser.ReadFields();
            if (cells == null || cells.Length == 0)
                continue;

            rows.Add(new BiblePerkRow(
                cells[0],
                cells[1],
                cells[2],
                cells[3],
                cells[4],
                cells[5],
                cells[6],
                cells[7],
                cells[8],
                cells[9],
                cells[10],
                cells[11],
                cells[12],
                cells[13],
                cells[14],
                cells[15],
                cells[16],
                cells[17],
                cells.Length > 18 ? cells[18] : string.Empty,
                cells.Length > 19 ? cells[19] : string.Empty,
                cells.Length > 20 ? cells[20] : string.Empty));
        }

        return rows;
    }

    private static IReadOnlyDictionary<int, string> ReadTlkEntries(PathInfo path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path.FullName));
        return document.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("id").GetInt32(),
                entry => entry.TryGetProperty("text", out var text) ? text.GetString() ?? string.Empty : string.Empty);
    }

    private static void AssertTlkDescription(
        BiblePerkRow row,
        string strRefText,
        IReadOnlyDictionary<int, string> tlkEntries,
        List<string> failures,
        string label)
    {
        const int CustomTlkOffset = 16777216;
        if (!int.TryParse(strRefText, out var strRef) || strRef < CustomTlkOffset)
        {
            failures.Add($"{Describe(row)}: {label} should be a custom TLK strref, found '{strRefText}'.");
            return;
        }

        var tlkId = strRef - CustomTlkOffset;
        if (!tlkEntries.TryGetValue(tlkId, out var actual))
        {
            failures.Add($"{Describe(row)}: {label} references missing TLK id {tlkId}.");
            return;
        }

        if (!NormalizeText(actual).Equals(NormalizeText(row.Description), StringComparison.Ordinal))
        {
            failures.Add($"{Describe(row)}: {label} mismatch. Bible='{row.Description}' TLK='{actual}'.");
        }
    }

    private static void AssertConcreteTlkDescription(
        BiblePerkRow row,
        string strRefText,
        IReadOnlyDictionary<int, string> tlkEntries,
        List<string> failures,
        string label)
    {
        const int CustomTlkOffset = 16777216;
        if (!int.TryParse(strRefText, out var strRef) || strRef < CustomTlkOffset)
        {
            failures.Add($"{Describe(row)}: {label} should be a custom TLK strref, found '{strRefText}'.");
            return;
        }

        var tlkId = strRef - CustomTlkOffset;
        if (!tlkEntries.TryGetValue(tlkId, out var actual))
        {
            failures.Add($"{Describe(row)}: {label} references missing TLK id {tlkId}.");
            return;
        }

        if (Regex.IsMatch(actual, @"(?i)(TBD|Description Placeholder|^.+ Description$)"))
        {
            failures.Add($"{Describe(row)}: {label} uses placeholder TLK text '{actual}'.");
        }
    }

    private static ReviewScope GetReviewScope(BiblePerkRow row)
    {
        if (string.IsNullOrWhiteSpace(row.PerkName))
            return new ReviewScope(false, "Skipped: no perk name");

        if (OutOfScopeTabs.Contains(row.Tab))
            return new ReviewScope(false, $"Skipped: out-of-scope tab {row.Tab}");

        // Mimicry techniques are learned creature abilities rather than purchasable perks, but they
        // remain fully in scope. The line-by-line review validates them through their live ability,
        // feat, spell, TLK, targeting, tier, and slot metadata instead of looking for a perk level.
        if (IsMimicryTechniqueRow(row))
            return new ReviewScope(true, "Scoped implemented Mimicry technique");

        if (!ScopedTypes.Contains(row.Type))
            return new ReviewScope(false, $"Skipped: non-scoped type {row.Type}");

        // Espionage was implemented while its workbook rows still said Design. Keep those rows in
        // the audit so stale Dev Status metadata cannot hide implementation drift.
        if (!ImplementedStatuses.Contains(row.DevStatus) &&
            !row.Tab.Equals("Espionage", StringComparison.OrdinalIgnoreCase))
            return new ReviewScope(false, $"Skipped: dev status {row.DevStatus}");

        return new ReviewScope(true, "Scoped implemented");
    }

    private static string DescribeRequirements(PerkLevel level)
    {
        if (level.Requirements.Count == 0)
            return "-";

        return string.Join("; ", level.Requirements.Select(DescribeRequirement));
    }

    private static string DescribeRequirement(IPerkRequirement requirement)
    {
        return requirement switch
        {
            PerkRequirementSkill skill => $"{skill.Type} {skill.RequiredRank}",
            PerkRequirementQuest quest => $"Quest {quest.QuestId}",
            PerkRequirementBeastLevel => $"Beast Level {typeof(PerkRequirementBeastLevel).GetField("_requiredLevel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)}",
            PerkRequirementBeastRole => $"Beast Role {typeof(PerkRequirementBeastRole).GetField("_requiredRole", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)}",
            PerkRequirementCharacterType => $"Character Type {typeof(PerkRequirementCharacterType).GetField("_requiredCharacterType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)}",
            PerkRequirementMustHavePerk => $"Must Have Perk {typeof(PerkRequirementMustHavePerk).GetField("_mustHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)} level {typeof(PerkRequirementMustHavePerk).GetField("_mustHavePerkLevel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)}",
            PerkRequirementCannotHavePerk => $"Cannot Have Perk {typeof(PerkRequirementCannotHavePerk).GetField("_cannotHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)}",
            PerkRequirementUnlock => $"Unlock {typeof(PerkRequirementUnlock).GetField("_perkType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)}",
            _ => requirement.GetType().Name
        };
    }

    private static string DescribeAbilityEvidence(PerkLevel level, IReadOnlyDictionary<FeatType, AbilityRecord> abilities)
    {
        var evidence = new List<string>();
        foreach (var feat in level.GrantedFeats.Where(feat => feat != FeatType.Invalid))
        {
            if (!abilities.TryGetValue(feat, out var ability))
            {
                evidence.Add($"{feat}: no active ability definition");
                continue;
            }

            var detail = ability.Detail;
            evidence.Add(DescribeAbilityEvidence(feat, ability));
        }

        return evidence.Count == 0
            ? "-"
            : string.Join("; ", evidence);
    }

    private static string DescribeAbilityEvidence(FeatType feat, AbilityRecord ability)
    {
        var detail = ability.Detail;
        return string.Join(
            " ",
            $"{feat}:",
            $"{ability.DefinitionType.Name}.cs",
            $"name='{detail.Name}'",
            $"level={detail.AbilityLevel}",
            $"skill={detail.SkillType}",
            $"activation={detail.ActivationType}",
            $"FP={ReadAbilityCost<AbilityRequirementFP>(detail, requirement => requirement.RequiredFP)}",
            $"STM={ReadAbilityCost<AbilityRequirementStamina>(detail, requirement => requirement.RequiredSTM)}",
            $"cast={ReadActivationDelay(detail)}",
            $"cooldown={ReadRecastDelay(detail)}");
    }

    private static string ReadAbilityCost<TRequirement>(
        AbilityDetail detail,
        Func<TRequirement, int> readCost)
        where TRequirement : IAbilityActivationRequirement
    {
        var requirements = detail.Requirements.OfType<TRequirement>().ToArray();
        return requirements.Length == 0
            ? "-"
            : string.Join("+", requirements.Select(requirement => readCost(requirement).ToString(CultureInfo.InvariantCulture)));
    }

    private static string ReadActivationDelay(AbilityDetail detail)
    {
        var seconds = detail.ActivationDelay?.Invoke(0, 0, detail.AbilityLevel) ?? 0f;
        return seconds.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string ReadRecastDelay(AbilityDetail detail)
    {
        if (detail.RecastDelay == null)
            return "-";

        try
        {
            return detail.RecastDelay(0).ToString("0.###", CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            return $"error:{ex.GetType().Name}";
        }
    }

    private static string DescribeFeatDescriptionEvidence(
        BiblePerkRow row,
        PerkLevel level,
        IReadOnlyDictionary<int, Dictionary<string, string>> featRows,
        IReadOnlyDictionary<int, string> tlkEntries,
        List<string> failures)
    {
        var evidence = new List<string>();
        foreach (var feat in level.GrantedFeats.Where(feat => feat != FeatType.Invalid))
        {
            if (!featRows.TryGetValue((int)feat, out var featRow))
            {
                evidence.Add($"{feat}: missing feat.2da row");
                failures.Add($"{Describe(row)}: granted feat {feat} is missing from feat.2da.");
                continue;
            }

            var descriptionRef = featRow.GetValueOrDefault("DESCRIPTION");
            if (IsAuxiliaryGrantedFeat(row, feat))
            {
                evidence.Add($"{feat}: {DescribeConcreteTlkReference(row, descriptionRef, tlkEntries, failures, $"{feat} feat.2da DESCRIPTION")}");
            }
            else
            {
                evidence.Add($"{feat}: {DescribeExactTlkReference(row, descriptionRef, tlkEntries, failures, $"{feat} feat.2da DESCRIPTION")}");
            }
        }

        return evidence.Count == 0
            ? "-"
            : string.Join("; ", evidence);
    }

    private static string DescribeSpellDescriptionEvidence(
        BiblePerkRow row,
        PerkLevel level,
        IReadOnlyDictionary<FeatType, AbilityRecord> abilities,
        IReadOnlyDictionary<int, Dictionary<string, string>> featRows,
        IReadOnlyDictionary<int, Dictionary<string, string>> spellRows,
        IReadOnlyDictionary<int, string> tlkEntries,
        List<string> failures)
    {
        var evidence = new List<string>();
        foreach (var feat in level.GrantedFeats.Where(feat => feat != FeatType.Invalid))
        {
            if (!abilities.ContainsKey(feat) || IsAuxiliaryGrantedFeat(row, feat))
                continue;

            if (!featRows.TryGetValue((int)feat, out var featRow) ||
                !featRow.TryGetValue("SPELLID", out var spellIdText) ||
                spellIdText.Equals("****", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(spellIdText, out var spellId))
            {
                evidence.Add($"{feat}: no spell row");
                continue;
            }

            if (!spellRows.TryGetValue(spellId, out var spellRow))
            {
                evidence.Add($"{feat}: missing spells.2da row {spellId}");
                failures.Add($"{Describe(row)}: granted feat {feat} references missing spells.2da row {spellId}.");
                continue;
            }

            evidence.Add($"{feat}: {DescribeExactTlkReference(row, spellRow.GetValueOrDefault("SpellDesc"), tlkEntries, failures, $"{feat} spells.2da SpellDesc")}");
        }

        return evidence.Count == 0
            ? "-"
            : string.Join("; ", evidence);
    }

    private static string DescribeExactTlkReference(
        BiblePerkRow row,
        string strRefText,
        IReadOnlyDictionary<int, string> tlkEntries,
        List<string> failures,
        string label)
    {
        const int CustomTlkOffset = 16777216;
        if (!int.TryParse(strRefText, out var strRef) || strRef < CustomTlkOffset)
        {
            failures.Add($"{Describe(row)}: {label} should be a custom TLK strref, found '{strRefText}'.");
            return $"FAIL ref={strRefText}";
        }

        var tlkId = strRef - CustomTlkOffset;
        if (!tlkEntries.TryGetValue(tlkId, out var actual))
        {
            failures.Add($"{Describe(row)}: {label} references missing TLK id {tlkId}.");
            return $"FAIL missing TLK {tlkId}";
        }

        if (!NormalizeText(actual).Equals(NormalizeText(row.Description), StringComparison.Ordinal))
        {
            failures.Add($"{Describe(row)}: {label} mismatch. Bible='{row.Description}' TLK='{actual}'.");
            return $"FAIL TLK {tlkId}";
        }

        return $"PASS exact Bible text TLK {tlkId}";
    }

    private static string DescribeConcreteTlkReference(
        BiblePerkRow row,
        string strRefText,
        IReadOnlyDictionary<int, string> tlkEntries,
        List<string> failures,
        string label)
    {
        const int CustomTlkOffset = 16777216;
        if (!int.TryParse(strRefText, out var strRef) || strRef < CustomTlkOffset)
        {
            failures.Add($"{Describe(row)}: {label} should be a custom TLK strref, found '{strRefText}'.");
            return $"FAIL ref={strRefText}";
        }

        var tlkId = strRef - CustomTlkOffset;
        if (!tlkEntries.TryGetValue(tlkId, out var actual))
        {
            failures.Add($"{Describe(row)}: {label} references missing TLK id {tlkId}.");
            return $"FAIL missing TLK {tlkId}";
        }

        if (Regex.IsMatch(actual, @"(?i)(TBD|Description Placeholder|^.+ Description$)"))
        {
            failures.Add($"{Describe(row)}: {label} uses placeholder TLK text '{actual}'.");
            return $"FAIL placeholder TLK {tlkId}";
        }

        return $"PASS concrete non-placeholder TLK {tlkId}";
    }

    private static void WriteImplementationReview(string path, IReadOnlyList<ImplementationReviewRow> rows)
    {
        var columns = new[]
        {
            "Verdict",
            "Scope",
            "Tab",
            "BibleRow",
            "Style",
            "BiblePrice",
            "CodePrice",
            "PerkName",
            "BibleSkillRequirements",
            "CodeRequirements",
            "BibleCharacterType",
            "BibleType",
            "BiblePrimaryStat",
            "BibleSecondaryStat",
            "BibleScalingSource",
            "BibleCrossSkill",
            "BibleFP",
            "BibleSTM",
            "BibleCastingTime",
            "BibleCooldownTime",
            "BibleDevStatus",
            "BibleAdditionalRequirements",
            "BibleNotes",
            "BibleDescription",
            "CodeDescription",
            "CodePerk",
            "GrantedFeats",
            "AbilityEvidence",
            "FeatDescriptionEvidence",
            "SpellDescriptionEvidence",
            "Findings"
        };
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", columns.Select(EscapeCsv)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                row.Verdict,
                row.Scope,
                row.Tab,
                row.BibleRow,
                row.Style,
                row.BiblePrice,
                row.CodePrice,
                row.PerkName,
                row.BibleSkillRequirements,
                row.CodeRequirements,
                row.BibleCharacterType,
                row.BibleType,
                row.BiblePrimaryStat,
                row.BibleSecondaryStat,
                row.BibleScalingSource,
                row.BibleCrossSkill,
                row.BibleFP,
                row.BibleSTM,
                row.BibleCastingTime,
                row.BibleCooldownTime,
                row.BibleDevStatus,
                row.BibleAdditionalRequirements,
                row.BibleNotes,
                row.BibleDescription,
                row.CodeDescription,
                row.CodePerk,
                row.GrantedFeats,
                row.AbilityEvidence,
                row.FeatDescriptionEvidence,
                row.SpellDescriptionEvidence,
                row.Findings
            }.Select(EscapeCsv)));
        }

        File.WriteAllText(path, builder.ToString());
    }

    private static string EscapeCsv(string value)
    {
        value ??= string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static bool IsScopedImplementedRow(BiblePerkRow row)
    {
        // Mimicry techniques use their own in-scope ability review rather than the perk-level path.
        return !OutOfScopeTabs.Contains(row.Tab) &&
               !IsMimicryTechniqueRow(row) &&
               ScopedTypes.Contains(row.Type) &&
               (ImplementedStatuses.Contains(row.DevStatus) ||
                row.Tab.Equals("Espionage", StringComparison.OrdinalIgnoreCase)) &&
               !string.IsNullOrWhiteSpace(row.PerkName);
    }

    private static bool IsMimicryTechniqueRow(BiblePerkRow row)
    {
        return row.Tab.Equals("Mimicry", StringComparison.OrdinalIgnoreCase) &&
               row.Style.Equals("Technique", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] GetExpectedWeaponProgressionTypePattern(string styleKey, string[] defaultPattern)
    {
        return WeaponProgressionTypePatternByStyle.GetValueOrDefault(styleKey, defaultPattern);
    }

    private static int[] GetExpectedWeaponProgressionPricePattern(string styleKey, int[] defaultPattern)
    {
        return WeaponProgressionPricePatternByStyle.GetValueOrDefault(styleKey, defaultPattern);
    }

    private static bool IsNativeStealthRow(BiblePerkRow row)
    {
        return row.Tab.Equals("Espionage", StringComparison.OrdinalIgnoreCase) &&
               GetBaseName(row.PerkName).Equals("Stealth", StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNativeStealthRow(
        BiblePerkRow row,
        PerkLevel level,
        PathInfo root,
        List<string> failures)
    {
        if (!row.Type.Equals("Toggle", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{Describe(row)}: native Stealth must be documented as a Toggle.");
        }

        if (level.GrantedFeats.Any(feat => feat != FeatType.Invalid))
        {
            failures.Add($"{Describe(row)}: native Stealth must not grant a duplicate custom feat.");
        }

        if (TryParseActivationSeconds(row.CastingTime) != null ||
            TryParseDurationSeconds(row.CooldownTime) != null)
        {
            failures.Add($"{Describe(row)}: native Stealth must not declare custom casting or cooldown timing.");
        }

        var abilityPath = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Espionage",
            "StealthAbilityDefinition.cs");
        if (File.Exists(abilityPath))
        {
            failures.Add($"{Describe(row)}: duplicate Stealth ability definition still exists.");
        }
    }

    private static bool ShouldValidateAsActiveAbility(BiblePerkRow row, PerkLevel level)
    {
        if (!IsActiveType(row.Type))
            return false;

        if (!row.Type.Equals("Capstone", StringComparison.OrdinalIgnoreCase))
            return true;

        return level.GrantedFeats.Any(feat => !IsPassiveIconTraitFeat(feat));
    }

    private static bool IsActiveType(string type)
    {
        return type.Equals("Aura", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Capstone", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Combat", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Stance", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Toggle", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTraitLikeType(string type)
    {
        return type.Equals("Trait", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Capstone", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProgressionType(string type)
    {
        return type;
    }

    private static bool IsPassiveIconTraitFeat(FeatType feat)
    {
        return feat.ToString().EndsWith("Trait", StringComparison.Ordinal);
    }

    private static bool IsPassiveTraitFeat(
        FeatType feat,
        IReadOnlyDictionary<FeatType, AbilityRecord> abilities)
    {
        return IsPassiveIconTraitFeat(feat) ||
               !abilities.ContainsKey(feat);
    }

    private static bool IsTameRow(BiblePerkRow row)
    {
        return row.Tab.Equals("Beast Mastery", StringComparison.OrdinalIgnoreCase) &&
               row.Style.Equals("Training", StringComparison.OrdinalIgnoreCase) &&
               GetBaseName(row.PerkName).Equals("Tame", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBeastPerkRow(BiblePerkRow row)
    {
        return row.Tab.Equals("Beast Mastery", StringComparison.OrdinalIgnoreCase) &&
               row.CharacterType.Equals("Beast", StringComparison.OrdinalIgnoreCase);
    }

    private static SkillRequirement? TryParseSkillRequirement(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
            return null;

        foreach (var (name, skill) in SkillNames.OrderByDescending(x => x.Key.Length))
        {
            var match = Regex.Match(value, $@"^{Regex.Escape(name)}\s+(\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;

            return new SkillRequirement(skill, ParseWholeNumber(match.Groups[1].Value));
        }

        return null;
    }

    private static int? ParseMimicrySkillRequirement(BiblePerkRow row, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(row.SkillRequirements) || row.SkillRequirements == "-")
            return 0;

        var requirement = TryParseSkillRequirement(row.SkillRequirements);
        if (requirement is { } parsed && parsed.Skill == SkillType.Mimicry && parsed.Rank is >= 0 and <= 50)
            return parsed.Rank;

        failures.Add($"{Describe(row)}: unsupported Mimicry skill requirement '{row.SkillRequirements}'.");
        return null;
    }

    private static int? TryParseBeastLevelRequirement(BiblePerkRow row)
    {
        return IsBeastPerkRow(row)
            ? TryParseWholeNumber(row.SkillRequirements)
            : null;
    }

    private static BeastRoleType? TryParseBeastRoleRequirement(BiblePerkRow row)
    {
        if (!IsBeastPerkRow(row))
            return null;

        return row.Style switch
        {
            "Balanced" => BeastRoleType.Balanced,
            "Bruiser" => BeastRoleType.Bruiser,
            "Damage" => BeastRoleType.Damage,
            "Evasion" => BeastRoleType.Evasion,
            "Force" => BeastRoleType.Force,
            "Tank" => BeastRoleType.Tank,
            _ => null
        };
    }

    private static CharacterType? TryParseCharacterType(string value)
    {
        return value switch
        {
            "Force" => CharacterType.ForceSensitive,
            "Standard" => CharacterType.Standard,
            _ => null
        };
    }

    private static PerkCategoryType? TryGetExpectedCategory(BiblePerkRow row)
    {
        return (row.Tab, row.Style) switch
        {
            ("Armor", "General") => PerkCategoryType.General,
            ("Beast Mastery", "Balanced") => PerkCategoryType.BeastBalanced,
            ("Beast Mastery", "Bioengineer") => PerkCategoryType.BeastMasteryIncubation,
            ("Beast Mastery", "Bruiser") => PerkCategoryType.BeastBruiser,
            ("Beast Mastery", "Damage") => PerkCategoryType.BeastDamage,
            ("Beast Mastery", "Evasion") => PerkCategoryType.BeastEvasion,
            ("Beast Mastery", "Force") => PerkCategoryType.BeastForce,
            ("Beast Mastery", "Tank") => PerkCategoryType.BeastTank,
            ("Beast Mastery", "Training") => PerkCategoryType.BeastMasteryTraining,
            ("Devices", "Assault Gadgets") => PerkCategoryType.DevicesAssaultGadgets,
            ("Devices", "Field Engineer") => PerkCategoryType.DevicesFieldEngineer,
            ("Devices", "Field Support") => PerkCategoryType.DevicesFieldSupport,
            ("Devices", "Grenadier") => PerkCategoryType.DevicesGrenadier,
            ("First Aid", "Combat Pharmacology") => PerkCategoryType.FirstAidCombatPharmacology,
            ("First Aid", "Trauma Medic") => PerkCategoryType.FirstAidTraumaMedic,
            ("Engineering", "Droidcraft") => PerkCategoryType.Engineering,
            ("Espionage", "Infiltrator") => PerkCategoryType.EspionageInfiltrator,
            ("Espionage", "Saboteur") => PerkCategoryType.EspionageSaboteur,
            ("Espionage", "Tradecraft") => PerkCategoryType.EspionageTradecraft,
            ("Fabrication", "Invention") => PerkCategoryType.Fabrication,
            ("Force", "Alter") => PerkCategoryType.ForceAlter,
            ("Force", "Control") => PerkCategoryType.ForceControl,
            ("Force", "Sense") => PerkCategoryType.ForceSense,
            ("Gathering", "General") => PerkCategoryType.Gathering,
            ("Gathering", "Harvesting") => PerkCategoryType.Gathering,
            ("Gathering", "Scavenging") => PerkCategoryType.Gathering,
            ("Heavy Vibroblade", "Immortal") => PerkCategoryType.HeavyVibrobladeDefense,
            ("Heavy Vibroblade", "Berserker") => PerkCategoryType.HeavyVibrobladeOffense,
            ("Katar", "Iron Guard") => PerkCategoryType.KatarIronGuard,
            ("Katar", "Scrapper") => PerkCategoryType.KatarVenomCurrent,
            ("Leadership", "Diplomat") => PerkCategoryType.Leadership,
            ("Leadership", "Field Steward") => PerkCategoryType.LeadershipFieldSteward,
            ("Leadership", "Mayor") => PerkCategoryType.Leadership,
            ("Leadership", "Vanguard Command") => PerkCategoryType.LeadershipVanguardCommand,
            ("Lightsaber", "Severance") => PerkCategoryType.LightsaberDefense,
            ("Lightsaber", "Ward") => PerkCategoryType.LightsaberOffense,
            ("Mimicry", "Mimicry") => PerkCategoryType.Mimicry,
            ("Piloting", "Shipwright") => PerkCategoryType.Piloting,
            ("Pistol", "Gambler") => PerkCategoryType.PistolGunslinger,
            ("Pistol", "Skirmisher") => PerkCategoryType.PistolSkirmisher,
            ("Rifle", "Marksman") => PerkCategoryType.RifleMarksman,
            ("Rifle", "Suppression") => PerkCategoryType.RiflePacification,
            ("Saberstaff", "Conduit") => PerkCategoryType.SaberstaffConduit,
            ("Saberstaff", "Tempest") => PerkCategoryType.SaberstaffTempest,
            ("Spear", "Vigor") => PerkCategoryType.SpearDamage,
            ("Spear", "Disabler") => PerkCategoryType.SpearDisabler,
            ("Staff", "Crusher") => PerkCategoryType.StaffCrusher,
            ("Staff", "Sentinel") => PerkCategoryType.StaffSentinel,
            ("Throwing", "Ordnance") => PerkCategoryType.ThrowingBombardier,
            ("Throwing", "Flurry") => PerkCategoryType.ThrowingDeadeye,
            ("Twin Blade", "Cyclone") => PerkCategoryType.TwinBladeCyclone,
            ("Twin Blade", "Lacerator") => PerkCategoryType.TwinBladeDuelist,
            ("Vibroblade", "Bulwark") => PerkCategoryType.VibrobladeDefense,
            ("Vibroblade", "Frenzy") => PerkCategoryType.VibrobladeOffense,
            ("Vibroknife", "Cutthroat") => PerkCategoryType.VibroknifeSaboteur,
            ("Vibroknife", "Saboteur") => PerkCategoryType.VibroknifeSaboteur,
            ("Vibroknife", "Shadow") => PerkCategoryType.VibroknifeShadow,
            _ => null
        };
    }

    private static PerkCategoryType[] GetExpectedCategories(BiblePerkRow row)
    {
        var category = TryGetExpectedCategory(row);
        return category.HasValue
            ? new[] { category.Value }
            : Array.Empty<PerkCategoryType>();
    }

    private static bool HasSkillRequirement(PerkLevel level, SkillType skill, int rank)
    {
        return level.Requirements
            .OfType<PerkRequirementSkill>()
            .Any(x => x.Type == skill && x.RequiredRank == rank);
    }

    private static bool HasBeastLevelRequirement(PerkLevel level, int beastLevel)
    {
        return level.Requirements
            .OfType<PerkRequirementBeastLevel>()
            .Any(x => (int)typeof(PerkRequirementBeastLevel)
                .GetField("_requiredLevel", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(x)! == beastLevel);
    }

    private static bool HasBeastRoleRequirement(PerkLevel level, BeastRoleType role)
    {
        return level.Requirements
            .OfType<PerkRequirementBeastRole>()
            .Any(x => typeof(PerkRequirementBeastRole)
                .GetField("_requiredRole", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(x)!
                .Equals(role));
    }

    private static bool HasCharacterTypeRequirement(PerkLevel level, CharacterType characterType)
    {
        return level.Requirements
            .OfType<PerkRequirementCharacterType>()
            .Any(x => typeof(PerkRequirementCharacterType)
                .GetField("_requiredCharacterType", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(x)!
                .Equals(characterType));
    }

    private static int ParseWholeNumber(string value)
    {
        return (int)decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static int? TryParseWholeNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
            return null;

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? (int)parsed
            : null;
    }

    private static float? TryParseActivationSeconds(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value == "-" ||
            value.Equals("Channel", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (value.Equals("Instant", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Queued", StringComparison.OrdinalIgnoreCase))
        {
            return 0f;
        }

        return TryParseDurationSeconds(value);
    }

    private static float? TryParseDurationSeconds(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
            return null;

        var total = 0f;
        var any = false;
        foreach (Match match in Regex.Matches(value, @"(\d+(?:\.\d+)?)\s*(minute|minutes|second|seconds)", RegexOptions.IgnoreCase))
        {
            var amount = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            total += match.Groups[2].Value.StartsWith("minute", StringComparison.OrdinalIgnoreCase)
                ? amount * 60f
                : amount;
            any = true;
        }

        return any ? total : null;
    }

    private static string GetBaseName(string perkName)
    {
        return Regex.Replace(perkName, @"\s+(I|II|III|IV|V|VI|VII|VIII|IX|X)$", "", RegexOptions.IgnoreCase);
    }

    private static bool HasRankSuffix(string perkName)
    {
        return Regex.IsMatch(perkName, @"\s+(I|II|III|IV|V|VI|VII|VIII|IX|X)$", RegexOptions.IgnoreCase);
    }

    private static string ConvertRomanSuffixToRankNumber(string value)
    {
        var match = Regex.Match(value, @"^(.*)\s+(I|II|III|IV|V|VI|VII|VIII|IX|X)$", RegexOptions.IgnoreCase);
        if (!match.Success)
            return value;

        return $"{match.Groups[1].Value}{GetExpectedLevel(value)}";
    }

    private static int GetExpectedLevel(string perkName)
    {
        var match = Regex.Match(perkName, @"\s+(I|II|III|IV|V|VI|VII|VIII|IX|X)$", RegexOptions.IgnoreCase);
        if (!match.Success)
            return 1;

        return match.Groups[1].Value.ToUpperInvariant() switch
        {
            "I" => 1,
            "II" => 2,
            "III" => 3,
            "IV" => 4,
            "V" => 5,
            "VI" => 6,
            "VII" => 7,
            "VIII" => 8,
            "IX" => 9,
            "X" => 10,
            _ => 1
        };
    }

    private static string GetRomanNumeral(int value)
    {
        return value switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            6 => "VI",
            7 => "VII",
            8 => "VIII",
            9 => "IX",
            10 => "X",
            _ => value.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string NormalizeName(string value)
    {
        return Regex.Replace(value, "[^A-Za-z0-9]", "").ToLowerInvariant();
    }

    private static string NormalizeText(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private static string Describe(BiblePerkRow row)
    {
        return $"{row.Tab}/{row.Style}/{row.Row} {row.PerkName}";
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR.Game.Server", "Readmes", "CombatUpgradeBiblePerkManifest.csv")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }

    private readonly record struct SkillRequirement(SkillType Skill, int Rank);

    private readonly record struct ReviewScope(bool ShouldValidate, string Description);

    private readonly record struct DroidInstructionTemplate(string Resref, PerkType Perk, int Level, string Name);

    private readonly record struct ExpectedDroidInstruction(PerkType Perk, int Level, string Name);

    private sealed record ImplementationReviewRow(
        string Verdict,
        string Scope,
        string Tab,
        string BibleRow,
        string Style,
        string BiblePrice,
        string CodePrice,
        string PerkName,
        string BibleSkillRequirements,
        string CodeRequirements,
        string BibleCharacterType,
        string BibleType,
        string BiblePrimaryStat,
        string BibleSecondaryStat,
        string BibleScalingSource,
        string BibleCrossSkill,
        string BibleFP,
        string BibleSTM,
        string BibleCastingTime,
        string BibleCooldownTime,
        string BibleDevStatus,
        string BibleAdditionalRequirements,
        string BibleNotes,
        string BibleDescription,
        string CodeDescription,
        string CodePerk,
        string GrantedFeats,
        string AbilityEvidence,
        string FeatDescriptionEvidence,
        string SpellDescriptionEvidence,
        string Findings);

    private sealed record BiblePerkRow(
        string Tab,
        string Row,
        string Style,
        string Price,
        string PerkName,
        string SkillRequirements,
        string CharacterType,
        string Type,
        string Description,
        string PrimaryStat,
        string SecondaryStat,
        string ScalingSource,
        string CrossSkill,
        string FP,
        string STM,
        string CastingTime,
        string CooldownTime,
        string DevStatus,
        string AdditionalRequirements,
        string Notes,
        string Slots);

    private sealed record PerkRecord(PerkType Type, PerkDetail Detail);

    private sealed record AbilityRecord(AbilityDetail Detail, Type DefinitionType);

    private sealed record MimicryReviewEvidence(
        string CodePerk,
        string CodePrice,
        string CodeDescription,
        string CodeRequirements,
        string GrantedFeats,
        string AbilityEvidence,
        string FeatDescriptionEvidence,
        string SpellDescriptionEvidence)
    {
        public static MimicryReviewEvidence Empty { get; } = new("", "", "", "", "", "", "", "");
    }

    private readonly record struct PerkMatch(PerkType Type, PerkDetail Perk, PerkLevel Level);
}

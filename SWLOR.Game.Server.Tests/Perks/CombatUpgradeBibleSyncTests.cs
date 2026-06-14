using System.Globalization;
using System.Reflection;
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
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class CombatUpgradeBibleSyncTests
{
    private static readonly HashSet<string> ScopedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Aura",
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
        "Espionage",
        "Farming",
        "Agriculture",
        "Smithery",
        "Engineering",
        "Fabrication",
        "Research",
        "Gathering"
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
        ["First Aid"] = SkillType.FirstAid,
        ["Force"] = SkillType.Force,
        ["Heavy Vibroblade"] = SkillType.HeavyVibroblade,
        ["Katar"] = SkillType.Katar,
        ["Leadership"] = SkillType.Leadership,
        ["Lightsaber"] = SkillType.Lightsaber,
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

            if (IsActiveType(row.Type))
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
            else if (row.Type.Equals("Trait", StringComparison.OrdinalIgnoreCase))
            {
                if (level.GrantedFeats.Count != 0)
                {
                    failures.Add($"{Describe(row)}: bible type is Trait but code grants feat(s): {string.Join(", ", level.GrantedFeats)}.");
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
        var perks = BuildPerksWithout2daLookup()
            .ToDictionary(x => x.Type, x => x.Detail);
        var expectedInstructions = GetExpectedDroidInstructions(perks)
            .ToDictionary(x => (x.Perk, x.Level), x => x);
        var templatesByPerkLevel = templates
            .GroupBy(x => (x.Perk, x.Level))
            .ToDictionary(x => x.Key, x => x.ToArray());
        var failures = new List<string>();

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
            if (!migrationSource.Contains($"{{ PerkType.{group.Key}, {maxLevel} }}", StringComparison.Ordinal))
                failures.Add($"Obsolete item migration missing current droid max level for {group.Key}={maxLevel}.");
        }

        migrationSource.Should().Contain("\"id_concgren3\"");
        migrationSource.Should().Contain("\"id_tranqshot3\"");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
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
                if (perkLevel.GrantedFeats.Count <= 0)
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

            var expected = NormalizeName(GetBaseName(ability.Detail.Name));
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
        var actual = NormalizeName(definitionName[..^suffix.Length]);
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
                cells[12],
                cells[13],
                cells[14],
                cells[15],
                cells[16],
                cells[17]));
        }

        return rows;
    }

    private static bool IsScopedImplementedRow(BiblePerkRow row)
    {
        return !OutOfScopeTabs.Contains(row.Tab) &&
               ScopedTypes.Contains(row.Type) &&
               ImplementedStatuses.Contains(row.DevStatus) &&
               !string.IsNullOrWhiteSpace(row.PerkName);
    }

    private static bool IsActiveType(string type)
    {
        return !type.Equals("Trait", StringComparison.OrdinalIgnoreCase);
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
            ("Heavy Vibroblade", "Defense") => PerkCategoryType.HeavyVibrobladeDefense,
            ("Heavy Vibroblade", "Offense") => PerkCategoryType.HeavyVibrobladeOffense,
            ("Katar", "Iron Guard") => PerkCategoryType.KatarIronGuard,
            ("Katar", "Venom Current") => PerkCategoryType.KatarVenomCurrent,
            ("Leadership", "Diplomat") => PerkCategoryType.Leadership,
            ("Leadership", "Field Steward") => PerkCategoryType.LeadershipFieldSteward,
            ("Leadership", "Mayor") => PerkCategoryType.Leadership,
            ("Leadership", "Vanguard Command") => PerkCategoryType.LeadershipVanguardCommand,
            ("Lightsaber", "Defense") => PerkCategoryType.LightsaberDefense,
            ("Lightsaber", "Offense") => PerkCategoryType.LightsaberOffense,
            ("Piloting", "Shipwright") => PerkCategoryType.Piloting,
            ("Pistol", "Gunslinger") => PerkCategoryType.PistolGunslinger,
            ("Pistol", "Skirmisher") => PerkCategoryType.PistolSkirmisher,
            ("Rifle", "Marksman") => PerkCategoryType.RifleMarksman,
            ("Rifle", "Pacification") => PerkCategoryType.RiflePacification,
            ("Saberstaff", "Conduit") => PerkCategoryType.SaberstaffConduit,
            ("Saberstaff", "Tempest") => PerkCategoryType.SaberstaffTempest,
            ("Spear", "Damage") => PerkCategoryType.SpearDamage,
            ("Spear", "Disabler") => PerkCategoryType.SpearDisabler,
            ("Staff", "Crusher") => PerkCategoryType.StaffCrusher,
            ("Staff", "Sentinel") => PerkCategoryType.StaffSentinel,
            ("Throwing", "Bombardier") => PerkCategoryType.ThrowingBombardier,
            ("Throwing", "Deadeye") => PerkCategoryType.ThrowingDeadeye,
            ("Twin Blade", "Cyclone") => PerkCategoryType.TwinBladeCyclone,
            ("Twin Blade", "Duelist") => PerkCategoryType.TwinBladeDuelist,
            ("Vibroblade", "Defense") => PerkCategoryType.VibrobladeDefense,
            ("Vibroblade", "Offense") => PerkCategoryType.VibrobladeOffense,
            ("Vibroknife", "Saboteur") => PerkCategoryType.VibroknifeSaboteur,
            ("Vibroknife", "Shadow") => PerkCategoryType.VibroknifeShadow,
            _ => null
        };
    }

    private static PerkCategoryType[] GetExpectedCategories(BiblePerkRow row)
    {
        if (row.Tab.Equals("Force", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                PerkCategoryType.ForceLight,
                PerkCategoryType.ForceDark,
                PerkCategoryType.ForceUniversal
            };
        }

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

    private readonly record struct DroidInstructionTemplate(string Resref, PerkType Perk, int Level, string Name);

    private readonly record struct ExpectedDroidInstruction(PerkType Perk, int Level, string Name);

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
        string CrossSkill,
        string FP,
        string STM,
        string CastingTime,
        string CooldownTime,
        string DevStatus);

    private sealed record PerkRecord(PerkType Type, PerkDetail Detail);

    private sealed record AbilityRecord(AbilityDetail Detail, Type DefinitionType);

    private readonly record struct PerkMatch(PerkType Type, PerkDetail Perk, PerkLevel Level);
}

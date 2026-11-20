using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

public class CombatUpgradeMigrationCoverageTests
{
    private const string PerkTypeEnumMemberPattern = @"^\s*([A-Za-z_]\w*)\s*(?:=\s*-?\d+)?\s*,?\s*(?://.*)?$";

    [Test]
    public void LoggedOutStatusEffects_RemainProcessLocalRuntimeState()
    {
        var root = FindRepositoryRoot();
        var statusEffectService = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "StatusEffect.cs"));

        statusEffectService.Should().Contain("private static readonly Dictionary<string, LoggedOutStatusEffects> _loggedOutPlayerEffects = new();");
        statusEffectService.Should().Contain("_loggedOutPlayerEffects[playerId] = new LoggedOutStatusEffects(player, effects, DateTime.UtcNow);");
        statusEffectService.Should().NotContain("ClearLoggedOutPlayerEffects");
    }

    [Test]
    public void ServerMigrationsFrom22_ReportCurrentMigrationStartWithoutOverallProgress()
    {
        var root = FindRepositoryRoot();
        var migrationService = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Migration.cs"));

        migrationService.Should().Contain("ConsoleProgressMigrationVersion = 22");
        migrationService.Should().Contain("Starting server migration");
        migrationService.Should().NotContain("migrations 22+ pending");
        migrationService.Should().NotContain("migrations 22+ complete");
    }

    [Test]
    public void DroidCpuTemplates_UseArmorAndConcreteWeaponSkillStats()
    {
        var root = FindRepositoryRoot();
        var cpuFiles = Directory
            .EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "d_*cpu*.uti.json")
            .OrderBy(x => x)
            .ToArray();
        var legacyWeaponGroups = new[]
        {
            12,
            13,
            14,
            15
        };
        var concreteWeaponSkills = new[]
        {
            (int)DroidStatSubType.Vibroblade,
            (int)DroidStatSubType.Vibroknife,
            (int)DroidStatSubType.Lightsaber,
            (int)DroidStatSubType.HeavyVibroblade,
            (int)DroidStatSubType.Spear,
            (int)DroidStatSubType.TwinBlade,
            (int)DroidStatSubType.Saberstaff,
            (int)DroidStatSubType.Katar,
            (int)DroidStatSubType.Staff,
            (int)DroidStatSubType.Pistol,
            (int)DroidStatSubType.Rifle,
            (int)DroidStatSubType.Throwing,
            (int)DroidStatSubType.Unarmed,
            (int)DroidStatSubType.TwinPistol
        };

        cpuFiles.Should().NotBeEmpty("droid CPU item templates are the live crafted CPU data");
        var generatorCpuRows = File.ReadLines(Path.Combine(
                root.FullName,
                "SWLOR.CLI",
                "InputFiles",
                "droid_item_list.tsv"))
            .Select(line => line.Split('\t'))
            .Where(row => row.Length > 2 && row[2].Equals("CPU", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(row => row[1], row => row, StringComparer.OrdinalIgnoreCase);
        generatorCpuRows.Should().HaveCount(cpuFiles.Length, "the droid item generator should be able to reproduce every live CPU template");

        foreach (var file in cpuFiles)
        {
            var templateName = Path.GetFileName(file);
            var template = JObject.Parse(File.ReadAllText(file));
            var templateResref = template["TemplateResRef"]!["value"]!.Value<string>()!;
            var stats = template["PropertiesList"]!["value"]!
                .Children<JObject>()
                .Where(prop => GetJsonFieldValue(prop, "PropertyName") == (int)ItemPropertyType.DroidStat)
                .GroupBy(prop => GetJsonFieldValue(prop, "Subtype"))
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(prop => GetJsonFieldValue(prop, "CostValue")).ToArray());
            var tier = stats[(int)DroidStatSubType.Tier].Single();

            stats.Should().ContainKey((int)DroidStatSubType.Armor, $"{templateName} should grant droids armor equipment rank");
            stats[(int)DroidStatSubType.Armor].Should().ContainSingle()
                .Which.Should().Be(GetTierArmorSkillRank(tier), $"{templateName} armor rank should follow CPU tier");
            stats.Keys.Should().Contain(key => concreteWeaponSkills.Contains(key), $"{templateName} should grant concrete weapon skill ranks");

            foreach (var legacyWeaponGroup in legacyWeaponGroups)
            {
                stats.Should().NotContainKey(legacyWeaponGroup, $"{templateName} should not use legacy droid weapon group stat IDs");
            }

            generatorCpuRows.Should().ContainKey(templateResref, $"{templateName} should be reproducible from the droid item generator input");
            var row = generatorCpuRows[templateResref];
            row.Length.Should().BeGreaterThanOrEqualTo(18, $"{templateResref} should define every CPU generator column");
            row[0].Should().Be(template["LocalizedName"]!["value"]!["0"]!.Value<string>());
            GetTsvInt(row, 3).Should().Be(tier, $"{templateResref} generator tier should match the template");
            GetTsvInt(row, 4).Should().Be(stats[(int)DroidStatSubType.AISlots].Single(), $"{templateResref} generator AI slots should match the template");
            GetTsvInt(row, 6).Should().Be(stats[(int)DroidStatSubType.HP].Single(), $"{templateResref} generator HP should match the template");
            GetTsvInt(row, 7).Should().Be(stats[(int)DroidStatSubType.STM].Single(), $"{templateResref} generator STM should match the template");
            GetTsvInt(row, 8).Should().Be(stats[(int)DroidStatSubType.MGT].Single(), $"{templateResref} generator MGT should match the template");
            GetTsvInt(row, 9).Should().Be(stats[(int)DroidStatSubType.PER].Single(), $"{templateResref} generator PER should match the template");
            GetTsvInt(row, 10).Should().Be(stats[(int)DroidStatSubType.VIT].Single(), $"{templateResref} generator VIT should match the template");
            GetTsvInt(row, 11).Should().Be(stats[(int)DroidStatSubType.WIL].Single(), $"{templateResref} generator WIL should match the template");
            GetTsvInt(row, 12).Should().Be(stats[(int)DroidStatSubType.AGI].Single(), $"{templateResref} generator AGI should match the template");
            GetTsvInt(row, 13).Should().Be(stats[(int)DroidStatSubType.SOC].Single(), $"{templateResref} generator SOC should match the template");
            GetTsvInt(row, 14).Should().Be(GetWeaponGroupRank(stats, 115, 116, 117), $"{templateResref} generator one-handed rank should match concrete skills");
            GetTsvInt(row, 15).Should().Be(GetWeaponGroupRank(stats, 118, 119, 120, 121), $"{templateResref} generator two-handed rank should match concrete skills");
            GetTsvInt(row, 16).Should().Be(GetWeaponGroupRank(stats, 122, 123, 128), $"{templateResref} generator martial rank should match concrete skills");
            GetTsvInt(row, 17).Should().Be(GetWeaponGroupRank(stats, 124, 125, 126, 129), $"{templateResref} generator ranged rank should match concrete skills");
            if (GetTsvInt(row, 16) > 0)
                stats.Should().ContainKey((int)DroidStatSubType.Unarmed, $"{templateResref} martial CPUs should grant Unarmed like Katar and Staff");
            if (GetTsvInt(row, 17) > 0)
                stats.Should().ContainKey((int)DroidStatSubType.TwinPistol, $"{templateResref} ranged CPUs should grant Twin Pistol like Pistol, Rifle, and Throwing");
        }
    }

    [Test]
    public void RemovedPerkCoverage_ReadsExplicitAndImplicitEnumMembers()
    {
        const string source = """
            Invalid = 0,
            ExplicitPerk = 42,
            ImplicitPerk,
            FinalImplicit
            """;

        Regex.Matches(source, PerkTypeEnumMemberPattern, RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)
            .Should()
            .Equal("Invalid", "ExplicitPerk", "ImplicitPerk", "FinalImplicit");
    }

    private static int GetJsonFieldValue(JObject obj, string field)
    {
        return obj[field]?["value"]?.Value<int>() ?? 0;
    }

    private static int GetTierArmorSkillRank(int tier)
    {
        return tier switch
        {
            1 => 5,
            2 => 15,
            3 => 25,
            4 => 35,
            5 => 45,
            _ => 0
        };
    }

    private static int GetTsvInt(IReadOnlyList<string> row, int index)
    {
        return string.IsNullOrWhiteSpace(row[index])
            ? 0
            : int.Parse(row[index], CultureInfo.InvariantCulture);
    }

    private static int GetWeaponGroupRank(
        IReadOnlyDictionary<int, int[]> stats,
        params int[] subTypes)
    {
        var ranks = subTypes
            .Select(subType => stats.TryGetValue(subType, out var values) ? values.Single() : 0)
            .Where(rank => rank > 0)
            .Distinct()
            .ToArray();

        if (ranks.Length > 1)
            Assert.Fail($"Concrete droid weapon skill ranks differ within a generator group: {string.Join(", ", ranks)}.");

        return ranks.Length == 0
            ? 0
            : ranks[0];
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}

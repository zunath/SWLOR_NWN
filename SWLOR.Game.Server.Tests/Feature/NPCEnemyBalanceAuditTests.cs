using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class NPCEnemyBalanceAuditTests
{
    private static readonly ExpectedEnemy[] ExpectedAlternateEnemies =
    {
        new("man_ranger_2", "mando_rgr_skin", "npc_mando_rifle", 13, 199, 11, 19, 11, 16, 16, 29, 7, 9, 0, 5, 4, 4, 24, 47),
        new("man_warrior_2", "mando_war_skin", "npc_mando_blade", 14, 203, 11, 16, 20, 11, 16, 21, 27, 5, 7, 4, 3, 7, 20, 31),
        new("v_raivor2", "raivor_skin", "raivor_c_claw", 14, 238, 20, 11, 11, 16, 16, 35, 6, 9, 0, 2, 6, 4, 27, 29),
        new("v_flesheater2", "flesheater_skin", "vellen_claw", 17, 291, 21, 12, 12, 17, 17, 40, 7, 10, 0, 3, 7, 5, 31, 29),
        new("s_app_m", "s_app_hide", "s_app_electro", 24, 363, 14, 20, 25, 14, 20, 32, 42, 9, 11, 6, 7, 11, 32, 32),
        new("ecoterr_2", "ecoter_hide", "npc_eco_rifle", 27, 490, 27, 15, 15, 22, 22, 59, 10, 14, 0, 5, 11, 9, 46, 47),
        new("byysk_guard002", "hu_byyskgua_hide", "vbyyskguardsword", 50, 2441, 42, 24, 24, 34, 34, 152, 26, 24, 2, 11, 21, 19, 94, 31),
    };

    private static readonly ExpectedDualWieldDamage[] ExpectedDualWieldDamageTotals =
    {
        new("s_app", 38),
        new("byysk_warrior", 43),
        new("vdathguard", 81),
        new("vkorrdunmarauder", 73),
        new("byysk_champion", 89),
        new("sith_commando", 34),
        new("vnpcssabot", 70),
        new("vnpcswar3", 59),
    };

    [Test]
    public void KorribanTemples_KeepFrogBossOutOfAmbientSpawnTable()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "SpawnDefinition",
            "KorribanSpawnDefinition.cs"));

        var tableStart = source.IndexOf("_builder.Create(\"KORRIBAN_TEMPLES\"", StringComparison.Ordinal);
        var bossStart = source.IndexOf("private void FrogBoss()", StringComparison.Ordinal);

        tableStart.Should().BeGreaterThanOrEqualTo(0);
        bossStart.Should().BeGreaterThan(tableStart);

        var ambientTable = source[tableStart..bossStart];
        ambientTable.Should().NotContain("\"frogboss\"");

        source.Should().Contain("_builder.Create(\"FrogBoss\", \"Alchemized Frog Boss\")");
        source.Should().Contain(".AddSpawn(ObjectType.Creature, \"frogboss\")");
        source.Should().Contain(".RespawnDelay(120)");
    }

    [Test]
    public void SpawnedAlternateEnemies_HaveCombatUpgradeStats()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedAlternateEnemies)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Resref}.utc.json");
            using var skin = ReadJson(root, "Module", "uti", $"{expected.SkinResref}.uti.json");
            using var weapon = ReadJson(root, "Module", "uti", $"{expected.WeaponResref}.uti.json");

            AssertCreatureHitPoints(utc.RootElement, expected);
            AssertCreatureAttributes(utc.RootElement, expected);
            AssertSkinCombatStats(skin.RootElement, expected);
            AssertWeaponStats(weapon.RootElement, expected);
        }
    }

    [Test]
    public void DualWieldWorldNPCs_TotalRuntimeWeaponDamageMatchesPreset()
    {
        var root = FindRepositoryRoot();

        foreach (var expected in ExpectedDualWieldDamageTotals)
        {
            using var utc = ReadJson(root, "Module", "utc", $"{expected.Resref}.utc.json");
            var rightHand = GetEquippedResref(utc.RootElement, 16);
            var leftHand = GetEquippedResref(utc.RootElement, 32);

            rightHand.Should().NotBeNullOrWhiteSpace(expected.Resref);
            leftHand.Should().NotBeNullOrWhiteSpace(expected.Resref);

            using var rightWeapon = ReadJson(root, "Module", "uti", $"{rightHand}.uti.json");
            using var leftWeapon = ReadJson(root, "Module", "uti", $"{leftHand}.uti.json");

            GetItemPropertyCost(rightWeapon.RootElement, 98).Should().NotBeNull($"{expected.Resref} right-hand weapon must use custom delay");
            GetItemPropertyCost(leftWeapon.RootElement, 98).Should().NotBeNull($"{expected.Resref} left-hand weapon must use custom delay");

            var totalDamage =
                GetItemPropertyCost(rightWeapon.RootElement, 93).GetValueOrDefault() +
                GetItemPropertyCost(leftWeapon.RootElement, 93).GetValueOrDefault();

            totalDamage.Should().Be(expected.TotalDMG, expected.Resref);
            GetString(rightWeapon.RootElement, "TemplateResRef").Should().Be(rightHand);
            GetString(leftWeapon.RootElement, "TemplateResRef").Should().Be(leftHand);
        }
    }

    private static void AssertCreatureHitPoints(JsonElement utc, ExpectedEnemy expected)
    {
        GetInt(utc, "CurrentHitPoints").Should().Be(expected.HP, expected.Resref);
        GetInt(utc, "HitPoints").Should().Be(expected.HP, expected.Resref);
        GetInt(utc, "MaxHitPoints").Should().Be(expected.HP, expected.Resref);
    }

    private static void AssertCreatureAttributes(JsonElement utc, ExpectedEnemy expected)
    {
        GetInt(utc, "Str").Should().Be(expected.Str, expected.Resref);
        GetInt(utc, "Dex").Should().Be(expected.Dex, expected.Resref);
        GetInt(utc, "Wis").Should().Be(expected.Wis, expected.Resref);
        GetInt(utc, "Con").Should().Be(expected.Con, expected.Resref);
        GetInt(utc, "Int").Should().Be(expected.Int, expected.Resref);
    }

    private static void AssertSkinCombatStats(JsonElement skin, ExpectedEnemy expected)
    {
        GetItemPropertyCost(skin, 99).Should().Be(expected.Level, expected.SkinResref);
        GetItemPropertyCost(skin, 96).Should().Be(expected.HP, expected.SkinResref);
        GetItemPropertyCost(skin, 92).Should().Be(expected.Stamina, expected.SkinResref);
        GetItemPropertyCost(skin, 91).Should().Be(expected.FP, expected.SkinResref);
        GetItemPropertyCost(skin, 111).Should().Be(expected.Attack, expected.SkinResref);
        GetItemPropertyCost(skin, 112).Should().Be(expected.ForceAttack, expected.SkinResref);
        GetItemPropertyCost(skin, 117).Should().Be(expected.Evasion, expected.SkinResref);
        GetItemPropertyCost(skin, 94, 1).Should().Be(expected.PhysicalDefense, expected.SkinResref);
        GetItemPropertyCost(skin, 94, 2).Should().Be(expected.ForceDefense, expected.SkinResref);
        GetItemPropertyCost(skin, 98).Should().BeNull("attack delay belongs on equipped weapons, not creature armor");

        GetItemPropertySubtypes(skin, 133)
            .Should()
            .BeEquivalentTo(new[] { 1, 2, 3, 4, 100, 101, 102, 103 }, expected.SkinResref);
    }

    private static void AssertWeaponStats(JsonElement weapon, ExpectedEnemy expected)
    {
        GetItemPropertyCost(weapon, 93).Should().Be(expected.WeaponDMG, expected.WeaponResref);
        GetItemPropertyCost(weapon, 98).Should().Be(expected.WeaponDelay, expected.WeaponResref);
    }

    private static JsonDocument ReadJson(DirectoryInfo root, params string[] pathParts)
    {
        var path = Path.Combine(new[] { root.FullName }.Concat(pathParts).ToArray());
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetProperty("value").GetInt32();
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetProperty("value").GetString() ?? string.Empty;
    }

    private static string GetEquippedResref(JsonElement utc, int slot)
    {
        return utc
            .GetProperty("Equip_ItemList")
            .GetProperty("value")
            .EnumerateArray()
            .Where(entry => entry.GetProperty("__struct_id").GetInt32() == slot)
            .Select(entry => GetString(entry, "EquippedRes"))
            .SingleOrDefault();
    }

    private static int? GetItemPropertyCost(JsonElement item, int propertyName, int? subtype = null)
    {
        return GetItemProperties(item)
            .Where(property =>
                GetInt(property, "PropertyName") == propertyName &&
                (!subtype.HasValue || GetInt(property, "Subtype") == subtype.Value))
            .Select(property => (int?)GetInt(property, "CostValue"))
            .SingleOrDefault();
    }

    private static int[] GetItemPropertySubtypes(JsonElement item, int propertyName)
    {
        return GetItemProperties(item)
            .Where(property => GetInt(property, "PropertyName") == propertyName)
            .Select(property => GetInt(property, "Subtype"))
            .OrderBy(subtype => subtype)
            .ToArray();
    }

    private static IEnumerable<JsonElement> GetItemProperties(JsonElement item)
    {
        return item
            .GetProperty("PropertiesList")
            .GetProperty("value")
            .EnumerateArray();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record ExpectedEnemy(
        string Resref,
        string SkinResref,
        string WeaponResref,
        int Level,
        int HP,
        int Str,
        int Dex,
        int Wis,
        int Con,
        int Int,
        int Stamina,
        int FP,
        int Attack,
        int ForceAttack,
        int Evasion,
        int PhysicalDefense,
        int ForceDefense,
        int WeaponDMG,
        int WeaponDelay);

    private sealed record ExpectedDualWieldDamage(string Resref, int TotalDMG);
}

using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class NPCEnemyBalanceAuditTests
{
    private const int RightHandSlot = 16;
    private const int LeftHandSlot = 32;
    private const int ItemPropertyFP = 91;
    private const int ItemPropertyStamina = 92;
    private const int ItemPropertyDMG = 93;
    private const int ItemPropertyDefense = 94;
    private const int ItemPropertyNPCHP = 96;
    private const int ItemPropertyDelay = 98;
    private const int ItemPropertyNPCLevel = 99;
    private const int ItemPropertyAttack = 111;
    private const int ItemPropertyForceAttack = 112;
    private const int ItemPropertyEvasion = 117;
    private const int ItemPropertyResistance = 133;
    private const int PhysicalDefenseSubtype = 1;
    private const int ForceDefenseSubtype = 2;

    private static readonly int[] ResistanceSubtypes = { 1, 2, 3, 4, 100, 101, 102, 103 };

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

        // This source substring check is intentionally pragmatic but fragile. It avoids adding an AST parser
        // for one spawn-table invariant, but method reordering, renaming FrogBoss, or changing the surrounding
        // source structure can break the ambient-table extraction even when runtime behavior is still correct.
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
            var rightHand = GetEquippedResref(utc.RootElement, RightHandSlot);
            var leftHand = GetEquippedResref(utc.RootElement, LeftHandSlot);

            rightHand.Should().NotBeNullOrWhiteSpace($"{expected.Resref} must have a right-hand weapon in slot {RightHandSlot}");
            leftHand.Should().NotBeNullOrWhiteSpace($"{expected.Resref} must have a left-hand weapon in slot {LeftHandSlot}");

            using var rightWeapon = ReadJson(root, "Module", "uti", $"{rightHand}.uti.json");
            using var leftWeapon = ReadJson(root, "Module", "uti", $"{leftHand}.uti.json");

            GetItemPropertyCost(rightWeapon.RootElement, ItemPropertyDelay).Should().NotBeNull($"{expected.Resref} right-hand weapon must use custom delay");
            GetItemPropertyCost(leftWeapon.RootElement, ItemPropertyDelay).Should().NotBeNull($"{expected.Resref} left-hand weapon must use custom delay");

            var totalDamage =
                GetItemPropertyCost(rightWeapon.RootElement, ItemPropertyDMG).GetValueOrDefault() +
                GetItemPropertyCost(leftWeapon.RootElement, ItemPropertyDMG).GetValueOrDefault();

            totalDamage.Should().Be(expected.TotalDMG, $"{expected.Resref} dual-wield runtime damage should match the World NPC preset total");
            GetString(rightWeapon.RootElement, "TemplateResRef").Should().Be(rightHand, $"{expected.Resref} right-hand weapon template reference should match its equipped resref");
            GetString(leftWeapon.RootElement, "TemplateResRef").Should().Be(leftHand, $"{expected.Resref} left-hand weapon template reference should match its equipped resref");
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
        GetItemPropertyCost(skin, ItemPropertyNPCLevel).Should().Be(expected.Level, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyNPCHP).Should().Be(expected.HP, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyStamina).Should().Be(expected.Stamina, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyFP).Should().Be(expected.FP, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyAttack).Should().Be(expected.Attack, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyForceAttack).Should().Be(expected.ForceAttack, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyEvasion).Should().Be(expected.Evasion, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyDefense, PhysicalDefenseSubtype).Should().Be(expected.PhysicalDefense, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyDefense, ForceDefenseSubtype).Should().Be(expected.ForceDefense, expected.SkinResref);
        GetItemPropertyCost(skin, ItemPropertyDelay).Should().BeNull("attack delay belongs on equipped weapons, not creature armor");

        GetItemPropertySubtypes(skin, ItemPropertyResistance)
            .Should()
            .BeEquivalentTo(ResistanceSubtypes, expected.SkinResref);
    }

    private static void AssertWeaponStats(JsonElement weapon, ExpectedEnemy expected)
    {
        GetItemPropertyCost(weapon, ItemPropertyDMG).Should().Be(expected.WeaponDMG, expected.WeaponResref);
        GetItemPropertyCost(weapon, ItemPropertyDelay).Should().Be(expected.WeaponDelay, expected.WeaponResref);
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

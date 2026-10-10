using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards the player double-weapon corpus against its matched per-end, same-tier one-handed
/// damage baselines and protects fixed-budget legacy/NPC double weapons from broad player tuning.
/// The player rows encode the damage of the comparable one-handed training, Hunter, craft, and
/// named-tier blueprint; purpose-built custom damage bonuses stay as documented per-row values.
/// </summary>
public class DoubleWeaponBalanceTests
{
    private sealed record ExpectedWeapon(string Resref, BaseItem BaseItem, int Damage, int DelayProperty);

    // Damage matches the corresponding one-handed tier/end; UTI delay property costs are displayed as ratings
    // (23 = Delay 230 for Twin Blades, 24 = Delay 240 for Saberstaffs/Twin Electroblades).
    private static readonly ExpectedWeapon[] PlayerWeapons =
    {
        new("asc_trnsabstaff", BaseItem.Saberstaff, 19, 24),
        new("asc_twinblade", BaseItem.TwoBladedSword, 19, 23),
        new("asc_twinelec", BaseItem.TwinElectroBlade, 19, 24),
        new("b_twinblade", BaseItem.TwoBladedSword, 5, 23),
        new("bloodprice_edge", BaseItem.TwoBladedSword, 41, 23),
        new("byysk_twinblade", BaseItem.TwoBladedSword, 15, 23),
        new("cap_sabstaff", BaseItem.TwinElectroBlade, 11, 24),
        new("cap_twinblade", BaseItem.TwoBladedSword, 11, 23),
        new("chi_twinblade", BaseItem.TwoBladedSword, 24, 23),
        new("chi_twinelec", BaseItem.TwinElectroBlade, 24, 24),
        new("circle_twin", BaseItem.TwoBladedSword, 23, 23),
        new("crosswind_edge", BaseItem.TwoBladedSword, 23, 23),
        new("del_twinblade", BaseItem.TwoBladedSword, 13, 23),
        new("duel_splitter", BaseItem.TwoBladedSword, 23, 23),
        new("fld_trnsabstaff", BaseItem.Saberstaff, 7, 24),
        new("fld_twinblade", BaseItem.TwoBladedSword, 7, 23),
        new("fld_twinelec", BaseItem.TwinElectroBlade, 7, 24),
        new("h_twinblade_1", BaseItem.TwoBladedSword, 6, 23),
        new("h_twinblade_2", BaseItem.TwoBladedSword, 10, 23),
        new("h_twinblade_3", BaseItem.TwoBladedSword, 14, 23),
        new("h_twinblade_4", BaseItem.TwoBladedSword, 18, 23),
        new("h_twinblade_5", BaseItem.TwoBladedSword, 22, 23),
        new("h_twinelec_1", BaseItem.TwinElectroBlade, 6, 24),
        new("h_twinelec_2", BaseItem.TwinElectroBlade, 10, 24),
        new("h_twinelec_3", BaseItem.TwinElectroBlade, 14, 24),
        new("h_twinelec_4", BaseItem.TwinElectroBlade, 18, 24),
        new("h_twinelec_5", BaseItem.TwinElectroBlade, 22, 24),
        new("infconduit_l1", BaseItem.Saberstaff, 23, 24),
        new("infconduit_l1_tr", BaseItem.Saberstaff, 23, 24),
        new("infconduit_l2", BaseItem.Saberstaff, 23, 24),
        new("infconduit_l2_tr", BaseItem.Saberstaff, 23, 24),
        new("infconduit_w1", BaseItem.Saberstaff, 41, 24),
        new("infconduit_w1_tr", BaseItem.Saberstaff, 41, 24),
        new("kwi_twinblade", BaseItem.TwoBladedSword, 23, 23),
        new("kwi_twinelec", BaseItem.TwinElectroBlade, 23, 24),
        new("lastcall_edge", BaseItem.TwoBladedSword, 23, 23),
        new("mando_sabstaff", BaseItem.TwinElectroBlade, 11, 24),
        new("mando_twinblade", BaseItem.TwoBladedSword, 11, 23),
        new("oph_twinblade", BaseItem.TwoBladedSword, 21, 23),
        new("poach_twinblade", BaseItem.TwoBladedSword, 14, 23),
        new("prm_trnsabstaff", BaseItem.Saberstaff, 15, 24),
        new("prm_twinblade", BaseItem.TwoBladedSword, 15, 23),
        new("prm_twinelec", BaseItem.TwinElectroBlade, 15, 24),
        new("proto_twinblade", BaseItem.TwoBladedSword, 17, 23),
        new("raider_twinblade", BaseItem.TwoBladedSword, 18, 23),
        new("sabcycl_l1", BaseItem.Saberstaff, 23, 24),
        new("sabcycl_l1_tr", BaseItem.Saberstaff, 23, 24),
        new("sabcycl_l2", BaseItem.Saberstaff, 23, 24),
        new("sabcycl_l2_tr", BaseItem.Saberstaff, 23, 24),
        new("sabcycl_w1", BaseItem.Saberstaff, 41, 24),
        new("sabcycl_w1_tr", BaseItem.Saberstaff, 41, 24),
        new("sc_twinblade", BaseItem.TwoBladedSword, 21, 23),
        new("scarlet_blades", BaseItem.TwoBladedSword, 23, 23),
        new("sith_twinblade", BaseItem.TwoBladedSword, 10, 23),
        new("slw_crosswind", BaseItem.TwoBladedSword, 10, 23),
        new("squall_blades", BaseItem.TwoBladedSword, 23, 23),
        new("ss_custom", BaseItem.Saberstaff, 5, 24),
        new("stormcall_blades", BaseItem.TwoBladedSword, 41, 23),
        new("stw_zerostate", BaseItem.TwoBladedSword, 22, 23),
        new("t_twin_elec", BaseItem.TwinElectroBlade, 5, 24),
        new("t_twinblade", BaseItem.TwoBladedSword, 4, 23),
        new("tit_twinblade", BaseItem.TwoBladedSword, 9, 23),
        new("trn_saberstaff_1", BaseItem.Saberstaff, 5, 24),
        new("trn_saberstaff_2", BaseItem.Saberstaff, 9, 24),
        new("trn_saberstaff_3", BaseItem.Saberstaff, 13, 24),
        new("trn_saberstaff_4", BaseItem.Saberstaff, 17, 24),
        new("trn_saberstaff_5", BaseItem.Saberstaff, 21, 24),
        new("twin_elec_1", BaseItem.TwinElectroBlade, 5, 24),
        new("twin_elec_2", BaseItem.TwinElectroBlade, 9, 24),
        new("twin_elec_3", BaseItem.TwinElectroBlade, 13, 24),
        new("twin_elec_4", BaseItem.TwinElectroBlade, 17, 24),
        new("twin_elec_5", BaseItem.TwinElectroBlade, 21, 24),
        new("vet_trnsabstaff", BaseItem.Saberstaff, 11, 24),
        new("vet_twinblade", BaseItem.TwoBladedSword, 11, 23),
        new("vet_twinelec", BaseItem.TwinElectroBlade, 11, 24),
    };

    // These legacy/NPC weapons are intentionally excluded from player-tier normalization. Their
    // original Delay 290 rating and exact damage budgets are retained; changing them would alter NPC
    // combat budgets or unsupported/static demonstration equipment.
    private static readonly ExpectedWeapon[] FixedBudgetWeapons =
    {
        new("arc_twinblade", BaseItem.TwoBladedSword, 40, 29),
        new("car_twinblade", BaseItem.TwoBladedSword, 32, 29),
        new("cer_twinblade", BaseItem.TwoBladedSword, 37, 29),
        new("crm_twinblade", BaseItem.TwoBladedSword, 54, 29),
        new("darkadept_wp", BaseItem.Saberstaff, 138, 29),
        new("dmnpckoyeestaff", BaseItem.Saberstaff, 68, 29),
        new("mandowarhero_wp", BaseItem.TwoBladedSword, 34, 29),
        new("psychopris4_wp", BaseItem.TwoBladedSword, 24, 29),
        new("sithassassin_wp", BaseItem.Saberstaff, 34, 29),
        new("sithinq2_wp", BaseItem.Saberstaff, 72, 29),
        new("sithinq_wp", BaseItem.Saberstaff, 109, 29),
        new("sithinqapp_wp", BaseItem.Saberstaff, 59, 29),
        new("sithinqinit_wp", BaseItem.Saberstaff, 50, 29),
        new("str_twinblade", BaseItem.TwoBladedSword, 48, 29),
        new("valcs1", BaseItem.Saberstaff, 15, 29),
        new("valcs2", BaseItem.Saberstaff, 19, 29),
        new("valcs3", BaseItem.Saberstaff, 24, 29),
        new("valcs4", BaseItem.Saberstaff, 26, 29),
        new("valgs1", BaseItem.Saberstaff, 18, 29),
        new("valgs2", BaseItem.Saberstaff, 23, 29),
        new("valgs3", BaseItem.Saberstaff, 29, 29),
        new("valgs4", BaseItem.Saberstaff, 31, 29),
        new("valis1", BaseItem.Saberstaff, 15, 29),
        new("valis2", BaseItem.Saberstaff, 19, 29),
        new("valis3", BaseItem.Saberstaff, 24, 29),
        new("valis4", BaseItem.Saberstaff, 26, 29),
        new("valsos1", BaseItem.Saberstaff, 15, 29),
        new("valsos2", BaseItem.Saberstaff, 19, 29),
        new("valsos4", BaseItem.Saberstaff, 26, 29),
        new("valss1", BaseItem.Saberstaff, 15, 29),
        new("valss2", BaseItem.Saberstaff, 19, 29),
        new("valss3", BaseItem.Saberstaff, 24, 29),
        new("valss4", BaseItem.Saberstaff, 26, 29),
        new("valws1", BaseItem.Saberstaff, 18, 29),
        new("valws2", BaseItem.Saberstaff, 23, 29),
        new("valws3", BaseItem.Saberstaff, 29, 29),
        new("valws4", BaseItem.Saberstaff, 31, 29),
        new("vnpcmsentguard", BaseItem.Saberstaff, 53, 29),
        new("vnpcmsentmast002", BaseItem.Saberstaff, 48, 29),
        new("vnpcmstaff1", BaseItem.Saberstaff, 33, 29),
        new("vnpcmstaff2", BaseItem.Saberstaff, 43, 29),
    };

    [Test]
    public void PlayerDoubleWeapons_MatchTheirPerEndTierDamageAndDelay()
    {
        var root = FindRepositoryRoot();
        var utiDirectory = Path.Combine(root, "Module", "uti");
        var expected = PlayerWeapons.ToDictionary(x => x.Resref, StringComparer.OrdinalIgnoreCase);

        foreach (var weapon in PlayerWeapons)
        {
            var stats = ReadBlueprint(Path.Combine(utiDirectory, weapon.Resref + ".uti.json"));
            stats.BaseItem.Should().Be(weapon.BaseItem, weapon.Resref + " must keep its double-weapon base type");
            stats.Damage.Should().Be(weapon.Damage, weapon.Resref + " must match its same-tier one-handed per-end damage");
            stats.DelayProperty.Should().Be(weapon.DelayProperty, weapon.Resref + " must retain its normalized attack-delay property");
        }

        var actualPlayerWeapons = Directory.EnumerateFiles(utiDirectory, "*.uti.json")
            .Select(ReadBlueprint)
            .Where(x => IsDoubleWeapon(x.BaseItem) && (x.DelayProperty == 23 || x.DelayProperty == 24))
            .Select(x => x.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        actualPlayerWeapons.Should().BeEquivalentTo(expected.Keys,
            "every player-tier double weapon must have an explicit same-tier balance expectation");
    }

    [Test]
    public void SupportedDoubleWeaponBaseTypes_UseTheirMatchedWeaponDelay()
    {
        Item.TwinBladeBaseItemTypes.Should().BeEquivalentTo(new[] { BaseItem.DoubleAxe, BaseItem.TwoBladedSword });
        Item.SaberstaffBaseItemTypes.Should().BeEquivalentTo(new[] { BaseItem.Saberstaff, BaseItem.TwinElectroBlade });

        WeaponDelay.GetWeaponDelay(BaseItem.DoubleAxe).Should().Be(ItemPropertyAttackDelay.Delay230);
        WeaponDelay.GetWeaponDelay(BaseItem.TwoBladedSword).Should().Be(ItemPropertyAttackDelay.Delay230);
        WeaponDelay.GetWeaponDelay(BaseItem.Saberstaff).Should().Be(ItemPropertyAttackDelay.Delay240);
        WeaponDelay.GetWeaponDelay(BaseItem.TwinElectroBlade).Should().Be(ItemPropertyAttackDelay.Delay240);
    }

    [Test]
    public void FixedBudgetLegacyDoubleWeapons_RetainOriginalDamageAndDelay290()
    {
        var root = FindRepositoryRoot();
        var utiDirectory = Path.Combine(root, "Module", "uti");
        var expected = FixedBudgetWeapons.ToDictionary(x => x.Resref, StringComparer.OrdinalIgnoreCase);

        foreach (var weapon in FixedBudgetWeapons)
        {
            var stats = ReadBlueprint(Path.Combine(utiDirectory, weapon.Resref + ".uti.json"));
            stats.BaseItem.Should().Be(weapon.BaseItem, weapon.Resref + " must keep its source base type");
            stats.Damage.Should().Be(weapon.Damage, weapon.Resref + " carries a fixed legacy/NPC damage budget");
            stats.DelayProperty.Should().Be(weapon.DelayProperty, weapon.Resref + " retains the original Delay 290 rating");
        }

        var actualFixedBudgetWeapons = Directory.EnumerateFiles(utiDirectory, "*.uti.json")
            .Select(ReadBlueprint)
            .Where(x => IsDoubleWeapon(x.BaseItem) && x.DelayProperty == 29 && x.Damage.HasValue)
            .Select(x => x.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        actualFixedBudgetWeapons.Should().BeEquivalentTo(expected.Keys,
            "every explicit Delay 290 double weapon must be consciously reviewed as a fixed legacy budget");
    }

    private static (string Resref, BaseItem BaseItem, int? Damage, int? DelayProperty) ReadBlueprint(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var resref = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
        var baseItem = (BaseItem)ReadInt(root.GetProperty("BaseItem"));
        var properties = root.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().ToArray();
        var damage = ReadProperty(properties, 93);
        var delay = ReadProperty(properties, 98);
        return (resref, baseItem, damage, delay);
    }

    private static int? ReadProperty(JsonElement[] properties, int propertyName)
    {
        var matches = properties
            .Where(x => ReadInt(x.GetProperty("PropertyName")) == propertyName)
            .Select(x => ReadInt(x.GetProperty("CostValue")))
            .ToArray();
        matches.Length.Should().BeLessThanOrEqualTo(1, "a blueprint should not contain duplicate balance properties");
        return matches.Length == 0 ? null : matches[0];
    }

    private static int ReadInt(JsonElement value) => value.GetProperty("value").GetInt32();

    private static bool IsDoubleWeapon(BaseItem baseItem) =>
        Item.TwinBladeBaseItemTypes.Contains(baseItem) || Item.SaberstaffBaseItemTypes.Contains(baseItem);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module", "uti")))
            directory = directory.Parent;
        directory.Should().NotBeNull("tests should run from within the SWLOR repository");
        return directory!.FullName;
    }
}

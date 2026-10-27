using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Engineer's power routing (plan section 10): each mode raises one subsystem and lowers the other two, no mode
/// raises weapon damage (so a crewed ship never kills faster than Balanced, the balance check in checks.py section L),
/// the math the combat code applies, the change cooldown, the feats and their hak data.
/// </summary>
public class PowerRoutingRulesTests
{
    [Test]
    public void EachMode_RaisesOneSubsystem_AndLowersTheOtherTwo()
    {
        PowerRoutingRules.WeaponCapacitorPercent(ShipPowerRouting.Weapons).Should().Be(-25);
        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Weapons).Should().Be(-10);
        PowerRoutingRules.SpeedPercent(ShipPowerRouting.Weapons).Should().Be(-10);
        PowerRoutingRules.WeaponDamagePercent(ShipPowerRouting.Weapons).Should().Be(0);

        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Shields).Should().Be(15);
        PowerRoutingRules.WeaponDamagePercent(ShipPowerRouting.Shields).Should().Be(-5);
        PowerRoutingRules.SpeedPercent(ShipPowerRouting.Shields).Should().Be(-10);
        PowerRoutingRules.WeaponCapacitorPercent(ShipPowerRouting.Shields).Should().Be(0);

        PowerRoutingRules.SpeedPercent(ShipPowerRouting.Engines).Should().Be(20);
        PowerRoutingRules.WeaponDamagePercent(ShipPowerRouting.Engines).Should().Be(-5);
        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Engines).Should().Be(-10);
        PowerRoutingRules.WeaponCapacitorPercent(ShipPowerRouting.Engines).Should().Be(0);

        PowerRoutingRules.WeaponCapacitorPercent(ShipPowerRouting.Balanced).Should().Be(0);
        PowerRoutingRules.WeaponDamagePercent(ShipPowerRouting.Balanced).Should().Be(0);
        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Balanced).Should().Be(0);
        PowerRoutingRules.SpeedPercent(ShipPowerRouting.Balanced).Should().Be(0);
    }

    [Test]
    public void NoMode_RaisesWeaponDamage_SoACrewedShipNeverOutpacesNSeparateShips()
    {
        foreach (var mode in Enum.GetValues<ShipPowerRouting>())
            PowerRoutingRules.WeaponDamagePercent(mode).Should().BeLessThanOrEqualTo(0, mode.ToString());
    }

    [Test]
    public void CapacitorCost_FallsForWeaponsOnlyInWeaponsMode_RoundedAndAtLeastOne()
    {
        PowerRoutingRules.ApplyCapacitorCost(6, ShipPowerRouting.Weapons, true).Should().Be(5, "4.5 rounds away from zero");
        PowerRoutingRules.ApplyCapacitorCost(3, ShipPowerRouting.Weapons, true).Should().Be(2);
        PowerRoutingRules.ApplyCapacitorCost(1, ShipPowerRouting.Weapons, true).Should().Be(1);
        PowerRoutingRules.ApplyCapacitorCost(0, ShipPowerRouting.Weapons, true).Should().Be(0);
        PowerRoutingRules.ApplyCapacitorCost(4, ShipPowerRouting.Weapons, false).Should().Be(4, "repairers keep their cost");
        PowerRoutingRules.ApplyCapacitorCost(6, ShipPowerRouting.Shields, true).Should().Be(6);
        PowerRoutingRules.ApplyCapacitorCost(6, ShipPowerRouting.Balanced, true).Should().Be(6);
    }

    [Test]
    public void ShieldRegenAndSpeed_FollowTheMode()
    {
        PowerRoutingRules.ApplyShieldRegen(10, ShipPowerRouting.Balanced).Should().BeApproximately(10, 1e-9);
        PowerRoutingRules.ApplyShieldRegen(10, ShipPowerRouting.Weapons).Should().BeApproximately(9, 1e-9);
        PowerRoutingRules.ApplyShieldRegen(10, ShipPowerRouting.Shields).Should().BeApproximately(11.5, 1e-9);
        PowerRoutingRules.ApplyShieldRegen(10, ShipPowerRouting.Engines).Should().BeApproximately(9, 1e-9);

        PowerRoutingRules.ApplySpeed(1.1, ShipPowerRouting.Balanced).Should().BeApproximately(1.1, 1e-9);
        PowerRoutingRules.ApplySpeed(1.0, ShipPowerRouting.Weapons).Should().BeApproximately(0.9, 1e-9);
        PowerRoutingRules.ApplySpeed(1.0, ShipPowerRouting.Shields).Should().BeApproximately(0.9, 1e-9);
        PowerRoutingRules.ApplySpeed(1.0, ShipPowerRouting.Engines).Should().BeApproximately(1.2, 1e-9);
    }

    [Test]
    public void TheRouting_ChangesAtMostEverySixSeconds_AndOnlyToAnotherMode()
    {
        var changed = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        PowerRoutingRules.CanChange(ShipPowerRouting.Balanced, ShipPowerRouting.Weapons, changed, changed.AddSeconds(5.9)).Should().BeFalse();
        PowerRoutingRules.SecondsUntilChange(changed, changed.AddSeconds(5.1)).Should().Be(1);
        PowerRoutingRules.CanChange(ShipPowerRouting.Balanced, ShipPowerRouting.Weapons, changed, changed.AddSeconds(6)).Should().BeTrue();
        PowerRoutingRules.CanChange(ShipPowerRouting.Weapons, ShipPowerRouting.Weapons, changed, changed.AddSeconds(60)).Should().BeFalse();
        PowerRoutingRules.SecondsUntilChange(changed, changed.AddSeconds(30)).Should().Be(0);
        PowerRoutingRules.CanChange(ShipPowerRouting.Balanced, ShipPowerRouting.Shields, default, changed).Should().BeTrue(
            "a ship that never routed power can at once");
        new ShipStatus().PowerRouting.Should().Be(ShipPowerRouting.Balanced, "saved ships load Balanced");
    }

    [Test]
    public void EachMode_HasItsFeat()
    {
        PowerRoutingRules.Feats.Should().Equal(FeatType.ShipPowerBalanced, FeatType.ShipPowerWeapons, FeatType.ShipPowerShields, FeatType.ShipPowerEngines);
        foreach (var mode in Enum.GetValues<ShipPowerRouting>())
        {
            PowerRoutingRules.TryGetMode(PowerRoutingRules.GetFeat(mode), out var back).Should().BeTrue();
            back.Should().Be(mode);
            PowerRoutingRules.GetName(mode).Should().Be(mode.ToString());
            PowerRoutingRules.GetEffect(mode).Should().NotBeNullOrWhiteSpace();
        }

        PowerRoutingRules.TryGetMode(FeatType.ShipAllWeapons, out _).Should().BeFalse();
        ((int)FeatType.ShipPowerBalanced).Should().Be(3351);
        ((int)FeatType.ShipPowerEngines).Should().Be(3354);
    }

    [Test]
    public void RoutingFeats_HaveTheirFeatRowsTextAndIcons()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        var rows = Test2daHelper.Read2da(new FileInfo(Path.Combine(directory!.FullName, "SWLOR_Haks", "sw_2da", "feat.2da")));
        using var tlk = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json")));
        var text = tlk.RootElement.GetProperty("entries").EnumerateArray()
            .ToDictionary(x => x.GetProperty("id").GetInt32(), x => x.GetProperty("text").GetString());

        var expected = new[]
        {
            (FeatType.ShipPowerBalanced, 192886, "ife_pwrbal", "Route Power: Balanced"),
            (FeatType.ShipPowerWeapons, 192888, "ife_pwrwep", "Route Power: Weapons"),
            (FeatType.ShipPowerShields, 192890, "ife_pwrshd", "Route Power: Shields"),
            (FeatType.ShipPowerEngines, 192892, "ife_pwreng", "Route Power: Engines"),
        };

        foreach (var (feat, tlkId, icon, name) in expected)
        {
            var row = rows[(int)feat];
            row["LABEL"].Should().Be(feat.ToString());
            row["FEAT"].Should().Be((16777216 + tlkId).ToString());
            row["DESCRIPTION"].Should().Be((16777216 + tlkId + 1).ToString());
            row["ICON"].Should().Be(icon);
            row["TARGETSELF"].Should().Be("1");
            row["SPELLID"].Should().Be("****");
            text[tlkId].Should().Be(name);
            text[tlkId + 1].Should().StartWith("Engineer station.");

            foreach (var file in new[] { icon, "pr0" + icon[3..], "pr5" + icon[3..] })
                File.Exists(Path.Combine(directory.FullName, "SWLOR_Haks", "sw_ability", file + ".tga")).Should().BeTrue(file);
        }

        text[192889].Should().Contain($"{PowerRoutingRules.WeaponsCapacitorSavingPercent}% less capacitor");
        text[192891].Should().Contain($"rises by {PowerRoutingRules.ShieldRegenBonusPercent}%");
        text[192893].Should().Contain($"rises by {PowerRoutingRules.SpeedBonusPercent}%");
    }
}

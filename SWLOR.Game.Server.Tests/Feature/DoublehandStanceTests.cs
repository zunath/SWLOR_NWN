using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class DoublehandStanceTests
{
    [TestCase(2, 2, false, true, false)]
    [TestCase(1, 1, false, true, false)]
    [TestCase(2, 0, false, true, true)]
    [TestCase(2, 2, true, true, true)]
    [TestCase(0, 2, true, true, true)]
    [TestCase(0, 0, true, false, false)]
    public void RefreshDoesNotReplayEmoteWithoutTransition(int desired, int applied, bool toggle, bool supported, bool expected)
    {
        DoublehandStance.ShouldPlayTransition(desired, applied, toggle, supported).Should().Be(expected);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void GripPreferenceSurvivesSettingsSerialization(bool enabled)
    {
        var settings = new SWLOR.Game.Server.Entity.PlayerSettings { AlternateGripEnabled = enabled };
        var saved = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
        Newtonsoft.Json.JsonConvert.DeserializeObject<SWLOR.Game.Server.Entity.PlayerSettings>(saved)
            .AlternateGripEnabled.Should().Be(enabled);
        Newtonsoft.Json.JsonConvert.DeserializeObject<SWLOR.Game.Server.Entity.PlayerSettings>("{}")
            .AlternateGripEnabled.Should().BeFalse();
    }

    [TestCase(BaseItem.Lightsaber, 1)]
    [TestCase(BaseItem.Saberstaff, 2)]
    [TestCase(BaseItem.Longsword, 1)]
    [TestCase(BaseItem.Electroblade, 1)]
    [TestCase(BaseItem.Katana, 1)]
    [TestCase(BaseItem.QuarterStaff, 2)]
    [TestCase(BaseItem.ShortSpear, 2)]
    [TestCase(BaseItem.TwoBladedSword, 2)]
    [TestCase(BaseItem.TwinElectroBlade, 2)]
    [TestCase(BaseItem.GreatSword, 2)]
    [TestCase(BaseItem.Dagger, 0)]
    public void SelectsStyleOnlyForSupportedWeaponAndEmptyOffhand(BaseItem weapon, int mode)
    {
        DoublehandStance.GetMode(true, weapon, false).Should().Be(mode);
        DoublehandStance.GetMode(false, weapon, false).Should().Be(0);
        DoublehandStance.GetMode(true, weapon, true).Should().Be(0);
        DoublehandStance.GetMode(true, null, false).Should().Be(0);
    }

    [Test]
    public void SwitchingWeaponsAndDisablingClearsPreviousStyleWithoutCycles()
    {
        var maps = new Dictionary<string, string> { ["dodges"] = "custom_dodge", ["throwr"] = "bowshot" };
        void Replace(string a, string b) { if (b == "") maps.Remove(a); else maps[a] = b; }
        DoublehandStance.ApplyTransition(1, 0, false, Replace);
        maps["1hreadyr"].Should().Be("2hreadyr");
        maps["1hparryl"].Should().Be("2hparryl");
        maps["2wslashr"].Should().Be("2hslashr");
        DoublehandStance.ApplyTransition(2, 1, false, Replace);
        maps.Should().NotContainKey("1hreadyr");
        maps["2hreadyr"].Should().Be("1hreadyr");
        maps["plreadyr"].Should().Be("1hreadyr");
        maps["plpause1"].Should().Be("sw_nohold");
        foreach (var prefix in new[] { "pl", "2h", "2w" })
            foreach (var suffix in new[] { "slashl", "slashr", "slasho", "closeh", "closel", "reach" })
                maps.Should().NotContainKey(prefix + suffix);
        maps.Should().NotContainKey("plparryl");
        maps.Should().NotContainKey("2wparryr");
        maps.Should().NotContainKey("2hparryl");
        maps.Should().NotContainKey("plslashr");
        maps.Should().NotContainKey("2hslashr");
        maps.Should().NotContainKey("2wslashr");
        maps.Values.Should().NotContain(value => maps.ContainsKey(value));
        DoublehandStance.ApplyTransition(0, 2, false, Replace);
        maps.Should().HaveCount(2);
        maps["dodges"].Should().Be("custom_dodge");
        maps["throwr"].Should().Be("bowshot");
    }

    [TestCase(1, "2h")]
    [TestCase(2, "1h")]
    public void QueueReleaseRestoresFullSelectedStyle(int mode, string destination)
    {
        var maps = new Dictionary<string, string>();
        DoublehandStance.ApplyTransition(mode, mode, true, (a, b) => { if (b == "") maps.Remove(a); else maps[a] = b; });
        maps.Should().HaveCount(mode == 1 ? 22 : 10);
        maps.Values.Should().OnlyContain(value => value.StartsWith(destination) || (mode == 2 && value == "sw_nohold"));
        maps.Clear();
        DoublehandStance.ApplyTransition(0, 0, true, (a, b) => maps[a] = b);
        maps.Should().BeEmpty();
    }
}

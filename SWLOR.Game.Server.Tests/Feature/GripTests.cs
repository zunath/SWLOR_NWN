using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class GripTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void GripPreferenceSurvivesSettingsSerialization(bool enabled)
    {
        var settings = new SWLOR.Game.Server.Entity.PlayerSettings { AlternateGripEnabled = enabled };
        var saved = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
        Newtonsoft.Json.JsonConvert.DeserializeObject<SWLOR.Game.Server.Entity.PlayerSettings>(saved)!
            .AlternateGripEnabled.Should().Be(enabled);
        Newtonsoft.Json.JsonConvert.DeserializeObject<SWLOR.Game.Server.Entity.PlayerSettings>("{}")!
            .AlternateGripEnabled.Should().BeFalse();
    }

    [TestCase(BaseItem.Lightsaber, 1)]
    [TestCase(BaseItem.Longsword, 1)]
    [TestCase(BaseItem.Electroblade, 1)]
    [TestCase(BaseItem.BastardSword, 1)]
    [TestCase(BaseItem.Katana, 1)]
    [TestCase(BaseItem.Scimitar, 1)]
    [TestCase(BaseItem.BattleAxe, 1)]
    [TestCase(BaseItem.Saberstaff, 0)]
    [TestCase(BaseItem.TwinElectroBlade, 0)]
    [TestCase(BaseItem.GreatSword, 0)]
    [TestCase(BaseItem.QuarterStaff, 0)]
    [TestCase(BaseItem.MagicStaff, 0)]
    [TestCase(BaseItem.Halberd, 0)]
    [TestCase(BaseItem.ShortSpear, 0)]
    [TestCase(BaseItem.Trident, 0)]
    [TestCase(BaseItem.TwoBladedSword, 0)]
    [TestCase(BaseItem.DoubleAxe, 0)]
    [TestCase(BaseItem.Dagger, 0)]
    [TestCase(BaseItem.Katar, 0)]
    [TestCase(BaseItem.PowerGlove, 0)]
    public void OnlyOneHandedBladesReceiveTwoHandedStyle(BaseItem weapon, int expected)
    {
        Grip.GetMode(true, weapon, false).Should().Be(expected);
        Grip.GetMode(false, weapon, false).Should().Be(0);
        Grip.GetMode(true, weapon, true).Should().Be(0);
        Grip.GetMode(true, null, false).Should().Be(0);
    }

    [Test]
    public void EnablingThenDisablingChangesOnlyOneHandedCombatAnimations()
    {
        var mappings = new Dictionary<string, string>
        {
            ["dodges"] = "custom_dodge",
            ["plpause1"] = "native_hold",
            ["xbowr"] = "native_rifle",
            ["plslashl"] = "original_polearm"
        };
        void Replace(string source, string destination)
        {
            if (destination.Length == 0) mappings.Remove(source);
            else mappings[source] = destination;
        }

        Grip.ApplyTransition(1, 0, false, Replace);
        foreach (var source in new[] { "1h", "2w" })
            foreach (var suffix in Grip.CombatSuffixes)
                mappings[source + suffix].Should().Be("2h" + suffix);

        mappings["dodges"].Should().Be("custom_dodge");
        mappings["plpause1"].Should().Be("native_hold");
        mappings["xbowr"].Should().Be("native_rifle");
        mappings["plslashl"].Should().Be("original_polearm");
        mappings.Values.Should().NotContain(value => mappings.ContainsKey(value));

        Grip.ApplyTransition(0, 1, false, Replace);
        mappings.Should().HaveCount(4);
        mappings.Should().ContainKey("dodges").WhoseValue.Should().Be("custom_dodge");
        mappings.Should().ContainKey("plpause1").WhoseValue.Should().Be("native_hold");
    }

    [Test]
    public void QueuedSwingStaysOwnedUntilReleaseAndThenGripReapplies()
    {
        var mappings = new Dictionary<string, string>();
        void Replace(string source, string destination)
        {
            if (destination.Length == 0) mappings.Remove(source);
            else mappings[source] = destination;
        }

        Grip.ApplyTransition(1, 0, false, Replace);
        foreach (var key in QueuedAttackAnimationPlayback.SwingKeys) mappings[key] = "queued_attack";
        Grip.ApplyTransition(0, 1, false, (source, destination) =>
        {
            if (!QueuedAttackAnimationPlayback.SwingKeys.Contains(source)) Replace(source, destination);
        });
        foreach (var key in QueuedAttackAnimationPlayback.SwingKeys) mappings[key].Should().Be("queued_attack");
        mappings.Should().NotContainKey("1hreadyr");

        foreach (var key in QueuedAttackAnimationPlayback.SwingKeys) mappings.Remove(key);
        Grip.ApplyTransition(1, 0, true, Replace);
        mappings["1hslashl"].Should().Be("2hslashl");
        mappings["2wstab"].Should().Be("2hstab");
        mappings["1hparryr"].Should().Be("2hparryr");
    }

    [Test]
    public void ForceRefreshReappliesSelectionWithoutIntroducingStanceEmotes()
    {
        var mappings = new Dictionary<string, string>();
        Grip.ApplyTransition(1, 1, true, (source, destination) => mappings[source] = destination);
        mappings.Should().HaveCount(22);
        mappings.Keys.Should().OnlyContain(source => source.StartsWith("1h") || source.StartsWith("2w"));
        mappings.Keys.Should().NotContain(source => source == "dodges" || source == "plpause1");
    }
}

using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipDataModelTests
{
    /// <summary>Checks the fluent setters store the new hull fields.</summary>
    [Test]
    public void ShipBuilder_SetsMidNodesSizeRoleAndManufacturer()
    {
        var ships = new ShipBuilder()
            .Create("test_ship")
            .MidPowerNodes(3)
            .Size(ShipSize.Medium)
            .Role(ShipRole.Gunship)
            .Manufacturer(ShipManufacturer.Corellian)
            .Build();

        var ship = ships["test_ship"];
        ship.MidPowerNodes.Should().Be(3);
        ship.Size.Should().Be(ShipSize.Medium);
        ship.Role.Should().Be(ShipRole.Gunship);
        ship.Manufacturer.Should().Be(ShipManufacturer.Corellian);
    }

    /// <summary>Checks ships saved before the new properties existed still load with usable collections.</summary>
    [Test]
    public void ShipStatus_DeserializedWithoutNewProperties_HasEmptyCollections()
    {
        const string json = "{\"ItemTag\":\"old_ship\",\"Hull\":10,\"HighPowerModules\":{\"1\":{\"ItemTag\":\"m\"}},\"LowPowerModules\":{}}";

        var status = JsonConvert.DeserializeObject<ShipStatus>(json)!;

        status.HighPowerModules.Should().ContainKey(1);
        status.MidPowerModules.Should().NotBeNull().And.BeEmpty();
        status.Resistances.Should().NotBeNull().And.BeEmpty();
        status.SlotTags.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>Checks the new collections survive a save and load round trip.</summary>
    [Test]
    public void ShipStatus_RoundTripKeepsMidModulesResistancesAndSlotTags()
    {
        var status = new ShipStatus();
        status.MidPowerModules[2] = new ShipStatus.ShipStatusModule { ItemTag = "mid" };
        status.Resistances[ResistanceType.Thermal] = 25;
        status.SlotTags[21] = ShipSlotTag.Sensors;

        var loaded = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status))!;

        loaded.MidPowerModules[2].ItemTag.Should().Be("mid");
        loaded.Resistances[ResistanceType.Thermal].Should().Be(25);
        loaded.SlotTags[21].Should().Be(ShipSlotTag.Sensors);
    }

    /// <summary>Checks the mid slots map to ten distinct feats that do not overlap high or low slots.</summary>
    [Test]
    public void MidSlotToFeat_CoversTenSlotsWithoutOverlappingHighOrLow()
    {
        var mid = Enumerable.Range(1, 10).Select(Space.MidSlotToFeat).ToArray();
        var highAndLow = Enumerable.Range(1, 10).Select(Space.HighSlotToFeat)
            .Concat(Enumerable.Range(1, 10).Select(Space.LowSlotToFeat)).ToArray();

        mid.Should().OnlyHaveUniqueItems().And.HaveCount(10);
        mid.Should().NotIntersectWith(highAndLow);
        mid.Should().OnlyContain(x => Space.ShipModuleFeats.ContainsKey(x));
        Enumerable.Range(1, 10).Select(slot => Space.MidFeatToSlot(Space.MidSlotToFeat(slot))).Should().Equal(Enumerable.Range(1, 10));
    }

    /// <summary>Checks the space-only resistance types stay out of the ground resistance set.</summary>
    [Test]
    public void SpaceResistanceTypes_AreNotGroundResistanceTypes()
    {
        Resistance.GetAllResistanceTypes().Should().NotContain([ResistanceType.Thermal, ResistanceType.Explosive, ResistanceType.EM]);
        Resistance.IsValidResistanceType(ResistanceType.Thermal).Should().BeFalse();
        Resistance.IsValidResistanceType(ResistanceType.Fire).Should().BeTrue();
    }
}

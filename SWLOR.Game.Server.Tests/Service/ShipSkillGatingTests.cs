using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipSkillGatingTests
{
    /// <summary>No stored requirements means nothing is unmet.</summary>
    [Test]
    public void FindUnmetSkillRequirement_NullOrEmpty_ReturnsNull()
    {
        Item.FindUnmetSkillRequirement(null, _ => 0).Should().BeNull();
        Item.FindUnmetSkillRequirement(new Dictionary<SkillType, int>(), _ => 0).Should().BeNull();
    }

    /// <summary>A rank at or above the requirement meets it; one below does not.</summary>
    [Test]
    public void FindUnmetSkillRequirement_ComparesRankToRequirement()
    {
        var requirements = new Dictionary<SkillType, int> { [SkillType.Piloting] = 20 };

        Item.FindUnmetSkillRequirement(requirements, _ => 20).Should().BeNull();
        Item.FindUnmetSkillRequirement(requirements, _ => 19)
            .Should().Be(new KeyValuePair<SkillType, int>(SkillType.Piloting, 20));
    }

    /// <summary>Every required skill is checked, not just the first.</summary>
    [Test]
    public void FindUnmetSkillRequirement_ReportsTheSkillThatFails()
    {
        var requirements = new Dictionary<SkillType, int>
        {
            [SkillType.Piloting] = 10,
            [SkillType.FirstAid] = 30
        };

        var unmet = Item.FindUnmetSkillRequirement(requirements, skill => skill == SkillType.Piloting ? 50 : 5);

        unmet.Should().Be(new KeyValuePair<SkillType, int>(SkillType.FirstAid, 30));
    }

    /// <summary>Saved ships written before skill gating load with no requirements.</summary>
    [Test]
    public void ShipStatus_OldJson_HasNoRequiredSkills()
    {
        var status = JsonConvert.DeserializeObject<ShipStatus>("{\"ItemTag\":\"old_ship\",\"HighPowerModules\":{\"1\":{\"ItemTag\":\"old_module\"}}}");

        status.RequiredSkills.Should().BeNull();
        status.HighPowerModules[1].RequiredSkills.Should().BeNull();
    }

    /// <summary>Stored requirements survive a save and load.</summary>
    [Test]
    public void ShipStatus_RequiredSkills_RoundTrip()
    {
        var status = new ShipStatus { RequiredSkills = new Dictionary<SkillType, int> { [SkillType.Piloting] = 15 } };
        status.LowPowerModules[2] = new ShipStatus.ShipStatusModule
        {
            RequiredSkills = new Dictionary<SkillType, int> { [SkillType.FirstAid] = 25 }
        };

        var loaded = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status));

        loaded.RequiredSkills.Should().Equal(status.RequiredSkills);
        loaded.LowPowerModules[2].RequiredSkills.Should().Equal(status.LowPowerModules[2].RequiredSkills);
    }
}

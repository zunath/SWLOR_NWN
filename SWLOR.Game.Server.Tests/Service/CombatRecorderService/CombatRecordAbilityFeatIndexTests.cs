using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecordAbilityFeatIndexTests
{
    [Test]
    public void EachDefinition_MapsToItsFeat()
    {
        var rankOne = new AbilityDetail { Name = "Creeping Terror I" };
        var rankTwo = new AbilityDetail { Name = "Creeping Terror II" };

        var index = CombatRecordAbilityFeatIndex.Build(new Dictionary<FeatType, AbilityDetail>
        {
            [FeatType.CreepingTerror1] = rankOne,
            [FeatType.CreepingTerror2] = rankTwo
        });

        index[rankOne].Should().Be(FeatType.CreepingTerror1);
        index[rankTwo].Should().Be(FeatType.CreepingTerror2);
    }

    [Test]
    public void DefinitionSharedByTwoFeats_HasNoFeat()
    {
        var shared = new AbilityDetail { Name = "Shared" };

        var index = CombatRecordAbilityFeatIndex.Build(new Dictionary<FeatType, AbilityDetail>
        {
            [FeatType.CreepingTerror1] = shared,
            [FeatType.CreepingTerror2] = shared
        });

        index[shared].Should().Be(FeatType.Invalid);
    }

    [Test]
    public void DefinitionsAreMatchedByReference()
    {
        var registered = new AbilityDetail { Name = "Creeping Terror I" };
        var lookalike = new AbilityDetail { Name = "Creeping Terror I" };

        var index = CombatRecordAbilityFeatIndex.Build(new Dictionary<FeatType, AbilityDetail>
        {
            [FeatType.CreepingTerror1] = registered,
            [FeatType.CreepingTerror2] = null
        });

        index.Should().ContainKey(registered);
        index.Should().NotContainKey(lookalike);
        index.Should().HaveCount(1);
    }
}

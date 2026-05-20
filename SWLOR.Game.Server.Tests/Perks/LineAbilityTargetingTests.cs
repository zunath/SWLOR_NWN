using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade;
using SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber;
using SWLOR.Game.Server.Feature.AbilityDefinition.Rifle;
using SWLOR.Game.Server.Feature.AbilityDefinition.Spear;
using SWLOR.Game.Server.Feature.AbilityDefinition.Staff;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LineAbilityTargetingTests
{
    [TestCase(FeatType.CoveringStrike1, Spell.CoveringStrike1, typeof(CoveringStrikeAbilityDefinition))]
    [TestCase(FeatType.Earthshatter1, Spell.Earthshatter1, typeof(EarthshatterAbilityDefinition))]
    [TestCase(FeatType.ForcePush2, Spell.ForcePush2, typeof(ForcePushAbilityDefinition))]
    [TestCase(FeatType.FractureStrike1, Spell.FractureStrike1, typeof(FractureStrikeAbilityDefinition))]
    [TestCase(FeatType.LineBreaker1, Spell.LineBreaker1, typeof(LineBreakerAbilityDefinition))]
    [TestCase(FeatType.PinningFire2, Spell.PinningFire2, typeof(PinningFireAbilityDefinition))]
    [TestCase(FeatType.SuppressiveLine1, Spell.SuppressiveLine1, typeof(SuppressiveLineAbilityDefinition))]
    [TestCase(FeatType.SweepingAdvance1, Spell.SweepingAdvance1, typeof(SweepingAdvanceAbilityDefinition))]
    [TestCase(FeatType.ThunderousChallenge1, Spell.ThunderousChallenge1, typeof(ThunderousChallengeAbilityDefinition))]
    public void OriginLineAbilities_DeclareForwardLengthAndWidthTargeting(
        FeatType feat,
        Spell spell,
        Type definitionType)
    {
        var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType)!;
        var ability = definition.BuildAbilities()[feat];

        ability.Targeting.Should().NotBeNull();
        ability.Targeting!.Spell.Should().Be(spell);
        ability.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Rect);
        ability.Targeting.SizeX.Should().Be(8f);
        ability.Targeting.SizeY.Should().Be(2.5f);
        ability.Targeting.Flags.Should().Be(
            AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);
    }
}

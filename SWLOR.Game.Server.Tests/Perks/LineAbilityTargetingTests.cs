using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Devices;
using SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade;
using SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber;
using SWLOR.Game.Server.Feature.AbilityDefinition.Rifle;
using SWLOR.Game.Server.Feature.AbilityDefinition.Staff;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LineAbilityTargetingTests
{
    [TestCase(FeatType.CoveringStrike1, Spell.CoveringStrike1, typeof(CoveringStrikeAbilityDefinition), 8f, 2.5f)]
    [TestCase(FeatType.Earthshatter1, Spell.Earthshatter1, typeof(EarthshatterAbilityDefinition), 8f, 2.5f)]
    [TestCase(FeatType.LineBreaker1, Spell.LineBreaker1, typeof(LineBreakerAbilityDefinition), 8f, 2.5f)]
    [TestCase(FeatType.SuppressiveLine1, Spell.SuppressiveLine1, typeof(SuppressiveLineAbilityDefinition), 20f, 3f)]
    [TestCase(FeatType.GuardiansChallenge2, Spell.GuardiansChallenge2, typeof(GuardiansChallengeAbilityDefinition), 8f, 2.5f)]
    [TestCase(FeatType.IonLance1, Spell.IonLance1, typeof(IonLanceAbilityDefinition), 8f, 2.5f)]
    public void OriginLineAbilities_DeclareForwardLengthAndWidthTargeting(
        FeatType feat,
        Spell spell,
        Type definitionType,
        float expectedLength,
        float expectedWidth)
    {
        var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType)!;
        var ability = definition.BuildAbilities()[feat];

        ability.Targeting.Should().NotBeNull();
        ability.Targeting!.Spell.Should().Be(spell);
        ability.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Rect);
        ability.Targeting.SizeX.Should().Be(expectedLength);
        ability.Targeting.SizeY.Should().Be(expectedWidth);
        ability.Targeting.Flags.Should().Be(
            AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);
    }
}

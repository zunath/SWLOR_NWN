using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>A pulse emitter keeps the settings of the ability that placed it and only its remaining time, marker and pending shots change.</summary>
[TestFixture]
public class FieldEngineerPulseEmitterTests
{
    private static FieldEngineerPulseEmitter Create(
        VisualEffect projectile = VisualEffect.None,
        VisualEffect beam = VisualEffect.None) =>
        new(
            activator: 1001,
            location: null!,
            skillType: SkillType.Fabrication,
            baseDamage: 40,
            statusDuration: 6,
            statusEffect: typeof(string),
            radius: 4.5f,
            durationSeconds: 12f,
            damageType: CombatDamageType.Physical,
            targetVisualEffect: VisualEffect.Vfx_Imp_Head_Acid,
            areaVisualEffect: VisualEffect.Vfx_Dur_Iounstone_Yellow,
            markerVisualEffect: VisualEffect.Vfx_Imp_Head_Acid,
            markerVisualEffectScale: 1.5f,
            isAreaPulse: true,
            appliesBeaconPulseBonuses: true,
            showAreaIndicator: true,
            projectileVisualEffect: projectile,
            beamVisualEffect: beam);

    [Test]
    public void TheEmitterKeepsEverySettingItWasGiven()
    {
        var emitter = Create();

        emitter.Activator.Should().Be(1001u);
        emitter.SkillType.Should().Be(SkillType.Fabrication);
        emitter.BaseDamage.Should().Be(40);
        emitter.StatusDuration.Should().Be(6);
        emitter.StatusEffect.Should().Be(typeof(string));
        emitter.Radius.Should().Be(4.5f);
        emitter.RemainingSeconds.Should().Be(12f);
        emitter.DamageType.Should().Be(CombatDamageType.Physical);
        emitter.TargetVisualEffect.Should().Be(VisualEffect.Vfx_Imp_Head_Acid);
        emitter.AreaVisualEffect.Should().Be(VisualEffect.Vfx_Dur_Iounstone_Yellow);
        emitter.MarkerVisualEffect.Should().Be(VisualEffect.Vfx_Imp_Head_Acid);
        emitter.MarkerVisualEffectScale.Should().Be(1.5f);
        emitter.IsAreaPulse.Should().BeTrue();
        emitter.AppliesBeaconPulseBonuses.Should().BeTrue();
        emitter.ShowAreaIndicator.Should().BeTrue();
    }

    [Test]
    public void ANewEmitterHasNoMarkerIndicatorOrPendingWork()
    {
        var emitter = Create();

        emitter.MarkerObject.Should().Be(0x7F000000u, "the marker object is invalid until one is created");
        emitter.AreaIndicatorId.Should().BeEmpty();
        emitter.PendingProjectileImpacts.Should().Be(0);
        emitter.ApplyPulse.Should().BeNull();
        emitter.ApplyTargetImpact.Should().BeNull();
        emitter.CreateAreaImpactBatch.Should().BeNull();
    }

    [Test]
    public void ShotVisualsAreOptionalAndEitherOneMakesTheEmitterShoot()
    {
        Create().HasShotVisual.Should().BeFalse();
        Create(projectile: VisualEffect.Vfx_Imp_Head_Acid).HasShotVisual.Should().BeTrue();
        Create(beam: VisualEffect.Vfx_Imp_Head_Acid).HasShotVisual.Should().BeTrue();
        Create(projectile: VisualEffect.Vfx_Imp_Head_Acid).ProjectileVisualEffect.Should().Be(VisualEffect.Vfx_Imp_Head_Acid);
        Create(beam: VisualEffect.Vfx_Dur_Iounstone_Yellow).BeamVisualEffect.Should().Be(VisualEffect.Vfx_Dur_Iounstone_Yellow);
    }

    [Test]
    public void TheMutableStateCanBeUpdated()
    {
        var emitter = Create();

        emitter.RemainingSeconds -= 2f;
        emitter.MarkerObject = 77;
        emitter.AreaIndicatorId = "indicator";
        emitter.PendingProjectileImpacts = 3;

        emitter.RemainingSeconds.Should().Be(10f);
        emitter.MarkerObject.Should().Be(77u);
        emitter.AreaIndicatorId.Should().Be("indicator");
        emitter.PendingProjectileImpacts.Should().Be(3);
    }
}

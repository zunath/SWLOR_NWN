using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A definition of the tests: it calls <c>ConfigureWeaponAbility</c> with a profile the test made by reflection, in each shape of
/// <see cref="WeaponAbilityProbeShape"/>, so the settings no shipped ability uses still run and are recorded.
/// </summary>
public sealed class WeaponAbilityProbe : WeaponActiveAbilityDefinitionBase
{
    /// <summary>The ability <c>ConfigureWeaponAbility</c> builds for the profile in the shape.</summary>
    public static AbilityDetail Build(WeaponAbilityProbeShape shape, object profile)
    {
        var builder = new AbilityBuilder().Create(FeatType.GuardCounter3, PerkType.GuardCounter).Name("Probe").Level(3);
        if (shape == WeaponAbilityProbeShape.PartyStatusByType)
        {
            ConfigurePartyStatus(builder, typeof(BleedStatusEffect), 10f, 5, true);
            return builder.Build()[FeatType.GuardCounter3];
        }

        if (shape == WeaponAbilityProbeShape.WeaponHelperWithStatus)
        {
            ConfigureWeapon(builder, SkillType.Katar, 28, 15, typeof(BleedStatusEffect), 8, typeof(SunderStatusEffect));
            return builder.Build()[FeatType.GuardCounter3];
        }

        var drawn = DrawnShape(shape);
        var hostile = drawn != AbilityTargetingShapeType.None ||
                      shape is WeaponAbilityProbeShape.HostileSingle or WeaponAbilityProbeShape.HostileSingleWithStatus
                          or WeaponAbilityProbeShape.HostileDirectArea;
        var area = drawn != AbilityTargetingShapeType.None || shape == WeaponAbilityProbeShape.HostileDirectArea;
        var friendly = shape is WeaponAbilityProbeShape.FriendlyTarget or WeaponAbilityProbeShape.FriendlyTargetWithStatus;
        var status = shape is WeaponAbilityProbeShape.HostileSingleWithStatus or WeaponAbilityProbeShape.FriendlyTargetWithStatus
            or WeaponAbilityProbeShape.SelfStatus
            ? typeof(BleedStatusEffect)
            : null;
        ConfigureWeaponAbility(
            builder,
            SkillType.Katar,
            28,
            status == null ? 0 : 15,
            status,
            shape == WeaponAbilityProbeShape.HostileSingleWithStatus ? new[] { typeof(SunderStatusEffect) } : null,
            8,
            4,
            shape == WeaponAbilityProbeShape.HostileTelegraphedAreaWithoutDelay ? 0f : 0.5f,
            area,
            hostile,
            friendly,
            Spell.Invalid,
            drawn,
            shape == WeaponAbilityProbeShape.HostileTelegraphedSphere ? 0f : 5f,
            3f,
            AbilityTargetingFlags.HarmsEnemies,
            Animation.DoubleStrike,
            10f,
            AbilityType.Invalid,
            (WeaponAbilityProfile)profile);
        return builder.Build()[FeatType.GuardCounter3];
    }

    private static AbilityTargetingShapeType DrawnShape(WeaponAbilityProbeShape shape) => shape switch
    {
        WeaponAbilityProbeShape.HostileTelegraphedArea or WeaponAbilityProbeShape.HostileTelegraphedAreaWithoutDelay => AbilityTargetingShapeType.Cone,
        WeaponAbilityProbeShape.HostileTelegraphedSphere => AbilityTargetingShapeType.Sphere,
        WeaponAbilityProbeShape.HostileTelegraphedRectangle => AbilityTargetingShapeType.Rect,
        WeaponAbilityProbeShape.HostileTelegraphedHemisphere => AbilityTargetingShapeType.HSphere,
        _ => AbilityTargetingShapeType.None
    };
}

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The ways <c>ConfigureWeaponAbility</c> is called for a profile: which kind of ability its arguments describe.</summary>
public enum WeaponAbilityProbeShape
{
    /// <summary>Hostile, one target, no impact status of its own.</summary>
    HostileSingle,

    /// <summary>Hostile, one target, with an impact status and an additional one.</summary>
    HostileSingleWithStatus,

    /// <summary>Hostile area with a drawn shape, so the impact is telegraphed.</summary>
    HostileTelegraphedArea,

    /// <summary>Hostile area with a drawn shape and no activation delay.</summary>
    HostileTelegraphedAreaWithoutDelay,

    /// <summary>Hostile area with a drawn sphere that has no size of its own.</summary>
    HostileTelegraphedSphere,

    /// <summary>Hostile area with a drawn rectangle.</summary>
    HostileTelegraphedRectangle,

    /// <summary>Hostile area with a drawn hemisphere.</summary>
    HostileTelegraphedHemisphere,

    /// <summary>Hostile area with no shape and no target limit, so each hit is a direct area impact.</summary>
    HostileDirectArea,

    /// <summary>Friendly target, no status of its own.</summary>
    FriendlyTarget,

    /// <summary>Friendly target with a status of its own.</summary>
    FriendlyTargetWithStatus,

    /// <summary>Self ability with a status of its own.</summary>
    SelfStatus,

    /// <summary>Self ability with no status of its own.</summary>
    SelfOnly,

    /// <summary>A hostile weapon ability made with <c>ConfigureWeapon</c> and an additional status.</summary>
    WeaponHelperWithStatus,

    /// <summary>A party status ability, made with the overload that takes the status effect type.</summary>
    PartyStatusByType
}

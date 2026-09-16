namespace SWLOR.Game.Server.Service.AnimationService;

/// <summary>The equipment arrangement for which a motion was authored, independent of its skill.</summary>
public enum AnimationEquipmentRequirement
{
    Unrestricted,
    OneHanded,
    WeaponAndShield,
    TwoHanded,
    Polearm,
    DualWield,
    Unarmed,
    Pistol,
    Rifle,
    Throwing
}

/// <summary>Native grip of an equipped item; Unknown deliberately cannot match an authored weapon motion.</summary>
public enum AnimationWeaponGrip
{
    Unknown,
    Empty,
    OneHanded,
    TwoHanded,
    Polearm,
    Shield,
    Unarmed,
    Pistol,
    Rifle,
    Throwing
}

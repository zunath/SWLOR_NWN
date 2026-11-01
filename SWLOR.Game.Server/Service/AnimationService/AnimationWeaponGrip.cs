namespace SWLOR.Game.Server.Service.AnimationService;

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
    Throwing,
    CompanionPistol
}

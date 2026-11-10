namespace SWLOR.Game.Server.Domain.Foundation;

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

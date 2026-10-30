namespace SWLOR.Game.Server.Service.AnimationService;

/// <summary>Persistent native holding layer of the main-hand weapon, which a custom-emote carrier leaves active.</summary>
public enum AnimationWeaponHold
{
    None,
    /// <summary>Rifles and cannons hold through <c>xbowr</c>.</summary>
    Rifle,
    /// <summary>Two-handed, polearm and double weapons hold through <c>plpause1</c>.</summary>
    Shoulder
}

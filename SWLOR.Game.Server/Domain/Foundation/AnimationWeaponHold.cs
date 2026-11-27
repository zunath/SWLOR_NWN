namespace SWLOR.Game.Server.Domain.Foundation;

/// <summary>Persistent native item holding layer, which a custom-emote carrier leaves active.</summary>
public enum AnimationWeaponHold
{
    None,
    /// <summary>Rifles and cannons hold through <c>xbowr</c>.</summary>
    Rifle,
    /// <summary>Two-handed, polearm and double weapons hold through <c>plpause1</c>.</summary>
    Shoulder,
    /// <summary>The left-hand torch layer, explicitly selected by microphone performances.</summary>
    Torch
}

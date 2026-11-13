namespace SWLOR.Game.Server.Service;

/// <summary>The immutable mesh selection stored under one client item identity.</summary>
public readonly record struct WeaponAppearanceKey(int BaseItem, int Top, int Middle, int Bottom);

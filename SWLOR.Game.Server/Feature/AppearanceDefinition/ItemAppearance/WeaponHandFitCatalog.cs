using System;
using System.Collections.Generic;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Weapon-space moves, in metres, that seat a held pistol in each playable hand. Pistols are fitted to the
/// male human default hand, so that body has no entry. Review every change with tools/WeaponGripFit.py
/// (see Readmes/WeaponHandFit.md).
/// </summary>
public static class WeaponHandFitCatalog
{
    /// <summary>One move per body and side. Every hand style of the body shares its wrist, so it inherits this.</summary>
    public static readonly Dictionary<string, FitVector> Bodies = new(StringComparer.OrdinalIgnoreCase)
    {
        // Dwarf frame (Ugnaught, Ewok).
        ["pmd0_handr"] = new(0.009f, 0f, 0f),
        ["pmd0_handl"] = new(-0.009f, 0f, 0f),
        ["pfd0_handr"] = new(0f, 0f, 0.014f),
        ["pfd0_handl"] = new(0f, 0f, 0.014f),
        // Half-orc frame (Cathar), measured on the Cathar hand 201.
        ["pmo0_handr"] = new(0.022f, -0.005f, -0.015f),
        ["pmo0_handl"] = new(-0.022f, -0.005f, -0.015f),
        ["pfo0_handr"] = new(0.032f, 0f, 0f),
        ["pfo0_handl"] = new(-0.032f, 0f, 0f),
        // Droid frame.
        ["pmz0_handr"] = new(0.025f, 0f, -0.01f),
        ["pmz0_handl"] = new(-0.025f, 0f, -0.01f),
        ["pfz0_handr"] = new(0.025f, 0f, -0.01f),
        ["pfz0_handl"] = new(-0.025f, 0f, -0.01f),
    };

    /// <summary>Hand models whose grip sits somewhere else than their body's default hand.</summary>
    public static readonly Dictionary<string, FitVector> Hands = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mon Calamari female: the fist sits at the bottom of the handle.
        ["pfh0_handr204"] = new(0.004f, -0.04f, 0.018f),
        ["pfh0_handl204"] = new(-0.004f, -0.04f, 0.018f),
        // Ewok female.
        ["pfd0_handr209"] = new(0.015f, 0f, 0f),
        ["pfd0_handl209"] = new(-0.015f, 0f, 0f),
        // Wookiee male.
        ["pme0_handr208"] = new(0.008f, 0f, 0f),
        ["pme0_handl208"] = new(-0.008f, 0f, 0f),
    };

    /// <summary>The move for a hand model such as <c>pfh0_handr204</c>, or zero when nothing is recorded.</summary>
    public static FitVector Get(string handModel)
    {
        if (string.IsNullOrEmpty(handModel))
            return FitVector.Zero;
        if (Hands.TryGetValue(handModel, out var move))
            return move;
        return handModel.Length >= 10 && Bodies.TryGetValue(handModel[..10], out move) ? move : FitVector.Zero;
    }
}

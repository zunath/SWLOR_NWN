using System;
using System.Collections.Generic;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Weapon-space moves, in metres, that seat a held pistol in each playable hand. Weapons are fitted to the
/// male human body, so that body has no entry. Nearly every value is derived from the hand models by
/// tools/WeaponGripFit.py, which also checks them (see Readmes/WeaponHandFit.md); the commented entries are
/// hands it cannot derive, judged on review sheets and in the client. A rifle takes
/// <see cref="WeaponHandFit.RifleMove"/> of the same move.
/// </summary>
public static class WeaponHandFitCatalog
{
    /// <summary>One move per body and side: the median of its derived hands. A hand style without its own entry takes it.</summary>
    public static readonly Dictionary<string, FitVector> Bodies = new(StringComparer.OrdinalIgnoreCase)
    {
        // Human frame, female.
        ["pfh0_handr"] = new(0.005f, -0.007f, 0.006f),
        ["pfh0_handl"] = new(-0.008f, -0.007f, 0.007f),
        // Elf frame (Bothan, Rodian, Wookiee).
        ["pme0_handr"] = new(-0.005f, 0.002f, -0.002f),
        ["pme0_handl"] = new(0.006f, 0.002f, -0.002f),
        ["pfe0_handr"] = new(-0.001f, 0.009f, -0.007f),
        ["pfe0_handl"] = new(-0.002f, 0.002f, -0.007f),
        // Dwarf frame (Ugnaught, Ewok).
        ["pmd0_handr"] = new(0.005f, -0.003f, -0.008f),
        ["pmd0_handl"] = new(-0.006f, -0.003f, -0.008f),
        ["pfd0_handr"] = new(0.014f, -0.01f, 0.008f),
        ["pfd0_handl"] = new(-0.018f, -0.01f, 0.009f),
        // Half-orc frame (Cathar).
        ["pmo0_handr"] = new(0.008f, -0.004f, -0.015f),
        ["pmo0_handl"] = new(-0.009f, -0.004f, -0.015f),
        ["pfo0_handr"] = new(0.016f, -0.012f, 0.013f),
        ["pfo0_handl"] = new(-0.022f, -0.012f, 0.015f),
        // Droid frame. No droid hand has a model on the reference body; judged by eye.
        ["pmz0_handr"] = new(0.025f, 0f, -0.01f),
        ["pmz0_handl"] = new(-0.025f, 0f, -0.01f),
        ["pfz0_handr"] = new(0.025f, 0f, -0.01f),
        ["pfz0_handl"] = new(-0.025f, 0f, -0.01f),
    };

    /// <summary>Hand models whose move is more than 3 mm from their body's.</summary>
    public static readonly Dictionary<string, FitVector> Hands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pfd0_handr009"] = new(0.016f, -0.01f, 0.011f),
        ["pfd0_handl009"] = new(-0.018f, -0.004f, 0.013f),
        ["pfd0_handr075"] = new(0.014f, -0.011f, 0.005f),
        ["pfd0_handl075"] = new(-0.018f, -0.011f, 0.006f),
        ["pfd0_handr113"] = new(0.01f, -0.01f, 0.008f),
        ["pfd0_handr154"] = new(0.017f, 0.005f, 0.008f),
        ["pfd0_handl154"] = new(-0.023f, 0.004f, 0.01f),
        ["pfd0_handr155"] = new(0.021f, -0.013f, 0.004f),
        ["pfd0_handl155"] = new(-0.026f, -0.013f, 0.007f),
        ["pfd0_handr196"] = new(0.019f, 0.001f, 0.008f),
        ["pfd0_handl196"] = new(-0.024f, 0.001f, 0.009f),
        ["pfd0_handr198"] = new(0.011f, 0.003f, 0.014f),
        ["pfd0_handl198"] = new(-0.014f, 0.003f, 0.016f),
        ["pfd0_handr203"] = new(0.014f, -0.012f, -0.001f),
        ["pfd0_handl203"] = new(-0.01f, -0.036f, -0.001f),
        ["pfd0_handr204"] = new(0.013f, -0.057f, 0.021f),
        ["pfd0_handl204"] = new(-0.013f, -0.057f, 0.021f),
        ["pfd0_handr245"] = new(0.008f, -0.005f, 0.011f),
        ["pfd0_handl245"] = new(-0.012f, -0.004f, 0.013f),
        ["pfd0_handr257"] = new(0.009f, -0.01f, 0.008f),
        ["pfd0_handr258"] = new(0.013f, -0.008f, 0.02f),
        ["pfd0_handl258"] = new(-0.018f, -0.009f, 0.019f),
        ["pfd0_handr262"] = new(0.015f, -0.011f, 0.005f),
        ["pfd0_handl262"] = new(-0.02f, -0.011f, 0.006f),
        ["pfd0_handr267"] = new(0.02f, 0.013f, -0.005f),
        ["pfd0_handl267"] = new(-0.024f, 0.013f, -0.003f),
        ["pfe0_handr004"] = new(-0.005f, 0.009f, -0.011f),
        ["pfe0_handl004"] = new(0.003f, 0.001f, -0.01f),
        ["pfe0_handl009"] = new(-0.002f, 0.005f, -0.003f),
        ["pfe0_handr154"] = new(0.001f, 0.017f, -0.007f),
        ["pfe0_handl154"] = new(-0.004f, 0.01f, -0.006f),
        ["pfe0_handr155"] = new(0.003f, 0.007f, -0.01f),
        ["pfe0_handl155"] = new(-0.006f, -0.001f, -0.009f),
        ["pfe0_handr186"] = new(-0.006f, 0.017f, -0.013f),
        ["pfe0_handl186"] = new(0f, 0.009f, -0.013f),
        ["pfe0_handr196"] = new(0.002f, 0.011f, -0.008f),
        ["pfe0_handl196"] = new(-0.005f, 0.003f, -0.007f),
        ["pfe0_handr198"] = new(-0.002f, 0.017f, -0.002f),
        ["pfe0_handl198"] = new(0.001f, 0.009f, -0.001f),
        ["pfe0_handr203"] = new(-0.004f, 0.026f, -0.016f),
        ["pfe0_handl203"] = new(0.009f, -0.001f, -0.025f),
        ["pfe0_handr245"] = new(-0.004f, 0.012f, -0.005f),
        ["pfe0_handl245"] = new(0.002f, 0.005f, -0.004f),
        ["pfe0_handr250"] = new(0.008f, 0.005f, -0.031f),
        ["pfe0_handl250"] = new(-0.012f, -0.003f, -0.03f),
        ["pfe0_handl257"] = new(-0.007f, -0.001f, -0.024f),
        ["pfe0_handr258"] = new(-0.002f, 0.01f, 0.002f),
        ["pfe0_handl258"] = new(-0.002f, 0.003f, 0.001f),
        ["pfe0_handl261"] = new(-0.001f, 0.001f, -0.004f),
        ["pfe0_handr267"] = new(0.002f, 0.022f, -0.017f),
        ["pfe0_handl267"] = new(-0.005f, 0.015f, -0.016f),
        ["pfh0_handr004"] = new(-0.001f, -0.007f, 0.004f),
        ["pfh0_handl004"] = new(-0.002f, -0.007f, 0.006f),
        ["pfh0_handr009"] = new(0.006f, -0.007f, 0.009f),
        ["pfh0_handl009"] = new(-0.008f, -0.003f, 0.011f),
        ["pfh0_handl045"] = new(-0.008f, -0.011f, 0.006f),
        ["pfh0_handr154"] = new(0.007f, 0.003f, 0.006f),
        ["pfh0_handl154"] = new(-0.011f, 0.003f, 0.008f),
        ["pfh0_handr155"] = new(0.01f, -0.01f, 0.003f),
        ["pfh0_handl155"] = new(-0.013f, -0.01f, 0.005f),
        ["pfh0_handr186"] = new(-0.001f, 0.002f, -0.001f),
        ["pfh0_handl186"] = new(-0.006f, 0.002f, 0f),
        ["pfh0_handr196"] = new(0.008f, -0.004f, 0.005f),
        ["pfh0_handl196"] = new(-0.012f, -0.004f, 0.007f),
        ["pfh0_handr198"] = new(0.003f, 0.002f, 0.012f),
        ["pfh0_handl198"] = new(-0.005f, 0.002f, 0.014f),
        ["pfh0_handr203"] = new(0.001f, 0.013f, -0.004f),
        ["pfh0_handl203"] = new(0.006f, -0.011f, -0.014f),
        // Mon Calamari female: the fist sits low on the handle.
        ["pfh0_handr204"] = new(0.004f, -0.04f, 0.018f),
        ["pfh0_handl204"] = new(-0.004f, -0.04f, 0.018f),
        ["pfh0_handr245"] = new(0.001f, -0.004f, 0.009f),
        ["pfh0_handl245"] = new(-0.004f, -0.003f, 0.011f),
        ["pfh0_handr246"] = new(0.004f, -0.007f, 0.011f),
        ["pfh0_handl246"] = new(-0.007f, -0.007f, 0.012f),
        ["pfh0_handr257"] = new(0.002f, -0.007f, 0.007f),
        ["pfh0_handl257"] = new(-0.014f, -0.01f, -0.012f),
        ["pfh0_handr258"] = new(0.004f, -0.006f, 0.017f),
        ["pfh0_handl258"] = new(-0.008f, -0.006f, 0.016f),
        ["pfh0_handr261"] = new(0.004f, -0.007f, 0.01f),
        ["pfh0_handl261"] = new(-0.007f, -0.007f, 0.011f),
        ["pfh0_handr267"] = new(0.009f, 0.009f, -0.005f),
        ["pfh0_handl267"] = new(-0.012f, 0.009f, -0.004f),
        ["pfo0_handr001"] = new(-0.024f, -0.012f, 0.013f),
        ["pfo0_handl001"] = new(0.018f, -0.012f, 0.015f),
        ["pfo0_handr004"] = new(0.005f, -0.012f, 0.01f),
        ["pfo0_handl004"] = new(-0.011f, -0.012f, 0.013f),
        ["pfo0_handr005"] = new(0.015f, -0.013f, 0.017f),
        ["pfo0_handl005"] = new(-0.02f, -0.013f, 0.019f),
        ["pfo0_handr009"] = new(0.018f, -0.012f, 0.018f),
        ["pfo0_handl009"] = new(-0.022f, -0.005f, 0.021f),
        ["pfo0_handr075"] = new(0.016f, -0.014f, 0.009f),
        ["pfo0_handl075"] = new(-0.021f, -0.014f, 0.011f),
        ["pfo0_handr113"] = new(0.013f, -0.012f, 0.014f),
        ["pfo0_handr154"] = new(0.02f, 0.006f, 0.013f),
        ["pfo0_handl154"] = new(-0.027f, 0.006f, 0.016f),
        ["pfo0_handr155"] = new(0.025f, -0.017f, 0.009f),
        ["pfo0_handl155"] = new(-0.031f, -0.017f, 0.012f),
        ["pfo0_handr186"] = new(0.005f, 0.004f, 0.003f),
        ["pfo0_handl186"] = new(-0.018f, 0.004f, 0.004f),
        ["pfo0_handr196"] = new(0.022f, -0.001f, 0.013f),
        ["pfo0_handl196"] = new(-0.029f, -0.001f, 0.015f),
        ["pfo0_handr198"] = new(0.013f, 0.004f, 0.022f),
        ["pfo0_handl198"] = new(-0.016f, 0.004f, 0.025f),
        // Cathar female.
        ["pfo0_handr201"] = new(0.032f, 0f, 0f),
        ["pfo0_handl201"] = new(-0.032f, 0f, 0f),
        ["pfo0_handr203"] = new(-0.031f, 0.024f, -0.001f),
        ["pfo0_handl203"] = new(0.044f, -0.02f, -0.017f),
        ["pfo0_handr245"] = new(0.009f, -0.006f, 0.018f),
        ["pfo0_handl245"] = new(-0.014f, -0.005f, 0.021f),
        ["pfo0_handr257"] = new(0.01f, -0.012f, 0.015f),
        ["pfo0_handr258"] = new(0.014f, -0.01f, 0.03f),
        ["pfo0_handl258"] = new(-0.022f, -0.011f, 0.028f),
        ["pfo0_handr262"] = new(0.017f, -0.014f, 0.01f),
        ["pfo0_handl262"] = new(-0.023f, -0.014f, 0.012f),
        ["pfo0_handr267"] = new(0.023f, 0.017f, -0.003f),
        ["pfo0_handl267"] = new(-0.029f, 0.016f, -0.002f),
        ["pmd0_handr196"] = new(0.005f, 0.004f, -0.007f),
        ["pmd0_handl196"] = new(-0.006f, 0.004f, -0.007f),
        ["pme0_handr196"] = new(-0.002f, 0.005f, -0.001f),
        ["pme0_handl196"] = new(0.002f, 0.005f, -0.001f),
        // Wookiee male; 209 is the same shape.
        ["pme0_handr208"] = new(0.008f, 0f, 0f),
        ["pme0_handl208"] = new(-0.004f, 0f, -0.002f),
        ["pme0_handr209"] = new(0.008f, 0f, 0f),
        ["pme0_handl209"] = new(-0.004f, 0f, -0.002f),
        ["pme0_handr250"] = new(0.001f, -0.001f, -0.014f),
        ["pme0_handl250"] = new(-0.001f, -0.001f, -0.014f),
        ["pmo0_handr001"] = new(0.015f, -0.005f, -0.015f),
        ["pmo0_handl001"] = new(-0.019f, -0.005f, -0.015f),
        ["pmo0_handr075"] = new(0.015f, -0.005f, -0.015f),
        ["pmo0_handl075"] = new(-0.019f, -0.005f, -0.015f),
        ["pmo0_handr186"] = new(0.015f, -0.005f, -0.015f),
        ["pmo0_handl186"] = new(-0.019f, -0.005f, -0.015f),
        ["pmo0_handr196"] = new(0.013f, 0.009f, -0.014f),
        ["pmo0_handl196"] = new(-0.016f, 0.008f, -0.013f),
        // Cathar male.
        ["pmo0_handr201"] = new(0.022f, -0.005f, -0.015f),
        ["pmo0_handl201"] = new(-0.018f, -0.005f, -0.017f),
        ["pmo0_handr203"] = new(0.015f, -0.005f, -0.015f),
        ["pmo0_handl203"] = new(-0.019f, -0.005f, -0.015f),
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

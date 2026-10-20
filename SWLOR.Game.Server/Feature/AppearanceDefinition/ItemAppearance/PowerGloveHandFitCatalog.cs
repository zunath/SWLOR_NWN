using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>Measured native hand proportions for the compact Power Glove appearances.</summary>
public static class PowerGloveHandFitCatalog
{
    private static readonly Dictionary<string, float> HandSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pfd0_handl001"] = 1.14f,
        ["pfd0_handl002"] = 1.14f,
        ["pfd0_handl201"] = 1.37f,
        ["pfd0_handl209"] = 1.25f,
        ["pfd0_handr001"] = 1.13f,
        ["pfd0_handr002"] = 1.13f,
        ["pfd0_handr201"] = 1.36f,
        ["pfd0_handr209"] = 1.26f,
        ["pfe0_handl001"] = 1.00f,
        ["pfe0_handl002"] = 1.00f,
        ["pfe0_handl044"] = 1.00f,
        ["pfe0_handl208"] = 1.00f,
        ["pfe0_handr001"] = 1.00f,
        ["pfe0_handr002"] = 1.00f,
        ["pfe0_handr044"] = 1.00f,
        ["pfe0_handr208"] = 1.00f,
        ["pfh0_handl001"] = 1.00f,
        ["pfh0_handl002"] = 1.00f,
        ["pfh0_handl045"] = 1.00f,
        ["pfh0_handl109"] = 1.00f,
        ["pfh0_handl201"] = 1.00f,
        ["pfh0_handl204"] = 1.00f,
        ["pfh0_handr001"] = 1.00f,
        ["pfh0_handr002"] = 1.00f,
        ["pfh0_handr045"] = 1.00f,
        ["pfh0_handr109"] = 1.00f,
        ["pfh0_handr201"] = 1.05f,
        ["pfh0_handr204"] = 1.00f,
        ["pfo0_handl001"] = 1.36f,
        ["pfo0_handl002"] = 1.66f,
        ["pfo0_handl201"] = 1.82f,
        ["pfo0_handr001"] = 1.41f,
        ["pfo0_handr002"] = 1.67f,
        ["pfo0_handr201"] = 1.80f,
        ["pfz0_handl001"] = 1.33f,
        ["pfz0_handl002"] = 1.33f,
        ["pfz0_handl201"] = 1.41f,
        ["pfz0_handr001"] = 1.29f,
        ["pfz0_handr002"] = 1.29f,
        ["pfz0_handr201"] = 1.36f,
        ["pmd0_handl001"] = 1.25f,
        ["pmd0_handl002"] = 1.56f,
        ["pmd0_handl201"] = 1.39f,
        ["pmd0_handl209"] = 1.25f,
        ["pmd0_handr001"] = 1.25f,
        ["pmd0_handr002"] = 1.55f,
        ["pmd0_handr201"] = 1.38f,
        ["pmd0_handr209"] = 1.26f,
        ["pme0_handl001"] = 1.00f,
        ["pme0_handl002"] = 1.00f,
        ["pme0_handl044"] = 1.08f,
        ["pme0_handl208"] = 1.21f,
        ["pme0_handr001"] = 1.00f,
        ["pme0_handr002"] = 1.00f,
        ["pme0_handr044"] = 1.06f,
        ["pme0_handr208"] = 1.20f,
        ["pmh0_handl001"] = 1.00f,
        ["pmh0_handl002"] = 1.25f,
        ["pmh0_handl045"] = 1.29f,
        ["pmh0_handl201"] = 1.31f,
        ["pmh0_handl204"] = 1.05f,
        ["pmh0_handr001"] = 1.00f,
        ["pmh0_handr002"] = 1.24f,
        ["pmh0_handr045"] = 1.25f,
        ["pmh0_handr201"] = 1.30f,
        ["pmh0_handr204"] = 1.08f,
        ["pmo0_handl001"] = 1.80f,
        ["pmo0_handl002"] = 1.75f,
        ["pmo0_handl201"] = 1.82f,
        ["pmo0_handr001"] = 1.80f,
        ["pmo0_handr002"] = 1.74f,
        ["pmo0_handr201"] = 1.80f,
        ["pmz0_handl001"] = 1.33f,
        ["pmz0_handl002"] = 1.33f,
        ["pmz0_handl201"] = 1.41f,
        ["pmz0_handr001"] = 1.29f,
        ["pmz0_handr002"] = 1.29f,
        ["pmz0_handr201"] = 1.36f,
    };

    public static float GetScale(string handModel, int appearanceId)
    {
        if (PowerGloveHandFitOverrides.TryGetScale(handModel, appearanceId, out var measuredScale))
            return measuredScale;
        if (PowerGloveOpenFrameFits.TryGetScale(handModel, appearanceId, out var frameScale))
            return frameScale;

        var compactScale = appearanceId switch
        {
            101 => .65f,
            102 => .60f,
            104 => .62f,
            105 => .58f,
            _ => 1f
        };
        if (compactScale == 1f) return 1f;
        var originalSize = 1f / compactScale;
        // Unmeasured armor hands retain the earlier full-sized fit.
        return !string.IsNullOrWhiteSpace(handModel) && HandSizes.TryGetValue(handModel, out var handSize)
            ? Math.Clamp(handSize, 1f, originalSize)
            : originalSize;
    }

    internal static bool HasMeasuredFit(string handModel, int appearanceId)
    {
        if (PowerGloveHandFitOverrides.HasFit(handModel, appearanceId))
            return true;
        if (PowerGloveGripFits.IsCorrected(appearanceId))
            return PowerGloveGripFits.HasTarget(handModel, appearanceId);
        if (appearanceId is >= 107 and <= 121)
            return PowerGloveOpenFrameFits.HasMeasuredScale(handModel);
        return (appearanceId is 101 or 102 or 103 or 104 or 105) &&
               !string.IsNullOrWhiteSpace(handModel) && HandSizes.ContainsKey(handModel);
    }
}

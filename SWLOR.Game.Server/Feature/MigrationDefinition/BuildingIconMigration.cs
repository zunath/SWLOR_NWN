using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Replaces the shared building inventory appearance while preserving custom models.</summary>
public static class BuildingIconMigration
{
    public static IReadOnlyDictionary<string, int> Models { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["structure_5000"] = 151,
        ["structure_5001"] = 152,
        ["structure_5002"] = 153,
        ["structure_5003"] = 154,
        ["structure_5004"] = 155,
        ["structure_5005"] = 156,
        ["structure_5006"] = 156,
        ["structure_5007"] = 156,
        ["structure_5008"] = 156,
        ["structure_5009"] = 157,
        ["structure_5010"] = 157,
        ["structure_5011"] = 157,
        ["structure_5012"] = 157,
        ["structure_5013"] = 158,
        ["structure_5014"] = 158,
        ["structure_5015"] = 158,
        ["structure_5016"] = 158,
        ["structure_5017"] = 159,
    };

    public static int GetUpdatedModel(string resref, int baseItem, int currentModel) =>
        baseItem == 29 && currentModel == 202 && resref != null && Models.TryGetValue(resref, out var model)
            ? model : currentModel;

    public static string GetUpdatedIcon(string resref, string currentIcon) =>
        string.Equals(currentIcon, "iit_midmisc_202", StringComparison.OrdinalIgnoreCase) &&
        resref != null && Models.TryGetValue(resref, out var model)
            ? $"iit_midmisc_{model:000}" : currentIcon;
}

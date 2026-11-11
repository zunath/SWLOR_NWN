using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Renames each furniture set's former Display Plinth after the placeable it now shows. Character migrations rename
/// carried copies in saved character files; the stored-item sweep renames saved item data, listings and placed structures.
/// </summary>
public static class FurnitureRenameMigration
{
    private sealed record Rename(StructureType Structure, string StructureResref, string BlueprintResref, string OldName, string NewName);

    private static readonly Rename[] Renames =
    {
        new(StructureType.AshmarkAltar, "structure_0339", "bpstructure0339", "Ashmark Display Plinth", "Ashmark Altar"),
        new(StructureType.GuttermarkEasel, "structure_0349", "bpstructure0349", "Guttermark Display Plinth", "Guttermark Easel"),
        new(StructureType.TrailmarkWaystone, "structure_0359", "bpstructure0359", "Trailmark Display Plinth", "Trailmark Waystone"),
        new(StructureType.StonewakeStoneAltar, "structure_0369", "bpstructure0369", "Stonewake Display Plinth", "Stonewake Stone Altar"),
        new(StructureType.AegislineSwordPedestal, "structure_0379", "bpstructure0379", "Aegisline Display Plinth", "Aegisline Sword Pedestal"),
        new(StructureType.VeilcarvedRunePillar, "structure_0389", "bpstructure0389", "Veilcarved Display Plinth", "Veilcarved Rune Pillar"),
        new(StructureType.SurgewakeHoloProjector, "structure_0399", "bpstructure0399", "Surgewake Display Plinth", "Surgewake Holo Projector"),
        new(StructureType.ResonantCrystal, "structure_0409", "bpstructure0409", "Resonant Display Plinth", "Resonant Crystal"),
        new(StructureType.FieldlineDisplayCase, "structure_0419", "bpstructure0419", "Fieldline Display Plinth", "Fieldline Display Case"),
        new(StructureType.CommandlineBust, "structure_0429", "bpstructure0429", "Commandline Display Plinth", "Commandline Bust"),
    };

    private static readonly IReadOnlyDictionary<StructureType, Rename> ByStructure =
        Renames.ToDictionary(x => x.Structure);

    private static readonly IReadOnlyDictionary<string, Rename> ByItemResref =
        Renames.SelectMany(x => new[] { (x.StructureResref, x), (x.BlueprintResref, x) })
            .ToDictionary(x => x.Item1, x => x.x);

    /// <summary>The default texts the former blueprints gave a structure or blueprint item, and their renamed texts.</summary>
    private static IEnumerable<(string Old, string New)> DefaultTexts(Rename rename)
    {
        yield return (rename.OldName, rename.NewName);
        yield return ($"Blueprint: {rename.OldName}", $"Blueprint: {rename.NewName}");
        yield return ($"A rare schematic for crafting {rename.OldName}.", $"A rare schematic for crafting {rename.NewName}.");
    }

    /// <summary>
    /// The text a renamed structure or blueprint item now shows for <paramref name="text"/>, its saved name or
    /// description. Other items, and any text other than the former defaults (such as a custom name), keep their text.
    /// </summary>
    public static string GetRenamedItemText(string resref, string text)
    {
        if (string.IsNullOrEmpty(text) || resref == null || !ByItemResref.TryGetValue(resref, out var rename))
            return text;
        foreach (var (oldText, newText) in DefaultTexts(rename))
            if (text == oldText)
                return newText;
        return text;
    }

    /// <summary>Renames a placed structure that still shows its former default name.</summary>
    public static bool RenameStructure(WorldProperty property)
    {
        if (property == null ||
            property.PropertyType != PropertyType.Structure ||
            !ByStructure.TryGetValue(property.StructureType, out var rename) ||
            property.CustomName != rename.OldName)
            return false;
        property.CustomName = rename.NewName;
        return true;
    }

    /// <summary>
    /// Live counterpart of the saved-data item rename: the item's name, both descriptions, and the original name a
    /// DM rename keeps for its Reset action.
    /// </summary>
    public static bool RenameItem(uint item)
    {
        var resref = GetResRef(item);
        if (!ByItemResref.ContainsKey(resref))
            return false;
        var changed = false;
        // A DM's custom name is kept; only the default name changes.
        var name = GetName(item);
        var renamed = GetRenamedItemText(resref, name);
        if (renamed != name && name == GetName(item, true))
        {
            SetName(item, renamed);
            changed = true;
        }
        var originalName = GetLocalString(item, RenameTargetViewModel.RenamedItemOriginalName);
        var renamedOriginal = GetRenamedItemText(resref, originalName);
        if (renamedOriginal != originalName)
        {
            SetLocalString(item, RenameTargetViewModel.RenamedItemOriginalName, renamedOriginal);
            changed = true;
        }
        foreach (var identified in new[] { true, false })
        {
            var description = GetDescription(item, false, identified);
            var updated = GetRenamedItemText(resref, description);
            if (updated == description) continue;
            SetDescription(item, updated, identified);
            changed = true;
        }
        return changed;
    }
}

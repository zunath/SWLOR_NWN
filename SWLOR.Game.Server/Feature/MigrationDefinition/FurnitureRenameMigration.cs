using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Renames each furniture set's former Display Plinth after the placeable it now shows. Players store recipe
/// knowledge in dictionaries keyed by enum name, so stale recipe names are mapped when a Player deserializes
/// (<see cref="RenamedRecipesJsonConverter"/>); server migration 29 rewrote the saved records.
/// Crafted structures and their blueprints keep the old name in saved item data, listings and placed structures.
/// </summary>
public static class FurnitureRenameMigration
{
    private sealed record Rename(string OldRecipe, RecipeType Recipe, StructureType Structure,
        string StructureResref, string BlueprintResref, string OldName, string NewName);

    private static readonly Rename[] Renames =
    {
        new("AshmarkDisplayPlinth", RecipeType.AshmarkAltar, StructureType.AshmarkAltar,
            "structure_0339", "bpstructure0339", "Ashmark Display Plinth", "Ashmark Altar"),
        new("GuttermarkDisplayPlinth", RecipeType.GuttermarkEasel, StructureType.GuttermarkEasel,
            "structure_0349", "bpstructure0349", "Guttermark Display Plinth", "Guttermark Easel"),
        new("TrailmarkDisplayPlinth", RecipeType.TrailmarkWaystone, StructureType.TrailmarkWaystone,
            "structure_0359", "bpstructure0359", "Trailmark Display Plinth", "Trailmark Waystone"),
        new("StonewakeDisplayPlinth", RecipeType.StonewakeStoneAltar, StructureType.StonewakeStoneAltar,
            "structure_0369", "bpstructure0369", "Stonewake Display Plinth", "Stonewake Stone Altar"),
        new("AegislineDisplayPlinth", RecipeType.AegislineSwordPedestal, StructureType.AegislineSwordPedestal,
            "structure_0379", "bpstructure0379", "Aegisline Display Plinth", "Aegisline Sword Pedestal"),
        new("VeilcarvedDisplayPlinth", RecipeType.VeilcarvedRunePillar, StructureType.VeilcarvedRunePillar,
            "structure_0389", "bpstructure0389", "Veilcarved Display Plinth", "Veilcarved Rune Pillar"),
        new("SurgewakeDisplayPlinth", RecipeType.SurgewakeHoloProjector, StructureType.SurgewakeHoloProjector,
            "structure_0399", "bpstructure0399", "Surgewake Display Plinth", "Surgewake Holo Projector"),
        new("ResonantDisplayPlinth", RecipeType.ResonantCrystal, StructureType.ResonantCrystal,
            "structure_0409", "bpstructure0409", "Resonant Display Plinth", "Resonant Crystal"),
        new("FieldlineDisplayPlinth", RecipeType.FieldlineDisplayCase, StructureType.FieldlineDisplayCase,
            "structure_0419", "bpstructure0419", "Fieldline Display Plinth", "Fieldline Display Case"),
        new("CommandlineDisplayPlinth", RecipeType.CommandlineBust, StructureType.CommandlineBust,
            "structure_0429", "bpstructure0429", "Commandline Display Plinth", "Commandline Bust"),
    };

    public static readonly IReadOnlyDictionary<string, RecipeType> RecipeRenames =
        Renames.ToDictionary(x => x.OldRecipe, x => x.Recipe);

    private static readonly IReadOnlyDictionary<StructureType, Rename> ByStructure =
        Renames.ToDictionary(x => x.Structure);

    private static readonly IReadOnlyDictionary<string, Rename> ByItemResref =
        Renames.SelectMany(x => new[] { (x.StructureResref, x), (x.BlueprintResref, x) })
            .ToDictionary(x => x.Item1, x => x.x);

    /// <summary>Moves entries saved under a former recipe name to the renamed member, keeping an existing entry.</summary>
    public static bool RenameRecipes(JObject recipes)
    {
        if (recipes == null) return false;
        var changed = false;
        foreach (var entry in recipes.Properties().ToArray())
        {
            if (!RecipeRenames.TryGetValue(entry.Name, out var replacement)) continue;
            var name = replacement.ToString();
            if (recipes.Property(name) == null && recipes.Property(((int)replacement).ToString()) == null)
                recipes[name] = entry.Value.DeepClone();
            entry.Remove();
            changed = true;
        }
        return changed;
    }

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
    /// Saves bank and market listings restored or imported after the release migration under their renamed names, so
    /// name searches find them. Loading a listing already renames it (its OnDeserialized callback).
    /// </summary>
    [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
    public static void SaveRestoredListings()
    {
        SaveRestoredListings<InventoryItem>(nameof(InventoryItem.Name), item => item.Name);
        SaveRestoredListings<MarketItem>(nameof(MarketItem.Name), item => item.Name);
    }

    private const string FormerTitle = "Display Plinth";

    private static void SaveRestoredListings<T>(string nameField, Func<T, string> getName)
        where T : EntityBase
    {
        var count = (int)DB.SearchCount(new DBQuery<T>().AddFieldSearch(nameField, FormerTitle, true));
        if (count <= 0)
            return;
        foreach (var listing in DB.Search(new DBQuery<T>().AddFieldSearch(nameField, FormerTitle, true).AddPaging(count, 0)))
        {
            if (!getName(listing).Contains(FormerTitle, StringComparison.Ordinal))
                DB.Set(listing);
        }
    }

    /// <summary>
    /// Renames a player's equipped and carried copies at login, including characters whose saved file the release's
    /// player migration already passed (such as a character file restored afterwards).
    /// </summary>
    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnEnter()
    {
        var player = GetEnteringObject();
        if (GetIsPC(player) && !GetIsDM(player))
            RenameInventory(player);
    }

    /// <summary>Renames an item, or every item a creature or container holds.</summary>
    public static bool RenameInventory(uint obj)
    {
        var visited = new HashSet<uint>();
        bool Visit(uint current)
        {
            if (!GetIsObjectValid(current) || !visited.Add(current)) return false;
            var changed = false;
            if (GetObjectType(current) == ObjectType.Item) changed |= RenameItem(current);
            if (GetObjectType(current) == ObjectType.Creature)
                for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                    changed |= Visit(GetItemInSlot((InventorySlot)slot, current));
            if (GetHasInventory(current))
                for (var item = GetFirstItemInInventory(current); GetIsObjectValid(item); item = GetNextItemInInventory(current))
                    changed |= Visit(item);
            return changed;
        }
        return Visit(obj);
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

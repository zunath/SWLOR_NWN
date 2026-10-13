using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>Saved builds are independent snapshots; editing them never equips techniques.</summary>
public static class MimicryLoadouts
{
    public const int MaxLoadouts = 5;
    public const int MaxNameLength = 24;

    public static bool EnsureDefault(Player player)
    {
        player.MimicryLoadouts ??= new List<MimicryLoadout>();
        if (player.MimicryLoadouts.Count > 0)
            return false;

        var loadout = new MimicryLoadout
        {
            Name = "Default",
            Techniques = new List<FeatType>(player.EquippedTechniques)
        };
        player.MimicryLoadouts.Add(loadout);
        player.ActiveMimicryLoadoutId = loadout.Id;
        return true;
    }

    public static bool Matches(IEnumerable<FeatType> left, IEnumerable<FeatType> right) =>
        left.ToHashSet().SetEquals(right);

    public static bool Save(Player player, string id, string name, IEnumerable<FeatType> techniques,
        out MimicryLoadout loadout, out string error)
    {
        loadout = id == null ? null : player.MimicryLoadouts.FirstOrDefault(x => x.Id == id);
        if (id != null && loadout == null)
        {
            error = "That loadout no longer exists.";
            return false;
        }
        if (loadout == null && player.MimicryLoadouts.Count >= MaxLoadouts)
        {
            error = $"You can save up to {MaxLoadouts} builds. Delete a build to make room.";
            return false;
        }
        if (!ValidateName(player, id, name, out name, out error))
            return false;

        var snapshot = techniques.Distinct().ToList();
        if (loadout == null)
        {
            loadout = new MimicryLoadout();
            player.MimicryLoadouts.Add(loadout);
        }
        loadout.Name = name;
        loadout.Techniques = snapshot;
        loadout.HotbarSlots = loadout.HotbarSlots.Where(x => snapshot.Contains(x.Value))
            .ToDictionary(x => x.Key, x => x.Value);
        return true;
    }

    public static bool Rename(Player player, string id, string name, out string error)
    {
        var loadout = player.MimicryLoadouts.FirstOrDefault(x => x.Id == id);
        if (loadout == null)
        {
            error = "That loadout no longer exists.";
            return false;
        }
        if (!ValidateName(player, id, name, out name, out error))
            return false;
        loadout.Name = name;
        return true;
    }

    public static bool Delete(Player player, string id, out string error)
    {
        error = string.Empty;
        if (player.MimicryLoadouts.Count <= 1)
        {
            error = "Keep at least one saved build.";
            return false;
        }
        if (player.MimicryLoadouts.RemoveAll(x => x.Id == id) == 0)
        {
            error = "That loadout no longer exists.";
            return false;
        }
        // Deleting a preset never changes the techniques actually equipped.
        if (player.ActiveMimicryLoadoutId == id)
            player.ActiveMimicryLoadoutId = string.Empty;
        return true;
    }

    private static bool ValidateName(Player player, string id, string name, out string trimmed, out string error)
    {
        trimmed = (name ?? string.Empty).Trim();
        var candidate = trimmed;
        error = string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxNameLength || trimmed.Any(char.IsControl))
            error = $"Use a name of 1–{MaxNameLength} characters without line breaks.";
        else if (player.MimicryLoadouts.Any(x => x.Id != id && string.Equals(x.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            error = "Another build already uses that name.";
        return error.Length == 0;
    }
}

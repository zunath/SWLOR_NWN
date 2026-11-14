using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Converts a registered ship (plan section 15): a retired hull becomes its new ship with the same interior (the
/// property's layout is not touched) and keeps its old model as a Hull Design when it is one; retired modules become
/// their revamp items and move to their new rows; configuration modules are removed and refunded; modules that no
/// longer fit go back to the owner; and the ship's stats are rebuilt from the hull and modules. Pure: the caller saves
/// the ship and gives the owner the credits, designs and returned modules.
/// </summary>
public static class PlayerShipConversion
{
    private static readonly Dictionary<string, ShipHullEntry> _hulls =
        RevampShipCatalog.Hulls.ToDictionary(x => x.Tag, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, ShipModuleStep> _steps =
        RevampModuleCatalog.Steps.ToDictionary(x => x.Tag, StringComparer.OrdinalIgnoreCase);

    private static readonly ShipModulePowerType[] SlotRows = { ShipModulePowerType.High, ShipModulePowerType.Mid, ShipModulePowerType.Low };

    /// <param name="ship">The ship, changed in place.</param>
    /// <param name="willpower">The owner's Willpower, which adds capacitor.</param>
    public static PlayerShipConversionResult Convert(PlayerShip ship, int willpower)
    {
        var result = new PlayerShipConversionResult();
        var status = ship.Status;
        if (status == null)
            return result;

        status.MidPowerModules ??= new Dictionary<int, ShipStatus.ShipStatusModule>();
        status.ConfigurationModules ??= new Dictionary<int, ShipStatus.ShipStatusModule>();
        status.HighPowerModules ??= new Dictionary<int, ShipStatus.ShipStatusModule>();
        status.LowPowerModules ??= new Dictionary<int, ShipStatus.ShipStatusModule>();

        if (LegacySpaceItemCatalog.TryGetHullByTag(status.ItemTag, out var legacyHull))
        {
            ship.SerializedItem = ConvertItem(ship.SerializedItem, legacyHull.Resref, legacyHull.ShipTag, result.Items);
            status.ItemTag = legacyHull.ShipTag;
            status.RequiredSkills = StoredObjectData.ReadRequiredSkills(ship.SerializedItem);
            ship.HullDesignAppearance = StoredObjectData.TryGetHullDesignKey(legacyHull, out _) ? legacyHull.Appearance : 0;
            result.HullConverted = true;
        }

        if (!_hulls.TryGetValue(status.ItemTag ?? string.Empty, out var hull))
            return result;
        result.ShipTag = hull.Tag;

        var needsRefit = result.HullConverted || status.ConfigurationModules.Count > 0 ||
            AllModules(status).Any(x => !_steps.ContainsKey(x.Module.ItemTag ?? string.Empty));
        if (needsRefit)
            Refit(ship, hull, result);

        var fitted = SlotRows.SelectMany(row => Row(status, row).Select(entry => new ShipFittedModule
        {
            Row = row,
            Slot = entry.Key,
            Step = _steps[entry.Value.ItemTag],
            Traits = entry.Value.Traits,
        })).ToList();
        result.ModulesKept = fitted.Count;
        var stats = ShipStatCalculator.Calculate(hull, fitted, willpower, null, status.HullTraits);
        ShipStatCalculator.Apply(stats, status, result.HullConverted);
        status.ActiveModules?.Clear();
        ship.PlayerHotBars?.Clear();
        result.Changed = true;
        return result;
    }

    /// <summary>Moves every module to its row on the new hull, converting retired ones and returning what no longer fits.</summary>
    private static void Refit(PlayerShip ship, ShipHullEntry hull, PlayerShipConversionResult result)
    {
        var status = ship.Status;
        var placed = SlotRows.ToDictionary(row => row, _ => new Dictionary<int, ShipStatus.ShipStatusModule>());
        var waiting = new List<(ShipModuleStep Step, int Slot, ShipStatus.ShipStatusModule Module)>();
        var index = 0;
        foreach (var (row, slot, module) in AllModules(status).ToList())
        {
            index++;
            var tag = module.ItemTag ?? string.Empty;
            if (LegacySpaceItemCatalog.TryGetConfigurationModule(tag, out var credits))
            {
                result.Items.Credits += credits;
                result.Items.ConfigurationModulesRefunded++;
                result.Items.Count(tag);
                continue;
            }

            if (LegacySpaceItemCatalog.TryGetModule(tag, out var replacement))
            {
                module.SerializedItem = ConvertItem(module.SerializedItem, tag, replacement, result.Items);
                module.ItemTag = replacement;
                module.RequiredSkills = StoredObjectData.ReadRequiredSkills(module.SerializedItem);
                module.Traits = null;
            }

            module.RecastTime = DateTime.MinValue;
            module.RecastStartedAt = DateTime.MinValue;
            module.NpcShot = null;
            if (!_steps.TryGetValue(module.ItemTag ?? string.Empty, out var step) || !Fits(hull, step))
            {
                Return(ship, module, $"{row}-{slot}-{index}", result);
                continue;
            }

            // A module already in its row keeps its slot when the new hull has it.
            if (step.Row == row && slot >= 1 && slot <= Capacity(hull, row) && !placed[row].ContainsKey(slot))
                placed[row][slot] = module;
            else
                waiting.Add((step, slot, module));
        }

        foreach (var (step, slot, module) in waiting)
        {
            var free = Enumerable.Range(1, Capacity(hull, step.Row)).Where(x => !placed[step.Row].ContainsKey(x)).ToList();
            if (free.Count == 0)
            {
                Return(ship, module, $"{step.Row}-{slot}-{module.ItemInstanceId}", result);
                continue;
            }
            placed[step.Row][free[0]] = module;
        }

        status.HighPowerModules = placed[ShipModulePowerType.High];
        status.MidPowerModules = placed[ShipModulePowerType.Mid];
        status.LowPowerModules = placed[ShipModulePowerType.Low];
        status.ConfigurationModules.Clear();
    }

    /// <summary>The fit rules of <see cref="ShipFitRules.GetFitError"/> for a revamp hull and module.</summary>
    private static bool Fits(ShipHullEntry hull, ShipModuleStep step)
    {
        if (step.Capital && hull.Size != ShipSize.Capital)
            return false;
        return step.Family != ShipModuleFamily.CloakingDevice || ShipCloakRules.CanFit(hull.Size, hull.Role);
    }

    private static int Capacity(ShipHullEntry hull, ShipModulePowerType row) => row switch
    {
        ShipModulePowerType.High => hull.High,
        ShipModulePowerType.Mid => hull.Mid,
        ShipModulePowerType.Low => hull.Low,
        _ => 0,
    };

    private static void Return(PlayerShip ship, ShipStatus.ShipStatusModule module, string key, PlayerShipConversionResult result)
    {
        if (string.IsNullOrWhiteSpace(module.SerializedItem))
            throw new InvalidOperationException($"Ship {ship.Id} has a module '{module.ItemTag}' with no saved item to return.");
        var (resref, tag, name) = StoredObjectData.ReadItemIdentity(module.SerializedItem);
        result.ReturnedModules.Add(new OverflowItem
        {
            Id = $"{ship.Id}-returned-module-{(string.IsNullOrWhiteSpace(module.ItemInstanceId) ? key : module.ItemInstanceId)}",
            PlayerId = ship.OwnerPlayerId,
            Name = name,
            Resref = resref,
            Tag = tag,
            Quantity = 1,
            Data = module.SerializedItem,
        });
    }

    /// <summary>A retired deed or module item as its replacement; a missing or unexpected item becomes a new one.</summary>
    private static string ConvertItem(string data, string legacyResref, string replacement, LegacySpaceItemTally tally)
    {
        var itemTally = new LegacySpaceItemTally();
        if (!string.IsNullOrWhiteSpace(data) &&
            StoredObjectData.MigrateLegacySpaceItems(data, out var migrated, itemTally) &&
            string.Equals(itemTally.RootReplacementResref, replacement, StringComparison.OrdinalIgnoreCase))
        {
            tally.Add(itemTally);
            return migrated;
        }

        tally.Count(legacyResref);
        if (LegacySpaceItemCatalog.TryGetHullByResref(legacyResref, out var hull) &&
            StoredObjectData.TryGetHullDesignKey(hull, out var key) && !tally.HullDesigns.Contains(key))
            tally.HullDesigns.Add(key);
        return LegacySpaceItemBlueprints.Get(replacement).Data;
    }

    private static IEnumerable<(ShipModulePowerType Row, int Slot, ShipStatus.ShipStatusModule Module)> AllModules(ShipStatus status) =>
        SlotRows.Append(ShipModulePowerType.Config)
            .SelectMany(row => Row(status, row).OrderBy(x => x.Key).Select(x => (row, x.Key, x.Value)));

    private static Dictionary<int, ShipStatus.ShipStatusModule> Row(ShipStatus status, ShipModulePowerType row) => row switch
    {
        ShipModulePowerType.High => status.HighPowerModules,
        ShipModulePowerType.Mid => status.MidPowerModules,
        ShipModulePowerType.Low => status.LowPowerModules,
        _ => status.ConfigurationModules,
    };
}

using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Carries an item's custom colours across a model change: captures them from the old materials, queues the
    /// carry for the replacement item and moves each colour onto the material that corresponds to it.
    /// </summary>
    public class TintMapCarryService : ITintMapCarryService
    {
        private readonly ITintMapApplyService _apply;
        private readonly IDroidService _droid;
        private readonly IGuiService _gui;
        private readonly ITintMapItemColorService _itemColor;
        private readonly ITintMapStateService _tintMapState;

        public TintMapCarryService(
            ITintMapApplyService apply,
            IDroidService droid,
            IGuiService gui,
            ITintMapItemColorService itemColor,
            ITintMapStateService tintMapState)
        {
            _apply = apply;
            _droid = droid;
            _gui = gui;
            _itemColor = itemColor;
            _tintMapState = tintMapState;
        }

        public TintMapItemColorCarry CaptureItemCustomColors(
            uint item,
            IReadOnlyList<TintMapMaterialSelection> selections)
        {
            var sources = new Dictionary<
                TintMapLayerType,
                IReadOnlyList<TintMapItemColorSource>>();
            if (!GetIsObjectValid(item) || selections == null)
                return new TintMapItemColorCarry(sources);

            foreach (var layer in Enum.GetValues<TintMapLayerType>())
            {
                if (TintMapVariable.IsCreatureColorLayer(layer))
                    continue;

                var layerSelections = selections
                    .Where(selection =>
                        selection.GetPaletteSource(layer) == item &&
                        selection.Material.Layers.Contains(layer))
                    .GroupBy(
                        selection => TintMapVariable.GetName(selection.Material.Resref, layer),
                        StringComparer.Ordinal)
                    .Select(group => group.First())
                    .ToList();
                var layerSources = layerSelections
                    .Select(selection =>
                    {
                        var variableName = TintMapVariable.GetName(
                            selection.Material.Resref,
                            layer);
                        var savedColor = TintMapColorResolver.GetSavedColor(selection, layer, includeGlobalColor: false);
                        var storedValue = TintMapColor.TryFromStoredValue(savedColor, out _) ||
                                          savedColor > 0 &&
                                          savedColor <= TintMapMaterialRegistry.PaletteColorCount
                            ? savedColor
                            : (int?)null;
                        return new TintMapItemColorSource(
                            selection.Material.Resref,
                            variableName,
                            storedValue);
                    })
                    .ToList();
                if (!layerSources.Any(source => source.StoredValue.HasValue))
                    continue;

                // Retain every source slot, including preset slots. The slot positions let the
                // delayed replacement map a partial custom tint only to its corresponding new
                // material, while the custom variable names remain available for stale cleanup
                // even when the layer contains several different colors.
                sources[layer] = layerSources;
            }

            return new TintMapItemColorCarry(sources);
        }

        public void LinkPendingItemColorCarryReplacement(uint sourceItem, uint replacementItem)
        {
            _tintMapState.LinkPendingItemColorCarryReplacement(sourceItem, replacementItem);
        }

        public void QueueItemCustomColorCarry(
            uint creature,
            uint sourceItem,
            uint item,
            uint player,
            InventorySlot slot,
            AppearanceArmor armorPart,
            TintMapItemColorCarry carry,
            bool applyImmediately = false)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(item))
                return;

            // A rapid second model selection can capture no colors because the first delayed carry
            // has not populated its intermediate material yet. It must still link the newest copy
            // into that pending lineage so the original carry can follow the destroyed intermediate.
            LinkPendingItemColorCarryReplacement(sourceItem, item);
            if (carry == null)
                return;

            var registration = _tintMapState.RegisterPendingItemColorCarry(
                sourceItem,
                item,
                armorPart,
                carry.Sources.Keys);
            void ApplyCarry() =>
                ApplyItemCustomColorCarry(creature, item, player, slot, armorPart, carry, registration);

            if (applyImmediately)
                ApplyCarry();
            else
                DelayCommand(TintMapApplyService.RefreshDelaySeconds, ApplyCarry);
        }

        private void ApplyItemCustomColorCarry(
            uint creature,
            uint item,
            uint player,
            InventorySlot slot,
            AppearanceArmor armorPart,
            TintMapItemColorCarry carry,
            (Guid Lineage, IReadOnlyDictionary<TintMapStateRevisionScope, int> Revisions) registration)
        {
            try
            {
                if (!GetIsObjectValid(creature))
                    return;

                if (!TryGetCarryTarget(creature, item, slot, registration.Lineage, out var targetItem))
                    return;

                var itemSelections = TintMapModelResolver.GetCurrentSelections(creature)
                    .Where(selection => selection.PaletteSource == targetItem)
                    .ToList();
                var selections = itemSelections
                    .Where(selection => selection.ArmorPart == armorPart)
                    .ToList();
                var removedStaleVariables = false;
                foreach (var (layer, sourceEntries) in carry.Sources)
                {
                    // A preset/custom edit made after the model click is newer intent. Skip only
                    // that layer so unrelated queued colors can still migrate and clean up.
                    if (!_tintMapState.PendingItemColorCarryLayerIsCurrent(
                            registration.Lineage,
                            layer,
                            armorPart,
                            registration.Revisions))
                    {
                        continue;
                    }

                    if (CarryLayer(creature, targetItem, itemSelections, selections, layer, sourceEntries))
                        removedStaleVariables = true;
                }

                if (removedStaleVariables && _droid.IsDroid(creature))
                    _droid.UpdateEquippedItemSnapshot(creature, targetItem);

                _apply.ApplyCurrentColors(creature);
                if (GetIsObjectValid(player))
                    _gui.PublishRefreshEvent(player, new AppearanceChangedRefreshEvent());
            }
            finally
            {
                _tintMapState.CompletePendingItemColorCarry(registration.Lineage);
            }
        }

        private bool TryGetCarryTarget(uint creature, uint item, InventorySlot slot, Guid lineage, out uint targetItem)
        {
            var slottedItem = GetItemInSlot(slot, creature);
            if (GetIsObjectValid(item) && slottedItem == item)
            {
                targetItem = item;
                return true;
            }

            if (!GetIsObjectValid(item) &&
                GetIsObjectValid(slottedItem) &&
                _tintMapState.BelongsToItemColorCarryLineage(slottedItem, lineage))
            {
                // Rapid model clicks destroy the intermediate copy. Only follow its slot
                // when the equipped item is a registered descendant of that replacement;
                // an ordinary equipment change must never redirect colors to another item.
                targetItem = slottedItem;
                return true;
            }

            targetItem = OBJECT_INVALID;
            return false;
        }

        private bool CarryLayer(
            uint creature,
            uint targetItem,
            IReadOnlyList<TintMapMaterialSelection> itemSelections,
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer,
            IReadOnlyList<TintMapItemColorSource> sourceEntries)
        {
            var removedStaleVariables = false;
            var destinations = selections
                .Where(selection =>
                    selection.GetPaletteSource(layer) == targetItem &&
                    selection.Material.Layers.Contains(layer))
                .GroupBy(
                    selection => TintMapVariable.GetName(selection.Material.Resref, layer),
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();

            var destinationVariables = destinations
                .Select(selection => TintMapVariable.GetName(selection.Material.Resref, layer))
                .ToHashSet(StringComparer.Ordinal);
            var activeVariables = itemSelections
                .Where(selection =>
                    selection.GetPaletteSource(layer) == targetItem &&
                    selection.Material.Layers.Contains(layer))
                .Select(selection => TintMapVariable.GetName(selection.Material.Resref, layer))
                .ToHashSet(StringComparer.Ordinal);
            var sourceVariables = sourceEntries
                .Select(source => source.VariableName)
                .ToHashSet(StringComparer.Ordinal);
            var replacedSources = sourceEntries
                .Where(source => !destinationVariables.Contains(source.VariableName))
                .ToList();
            var replacementDestinations = destinations
                .Where(selection => !sourceVariables.Contains(TintMapVariable.GetName(
                    selection.Material.Resref,
                    layer)))
                .ToList();
            var unmatchedSources = replacedSources.ToList();
            var unmatchedDestinations = replacementDestinations.ToList();
            foreach (var destination in replacementDestinations)
            {
                var equivalentSources = unmatchedSources
                    .Where(source => TintMapColorResolver.AreEquipmentMaterialsEquivalent(
                        source.MaterialResref,
                        destination,
                        layer))
                    .ToList();
                if (equivalentSources.Count != 1)
                    continue;

                var source = equivalentSources[0];
                CarryStoredItemColor(creature, destination, layer, source);
                unmatchedSources.Remove(source);
                unmatchedDestinations.Remove(destination);
            }

            // Preserve positional carry only after material identity has consumed
            // every unambiguous pair. A count-changing replacement can still safely
            // carry one remaining explicit value to one remaining destination.
            if (unmatchedSources.Count == unmatchedDestinations.Count)
            {
                for (var index = 0; index < unmatchedDestinations.Count; index++)
                {
                    CarryStoredItemColor(
                        creature,
                        unmatchedDestinations[index],
                        layer,
                        unmatchedSources[index]);
                }
            }
            else
            {
                var storedSources = unmatchedSources
                    .Where(source => source.StoredValue.HasValue)
                    .ToList();
                if (storedSources.Count == 1 && unmatchedDestinations.Count == 1)
                {
                    CarryStoredItemColor(
                        creature,
                        unmatchedDestinations[0],
                        layer,
                        storedSources[0]);
                }
            }

            foreach (var variableName in sourceEntries
                         .Where(source => source.StoredValue.HasValue)
                         .Select(source => source.VariableName))
            {
                if (destinationVariables.Contains(variableName) ||
                    activeVariables.Contains(variableName))
                {
                    continue;
                }

                DeleteLocalInt(targetItem, variableName);
                removedStaleVariables = true;
            }

            return removedStaleVariables;
        }

        private void CarryStoredItemColor(
            uint creature,
            TintMapMaterialSelection destination,
            TintMapLayerType layer,
            TintMapItemColorSource source)
        {
            if (!source.StoredValue.HasValue)
                return;

            _itemColor.SetStoredColor(
                creature,
                destination,
                layer,
                source.StoredValue.Value,
                invalidatePendingCarry: false);
        }

        public void CarryStoredEquipmentCustomColors(uint creature)
        {
            var selections = TintMapModelResolver.GetCurrentSelections(creature);
            foreach (var itemSelections in selections
                         .Where(selection =>
                             selection.PaletteSource != creature &&
                             GetIsObjectValid(selection.PaletteSource))
                         .GroupBy(selection => selection.PaletteSource))
            {
                var item = itemSelections.Key;
                var storedColors = new List<(
                    string MaterialResref,
                    TintMapLayerType Layer,
                    int StoredValue)>();
                foreach (var (variableName, savedColor) in TintMapOverrideLocals.GetItemTintOverrides(item))
                {
                    if (!TintMapVariable.TryParse(
                            variableName,
                            out var materialResref,
                            out var layer) ||
                        TintMapVariable.IsCreatureColorLayer(layer) ||
                        !TintMapColor.TryFromStoredValue(savedColor, out _) &&
                        (savedColor <= 0 ||
                         savedColor > TintMapMaterialRegistry.PaletteColorCount))
                    {
                        continue;
                    }

                    storedColors.Add((materialResref, layer, savedColor));
                }

                foreach (var selection in itemSelections)
                {
                    foreach (var layer in selection.Material.Layers.Where(layer =>
                                 !TintMapVariable.IsCreatureColorLayer(layer) &&
                                 selection.GetPaletteSource(layer) == item))
                    {
                        var destinationVariable = TintMapVariable.GetName(
                            selection.Material.Resref,
                            layer);
                        var destinationColor = GetLocalInt(item, destinationVariable);
                        if (TintMapColor.TryFromStoredValue(destinationColor, out _) ||
                            destinationColor > 0 &&
                            destinationColor <= TintMapMaterialRegistry.PaletteColorCount)
                        {
                            continue;
                        }

                        if (TintMapColorResolver.HasExplicitItemPresetColor(selection, layer, destinationColor))
                        {
                            // A per-part palette marker is an explicit opt-out from the item-wide
                            // RGB tint, including while the part's former material is inactive.
                            continue;
                        }

                        var matchingColors = storedColors
                            .Where(stored =>
                                stored.Layer == layer &&
                                TintMapColorResolver.AreEquipmentMaterialsEquivalent(
                                    stored.MaterialResref,
                                    selection,
                                    layer))
                            .Select(stored => stored.StoredValue)
                            .Distinct()
                            .ToList();
                        if (matchingColors.Count == 1)
                        {
                            _itemColor.SetStoredColor(
                                creature,
                                selection,
                                layer,
                                matchingColors[0],
                                invalidatePendingCarry: false);
                            continue;
                        }

                        if (TintMapColor.TryFromStoredValue(
                                GetLocalInt(
                                    item,
                                    TintMapVariable.GetItemGlobalColorStateName(layer)),
                                out _))
                        {
                            // The missing TM_* value is the durable inheritance signal. The
                            // following ApplyCurrentColors pass resolves it through TMG_*.
                            continue;
                        }
                    }
                }
            }
        }
    }
}

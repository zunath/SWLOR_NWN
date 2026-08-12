using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public sealed class TintMapItemColorCarry
    {
        public IReadOnlyDictionary<TintMapLayerType, IReadOnlyList<TintMapItemColorSource>> Sources { get; }

        public TintMapItemColorCarry(
            IReadOnlyDictionary<TintMapLayerType, IReadOnlyList<TintMapItemColorSource>> sources)
        {
            Sources = sources;
        }
    }

    public readonly record struct TintMapItemColorSource(
        string VariableName,
        TintMapColor? Color);

    public static class TintMapService
    {
        private const float RefreshDelaySeconds = 0.2f;
        private const string AreaCompatibilityRefreshVariable = "TINT_MAP_COMPAT_REFRESHED";

        private readonly record struct ItemColorCarryRevisionScope(
            TintMapLayerType Layer,
            AppearanceArmor ArmorPart);

        private sealed class PendingItemColorCarryLineage
        {
            public HashSet<uint> Items { get; } = new();
            public Dictionary<ItemColorCarryRevisionScope, int> Revisions { get; } = new();
            public int PendingCount { get; set; }
        }

        private static readonly object PendingItemColorCarryLock = new();
        private static readonly Dictionary<uint, Guid> ItemColorCarryLineages = new();
        private static readonly Dictionary<Guid, PendingItemColorCarryLineage> PendingItemColorCarries = new();

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnModuleEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            DelayCommand(1f, () => ApplyCurrentColors(player));
        }

        [NWNEventHandler(ScriptName.OnPlayerSpawn)]
        public static void OnPlayerSpawn()
        {
            QueueRefresh(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        [NWNEventHandler(ScriptName.OnDroidSpawn)]
        public static void OnCreatureSpawn()
        {
            QueueRefresh(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter()
        {
            var creature = GetEnteringObject();
            if (GetObjectType(creature) != ObjectType.Creature)
                return;

            QueueRefresh(creature);
            if (GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
            {
                var area = GetArea(creature);
                if (!GetIsObjectValid(area) ||
                    GetLocalInt(area, AreaCompatibilityRefreshVariable) != 0)
                {
                    return;
                }

                SetLocalInt(area, AreaCompatibilityRefreshVariable, 1);
                QueueOtherCreaturesInArea(area, creature);
                QueueWorldItemsInArea(area);
            }
        }

        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public static void OnModuleUnacquire()
        {
            QueueItemRefresh(GetModuleItemLost());
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnModuleEquip()
        {
            QueueRefresh(GetPCItemLastEquippedBy());
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void OnModuleUnequip()
        {
            QueueRefresh(GetPCItemLastUnequippedBy());
        }

        [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
        public static void OnAppearanceEquipmentChanged()
        {
            var creature = OBJECT_SELF;
            if (!Droid.IsDroid(creature) && !GetIsDMPossessed(creature))
                return;

            QueueRefreshAndEditor(creature, GetMaster(creature));
        }

        [NWNEventHandler(ScriptName.OnAppearanceEdit)]
        public static void OnAppearanceEdit()
        {
            ApplyCurrentColors(OBJECT_SELF);
        }

        public static void ApplyCurrentColors(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            // Rebuild the complete tint state in one pass. Parameter-scoped wildcard resets are
            // not reliable for modular creatures: on the game client they can discard palette
            // rows already written for other layers, which leaves equipped clothing at row zero.
            // A complete reset also removes legacy wildcard values left by older implementations
            // before the current material-scoped values are installed.
            ResetMaterialShaderUniforms(creature);

            var selections = TintMapModelResolver.GetCurrentSelections(creature);
            var creatureLayers = new HashSet<TintMapLayerType>();
            foreach (var selection in selections)
            {
                foreach (var layer in selection.Material.Layers)
                {
                    if (TintMapVariable.IsCreatureColorLayer(layer))
                    {
                        creatureLayers.Add(layer);
                        continue;
                    }

                    ApplyColor(creature, selection, layer, GetEffectiveColor(creature, selection, layer));
                }
            }

            foreach (var layer in creatureLayers)
            {
                ApplyCreatureColor(
                    creature,
                    selections,
                    layer,
                    GetEffectiveCreatureColor(creature, layer));
            }
        }

        public static void ApplyCurrentItemColors(uint item)
        {
            if (!GetIsObjectValid(item) ||
                GetObjectType(item) != ObjectType.Item ||
                GetIsObjectValid(GetItemPossessor(item)))
            {
                return;
            }

            foreach (var selection in TintMapModelResolver.GetWorldItemSelections(item))
            {
                foreach (var layer in selection.Material.Layers)
                {
                    ApplyColor(item, selection, layer, GetEffectiveColor(OBJECT_INVALID, selection, layer));
                }
            }
        }

        public static void SetColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            TintMapColor color)
        {
            SetColor(creature, selection, layer, color, invalidatePendingCarry: true);
        }

        private static void SetColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            TintMapColor color,
            bool invalidatePendingCarry)
        {
            SetStoredColor(
                creature,
                selection,
                layer,
                color.ToStoredValue(),
                invalidatePendingCarry);
        }

        private static void SetStoredColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            int savedColor,
            bool invalidatePendingCarry)
        {
            var paletteSource = selection.GetPaletteSource(layer);
            if (invalidatePendingCarry)
                MarkPendingItemColorEdit(paletteSource, layer, selection.ArmorPart);

            var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
            var setVariables = GetEquivalentItemTintVariables(
                paletteSource,
                selection,
                layer);
            if (!setVariables.Contains(variableName, StringComparer.Ordinal))
                setVariables.Add(variableName);
            foreach (var setVariable in setVariables)
            {
                SetLocalInt(paletteSource, setVariable, savedColor);
            }

            SaveDroidOverride(creature, selection, layer, variableName, savedColor);
            ApplyColor(
                creature,
                selection,
                layer,
                GetEffectiveColor(creature, selection, layer));
        }

        public static void ResetColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            int? paletteColor = null)
        {
            var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
            var paletteSource = selection.GetPaletteSource(layer);
            MarkPendingItemColorEdit(paletteSource, layer, selection.ArmorPart);
            var resetVariables = GetEquivalentItemTintVariables(
                paletteSource,
                selection,
                layer);
            if (!resetVariables.Contains(variableName, StringComparer.Ordinal))
                resetVariables.Add(variableName);
            var savedPaletteColor = GetObjectType(paletteSource) == ObjectType.Item &&
                                    TintMapColor.TryFromStoredValue(
                                        GetLocalInt(
                                            paletteSource,
                                            TintMapVariable.GetItemGlobalColorStateName(layer)),
                                        out _)
                ? paletteColor.HasValue
                    ? paletteColor.Value + 1
                    : GetStandardColor(creature, selection, layer) + 1
                : 0;
            foreach (var resetVariable in resetVariables)
            {
                if (savedPaletteColor > 0)
                    SetLocalInt(paletteSource, resetVariable, savedPaletteColor);
                else
                    DeleteLocalInt(paletteSource, resetVariable);
            }

            SaveDroidOverride(creature, selection, layer, variableName, savedPaletteColor);
            ApplyColor(
                creature,
                selection,
                layer,
                GetEffectiveColor(creature, selection, layer));
        }

        /// <summary>
        /// Removes a part-specific material override so the material can inherit an active
        /// item-wide custom color. This is distinct from selecting a preset, which deliberately
        /// stores a compatibility palette value beneath an active global tint.
        /// </summary>
        public static void ResetColorToInheritance(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
            var paletteSource = selection.GetPaletteSource(layer);
            MarkPendingItemColorEdit(paletteSource, layer, selection.ArmorPart);
            var resetVariables = GetEquivalentItemTintVariables(
                paletteSource,
                selection,
                layer);
            if (!resetVariables.Contains(variableName, StringComparer.Ordinal))
                resetVariables.Add(variableName);
            foreach (var resetVariable in resetVariables)
            {
                DeleteLocalInt(paletteSource, resetVariable);
            }

            SaveDroidOverride(creature, selection, layer, variableName, 0);
            ApplyColor(
                creature,
                selection,
                layer,
                GetEffectiveColor(creature, selection, layer));
        }

        public static void SetGlobalItemCustomColor(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer,
            TintMapColor color)
        {
            if (selections == null || TintMapVariable.IsCreatureColorLayer(layer))
                return;

            var itemSelections = selections
                .Where(selection =>
                    selection.Material.Layers.Contains(layer) &&
                    GetIsObjectValid(selection.GetPaletteSource(layer)) &&
                    GetObjectType(selection.GetPaletteSource(layer)) == ObjectType.Item)
                .ToList();
            if (itemSelections.Count == 0)
                return;

            var item = itemSelections[0].GetPaletteSource(layer);
            itemSelections = itemSelections
                .Where(selection => selection.GetPaletteSource(layer) == item)
                .ToList();
            var stateVariable = TintMapVariable.GetItemGlobalColorStateName(layer);
            var hasPreviousGlobalColor = TintMapColor.TryFromStoredValue(
                GetLocalInt(item, stateVariable),
                out var previousGlobalColor);
            SetLocalInt(
                item,
                stateVariable,
                color.ToStoredValue());
            foreach (var selection in itemSelections)
            {
                var savedSelectionColor = GetSavedColor(selection, layer);
                var hasSelectionColor = TryGetCustomColor(
                    selection,
                    layer,
                    out var selectionColor);
                var hasExplicitPresetColor = HasExplicitItemPresetColor(
                    selection,
                    layer,
                    savedSelectionColor);
                if (!hasExplicitPresetColor &&
                    (!hasSelectionColor ||
                     hasPreviousGlobalColor && selectionColor == previousGlobalColor))
                {
                    SetColor(creature, selection, layer, color);
                }
            }

            // The global marker is meaningful state even when every active material has an
            // independent override and the loop does not update a material.
            Droid.UpdateEquippedItemSnapshot(creature, item);
        }

        public static void ResetGlobalItemCustomColor(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer)
        {
            if (selections == null || TintMapVariable.IsCreatureColorLayer(layer))
                return;

            var itemSelections = selections
                .Where(selection =>
                    selection.Material.Layers.Contains(layer) &&
                    GetIsObjectValid(selection.GetPaletteSource(layer)) &&
                    GetObjectType(selection.GetPaletteSource(layer)) == ObjectType.Item)
                .ToList();
            if (itemSelections.Count == 0)
                return;

            var item = itemSelections[0].GetPaletteSource(layer);
            itemSelections = itemSelections
                .Where(selection => selection.GetPaletteSource(layer) == item)
                .ToList();
            ResetGlobalItemCustomColor(creature, item, itemSelections, layer);
        }

        private static void ResetGlobalItemCustomColor(
            uint creature,
            uint item,
            IReadOnlyList<TintMapMaterialSelection> itemSelections,
            TintMapLayerType layer)
        {
            var stateVariable = TintMapVariable.GetItemGlobalColorStateName(layer);
            var hasGlobalColor = TintMapColor.TryFromStoredValue(
                GetLocalInt(item, stateVariable),
                out var globalColor);

            // Compatibility for items authored before the explicit global-intent marker existed:
            // only a complete, uniform set can safely be interpreted as a global custom color.
            if (!hasGlobalColor)
            {
                var customColors = itemSelections
                    .Select(selection => TryGetCustomColor(selection, layer, out var color)
                        ? (TintMapColor?)color
                        : null)
                    .ToList();
                var distinctColors = customColors
                    .Where(color => color.HasValue)
                    .Select(color => color!.Value)
                    .Distinct()
                    .ToList();
                hasGlobalColor = customColors.All(color => color.HasValue) && distinctColors.Count == 1;
                if (hasGlobalColor)
                    globalColor = distinctColors[0];
            }

            DeleteLocalInt(item, stateVariable);
            // Persist removal of the marker even when there is no inferred global color, or
            // when no active material still matches it closely enough to be reset below.
            Droid.UpdateEquippedItemSnapshot(creature, item);
            if (!hasGlobalColor)
                return;

            var resetSelections = itemSelections
                .Where(selection =>
                    TryGetCustomColor(selection, layer, out var color) &&
                    color == globalColor)
                .ToList();
            var resetVariables = GetItemTintOverrides(item)
                .Where(entry =>
                    TintMapVariable.TryParse(
                        entry.Key,
                        out _,
                        out var variableLayer) &&
                    variableLayer == layer &&
                    TintMapColor.TryFromStoredValue(entry.Value, out var color) &&
                    color == globalColor)
                .Select(entry => entry.Key)
                .ToList();
            foreach (var resetVariable in resetVariables)
            {
                DeleteLocalInt(item, resetVariable);
            }

            foreach (var selection in resetSelections)
            {
                ResetColor(creature, selection, layer);
            }

            Droid.UpdateEquippedItemSnapshot(creature, item);
        }

        /// <summary>
        /// Clears persisted tint state when the selected armor part no longer exposes the chosen
        /// channel. There is no material selection for the normal reset path in that case, but the
        /// user's preset click still replaces the old custom color.
        /// </summary>
        public static void ResetInactiveItemCustomColor(
            uint creature,
            uint item,
            TintMapLayerType layer,
            AppearanceArmor armorPart)
        {
            if (!GetIsObjectValid(item) ||
                GetObjectType(item) != ObjectType.Item ||
                TintMapVariable.IsCreatureColorLayer(layer))
            {
                return;
            }

            if (armorPart == AppearanceArmor.Invalid)
            {
                ResetGlobalItemCustomColor(
                    creature,
                    item,
                    Array.Empty<TintMapMaterialSelection>(),
                    layer);
                return;
            }

            MarkPendingItemColorEdit(item, layer, armorPart);
            foreach (var variableName in GetItemTintOverrides(item).Keys)
            {
                if (!TintMapVariable.TryParse(
                        variableName,
                        out var materialResref,
                        out var variableLayer) ||
                    variableLayer != layer ||
                    !TintMapMaterialRegistry.IsEquipmentMaterialExclusiveToArmorPart(
                        materialResref,
                        layer,
                        armorPart))
                {
                    continue;
                }

                DeleteLocalInt(item, variableName);
            }

            if (TryGetArmorColorChannel(layer, out var colorChannel))
            {
                DeleteLocalInt(
                    item,
                    ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                        armorPart,
                        colorChannel));
            }

            Droid.UpdateEquippedItemSnapshot(creature, item);
        }

        public static bool TryGetCustomColor(
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            out TintMapColor color)
        {
            var savedColor = GetSavedColor(selection, layer);
            return TintMapColor.TryFromStoredValue(savedColor, out color);
        }

        private static bool HasExplicitItemPresetColor(
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            int savedColor)
        {
            // TM_* values in the legacy 1-176 format are explicit per-material presets. They are
            // also written by ResetColor when a part opts out of an active global RGB tint.
            if (savedColor > 0 && savedColor <= TintMapMaterialRegistry.PaletteColorCount)
                return true;

            if (selection.ArmorPart == AppearanceArmor.Invalid ||
                !TryGetArmorColorChannel(layer, out var colorChannel))
            {
                return false;
            }

            var item = selection.GetPaletteSource(layer);
            return GetIsObjectValid(item) &&
                   GetObjectType(item) == ObjectType.Item &&
                   GetLocalInt(
                       item,
                       ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                           selection.ArmorPart,
                           colorChannel)) > 0;
        }

        private static bool TryGetArmorColorChannel(
            TintMapLayerType layer,
            out AppearanceArmorColor colorChannel)
        {
            colorChannel = layer switch
            {
                TintMapLayerType.Metal1 => AppearanceArmorColor.Metal1,
                TintMapLayerType.Metal2 => AppearanceArmorColor.Metal2,
                TintMapLayerType.Cloth1 => AppearanceArmorColor.Cloth1,
                TintMapLayerType.Cloth2 => AppearanceArmorColor.Cloth2,
                TintMapLayerType.Leather1 => AppearanceArmorColor.Leather1,
                TintMapLayerType.Leather2 => AppearanceArmorColor.Leather2,
                _ => AppearanceArmorColor.NumColors
            };
            return colorChannel != AppearanceArmorColor.NumColors;
        }

        public static void CarryStoredCreatureCustomColors(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            var colors = new Dictionary<TintMapLayerType, TintMapColor>();
            foreach (var layer in Enum.GetValues<TintMapLayerType>())
            {
                if (!TintMapVariable.IsCreatureColorLayer(layer) ||
                    !TintMapColor.TryFromStoredValue(
                        GetLocalInt(creature, GetCreatureCustomColorStateVariable(layer)),
                        out var color))
                {
                    continue;
                }

                colors[layer] = color;
            }

            ApplyCreatureCustomColors(creature, colors);
        }

        public static void ResetCreatureCustomColor(uint creature, TintMapLayerType layer)
        {
            if (!GetIsObjectValid(creature) || !TintMapVariable.IsCreatureColorLayer(layer))
                return;

            var variableNames = GetCreatureCustomColorVariables(creature)
                .Where(variableName =>
                    TintMapVariable.TryGetLayer(variableName, out var variableLayer) &&
                    variableLayer == layer)
                .ToList();
            variableNames.Add(GetCreatureCustomColorStateVariable(layer));
            foreach (var variableName in variableNames)
            {
                DeleteLocalInt(creature, variableName);
            }

            RemoveDroidOverrides(creature, variableNames);
            ApplyCurrentColors(creature);
        }

        public static void SetCreatureCustomColor(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer,
            TintMapColor color)
        {
            if (!GetIsObjectValid(creature) ||
                selections == null ||
                !TintMapVariable.IsCreatureColorLayer(layer))
            {
                return;
            }

            var savedColor = color.ToStoredValue();
            var stateVariable = GetCreatureCustomColorStateVariable(layer);
            SetLocalInt(creature, stateVariable, savedColor);
            var variableNames = GetCreatureCustomColorVariables(creature)
                .Where(variableName =>
                    TintMapVariable.TryGetLayer(variableName, out var variableLayer) &&
                    variableLayer == layer)
                .ToHashSet(StringComparer.Ordinal);

            // The NUI can remain open while an equipment or body-part replacement completes.
            // Persist every currently rendered semantic material so model swaps and droid
            // snapshots retain the edit. Shader values remain scoped to materials which the
            // tint-map registry explicitly declares for this semantic layer; publishing an empty
            // material name would also recolor unrelated equipment and full-body NPC materials.
            var currentSelections = TintMapModelResolver.GetCurrentSelections(creature);
            foreach (var selection in selections.Concat(currentSelections))
            {
                if (selection.GetPaletteSource(layer) != creature ||
                    !selection.Material.Layers.Contains(layer))
                {
                    continue;
                }

                var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
                variableNames.Add(variableName);
            }

            foreach (var variableName in variableNames)
            {
                SetLocalInt(creature, variableName, savedColor);
            }

            SaveDroidOverrides(
                creature,
                variableNames.Concat(new[] { stateVariable }).ToList(),
                savedColor);
            ApplyCurrentColors(creature);
        }

        private static void ApplyCreatureCustomColors(
            uint creature,
            IReadOnlyDictionary<TintMapLayerType, TintMapColor> colors)
        {
            if (colors.Count == 0)
                return;

            var selections = TintMapModelResolver.GetCurrentSelections(creature);
            foreach (var (layer, color) in colors)
            {
                SetCreatureCustomColor(creature, selections, layer, color);
            }
        }

        public static void RestoreDroidOverrides(
            uint droid,
            IReadOnlyDictionary<string, int> tintOverrides)
        {
            if (!GetIsObjectValid(droid) || tintOverrides == null)
                return;

            foreach (var (variableName, savedColor) in tintOverrides)
            {
                if (string.IsNullOrWhiteSpace(variableName) || savedColor <= 0)
                    continue;

                SetLocalInt(droid, variableName, savedColor);
            }
        }

        public static void ReplaceItemTintOverrides(uint sourceItem, uint targetItem)
        {
            if (!GetIsObjectValid(sourceItem) || !GetIsObjectValid(targetItem))
                return;

            var sourceOverrides = GetItemTintOverrides(sourceItem);
            var targetOverrides = GetItemTintOverrides(targetItem);

            foreach (var variableName in targetOverrides.Keys)
            {
                DeleteLocalInt(targetItem, variableName);
            }

            foreach (var (variableName, savedColor) in sourceOverrides)
            {
                SetLocalInt(targetItem, variableName, savedColor);
            }
        }

        public static TintMapItemColorCarry CaptureItemCustomColors(
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
                        TintMapColor? color = TryGetCustomColor(selection, layer, out var customColor)
                            ? customColor
                            : null;
                        return new TintMapItemColorSource(variableName, color);
                    })
                    .ToList();
                if (!layerSources.Any(source => source.Color.HasValue))
                    continue;

                // Retain every source slot, including preset slots. The slot positions let the
                // delayed replacement map a partial custom tint only to its corresponding new
                // material, while the custom variable names remain available for stale cleanup
                // even when the layer contains several different colors.
                sources[layer] = layerSources;
            }

            return new TintMapItemColorCarry(sources);
        }

        private static (Guid Lineage, IReadOnlyDictionary<ItemColorCarryRevisionScope, int> Revisions)
            RegisterPendingItemColorCarry(
                uint sourceItem,
                uint replacementItem,
                AppearanceArmor armorPart,
                IEnumerable<TintMapLayerType> layers)
        {
            lock (PendingItemColorCarryLock)
            {
                if (!ItemColorCarryLineages.TryGetValue(sourceItem, out var lineage) ||
                    !PendingItemColorCarries.TryGetValue(lineage, out var state))
                {
                    lineage = Guid.NewGuid();
                    state = new PendingItemColorCarryLineage();
                    PendingItemColorCarries[lineage] = state;
                }

                ItemColorCarryLineages[replacementItem] = lineage;
                state.Items.Add(replacementItem);
                state.PendingCount++;
                var revisions = layers
                    .Distinct()
                    .ToDictionary(
                        layer => new ItemColorCarryRevisionScope(layer, armorPart),
                        layer => state.Revisions.GetValueOrDefault(
                            new ItemColorCarryRevisionScope(layer, armorPart)));
                return (lineage, revisions);
            }
        }

        public static void LinkPendingItemColorCarryReplacement(uint sourceItem, uint replacementItem)
        {
            lock (PendingItemColorCarryLock)
            {
                if (!ItemColorCarryLineages.TryGetValue(sourceItem, out var lineage) ||
                    !PendingItemColorCarries.TryGetValue(lineage, out var state))
                {
                    return;
                }

                ItemColorCarryLineages[replacementItem] = lineage;
                state.Items.Add(replacementItem);
            }
        }

        private static bool BelongsToItemColorCarryLineage(uint item, Guid lineage)
        {
            lock (PendingItemColorCarryLock)
            {
                return ItemColorCarryLineages.TryGetValue(item, out var itemLineage) &&
                       itemLineage == lineage;
            }
        }

        private static bool PendingItemColorCarryLayerIsCurrent(
            Guid lineage,
            TintMapLayerType layer,
            AppearanceArmor armorPart,
            IReadOnlyDictionary<ItemColorCarryRevisionScope, int> capturedRevisions)
        {
            var scope = new ItemColorCarryRevisionScope(layer, armorPart);
            lock (PendingItemColorCarryLock)
            {
                return PendingItemColorCarries.TryGetValue(lineage, out var state) &&
                       state.Revisions.GetValueOrDefault(scope) ==
                       capturedRevisions.GetValueOrDefault(scope);
            }
        }

        private static void MarkPendingItemColorEdit(
            uint item,
            TintMapLayerType layer,
            AppearanceArmor armorPart)
        {
            lock (PendingItemColorCarryLock)
            {
                if (!ItemColorCarryLineages.TryGetValue(item, out var lineage) ||
                    !PendingItemColorCarries.TryGetValue(lineage, out var state))
                {
                    return;
                }

                var scope = new ItemColorCarryRevisionScope(layer, armorPart);
                state.Revisions[scope] = state.Revisions.GetValueOrDefault(scope) + 1;
            }
        }

        private static void CompletePendingItemColorCarry(Guid lineage)
        {
            lock (PendingItemColorCarryLock)
            {
                if (!PendingItemColorCarries.TryGetValue(lineage, out var state))
                    return;

                state.PendingCount--;
                if (state.PendingCount > 0)
                    return;

                foreach (var item in state.Items)
                {
                    if (ItemColorCarryLineages.TryGetValue(item, out var itemLineage) &&
                        itemLineage == lineage)
                    {
                        ItemColorCarryLineages.Remove(item);
                    }
                }

                PendingItemColorCarries.Remove(lineage);
            }
        }

        public static void QueueItemCustomColorCarry(
            uint creature,
            uint sourceItem,
            uint item,
            uint player,
            InventorySlot slot,
            AppearanceArmor armorPart,
            TintMapItemColorCarry carry)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(item))
                return;

            // A rapid second model selection can capture no colors because the first delayed carry
            // has not populated its intermediate material yet. It must still link the newest copy
            // into that pending lineage so the original carry can follow the destroyed intermediate.
            LinkPendingItemColorCarryReplacement(sourceItem, item);
            if (carry == null)
                return;

            var registration = RegisterPendingItemColorCarry(
                sourceItem,
                item,
                armorPart,
                carry.Sources.Keys);
            DelayCommand(RefreshDelaySeconds, () =>
            {
                try
                {
                    if (!GetIsObjectValid(creature))
                        return;

                    var slottedItem = GetItemInSlot(slot, creature);
                    uint targetItem;
                    if (GetIsObjectValid(item) && slottedItem == item)
                    {
                        targetItem = item;
                    }
                    else if (!GetIsObjectValid(item) &&
                             GetIsObjectValid(slottedItem) &&
                             BelongsToItemColorCarryLineage(slottedItem, registration.Lineage))
                    {
                        // Rapid model clicks destroy the intermediate copy. Only follow its slot
                        // when the equipped item is a registered descendant of that replacement;
                        // an ordinary equipment change must never redirect colors to another item.
                        targetItem = slottedItem;
                    }
                    else
                    {
                        return;
                    }

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
                        if (!PendingItemColorCarryLayerIsCurrent(
                                registration.Lineage,
                                layer,
                                armorPart,
                                registration.Revisions))
                        {
                            continue;
                        }

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
                        var distinctColors = replacedSources
                            .Where(source => source.Color.HasValue)
                            .Select(source => source.Color!.Value)
                            .Distinct()
                            .ToList();
                        var globalColor = TintMapColor.TryFromStoredValue(
                            GetLocalInt(
                                targetItem,
                                TintMapVariable.GetItemGlobalColorStateName(layer)),
                            out var storedGlobalColor)
                            ? storedGlobalColor
                            : (TintMapColor?)null;
                        if (distinctColors.Count == 1 || globalColor.HasValue)
                        {
                            for (var index = 0; index < replacementDestinations.Count; index++)
                            {
                                var color = index < replacedSources.Count
                                    ? replacedSources[index].Color ?? globalColor
                                    : globalColor;
                                if (color is { } replacementColor)
                                {
                                    SetColor(
                                        creature,
                                        replacementDestinations[index],
                                        layer,
                                        replacementColor,
                                        invalidatePendingCarry: false);
                                }
                            }
                        }

                        foreach (var variableName in sourceEntries
                                     .Where(source => source.Color.HasValue)
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
                    }

                    if (removedStaleVariables && Droid.IsDroid(creature))
                        Droid.UpdateEquippedItemSnapshot(creature, targetItem);

                    ApplyCurrentColors(creature);
                    if (GetIsObjectValid(player))
                        Gui.PublishRefreshEvent(player, new AppearanceChangedRefreshEvent());
                }
                finally
                {
                    CompletePendingItemColorCarry(registration.Lineage);
                }
            });
        }

        public static void QueueRefresh(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            DelayCommand(RefreshDelaySeconds, () =>
            {
                CarryStoredEquipmentCustomColors(creature);
                CarryStoredCreatureCustomColors(creature);
                ApplyCurrentColors(creature);
            });
        }

        public static void QueueItemRefresh(uint item)
        {
            if (!GetIsObjectValid(item))
                return;

            DelayCommand(RefreshDelaySeconds, () => ApplyCurrentItemColors(item));
        }

        public static void QueueRefreshAndEditor(uint creature, uint player)
        {
            QueueRefresh(creature);
            if (!GetIsObjectValid(player))
                return;

            DelayCommand(
                RefreshDelaySeconds,
                () => Gui.PublishRefreshEvent(player, new AppearanceChangedRefreshEvent()));
        }

        private static void QueueWorldItemsInArea(uint area)
        {
            if (!GetIsObjectValid(area))
                return;

            for (var item = GetFirstObjectInArea(area, ObjectType.Item);
                 GetIsObjectValid(item);
                 item = GetNextObjectInArea(area, ObjectType.Item))
            {
                QueueItemRefresh(item);
            }
        }

        private static void QueueOtherCreaturesInArea(uint area, uint enteringCreature)
        {
            if (!GetIsObjectValid(area))
                return;

            // Placed creatures can finish their spawn scripts before the managed server has
            // registered the tint-map hooks. Refresh every creature when a player first makes the
            // area visible so those static NPCs do not retain the generated MTR's row-zero defaults.
            for (var creature = GetFirstObjectInArea(area, ObjectType.Creature);
                 GetIsObjectValid(creature);
                 creature = GetNextObjectInArea(area, ObjectType.Creature))
            {
                if (creature != enteringCreature)
                    QueueRefresh(creature);
            }
        }

        private static TintMapColorSelection GetEffectiveColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var savedColor = GetSavedColor(selection, layer);

            var standardColor = GetStandardColor(creature, selection, layer);
            if (TintMapColor.TryFromStoredValue(savedColor, out var customColor))
                return new TintMapColorSelection(standardColor, customColor);

            // Values 1-176 are the palette-index format used by the original tint-map branch.
            var paletteColor = savedColor > 0 && savedColor <= TintMapMaterialRegistry.PaletteColorCount
                ? savedColor - 1
                : standardColor;
            return new TintMapColorSelection(paletteColor, null);
        }

        private static TintMapColorSelection GetEffectiveCreatureColor(
            uint creature,
            TintMapLayerType layer)
        {
            if (TintMapColor.TryFromStoredValue(
                    GetLocalInt(creature, GetCreatureCustomColorStateVariable(layer)),
                    out var globalColor))
            {
                return new TintMapColorSelection(
                    GetCreatureStandardColor(creature, layer),
                    globalColor);
            }

            return new TintMapColorSelection(GetCreatureStandardColor(creature, layer), null);
        }

        public static TintMapColor GetEffectiveDisplayColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var effectiveColor = TintMapVariable.IsCreatureColorLayer(layer)
                ? GetEffectiveCreatureColor(creature, layer)
                : GetEffectiveColor(creature, selection, layer);
            return effectiveColor.CustomColor ??
                   TintMapPaletteColors.GetColor(layer, effectiveColor.PaletteColorId);
        }

        private static int GetSavedColor(
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var paletteSource = selection.GetPaletteSource(layer);
            var savedColor = GetLocalInt(
                paletteSource,
                TintMapVariable.GetName(selection.Material.Resref, layer));
            if (savedColor > 0 || string.IsNullOrWhiteSpace(selection.OverrideModelResref))
                return savedColor;

            // A worn cloak renders the texture selected by cloakmodel.2da, while its dropped
            // ground model retains the appearance-number material. Read the worn material's
            // same semantic layer so dropping the item does not discard its visible tint.
            foreach (var material in TintMapMaterialRegistry.GetMaterials(selection.OverrideModelResref))
            {
                if (!material.Layers.Contains(layer))
                    continue;

                savedColor = GetLocalInt(
                    paletteSource,
                    TintMapVariable.GetName(material.Resref, layer));
                if (savedColor > 0)
                    return savedColor;
            }

            // A race or gender-specific worn cloak can use a generated material resref that is
            // unrelated to the generic cloakmodel material. Match its semantic material slot and
            // accept the fallback only when it identifies one unambiguous stored color.
            var overrideMaterials = TintMapMaterialRegistry
                .GetMaterials(selection.OverrideModelResref)
                .Where(material => material.Layers.Contains(layer))
                .ToList();
            var equivalentColors = GetItemTintOverrides(paletteSource)
                .Where(entry =>
                    TintMapVariable.TryParse(
                        entry.Key,
                        out var materialResref,
                        out var variableLayer) &&
                    variableLayer == layer &&
                    overrideMaterials.Any(material =>
                        TintMapMaterialRegistry.AreEquipmentMaterialSlotsEquivalent(
                            materialResref,
                            selection.OverrideModelResref,
                            material.Resref,
                            layer)))
                .Select(entry => entry.Value)
                .Where(value => value > 0)
                .Distinct()
                .ToList();
            if (equivalentColors.Count == 1)
                return equivalentColors[0];

            return 0;
        }

        private static int GetStandardColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            if (selection.UsesItemColor(layer))
            {
                var itemColor = layer switch
                {
                    TintMapLayerType.Metal1 => AppearanceArmorColor.Metal1,
                    TintMapLayerType.Metal2 => AppearanceArmorColor.Metal2,
                    TintMapLayerType.Cloth1 => AppearanceArmorColor.Cloth1,
                    TintMapLayerType.Cloth2 => AppearanceArmorColor.Cloth2,
                    TintMapLayerType.Leather1 => AppearanceArmorColor.Leather1,
                    TintMapLayerType.Leather2 => AppearanceArmorColor.Leather2,
                    _ => AppearanceArmorColor.NumColors
                };

                if (itemColor != AppearanceArmorColor.NumColors)
                {
                    var colorIndex = (int)itemColor;
                    if (selection.ArmorPart != AppearanceArmor.Invalid)
                    {
                        var perPartIndex = ArmorColorIndexCalculator.CalculatePerPart(
                            selection.ArmorPart,
                            itemColor);
                        var perPartColor = GetItemAppearance(
                            selection.PaletteSource,
                            ItemAppearanceType.ArmorColor,
                            perPartIndex);
                        var hasExplicitOverride = GetLocalInt(
                            selection.PaletteSource,
                            ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                                selection.ArmorPart,
                                itemColor)) > 0;

                        if (ArmorColorIndexCalculator.ShouldUsePerPartColor(
                                perPartColor,
                                hasExplicitOverride))
                        {
                            return Math.Clamp(perPartColor, 0, TintMapMaterialRegistry.PaletteColorCount - 1);
                        }
                    }

                    return Math.Clamp(
                        GetItemAppearance(selection.PaletteSource, ItemAppearanceType.ArmorColor, colorIndex),
                        0,
                        TintMapMaterialRegistry.PaletteColorCount - 1);
                }
            }

            return GetCreatureStandardColor(creature, layer);
        }

        private static int GetCreatureStandardColor(uint creature, TintMapLayerType layer)
        {
            var creatureColor = layer switch
            {
                TintMapLayerType.Skin => ColorChannel.Skin,
                TintMapLayerType.Hair => ColorChannel.Hair,
                TintMapLayerType.Tattoo1 => ColorChannel.Tattoo1,
                TintMapLayerType.Tattoo2 => ColorChannel.Tattoo2,
                _ => (ColorChannel)(-1)
            };

            return (int)creatureColor >= 0 && GetObjectType(creature) == ObjectType.Creature
                ? Math.Clamp(GetColor(creature, creatureColor), 0, TintMapMaterialRegistry.PaletteColorCount - 1)
                : 0;
        }

        public static int GetStandardColorId(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            return GetStandardColor(creature, selection, layer);
        }

        private static void ApplyColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            TintMapColorSelection color)
        {
            ApplyMaterialColor(creature, selection.Material.Resref, layer, color);
        }

        private static void ApplyCreatureColor(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer,
            TintMapColorSelection color)
        {
            var appliedMaterials = new HashSet<string>(StringComparer.Ordinal);
            foreach (var selection in selections)
            {
                if (selection.GetPaletteSource(layer) != creature ||
                    !selection.Material.Layers.Contains(layer) ||
                    !appliedMaterials.Add(selection.Material.Resref))
                {
                    continue;
                }

                ApplyMaterialColor(creature, selection.Material.Resref, layer, color);
            }
        }

        private static void ApplyMaterialColor(
            uint creature,
            string materialResref,
            TintMapLayerType layer,
            TintMapColorSelection color)
        {
            var layerDefinition = TintMapMaterialRegistry.GetLayer(layer);
            var customColor = color.CustomColor;

            SetMaterialShaderUniformVec4(
                creature,
                materialResref,
                layerDefinition.UniformName,
                customColor.HasValue
                    ? 0f
                    : TintMapMaterialRegistry.GetPaletteCoordinate(layer, color.PaletteColorId));

            if (!customColor.HasValue)
            {
                // Presets only need the palette-row override. Removing stale custom values keeps
                // modular creatures below the engine's per-object shader-override limit; writing
                // zero-valued RGB and mode overrides here can otherwise crowd out later body parts.
                ResetMaterialShaderUniforms(
                    creature,
                    materialResref,
                    layerDefinition.ColorUniformName);
                ResetMaterialShaderUniforms(
                    creature,
                    materialResref,
                    layerDefinition.CustomModeUniformName);
                return;
            }

            SetMaterialShaderUniformVec4(
                creature,
                materialResref,
                layerDefinition.ColorUniformName,
                customColor.Value.Red / 255f,
                customColor.Value.Green / 255f,
                customColor.Value.Blue / 255f,
                1f);
            // Keep a dedicated mode scalar, while the zero palette row above provides a second
            // activation signal for clients that drop that scalar override. Every real palette
            // coordinate is strictly positive, so zero cannot collide with a preset selection.
            SetMaterialShaderUniformVec4(
                creature,
                materialResref,
                layerDefinition.CustomModeUniformName,
                1f);
        }

        private static Dictionary<string, int> GetItemTintOverrides(uint item)
        {
            var tintOverrides = new Dictionary<string, int>(StringComparer.Ordinal);
            var variableCount = ObjectPlugin.GetLocalVariableCount(item);
            for (var index = 0; index < variableCount; index++)
            {
                var variable = ObjectPlugin.GetLocalVariable(item, index);
                if (variable.Type != LocalVariableType.Int ||
                    (!variable.Key.StartsWith(TintMapVariable.Prefix, StringComparison.Ordinal) &&
                     !TintMapVariable.IsItemGlobalColorStateName(variable.Key) &&
                     !ArmorColorIndexCalculator.IsPerPartOverrideVariableName(variable.Key)))
                {
                    continue;
                }

                tintOverrides[variable.Key] = GetLocalInt(item, variable.Key);
            }

            return tintOverrides;
        }

        private static List<string> GetEquivalentItemTintVariables(
            uint item,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            if (!GetIsObjectValid(item) ||
                GetObjectType(item) != ObjectType.Item ||
                TintMapVariable.IsCreatureColorLayer(layer))
            {
                return new List<string>();
            }

            return GetItemTintOverrides(item).Keys
                .Where(variableName =>
                    TintMapVariable.TryParse(
                        variableName,
                        out var materialResref,
                        out var variableLayer) &&
                    variableLayer == layer &&
                    AreEquipmentMaterialsEquivalent(materialResref, selection, layer))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static bool AreEquipmentMaterialsEquivalent(
            string sourceMaterialResref,
            TintMapMaterialSelection destination,
            TintMapLayerType layer)
        {
            return TintMapMaterialRegistry.AreEquipmentMaterialSlotsEquivalent(
                sourceMaterialResref,
                destination.ModelResref,
                destination.Material.Resref,
                layer);
        }

        private static void CarryStoredEquipmentCustomColors(uint creature)
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
                foreach (var (variableName, savedColor) in GetItemTintOverrides(item))
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

                        if (HasExplicitItemPresetColor(selection, layer, destinationColor))
                        {
                            // A per-part palette marker is an explicit opt-out from the item-wide
                            // RGB tint, including while the part's former material is inactive.
                            continue;
                        }

                        if (TintMapColor.TryFromStoredValue(
                                GetLocalInt(
                                    item,
                                    TintMapVariable.GetItemGlobalColorStateName(layer)),
                                out var globalColor))
                        {
                            SetColor(
                                creature,
                                selection,
                                layer,
                                globalColor,
                                invalidatePendingCarry: false);
                            continue;
                        }

                        var matchingColors = storedColors
                            .Where(stored =>
                                stored.Layer == layer &&
                                AreEquipmentMaterialsEquivalent(
                                    stored.MaterialResref,
                                    selection,
                                    layer))
                            .Select(stored => stored.StoredValue)
                            .Distinct()
                            .ToList();
                        if (matchingColors.Count == 1)
                            SetStoredColor(
                                creature,
                                selection,
                                layer,
                                matchingColors[0],
                                invalidatePendingCarry: false);
                    }
                }
            }
        }

        private static IReadOnlyList<string> GetCreatureCustomColorVariables(uint creature)
        {
            var variableNames = new List<string>();
            var variableCount = ObjectPlugin.GetLocalVariableCount(creature);
            for (var index = 0; index < variableCount; index++)
            {
                var variable = ObjectPlugin.GetLocalVariable(creature, index);
                if (variable.Type != LocalVariableType.Int ||
                    !TintMapVariable.TryGetLayer(variable.Key, out var layer) ||
                    !TintMapVariable.IsCreatureColorLayer(layer))
                {
                    continue;
                }

                variableNames.Add(variable.Key);
            }

            return variableNames;
        }

        private static string GetCreatureCustomColorStateVariable(TintMapLayerType layer)
        {
            return TintMapVariable.GetCreatureColorStateName(layer);
        }

        private static void RemoveDroidOverrides(uint creature, IReadOnlyList<string> variableNames)
        {
            if (variableNames.Count == 0 || !Droid.IsDroid(creature))
                return;

            var controller = Droid.GetControllerItem(creature);
            if (!GetIsObjectValid(controller))
                return;

            var constructedDroid = Droid.LoadConstructedDroid(controller);
            var changed = false;
            foreach (var variableName in variableNames)
            {
                changed |= constructedDroid.TintOverrides.Remove(variableName);
            }

            if (changed)
                Droid.SaveConstructedDroid(controller, constructedDroid);
        }

        private static void SaveDroidOverrides(
            uint creature,
            IReadOnlyList<string> variableNames,
            int savedColor)
        {
            if (variableNames.Count == 0 || !Droid.IsDroid(creature))
                return;

            var controller = Droid.GetControllerItem(creature);
            if (!GetIsObjectValid(controller))
                return;

            var constructedDroid = Droid.LoadConstructedDroid(controller);
            foreach (var variableName in variableNames)
            {
                constructedDroid.TintOverrides[variableName] = savedColor;
            }

            Droid.SaveConstructedDroid(controller, constructedDroid);
        }

        private static void SaveDroidOverride(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            string variableName,
            int savedColor)
        {
            if (!Droid.IsDroid(creature))
                return;

            var paletteSource = selection.GetPaletteSource(layer);
            if (paletteSource != creature)
            {
                Droid.UpdateEquippedItemSnapshot(creature, paletteSource);
                return;
            }

            var controller = Droid.GetControllerItem(creature);
            if (!GetIsObjectValid(controller))
                return;

            var constructedDroid = Droid.LoadConstructedDroid(controller);
            if (savedColor > 0)
                constructedDroid.TintOverrides[variableName] = savedColor;
            else
                constructedDroid.TintOverrides.Remove(variableName);

            Droid.SaveConstructedDroid(controller, constructedDroid);
        }
    }
}

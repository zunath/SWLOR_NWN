using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Sets and resets the colours of an item's materials: one part, the whole item, and the colours a part that
    /// no longer shows the channel still carries.
    /// </summary>
    public class TintMapItemColorService : ITintMapItemColorService
    {
        private readonly ITintMapApplyService _apply;
        private readonly IDroidService _droid;
        private readonly ITintMapDroidOverrideService _droidOverrides;
        private readonly ITintMapStateService _tintMapState;

        public TintMapItemColorService(
            ITintMapApplyService apply,
            IDroidService droid,
            ITintMapDroidOverrideService droidOverrides,
            ITintMapStateService tintMapState)
        {
            _apply = apply;
            _droid = droid;
            _droidOverrides = droidOverrides;
            _tintMapState = tintMapState;
        }

        public void SetColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            TintMapColor color)
        {
            SetColor(creature, selection, layer, color, invalidatePendingCarry: true);
        }

        private void SetColor(
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

        public void SetStoredColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            int savedColor,
            bool invalidatePendingCarry)
        {
            var paletteSource = selection.GetPaletteSource(layer);
            if (invalidatePendingCarry)
                _tintMapState.MarkPendingItemColorEdit(paletteSource, layer, selection.ArmorPart);

            var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
            var setVariables = TintMapColorResolver.GetEquivalentItemTintVariables(
                paletteSource,
                selection,
                layer);
            if (!setVariables.Contains(variableName, StringComparer.Ordinal))
                setVariables.Add(variableName);
            foreach (var setVariable in setVariables)
            {
                SetLocalInt(paletteSource, setVariable, savedColor);
            }

            _droidOverrides.SaveDroidOverride(creature, selection, layer, variableName, savedColor);
            _apply.ApplyColor(
                creature,
                selection,
                layer,
                TintMapColorResolver.GetEffectiveColor(creature, selection, layer));
            _apply.ApplyCurrentColorsAndPublish(creature);
        }

        public void ResetColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            int? paletteColor = null)
        {
            var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
            var paletteSource = selection.GetPaletteSource(layer);
            _tintMapState.MarkPendingItemColorEdit(paletteSource, layer, selection.ArmorPart);
            var resetVariables = TintMapColorResolver.GetEquivalentItemTintVariables(
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
                    : TintMapColorResolver.GetStandardColor(creature, selection, layer) + 1
                : 0;
            foreach (var resetVariable in resetVariables)
            {
                if (savedPaletteColor > 0)
                    SetLocalInt(paletteSource, resetVariable, savedPaletteColor);
                else
                    DeleteLocalInt(paletteSource, resetVariable);
            }

            _droidOverrides.SaveDroidOverride(creature, selection, layer, variableName, savedPaletteColor);
            _apply.ApplyColor(
                creature,
                selection,
                layer,
                TintMapColorResolver.GetEffectiveColor(creature, selection, layer));
            _apply.ApplyCurrentColorsAndPublish(creature);
        }

        /// <summary>
        /// Removes a part-specific material override so the material can inherit an active
        /// item-wide custom color. This is distinct from selecting a preset, which deliberately
        /// stores a compatibility palette value beneath an active global tint.
        /// </summary>
        public void ResetColorToInheritance(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
            var paletteSource = selection.GetPaletteSource(layer);
            _tintMapState.MarkPendingItemColorEdit(paletteSource, layer, selection.ArmorPart);
            var resetVariables = TintMapColorResolver.GetEquivalentItemTintVariables(
                paletteSource,
                selection,
                layer);
            if (!resetVariables.Contains(variableName, StringComparer.Ordinal))
                resetVariables.Add(variableName);
            foreach (var resetVariable in resetVariables)
            {
                DeleteLocalInt(paletteSource, resetVariable);
            }

            _droidOverrides.SaveDroidOverride(creature, selection, layer, variableName, 0);
            _apply.ApplyColor(
                creature,
                selection,
                layer,
                TintMapColorResolver.GetEffectiveColor(creature, selection, layer));
            _apply.ApplyCurrentColorsAndPublish(creature);
        }

        public void SetGlobalItemCustomColor(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer,
            TintMapColor color,
            uint item = OBJECT_INVALID)
        {
            if (selections == null || TintMapVariable.IsCreatureColorLayer(layer))
                return;

            var itemSelections = selections
                .Where(selection =>
                    selection.Material.Layers.Contains(layer) &&
                    GetIsObjectValid(selection.GetPaletteSource(layer)) &&
                    GetObjectType(selection.GetPaletteSource(layer)) == ObjectType.Item)
                .ToList();
            if (!GetIsObjectValid(item) && itemSelections.Count > 0)
                item = itemSelections[0].GetPaletteSource(layer);
            if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
                return;

            itemSelections = itemSelections
                .Where(selection => selection.GetPaletteSource(layer) == item)
                .ToList();
            _tintMapState.MarkPendingItemColorEdit(item, layer, AppearanceArmor.Invalid);
            var stateVariable = TintMapVariable.GetItemGlobalColorStateName(layer);
            var inheritanceStateVariable = TintMapVariable.GetItemGlobalInheritanceStateName(layer);
            var hasPreviousGlobalColor = TintMapColor.TryFromStoredValue(
                GetLocalInt(item, stateVariable),
                out var previousGlobalColor);
            if (!hasPreviousGlobalColor)
            {
                hasPreviousGlobalColor = TryInferLegacyGlobalItemCustomColor(
                    item,
                    itemSelections,
                    layer,
                    out previousGlobalColor);
            }

            // Items created before inheritance intent was persisted copied the old global RGB
            // into every inheriting TM_* entry. Normalize that representation once. From this
            // point forward, absence of a material value means inheritance and an explicit TM_*
            // remains independent even when its RGB happens to equal the global value.
            if (hasPreviousGlobalColor && GetLocalInt(item, inheritanceStateVariable) == 0)
            {
                foreach (var legacyVariable in TintMapOverrideLocals.GetItemTintOverrides(item)
                             .Where(entry =>
                                 TintMapVariable.TryGetLayer(entry.Key, out var variableLayer) &&
                                 variableLayer == layer &&
                                 TintMapColor.TryFromStoredValue(entry.Value, out var legacyColor) &&
                                 legacyColor == previousGlobalColor)
                             .Select(entry => entry.Key)
                             .ToList())
                {
                    DeleteLocalInt(item, legacyVariable);
                }
            }

            SetLocalInt(
                item,
                stateVariable,
                color.ToStoredValue());
            SetLocalInt(item, inheritanceStateVariable, 1);
            foreach (var selection in itemSelections)
            {
                var savedSelectionColor = TintMapColorResolver.GetSavedColor(selection, layer, includeGlobalColor: false);
                var hasExplicitPresetColor = TintMapColorResolver.HasExplicitItemPresetColor(
                    selection,
                    layer,
                    savedSelectionColor);
                if (!hasExplicitPresetColor && savedSelectionColor <= 0)
                {
                    _apply.ApplyColor(
                        creature,
                        selection,
                        layer,
                        new TintMapColorSelection(TintMapColorResolver.GetStandardColor(creature, selection, layer), color));
                }
            }

            // The global marker is meaningful state even when every active material has an
            // independent override and the loop does not update a material.
            _droid.UpdateEquippedItemSnapshot(creature, item);
            _apply.ApplyCurrentColorsAndPublish(creature);
        }

        public void ResetGlobalItemCustomColor(
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
            _apply.ApplyCurrentColorsAndPublish(creature);
        }

        private void ResetGlobalItemCustomColor(
            uint creature,
            uint item,
            IReadOnlyList<TintMapMaterialSelection> itemSelections,
            TintMapLayerType layer)
        {
            _tintMapState.MarkPendingItemColorEdit(item, layer, AppearanceArmor.Invalid);
            var stateVariable = TintMapVariable.GetItemGlobalColorStateName(layer);
            var inheritanceStateVariable = TintMapVariable.GetItemGlobalInheritanceStateName(layer);
            var usesExplicitInheritance = GetLocalInt(item, inheritanceStateVariable) != 0;
            var hasGlobalColor = TintMapColor.TryFromStoredValue(
                GetLocalInt(item, stateVariable),
                out var globalColor);

            // Compatibility for items authored before the explicit global-intent marker existed:
            // only a complete, uniform set can safely be interpreted as a global custom color.
            if (!hasGlobalColor)
                hasGlobalColor = TryInferLegacyGlobalItemCustomColor(
                    item,
                    itemSelections,
                    layer,
                    out globalColor);

            DeleteLocalInt(item, stateVariable);
            DeleteLocalInt(item, inheritanceStateVariable);
            // Persist removal of the marker even when there is no inferred global color, or
            // when no active material still matches it closely enough to be reset below.
            _droid.UpdateEquippedItemSnapshot(creature, item);
            if (!hasGlobalColor)
                return;

            if (usesExplicitInheritance)
            {
                foreach (var selection in itemSelections)
                {
                    _apply.ApplyColor(
                        creature,
                        selection,
                        layer,
                        TintMapColorResolver.GetEffectiveColor(creature, selection, layer));
                }

                return;
            }

            var resetSelections = itemSelections
                .Where(selection =>
                    TintMapColorResolver.TryGetCustomColor(selection, layer, out var color) &&
                    color == globalColor)
                .ToList();
            var resetVariables = TintMapOverrideLocals.GetItemTintOverrides(item)
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

            _droid.UpdateEquippedItemSnapshot(creature, item);
        }

        private bool TryInferLegacyGlobalItemCustomColor(
            uint item,
            IReadOnlyList<TintMapMaterialSelection> itemSelections,
            TintMapLayerType layer,
            out TintMapColor globalColor)
        {
            globalColor = default;
            if (itemSelections.Select(selection => (selection.ArmorPart, selection.WeaponPart))
                    .Distinct().Take(2).Count() < 2)
            {
                // Empty sets, one part, and multiple profiles of the same robe cannot
                // distinguish an independently authored part color from global intent.
                return false;
            }

            var customColors = itemSelections
                .Select(selection =>
                {
                    var savedColor = GetLocalInt(
                        selection.GetPaletteSource(layer),
                        TintMapVariable.GetName(selection.Material.Resref, layer));
                    return TintMapColor.TryFromStoredValue(savedColor, out var color)
                        ? (TintMapColor?)color
                        : null;
                })
                .ToList();
            var distinctColors = customColors
                .Where(color => color.HasValue)
                .Select(color => color!.Value)
                .Distinct()
                .ToList();
            if (!customColors.All(color => color.HasValue) || distinctColors.Count != 1)
                return false;

            globalColor = distinctColors[0];
            return true;
        }

        /// <summary>
        /// Clears persisted tint state when the selected armor part no longer exposes the chosen
        /// channel. There is no material selection for the normal reset path in that case, but the
        /// user's preset click still replaces the old custom color.
        /// </summary>
        public void ResetInactiveItemCustomColor(
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
                _apply.ApplyCurrentColorsAndPublish(creature);
                return;
            }

            _tintMapState.MarkPendingItemColorEdit(item, layer, armorPart);
            foreach (var variableName in TintMapOverrideLocals.GetItemTintOverrides(item).Keys)
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

            if (TintMapColorResolver.TryGetArmorColorChannel(layer, out var colorChannel))
            {
                DeleteLocalInt(
                    item,
                    ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                        armorPart,
                        colorChannel));
            }

            _droid.UpdateEquippedItemSnapshot(creature, item);
            _apply.ApplyCurrentColorsAndPublish(creature);
        }
    }
}

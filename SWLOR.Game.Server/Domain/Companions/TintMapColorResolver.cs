using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// The colour a creature, an item part or a worn material shows: the saved value, the standard palette
    /// colour and the effective colour built from them. Reads object locals and appearance only, so it needs no service.
    /// </summary>
    public static class TintMapColorResolver
    {
        public static TintMapColorSelection GetEffectiveColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var savedColor = GetSavedColor(selection, layer);

            if (savedColor == 0 && TintMapVariable.IsCreatureColorLayer(layer))
                return GetEffectiveCreatureColor(creature, layer);

            var standardColor = GetStandardColor(creature, selection, layer);
            if (TintMapColor.TryFromStoredValue(savedColor, out var customColor))
            {
                return new TintMapColorSelection(
                    TintMapPaletteColors.GetClosestColorId(layer, customColor),
                    customColor);
            }

            // Values 1-176 are the palette-index format used by the original tint-map branch.
            var paletteColor = savedColor > 0 && savedColor <= TintMapMaterialRegistry.PaletteColorCount
                ? savedColor - 1
                : standardColor;
            return new TintMapColorSelection(paletteColor, null);
        }

        public static TintMapColorSelection GetEffectiveCreatureColor(
            uint creature,
            TintMapLayerType layer)
        {
            if (TintMapColor.TryFromStoredValue(
                    GetLocalInt(creature, GetCreatureCustomColorStateVariable(layer)),
                    out var globalColor))
            {
                return new TintMapColorSelection(
                    TintMapPaletteColors.GetClosestColorId(layer, globalColor),
                    globalColor);
            }

            return new TintMapColorSelection(GetCreatureStandardColor(creature, layer), null);
        }

        public static TintMapColor GetEffectiveDisplayColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var effectiveColor = GetEffectiveColor(creature, selection, layer);
            return effectiveColor.CustomColor ??
                   TintMapPaletteColors.GetColor(layer, effectiveColor.PaletteColorId);
        }

        public static int GetSavedColor(
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            bool includeGlobalColor = true)
        {
            var paletteSource = selection.GetPaletteSource(layer);
            var savedColor = GetLocalInt(
                paletteSource,
                TintMapVariable.GetName(selection.Material.Resref, layer));
            if (savedColor > 0)
                return savedColor;

            // A worn cloak renders the texture selected by cloakmodel.2da, while its dropped
            // ground model retains the appearance-number material. Read the worn material's
            // same semantic layer so dropping the item does not discard its visible tint.
            if (!string.IsNullOrWhiteSpace(selection.OverrideModelResref))
            {
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
                var equivalentColors = TintMapOverrideLocals.GetItemTintOverrides(paletteSource)
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
            }

            if (includeGlobalColor &&
                !TintMapVariable.IsCreatureColorLayer(layer) &&
                GetObjectType(paletteSource) == ObjectType.Item &&
                !HasExplicitItemPresetColor(selection, layer, 0))
            {
                var globalColor = GetLocalInt(
                    paletteSource,
                    TintMapVariable.GetItemGlobalColorStateName(layer));
                if (TintMapColor.TryFromStoredValue(globalColor, out _))
                    return globalColor;
            }

            return 0;
        }

        public static int GetStandardColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            if (selection.UsesItemColor(layer))
            {
                if (selection.WeaponPart != AppearanceWeapon.Invalid)
                {
                    return Math.Clamp(
                        GetItemAppearance(
                            selection.PaletteSource,
                            ItemAppearanceType.WeaponColor,
                            (int)selection.WeaponPart),
                        0,
                        TintMapMaterialRegistry.PaletteColorCount - 1);
                }

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
                        if (selection.ArmorPart == AppearanceArmor.Robe)
                            perPartColor = GetNativePaletteBaseline(selection.PaletteSource, perPartIndex, perPartColor);
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
            var creatureColor = GetCreatureColorChannel(layer);

            return (int)creatureColor >= 0 && GetObjectType(creature) == ObjectType.Creature
                ? Math.Clamp(GetNativePaletteBaseline(creature, (int)layer, GetColor(creature, creatureColor)),
                    0, TintMapMaterialRegistry.PaletteColorCount - 1)
                : 0;
        }

        public static ColorChannel GetCreatureColorChannel(TintMapLayerType layer)
        {
            return layer switch
            {
                TintMapLayerType.Skin => ColorChannel.Skin,
                TintMapLayerType.Hair => ColorChannel.Hair,
                TintMapLayerType.Tattoo1 => ColorChannel.Tattoo1,
                TintMapLayerType.Tattoo2 => ColorChannel.Tattoo2,
                _ => (ColorChannel)(-1)
            };
        }

        public static int GetStandardColorId(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            return GetStandardColor(creature, selection, layer);
        }

        private static int GetNativePaletteBaseline(uint target, int channel, int nativeColor)
        {
            return TintMapNativePaletteProjection.GetBaseline(nativeColor,
                GetLocalInt(target, TintMapNativePaletteProjection.BaselineName(channel)),
                GetLocalInt(target, TintMapNativePaletteProjection.LastAppliedName(channel)));
        }

        public static bool TryGetCustomColor(
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            out TintMapColor color)
        {
            var savedColor = GetSavedColor(selection, layer);
            return TintMapColor.TryFromStoredValue(savedColor, out color);
        }

        public static bool HasExplicitItemPresetColor(
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

        public static bool TryGetArmorColorChannel(
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

        public static List<string> GetEquivalentItemTintVariables(
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

            var variables = TintMapOverrideLocals.GetItemTintOverrides(item).Keys
                .Where(variableName =>
                    TintMapVariable.TryParse(
                        variableName,
                        out var materialResref,
                        out var variableLayer) &&
                    variableLayer == layer &&
                    AreEquipmentMaterialsEquivalent(materialResref, selection, layer))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (selection.ArmorPart == AppearanceArmor.Robe)
            {
                // The robe's retained render profiles share one native palette slot. Install an
                // edit on every current profile even if that alias had no previous custom local.
                variables.AddRange(TintMapMaterialRegistry.GetMaterials(selection.ModelResref)
                    .Where(material => material.Layers.Contains(layer))
                    .Select(material => TintMapVariable.GetName(material.Resref, layer)));
            }
            return variables.Distinct(StringComparer.Ordinal).ToList();
        }

        public static bool AreEquipmentMaterialsEquivalent(
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

        public static IReadOnlyList<string> GetCreatureCustomColorVariables(uint creature)
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

        public static string GetCreatureCustomColorStateVariable(TintMapLayerType layer)
        {
            return TintMapVariable.GetCreatureColorStateName(layer);
        }

        public static IEnumerable<string> GetCreatureTintSnapshotVariables(uint creature)
        {
            return GetCreatureCustomColorVariables(creature).Concat(
                Enum.GetValues<TintMapLayerType>()
                    .Where(TintMapVariable.IsCreatureColorLayer)
                    .Select(GetCreatureCustomColorStateVariable));
        }
    }
}

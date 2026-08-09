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
    public static class TintMapService
    {
        private const float RefreshDelaySeconds = 0.2f;

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
                QueueWorldItemsInArea(GetArea(creature));
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

            foreach (var selection in TintMapModelResolver.GetCurrentSelections(creature))
            {
                foreach (var layer in selection.Material.Layers)
                {
                    ApplyColor(creature, selection, layer, GetEffectiveColor(creature, selection, layer));
                }
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
            var savedColor = color.ToStoredValue();
            var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
            SetLocalInt(selection.GetPaletteSource(layer), variableName, savedColor);
            SaveDroidOverride(creature, selection, layer, variableName, savedColor);
            ApplyColor(
                creature,
                selection,
                layer,
                new TintMapColorSelection(GetStandardColor(creature, selection, layer), color));
        }

        public static void ResetColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
            DeleteLocalInt(selection.GetPaletteSource(layer), variableName);
            SaveDroidOverride(creature, selection, layer, variableName, 0);
            ApplyColor(
                creature,
                selection,
                layer,
                new TintMapColorSelection(GetStandardColor(creature, selection, layer), null));
        }

        public static bool TryGetCustomColor(
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            out TintMapColor color)
        {
            var savedColor = GetSavedColor(selection, layer);
            return TintMapColor.TryFromStoredValue(savedColor, out color);
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

        public static void QueueRefresh(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            DelayCommand(RefreshDelaySeconds, () => ApplyCurrentColors(creature));
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

                        // 255 is NWN's per-part "inherit global color" sentinel.
                        if (perPartColor != 255)
                            return Math.Clamp(perPartColor, 0, TintMapMaterialRegistry.PaletteColorCount - 1);
                    }

                    return Math.Clamp(
                        GetItemAppearance(selection.PaletteSource, ItemAppearanceType.ArmorColor, colorIndex),
                        0,
                        TintMapMaterialRegistry.PaletteColorCount - 1);
                }
            }

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

        private static void ApplyColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            TintMapColorSelection color)
        {
            var layerDefinition = TintMapMaterialRegistry.GetLayer(layer);
            var paletteCoordinate = TintMapMaterialRegistry.GetPaletteCoordinate(layer, color.PaletteColorId);
            SetMaterialShaderUniformVec4(
                creature,
                selection.Material.Resref,
                layerDefinition.UniformName,
                paletteCoordinate);
            var customColor = color.CustomColor;
            SetMaterialShaderUniformVec4(
                creature,
                selection.Material.Resref,
                layerDefinition.ColorUniformName,
                (customColor?.Red ?? 0) / 255f,
                (customColor?.Green ?? 0) / 255f,
                (customColor?.Blue ?? 0) / 255f,
                customColor.HasValue ? 1f : 0f);
        }

        private static Dictionary<string, int> GetItemTintOverrides(uint item)
        {
            var tintOverrides = new Dictionary<string, int>(StringComparer.Ordinal);
            var variableCount = ObjectPlugin.GetLocalVariableCount(item);
            for (var index = 0; index < variableCount; index++)
            {
                var variable = ObjectPlugin.GetLocalVariable(item, index);
                if (variable.Type != LocalVariableType.Int ||
                    !variable.Key.StartsWith(TintMapVariable.Prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                tintOverrides[variable.Key] = GetLocalInt(item, variable.Key);
            }

            return tintOverrides;
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

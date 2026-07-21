using System;
using System.Collections.Generic;
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
        private const string LocalVariablePrefix = "TM_";
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

        public static void SetColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            int colorId)
        {
            colorId = Math.Clamp(colorId, 0, TintMapMaterialRegistry.PaletteColorCount - 1);
            var variableName = GetLocalVariableName(selection.Material.Resref, layer);
            SetLocalInt(selection.PaletteSource, variableName, colorId + 1);
            SaveDroidOverride(creature, selection, variableName, colorId + 1);
            ApplyColor(creature, selection, layer, colorId);
        }

        public static void ResetColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var variableName = GetLocalVariableName(selection.Material.Resref, layer);
            DeleteLocalInt(selection.PaletteSource, variableName);
            SaveDroidOverride(creature, selection, variableName, 0);
            ApplyColor(creature, selection, layer, GetStandardColor(creature, selection, layer));
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

        public static void QueueRefreshAndEditor(uint creature, uint player)
        {
            QueueRefresh(creature);
            if (!GetIsObjectValid(player))
                return;

            DelayCommand(
                RefreshDelaySeconds,
                () => Gui.PublishRefreshEvent(player, new AppearanceChangedRefreshEvent()));
        }

        private static int GetEffectiveColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            var savedColor = GetLocalInt(
                selection.PaletteSource,
                GetLocalVariableName(selection.Material.Resref, layer));

            return savedColor > 0
                ? savedColor - 1
                : GetStandardColor(creature, selection, layer);
        }

        private static int GetStandardColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer)
        {
            if (selection.UsesItemColors)
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

            return (int)creatureColor >= 0
                ? Math.Clamp(GetColor(creature, creatureColor), 0, TintMapMaterialRegistry.PaletteColorCount - 1)
                : 0;
        }

        private static void ApplyColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            int colorId)
        {
            var layerDefinition = TintMapMaterialRegistry.GetLayer(layer);
            var paletteCoordinate = TintMapMaterialRegistry.GetPaletteCoordinate(layer, colorId);
            SetMaterialShaderUniformVec4(
                creature,
                selection.Material.Resref,
                layerDefinition.UniformName,
                paletteCoordinate);
        }

        private static Dictionary<string, int> GetItemTintOverrides(uint item)
        {
            var tintOverrides = new Dictionary<string, int>(StringComparer.Ordinal);
            var variableCount = ObjectPlugin.GetLocalVariableCount(item);
            for (var index = 0; index < variableCount; index++)
            {
                var variable = ObjectPlugin.GetLocalVariable(item, index);
                if (variable.Type != LocalVariableType.Int ||
                    !variable.Key.StartsWith(LocalVariablePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                tintOverrides[variable.Key] = GetLocalInt(item, variable.Key);
            }

            return tintOverrides;
        }

        private static string GetLocalVariableName(string materialResref, TintMapLayerType layer)
        {
            return $"{LocalVariablePrefix}{materialResref}_{(int)layer}";
        }

        private static void SaveDroidOverride(
            uint creature,
            TintMapMaterialSelection selection,
            string variableName,
            int savedColor)
        {
            if (!Droid.IsDroid(creature))
                return;

            if (selection.PaletteSource != creature)
            {
                Droid.UpdateEquippedItemSnapshot(creature, selection.PaletteSource);
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

using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    public class AppearanceColorService
    {
        private AppearanceTintEditorService AppearanceTintEditorService { get; }
        private ITintMapApplyService TintMapApply { get; }
        private ITintMapCreatureColorService TintMapCreatureColor { get; }
        private ITintMapItemColorService TintMapItemColor { get; }

        public AppearanceColorService(AppearanceTintEditorService appearanceTintEditorService, ITintMapApplyService tintMapApply, ITintMapCreatureColorService tintMapCreatureColor, ITintMapItemColorService tintMapItemColor)
        {
            AppearanceTintEditorService = appearanceTintEditorService;
            TintMapApply = tintMapApply;
            TintMapCreatureColor = tintMapCreatureColor;
            TintMapItemColor = tintMapItemColor;
        }

        public void SynchronizeCustomTintControlsToPaletteColor(IAppearanceEditorHost host, int colorId)
        {
            if (!AppearanceTintEditorService.TryGetSelectedTintLayer(host, out var layerType))
                return;

            var color = TintMapPaletteColors.GetColor(layerType, colorId);
            AppearanceTintEditorService.SetSelectedTintColor(host, new GuiColor(color.Red, color.Green, color.Blue));
        }

        public bool ApplySelectedPaletteColor(IAppearanceEditorHost host, int colorId, bool reloadEditor = true)
        {
            host.ToggleItemEquippedFlags();
            if (host.DoesNotHaveItemEquipped)
                return false;

            if (colorId < 0 || colorId >= TintMapMaterialRegistry.PaletteColorCount)
                return false;

            // A newer preset selection replaces any uncommitted text for this target.
            host.Session.HasTintComponentDraft = false;
            host.Session.TintComponentApply = null;
            host.Session.PendingPickerColor = null;
            host.Session.PendingPickerApply = null;

            if (host.IsEquipmentSelected && host.SelectedItemTypeIndex == 0)
                return ApplyArmorPaletteColor(host, colorId);

            if (!host.IsAppearanceSelected &&
                (!host.IsEquipmentSelected || (host.SelectedItemTypeIndex != 1 && host.SelectedItemTypeIndex != 2)))
            {
                return false;
            }

            ResetCurrentCustomTintOverrides(host);

            // Appearance - Skin, Hair, or Tattoo
            if (host.IsAppearanceSelected)
            {
                switch (host.SelectedColorCategoryIndex)
                {
                    case 0: // 0 = Skin
                        SetColor(host.Session.Target, ColorChannel.Skin, colorId);
                        break;
                    case 1: //  1 = Hair
                        SetColor(host.Session.Target, ColorChannel.Hair, colorId);
                        break;
                    case 2: // 2 = Tattoo 1
                        SetColor(host.Session.Target, ColorChannel.Tattoo1, colorId);
                        break;
                    case 3: // 3 = Tattoo 2
                        SetColor(host.Session.Target, ColorChannel.Tattoo2, colorId);
                        break;
                }
            }
            // Helmet/Cloak - Cloth 1, Cloth 2, Leather 1, Leather 2, Metal 1, Metal 2
            else if (host.IsEquipmentSelected && (host.SelectedItemTypeIndex == 1 || host.SelectedItemTypeIndex == 2))
            {
                switch (host.SelectedColorCategoryIndex)
                {
                    case 0: // 0 = Leather 1
                        ModifyHelmetCloakColor(host, AppearanceArmorColor.Leather1, colorId);
                        break;
                    case 1: // 1 = Leather 2
                        ModifyHelmetCloakColor(host, AppearanceArmorColor.Leather2, colorId);
                        break;
                    case 2: // 2 = Cloth 1
                        ModifyHelmetCloakColor(host, AppearanceArmorColor.Cloth1, colorId);
                        break;
                    case 3: // 3 = Cloth 2
                        ModifyHelmetCloakColor(host, AppearanceArmorColor.Cloth2, colorId);
                        break;
                    case 4: // 4 = Metal 1
                        ModifyHelmetCloakColor(host, AppearanceArmorColor.Metal1, colorId);
                        break;
                    case 5: // 5 = Metal 2
                        ModifyHelmetCloakColor(host, AppearanceArmorColor.Metal2, colorId);
                        break;
                }
            }

            TintMapApply.RefreshAfterColorChange(host.Session.Target);
            if (reloadEditor)
                AppearanceTintEditorService.LoadTintMapEditor(host);
            return true;
        }

        public void ResetCurrentCustomTintOverrides(IAppearanceEditorHost host, int? paletteColor = null)
        {
            if (!AppearanceTintEditorService.TryGetSelectedTintLayer(host, out var selectedLayerType))
                return;

            if (!AppearanceTintEditorService.TryGetEditableTintSelections(host, out var selections, out var layerType, out var layer) || selections.Count == 0)
            {
                // A model can stop exposing a semantic channel (for example, a hairless
                // head). Selecting a preset still means the persisted custom channel is
                // being replaced, even though there is no current material to reset.
                if (host.IsAppearanceSelected && TintMapVariable.IsCreatureColorLayer(selectedLayerType))
                {
                    TintMapCreatureColor.ResetCreatureCustomColor(host.Session.Target, selectedLayerType);
                    AppearanceTintEditorService.LoadTintMapEditor(host);
                }
                else if (host.IsEquipmentSelected && host.SelectedItemTypeIndex is >= 0 and <= 2)
                {
                    var item = host.GetItem();
                    if (GetIsObjectValid(item))
                    {
                        TintMapItemColor.ResetInactiveItemCustomColor(
                            host.Session.Target,
                            item,
                            selectedLayerType,
                            host.SelectedItemTypeIndex != 0 || host.Session.ColorTarget == AppearanceColorTarget.Global
                                ? AppearanceArmor.Invalid
                                : AppearancePartEditService.GetArmorModelType(host, host.Session.ColorTarget));
                        AppearanceTintEditorService.LoadTintMapEditor(host);
                    }
                }

                return;
            }

            if (host.IsEquipmentSelected &&
                (host.SelectedItemTypeIndex != 0 || host.Session.ColorTarget == AppearanceColorTarget.Global))
            {
                if (selections.Count == 0)
                    TintMapItemColor.ResetInactiveItemCustomColor(host.Session.Target, host.GetItem(), layerType, AppearanceArmor.Invalid);
                else
                    TintMapItemColor.ResetGlobalItemCustomColor(host.Session.Target, selections, layerType);
                AppearanceTintEditorService.LoadTintMapEditor(host);
            }
            else
            {
                ResetCustomTintOverrides(host, selections, layerType, layer, paletteColor);
            }
        }

        public void ResetCustomTintOverrides(IAppearanceEditorHost host, 
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layerType,
            TintMapLayerDefinition layer,
            int? paletteColor = null)
        {
            if (TintMapVariable.IsCreatureColorLayer(layerType))
            {
                TintMapCreatureColor.ResetCreatureCustomColor(host.Session.Target, layerType);
            }
            else
            {
                foreach (var selection in selections)
                {
                    TintMapItemColor.ResetColor(host.Session.Target, selection, layerType, paletteColor);
                }
            }

            AppearanceTintEditorService.LoadTintMapEditor(host);
        }

        public static void ModifyHelmetCloakColor(IAppearanceEditorHost host, AppearanceArmorColor colorChannel, int colorId)
        {
            host.ToggleItemEquippedFlags();
            if (host.DoesNotHaveItemEquipped)
                return;

            var item = host.GetItem();
            SetItemColorInPlace(host, item, (int)colorChannel, colorId);
        }

        public static void SetItemColorInPlace(IAppearanceEditorHost host, uint item, int colorIndex, int colorId)
        {
            if (!GetIsObjectValid(item))
                return;

            EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorColor, colorIndex, colorId);
            EquippedItemAppearance.Refresh(host.Session.Target, item, resetShaderOverrides: false);
        }

        public bool ApplyArmorPaletteColor(IAppearanceEditorHost host, int colorId)
        {
            if (host.Session.ColorTarget == AppearanceColorTarget.Invalid)
                return false;

            ResetCurrentCustomTintOverrides(host, colorId);
            var item = host.GetItem();
            if (!GetIsObjectValid(item))
                return false;

            int colorIndex;

            if (host.Session.ColorTarget == AppearanceColorTarget.Global)
            {
                colorIndex = (int)host.Session.SelectedColorChannel;
            }
            else
            {
                var armorModel = AppearancePartEditService.GetArmorModelType(host, host.Session.ColorTarget);
                colorIndex = ArmorColorIndexCalculator.CalculatePerPart(armorModel, host.Session.SelectedColorChannel);
                SetLocalInt(
                    item,
                    ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                        armorModel,
                        host.Session.SelectedColorChannel),
                    1);
            }

            SetItemColorInPlace(host, item, colorIndex, colorId);
            host.ChangeColor(host.Session.ColorTarget, host.Session.SelectedColorChannel, colorId);
            return true;
        }

        public void ResetArmorColorToInheritance(IAppearanceEditorHost host, AppearanceColorTarget colorTarget, AppearanceArmorColor colorChannel)
        {
            // Settle the previous edit before clearing this part. Reloading afterward
            // must not replay a draft over the user's newer inheritance reset.
            AppearanceTintEditorService.FlushPendingPickerColor(host);
            AppearanceTintEditorService.CommitCustomTintComponents(host);
            ResetCustomTintOverrides(host, colorTarget, colorChannel);

            var item = host.GetItem();
            var armorModel = AppearancePartEditService.GetArmorModelType(host, colorTarget);
            var index = ArmorColorIndexCalculator.CalculatePerPart(armorModel, colorChannel);
            DeleteLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(armorModel, colorChannel));

            SetItemColorInPlace(host, item, index, 255);
            host.ChangeColor(colorTarget, colorChannel, 255);
            if (host.Session.ColorTarget == colorTarget && host.Session.SelectedColorChannel == colorChannel)
                AppearanceTintEditorService.LoadTintMapEditor(host);
        }

        public void ResetCustomTintOverrides(IAppearanceEditorHost host, 
            AppearanceColorTarget colorTarget,
            AppearanceArmorColor colorChannel)
        {
            if (!TryGetArmorTintLayer(colorChannel, out var layerType))
                return;

            var item = host.GetItem();
            if (!GetIsObjectValid(item))
                return;


            var armorPart = colorTarget == AppearanceColorTarget.Global
                ? AppearanceArmor.Invalid
                : AppearancePartEditService.GetArmorModelType(host, colorTarget);
            var selections = host.Session.TintMapSelections
                .Where(selection =>
                    selection.GetPaletteSource(layerType) == item &&
                    selection.Material.Layers.Contains(layerType) &&
                    (armorPart == AppearanceArmor.Invalid || selection.ArmorPart == armorPart))
                .ToList();
            if (selections.Count == 0)
            {
                TintMapItemColor.ResetInactiveItemCustomColor(
                    host.Session.Target,
                    item,
                    layerType,
                    armorPart);
                return;
            }

            foreach (var selection in selections)
            {
                TintMapItemColor.ResetColorToInheritance(host.Session.Target, selection, layerType);
            }
        }

        public static bool TryGetArmorTintLayer(
            AppearanceArmorColor colorChannel,
            out TintMapLayerType layerType)
        {
            layerType = colorChannel switch
            {
                AppearanceArmorColor.Leather1 => TintMapLayerType.Leather1,
                AppearanceArmorColor.Leather2 => TintMapLayerType.Leather2,
                AppearanceArmorColor.Cloth1 => TintMapLayerType.Cloth1,
                AppearanceArmorColor.Cloth2 => TintMapLayerType.Cloth2,
                AppearanceArmorColor.Metal1 => TintMapLayerType.Metal1,
                AppearanceArmorColor.Metal2 => TintMapLayerType.Metal2,
                _ => default
            };
            return colorChannel is >= AppearanceArmorColor.Leather1 and <= AppearanceArmorColor.Metal2;
        }

        public static int GetArmorSwatchColor(IAppearanceEditorHost host, 
            uint item,
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            var perPartColorIndex = ArmorColorIndexCalculator.CalculatePerPart(armorPart, colorChannel);
            var perPartColor = GetItemAppearance(
                item,
                ItemAppearanceType.ArmorColor,
                perPartColorIndex);
            var hasExplicitOverride = GetLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    armorPart,
                    colorChannel)) > 0;
            var baseline = armorPart == AppearanceArmor.Robe
                ? GetLocalInt(item, TintMapNativePaletteProjection.BaselineName(perPartColorIndex)) : 0;
            var lastApplied = armorPart == AppearanceArmor.Robe
                ? GetLocalInt(item, TintMapNativePaletteProjection.LastAppliedName(perPartColorIndex)) : 0;
            int? materialColor = null;
            if (TryGetArmorTintLayer(colorChannel, out var layer))
            {
                foreach (var selection in host.Session.TintMapSelections.Where(selection =>
                             selection.ArmorPart == armorPart && selection.GetPaletteSource(layer) == item &&
                             selection.Material.Layers.Contains(layer)))
                {
                    var savedColor = GetLocalInt(item, TintMapVariable.GetName(selection.Material.Resref, layer));
                    if (TintMapColor.TryFromStoredValue(savedColor, out var color))
                        materialColor = TintMapPaletteColors.GetClosestColorId(layer, color);
                    else if (savedColor is > 0 and <= TintMapMaterialRegistry.PaletteColorCount)
                        materialColor = savedColor - 1;
                    if (materialColor.HasValue)
                        break;
                }
            }
            return ResolveArmorSwatchColorId(perPartColor, hasExplicitOverride, baseline, lastApplied, materialColor);
        }

        public static int ResolveArmorSwatchColorId(
            int nativeColor, bool hasExplicitOverride, int baseline, int lastApplied, int? materialColor)
        {
            if (materialColor.HasValue)
                return materialColor.Value;
            var authoredColor = TintMapNativePaletteProjection.GetBaseline(nativeColor, baseline, lastApplied);
            return ArmorColorIndexCalculator.ShouldUsePerPartColor(authoredColor, hasExplicitOverride)
                ? authoredColor : 255;
        }

        public static void MarkPerPartColorOverride(
            uint item,
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            SetLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    armorPart,
                    colorChannel),
                1);
        }

        public static void ClearPerPartColorOverride(
            uint item,
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            DeleteLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    armorPart,
                    colorChannel));
        }

        public static void CopyColors(IAppearanceEditorHost host, uint item, AppearanceColorTarget copyToTarget, AppearanceColorTarget copyFromTarget)
        {
            var copyFrom = AppearancePartEditService.GetArmorModelType(host, copyFromTarget);
            var copyTo = AppearancePartEditService.GetArmorModelType(host, copyToTarget);

            foreach (var colorChannel in new[]
                     {
                         AppearanceArmorColor.Cloth1,
                         AppearanceArmorColor.Cloth2,
                         AppearanceArmorColor.Leather1,
                         AppearanceArmorColor.Leather2,
                         AppearanceArmorColor.Metal1,
                         AppearanceArmorColor.Metal2
                     })
            {
                CopyColor(host, item, copyToTarget, copyFrom, copyTo, colorChannel);
            }

            TintMapModelResolver.CopyArmorPartTintOverrides(
                host.Session.Target,
                item,
                copyFrom,
                copyTo);
        }

        public static void CopyColor(IAppearanceEditorHost host, 
            uint item,
            AppearanceColorTarget copyToTarget,
            AppearanceArmor copyFrom,
            AppearanceArmor copyTo,
            AppearanceArmorColor colorChannel)
        {
            var copyFromIndex = ArmorColorIndexCalculator.CalculatePerPart(copyFrom, colorChannel);
            var sourceColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, copyFromIndex);
            var sourceHasExplicitOverride = GetLocalInt(
                item,
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(copyFrom, colorChannel)) > 0;
            var sourceUsesPerPartColor = ArmorColorIndexCalculator.ShouldUsePerPartColor(
                sourceColor,
                sourceHasExplicitOverride);
            var copyToIndex = ArmorColorIndexCalculator.CalculatePerPart(copyTo, colorChannel);

            host.ChangeColor(copyToTarget, colorChannel, sourceUsesPerPartColor ? sourceColor : 255);
            EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorColor,
                copyToIndex, sourceUsesPerPartColor ? sourceColor : 255);
            if (sourceUsesPerPartColor)
                MarkPerPartColorOverride(item, copyTo, colorChannel);
            else
                ClearPerPartColorOverride(item, copyTo, colorChannel);
        }

        public void CopyArmorSide(IAppearanceEditorHost host, bool copyToRight)
        {
            AppearanceTintEditorService.FlushPendingPickerColor(host);
            AppearanceTintEditorService.CommitCustomTintComponents(host);
            host.ToggleItemEquippedFlags();
            if (host.DoesNotHaveItemEquipped || host.SelectedItemTypeIndex != 0)
                return;

            var item = host.GetItem();
            host.Session.SkipAdjustArmorPart = true;
            host.IsCopyEnabled = false;
            try
            {
                foreach (var (left, right) in new[]
                         {
                             (AppearanceColorTarget.LeftShoulder, AppearanceColorTarget.RightShoulder),
                             (AppearanceColorTarget.LeftBicep, AppearanceColorTarget.RightBicep),
                             (AppearanceColorTarget.LeftForearm, AppearanceColorTarget.RightForearm),
                             (AppearanceColorTarget.LeftHand, AppearanceColorTarget.RightHand),
                             (AppearanceColorTarget.LeftThigh, AppearanceColorTarget.RightThigh),
                             (AppearanceColorTarget.LeftShin, AppearanceColorTarget.RightShin),
                             (AppearanceColorTarget.LeftFoot, AppearanceColorTarget.RightFoot)
                         })
                {
                    var source = copyToRight ? left : right;
                    var destination = copyToRight ? right : left;
                    CopyColors(host, item, destination, source);
                    EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorModel,
                        (int)AppearancePartEditService.GetArmorModelType(host, destination),
                        GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearancePartEditService.GetArmorModelType(host, source)));
                }

                EquippedItemAppearance.Refresh(host.Session.Target, item);
                host.LoadItemParts();
            }
            finally
            {
                host.Session.SkipAdjustArmorPart = false;
                host.IsCopyEnabled = true;
            }
        }
    }
}

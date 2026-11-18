using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Preview;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    /// <summary>
    /// The colour indicators: the swatch under each armor channel, the part swatches, the body swatches, the palette outline and
    /// the target text, kept in step with the item or creature the editor is about.
    /// </summary>
    public class AppearanceSwatchService
    {
        private AppearanceColorService AppearanceColorService { get; }
        private AppearancePartEditService AppearancePartEditService { get; }
        private AppearanceTintEditorService AppearanceTintEditorService { get; }

        public AppearanceSwatchService(AppearanceColorService appearanceColorService, AppearancePartEditService appearancePartEditService, AppearanceTintEditorService appearanceTintEditorService)
        {
            AppearanceColorService = appearanceColorService;
            AppearancePartEditService = appearancePartEditService;
            AppearanceTintEditorService = appearanceTintEditorService;
        }

        /// <summary>The six armor material channels in the order the part swatch row shows them.</summary>
        public readonly AppearanceArmorColor[] SwatchChannels =
        {
            AppearanceArmorColor.Leather1, AppearanceArmorColor.Cloth1, AppearanceArmorColor.Metal1,
            AppearanceArmorColor.Leather2, AppearanceArmorColor.Cloth2, AppearanceArmorColor.Metal2
        };

        public readonly (string Name, TintMapLayerType Layer, ColorChannel Channel)[] BodyColorSwatches =
        {
            ("Skin", TintMapLayerType.Skin, ColorChannel.Skin),
            ("Hair", TintMapLayerType.Hair, ColorChannel.Hair),
            ("Tattoo1", TintMapLayerType.Tattoo1, ColorChannel.Tattoo1),
            ("Tattoo2", TintMapLayerType.Tattoo2, ColorChannel.Tattoo2)
        };

        public static AppearanceColorTarget ToColorTarget(AppearanceEditorSlot slot) =>
            Enum.TryParse<AppearanceColorTarget>(slot.ToString(), out var target) ? target : AppearanceColorTarget.Global;

        /// <summary>
        /// The code over a swatch: its palette code (P4), "RGB" for a custom colour, or nothing when
        /// it has no colour of its own (a part using the outfit colour).
        /// </summary>
        public static string SwatchCode(GuiRectangle region, bool custom)
        {
            if (custom)
                return "RGB";
            if (region == null || region.Width < ArmorColorRegionCatalog.ColorSize)
                return string.Empty;
            return AppearancePreview.PaletteCode((int)(region.Y / ArmorColorRegionCatalog.ColorSize) * ArmorColorRegionCatalog.TextureColorsPerRow + (int)(region.X / ArmorColorRegionCatalog.ColorSize));
        }

        public void UpdateColorSwatch(IAppearanceEditorHost host, AppearanceColorTarget target, AppearanceArmorColor channel, string regionName)
        {
            var name = regionName[..^"Region".Length];
            var item = host.GetItem();
            AppearanceColorService.TryGetArmorTintLayer(channel, out var layer);
            var colorId = target == AppearanceColorTarget.Global
                ? GetItemAppearance(item, ItemAppearanceType.ArmorColor, (int)channel)
                : AppearanceColorService.GetArmorSwatchColor(host, item, AppearancePartEditService.GetArmorModelType(host, target), channel);
            var custom = default(TintMapColor);
            var hasCustom = target == AppearanceColorTarget.Global
                ? TintMapColor.TryFromStoredValue(GetLocalInt(item,
                    TintMapVariable.GetItemGlobalColorStateName(layer)), out custom)
                : colorId != 255 && host.Session.TintMapSelections.Any(selection =>
                    selection.ArmorPart == AppearancePartEditService.GetArmorModelType(host, target) &&
                    selection.GetPaletteSource(layer) == item && selection.Material.Layers.Contains(layer) &&
                    TintMapColorResolver.TryGetCustomColor(selection, layer, out custom));
            var color = hasCustom ? custom : colorId == 255 ? default
                : TintMapPaletteColors.GetColor(layer, colorId);
            var previous = host.Get<GuiColor>(name + "Tint");
            if (previous == null || previous.R != color.Red || previous.G != color.Green || previous.B != color.Blue)
                host.Set(new GuiColor(color.Red, color.Green, color.Blue), name + "Tint");
            if (previous == null || host.Get<bool>(name + "Custom") != hasCustom)
                host.Set(hasCustom, name + "Custom");
            OnSwatchUpdated(host, target, channel);
        }

        public void InitializeColorSelection(IAppearanceEditorHost host)
        {
            host.Session.ActiveColorSelection = null;
            foreach (var swatch in ArmorColorRegionCatalog.Entries)
                host.Set(false, swatch.SelectedName);
            UpdateColorSelection(host);
        }

        public void UpdateColorSelection(IAppearanceEditorHost host)
        {
            var selected = host.Session.ColorMappingsRegistered &&
                           ArmorColorRegionCatalog.TryGet(host.Session.ColorTarget, host.Session.SelectedColorChannel, out var swatch)
                ? swatch.SelectedName
                : null;
            if (selected == host.Session.ActiveColorSelection)
            {
                SyncSelectedPartSelection(host);
                return;
            }

            if (host.Session.ActiveColorSelection != null)
                host.Set(false, host.Session.ActiveColorSelection);
            host.Session.ActiveColorSelection = selected;
            if (selected != null)
                host.Set(true, selected);
            SyncSelectedPartSelection(host);
        }

        public void ChangeColor(IAppearanceEditorHost host, AppearanceColorTarget target, AppearanceArmorColor channel, int colorId)
        {
            var swatch = ArmorColorRegionCatalog.Get(target, channel);
            host.Set(ArmorColorRegionCatalog.BuildPaletteRegion(colorId), swatch.RegionName);
            UpdateColorSwatch(host, target, channel, swatch.RegionName);
            if (target == host.Session.ColorTarget && channel == host.Session.SelectedColorChannel)
                UpdateTargetedColor(host);
        }

        private void OnSwatchUpdated(IAppearanceEditorHost host, AppearanceColorTarget target, AppearanceArmorColor channel)
        {
            if (target != AppearanceColorTarget.Global && target == ToColorTarget(host.Session.SelectedSlot))
                MirrorPartSwatch(host, channel);
            RefreshPaletteSelection(host);
        }

        public void SyncSelectedPartSwatches(IAppearanceEditorHost host)
        {
            var visible = host.IsEquipmentSelected && host.SelectedItemTypeIndex == 0 && ToColorTarget(host.Session.SelectedSlot) != AppearanceColorTarget.Global;
            host.SetIfChanged(nameof(AppearanceEditorViewModel.IsPartColorsVisible), visible);
            if (!visible)
                return;
            foreach (var channel in SwatchChannels)
                MirrorPartSwatch(host, channel);
            SyncSelectedPartSelection(host);
        }

        private void MirrorPartSwatch(IAppearanceEditorHost host, AppearanceArmorColor channel)
        {
            var source = $"{ToColorTarget(host.Session.SelectedSlot)}{channel}";
            var region = host.Get<GuiRectangle>(source + "Region") ?? ArmorColorRegionCatalog.BuildPaletteRegion(255);
            var custom = host.Get<bool>(source + "Custom");
            host.Set(region, $"SelectedPart{channel}Region");
            host.Set(host.Get<GuiColor>(source + "Tint") ?? new GuiColor(0, 0, 0), $"SelectedPart{channel}Tint");
            host.SetIfChanged($"SelectedPart{channel}Custom", custom);
            host.SetIfChanged($"SelectedPart{channel}Code", SwatchCode(region, custom));
        }

        public void SyncSelectedPartSelection(IAppearanceEditorHost host)
        {
            var partTarget = ToColorTarget(host.Session.SelectedSlot);
            foreach (var channel in SwatchChannels)
                host.SetIfChanged($"SelectedPart{channel}Selected", host.Session.ColorTarget == partTarget && host.Session.SelectedColorChannel == channel);
            RefreshPaletteSelection(host);
        }

        public void RefreshBodySwatches(IAppearanceEditorHost host)
        {
            if (!host.IsAppearanceSelected)
                return;
            for (var index = 0; index < BodyColorSwatches.Length; index++)
            {
                var (name, layer, channel) = BodyColorSwatches[index];
                var colorId = GetColor(host.Session.Target, channel);
                var hasCustom = TintMapColor.TryFromStoredValue(
                    GetLocalInt(host.Session.Target, TintMapVariable.GetCreatureColorStateName(layer)), out var custom);
                var color = hasCustom ? custom : TintMapPaletteColors.GetColor(layer, colorId);
                var region = ArmorColorRegionCatalog.BuildPaletteRegion(colorId);
                host.SetIfChanged($"Body{name}Region", region);
                host.Set(new GuiColor(color.Red, color.Green, color.Blue), $"Body{name}Tint");
                host.SetIfChanged($"Body{name}Custom", hasCustom);
                host.SetIfChanged($"Body{name}Selected", host.SelectedColorCategoryIndex == index);
            }
            RefreshPaletteSelection(host);
        }

        public void RefreshItemBaseSwatches(IAppearanceEditorHost host)
        {
            // Helmets and cloaks only use the six item channels.
            if (!host.IsEquipmentSelected || host.SelectedItemTypeIndex is not (1 or 2) || host.DoesNotHaveItemEquipped)
                return;
            foreach (var channel in SwatchChannels)
            {
                var name = $"Global{channel}";
                host.Set(BuildColorRegion(host, AppearanceColorTarget.Global, channel), name + "Region");
                UpdateColorSwatch(host, AppearanceColorTarget.Global, channel, name + "Region");
            }
        }

        public void ClickBaseSwatch(IAppearanceEditorHost host, AppearanceArmorColor channel)
        {
            if (host.SelectedItemTypeIndex is 1 or 2)
            {
                // Helmet and cloak channels are the colour categories, in LoadColorCategoryOptions order.
                var category = channel switch
                {
                    AppearanceArmorColor.Leather1 => 0,
                    AppearanceArmorColor.Leather2 => 1,
                    AppearanceArmorColor.Cloth1 => 2,
                    AppearanceArmorColor.Cloth2 => 3,
                    AppearanceArmorColor.Metal1 => 4,
                    _ => 5
                };
                if (host.ColorCategorySelected != null && host.SelectedColorCategoryIndex < host.ColorCategorySelected.Count)
                    host.ColorCategorySelected[host.SelectedColorCategoryIndex] = false;
                host.SelectedColorCategoryIndex = category;
                if (host.ColorCategorySelected != null && category < host.ColorCategorySelected.Count)
                    host.ColorCategorySelected[category] = true;
                host.Session.ColorTarget = AppearanceColorTarget.Global;
                host.Session.SelectedColorChannel = channel;
                UpdateColorSelection(host);
                AppearanceTintEditorService.LoadTintMapEditor(host);
                return;
            }
            ClickColorTarget(host, AppearanceColorTarget.Global, channel);
        }

        public void ClickBodySwatch(IAppearanceEditorHost host, int index)
        {
            if (!host.IsAppearanceSelected || index < 0 || index >= BodyColorSwatches.Length)
                return;
            if (host.ColorCategorySelected != null && host.SelectedColorCategoryIndex < host.ColorCategorySelected.Count)
                host.ColorCategorySelected[host.SelectedColorCategoryIndex] = false;
            host.SelectedColorCategoryIndex = index;
            if (host.ColorCategorySelected != null && index < host.ColorCategorySelected.Count)
                host.ColorCategorySelected[index] = true;
            AppearanceTintEditorService.LoadTintMapEditor(host);
            RefreshBodySwatches(host);
        }

        public void UpdateColorSheet(IAppearanceEditorHost host, int value)
        {
            if (host.IsAppearanceSelected)
            {
                if (value == 0) // 0 = Skin Color
                {
                    host.ColorSheetResref = "gui_pal_skin";
                }
                else if (value == 1) // 1 = Hair Color
                {
                    host.ColorSheetResref = "gui_pal_hair01";
                }
                else if (value == 2) // 2 = Tattoo Color 1
                {
                    host.ColorSheetResref = "gui_pal_tattoo";
                }
                else if (value == 3) // 3 = Tattoo Color 2
                {
                    host.ColorSheetResref = "gui_pal_tattoo";
                }
            }
            else if (host.IsEquipmentSelected)
            {
                if (value == 0) // 0 = Leather 1
                {
                    host.ColorSheetResref = "gui_pal_tattoo";
                }
                else if (value == 1) // 1 = Leather 2
                {
                    host.ColorSheetResref = "gui_pal_tattoo";
                }
                else if (value == 2) // 2 = Cloth 1
                {
                    host.ColorSheetResref = "gui_pal_tattoo";
                }
                else if (value == 3) // 3 = Cloth 2
                {
                    host.ColorSheetResref = "gui_pal_tattoo";
                }
                else if (value == 4) // 4 = Metal 1
                {
                    host.ColorSheetResref = "gui_pal_armor01";
                }
                else if (value == 5) // 5 = Metal 2
                {
                    host.ColorSheetResref = "gui_pal_armor01";
                }
            }
        }

        public void UpdatePaletteSheetForChannel(IAppearanceEditorHost host)
        {
            if (!host.IsEquipmentSelected || host.SelectedItemTypeIndex != 0)
                return;
            host.ColorSheetResref = host.Session.SelectedColorChannel is AppearanceArmorColor.Metal1 or AppearanceArmorColor.Metal2
                ? "gui_pal_armor01"
                : "gui_pal_tattoo";
        }

        private void RefreshPaletteSelection(IAppearanceEditorHost host)
        {
            string source = null;
            if (host.IsAppearanceSelected && host.SelectedColorCategoryIndex >= 0 && host.SelectedColorCategoryIndex < BodyColorSwatches.Length)
                source = $"Body{BodyColorSwatches[host.SelectedColorCategoryIndex].Name}";
            else if (host.IsEquipmentSelected && host.SelectedItemTypeIndex is 0 or 1 or 2)
                source = $"{(host.SelectedItemTypeIndex == 0 ? host.Session.ColorTarget : AppearanceColorTarget.Global)}{host.Session.SelectedColorChannel}";

            var region = source == null ? null : host.Get<GuiRectangle>(source + "Region");
            var custom = source != null && host.Get<bool>(source + "Custom");
            float[] points = Array.Empty<float>();
            if (region != null && !custom && region.Width >= ArmorColorRegionCatalog.ColorSize)
            {
                var column = (int)(region.X / ArmorColorRegionCatalog.ColorSize);
                var row = (int)(region.Y / ArmorColorRegionCatalog.ColorSize);
                var left = AppearanceEditorViewModel.PaletteHeaderWidth + column * AppearanceEditorViewModel.PaletteCellWidth + 1f;
                var top = AppearanceEditorViewModel.PaletteHeaderHeight + row * AppearanceEditorViewModel.PaletteCellHeight + 1f;
                var right = left + AppearanceEditorViewModel.PaletteCellWidth - 2f;
                var bottom = top + AppearanceEditorViewModel.PaletteCellHeight - 2f;
                points = new[] { left, top, right, top, right, bottom, left, bottom, left, top };
            }

            if (!points.SequenceEqual(host.Session.PaletteOutline))
            {
                host.Session.PaletteOutline = points;
                var list = new GuiBindingList<float>();
                foreach (var value in points)
                    list.Add(value);
                host.PaletteSelectionPoints = list;
            }
            host.SetIfChanged(nameof(AppearanceEditorViewModel.IsPaletteSelectionVisible), points.Length > 0);
        }

        public GuiRectangle BuildColorRegion(IAppearanceEditorHost host, AppearanceColorTarget target, AppearanceArmorColor colorChannel)
        {
            var item = host.GetItem();
            int colorId;

            if (target == AppearanceColorTarget.Global)
            {
                colorId = GetItemAppearance(item, ItemAppearanceType.ArmorColor, (int)colorChannel);
            }
            else
            {
                var armorModel = AppearancePartEditService.GetArmorModelType(host, target);
                colorId = AppearanceColorService.GetArmorSwatchColor(host, item, armorModel, colorChannel);
            }
            return ArmorColorRegionCatalog.BuildPaletteRegion(colorId);
        }

        public void UpdateAllColors(IAppearanceEditorHost host)
        {
            AppearanceTintEditorService.RefreshTintMapAvailability(host);
            foreach (var swatch in ArmorColorRegionCatalog.Entries)
            {
                host.Set(BuildColorRegion(host, swatch.Target, swatch.Channel), swatch.RegionName);
                UpdateColorSwatch(host, swatch.Target, swatch.Channel, swatch.RegionName);
            }
        }

        public void UpdateEditedColorSwatches(IAppearanceEditorHost host)
        {
            // Only this material channel changed. Inherited parts remain empty;
            // legacy global overrides may have been normalized by the service.
            foreach (var target in ArmorColorRegionCatalog.Targets)
            {
                if (host.Session.ColorTarget != AppearanceColorTarget.Global && target != host.Session.ColorTarget)
                    continue;
                var swatch = ArmorColorRegionCatalog.Get(target, host.Session.SelectedColorChannel);
                var region = BuildColorRegion(host, target, host.Session.SelectedColorChannel);
                var previous = host.Get<GuiRectangle>(swatch.RegionName);
                if (previous == null || previous.X != region.X || previous.Y != region.Y ||
                    previous.Width != region.Width || previous.Height != region.Height)
                    host.Set(region, swatch.RegionName);
                UpdateColorSwatch(host, target, host.Session.SelectedColorChannel, swatch.RegionName);
            }
        }

        public void UpdateTargetedColor(IAppearanceEditorHost host)
        {
            UpdateColorSelection(host);
            string targetName;
            string channelName;

            switch (host.Session.ColorTarget)
            {
                case AppearanceColorTarget.Global:
                    targetName = "Global";
                    break;
                case AppearanceColorTarget.LeftShoulder:
                    targetName = "Left Shoulder";
                    break;
                case AppearanceColorTarget.LeftBicep:
                    targetName = "Left Bicep";
                    break;
                case AppearanceColorTarget.LeftForearm:
                    targetName = "Left Forearm";
                    break;
                case AppearanceColorTarget.LeftHand:
                    targetName = "Left Hand";
                    break;
                case AppearanceColorTarget.LeftThigh:
                    targetName = "Left Thigh";
                    break;
                case AppearanceColorTarget.LeftShin:
                    targetName = "Left Shin";
                    break;
                case AppearanceColorTarget.LeftFoot:
                    targetName = "Left Foot";
                    break;
                case AppearanceColorTarget.RightShoulder:
                    targetName = "Right Shoulder";
                    break;
                case AppearanceColorTarget.RightBicep:
                    targetName = "Right Bicep";
                    break;
                case AppearanceColorTarget.RightForearm:
                    targetName = "Right Forearm";
                    break;
                case AppearanceColorTarget.RightHand:
                    targetName = "Right Hand";
                    break;
                case AppearanceColorTarget.RightThigh:
                    targetName = "Right Thigh";
                    break;
                case AppearanceColorTarget.RightShin:
                    targetName = "Right Shin";
                    break;
                case AppearanceColorTarget.RightFoot:
                    targetName = "Right Foot";
                    break;
                case AppearanceColorTarget.Neck:
                    targetName = "Neck";
                    break;
                case AppearanceColorTarget.Chest:
                    targetName = "Chest";
                    break;
                case AppearanceColorTarget.Belt:
                    targetName = "Belt";
                    break;
                case AppearanceColorTarget.Pelvis:
                    targetName = "Pelvis";
                    break;
                case AppearanceColorTarget.Robe:
                    targetName = "Robe";
                    break;
                default:
                    targetName = "Unknown";
                    break;
            }

            switch (host.Session.SelectedColorChannel)
            {
                case AppearanceArmorColor.Leather1:
                    channelName = "Leather 1";
                    break;
                case AppearanceArmorColor.Leather2:
                    channelName = "Leather 2";
                    break;
                case AppearanceArmorColor.Cloth1:
                    channelName = "Cloth 1";
                    break;
                case AppearanceArmorColor.Cloth2:
                    channelName = "Cloth 2";
                    break;
                case AppearanceArmorColor.Metal1:
                    channelName = "Metal 1";
                    break;
                case AppearanceArmorColor.Metal2:
                    channelName = "Metal 2";
                    break;
                default:
                    channelName = "Unknown";
                    break;
            }

            host.ColorTargetText = $"{targetName} / {channelName}";
        }

        public void ClickColorTarget(IAppearanceEditorHost host, AppearanceColorTarget target, AppearanceArmorColor channel)
        {
            host.ToggleItemEquippedFlags();
            if (host.DoesNotHaveItemEquipped)
                return;

            host.Session.ColorTarget = target;
            host.Session.SelectedColorChannel = channel;

            UpdateTargetedColor(host);
            UpdatePaletteSheetForChannel(host);
            AppearanceTintEditorService.LoadTintMapEditor(host);
        }
    }
}

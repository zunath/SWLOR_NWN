using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel
    {
        public GuiBindingList<string> HeadSwatchLeftLabel { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> HeadSwatchLeftTexture { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiRectangle> HeadSwatchLeftRegion { get => Get<GuiBindingList<GuiRectangle>>(); set => Set(value); }
        public GuiBindingList<GuiColor> HeadSwatchLeftTint { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> HeadSwatchLeftCustom { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> HeadSwatchLeftSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> HeadSwatchLeftVisible { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<string> HeadSwatchRightLabel { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> HeadSwatchRightTexture { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiRectangle> HeadSwatchRightRegion { get => Get<GuiBindingList<GuiRectangle>>(); set => Set(value); }
        public GuiBindingList<GuiColor> HeadSwatchRightTint { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> HeadSwatchRightCustom { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> HeadSwatchRightSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> HeadSwatchRightVisible { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        private void RefreshHeadColorSwatches()
        {
            // Every list binding has the same row count, including the empty right-hand cell
            // when a head has an odd number of channels. Unused channels have no list row.
            var pendingLabels = new Dictionary<string, GuiBindingList<string>>();
            foreach (var side in new[] { "Left", "Right" })
            {
                var labels = new GuiBindingList<string>();
                var textures = new GuiBindingList<string>();
                var regions = new GuiBindingList<GuiRectangle>();
                var tints = new GuiBindingList<GuiColor>();
                var customs = new GuiBindingList<bool>();
                var selected = new GuiBindingList<bool>();
                var visible = new GuiBindingList<bool>();
                for (var first = 0; first < _appearanceColorLayers.Count; first += 2)
                {
                    var index = first + (side == "Left" ? 0 : 1);
                    var exists = index < _appearanceColorLayers.Count;
                    var layer = exists ? _appearanceColorLayers[index] : TintMapLayerType.Skin;
                    labels.Add(exists ? HeadTintChannels.GetLabel(layer).Replace(" Color", "") : "");
                    textures.Add(layer switch
                    {
                        TintMapLayerType.Skin => "gui_pal_skin",
                        TintMapLayerType.Hair => "gui_pal_hair01",
                        TintMapLayerType.Metal1 or TintMapLayerType.Metal2 => "gui_pal_armor01",
                        _ => "gui_pal_tattoo"
                    });
                    GuiRectangle region;
                    GuiColor tint;
                    bool custom;
                    if (index < BodyColorSwatches.Length)
                    {
                        var name = "Body" + BodyColorSwatches[index].Name;
                        region = Get<GuiRectangle>(name + "Region");
                        tint = Get<GuiColor>(name + "Tint");
                        custom = Get<bool>(name + "Custom");
                    }
                    else
                    {
                        var material = exists ? _tintMapSelections.FirstOrDefault(selection =>
                            HeadTintChannels.IsHead(selection) && selection.Material.Layers.Contains(layer)) : null;
                        var color = material == null ? new TintMapColorSelection(0, null)
                            : TintMapService.GetEffectiveColor(_target, material, layer);
                        var rgb = color.CustomColor ?? TintMapPaletteColors.GetColor(layer, color.PaletteColorId);
                        region = BuildPaletteRegion(color.PaletteColorId);
                        tint = new GuiColor(rgb.Red, rgb.Green, rgb.Blue);
                        custom = color.CustomColor.HasValue;
                    }
                    regions.Add(region);
                    tints.Add(tint);
                    customs.Add(custom);
                    selected.Add(exists && index == SelectedColorCategoryIndex);
                    visible.Add(exists);
                    if (exists && index == SelectedColorCategoryIndex)
                    {
                        Set(region, "HeadSelectedRegion");
                        Set(custom, "HeadSelectedCustom");
                    }
                }
                var prefix = "HeadSwatch" + side;
                Set(textures, prefix + "Texture");
                Set(regions, prefix + "Region");
                Set(tints, prefix + "Tint");
                Set(customs, prefix + "Custom");
                Set(selected, prefix + "Selected");
                Set(visible, prefix + "Visible");
                pendingLabels[side] = labels;
            }
            Set(pendingLabels["Right"], nameof(HeadSwatchRightLabel));
            // The left labels drive row count; publish them after both columns are ready.
            Set(pendingLabels["Left"], nameof(HeadSwatchLeftLabel));
        }

        public Action OnClickHeadColorSwatch(int column) => () =>
        {
            if (column is < 0 or > 1)
                return;
            var row = NuiGetEventArrayIndex();
            if (row < 0)
                return;
            OnClickBodySwatch(row * 2 + column)();
        };
    }
}

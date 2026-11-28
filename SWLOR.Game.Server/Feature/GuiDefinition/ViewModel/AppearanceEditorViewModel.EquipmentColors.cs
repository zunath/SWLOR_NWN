using System;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel
    {
        public GuiBindingList<string> ItemSwatchLeftLabel { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ItemSwatchLeftTexture { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ItemSwatchLeftTooltip { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiRectangle> ItemSwatchLeftRegion { get => Get<GuiBindingList<GuiRectangle>>(); set => Set(value); }
        public GuiBindingList<GuiColor> ItemSwatchLeftTint { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> ItemSwatchLeftCustom { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> ItemSwatchLeftSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<string> ItemSwatchRightLabel { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ItemSwatchRightTexture { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ItemSwatchRightTooltip { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiRectangle> ItemSwatchRightRegion { get => Get<GuiBindingList<GuiRectangle>>(); set => Set(value); }
        public GuiBindingList<GuiColor> ItemSwatchRightTint { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> ItemSwatchRightCustom { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> ItemSwatchRightSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<string> PartSwatchLeftLabel { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> PartSwatchLeftTexture { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> PartSwatchLeftTooltip { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiRectangle> PartSwatchLeftRegion { get => Get<GuiBindingList<GuiRectangle>>(); set => Set(value); }
        public GuiBindingList<GuiColor> PartSwatchLeftTint { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> PartSwatchLeftCustom { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> PartSwatchLeftSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<string> PartSwatchRightLabel { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> PartSwatchRightTexture { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> PartSwatchRightTooltip { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiRectangle> PartSwatchRightRegion { get => Get<GuiBindingList<GuiRectangle>>(); set => Set(value); }
        public GuiBindingList<GuiColor> PartSwatchRightTint { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> PartSwatchRightCustom { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> PartSwatchRightSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        internal static AppearanceArmorColor? EquipmentSwatchChannel(int row, int column) =>
            (row, column) switch
            {
                (0, 0) => AppearanceArmorColor.Cloth1,
                (0, 1) => AppearanceArmorColor.Cloth2,
                (1, 0) => AppearanceArmorColor.Leather1,
                (1, 1) => AppearanceArmorColor.Leather2,
                (2, 0) => AppearanceArmorColor.Metal1,
                (2, 1) => AppearanceArmorColor.Metal2,
                _ => null
            };

        private void RefreshEquipmentColorSwatches()
        {
            foreach (var part in new[] { false, true })
            foreach (var column in new[] { 0, 1 })
            {
                var prefix = (part ? "PartSwatch" : "ItemSwatch") + (column == 0 ? "Left" : "Right");
                var labels = new GuiBindingList<string>();
                var textures = new GuiBindingList<string>();
                var tips = new GuiBindingList<string>();
                var regions = new GuiBindingList<GuiRectangle>();
                var colors = new GuiBindingList<GuiColor>();
                var custom = new GuiBindingList<bool>();
                var selected = new GuiBindingList<bool>();
                for (var row = 0; row < 3; row++)
                {
                    var channel = EquipmentSwatchChannel(row, column).Value;
                    var name = (part ? "SelectedPart" : "Global") + channel;
                    var text = channel.ToString();
                    labels.Add(text.Insert(text.Length - 1, " "));
                    textures.Add(channel is AppearanceArmorColor.Metal1 or AppearanceArmorColor.Metal2
                        ? "gui_pal_armor01" : "gui_pal_tattoo");
                    tips.Add(part ? Get<string>(name + "Tooltip")
                        : "Colours the whole item. Parts with an empty swatch use this colour.");
                    regions.Add(Get<GuiRectangle>(name + "Region") ?? BuildPaletteRegion(255));
                    colors.Add(Get<GuiColor>(name + "Tint") ?? new GuiColor(0, 0, 0));
                    custom.Add(Get<bool>(name + "Custom"));
                    selected.Add(Get<bool>(name + "Selected"));
                }
                PublishEquipmentSwatchValues(textures, prefix + "Texture");
                PublishEquipmentSwatchValues(tips, prefix + "Tooltip");
                PublishEquipmentSwatchValues(regions, prefix + "Region");
                PublishEquipmentSwatchValues(colors, prefix + "Tint");
                PublishEquipmentSwatchValues(custom, prefix + "Custom");
                PublishEquipmentSwatchValues(selected, prefix + "Selected");
                PublishEquipmentSwatchValues(labels, prefix + "Label");
            }
        }

        internal static bool SameEquipmentSwatchValue(object first, object second) => (first, second) switch
        {
            (GuiRectangle a, GuiRectangle b) => a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height,
            (GuiColor a, GuiColor b) => a.R == b.R && a.G == b.G && a.B == b.B && a.Alpha == b.Alpha,
            _ => Equals(first, second)
        };

        private void PublishEquipmentSwatchValues<T>(GuiBindingList<T> values, string name)
        {
            var current = Get<GuiBindingList<T>>(name);
            // Armor hydration touches many scalar colors; publish only lists whose
            // displayed values actually changed.
            if (current != null && current.Count == values.Count &&
                current.Zip(values).All(pair => SameEquipmentSwatchValue(pair.First, pair.Second)))
                return;
            Set(values, name);
        }

        public Action OnClickEquipmentColorSwatch(bool part, int column) => () =>
        {
            var channel = EquipmentSwatchChannel(NuiGetEventArrayIndex(), column);
            if (!channel.HasValue)
                return;
            if (part) OnClickPartSwatch(channel.Value)();
            else OnClickBaseSwatch(channel.Value)();
        };

        public Action OnMouseDownEquipmentColorSwatch(int column) => () =>
        {
            var channel = EquipmentSwatchChannel(NuiGetEventArrayIndex(), column);
            if (channel.HasValue)
                OnMouseDownPartSwatch(channel.Value)();
        };
    }
}

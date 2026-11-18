using SWLOR.Game.Server.Feature.AppearanceEditing;
using System.Linq;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel
    {
        private void UpdateColorSwatch(AppearanceColorTarget target, AppearanceArmorColor channel, string regionName)
        {
            var name = regionName[..^"Region".Length];
            var item = GetItem();
            AppearanceColorService.TryGetArmorTintLayer(channel, out var layer);
            var colorId = target == AppearanceColorTarget.Global
                ? GetItemAppearance(item, ItemAppearanceType.ArmorColor, (int)channel)
                : AppearanceColorService.GetArmorSwatchColor(this, item, AppearancePartEditService.GetArmorModelType(this, target), channel);
            var custom = default(TintMapColor);
            var hasCustom = target == AppearanceColorTarget.Global
                ? TintMapColor.TryFromStoredValue(GetLocalInt(item,
                    TintMapVariable.GetItemGlobalColorStateName(layer)), out custom)
                : colorId != 255 && _session.TintMapSelections.Any(selection =>
                    selection.ArmorPart == AppearancePartEditService.GetArmorModelType(this, target) &&
                    selection.GetPaletteSource(layer) == item && selection.Material.Layers.Contains(layer) &&
                    TintMapColorResolver.TryGetCustomColor(selection, layer, out custom));
            var color = hasCustom ? custom : colorId == 255 ? default
                : TintMapPaletteColors.GetColor(layer, colorId);
            var previous = Get<GuiColor>(name + "Tint");
            if (previous == null || previous.R != color.Red || previous.G != color.Green || previous.B != color.Blue)
                Set(new GuiColor(color.Red, color.Green, color.Blue), name + "Tint");
            if (previous == null || Get<bool>(name + "Custom") != hasCustom)
                Set(hasCustom, name + "Custom");
            OnSwatchUpdated(target, channel);
        }
    }
}

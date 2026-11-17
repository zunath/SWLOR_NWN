using System.Collections.Generic;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel
    {
        // The swatches (ArmorColorRegionCatalog) are bound by name; the editor publishes each one's region here.

        private void RegisterColorMappings()
        {
            InitializeRegions();
            _session.ColorMappingsRegistered = true;
        }

        private void InitializeRegions()
        {
            foreach (var swatch in ArmorColorRegionCatalog.Entries)
                Set(ArmorColorRegionCatalog.NeutralRegion(), swatch.RegionName);
        }

        private void ChangeColor(AppearanceColorTarget target, AppearanceArmorColor channel, int colorId)
        {
            var swatch = ArmorColorRegionCatalog.Get(target, channel);
            Set(ArmorColorRegionCatalog.BuildPaletteRegion(colorId), swatch.RegionName);
            UpdateColorSwatch(target, channel, swatch.RegionName);
            if (target == _session.ColorTarget && channel == _session.SelectedColorChannel)
                UpdateTargetedColor();
        }
    }
}

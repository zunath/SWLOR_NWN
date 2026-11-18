using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel
    {

        private void InitializeColorSelection()
        {
            _session.ActiveColorSelection = null;
            foreach (var swatch in ArmorColorRegionCatalog.Entries)
                Set(false, swatch.SelectedName);
            UpdateColorSelection();
        }

        private void UpdateColorSelection()
        {
            var selected = _session.ColorMappingsRegistered &&
                           ArmorColorRegionCatalog.TryGet(_session.ColorTarget, _session.SelectedColorChannel, out var swatch)
                ? swatch.SelectedName
                : null;
            if (selected == _session.ActiveColorSelection)
            {
                SyncSelectedPartSelection();
                return;
            }

            if (_session.ActiveColorSelection != null)
                Set(false, _session.ActiveColorSelection);
            _session.ActiveColorSelection = selected;
            if (selected != null)
                Set(true, selected);
            SyncSelectedPartSelection();
        }
    }
}

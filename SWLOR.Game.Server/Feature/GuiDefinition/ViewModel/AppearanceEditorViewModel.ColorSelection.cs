using System.Linq;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel
    {
        private string _activeColorSelection;

        internal static string GetColorSelectionPropertyName(string regionPropertyName) =>
            regionPropertyName[..^"Region".Length] + "Selected";

        private void InitializeColorSelection()
        {
            _activeColorSelection = null;
            foreach (var region in _colorMappings.Values.SelectMany(channels => channels.Values))
                Set(false, GetColorSelectionPropertyName(region.PropertyName));
            UpdateColorSelection();
        }

        private void UpdateColorSelection()
        {
            var selected = _colorMappings != null &&
                           _colorMappings.TryGetValue(_colorTarget, out var channels) &&
                           channels.TryGetValue(_selectedColorChannel, out var region)
                ? GetColorSelectionPropertyName(region.PropertyName)
                : null;
            if (selected == _activeColorSelection)
                return;

            if (_activeColorSelection != null)
                Set(false, _activeColorSelection);
            _activeColorSelection = selected;
            if (selected != null)
                Set(true, selected);
        }

        // Cached scalar bindings allow one active swatch to move without replacing
        // controls, changing their dimensions, or writing any appearance colors.
        public bool GlobalLeather1Selected => Get<bool>();
        public bool GlobalLeather2Selected => Get<bool>();
        public bool GlobalCloth1Selected => Get<bool>();
        public bool GlobalCloth2Selected => Get<bool>();
        public bool GlobalMetal1Selected => Get<bool>();
        public bool GlobalMetal2Selected => Get<bool>();
        public bool LeftShoulderLeather1Selected => Get<bool>();
        public bool LeftShoulderLeather2Selected => Get<bool>();
        public bool LeftShoulderCloth1Selected => Get<bool>();
        public bool LeftShoulderCloth2Selected => Get<bool>();
        public bool LeftShoulderMetal1Selected => Get<bool>();
        public bool LeftShoulderMetal2Selected => Get<bool>();
        public bool LeftBicepLeather1Selected => Get<bool>();
        public bool LeftBicepLeather2Selected => Get<bool>();
        public bool LeftBicepCloth1Selected => Get<bool>();
        public bool LeftBicepCloth2Selected => Get<bool>();
        public bool LeftBicepMetal1Selected => Get<bool>();
        public bool LeftBicepMetal2Selected => Get<bool>();
        public bool LeftForearmLeather1Selected => Get<bool>();
        public bool LeftForearmLeather2Selected => Get<bool>();
        public bool LeftForearmCloth1Selected => Get<bool>();
        public bool LeftForearmCloth2Selected => Get<bool>();
        public bool LeftForearmMetal1Selected => Get<bool>();
        public bool LeftForearmMetal2Selected => Get<bool>();
        public bool LeftHandLeather1Selected => Get<bool>();
        public bool LeftHandLeather2Selected => Get<bool>();
        public bool LeftHandCloth1Selected => Get<bool>();
        public bool LeftHandCloth2Selected => Get<bool>();
        public bool LeftHandMetal1Selected => Get<bool>();
        public bool LeftHandMetal2Selected => Get<bool>();
        public bool LeftThighLeather1Selected => Get<bool>();
        public bool LeftThighLeather2Selected => Get<bool>();
        public bool LeftThighCloth1Selected => Get<bool>();
        public bool LeftThighCloth2Selected => Get<bool>();
        public bool LeftThighMetal1Selected => Get<bool>();
        public bool LeftThighMetal2Selected => Get<bool>();
        public bool LeftShinLeather1Selected => Get<bool>();
        public bool LeftShinLeather2Selected => Get<bool>();
        public bool LeftShinCloth1Selected => Get<bool>();
        public bool LeftShinCloth2Selected => Get<bool>();
        public bool LeftShinMetal1Selected => Get<bool>();
        public bool LeftShinMetal2Selected => Get<bool>();
        public bool LeftFootLeather1Selected => Get<bool>();
        public bool LeftFootLeather2Selected => Get<bool>();
        public bool LeftFootCloth1Selected => Get<bool>();
        public bool LeftFootCloth2Selected => Get<bool>();
        public bool LeftFootMetal1Selected => Get<bool>();
        public bool LeftFootMetal2Selected => Get<bool>();
        public bool RightShoulderLeather1Selected => Get<bool>();
        public bool RightShoulderLeather2Selected => Get<bool>();
        public bool RightShoulderCloth1Selected => Get<bool>();
        public bool RightShoulderCloth2Selected => Get<bool>();
        public bool RightShoulderMetal1Selected => Get<bool>();
        public bool RightShoulderMetal2Selected => Get<bool>();
        public bool RightBicepLeather1Selected => Get<bool>();
        public bool RightBicepLeather2Selected => Get<bool>();
        public bool RightBicepCloth1Selected => Get<bool>();
        public bool RightBicepCloth2Selected => Get<bool>();
        public bool RightBicepMetal1Selected => Get<bool>();
        public bool RightBicepMetal2Selected => Get<bool>();
        public bool RightForearmLeather1Selected => Get<bool>();
        public bool RightForearmLeather2Selected => Get<bool>();
        public bool RightForearmCloth1Selected => Get<bool>();
        public bool RightForearmCloth2Selected => Get<bool>();
        public bool RightForearmMetal1Selected => Get<bool>();
        public bool RightForearmMetal2Selected => Get<bool>();
        public bool RightHandLeather1Selected => Get<bool>();
        public bool RightHandLeather2Selected => Get<bool>();
        public bool RightHandCloth1Selected => Get<bool>();
        public bool RightHandCloth2Selected => Get<bool>();
        public bool RightHandMetal1Selected => Get<bool>();
        public bool RightHandMetal2Selected => Get<bool>();
        public bool RightThighLeather1Selected => Get<bool>();
        public bool RightThighLeather2Selected => Get<bool>();
        public bool RightThighCloth1Selected => Get<bool>();
        public bool RightThighCloth2Selected => Get<bool>();
        public bool RightThighMetal1Selected => Get<bool>();
        public bool RightThighMetal2Selected => Get<bool>();
        public bool RightShinLeather1Selected => Get<bool>();
        public bool RightShinLeather2Selected => Get<bool>();
        public bool RightShinCloth1Selected => Get<bool>();
        public bool RightShinCloth2Selected => Get<bool>();
        public bool RightShinMetal1Selected => Get<bool>();
        public bool RightShinMetal2Selected => Get<bool>();
        public bool RightFootLeather1Selected => Get<bool>();
        public bool RightFootLeather2Selected => Get<bool>();
        public bool RightFootCloth1Selected => Get<bool>();
        public bool RightFootCloth2Selected => Get<bool>();
        public bool RightFootMetal1Selected => Get<bool>();
        public bool RightFootMetal2Selected => Get<bool>();
        public bool NeckLeather1Selected => Get<bool>();
        public bool NeckLeather2Selected => Get<bool>();
        public bool NeckCloth1Selected => Get<bool>();
        public bool NeckCloth2Selected => Get<bool>();
        public bool NeckMetal1Selected => Get<bool>();
        public bool NeckMetal2Selected => Get<bool>();
        public bool ChestLeather1Selected => Get<bool>();
        public bool ChestLeather2Selected => Get<bool>();
        public bool ChestCloth1Selected => Get<bool>();
        public bool ChestCloth2Selected => Get<bool>();
        public bool ChestMetal1Selected => Get<bool>();
        public bool ChestMetal2Selected => Get<bool>();
        public bool BeltLeather1Selected => Get<bool>();
        public bool BeltLeather2Selected => Get<bool>();
        public bool BeltCloth1Selected => Get<bool>();
        public bool BeltCloth2Selected => Get<bool>();
        public bool BeltMetal1Selected => Get<bool>();
        public bool BeltMetal2Selected => Get<bool>();
        public bool PelvisLeather1Selected => Get<bool>();
        public bool PelvisLeather2Selected => Get<bool>();
        public bool PelvisCloth1Selected => Get<bool>();
        public bool PelvisCloth2Selected => Get<bool>();
        public bool PelvisMetal1Selected => Get<bool>();
        public bool PelvisMetal2Selected => Get<bool>();
        public bool RobeLeather1Selected => Get<bool>();
        public bool RobeLeather2Selected => Get<bool>();
        public bool RobeCloth1Selected => Get<bool>();
        public bool RobeCloth2Selected => Get<bool>();
        public bool RobeMetal1Selected => Get<bool>();
        public bool RobeMetal2Selected => Get<bool>();
    }
}


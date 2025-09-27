using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.UI.Component;

namespace SWLOR.Component.Character.UI.ViewModel
{
    public partial class AppearanceEditorViewModel
    {
        private class ColorRegion
        {
            public string PropertyName { get; set; }
            public GuiRectangle Region { get; set; }

            public ColorRegion(string propertyName, GuiRectangle region)
            {
                PropertyName = propertyName;
                Region = region;
            }
        }

        private Dictionary<ColorTarget, Dictionary<ItemAppearanceArmorColorType, ColorRegion>> _colorMappings;

        private void RegisterColorMappings()
        {
            InitializeRegions();

            _colorMappings = new Dictionary<ColorTarget, Dictionary<ItemAppearanceArmorColorType, ColorRegion>>
            {
                [ColorTarget.Global] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(GlobalLeather1Region), GlobalLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(GlobalLeather2Region), GlobalLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(GlobalCloth1Region), GlobalCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(GlobalCloth2Region), GlobalCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(GlobalMetal1Region), GlobalMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(GlobalMetal2Region), GlobalMetal2Region)
                },
                [ColorTarget.LeftShoulder] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(LeftShoulderLeather1Region), LeftShoulderLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(LeftShoulderLeather2Region), LeftShoulderLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(LeftShoulderCloth1Region), LeftShoulderCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(LeftShoulderCloth2Region), LeftShoulderCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(LeftShoulderMetal1Region), LeftShoulderMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(LeftShoulderMetal2Region), LeftShoulderMetal2Region)
                },
                [ColorTarget.LeftBicep] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(LeftBicepLeather1Region), LeftBicepLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(LeftBicepLeather2Region), LeftBicepLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(LeftBicepCloth1Region), LeftBicepCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(LeftBicepCloth2Region), LeftBicepCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(LeftBicepMetal1Region), LeftBicepMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(LeftBicepMetal2Region), LeftBicepMetal2Region)
                },
                [ColorTarget.LeftForearm] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(LeftForearmLeather1Region), LeftForearmLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(LeftForearmLeather2Region), LeftForearmLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(LeftForearmCloth1Region), LeftForearmCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(LeftForearmCloth2Region), LeftForearmCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(LeftForearmMetal1Region), LeftForearmMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(LeftForearmMetal2Region), LeftForearmMetal2Region)
                },
                [ColorTarget.LeftHand] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(LeftHandLeather1Region), LeftHandLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(LeftHandLeather2Region), LeftHandLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(LeftHandCloth1Region), LeftHandCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(LeftHandCloth2Region), LeftHandCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(LeftHandMetal1Region), LeftHandMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(LeftHandMetal2Region), LeftHandMetal2Region)
                },
                [ColorTarget.LeftThigh] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(LeftThighLeather1Region), LeftThighLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(LeftThighLeather2Region), LeftThighLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(LeftThighCloth1Region), LeftThighCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(LeftThighCloth2Region), LeftThighCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(LeftThighMetal1Region), LeftThighMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(LeftThighMetal2Region), LeftThighMetal2Region)
                },
                [ColorTarget.LeftShin] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(LeftShinLeather1Region), LeftShinLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(LeftShinLeather2Region), LeftShinLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(LeftShinCloth1Region), LeftShinCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(LeftShinCloth2Region), LeftShinCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(LeftShinMetal1Region), LeftShinMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(LeftShinMetal2Region), LeftShinMetal2Region)
                },
                [ColorTarget.LeftFoot] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(LeftFootLeather1Region), LeftFootLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(LeftFootLeather2Region), LeftFootLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(LeftFootCloth1Region), LeftFootCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(LeftFootCloth2Region), LeftFootCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(LeftFootMetal1Region), LeftFootMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(LeftFootMetal2Region), LeftFootMetal2Region)
                },
                [ColorTarget.RightShoulder] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(RightShoulderLeather1Region), RightShoulderLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(RightShoulderLeather2Region), RightShoulderLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(RightShoulderCloth1Region), RightShoulderCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(RightShoulderCloth2Region), RightShoulderCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(RightShoulderMetal1Region), RightShoulderMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(RightShoulderMetal2Region), RightShoulderMetal2Region)
                },
                [ColorTarget.RightBicep] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(RightBicepLeather1Region), RightBicepLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(RightBicepLeather2Region), RightBicepLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(RightBicepCloth1Region), RightBicepCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(RightBicepCloth2Region), RightBicepCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(RightBicepMetal1Region), RightBicepMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(RightBicepMetal2Region), RightBicepMetal2Region)
                },
                [ColorTarget.RightForearm] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(RightForearmLeather1Region), RightForearmLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(RightForearmLeather2Region), RightForearmLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(RightForearmCloth1Region), RightForearmCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(RightForearmCloth2Region), RightForearmCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(RightForearmMetal1Region), RightForearmMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(RightForearmMetal2Region), RightForearmMetal2Region)
                },
                [ColorTarget.RightHand] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(RightHandLeather1Region), RightHandLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(RightHandLeather2Region), RightHandLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(RightHandCloth1Region), RightHandCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(RightHandCloth2Region), RightHandCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(RightHandMetal1Region), RightHandMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(RightHandMetal2Region), RightHandMetal2Region)
                },
                [ColorTarget.RightThigh] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(RightThighLeather1Region), RightThighLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(RightThighLeather2Region), RightThighLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(RightThighCloth1Region), RightThighCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(RightThighCloth2Region), RightThighCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(RightThighMetal1Region), RightThighMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(RightThighMetal2Region), RightThighMetal2Region)
                },
                [ColorTarget.RightShin] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(RightShinLeather1Region), RightShinLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(RightShinLeather2Region), RightShinLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(RightShinCloth1Region), RightShinCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(RightShinCloth2Region), RightShinCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(RightShinMetal1Region), RightShinMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(RightShinMetal2Region), RightShinMetal2Region)
                },
                [ColorTarget.RightFoot] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(RightFootLeather1Region), RightFootLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(RightFootLeather2Region), RightFootLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(RightFootCloth1Region), RightFootCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(RightFootCloth2Region), RightFootCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(RightFootMetal1Region), RightFootMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(RightFootMetal2Region), RightFootMetal2Region)
                },
                [ColorTarget.Neck] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(NeckLeather1Region), NeckLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(NeckLeather2Region), NeckLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(NeckCloth1Region), NeckCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(NeckCloth2Region), NeckCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(NeckMetal1Region), NeckMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(NeckMetal2Region), NeckMetal2Region)
                },
                [ColorTarget.Chest] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(ChestLeather1Region), ChestLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(ChestLeather2Region), ChestLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(ChestCloth1Region), ChestCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(ChestCloth2Region), ChestCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(ChestMetal1Region), ChestMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(ChestMetal2Region), ChestMetal2Region)
                },
                [ColorTarget.Belt] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(BeltLeather1Region), BeltLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(BeltLeather2Region), BeltLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(BeltCloth1Region), BeltCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(BeltCloth2Region), BeltCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(BeltMetal1Region), BeltMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(BeltMetal2Region), BeltMetal2Region)
                },
                [ColorTarget.Pelvis] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(PelvisLeather1Region), PelvisLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(PelvisLeather2Region), PelvisLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(PelvisCloth1Region), PelvisCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(PelvisCloth2Region), PelvisCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(PelvisMetal1Region), PelvisMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(PelvisMetal2Region), PelvisMetal2Region)
                },
                [ColorTarget.Robe] = new()
                {
                    [ItemAppearanceArmorColorType.Leather1] = new ColorRegion(nameof(RobeLeather1Region), RobeLeather1Region),
                    [ItemAppearanceArmorColorType.Leather2] = new ColorRegion(nameof(RobeLeather2Region), RobeLeather2Region),
                    [ItemAppearanceArmorColorType.Cloth1] = new ColorRegion(nameof(RobeCloth1Region), RobeCloth1Region),
                    [ItemAppearanceArmorColorType.Cloth2] = new ColorRegion(nameof(RobeCloth2Region), RobeCloth2Region),
                    [ItemAppearanceArmorColorType.Metal1] = new ColorRegion(nameof(RobeMetal1Region), RobeMetal1Region),
                    [ItemAppearanceArmorColorType.Metal2] = new ColorRegion(nameof(RobeMetal2Region), RobeMetal2Region)
                },
            };
        }

        private void InitializeRegions()
        {
            const int X = 247;
            const int Y = 55;
            const int Width = 1;
            const int Height = 1;

            // Global
            GlobalLeather1Region = new GuiRectangle(X, Y, Width, Height);
            GlobalLeather2Region = new GuiRectangle(X, Y, Width, Height);
            GlobalCloth1Region = new GuiRectangle(X, Y, Width, Height);
            GlobalCloth2Region = new GuiRectangle(X, Y, Width, Height);
            GlobalMetal1Region = new GuiRectangle(X, Y, Width, Height);
            GlobalMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Left Shoulder
            LeftShoulderLeather1Region = new GuiRectangle(X, Y, Width, Height);
            LeftShoulderLeather2Region = new GuiRectangle(X, Y, Width, Height);
            LeftShoulderCloth1Region = new GuiRectangle(X, Y, Width, Height);
            LeftShoulderCloth2Region = new GuiRectangle(X, Y, Width, Height);
            LeftShoulderMetal1Region = new GuiRectangle(X, Y, Width, Height);
            LeftShoulderMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Left Bicep
            LeftBicepLeather1Region = new GuiRectangle(X, Y, Width, Height);
            LeftBicepLeather2Region = new GuiRectangle(X, Y, Width, Height);
            LeftBicepCloth1Region = new GuiRectangle(X, Y, Width, Height);
            LeftBicepCloth2Region = new GuiRectangle(X, Y, Width, Height);
            LeftBicepMetal1Region = new GuiRectangle(X, Y, Width, Height);
            LeftBicepMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Left Forearm
            LeftForearmLeather1Region = new GuiRectangle(X, Y, Width, Height);
            LeftForearmLeather2Region = new GuiRectangle(X, Y, Width, Height);
            LeftForearmCloth1Region = new GuiRectangle(X, Y, Width, Height);
            LeftForearmCloth2Region = new GuiRectangle(X, Y, Width, Height);
            LeftForearmMetal1Region = new GuiRectangle(X, Y, Width, Height);
            LeftForearmMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Left Hand
            LeftHandLeather1Region = new GuiRectangle(X, Y, Width, Height);
            LeftHandLeather2Region = new GuiRectangle(X, Y, Width, Height);
            LeftHandCloth1Region = new GuiRectangle(X, Y, Width, Height);
            LeftHandCloth2Region = new GuiRectangle(X, Y, Width, Height);
            LeftHandMetal1Region = new GuiRectangle(X, Y, Width, Height);
            LeftHandMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Left Thigh
            LeftThighLeather1Region = new GuiRectangle(X, Y, Width, Height);
            LeftThighLeather2Region = new GuiRectangle(X, Y, Width, Height);
            LeftThighCloth1Region = new GuiRectangle(X, Y, Width, Height);
            LeftThighCloth2Region = new GuiRectangle(X, Y, Width, Height);
            LeftThighMetal1Region = new GuiRectangle(X, Y, Width, Height);
            LeftThighMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Left Shin
            LeftShinLeather1Region = new GuiRectangle(X, Y, Width, Height);
            LeftShinLeather2Region = new GuiRectangle(X, Y, Width, Height);
            LeftShinCloth1Region = new GuiRectangle(X, Y, Width, Height);
            LeftShinCloth2Region = new GuiRectangle(X, Y, Width, Height);
            LeftShinMetal1Region = new GuiRectangle(X, Y, Width, Height);
            LeftShinMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Left Foot
            LeftFootLeather1Region = new GuiRectangle(X, Y, Width, Height);
            LeftFootLeather2Region = new GuiRectangle(X, Y, Width, Height);
            LeftFootCloth1Region = new GuiRectangle(X, Y, Width, Height);
            LeftFootCloth2Region = new GuiRectangle(X, Y, Width, Height);
            LeftFootMetal1Region = new GuiRectangle(X, Y, Width, Height);
            LeftFootMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Right Shoulder
            RightShoulderLeather1Region = new GuiRectangle(X, Y, Width, Height);
            RightShoulderLeather2Region = new GuiRectangle(X, Y, Width, Height);
            RightShoulderCloth1Region = new GuiRectangle(X, Y, Width, Height);
            RightShoulderCloth2Region = new GuiRectangle(X, Y, Width, Height);
            RightShoulderMetal1Region = new GuiRectangle(X, Y, Width, Height);
            RightShoulderMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Right Bicep
            RightBicepLeather1Region = new GuiRectangle(X, Y, Width, Height);
            RightBicepLeather2Region = new GuiRectangle(X, Y, Width, Height);
            RightBicepCloth1Region = new GuiRectangle(X, Y, Width, Height);
            RightBicepCloth2Region = new GuiRectangle(X, Y, Width, Height);
            RightBicepMetal1Region = new GuiRectangle(X, Y, Width, Height);
            RightBicepMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Right Forearm
            RightForearmLeather1Region = new GuiRectangle(X, Y, Width, Height);
            RightForearmLeather2Region = new GuiRectangle(X, Y, Width, Height);
            RightForearmCloth1Region = new GuiRectangle(X, Y, Width, Height);
            RightForearmCloth2Region = new GuiRectangle(X, Y, Width, Height);
            RightForearmMetal1Region = new GuiRectangle(X, Y, Width, Height);
            RightForearmMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Right Hand
            RightHandLeather1Region = new GuiRectangle(X, Y, Width, Height);
            RightHandLeather2Region = new GuiRectangle(X, Y, Width, Height);
            RightHandCloth1Region = new GuiRectangle(X, Y, Width, Height);
            RightHandCloth2Region = new GuiRectangle(X, Y, Width, Height);
            RightHandMetal1Region = new GuiRectangle(X, Y, Width, Height);
            RightHandMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Right Thigh
            RightThighLeather1Region = new GuiRectangle(X, Y, Width, Height);
            RightThighLeather2Region = new GuiRectangle(X, Y, Width, Height);
            RightThighCloth1Region = new GuiRectangle(X, Y, Width, Height);
            RightThighCloth2Region = new GuiRectangle(X, Y, Width, Height);
            RightThighMetal1Region = new GuiRectangle(X, Y, Width, Height);
            RightThighMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Right Shin
            RightShinLeather1Region = new GuiRectangle(X, Y, Width, Height);
            RightShinLeather2Region = new GuiRectangle(X, Y, Width, Height);
            RightShinCloth1Region = new GuiRectangle(X, Y, Width, Height);
            RightShinCloth2Region = new GuiRectangle(X, Y, Width, Height);
            RightShinMetal1Region = new GuiRectangle(X, Y, Width, Height);
            RightShinMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Right Foot
            RightFootLeather1Region = new GuiRectangle(X, Y, Width, Height);
            RightFootLeather2Region = new GuiRectangle(X, Y, Width, Height);
            RightFootCloth1Region = new GuiRectangle(X, Y, Width, Height);
            RightFootCloth2Region = new GuiRectangle(X, Y, Width, Height);
            RightFootMetal1Region = new GuiRectangle(X, Y, Width, Height);
            RightFootMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Neck
            NeckLeather1Region = new GuiRectangle(X, Y, Width, Height);
            NeckLeather2Region = new GuiRectangle(X, Y, Width, Height);
            NeckCloth1Region = new GuiRectangle(X, Y, Width, Height);
            NeckCloth2Region = new GuiRectangle(X, Y, Width, Height);
            NeckMetal1Region = new GuiRectangle(X, Y, Width, Height);
            NeckMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Chest
            ChestLeather1Region = new GuiRectangle(X, Y, Width, Height);
            ChestLeather2Region = new GuiRectangle(X, Y, Width, Height);
            ChestCloth1Region = new GuiRectangle(X, Y, Width, Height);
            ChestCloth2Region = new GuiRectangle(X, Y, Width, Height);
            ChestMetal1Region = new GuiRectangle(X, Y, Width, Height);
            ChestMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Belt
            BeltLeather1Region = new GuiRectangle(X, Y, Width, Height);
            BeltLeather2Region = new GuiRectangle(X, Y, Width, Height);
            BeltCloth1Region = new GuiRectangle(X, Y, Width, Height);
            BeltCloth2Region = new GuiRectangle(X, Y, Width, Height);
            BeltMetal1Region = new GuiRectangle(X, Y, Width, Height);
            BeltMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Pelvis
            PelvisLeather1Region = new GuiRectangle(X, Y, Width, Height);
            PelvisLeather2Region = new GuiRectangle(X, Y, Width, Height);
            PelvisCloth1Region = new GuiRectangle(X, Y, Width, Height);
            PelvisCloth2Region = new GuiRectangle(X, Y, Width, Height);
            PelvisMetal1Region = new GuiRectangle(X, Y, Width, Height);
            PelvisMetal2Region = new GuiRectangle(X, Y, Width, Height);

            // Robe
            RobeLeather1Region = new GuiRectangle(X, Y, Width, Height);
            RobeLeather2Region = new GuiRectangle(X, Y, Width, Height);
            RobeCloth1Region = new GuiRectangle(X, Y, Width, Height);
            RobeCloth2Region = new GuiRectangle(X, Y, Width, Height);
            RobeMetal1Region = new GuiRectangle(X, Y, Width, Height);
            RobeMetal2Region = new GuiRectangle(X, Y, Width, Height);
        }

        private (int, int) ColorIdToCoordinates(int colorId)
        {
            var x = colorId % TextureColorsPerRow;
            var y = colorId / TextureColorsPerRow;

            return (x, y);
        }
        private void ChangeColor(ColorTarget target, ItemAppearanceArmorColorType channel, int colorId)
        {
            if (colorId >= 255)
            {
                _colorMappings[target][channel].Region = new GuiRectangle(247, 55, 1, 1);
            }
            else
            {
                var (x, y) = ColorIdToCoordinates(colorId);
                _colorMappings[target][channel].Region = new GuiRectangle(x * ColorSize, y * ColorSize, ColorSize, ColorSize);
            }

            GetType().GetProperty(_colorMappings[target][channel].PropertyName)?.SetValue(this, _colorMappings[target][channel].Region);
        }

        // Global
        public GuiRectangle GlobalLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle GlobalLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle GlobalCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle GlobalCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle GlobalMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle GlobalMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Left Shoulder
        public GuiRectangle LeftShoulderLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle LeftShoulderLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftShoulderCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftShoulderCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftShoulderMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftShoulderMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Left Bicep
        public GuiRectangle LeftBicepLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle LeftBicepLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftBicepCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftBicepCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftBicepMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftBicepMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Left Forearm
        public GuiRectangle LeftForearmLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle LeftForearmLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftForearmCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftForearmCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftForearmMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftForearmMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Left Hand
        public GuiRectangle LeftHandLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle LeftHandLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftHandCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftHandCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftHandMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftHandMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Left Thigh
        public GuiRectangle LeftThighLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle LeftThighLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftThighCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftThighCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftThighMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftThighMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Left Shin
        public GuiRectangle LeftShinLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle LeftShinLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftShinCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftShinCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftShinMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftShinMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Left Foot
        public GuiRectangle LeftFootLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle LeftFootLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftFootCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftFootCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftFootMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle LeftFootMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Right Shoulder
        public GuiRectangle RightShoulderLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle RightShoulderLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightShoulderCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightShoulderCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightShoulderMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightShoulderMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Right Bicep
        public GuiRectangle RightBicepLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle RightBicepLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightBicepCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightBicepCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightBicepMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightBicepMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Right Forearm
        public GuiRectangle RightForearmLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle RightForearmLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightForearmCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightForearmCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightForearmMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightForearmMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Right Hand
        public GuiRectangle RightHandLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle RightHandLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightHandCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightHandCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightHandMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightHandMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Right Thigh
        public GuiRectangle RightThighLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle RightThighLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightThighCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightThighCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightThighMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightThighMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Right Shin
        public GuiRectangle RightShinLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle RightShinLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightShinCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightShinCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightShinMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightShinMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Right Foot
        public GuiRectangle RightFootLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle RightFootLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightFootCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightFootCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightFootMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RightFootMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Neck
        public GuiRectangle NeckLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle NeckLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle NeckCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle NeckCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle NeckMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle NeckMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Chest
        public GuiRectangle ChestLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle ChestLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle ChestCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle ChestCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle ChestMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle ChestMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Belt
        public GuiRectangle BeltLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle BeltLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle BeltCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle BeltCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle BeltMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle BeltMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Pelvis
        public GuiRectangle PelvisLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle PelvisLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle PelvisCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle PelvisCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle PelvisMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle PelvisMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        // Robe
        public GuiRectangle RobeLeather1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public GuiRectangle RobeLeather2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RobeCloth1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RobeCloth2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RobeMetal1Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
        public GuiRectangle RobeMetal2Region
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }
    }
}

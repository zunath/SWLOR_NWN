using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class AppearanceEditorDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<AppearanceEditorViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AppearanceEditor)
                .BindOnOpened(model => model.OnLoadWindow())
                .BindOnClosed(model => model.OnCloseWindow())
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Appearance Editor")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .SetText("Appearance")
                            .SetHeight(32f)
                            .BindIsToggled(model => model.IsAppearanceSelected)
                            .BindOnClicked(model => model.OnSelectAppearance());

                        row.AddToggleButton()
                            .SetText("Equipment")
                            .SetHeight(32f)
                            .BindIsToggled(model => model.IsEquipmentSelected)
                            .BindOnClicked(model => model.OnSelectEquipment());

                        row.AddToggleButton()
                            .SetText("Outfits")
                            .SetHeight(32f)
                            .BindIsToggled(model => model.IsOutfitsSelected)
                            .BindOnClicked(model => model.OnSelectOutfits());
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddComboBox()
                            .AddOption("Skin Color", 1)
                            .AddOption("Hair Color", 2)
                            .AddOption("Tattoo Color", 3)
                            .AddOption("Head", 4)
                            .AddOption("Torso", 5)
                            .AddOption("Pelvis", 6)
                            .AddOption("Right Bicep", 7)
                            .AddOption("Right Forearm", 8)
                            .AddOption("Right Hand", 9)
                            .AddOption("Right Thigh", 10)
                            .AddOption("Right Shin", 11)
                            .AddOption("Left Bicep", 12)
                            .AddOption("Left Forearm", 13)
                            .AddOption("Left Hand", 14)
                            .AddOption("Left Thigh", 15)
                            .AddOption("Left Shin", 16)
                            .BindSelectedIndex(model => model.SelectedPartIndex);
                        row.AddSpacer();

                        row.BindIsVisible(model => model.IsAppearanceSelected);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddImage()
                            .BindResref(model => model.ColorSheetResref)
                            .BindIsVisible(model => model.IsColorSheetPartSelected)
                            .SetHeight(176f)
                            .SetWidth(256f)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetAspect(NuiAspect.ExactScaled)
                            .BindOnMouseDown(model => model.OnSelectColor());
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Apply Changes")
                            .BindOnClicked(model => model.OnApplyChanges());

                        row.AddButton()
                            .SetText("Cancel")
                            .BindOnClicked(model => model.OnCancelChanges());

                        row.AddSpacer();
                    });

                });

            return _builder.Build();
        }
    }
}

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
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                    {
                                        template.AddToggleButton()
                                            .BindText(model => model.ColorCategoryOptions)
                                            .BindIsToggled(model => model.ColorCategorySelected)
                                            .BindOnClicked(model => model.OnSelectColorCategory());
                                    })
                                    .BindRowCount(model => model.ColorCategoryOptions);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                    {
                                        template.AddToggleButton()
                                            .BindText(model => model.PartCategoryOptions)
                                            .BindIsToggled(model => model.PartCategorySelected)
                                            .BindOnClicked(model => model.OnSelectPartCategory());
                                    })
                                    .BindRowCount(model => model.PartCategoryOptions);
                            });

                        });


                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddImage()
                                    .BindResref(model => model.ColorSheetResref)
                                    .SetHeight(176f)
                                    .SetWidth(256f)
                                    .SetVerticalAlign(NuiVerticalAlign.Top)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetAspect(NuiAspect.ExactScaled)
                                    .BindOnMouseDown(model => model.OnSelectColor());
                                
                                row2.BindIsVisible(model => model.IsColorSheetPartSelected);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                    {
                                        template.AddToggleButton()
                                            .BindText(model => model.PartOptions)
                                            .BindIsToggled(model => model.PartSelected)
                                            .BindOnClicked(model => model.OnSelectPart());
                                    })
                                    .BindRowCount(model => model.PartOptions);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText("Previous Part")
                                    .SetHeight(32f)
                                    .BindOnClicked(model => model.OnPreviousPart());

                                row2.AddButton()
                                    .SetText("Next Part")
                                    .SetHeight(32f)
                                    .BindOnClicked(model => model.OnNextPart());
                            });

                        });
                        
                        row.BindIsVisible(model => model.IsAppearanceSelected);
                    });
                });

            return _builder.Build();
        }
    }
}

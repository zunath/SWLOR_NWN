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
                .SetInitialGeometry(0, 0, 476.57895f, 530.2632f)
                .SetTitle("Appearance Editor")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();

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

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.BindIsVisible(model => model.IsEquipmentSelected);

                        row.AddSpacer();
                        
                        row.AddComboBox()
                            .AddOption("Armor", 0)
                            .AddOption("Helmet", 1)
                            .BindSelectedIndex(model => model.SelectedItemTypeIndex);

                        row.AddSpacer();

                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("No item is equipped.")
                            .BindIsVisible(model => model.DoesNotHaveItemEquipped);

                        row.SetHeight(20f);
                        row.BindIsVisible(model => model.DoesNotHaveItemEquipped);
                    });

                    col.AddRow(row =>
                    {
                        row.BindIsVisible(model => model.HasItemEquipped);

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
                                    .BindRowCount(model => model.PartOptions)
                                    .SetWidth(256f);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddButton()
                                    .SetText("Previous Part")
                                    .SetHeight(32f)
                                    .SetWidth(128f)
                                    .BindOnClicked(model => model.OnPreviousPart());

                                row2.AddButton()
                                    .SetText("Next Part")
                                    .SetHeight(32f)
                                    .SetWidth(128f)
                                    .BindOnClicked(model => model.OnNextPart());
                            });

                        });
                    });
                });

            return _builder.Build();
        }
    }
}

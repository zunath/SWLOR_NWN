using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class PlanterDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<PlanterViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Planter)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 500f, 400f)
                .SetTitle("Planter")

                .DefinePartialView(PlanterViewModel.NoCropPartial, partial =>
                {
                    partial.AddColumn(col =>
                    {
                        BuildMaxConcurrentCropsSection(col);
                        BuildCropSelectionSection(col);
                        BuildPlantSection(col);
                    });
                })

                .DefinePartialView(PlanterViewModel.GrowingPartial, partial =>
                {
                    partial.AddColumn(col =>
                    {
                        BuildGrowingDetailsSection(col);
                        BuildGrowingActionsSection(col);
                    });
                })

                .DefinePartialView(PlanterViewModel.HarvestPartial, partial =>
                {
                    partial.AddColumn(col =>
                    {
                        BuildHarvestDetailsSection(col);
                        BuildHarvestActionsSection(col);
                    });
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddPartialView(PlanterViewModel.PartialElement);
                    });
                });

            return _builder.Build();
        }

        private void BuildMaxConcurrentCropsSection(GuiColumn<PlanterViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddLabel()
                    .BindText(model => model.MaxConcurrentCropsText);

                row.AddSpacer();
            });
        }

        private void BuildCropSelectionSection(GuiColumn<PlanterViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Select a crop to plant:");
            });

            col.AddRow(row =>
            {
                row.AddComboBox()
                    .BindOptions(model => model.CropOptions)
                    .BindSelectedIndex(model => model.SelectedCropIndex)
                    .SetHeight(32f);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.SelectedCropDescription);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.SelectedCropYields);
            });
        }

        private void BuildPlantSection(GuiColumn<PlanterViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddButton()
                    .SetText("Plant")
                    .BindOnClicked(model => model.OnClickPlant())
                    .BindIsEnabled(model => model.IsPlantEnabled);

                row.AddSpacer();
            });
        }

        private void BuildGrowingDetailsSection(GuiColumn<PlanterViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.GrowingCropName);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.StageLabel);
            });

            col.AddRow(row =>
            {
                row.AddProgressBar()
                    .BindValue(model => model.GrowthProgress)
                    .SetColor(0, 120, 0)
                    .AddDrawList(list =>
                    {
                        list.AddText(text =>
                        {
                            text.SetColor(255, 255, 255);
                            text.SetBounds(20, 10, 400, 100);
                            text.BindText(model => model.TimeRemainingText);
                        });
                    });
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.GrowingTendBonusText);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.FertilizerStatusText);
            });
        }

        private void BuildGrowingActionsSection(GuiColumn<PlanterViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddButton()
                    .SetText("Tend")
                    .BindOnClicked(model => model.OnClickTend())
                    .BindIsEnabled(model => model.IsTendEnabled);

                row.AddButton()
                    .SetText("Clear Crop")
                    .BindOnClicked(model => model.OnClickClearCrop());

                row.AddSpacer();
            });

            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddButton()
                    .SetText("Growth Accelerant")
                    .BindOnClicked(model => model.OnClickGrowthFertilizer())
                    .BindIsEnabled(model => model.IsFertilizeEnabled);

                row.AddButton()
                    .SetText("Yield Compost")
                    .BindOnClicked(model => model.OnClickYieldFertilizer())
                    .BindIsEnabled(model => model.IsFertilizeEnabled);

                row.AddButton()
                    .SetText("Quality Nutrient")
                    .BindOnClicked(model => model.OnClickQualityFertilizer())
                    .BindIsEnabled(model => model.IsFertilizeEnabled);

                row.AddSpacer();
            });
        }

        private void BuildHarvestDetailsSection(GuiColumn<PlanterViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.HarvestCropName);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Ready to harvest!")
                    .SetColor(0, 255, 0);
            });

            col.AddRow(row =>
            {
                row.AddLabel()
                    .BindText(model => model.HarvestTendBonusText);
            });
        }

        private void BuildHarvestActionsSection(GuiColumn<PlanterViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddSpacer();

                row.AddButton()
                    .SetText("Harvest")
                    .BindOnClicked(model => model.OnClickHarvest());

                row.AddSpacer();
            });
        }
    }
}

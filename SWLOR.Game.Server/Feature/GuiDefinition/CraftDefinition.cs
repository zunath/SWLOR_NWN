using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CraftDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CraftViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Craft)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 900f, 600f)
                .SetTitle("Craft Item")
                .BindIsClosable(model => model.IsClosable)
                .BindOnClosed(model => model.OnWindowClosed());

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(CraftViewModel.CraftContentElement);
                layout.AddLeadingColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.RecipeName)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.RecipeLevel)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.YourSkill)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Select Enhancements")
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .BindIsVisible(model => model.IsEnhancement1Visible);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement1Resref)
                            .BindIsVisible(model => model.IsEnhancement1Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement1())
                            .BindTooltip(model => model.Enhancement1Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement2Resref)
                            .BindIsVisible(model => model.IsEnhancement2Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement2())
                            .BindTooltip(model => model.Enhancement2Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement3Resref)
                            .BindIsVisible(model => model.IsEnhancement3Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement3())
                            .BindTooltip(model => model.Enhancement3Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement4Resref)
                            .BindIsVisible(model => model.IsEnhancement4Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement4())
                            .BindTooltip(model => model.Enhancement4Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement5Resref)
                            .BindIsVisible(model => model.IsEnhancement5Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement5())
                            .BindTooltip(model => model.Enhancement5Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement6Resref)
                            .BindIsVisible(model => model.IsEnhancement6Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement6())
                            .BindTooltip(model => model.Enhancement6Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement7Resref)
                            .BindIsVisible(model => model.IsEnhancement7Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement7())
                            .BindTooltip(model => model.Enhancement7Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement8Resref)
                            .BindIsVisible(model => model.IsEnhancement8Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement8())
                            .BindTooltip(model => model.Enhancement8Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.RecipeDescription)
                                    .BindColor(model => model.RecipeColors)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                        })
                            .BindRowCount(model => model.RecipeDescription);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .BindText(model => model.CraftText)
                            .BindOnClicked(model => model.OnClickManualCraft())
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .SetHeight(35f);

                        row.AddSpacer();
                    });

                }, 270f);
            });
            window.DefinePartialView(CraftViewModel.CraftContentPartial, group =>
            {
                group.SetWidth(560f);
                group.SetShowBorder(false);
                group.SetScrollbars(NuiScrollbars.None);
                group.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.StatusText)
                            .BindColor(model => model.StatusColor)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.DurabilityText)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.DurabilityPercentage)
                            .SetColor(169, 169, 169);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.ProgressText)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.ProgressPercentage)
                            .SetColor(50, 205, 50);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.QualityText)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.QualityPercentage)
                            .SetColor(0, 191, 255);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.CP)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Synthesis Abilities:")
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.BasicSynthesisText)
                            .BindOnClicked(model => model.OnClickBasicSynthesis())
                            .BindTooltip(model => model.BasicSynthesisTooltip)
                            .BindIsEnabled(model => model.IsBasicSynthesisEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.RapidSynthesisText)
                            .BindOnClicked(model => model.OnClickRapidSynthesis())
                            .BindTooltip(model => model.RapidSynthesisTooltip)
                            .BindIsEnabled(model => model.IsRapidSynthesisEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.CarefulSynthesisText)
                            .BindOnClicked(model => model.OnClickCarefulSynthesis())
                            .BindTooltip(model => model.CarefulSynthesisTooltip)
                            .BindIsEnabled(model => model.IsCarefulSynthesisEnabled);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Touch Abilities:")
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.BasicTouchText)
                            .BindOnClicked(model => model.OnClickBasicTouch())
                            .BindTooltip(model => model.BasicTouchTooltip)
                            .BindIsEnabled(model => model.IsBasicTouchEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.StandardTouchText)
                            .BindOnClicked(model => model.OnClickStandardTouch())
                            .BindTooltip(model => model.StandardTouchTooltip)
                            .BindIsEnabled(model => model.IsStandardTouchEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.PreciseTouchText)
                            .BindOnClicked(model => model.OnClickPreciseTouch())
                            .BindTooltip(model => model.PreciseTouchTooltip)
                            .BindIsEnabled(model => model.IsPreciseTouchEnabled);
                    });


                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Abilities:")
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.MastersMendText)
                            .BindOnClicked(model => model.OnClickMastersMend())
                            .BindTooltip(model => model.MastersMendTooltip)
                            .BindIsEnabled(model => model.IsMastersMendEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.SteadyHandText)
                            .BindOnClicked(model => model.OnClickSteadyHand())
                            .BindTooltip(model => model.SteadyHandTooltip)
                            .BindIsEnabled(model => model.IsSteadyHandEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.MuscleMemoryText)
                            .BindOnClicked(model => model.OnClickMuscleMemory())
                            .BindTooltip(model => model.MuscleMemoryTooltip)
                            .BindIsEnabled(model => model.IsMuscleMemoryEnabled);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.VenerationText)
                            .BindOnClicked(model => model.OnClickVeneration())
                            .BindTooltip(model => model.VenerationTooltip)
                            .BindIsEnabled(model => model.IsVenerationEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .BindText(model => model.WasteNotText)
                            .BindOnClicked(model => model.OnClickWasteNot())
                            .BindTooltip(model => model.WasteNotTooltip)
                            .BindIsEnabled(model => model.IsWasteNotEnabled);
                    });

                    col.AddRow(row => row.AddLabel()
                        .BindText(model => model.BuffSummary)
                        .BindTooltip(model => model.BuffSummary)
                        .SetHeight(25f));
                    col.AddRow(row => row.AddLabel()
                        .BindText(model => model.QualityRewards)
                        .BindTooltip(model => model.QualityRewards)
                        .SetHeight(25f));
                    col.AddRow(row => row.AddList(template => template.AddCell(cell => cell.AddLabel()
                        .BindText(model => model.ActionHistory)
                        .BindTooltip(model => model.ActionHistory)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left)))
                        .BindRowCount(model => model.ActionHistory)
                        .SetHeight(110f));
                });
            });
            return _builder.Build();
        }
    }
}

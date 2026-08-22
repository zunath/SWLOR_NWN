using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class LightsaberWorkbenchDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<LightsaberWorkbenchViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.LightsaberWorkbench)
                .SetInitialGeometry(0, 0, 545f, 740f)
                .SetTitle("Lightsaber Workbench")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())

                // Wrap all content in a scrollable group so the window stays usable
                // (scrolls instead of clipping) when resized smaller than its content.
                .AddRow(wrapperRow =>
                {
                    wrapperRow.AddGroup(wrapper =>
                    {
                    wrapper.SetShowBorder(false);
                    wrapper.AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddToggleButton()
                            .SetText("Lightsaber")
                            .SetHeight(32f)
                            .SetWidth(150f)
                            .BindIsToggled(model => model.IsLightsaberSelected)
                            .BindOnClicked(model => model.OnClickLightsaber());
                        row.AddToggleButton()
                            .SetText("Saberstaff")
                            .SetHeight(32f)
                            .SetWidth(150f)
                            .BindIsToggled(model => model.IsSaberstaffSelected)
                            .BindOnClicked(model => model.OnClickSaberstaff());
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .SetText("Top (Emitter / Blade)")
                            .SetHeight(20f)
                            .SetWidth(220f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddImage()
                            .BindResref(model => model.TopPreview)
                            .SetAspect(NuiAspect.Fit)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(150f)
                            .SetWidth(150f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("<")
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindOnClicked(model => model.OnClickPreviousTop());
                        row.AddLabel()
                            .BindText(model => model.TopName)
                            .SetHeight(32f)
                            .SetWidth(220f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                        row.AddButton()
                            .SetText(">")
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindOnClicked(model => model.OnClickNextTop());
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.TopCountText)
                            .SetHeight(16f)
                            .SetWidth(200f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .SetText("Bottom (Hilt)")
                            .SetHeight(20f)
                            .SetWidth(220f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddImage()
                            .BindResref(model => model.BottomPreview)
                            .SetAspect(NuiAspect.Fit)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(150f)
                            .SetWidth(150f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("<")
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindOnClicked(model => model.OnClickPreviousBottom());
                        row.AddLabel()
                            .BindText(model => model.BottomName)
                            .SetHeight(32f)
                            .SetWidth(220f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                        row.AddButton()
                            .SetText(">")
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindOnClicked(model => model.OnClickNextBottom());
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.BottomCountText)
                            .SetHeight(16f)
                            .SetWidth(200f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .SetText("Enhancements (optional)")
                            .SetHeight(20f)
                            .SetWidth(200f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement1Resref)
                            .BindOnClicked(model => model.OnClickEnhancement1())
                            .BindTooltip(model => model.Enhancement1Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement2Resref)
                            .BindOnClicked(model => model.OnClickEnhancement2())
                            .BindTooltip(model => model.Enhancement2Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .SetText("Weapon Submission Token (optional)")
                            .SetHeight(20f)
                            .SetWidth(260f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButtonImage()
                            .BindImageResref(model => model.SubmissionResref)
                            .BindOnClicked(model => model.OnClickSubmissionToken())
                            .BindTooltip(model => model.SubmissionTooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.StatusText)
                            .BindColor(model => model.StatusColor)
                            .SetHeight(20f)
                            .SetWidth(400f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("Construct")
                            .SetHeight(40f)
                            .SetWidth(200f)
                            .BindOnClicked(model => model.OnClickConstruct());
                        row.AddSpacer();
                    });
                    });
                    });
                });

            return _builder.Build();
        }
    }
}

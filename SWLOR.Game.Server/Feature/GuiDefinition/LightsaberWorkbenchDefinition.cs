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
                .SetInitialGeometry(0, 0, 545f, 610f)
                .SetTitle("Lightsaber Workbench")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())

                .AddColumn(col =>
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
                            .SetText("Hilt")
                            .SetHeight(20f)
                            .SetWidth(200f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddImage()
                            .BindResref(model => model.HiltPreview)
                            .SetAspect(NuiAspect.Fit)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(160f)
                            .SetWidth(160f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("<")
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindOnClicked(model => model.OnClickPreviousHilt());
                        row.AddLabel()
                            .BindText(model => model.HiltName)
                            .SetHeight(32f)
                            .SetWidth(220f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                        row.AddButton()
                            .SetText(">")
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindOnClicked(model => model.OnClickNextHilt());
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.HiltCountText)
                            .SetHeight(16f)
                            .SetWidth(200f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .SetText("Blade Color")
                            .SetHeight(20f)
                            .SetWidth(200f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddImage()
                            .BindResref(model => model.ColorPreview)
                            .SetAspect(NuiAspect.Fit)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(120f)
                            .SetWidth(120f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddButton()
                            .SetText("<")
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindOnClicked(model => model.OnClickPreviousColor());
                        row.AddLabel()
                            .BindText(model => model.ColorName)
                            .SetHeight(32f)
                            .SetWidth(220f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                        row.AddButton()
                            .SetText(">")
                            .SetHeight(32f)
                            .SetWidth(32f)
                            .BindOnClicked(model => model.OnClickNextColor());
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddLabel()
                            .BindText(model => model.ColorCountText)
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

            return _builder.Build();
        }
    }
}

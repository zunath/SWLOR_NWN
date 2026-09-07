using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition;

public class AnimationDebugDefinition : IGuiWindowDefinition
{
    private readonly GuiWindowBuilder<AnimationDebugViewModel> _builder = new();

    public GuiConstructedWindow BuildWindow()
    {
        var window = _builder.CreateWindow(GuiWindowType.AnimationDebug)
            .SetInitialGeometry(20f, 60f, 580f, 510f)
            .SetTitle("Animation Tester")
            .SetIsResizable(true).SetIsCollapsible(true)
            .BindOnClosed(m => m.OnWindowClosed())
            .DefinePartialView(AnimationDebugViewModel.MainContentPartial, AddContent);
        window.AddStandardLayout(layout => layout.SetContentPartialElement(AnimationDebugViewModel.ContentElement));
        return _builder.Build();
    }

    private static void AddContent(GuiGroup<AnimationDebugViewModel> host) => host.AddColumn(col =>
        col.AddRow(row => row.AddGroup(panel =>
        {
            panel.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
            panel.AddColumn(content =>
            {
                content.AddRow(r => r.AddLabel().SetText("Preview on yourself with your current equipment. No perk effects.")
                    .SetHorizontalAlign(NuiHorizontalAlign.Left).SetHeight(24f));
                content.AddRow(r => r.AddTextEdit().SetPlaceholder("Search animations...")
                    .BindValue(m => m.SearchText).SetMaxLength(64).SetHeight(32f));
                content.AddRow(r => r.AddLabel().BindText(m => m.ResultText)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left).SetHeight(24f));
                content.AddRow(r => r.AddList(template =>
                {
                    template.AddCell(cell => cell.AddLabel().BindText(m => m.Names)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.SetWidth(70f);
                        cell.AddLabel().BindText(m => m.Durations);
                    });
                    template.AddCell(cell =>
                    {
                        cell.SetIsVariable(false);
                        cell.AddButton().SetText("Play").SetWidth(62f).BindOnClicked(m => m.OnPlayRow());
                    });
                }).BindRowCount(m => m.Names).SetRowHeight(32f).SetHeight(240f));
                content.AddRow(r =>
                {
                    r.AddButton().SetText("Previous").SetWidth(90f).SetHeight(32f)
                        .BindIsEnabled(m => m.HasPrevious).BindOnClicked(m => m.OnPrevious());
                    r.AddLabel().BindText(m => m.PageText).SetHeight(32f);
                    r.AddButton().SetText("Next").SetWidth(90f).SetHeight(32f)
                        .BindIsEnabled(m => m.HasNext).BindOnClicked(m => m.OnNext());
                    r.AddButton().SetText("Stop").SetWidth(80f).SetHeight(32f).BindOnClicked(m => m.OnStop());
                });
                content.AddRow(r => r.AddText().BindText(m => m.StatusText).SetHeight(52f));
            });
        }).SetWidth(540f)));
}

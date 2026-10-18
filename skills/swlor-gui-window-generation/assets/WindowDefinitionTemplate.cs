// TEMPLATE: copy to SWLOR.Game.Server/Feature/GuiDefinition/<YourWindow>Definition.cs
// and rename every "TemplateWindow" token. Delete markers as you fill them.
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class TemplateWindowDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<TemplateWindowViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        // TEMPLATE: one tab row is 28f + ~20f margin allowance. 48f for one row,
        // 112f for three (see DebugNuiGallery).
        private const float TabPanelHeight = 48f;
        // TEMPLATE: fixed tab-panel width, 250-560f (rule R5).
        private const float ContentPanelWidth = 560f;
        // TEMPLATE: delete if the wireframe has no side rail.
        private const float SideRailWidth = 240f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.TemplateWindow) // TEMPLATE: your enum entry
                .SetInitialGeometry(0, 0, 900f, 560f)                        // TEMPLATE: wireframe proportions
                .SetTitle("Template Window")                                 // TEMPLATE: wireframe title
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                // TEMPLATE: one DefinePartialView per tab in the wireframe.
                // TEMPLATE(no-tabs): if the wireframe has NO tab strip, keep exactly
                // ONE DefinePartialView (e.g. MainContentPartial) whose builder holds
                // the ENTIRE body - stacked sections all go inside it as rows.
                .DefinePartialView(TemplateWindowViewModel.FirstTabPartial, AddFirstTab)
                .DefinePartialView(TemplateWindowViewModel.SecondTabPartial, AddSecondTab);

            // Rule R5: never hand-roll the root layout - not even as stacked rows in
            // a root column. AddStandardLayout is mandatory with OR without tabs.
            window.AddStandardLayout(layout =>
            {
                // TEMPLATE(no-tabs): delete SetTabPanelHeight and the AddTabRow block
                // entirely; keep SetContentPartialElement.
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("First")   // TEMPLATE: tab captions from the wireframe
                        .AddOption("Second")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(232f)       // TEMPLATE: ~116f per option
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(TemplateWindowViewModel.TabContentElement);
                // TEMPLATE: delete if no side rail; add more for multiple rails.
                layout.AddSideColumn(AddSideRail, SideRailWidth);
            });

            return _builder.Build();
        }

        // Each tab: fixed-width borderless panel (rule R5). Scrolling comes from
        // the standard layout's content host group.
        private static void AddFirstTab(GuiGroup<TemplateWindowViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content =>
                        {
                            // TEMPLATE: build this tab's widgets here, one AddRow per
                            // wireframe row, using references/wireframe-to-widget-mapping.md.
                            // Rule R2c: no SetHeight on rows containing buttons/inputs.
                            content.AddRow(r => r.AddLabel()
                                .BindText(model => model.StatusText)
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left));
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddSecondTab(GuiGroup<TemplateWindowViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content =>
                        {
                            // TEMPLATE: second tab's widgets.
                            content.AddRow(r => r.AddLabel()
                                .SetText("Second tab")
                                .SetHeight(20f)
                                .SetHorizontalAlign(NuiHorizontalAlign.Left));
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        // TEMPLATE: delete if no side rail. Side rails live in the window root
        // (geometry-bound), so they survive tab swaps.
        private static void AddSideRail(GuiColumn<TemplateWindowViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Details")   // TEMPLATE: rail content from the wireframe
                    .SetHeight(24f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }
    }
}

using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The DM tools that do not fit on the toolbar: the animation tester and the admin windows.
    /// </summary>
    public class DMToolbarMoreDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMToolbarMoreViewModel> _builder = new();

        private const float ContentPanelWidth = 250f;
        private const float ButtonWidth = 230f;
        private const float ButtonHeight = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMToolbarMore)
                .SetInitialGeometry(DMToolbarViewModel.ToolbarX + 8 * (DMToolbarViewModel.ButtonWidth + 8f), 0, 290f, 300f)
                .SetTitle("More DM Tools")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(DMToolbarMoreViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(DMToolbarMoreViewModel.MainContentElement);
            });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<DMToolbarMoreViewModel> host)
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
                            AddButton(content, "Animations", "Search and preview animations on your character",
                                model => model.OnClickAnimations(), null);

                            content.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .SetText("ADMIN")
                                    .SetColor(221, 181, 93)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetHeight(20f);
                            });

                            AddButton(content, "Manage Staff", "Add or remove DMs and admins",
                                model => model.OnClickManageStaff(), model => model.IsAdmin);
                            AddButton(content, "Manage Bans", "Add or remove banned players",
                                model => model.OnClickManageBans(), model => model.IsAdmin);
                            AddButton(content, "Property Diagnostics", "Property loading diagnostics",
                                model => model.OnClickPropertyDiagnostics(), model => model.IsAdmin);
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddButton(
            GuiColumn<DMToolbarMoreViewModel> col,
            string text,
            string tooltip,
            Expression<Func<DMToolbarMoreViewModel, Action>> onClick,
            Expression<Func<DMToolbarMoreViewModel, bool>> isEnabled)
        {
            col.AddRow(row =>
            {
                var button = row.AddButton()
                    .SetText(text)
                    .SetTooltip(tooltip)
                    .SetWidth(ButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(onClick);

                if (isEnabled != null)
                {
                    button.BindIsEnabled(isEnabled)
                        .SetDisabledTooltip("Admins only");
                }
            });
        }
    }
}

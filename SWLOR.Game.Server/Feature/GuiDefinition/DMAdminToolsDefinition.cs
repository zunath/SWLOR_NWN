using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The admin windows, opened from the DM toolbar's Admin button. Only admins see that button.
    /// </summary>
    public class DMAdminToolsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<DMAdminToolsViewModel> _builder = new();

        private const float ContentPanelWidth = 250f;
        private const float ButtonWidth = 230f;
        private const float ButtonHeight = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.DMAdminTools)
                .SetInitialGeometry(DMToolbarViewModel.ToolbarX + DMToolbarViewModel.PublicButtonCount * DMToolbarViewModel.ButtonSlotWidth, 0, 290f, 220f)
                .SetTitle("Admin Tools")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetKeepsOpenOnAreaChange(true)
                .DefinePartialView(DMAdminToolsViewModel.MainContentPartial, AddMainContent);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(DMAdminToolsViewModel.MainContentElement);
            });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<DMAdminToolsViewModel> host)
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
                            AddButton(content, "Manage Staff", "Add or remove DMs and admins",
                                model => model.OnClickManageStaff());
                            AddButton(content, "Manage Bans", "Add or remove banned players",
                                model => model.OnClickManageBans());
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddButton(
            GuiColumn<DMAdminToolsViewModel> col,
            string text,
            string tooltip,
            Expression<Func<DMAdminToolsViewModel, Action>> onClick)
        {
            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText(text)
                    .SetTooltip(tooltip)
                    .SetWidth(ButtonWidth)
                    .SetHeight(ButtonHeight)
                    .BindOnClicked(onClick);
            });
        }
    }
}

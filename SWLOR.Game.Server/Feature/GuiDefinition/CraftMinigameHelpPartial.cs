using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The help page every crafting minigame window shows. A window defines it with
    /// <c>DefinePartialView(CraftMinigameHelp.PartialName, group => CraftMinigameHelpPartial.Build(group, ...))</c>, puts a
    /// <see cref="AddHelpButton{T}"/> in its main view, and derives its view model from <c>CraftMinigameViewModel</c>.
    /// </summary>
    public static class CraftMinigameHelpPartial
    {
        private const float TitleHeight = 32f;
        private const float CloseButtonWidth = 120f;
        private const float CloseButtonHeight = 36f;
        private const float HelpButtonSize = 34f;

        /// <summary>Builds the help page: a title, the text, and a Close button.</summary>
        /// <param name="group">The partial view's group.</param>
        /// <param name="title">The page title, such as "How the Plasma Forge Works".</param>
        /// <param name="body">The help text. Game text is ASCII punctuation only.</param>
        /// <param name="onClose">The view model method that returns to the main view.</param>
        public static void Build<T>(GuiGroup<T> group, string title, string body, Expression<Func<T, Action>> onClose)
            where T : IGuiViewModel
        {
            group.SetShowBorder(false)
                .SetScrollbars(NuiScrollbars.Auto);
            group.AddColumn(column =>
            {
                column.AddRow(row =>
                {
                    row.AddLabel()
                        .SetText(title)
                        .SetHeight(TitleHeight);
                });
                column.AddRow(row =>
                {
                    row.AddText()
                        .SetText(body)
                        .SetShowBorder(false)
                        .SetScrollbars(NuiScrollbars.Auto);
                });
                column.AddRow(row =>
                {
                    row.AddSpacer();
                    row.AddButton()
                        .SetText("Close")
                        .SetWidth(CloseButtonWidth)
                        .SetHeight(CloseButtonHeight)
                        .BindOnClicked(onClose);
                    row.AddSpacer();
                });
            });
        }

        /// <summary>Adds the "?" button that opens the help page to a row of the main view.</summary>
        public static void AddHelpButton<T>(GuiRow<T> row, string tooltip, Expression<Func<T, Action>> onHelp)
            where T : IGuiViewModel
        {
            row.AddButton()
                .SetText("?")
                .SetTooltip(tooltip)
                .SetWidth(HelpButtonSize)
                .SetHeight(HelpButtonSize)
                .BindOnClicked(onHelp);
        }
    }
}

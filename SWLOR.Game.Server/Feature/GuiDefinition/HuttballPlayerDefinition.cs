using System.Linq.Expressions;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.HuttballService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The compact Huttball window for players, shaped like the dice bag so it tucks into a corner of the screen:
    /// team and role pickers with Join, Leave, and Captain, a stats box, Re-roll and Medic, then one large button
    /// per action. Nothing has a set size except the stats box, so the buttons grow and shrink with the window in
    /// both directions.
    /// </summary>
    public class HuttballPlayerDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HuttballPlayerViewModel> _builder = new();

        private const float StatsHeight = 104f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.HuttballPlayer)
                .SetInitialGeometry(0f, 0f, 220f, 580f)
                .SetTitle("Huttball")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddComboBox()
                            .AddOption("Home", 0)
                            .AddOption("Away", 1)
                            .BindSelectedIndex(model => model.TeamSelection)
                            .BindIsEnabled(model => model.CanJoin);
                        row.AddComboBox()
                            .AddOption("Bruiser", 0)
                            .AddOption("Catcher", 1)
                            .AddOption("Thrower", 2)
                            .AddOption("Blocker", 3)
                            .BindSelectedIndex(model => model.RoleSelection);
                    });
                    col.AddRow(row =>
                    {
                        AddButton(row, "Join", "Join the chosen team in the chosen role, between drives. The first player on a team captains it.",
                            model => model.OnJoin(), model => model.CanJoin);
                        AddButton(row, "Leave", "Leave the match.",
                            model => model.OnLeave(), model => model.CanLeave);
                        AddButton(row, "Captain", "Open the captain's window: set up a match with no DM, or run your team's drives and turns.",
                            model => model.OnCaptain(), model => model.CanOpenCaptain);
                    });

                    col.AddRow(row => row.AddText()
                        .BindText(model => model.StatsText)
                        .SetShowBorder(true)
                        .SetHeight(StatsHeight));

                    col.AddRow(row =>
                    {
                        AddButton(row, "Re-roll", "Right after one of your team's rolls: undo it and roll again. You keep the new result.",
                            model => model.OnReroll(), model => model.CanReroll);
                        AddButton(row, "Medic", "Click an injured teammate on the bench: their injury drops one step.",
                            model => model.OnMedic(), model => model.CanUseMedic);
                    });

                    col.AddRow(row => row.AddButton()
                        .BindText(model => model.MoveText)
                        .SetTooltip("Move: click a square. Stand Up: get back on your feet, using this turn's movement.")
                        .BindOnClicked(model => model.OnMoveOrStand())
                        .BindIsEnabled(model => model.CanMoveOrStand));
                    AddActionRow(col, "Block", "Opposed strength roll against an adjacent opponent.",
                        model => model.OnBlock(), model => model.CanBlock);
                    AddActionRow(col, "Pass", $"Throw the ball to a teammate up to {HuttballRules.PassRange} squares away.",
                        model => model.OnPass(), model => model.CanPass);
                    AddActionRow(col, "Dirty Move", "Kick a downed opponent: another injury roll, but the referee may send you off.",
                        model => model.OnDirtyMove(), model => model.CanDirtyMove);
                    AddActionRow(col, "Done", "Finish your turn.",
                        model => model.OnDone(), model => model.CanFinish);
                });

            return _builder.Build();
        }

        private static void AddActionRow(
            GuiColumn<HuttballPlayerViewModel> col,
            string text,
            string tooltip,
            Expression<Func<HuttballPlayerViewModel, Action>> onClick,
            Expression<Func<HuttballPlayerViewModel, bool>> isEnabled)
        {
            col.AddRow(row => AddButton(row, text, tooltip, onClick, isEnabled));
        }

        private static void AddButton(
            GuiRow<HuttballPlayerViewModel> row,
            string text,
            string tooltip,
            Expression<Func<HuttballPlayerViewModel, Action>> onClick,
            Expression<Func<HuttballPlayerViewModel, bool>> isEnabled)
        {
            row.AddButton()
                .SetText(text)
                .SetTooltip(tooltip)
                .BindOnClicked(onClick)
                .BindIsEnabled(isEnabled);
        }
    }
}

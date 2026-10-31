using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.HuttballService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The staff Huttball panel: score, turn, the player being commanded, its actions, the DM's match controls,
    /// and the play-by-play. Opened with /huttball by staff.
    /// </summary>
    /// <remarks>
    /// The rows sit in one group that scrolls up and down only. A group that may also scroll sideways lays its
    /// content out with no width limit, so nothing in it stretches; scrolling only vertically keeps the content as
    /// wide as the window, and the buttons, which have no set width, share each row.
    /// </remarks>
    public class HuttballDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HuttballViewModel> _builder = new();

        private const float LabelHeight = 20f;
        private const float StepButtonWidth = 40f;
        private const float LogHeight = 200f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Huttball)
                .SetInitialGeometry(0f, 0f, 460f, 760f)
                .SetTitle("Huttball")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .AddColumn(col => col.AddRow(row => row.AddGroup(group =>
                {
                    group.SetShowBorder(false);
                    group.SetScrollbars(NuiScrollbars.Y);
                    group.AddColumn(AddMainContent);
                })));

            return _builder.Build();
        }

        private static void AddMainContent(GuiColumn<HuttballViewModel> content)
        {
            AddBoundLabel(content, model => model.ScoreText);
            AddBoundLabel(content, model => model.TurnText);
            AddBoundLabel(content, model => model.BallText);
            AddMatchText(content, model => model.ResourcesText, 44f);
            content.AddRow(r =>
            {
                r.BindIsVisible(model => model.HasMatch);
                r.AddText()
                    .SetText(Huttball.PitchKey)
                    .SetHeight(56f);
            });

            AddSectionLabel(content, "Player", model => model.HasMatch);
            AddMatchText(content, model => model.UnitText, 56f);
            content.AddRow(r =>
            {
                r.BindIsVisible(model => model.HasMatch);
                AddActionButton(r, "Move", "Click a square. In setup, take any free square in your half.",
                    model => model.OnMove(), model => model.CanMove);
                AddActionButton(r, "Block", "Opposed strength roll against an adjacent opponent.",
                    model => model.OnBlock(), model => model.CanBlock);
                AddActionButton(r, "Pass", $"Throw the ball to a teammate up to {HuttballRules.PassRange} squares away.",
                    model => model.OnPass(), model => model.CanPass);
            });
            content.AddRow(r =>
            {
                r.BindIsVisible(model => model.HasMatch);
                AddActionButton(r, "Dirty Move", "Kick a downed opponent: another injury roll, but the referee may send you off.",
                    model => model.OnDirtyMove(), model => model.CanDirtyMove);
                AddActionButton(r, "Stand Up", "Get back on your feet. Uses this turn's movement.",
                    model => model.OnStandUp(), model => model.CanStandUp);
                AddActionButton(r, "Done", "Finish this player's turn.",
                    model => model.OnDone(), model => model.CanFinish);
            });
            content.AddRow(r =>
            {
                r.BindIsVisible(model => model.HasMatch);
                AddActionButton(r, "Re-roll", "Right after one of your team's rolls: undo it and roll again. You keep the new result.",
                    model => model.OnReroll(), model => model.CanReroll);
                AddActionButton(r, "Medic", "Click an injured teammate on the bench: their injury drops one step.",
                    model => model.OnMedic(), model => model.CanUseMedic);
            });

            AddSectionLabel(content, "DM Controls", model => model.IsDM);
            AddDMRow(content, r =>
            {
                AddDMButton(r, "Create Match", "Lay a 15 x 20 pitch over this area's battle grid.", model => model.OnCreateMatch())
                    .BindIsEnabled(model => model.HasNoMatch);
                AddDMButton(r, "End Match", "End the match and clear the pitch, ball, and effects.", model => model.OnEndMatch())
                    .BindIsEnabled(model => model.HasMatch);
                AddDMButton(r, "Test Teams", "For testing: add one test player of each role to each team, lined up in their own halves.", model => model.OnSpawnTestTeams())
                    .BindIsEnabled(model => model.IsSetup);
            });
            AddDMRow(content, r =>
            {
                r.AddComboBox()
                    .AddOption("Home team", 0)
                    .AddOption("Away team", 1)
                    .BindSelectedIndex(model => model.TeamSelection);
                r.AddComboBox()
                    .AddOption("Bruiser", 0)
                    .AddOption("Catcher", 1)
                    .AddOption("Thrower", 2)
                    .AddOption("Blocker", 3)
                    .BindSelectedIndex(model => model.RoleSelection);
            });
            AddDMRow(content, r =>
            {
                AddDMButton(r, "Add Player", "Click a creature to add it to the chosen team and role.", model => model.OnAddPlayer())
                    .BindIsEnabled(model => model.HasMatch);
                AddDMButton(r, "Remove Player", "Click a player to take out of the match.", model => model.OnRemovePlayer())
                    .BindIsEnabled(model => model.HasMatch);
                AddDMButton(r, "Set Role", "Give the selected player the chosen role (between drives).", model => model.OnSetRole())
                    .BindIsEnabled(model => model.IsSetup);
            });
            AddDMRow(content, r =>
            {
                AddDMButton(r, "Select Player", "Click a player to command with the buttons above.", model => model.OnSelectUnit())
                    .BindIsEnabled(model => model.HasMatch);
                AddDMButton(r, "Start Drive", "Click where the ball lands to kick off.", model => model.OnStartDrive())
                    .BindIsEnabled(model => model.IsSetup);
                AddDMButton(r, "End Turn", "End the active team's turn now.", model => model.OnEndTeamTurn())
                    .BindIsEnabled(model => model.IsPlaying);
            });
            AddDMRow(content, r =>
            {
                AddResourceStepper(r, "Re-rolls", "re-roll", model => model.OnRerollDown(), model => model.OnRerollUp());
                AddResourceStepper(r, "Medics", "medic", model => model.OnMedicDown(), model => model.OnMedicUp());
                AddResourceStepper(r, "Bribes", "bribe (one is spent automatically when a dirty move is spotted)",
                    model => model.OnBribeDown(), model => model.OnBribeUp());
            });

            AddSectionLabel(content, "Play-by-Play", model => model.HasMatch);
            content.AddRow(r =>
            {
                r.BindIsVisible(model => model.HasMatch);
                // A set height: in a group that scrolls vertically, a box with none would collapse.
                r.AddText()
                    .BindText(model => model.LogText)
                    .SetShowBorder(true)
                    .SetScrollbars(NuiScrollbars.Auto)
                    .SetHeight(LogHeight);
            });
        }

        private static void AddBoundLabel(GuiColumn<HuttballViewModel> content, Expression<Func<HuttballViewModel, string>> text)
        {
            content.AddRow(r => r.AddLabel()
                .BindText(text)
                .SetHeight(LabelHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        private static void AddMatchText(
            GuiColumn<HuttballViewModel> content,
            Expression<Func<HuttballViewModel, string>> text,
            float height)
        {
            content.AddRow(r =>
            {
                r.BindIsVisible(model => model.HasMatch);
                r.AddText()
                    .BindText(text)
                    .SetHeight(height);
            });
        }

        private static void AddSectionLabel(
            GuiColumn<HuttballViewModel> content,
            string text,
            Expression<Func<HuttballViewModel, bool>> isVisible)
        {
            content.AddRow(r =>
            {
                r.BindIsVisible(isVisible);
                r.AddLabel()
                    .SetText(text)
                    .SetHeight(LabelHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
        }

        /// <summary>
        /// A player action, greyed out whenever the commanded player cannot take it right now. It shares its row's
        /// width with the other buttons in the row.
        /// </summary>
        private static void AddActionButton(
            GuiRow<HuttballViewModel> row,
            string text,
            string tooltip,
            Expression<Func<HuttballViewModel, Action>> onClick,
            Expression<Func<HuttballViewModel, bool>> isEnabled)
        {
            row.AddButton()
                .SetText(text)
                .SetTooltip(tooltip)
                .BindOnClicked(onClick)
                .BindIsEnabled(isEnabled);
        }

        private static void AddDMRow(GuiColumn<HuttballViewModel> content, Action<GuiRow<HuttballViewModel>> build)
        {
            content.AddRow(r =>
            {
                r.BindIsVisible(model => model.IsDM);
                build(r);
            });
        }

        private static GuiButton<HuttballViewModel> AddDMButton(
            GuiRow<HuttballViewModel> row,
            string text,
            string tooltip,
            Expression<Func<HuttballViewModel, Action>> onClick)
        {
            return row.AddButton()
                .SetText(text)
                .SetTooltip(tooltip)
                .BindOnClicked(onClick);
        }

        /// <summary>
        /// A resource name with buttons that take one from, or give one to, the team chosen in the team box. The
        /// name stretches; the buttons stay small.
        /// </summary>
        private static void AddResourceStepper(
            GuiRow<HuttballViewModel> row,
            string label,
            string noun,
            Expression<Func<HuttballViewModel, Action>> onDown,
            Expression<Func<HuttballViewModel, Action>> onUp)
        {
            row.AddLabel()
                .SetText(label)
                .SetHorizontalAlign(NuiHorizontalAlign.Right)
                .SetVerticalAlign(NuiVerticalAlign.Middle);
            row.AddButton()
                .SetText("-")
                .SetTooltip($"Take a {noun} from the chosen team.")
                .SetWidth(StepButtonWidth)
                .BindOnClicked(onDown)
                .BindIsEnabled(model => model.HasMatch);
            row.AddButton()
                .SetText("+")
                .SetTooltip($"Give the chosen team a {noun}.")
                .SetWidth(StepButtonWidth)
                .BindOnClicked(onUp)
                .BindIsEnabled(model => model.HasMatch);
        }
    }
}

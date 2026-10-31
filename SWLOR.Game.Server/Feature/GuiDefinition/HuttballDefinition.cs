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
    /// The staff Huttball window, opened with /huttball by staff. Like the Appearance Editor it is split into tabs:
    /// Match (score, drives, turns, team resources), Players (adding players and acting for the selected one), and
    /// Play-by-Play. Each tab is a fixed-width panel; the window scrolls when it is smaller than the panel.
    /// </summary>
    public class HuttballDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HuttballViewModel> _builder = new();

        private const float TabRowHeight = 28f;
        private const float TabPanelHeight = 48f;
        private const float TabWidth = 120f;
        private const float PanelWidth = 440f;
        private const float LabelHeight = 20f;
        private const float ButtonWidth = 136f;
        private const float ComboWidth = 208f;
        private const float StepLabelWidth = 100f;
        private const float StepButtonWidth = 40f;
        private const float LogHeight = 360f;
        private const float ControlHeight = 32f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.Huttball)
                .SetInitialGeometry(0f, 0f, 480f, 520f)
                .SetTitle("Huttball")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(HuttballViewModel.MatchTabPartial, AddMatchTab)
                .DefinePartialView(HuttballViewModel.PlayersTabPartial, AddPlayersTab)
                .DefinePartialView(HuttballViewModel.LogTabPartial, AddLogTab);

            window.AddStandardLayout(layout =>
            {
                layout.SetTabPanelHeight(TabPanelHeight);
                layout.AddTabRow(row =>
                {
                    row.SetHeight(TabRowHeight);
                    row.AddToggles()
                        .AddOption("Match")
                        .AddOption("Players")
                        .AddOption("Play-by-Play")
                        .BindSelectedValue(model => model.TabToggleValue)
                        .SetWidth(TabWidth * 3)
                        .SetHeight(TabRowHeight);
                });
                layout.SetContentPartialElement(HuttballViewModel.TabContentElement);
            });

            return _builder.Build();
        }

        /// <summary>
        /// Score, turn, and ball; creating and ending the match; drives and turns; and each team's resources.
        /// </summary>
        private static void AddMatchTab(GuiGroup<HuttballViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddBoundLabel(content, model => model.ScoreText);
                AddBoundLabel(content, model => model.TurnText);
                AddBoundLabel(content, model => model.BallText);
                content.AddRow(r => r.AddText()
                    .SetText(Huttball.PitchKey)
                    .SetHeight(56f));

                AddSectionLabel(content, "Match");
                content.AddRow(r =>
                {
                    AddButton(r, "Create Match", "Lay a 15 x 20 pitch over this area's battle grid.", model => model.OnCreateMatch())
                        .BindIsEnabled(model => model.HasNoMatch);
                    AddButton(r, "End Match", "End the match and clear the pitch, ball, and effects.", model => model.OnEndMatch())
                        .BindIsEnabled(model => model.HasMatch);
                    AddButton(r, "Test Teams", "For testing: add one test player of each role to each team, lined up in their own halves.", model => model.OnSpawnTestTeams())
                        .BindIsEnabled(model => model.IsSetup);
                    r.AddSpacer();
                });
                content.AddRow(r =>
                {
                    AddButton(r, "Start Drive", "Click where the ball lands to kick off.", model => model.OnStartDrive())
                        .BindIsEnabled(model => model.IsSetup);
                    AddButton(r, "End Turn", "End the active team's turn now.", model => model.OnEndTeamTurn())
                        .BindIsEnabled(model => model.IsPlaying);
                    r.AddSpacer();
                });

                AddSectionLabel(content, "Team Resources");
                content.AddRow(r => r.AddText()
                    .BindText(model => model.ResourcesText)
                    .SetHeight(44f));
                content.AddRow(r =>
                {
                    AddTeamCombo(r);
                    r.AddSpacer();
                });
                AddResourceStepper(content, "Re-rolls", "re-roll", model => model.OnRerollDown(), model => model.OnRerollUp());
                AddResourceStepper(content, "Medics", "medic", model => model.OnMedicDown(), model => model.OnMedicUp());
                AddResourceStepper(content, "Bribes", "bribe (one is spent automatically when a dirty move is spotted)",
                    model => model.OnBribeDown(), model => model.OnBribeUp());
            });
        }

        /// <summary>
        /// Adding and removing players, picking the one to command, and that player's actions.
        /// </summary>
        private static void AddPlayersTab(GuiGroup<HuttballViewModel> host)
        {
            AddPanel(host, content =>
            {
                AddBoundLabel(content, model => model.TurnText);

                AddSectionLabel(content, "Roster");
                content.AddRow(r =>
                {
                    AddTeamCombo(r);
                    r.AddComboBox()
                        .AddOption("Bruiser", 0)
                        .AddOption("Catcher", 1)
                        .AddOption("Thrower", 2)
                        .AddOption("Blocker", 3)
                        .BindSelectedIndex(model => model.RoleSelection)
                        .SetWidth(ComboWidth)
                        .SetHeight(ControlHeight);
                    r.AddSpacer();
                });
                content.AddRow(r =>
                {
                    AddButton(r, "Add Player", "Click a creature to add it to the chosen team and role.", model => model.OnAddPlayer())
                        .BindIsEnabled(model => model.HasMatch);
                    AddButton(r, "Remove Player", "Click a player to take out of the match.", model => model.OnRemovePlayer())
                        .BindIsEnabled(model => model.HasMatch);
                    AddButton(r, "Set Role", "Give the selected player the chosen role (between drives).", model => model.OnSetRole())
                        .BindIsEnabled(model => model.IsSetup);
                    r.AddSpacer();
                });

                AddSectionLabel(content, "Selected Player");
                content.AddRow(r =>
                {
                    AddButton(r, "Select Player", "Click a player to command with the buttons below.", model => model.OnSelectUnit())
                        .BindIsEnabled(model => model.HasMatch);
                    r.AddSpacer();
                });
                content.AddRow(r => r.AddText()
                    .BindText(model => model.UnitText)
                    .SetHeight(56f));
                content.AddRow(r =>
                {
                    AddActionButton(r, "Move", "Click a square. In setup, take any free square in your half.",
                        model => model.OnMove(), model => model.CanMove);
                    AddActionButton(r, "Block", "Opposed strength roll against an adjacent opponent.",
                        model => model.OnBlock(), model => model.CanBlock);
                    AddActionButton(r, "Pass", $"Throw the ball to a teammate up to {HuttballRules.PassRange} squares away.",
                        model => model.OnPass(), model => model.CanPass);
                    r.AddSpacer();
                });
                content.AddRow(r =>
                {
                    AddActionButton(r, "Dirty Move", "Kick a downed opponent: another injury roll, but the referee may send you off.",
                        model => model.OnDirtyMove(), model => model.CanDirtyMove);
                    AddActionButton(r, "Stand Up", "Get back on your feet. Uses this turn's movement.",
                        model => model.OnStandUp(), model => model.CanStandUp);
                    AddActionButton(r, "Done", "Finish this player's turn.",
                        model => model.OnDone(), model => model.CanFinish);
                    r.AddSpacer();
                });
                content.AddRow(r =>
                {
                    AddActionButton(r, "Re-roll", "Right after one of this team's rolls: undo it and roll again. The new result stands.",
                        model => model.OnReroll(), model => model.CanReroll);
                    AddActionButton(r, "Medic", "Click an injured teammate on the bench: their injury drops one step.",
                        model => model.OnMedic(), model => model.CanUseMedic);
                    r.AddSpacer();
                });
            });
        }

        /// <summary>
        /// The match's play-by-play, newest first.
        /// </summary>
        private static void AddLogTab(GuiGroup<HuttballViewModel> host)
        {
            AddPanel(host, content =>
            {
                content.AddRow(r => r.AddText()
                    .BindText(model => model.LogText)
                    .SetShowBorder(true)
                    .SetScrollbars(NuiScrollbars.Auto)
                    .SetHeight(LogHeight));
            });
        }

        /// <summary>
        /// A tab's fixed-width borderless panel (rule R5). Scrolling comes from the standard layout's host group.
        /// </summary>
        private static void AddPanel(GuiGroup<HuttballViewModel> host, Action<GuiColumn<HuttballViewModel>> build)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(build);
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }

        private static void AddTeamCombo(GuiRow<HuttballViewModel> row)
        {
            row.AddComboBox()
                .AddOption("Home team", 0)
                .AddOption("Away team", 1)
                .BindSelectedIndex(model => model.TeamSelection)
                .SetWidth(ComboWidth)
                .SetHeight(ControlHeight);
        }

        private static void AddBoundLabel(GuiColumn<HuttballViewModel> content, Expression<Func<HuttballViewModel, string>> text)
        {
            content.AddRow(r => r.AddLabel()
                .BindText(text)
                .SetHeight(LabelHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        private static void AddSectionLabel(GuiColumn<HuttballViewModel> content, string text)
        {
            content.AddRow(r => r.AddLabel()
                .SetText(text)
                .SetHeight(LabelHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        private static GuiButton<HuttballViewModel> AddButton(
            GuiRow<HuttballViewModel> row,
            string text,
            string tooltip,
            Expression<Func<HuttballViewModel, Action>> onClick)
        {
            return row.AddButton()
                .SetText(text)
                .SetTooltip(tooltip)
                .SetWidth(ButtonWidth)
                .SetHeight(ControlHeight)
                .BindOnClicked(onClick);
        }

        /// <summary>
        /// An action for the selected player, greyed out whenever that player cannot take it right now.
        /// </summary>
        private static void AddActionButton(
            GuiRow<HuttballViewModel> row,
            string text,
            string tooltip,
            Expression<Func<HuttballViewModel, Action>> onClick,
            Expression<Func<HuttballViewModel, bool>> isEnabled)
        {
            AddButton(row, text, tooltip, onClick)
                .BindIsEnabled(isEnabled);
        }

        /// <summary>
        /// A resource name with buttons that take one from, or give one to, the team chosen in the team box.
        /// </summary>
        private static void AddResourceStepper(
            GuiColumn<HuttballViewModel> content,
            string label,
            string noun,
            Expression<Func<HuttballViewModel, Action>> onDown,
            Expression<Func<HuttballViewModel, Action>> onUp)
        {
            content.AddRow(r =>
            {
                r.AddLabel()
                    .SetText(label)
                    .SetWidth(StepLabelWidth)
                    .SetHeight(ControlHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                r.AddButton()
                    .SetText("-")
                    .SetTooltip($"Take a {noun} from the chosen team.")
                    .SetWidth(StepButtonWidth)
                    .SetHeight(ControlHeight)
                    .BindOnClicked(onDown)
                    .BindIsEnabled(model => model.HasMatch);
                r.AddButton()
                    .SetText("+")
                    .SetTooltip($"Give the chosen team a {noun}.")
                    .SetWidth(StepButtonWidth)
                    .SetHeight(ControlHeight)
                    .BindOnClicked(onUp)
                    .BindIsEnabled(model => model.HasMatch);
                r.AddSpacer();
            });
        }
    }
}

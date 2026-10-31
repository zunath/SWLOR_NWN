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
    /// The Huttball match panel: score, turn, the player being commanded, its actions, the DM's match controls,
    /// and the play-by-play. Opened with /huttball.
    /// </summary>
    public class HuttballDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<HuttballViewModel> _builder = new();

        private const float ContentPanelWidth = 420f;
        private const float LabelHeight = 20f;
        private const float ButtonWidth = 130f;
        private const float ComboWidth = 150f;
        private const float ResourceLabelWidth = 100f;
        private const float StepButtonSize = 40f;

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Huttball)
                .SetInitialGeometry(0f, 0f, 460f, 720f)
                .SetTitle("Huttball")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .DefinePartialView(HuttballViewModel.MainContentPartial, AddMainContent)
                .AddStandardLayout(layout =>
                {
                    layout.SetContentPartialElement(HuttballViewModel.MainContentElement);
                });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<HuttballViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.Auto);
                        panel.AddColumn(content =>
                        {
                            AddBoundLabel(content, model => model.ScoreText);
                            AddBoundLabel(content, model => model.TurnText);
                            AddBoundLabel(content, model => model.BallText);
                            content.AddRow(r => r.AddText()
                                .BindText(model => model.ResourcesText)
                                .BindIsVisible(model => model.HasMatch)
                                .SetHeight(44f));
                            content.AddRow(r => r.AddText()
                                .SetText(Huttball.PitchKey)
                                .BindIsVisible(model => model.HasMatch)
                                .SetHeight(56f));

                            AddSectionLabel(content, "Player", model => model.HasMatch);
                            content.AddRow(r => r.AddText()
                                .BindText(model => model.UnitText)
                                .BindIsVisible(model => model.HasMatch)
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
                                AddActionButton(r, "Re-roll", "Right after one of your team's rolls: undo it and roll again. You keep the new result.",
                                    model => model.OnReroll(), model => model.CanReroll);
                                AddActionButton(r, "Medic", "Click an injured teammate on the bench: their injury drops one step.",
                                    model => model.OnMedic(), model => model.CanUseMedic);
                                r.AddSpacer();
                            });

                            AddSectionLabel(content, "DM Controls", model => model.IsDM);
                            content.AddRow(r =>
                            {
                                AddDMButton(r, "Create Match", "Turn this area's battle grid into a pitch.", model => model.OnCreateMatch())
                                    .BindIsEnabled(model => model.HasNoMatch);
                                AddDMButton(r, "End Match", "End the match and clear the ball and effects.", model => model.OnEndMatch())
                                    .BindIsEnabled(model => model.HasMatch);
                                AddDMButton(r, "Spawn Test Teams", "For testing: add one test player of each role to each team, lined up in their own halves.", model => model.OnSpawnTestTeams())
                                    .BindIsEnabled(model => model.IsSetup);
                                r.AddSpacer();
                            });
                            content.AddRow(r =>
                            {
                                r.AddComboBox()
                                    .AddOption("Home team", 0)
                                    .AddOption("Away team", 1)
                                    .BindSelectedIndex(model => model.TeamSelection)
                                    .BindIsVisible(model => model.IsDM)
                                    .SetWidth(ComboWidth);
                                r.AddComboBox()
                                    .AddOption("Bruiser", 0)
                                    .AddOption("Catcher", 1)
                                    .AddOption("Thrower", 2)
                                    .AddOption("Blocker", 3)
                                    .BindSelectedIndex(model => model.RoleSelection)
                                    .BindIsVisible(model => model.IsDM)
                                    .SetWidth(ComboWidth);
                                r.AddSpacer();
                            });
                            content.AddRow(r =>
                            {
                                AddDMButton(r, "Add Player", "Click a creature to add it to the chosen team and role.", model => model.OnAddPlayer())
                                    .BindIsEnabled(model => model.HasMatch);
                                AddDMButton(r, "Remove Player", "Click a player to take out of the match.", model => model.OnRemovePlayer())
                                    .BindIsEnabled(model => model.HasMatch);
                                AddDMButton(r, "Set Role", "Give the selected player the chosen role (between drives).", model => model.OnSetRole())
                                    .BindIsEnabled(model => model.IsSetup);
                                r.AddSpacer();
                            });
                            content.AddRow(r =>
                            {
                                AddDMButton(r, "Select Player", "Click a player to command with the buttons above.", model => model.OnSelectUnit())
                                    .BindIsEnabled(model => model.HasMatch);
                                AddDMButton(r, "Start Drive", "Click where the ball lands to kick off.", model => model.OnStartDrive())
                                    .BindIsEnabled(model => model.IsSetup);
                                AddDMButton(r, "End Team Turn", "End the active team's turn now.", model => model.OnEndTeamTurn())
                                    .BindIsEnabled(model => model.IsPlaying);
                                r.AddSpacer();
                            });
                            AddResourceStepper(content, "Re-rolls", "re-roll",
                                model => model.OnRerollDown(), model => model.OnRerollUp());
                            AddResourceStepper(content, "Medics", "medic",
                                model => model.OnMedicDown(), model => model.OnMedicUp());
                            AddResourceStepper(content, "Bribes", "bribe (one is spent automatically when a dirty move is spotted)",
                                model => model.OnBribeDown(), model => model.OnBribeUp());

                            AddSectionLabel(content, "Play-by-Play", model => model.HasMatch);
                            content.AddRow(r => r.AddText()
                                .BindText(model => model.LogText)
                                .BindIsVisible(model => model.HasMatch)
                                .SetShowBorder(true)
                                .SetScrollbars(NuiScrollbars.Auto)
                                .SetHeight(220f));
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddBoundLabel(GuiColumn<HuttballViewModel> content, Expression<Func<HuttballViewModel, string>> text)
        {
            content.AddRow(r => r.AddLabel()
                .BindText(text)
                .SetHeight(LabelHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        private static void AddSectionLabel(
            GuiColumn<HuttballViewModel> content,
            string text,
            Expression<Func<HuttballViewModel, bool>> isVisible)
        {
            content.AddRow(r => r.AddLabel()
                .SetText(text)
                .BindIsVisible(isVisible)
                .SetHeight(LabelHeight)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        /// <summary>
        /// A player action, greyed out whenever the commanded player cannot take it right now.
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
                .SetWidth(ButtonWidth)
                .BindOnClicked(onClick)
                .BindIsEnabled(isEnabled)
                .BindIsVisible(model => model.HasMatch);
        }

        /// <summary>
        /// A DM row that takes one of a resource from, or gives one to, the team chosen in the team box.
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
                    .BindIsVisible(model => model.IsDM)
                    .SetWidth(ResourceLabelWidth)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                r.AddButton()
                    .SetText("-")
                    .SetTooltip($"Take a {noun} from the chosen team.")
                    .SetWidth(StepButtonSize)
                    .BindOnClicked(onDown)
                    .BindIsVisible(model => model.IsDM)
                    .BindIsEnabled(model => model.HasMatch);
                r.AddButton()
                    .SetText("+")
                    .SetTooltip($"Give the chosen team a {noun}.")
                    .SetWidth(StepButtonSize)
                    .BindOnClicked(onUp)
                    .BindIsVisible(model => model.IsDM)
                    .BindIsEnabled(model => model.HasMatch);
                r.AddSpacer();
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
                .SetWidth(ButtonWidth)
                .BindOnClicked(onClick)
                .BindIsVisible(model => model.IsDM);
        }
    }
}

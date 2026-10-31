using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HuttballService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The staff Huttball panel: runs the match and acts with any player in it, such as the creatures of the team
    /// a DM controls. Players use the compact <see cref="HuttballPlayerViewModel"/> instead.
    /// </summary>
    public class HuttballViewModel : GuiViewModelBase<HuttballViewModel, GuiPayloadBase>,
        IGuiRefreshable<HuttballRefreshEvent>
    {
        private uint _selectedUnit = OBJECT_INVALID;
        private bool _isStaff;

        public string ScoreText { get => Get<string>(); set => Set(value); }
        public string TurnText { get => Get<string>(); set => Set(value); }
        public string BallText { get => Get<string>(); set => Set(value); }
        public string UnitText { get => Get<string>(); set => Set(value); }
        public string LogText { get => Get<string>(); set => Set(value); }
        public bool IsDM { get => Get<bool>(); set => Set(value); }
        public bool HasMatch { get => Get<bool>(); set => Set(value); }
        public bool HasNoMatch { get => Get<bool>(); set => Set(value); }
        public bool CanMove { get => Get<bool>(); set => Set(value); }
        public bool CanBlock { get => Get<bool>(); set => Set(value); }
        public bool CanPass { get => Get<bool>(); set => Set(value); }
        public bool CanDirtyMove { get => Get<bool>(); set => Set(value); }
        public bool CanStandUp { get => Get<bool>(); set => Set(value); }
        public bool CanFinish { get => Get<bool>(); set => Set(value); }
        public bool CanReroll { get => Get<bool>(); set => Set(value); }
        public bool CanUseMedic { get => Get<bool>(); set => Set(value); }
        public string ResourcesText { get => Get<string>(); set => Set(value); }
        public bool IsSetup { get => Get<bool>(); set => Set(value); }
        public bool IsPlaying { get => Get<bool>(); set => Set(value); }

        /// <summary>The team a DM adds players to: 0 Home, 1 Away.</summary>
        public int TeamSelection { get => Get<int>(); set => Set(value); }

        /// <summary>The role a DM gives added players, in <see cref="HuttballRole"/> order from 0.</summary>
        public int RoleSelection { get => Get<int>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            // The window has no swappable partial: its layout is already on screen, so the combo values set here
            // reach controls that exist.
            _isStaff = CheckIsStaff();
            TeamSelection = 0;
            RoleSelection = 0;
            Refresh();
            WatchOnClient(model => model.TeamSelection);
            WatchOnClient(model => model.RoleSelection);
        }

        public void Refresh(HuttballRefreshEvent payload)
        {
            Refresh();
        }

        private uint Controlled => DMPresence.GetControlledCreature(Player);
        private uint MatchArea => GetArea(Controlled);
        private HuttballMatch Match => Huttball.GetMatch(MatchArea);
        private bool UserIsDM => _isStaff;

        // Checked once per opening, since every match change refreshes every open panel.
        private bool CheckIsStaff() => Huttball.IsStaff(Player);
        private HuttballTeam SelectedTeam => TeamSelection == 1 ? HuttballTeam.Away : HuttballTeam.Home;
        private HuttballRole SelectedRole => (HuttballRole)(RoleSelection + 1);

        /// <summary>
        /// The piece this window commands: a player's own character, or the player a DM picked.
        /// </summary>
        private HuttballPlayer GetUnit()
        {
            var match = Match;
            if (match == null)
                return null;

            return match.GetPlayer(UserIsDM ? _selectedUnit : Controlled);
        }

        private void Refresh()
        {
            var match = Match;
            IsDM = UserIsDM;
            HasMatch = match != null;
            HasNoMatch = match == null;

            if (match == null)
            {
                ScoreText = "No Huttball match in this area.";
                TurnText = IsDM ? "Place a battle grid (/battlegrid), then create a match." : "Ask a DM to set up a match.";
                BallText = string.Empty;
                UnitText = string.Empty;
                LogText = string.Empty;
                ResourcesText = string.Empty;
                IsSetup = IsPlaying = false;
                CanMove = CanBlock = CanPass = CanDirtyMove = CanStandUp = CanFinish = CanReroll = CanUseMedic = false;
                return;
            }

            IsSetup = match.Phase == HuttballPhase.Setup;
            IsPlaying = match.Phase == HuttballPhase.Playing;
            ScoreText = $"Score: Home (blue) {match.GetScore(HuttballTeam.Home)} - Away (red) {match.GetScore(HuttballTeam.Away)}";
            ResourcesText = $"{DescribeResources(match, HuttballTeam.Home)}\n{DescribeResources(match, HuttballTeam.Away)}";
            TurnText = match.Phase switch
            {
                HuttballPhase.Setup => $"Half {match.Half}, turn {match.Turn}: take your places. The {match.ActiveTeam} team moves first.",
                HuttballPhase.Playing => $"Half {match.Half}, turn {match.Turn} of {HuttballRules.TurnsPerHalf}: the {match.ActiveTeam} team's move.",
                _ => "Full time.",
            };
            BallText = match.BallCarrier != null
                ? $"Ball: held by {match.BallCarrier.DisplayName} ({match.BallCarrier.Team})."
                : match.LooseBall != null
                    ? $"Ball: loose at {match.LooseBall.Value}."
                    : "Ball: not in play.";

            var unit = GetUnit();
            UnitText = DescribeUnit(match, unit);
            CanMove = unit != null && match.CanMove(unit);
            CanBlock = unit != null && match.CanBlock(unit);
            CanPass = unit != null && match.CanPass(unit);
            CanDirtyMove = unit != null && match.CanDirtyMove(unit);
            CanStandUp = unit != null && match.CanStandUp(unit);
            CanFinish = unit != null && match.CanFinish(unit);

            var team = GetResourceTeam();
            CanReroll = team != null && match.CanReroll(team.Value);
            CanUseMedic = team != null && match.CanUseMedic(team.Value);
            LogText = string.Join("\n", match.Log.Reverse());
        }

        private static string DescribeResources(HuttballMatch match, HuttballTeam team)
        {
            return $"{team}: re-rolls {match.GetRerolls(team)}, medics {match.GetMedics(team)}, bribes {match.GetBribes(team)}";
        }

        /// <summary>
        /// The team whose re-rolls and medics this window spends: a player's own team, or the team of the player a
        /// DM selected.
        /// </summary>
        private HuttballTeam? GetResourceTeam()
        {
            return GetUnit()?.Team;
        }

        private string DescribeUnit(HuttballMatch match, HuttballPlayer unit)
        {
            if (unit == null)
                return IsDM ? "No player selected. Use Select Player." : "You are not in this match.";

            var where = unit.Square != null ? $"at {unit.Square.Value}" : "off the pitch";
            var state = unit.Status == HuttballPlayerStatus.Standing ? "standing" : HuttballMatch.DescribeStatus(unit.Status);
            var stats = unit.Stats;
            var text = $"{unit.DisplayName}, {unit.Team} team, {state} {where}. " +
                       $"STR +{stats.Strength}, AGI +{stats.Agility}, move {stats.Movement}.";

            if (match.Phase == HuttballPhase.Playing && unit.Team == match.ActiveTeam)
            {
                text += unit.IsDone
                    ? " Done this turn."
                    : $" {unit.MovementLeft} move left, {(unit.HasActed ? "action used" : "action ready")}.";
            }

            return text;
        }

        private void Run(
            System.Func<HuttballMatch, HuttballActionResult> command,
            uint mover = OBJECT_INVALID,
            uint striker = OBJECT_INVALID,
            uint struck = OBJECT_INVALID)
        {
            Huttball.Command(Player, command, mover, striker, struck);
            Refresh();
        }

        private bool RequireUnit(out HuttballPlayer unit)
        {
            unit = GetUnit();
            if (unit != null)
                return true;

            DMPresence.SendMessage(Player, ColorToken.Red(UserIsDM
                ? "Select a player in the match first."
                : "You are not in this match."));
            return false;
        }

        private void PickSquare(string message, System.Action<HuttballSquare> onPicked)
        {
            Huttball.PickSquare(Player, message, onPicked);
        }

        private void PickCreature(string message, System.Action<uint> onPicked)
        {
            Huttball.PickCreature(Player, message, onPicked);
        }

        public Action OnMove() => () =>
        {
            if (!RequireUnit(out var unit))
                return;

            var prompt = Match.Phase == HuttballPhase.Setup
                ? $"Click a square in your own half for {unit.Name}."
                : $"Click where {unit.Name} should move ({unit.MovementLeft} squares left).";
            PickSquare(prompt, square => Run(match => match.Move(unit.Creature, square), unit.Creature));
        };

        public Action OnBlock() => () =>
        {
            if (!RequireUnit(out var unit))
                return;

            PickCreature($"Click an opponent next to {unit.Name} to block.",
                target => Run(match => match.Block(unit.Creature, target), striker: unit.Creature, struck: target));
        };

        public Action OnPass() => () =>
        {
            if (!RequireUnit(out var unit))
                return;

            PickCreature($"Click a teammate within {HuttballRules.PassRange} squares to throw to.",
                target => Run(match => match.Pass(unit.Creature, target)));
        };

        public Action OnDirtyMove() => () =>
        {
            if (!RequireUnit(out var unit))
                return;

            PickCreature("Click a downed opponent next to you. The referee may be watching...",
                target => Run(match => match.DirtyMove(unit.Creature, target), striker: unit.Creature, struck: target));
        };

        public Action OnStandUp() => () =>
        {
            if (RequireUnit(out var unit))
                Run(match => match.StandUp(unit.Creature));
        };

        public Action OnDone() => () =>
        {
            if (RequireUnit(out var unit))
                Run(match => match.FinishActivation(unit.Creature));
        };

        public Action OnCreateMatch() => () =>
        {
            if (!UserIsDM)
                return;

            var error = Huttball.CreateMatch(MatchArea);
            if (error != null)
                DMPresence.SendMessage(Player, ColorToken.Red(error));
            Refresh();
        };

        public Action OnEndMatch() => () =>
        {
            if (!UserIsDM)
                return;

            Huttball.EndMatch(MatchArea);
            _selectedUnit = OBJECT_INVALID;
            Refresh();
        };

        public Action OnAddPlayer() => () =>
        {
            if (!UserIsDM)
                return;

            var team = SelectedTeam;
            var role = SelectedRole;
            PickCreature($"Click a creature to add to the {team} team as a {role}.", target =>
            {
                if (GetArea(target) != MatchArea)
                {
                    DMPresence.SendMessage(Player, ColorToken.Red("Pick a creature in this area."));
                    return;
                }

                var name = GetName(target);
                var other = Huttball.FindMatchArea(target);
                if (GetIsObjectValid(other) && other != MatchArea)
                {
                    DMPresence.SendMessage(Player, ColorToken.Red($"{name} is already playing in another match."));
                    return;
                }

                _selectedUnit = target;
                Run(match => match.AddPlayer(target, name, team, role));
            });
        };

        public Action OnRemovePlayer() => () =>
        {
            if (!UserIsDM)
                return;

            PickCreature("Click a player to take out of the match.", target =>
            {
                if (target == _selectedUnit)
                    _selectedUnit = OBJECT_INVALID;
                Huttball.ClearEffects(target);
                Run(match => match.RemovePlayer(target));

                // Test players exist only for the match.
                if (GetTag(target) == Huttball.TestPlayerTag)
                    DestroyObject(target);
            });
        };

        public Action OnSpawnTestTeams() => () =>
        {
            if (!UserIsDM)
                return;

            Huttball.SpawnTestTeams(MatchArea, Player);
            Refresh();
        };

        public Action OnSetRole() => () =>
        {
            if (!UserIsDM || !RequireUnit(out var unit))
                return;

            var role = SelectedRole;
            Run(match => match.SetRole(unit.Creature, role));
        };

        public Action OnSelectUnit() => () =>
        {
            if (!UserIsDM)
                return;

            PickCreature("Click the player you want to command.", target =>
            {
                if (Match?.GetPlayer(target) == null)
                {
                    DMPresence.SendMessage(Player, ColorToken.Red($"{GetName(target)} is not in this match."));
                    return;
                }

                _selectedUnit = target;
                Refresh();
            });
        };

        public Action OnStartDrive() => () =>
        {
            if (!UserIsDM)
                return;

            PickSquare("Click the square where the ball lands for the kick-off.",
                square => Run(match => match.StartDrive(square)));
        };

        public Action OnEndTeamTurn() => () =>
        {
            if (UserIsDM)
                Run(match => match.EndTeamTurn());
        };

        /// <summary>
        /// Undoes the team's last roll and rolls it again, spending one of the team's re-rolls.
        /// </summary>
        public Action OnReroll() => () =>
        {
            var team = GetResourceTeam();
            if (team == null)
            {
                RequireUnit(out _);
                return;
            }

            Run(match => match.Reroll(team.Value));
        };

        /// <summary>
        /// Sends the team's medic to an injured teammate on the bench.
        /// </summary>
        public Action OnMedic() => () =>
        {
            var team = GetResourceTeam();
            if (team == null)
            {
                RequireUnit(out _);
                return;
            }

            PickCreature($"Click an injured {team} teammate on the bench for the medic.",
                target => Run(match => match.UseMedic(team.Value, target)));
        };

        public Action OnBribeDown() => () => ChangeResource((match, team) => match.SetBribes(team, match.GetBribes(team) - 1));

        public Action OnBribeUp() => () => ChangeResource((match, team) => match.SetBribes(team, match.GetBribes(team) + 1));

        public Action OnRerollDown() => () => ChangeResource((match, team) => match.SetRerolls(team, match.GetRerolls(team) - 1));

        public Action OnRerollUp() => () => ChangeResource((match, team) => match.SetRerolls(team, match.GetRerolls(team) + 1));

        public Action OnMedicDown() => () => ChangeResource((match, team) => match.SetMedics(team, match.GetMedics(team) - 1));

        public Action OnMedicUp() => () => ChangeResource((match, team) => match.SetMedics(team, match.GetMedics(team) + 1));

        /// <summary>
        /// Changes a resource of the team chosen in the DM team box, then updates the scoreboards and panels.
        /// </summary>
        private void ChangeResource(System.Action<HuttballMatch, HuttballTeam> change)
        {
            if (!UserIsDM)
                return;

            var match = Match;
            if (match == null)
                return;

            change(match, SelectedTeam);
            Huttball.ResourcesChanged(MatchArea);
        }
    }
}

using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HuttballService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Huttball match panel. Players use it to act with their own character; DMs also use it to run the match
    /// and to act with any player in it, such as the creatures of the team they control.
    /// </summary>
    public class HuttballViewModel : GuiViewModelBase<HuttballViewModel, GuiPayloadBase>,
        IGuiRefreshable<HuttballRefreshEvent>
    {
        public const string MainContentElement = "huttball_content";
        public const string MainContentPartial = "HUTTBALL_MAIN";

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
        public bool CanAct { get => Get<bool>(); set => Set(value); }
        public bool IsSetup { get => Get<bool>(); set => Set(value); }
        public bool IsPlaying { get => Get<bool>(); set => Set(value); }

        /// <summary>The team a DM adds players to: 0 Home, 1 Away.</summary>
        public int TeamSelection { get => Get<int>(); set => Set(value); }

        /// <summary>The role a DM gives added players, in <see cref="HuttballRole"/> order from 0.</summary>
        public int RoleSelection { get => Get<int>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _isStaff = CheckIsStaff();
            TeamSelection = 0;
            RoleSelection = 0;
            Refresh();
            WatchOnClient(model => model.TeamSelection);
            WatchOnClient(model => model.RoleSelection);

            // Combo values are input binds, which the client drops when they arrive before the layout that shows them.
            SwapNestedPartialView(MainContentElement, MainContentPartial, onAfterApply: () =>
            {
                TeamSelection = TeamSelection;
                RoleSelection = RoleSelection;
            });
        }

        public void Refresh(HuttballRefreshEvent payload)
        {
            Refresh();
        }

        private uint Controlled => DMPresence.GetControlledCreature(Player);
        private uint MatchArea => GetArea(Controlled);
        private HuttballMatch Match => Huttball.GetMatch(MatchArea);
        private bool UserIsDM => _isStaff;

        /// <summary>
        /// Staff run matches, as they may run the battle grid: DMs, and staff on their own characters. Staff are
        /// matched by CD key, which resolves from the creature they control. Checked once per opening, since every
        /// match change refreshes every open panel.
        /// </summary>
        private bool CheckIsStaff()
        {
            var controlled = Controlled;
            if (string.IsNullOrEmpty(GetPCPublicCDKey(controlled)))
                return false;

            var level = Authorization.GetAuthorizationLevel(controlled);
            return level.HasFlag(AuthorizationLevel.DM) || level.HasFlag(AuthorizationLevel.Admin);
        }
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
                CanAct = IsSetup = IsPlaying = false;
                return;
            }

            IsSetup = match.Phase == HuttballPhase.Setup;
            IsPlaying = match.Phase == HuttballPhase.Playing;
            ScoreText = $"Home {match.GetScore(HuttballTeam.Home)} - Away {match.GetScore(HuttballTeam.Away)}" +
                        $"   (bribes: Home {match.GetBribes(HuttballTeam.Home)}, Away {match.GetBribes(HuttballTeam.Away)})";
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
            CanAct = unit != null;
            LogText = string.Join("\n", match.Log.Reverse());
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
            var match = Match;
            if (match == null)
            {
                DMPresence.SendMessage(Player, ColorToken.Red("There is no Huttball match in this area."));
                return;
            }

            Huttball.Apply(MatchArea, command(match), Player, mover, striker, struck);
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

        /// <summary>
        /// Asks for a square of the pitch: the ground, or anything standing on it.
        /// </summary>
        private void PickSquare(string message, System.Action<HuttballSquare> onPicked)
        {
            var controlled = Controlled;
            Targeting.EnterTargetingMode(controlled, ObjectType.All, message, target =>
            {
                // A ground pick arrives as the area object, the way the battle grid window reads it.
                var area = GetArea(controlled);
                var pitch = Huttball.GetPitch(area);
                if (pitch == null)
                    return;

                Vector3 position;
                if (target == area)
                    position = GetTargetingModeSelectedPosition();
                else if (GetIsObjectValid(target) && GetArea(target) == area)
                    position = GetPosition(target);
                else
                {
                    DMPresence.SendMessage(Player, ColorToken.Red("Pick a square of the pitch."));
                    return;
                }

                onPicked(pitch.Value.GetSquare(position));
            });
        }

        private void PickCreature(string message, System.Action<uint> onPicked)
        {
            Targeting.EnterTargetingMode(Controlled, ObjectType.Creature, message, onPicked);
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
            });
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

        public Action OnBribeDown() => () => ChangeBribes(-1);

        public Action OnBribeUp() => () => ChangeBribes(1);

        private void ChangeBribes(int change)
        {
            if (!UserIsDM)
                return;

            var match = Match;
            if (match == null)
                return;

            match.SetBribes(SelectedTeam, match.GetBribes(SelectedTeam) + change);
            Huttball.RefreshWindows();
        }
    }
}

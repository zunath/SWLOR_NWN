using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HuttballService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The compact Huttball window a player keeps in a corner of the screen: joining or leaving a team, their own
    /// stats, and the actions their character can take, each greyed out when it cannot be taken. Move turns into
    /// Stand Up while they are down. Captain opens the match window, to set up a match with no DM or run their team.
    /// </summary>
    public class HuttballPlayerViewModel : GuiViewModelBase<HuttballPlayerViewModel, GuiPayloadBase>,
        IGuiRefreshable<HuttballRefreshEvent>
    {
        public string StatsText { get => Get<string>(); set => Set(value); }
        public string MoveText { get => Get<string>(); set => Set(value); }
        public bool CanMoveOrStand { get => Get<bool>(); set => Set(value); }
        public bool CanBlock { get => Get<bool>(); set => Set(value); }
        public bool CanPass { get => Get<bool>(); set => Set(value); }
        public bool CanDirtyMove { get => Get<bool>(); set => Set(value); }
        public bool CanFinish { get => Get<bool>(); set => Set(value); }
        public bool CanReroll { get => Get<bool>(); set => Set(value); }
        public bool CanUseMedic { get => Get<bool>(); set => Set(value); }
        public bool CanJoin { get => Get<bool>(); set => Set(value); }
        public bool CanLeave { get => Get<bool>(); set => Set(value); }
        public bool CanOpenCaptain { get => Get<bool>(); set => Set(value); }

        /// <summary>The team to join: 0 Home, 1 Away.</summary>
        public int TeamSelection { get => Get<int>(); set => Set(value); }

        /// <summary>The role to join as, in <see cref="HuttballRole"/> order from 0.</summary>
        public int RoleSelection { get => Get<int>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
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

        private uint Character => DMPresence.GetControlledCreature(Player);
        private HuttballMatch Match => Huttball.GetMatch(GetArea(Character));

        private HuttballPlayer GetMe()
        {
            return Match?.GetPlayer(Character);
        }

        private void Refresh()
        {
            var match = Match;
            var me = match?.GetPlayer(Character);
            StatsText = Describe(match, me);

            var isDown = me != null && me.Status == HuttballPlayerStatus.KnockedDown;
            MoveText = isDown ? "Stand Up" : "Move";
            CanMoveOrStand = me != null && (isDown ? match.CanStandUp(me) : match.CanMove(me));
            CanBlock = me != null && match.CanBlock(me);
            CanPass = me != null && match.CanPass(me);
            CanDirtyMove = me != null && match.CanDirtyMove(me);
            CanFinish = me != null && match.CanFinish(me);
            CanReroll = me != null && match.CanReroll(me.Team);
            CanUseMedic = me != null && match.CanUseMedic(me.Team);
            CanJoin = match != null && me == null && match.Phase == HuttballPhase.Setup;
            CanLeave = me != null;
            CanOpenCaptain = match == null || match.IsCaptain(Character);
        }

        private static string Describe(HuttballMatch match, HuttballPlayer me)
        {
            if (match == null)
                return "No Huttball match here. Captain lets you set one up.";
            if (me == null)
                return match.Phase == HuttballPhase.Setup
                    ? "Pick a team and role, then Join."
                    : "You can join between drives.";

            var stats = me.Stats;
            var captain = match.IsCaptain(me.Creature) ? " (captain)" : string.Empty;
            var state = me.Status == HuttballPlayerStatus.Standing
                ? me.Square == null ? "Waiting to take the field" : "Standing"
                : HuttballMatch.DescribeStatus(me.Status);
            var turn = match.Phase switch
            {
                HuttballPhase.Setup => "Take your place in your half.",
                HuttballPhase.Playing when me.Team != match.ActiveTeam => $"{match.ActiveTeam} team's turn.",
                HuttballPhase.Playing when me.IsDone => "Your turn: done.",
                HuttballPhase.Playing => $"Your turn: {me.MovementLeft} move, action {(me.HasActed ? "used" : "ready")}.",
                _ => "Full time.",
            };
            var ball = match.BallCarrier == me ? " You have the ball!" : string.Empty;

            return $"{me.Role}, {me.Team} team{captain}\n" +
                   $"STR +{stats.Strength}  AGI +{stats.Agility}  Move {stats.Movement}\n" +
                   $"{state}.{ball}\n" +
                   $"{turn}\n" +
                   $"Home {match.GetScore(HuttballTeam.Home)} - Away {match.GetScore(HuttballTeam.Away)} | " +
                   $"Re-rolls {match.GetRerolls(me.Team)}, Medics {match.GetMedics(me.Team)}";
        }

        private bool RequireMe(out HuttballPlayer me)
        {
            me = GetMe();
            if (me != null)
                return true;

            SendMessageToPC(Character, ColorToken.Red("You are not playing in a Huttball match here."));
            return false;
        }

        public Action OnMoveOrStand() => () =>
        {
            if (!RequireMe(out var me))
                return;

            if (me.Status == HuttballPlayerStatus.KnockedDown)
            {
                Huttball.Command(Player, match => match.StandUp(me.Creature));
                return;
            }

            var prompt = Match.Phase == HuttballPhase.Setup
                ? "Click a square in your own half."
                : $"Click where to move ({me.MovementLeft} squares left).";
            Huttball.PickSquare(Player, prompt,
                square => Huttball.Command(Player, match => match.Move(me.Creature, square), mover: me.Creature));
        };

        public Action OnBlock() => () =>
        {
            if (!RequireMe(out var me))
                return;

            Huttball.PickCreature(Player, "Click an opponent next to you to block.",
                target => Huttball.Command(Player, match => match.Block(me.Creature, target), striker: me.Creature, struck: target));
        };

        public Action OnPass() => () =>
        {
            if (!RequireMe(out var me))
                return;

            Huttball.PickCreature(Player, $"Click a teammate within {HuttballRules.PassRange} squares to throw to.",
                target => Huttball.Command(Player, match => match.Pass(me.Creature, target)));
        };

        public Action OnDirtyMove() => () =>
        {
            if (!RequireMe(out var me))
                return;

            Huttball.PickCreature(Player, "Click a downed opponent next to you. The referee may be watching...",
                target => Huttball.Command(Player, match => match.DirtyMove(me.Creature, target), striker: me.Creature, struck: target));
        };

        public Action OnDone() => () =>
        {
            if (RequireMe(out var me))
                Huttball.Command(Player, match => match.FinishActivation(me.Creature));
        };

        public Action OnReroll() => () =>
        {
            if (RequireMe(out var me))
                Huttball.Command(Player, match => match.Reroll(me.Team));
        };

        /// <summary>
        /// Joins the chosen team in the chosen role, between drives. The first player on a team captains it.
        /// </summary>
        public Action OnJoin() => () =>
        {
            var match = Match;
            if (match == null)
                return;
            if (match.Phase != HuttballPhase.Setup)
            {
                SendMessageToPC(Character, ColorToken.Red("You can only join between drives."));
                return;
            }

            var other = Huttball.FindMatchArea(Character);
            if (GetIsObjectValid(other) && other != GetArea(Character))
            {
                SendMessageToPC(Character, ColorToken.Red("You are already playing in another match."));
                return;
            }

            var team = TeamSelection == 1 ? HuttballTeam.Away : HuttballTeam.Home;
            var role = (HuttballRole)(RoleSelection + 1);
            var character = Character;
            Huttball.Command(Player, m => m.AddPlayer(character, GetName(character), team, role, Huttball.CanCaptain(character)));
        };

        public Action OnLeave() => () =>
        {
            if (!RequireMe(out var me))
                return;

            Huttball.ClearEffects(me.Creature);
            Huttball.Command(Player, match => match.RemovePlayer(me.Creature));
        };

        /// <summary>
        /// Opens the match window for a captain, or for anyone where there is no match yet so they can set one up.
        /// </summary>
        public Action OnCaptain() => () =>
        {
            var match = Match;
            if (match != null && !match.IsCaptain(Character))
            {
                SendMessageToPC(Character, ColorToken.Red("Only a team captain can open the captain's window."));
                return;
            }

            Gui.TogglePlayerWindow(Player, GuiWindowType.Huttball);
        };

        public Action OnMedic() => () =>
        {
            if (!RequireMe(out var me))
                return;

            Huttball.PickCreature(Player, "Click an injured teammate on the bench for the medic.",
                target => Huttball.Command(Player, match => match.UseMedic(me.Team, target)));
        };
    }
}

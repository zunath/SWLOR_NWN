using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HuttballService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The compact Huttball window a player keeps in a corner of the screen: their own stats and the actions their
    /// character can take, each greyed out when it cannot be taken. Move turns into Stand Up while they are down.
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

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Refresh();
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
        }

        private static string Describe(HuttballMatch match, HuttballPlayer me)
        {
            if (match == null)
                return "No Huttball match here.";
            if (me == null)
                return "You are not playing in this match.";

            var stats = me.Stats;
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

            return $"{me.Role}, {me.Team} team\n" +
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

        public Action OnMedic() => () =>
        {
            if (!RequireMe(out var me))
                return;

            Huttball.PickCreature(Player, "Click an injured teammate on the bench for the medic.",
                target => Huttball.Command(Player, match => match.UseMedic(me.Team, target)));
        };
    }
}

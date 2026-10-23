using System.Collections.Generic;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>
    /// The life of a bite, from the moment a cast picks a fish to the end of its fight. A bite spends one bait, takes one of the
    /// fishing point's bites and pays Agriculture XP at once, caught or not, exactly as the catch roll it replaces did; then it
    /// opens a fight in the Line window. At the start of the bite the angler may instead choose Quick Reel, which lands the fish by
    /// today's catch roll at once. The fish is otherwise landed only when the fight is won. A fight the angler leaves unfinished
    /// (window closed, logout, moving away, combat, death) loses the fish as an escape.
    /// </summary>
    public sealed class FishingFightLifecycle
    {
        public const string BiteMessage = "Something takes the bait! Reel it in.";
        public const string LostMessage = "You failed to reel the fish in...";
        public const string SnappedMessage = "Your line snapped. " + LostMessage;
        public const string EscapedMessage = "The fish got away. " + LostMessage;
        public const string QuickReelMissedMessage = "The fish shook the hook. " + LostMessage;

        /// <summary>How often a fight in progress checks that the angler is still there and still.</summary>
        public const float CheckSeconds = 1.0f;

        private readonly IFishingFightWorld _world;
        private readonly IMinigameRandom _random;
        private readonly Dictionary<uint, FishingFight> _active = new();

        public FishingFightLifecycle(IFishingFightWorld world, IMinigameRandom random)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>The angler's fight in progress, or null.</summary>
        public FishingFight GetActive(uint player) => _active.TryGetValue(player, out var fight) ? fight : null;

        /// <summary>True while the fight is the angler's fight in progress.</summary>
        public bool IsActive(FishingFight fight) =>
            fight != null && !fight.Settled && _active.TryGetValue(fight.Player, out var active) && active == fight;

        /// <summary>
        /// Handles a bite: spends the bait, takes one of the fishing point's bites and pays the Agriculture XP for the bite, then
        /// opens the fight. Nothing the fight does changes any of those. Returns the fight, or null when its window could not open
        /// (the fish is lost, and the bait and XP are spent all the same).
        /// </summary>
        public FishingFight Bite(FishingBite bite)
        {
            if (bite == null)
                throw new ArgumentNullException(nameof(bite));

            var previous = GetActive(bite.Player);
            if (previous != null)
                Abandon(previous, FishingFightEnd.Replaced);

            var remainingBait = Math.Max(0, bite.BaitRemaining - 1);
            _world.SetRemainingBait(bite.Rod, remainingBait);

            // The fight pays today's XP for the bite times the multiplier of the fish's level band, which keeps the XP per hour at
            // today's (gate 6). Today's XP is paid now, caught or not. The rest is paid when the fight ends on its own, in proportion
            // to how long it lasted, so a fight abandoned at once to cast again earns no more than the old catch roll did.
            var baseXp = Skill.GetDeltaXP(bite.Fish.Level - bite.Rank);
            var fightXp = CraftMinigameXp.ScaleBaseXp(baseXp, CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, bite.Fish.Level));
            var xp = Math.Min(baseXp, fightXp);

            _world.Message(bite.Player, BiteMessage);
            _world.Message(bite.Player, $"Bait Remaining: {remainingBait}x {bite.BaitName}");

            var remainingAttempts = bite.PointAttemptsRemaining - 1;
            if (remainingAttempts <= 0)
                _world.ExhaustFishingPoint(bite.FishingPoint);
            else
                _world.SetFishingPointAttempts(bite.FishingPoint, remainingAttempts);

            _world.GiveAgricultureXp(bite.Player, xp);

            var setup = LineSetup.ForFish(
                bite.Fish.Level,
                bite.Fish.Rarity,
                bite.Rank,
                FishingRodTier.Tier(bite.RodType));
            var fight = new FishingFight(Guid.NewGuid().ToString(), bite, new LineEngine(_random, setup))
            {
                XpOnFinish = fightXp - xp,
                StartedAt = _world.UtcNow
            };
            _active[bite.Player] = fight;
            _world.BeginFight(bite.Player);

            bool opened;
            try
            {
                opened = _world.OpenWindow(bite.Player, fight);
            }
            catch
            {
                Abandon(fight, FishingFightEnd.Left);
                throw;
            }

            if (!opened)
            {
                Abandon(fight, FishingFightEnd.Left);
                return null;
            }

            _world.Schedule(CheckSeconds, () => Check(fight));
            return fight;
        }

        /// <summary>
        /// Settles a fight whose engine has ended: a landed fish goes to the angler, and a snapped line or an escape loses it.
        /// Returns null when the fight was already settled or its engine is still going, so a repeated call does nothing.
        /// </summary>
        public FishingFightResult Settle(FishingFight fight)
        {
            if (fight == null)
                throw new ArgumentNullException(nameof(fight));

            var ending = fight.Engine.Ending;
            if (ending == LineEnding.InProgress || !fight.TrySettle())
                return null;

            Forget(fight);
            _world.EndFight(fight.Player);

            string message;
            switch (ending)
            {
                case LineEnding.Landed:
                    _world.CreateFish(fight.Player, fight.Fish.Resref);
                    message = $"You landed a {fight.Fish.Name}!";
                    _world.Message(fight.Player, message);
                    _world.PlayAnimation(fight.Player, WorkAnimation.FishLanded);
                    break;
                case LineEnding.Snapped:
                    message = SnappedMessage;
                    _world.Message(fight.Player, ColorToken.Red(message));
                    break;
                default:
                    message = EscapedMessage;
                    _world.Message(fight.Player, ColorToken.Red(message));
                    break;
            }

            // The rest of the multiplied XP accrues with the time the fight took, so a fight lost on purpose after one move earns
            // almost none of it.
            var rest = (int)Math.Round(
                fight.XpOnFinish * LineRules.XpShare((_world.UtcNow - fight.StartedAt).TotalSeconds),
                MidpointRounding.AwayFromZero);
            if (rest > 0)
                _world.GiveAgricultureXp(fight.Player, rest);

            fight.Result = new FishingFightResult(ending, message);
            return fight.Result;
        }

        /// <summary>True while the angler may still choose Quick Reel: the fight is theirs and they have not made a move in it.</summary>
        public bool CanQuickReel(FishingFight fight) => IsActive(fight) && fight.Engine.Turn == 0;

        /// <summary>The chance, in percent, that Quick Reel lands this fight's fish.</summary>
        public static int QuickReelChance(FishingFight fight)
        {
            if (fight == null)
                throw new ArgumentNullException(nameof(fight));

            return FishingQuickReel.CatchChancePercent(fight.Bite.Rank, fight.Fish.Level);
        }

        /// <summary>
        /// Ends the fight at once by the catch roll fishing had before the Line (<see cref="FishingQuickReel"/>): no fight time, and no
        /// XP beyond the bite's, which is today's. The bait, the point's bite and that XP were spent at the bite. It is the choice at
        /// the start of a bite, so it is refused once the angler has made a move, or when the fight is over. Returns the result, or
        /// null when it was refused.
        /// </summary>
        public FishingFightResult QuickReel(FishingFight fight)
        {
            if (fight == null)
                throw new ArgumentNullException(nameof(fight));

            if (!CanQuickReel(fight) || !fight.TrySettle())
                return null;

            Forget(fight);
            _world.EndFight(fight.Player);

            // The multiplied XP is the fight's, paid for its time.
            fight.XpOnFinish = 0;

            var landed = _random.D100() <= QuickReelChance(fight);
            string message;
            if (landed)
            {
                _world.CreateFish(fight.Player, fight.Fish.Resref);
                message = $"You landed a {fight.Fish.Name}!";
                _world.Message(fight.Player, message);
                _world.PlayAnimation(fight.Player, WorkAnimation.FishLanded);
            }
            else
            {
                message = QuickReelMissedMessage;
                _world.Message(fight.Player, ColorToken.Red(message));
            }

            fight.Result = new FishingFightResult(landed ? LineEnding.Landed : LineEnding.Escaped, message);
            return fight.Result;
        }

        /// <summary>
        /// Loses the fish because the angler left the fight unfinished. It does nothing once the fight is settled, so a window that
        /// closes after the last action, or a check that runs late, changes nothing.
        /// </summary>
        public void Abandon(FishingFight fight, FishingFightEnd reason)
        {
            if (fight == null || !fight.TrySettle())
                return;

            Forget(fight);
            _world.EndFight(fight.Player);

            var state = _world.GetPlayerState(fight.Player);
            var message = AbandonedMessage(reason);
            if (state.IsValid && !state.IsDead && message != null)
                _world.Message(fight.Player, ColorToken.Red(message));

            if (reason != FishingFightEnd.WindowClosed && state.IsValid && state.HasWindowOpen)
                _world.CloseWindow(fight.Player);
        }

        /// <summary>
        /// Checks the angler now: they must still be there, alive, out of combat, holding the rod, still fishing, standing where they cast
        /// and watching. A fight whose
        /// angler is not loses the fish, as an escape, and the method says so. The window calls it before it resolves an action, so a
        /// click cannot beat the periodic check. Returns false when the fight is over or was just lost.
        /// </summary>
        public bool Revalidate(FishingFight fight)
        {
            if (!IsActive(fight))
                return false;

            var state = _world.GetPlayerState(fight.Player);
            var start = fight.Bite.Position;
            FishingFightEnd? end = null;
            if (!state.IsValid)
                end = FishingFightEnd.Left;
            else if (state.IsDead)
                end = FishingFightEnd.Died;
            else if (state.IsInCombat)
                end = FishingFightEnd.Combat;
            else if (state.EquippedRod != fight.Bite.Rod)
                end = FishingFightEnd.RodUnequipped;
            else if (!state.OwnsActivity)
                end = FishingFightEnd.Interrupted;
            else if (state.Position.X != start.X || state.Position.Y != start.Y || state.Position.Z != start.Z)
                end = FishingFightEnd.Moved;
            else if (!state.HasWindowOpen)
                end = FishingFightEnd.WindowClosed;

            if (!end.HasValue)
                return true;

            Abandon(fight, end.Value);
            return false;
        }

        /// <summary>
        /// Plays the clip of the action the angler just took, while the fight goes on. Set Hook has none. The action that lands the
        /// fish plays the landing instead, when the fight settles.
        /// </summary>
        public void PlayAction(FishingFight fight, string actionId)
        {
            if (fight == null)
                throw new ArgumentNullException(nameof(fight));

            var clip = WorkAnimation.ForFishing(actionId);
            if (clip != null && IsActive(fight))
                _world.PlayAnimation(fight.Player, clip);
        }

        /// <summary>
        /// The seconds the angler must still wait before their next action may resolve, 0 when it may now. The first action of a fight
        /// is free; each one after it needs <see cref="LineRules.TurnSeconds"/> since the one before, so the pace the XP multipliers
        /// assume holds however fast the angler clicks.
        /// </summary>
        public double SecondsUntilNextTurn(FishingFight fight)
        {
            if (fight?.NextTurnAt == null || fight.Settled)
                return 0;

            return Math.Max(0, (fight.NextTurnAt.Value - _world.UtcNow).TotalSeconds);
        }

        /// <summary>Starts the wait for the next action. The window calls it once an action has resolved.</summary>
        public void TurnTaken(FishingFight fight)
        {
            if (fight == null)
                throw new ArgumentNullException(nameof(fight));

            fight.NextTurnAt = _world.UtcNow.AddSeconds(LineRules.TurnSeconds);
        }

        /// <summary>One check of a fight in progress, repeated every <see cref="CheckSeconds"/> until the fight is over.</summary>
        private void Check(FishingFight fight)
        {
            if (Revalidate(fight))
                _world.Schedule(CheckSeconds, () => Check(fight));
        }

        private static string AbandonedMessage(FishingFightEnd reason) => reason switch
        {
            FishingFightEnd.WindowClosed => "You let the fish go. " + LostMessage,
            FishingFightEnd.Moved => "You moved and the fish got away. " + LostMessage,
            FishingFightEnd.Combat => "The fight drew you away and the fish got away. " + LostMessage,
            FishingFightEnd.RodUnequipped => "You put the rod away and the fish got away. " + LostMessage,
            FishingFightEnd.Interrupted => "You stopped fishing and the fish got away. " + LostMessage,
            FishingFightEnd.Died => null,
            FishingFightEnd.Replaced => null,
            _ => EscapedMessage
        };

        private void Forget(FishingFight fight)
        {
            if (_active.TryGetValue(fight.Player, out var active) && active == fight)
                _active.Remove(fight.Player);
        }
    }
}

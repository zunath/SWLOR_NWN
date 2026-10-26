using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>What one action of the board came to.</summary>
    /// <param name="Performed">False when the action was refused and nothing changed.</param>
    /// <param name="Message">Why it was refused; empty when it was performed.</param>
    /// <param name="Result">How the session ended because of this action (the last search), or null while it goes on.</param>
    public sealed record ScavengeStep(bool Performed, string Message, ScavengeResult Result)
    {
        public static ScavengeStep Refused(string message) => new(false, message, null);
    }

    /// <summary>
    /// The life of a visit to a scavenge site, from the moment a player opens it to the moment the site is emptied. A scavenger plays the
    /// Search board, or chooses Quick Search, which resolves the site by the rules scavenging had before the board. The site is marked
    /// searched (<c>FULLY_HARVESTED</c>) when the visit begins, so a site is searched once however the visit ends. Gathering XP is paid
    /// for every Search and Dig Deeper as it happens; what the site gave is put in its inventory when the visit ends, and a visit left
    /// unfinished (window closed, moving away, combat, death, logout) keeps the finds so far and loses the searches not yet spent. A
    /// scavenger who dies or logs out keeps the finds on their record instead of in the site, and gets them back later.
    /// </summary>
    public sealed class ScavengeLifecycle
    {
        public const string SiteInUseMessage = "Another scavenger is searching this site.";

        /// <summary>Shown to a player who opens a site the scavenger who searched it has not yet opened to collect what it gave.</summary>
        public const string SiteHeldMessage = "Another scavenger is collecting from this site.";

        /// <summary>How often a board in progress checks that the scavenger is still there and safe.</summary>
        public const float CheckSeconds = 1.0f;

        /// <summary>How far, in metres, the scavenger may stray from the site before they stop searching.</summary>
        public const float MaxDistance = 6.0f;

        private readonly IScavengeWorld _world;
        private readonly IMinigameRandom _random;
        private readonly Dictionary<uint, ScavengeSession> _byPlayer = new();
        private readonly Dictionary<uint, ScavengeSession> _bySite = new();

        public ScavengeLifecycle(IScavengeWorld world, IMinigameRandom random)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>The scavenger's session in progress, or null.</summary>
        public ScavengeSession GetActive(uint player) => _byPlayer.TryGetValue(player, out var session) ? session : null;

        /// <summary>The session in progress at a site, or null.</summary>
        public ScavengeSession GetActiveAt(uint site) => _bySite.TryGetValue(site, out var session) ? session : null;

        /// <summary>True while the site has a session in progress.</summary>
        public bool IsInUse(uint site) => GetActiveAt(site) != null;

        /// <summary>True while the session is its scavenger's session in progress.</summary>
        public bool IsActive(ScavengeSession session) =>
            session != null && !session.Settled && _byPlayer.TryGetValue(session.Player, out var active) && active == session;

        /// <summary>
        /// Begins a visit: marks the site searched and opens the board. Returns the session, or null when another scavenger is at the site.
        /// A window that cannot open leaves the visit to Quick Search, so the site is never wasted. A scavenger who starts a second visit
        /// ends the first.
        /// </summary>
        public ScavengeSession Start(ScavengeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // The scavenger opened the site they are already searching: carry on with that visit.
            var occupant = GetActiveAt(request.Site);
            if (occupant != null)
            {
                if (occupant.Player == request.Player)
                    return occupant;

                _world.Message(request.Player, SiteInUseMessage);
                return null;
            }

            var previous = GetActive(request.Player);
            if (previous != null)
                End(previous, ScavengeEnd.Replaced);

            // Finds a death or a logout left on the scavenger's record come back before this visit can overwrite them. This comes after
            // the first visit ends, because a visit in progress keeps its own finds on the record until it puts them in its site.
            _world.ReturnPendingFinds(request.Player);

            var setup = SearchSetup.ForRank(request.GatheringRank, request.SiteLevel, request.Effects);
            var session = new ScavengeSession(Guid.NewGuid().ToString(), request, new SearchEngine(_random, setup))
            {
                StartedAt = _world.UtcNow
            };
            _byPlayer[request.Player] = session;
            _bySite[request.Site] = session;
            _world.MarkSearched(request.Site);

            bool opened;
            try
            {
                opened = _world.OpenWindow(request.Player, session);
            }
            catch
            {
                End(session, ScavengeEnd.Left);
                throw;
            }

            if (!opened)
            {
                Resolve(session);
                return session;
            }

            _world.Schedule(CheckSeconds, () => Check(session));
            return session;
        }

        /// <summary>True while the scavenger may still choose Quick Search: the board is theirs and they have not made an action on it.</summary>
        public bool CanQuickSearch(ScavengeSession session) =>
            IsActive(session) && session.Mode == ScavengeMode.Board && session.Engine.Turn == 0;

        /// <summary>
        /// Resolves the site at once by the rules scavenging had before the board (<see cref="ScavengeQuickSearch"/>): today's attempts, rolls
        /// and XP, with no Dig Deeper, taking as long as today's attempts. It is the choice at the start of a visit, so it is refused once
        /// the scavenger has made an action. Returns the result, or null when it was refused.
        /// </summary>
        public ScavengeResult QuickSearch(ScavengeSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            return CanQuickSearch(session) ? Resolve(session) : null;
        }

        /// <summary>
        /// Rebuilds the board's rules with the scavenger's perks and style as they are now, keeping the board's hidden layout. The style is
        /// chosen in the window, so it can change until the first action; after that the visit keeps the perks it began with. Returns
        /// false when the visit is over or has acted.
        /// </summary>
        public bool RefreshEffects(ScavengeSession session, SearchEffects effects)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (!CanQuickSearch(session))
                return false;

            var request = session.Request with { Effects = effects };
            var setup = SearchSetup.ForRank(request.GatheringRank, request.SiteLevel, effects);
            session.Reconfigure(request, new SearchEngine(_random, setup, session.Engine.Board.Promising));
            return true;
        }

        /// <summary>
        /// Performs one action of the board for the scavenger: checks they are still there, keeps the pace, resolves the action, pays the XP
        /// of an attempt, rolls the loot of a find, and settles the visit when the last search is spent.
        /// </summary>
        public ScavengeStep Act(ScavengeSession session, string actionId)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (!IsActive(session))
                return ScavengeStep.Refused(session.Result?.Message ?? "The search is over.");
            if (!Revalidate(session))
                return ScavengeStep.Refused(session.Result?.Message ?? "The search is over.");

            // The pace keeps the XP multiplier honest, however fast the client sends clicks.
            var wait = SecondsUntilNextAction(session);
            if (wait > 0)
            {
                var seconds = (int)Math.Ceiling(wait);
                return ScavengeStep.Refused($"Take your time. Wait {seconds} more {(seconds == 1 ? "second" : "seconds")} before your next action.");
            }

            var engine = session.Engine;
            var result = engine.Perform(actionId);
            if (!result.Performed)
                return ScavengeStep.Refused(result.RejectedReason);

            session.NextActionAt = _world.UtcNow.AddSeconds(SearchRules.ActionSeconds);

            var last = engine.Last;
            if (last.PaysXp)
            {
                var xp = SearchRules.AttemptXp(session.Request.DeltaXp, session.Request.SiteLevel, last.IsFind);
                if (xp > 0)
                    _world.GiveGatheringXp(session.Player, xp);

                session.XpPaid += xp;
            }

            if (last.IsFind)
            {
                var effects = session.Request.Effects ?? SearchEffects.None;
                var deeper = last.Kind == SearchEventKind.DigDeeper;

                // The first Dig Deeper find of a board gets the Jackpot weight on top of the dig's.
                var jackpot = deeper && !session.JackpotSpent ? effects.JackpotWeightBonus : 0;
                session.JackpotSpent |= deeper;
                session.AddFind(ScavengeLoot.Roll(
                    session.Request.Table,
                    session.Request.TreasureHunterLevel,
                    session.Request.CreditFinderLevel,
                    deeper,
                    _random,
                    effects.DeeperWeightBonus + jackpot,
                    effects.FindWeightBonus));

                // XP is already saved; the find must survive a restart too, until the visit ends and puts it in the site.
                _world.SavePendingFinds(session.Player, session.Finds);
            }

            if (result.Status == MinigameStatus.InProgress)
                return new ScavengeStep(true, string.Empty, null);

            return new ScavengeStep(true, string.Empty, End(session, ScavengeEnd.Completed));
        }

        /// <summary>
        /// The seconds the scavenger must still wait before their next action may resolve, 0 when it may now. The first action of a visit
        /// is free; each one after it needs <see cref="SearchRules.ActionSeconds"/> since the one before.
        /// </summary>
        public double SecondsUntilNextAction(ScavengeSession session)
        {
            if (session?.NextActionAt == null || session.Settled)
                return 0;

            return Math.Max(0, (session.NextActionAt.Value - _world.UtcNow).TotalSeconds);
        }

        /// <summary>
        /// Ends the visit because the scavenger left it unfinished. Whatever the board has found is kept, the searches not yet spent are
        /// lost, and the site stays searched. It does nothing once the session is settled, so a window that closes after the last action
        /// changes nothing.
        /// </summary>
        public void Abandon(ScavengeSession session, ScavengeEnd reason)
        {
            if (session == null)
                return;

            End(session, reason);
        }

        /// <summary>
        /// Checks the scavenger now: they must still be there, alive, out of combat, near the site and watching. A visit whose scavenger is
        /// not ends, and the method says so. The window calls it before it resolves an action, so a click cannot beat the periodic check.
        /// Returns false when the visit is over or was just ended.
        /// </summary>
        public bool Revalidate(ScavengeSession session)
        {
            if (!IsActive(session))
                return false;

            // Quick Search settles at once and the check is for a board.
            if (session.Mode != ScavengeMode.Board)
                return true;

            var state = _world.GetPlayerState(session.Player, session.Site);
            ScavengeEnd? end = null;
            if (!state.IsValid)
                end = ScavengeEnd.Left;
            else if (state.IsDead)
                end = ScavengeEnd.Died;
            else if (!state.SiteValid)
                end = ScavengeEnd.SiteGone;
            else if (state.IsInCombat)
                end = ScavengeEnd.Combat;
            else if (state.DistanceToSite > MaxDistance)
                end = ScavengeEnd.Moved;
            else if (!state.HasWindowOpen)
                end = ScavengeEnd.WindowClosed;

            if (!end.HasValue)
                return true;

            End(session, end.Value);
            return false;
        }

        private void Check(ScavengeSession session)
        {
            if (Revalidate(session))
                _world.Schedule(CheckSeconds, () => Check(session));
        }

        private ScavengeResult Resolve(ScavengeSession session)
        {
            session.Mode = ScavengeMode.Quick;
            var request = session.Request;
            var hardLook = CraftRankMilestones.HardLookLevelForRank(request.GatheringRank);
            var quick = ScavengeQuickSearch.Resolve(_random, hardLook, request.DeltaXp);

            foreach (var attempt in quick.Attempts)
            {
                _world.SkillCheckMessage(
                    session.Player,
                    attempt.Success
                        ? $"Search *success*: ({attempt.Roll} vs. DC: {attempt.Dc})"
                        : $"Search *failure*: ({attempt.Roll} vs DC: {attempt.Dc})");

                if (attempt.Xp > 0)
                    _world.GiveGatheringXp(session.Player, attempt.Xp);

                session.XpPaid += attempt.Xp;
                if (attempt.Success)
                    session.AddFind(ScavengeLoot.Roll(request.Table, request.TreasureHunterLevel, request.CreditFinderLevel, false, _random));
            }

            return End(session, ScavengeEnd.QuickSearch, quick.Seconds);
        }

        /// <summary>
        /// Settles the visit once. What the visit found goes one of two ways. Normally it is put in the site, which is then opened for the
        /// scavenger, held for them when they left it behind, or removed when it gave nothing. When the scavenger cannot take it from the
        /// site (they died or logged out, or the site is gone) it stays on their record instead and is never put in the site, so nothing
        /// is duplicated: they get it back at once if they are here and alive, otherwise on respawn, at their next visit or at login.
        /// Returns null when the visit was already settled.
        /// </summary>
        private ScavengeResult End(ScavengeSession session, ScavengeEnd reason, double animationSeconds = 0)
        {
            if (!session.TrySettle())
                return null;

            Forget(session);

            var request = session.Request;
            var state = _world.GetPlayerState(session.Player, session.Site);

            // A scavenger who is leaving the game is absent even though the engine still reports the creature as valid: the client leave
            // event runs before the creature is removed. Nothing is said to them, rolled for them or held for them.
            var present = state.IsValid && reason != ScavengeEnd.Left;
            var carried = !present || !state.SiteValid || state.IsDead || reason == ScavengeEnd.Died;
            var names = new List<string>();
            var returned = false;
            if (carried)
            {
                if (session.Finds.Count > 0)
                {
                    _world.SavePendingFinds(session.Player, session.Finds);
                    if (present && !state.IsDead)
                    {
                        _world.ReturnPendingFinds(session.Player);
                        returned = true;
                    }
                }
            }
            else
            {
                names = session.Finds
                    .Select(find => $"{find.Quantity}x {_world.CreateItem(request.Site, find.Resref, find.Quantity)}")
                    .ToList();
                if (session.Finds.Count > 0)
                    _world.SavePendingFinds(session.Player, Array.Empty<ScavengeFind>());

                // The site's extra loot tables roll once for every search that found something, as they always did. A find from Dig Deeper skips them.
                var searchFinds = session.Finds.Count(find => !find.Deeper);
                for (var find = 0; find < searchFinds; find++)
                    _world.SpawnBonusLoot(request.Site, session.Player);
            }

            var message = Summary(reason, names, carried, session.Finds.Count > 0, returned);
            if (present && message.Length > 0 && !returned)
                _world.Message(session.Player, message);

            if (reason != ScavengeEnd.WindowClosed && present && state.HasWindowOpen)
                _world.CloseWindow(session.Player);

            if (carried || session.Finds.Count == 0)
            {
                // Nothing is left in the site for anyone: it gave nothing, or what it gave went to the scavenger's record.
                _world.DestroySite(session.Player, request.Site, animationSeconds);
            }
            else if (reason is ScavengeEnd.Moved or ScavengeEnd.Combat or ScavengeEnd.Replaced)
            {
                // The scavenger is away from the site, in a fight or at another site, so it is not opened for them: what it gave waits in
                // the site, held for them for a short while.
                _world.HoldSite(session.Player, request.Site, animationSeconds);
            }
            else
            {
                _world.OpenSite(session.Player, request.Site, animationSeconds);
            }

            session.Result = new ScavengeResult(reason, session.Finds.ToList(), message);
            return session.Result;
        }

        private static string Summary(ScavengeEnd reason, IReadOnlyList<string> names, bool carried, bool hasFinds, bool returned)
        {
            if (carried)
            {
                if (!hasFinds)
                    return reason == ScavengeEnd.Died ? string.Empty : "You found nothing.";

                if (returned)
                    return "What you found came to you instead of the site.";

                return reason == ScavengeEnd.Died
                    ? "You died before you could collect what you found. It will be returned to you when you respawn."
                    : string.Empty;
            }

            var found = names.Count == 0 ? "You found nothing." : $"You found {string.Join(", ", names)}.";
            var waiting = names.Count > 0 ? " It waits in the site for a short while; go back to it to collect." : string.Empty;
            return reason switch
            {
                ScavengeEnd.Completed => "You finish searching. " + found,
                ScavengeEnd.QuickSearch => found,
                ScavengeEnd.WindowClosed => "You stop searching. " + found,
                ScavengeEnd.Moved => "You moved away and stopped searching. " + found + waiting,
                ScavengeEnd.Combat => "The fight drew you away from the site. " + found + waiting,
                ScavengeEnd.Replaced => "You stopped searching here. " + found + waiting,
                _ => string.Empty
            };
        }

        private void Forget(ScavengeSession session)
        {
            if (_byPlayer.TryGetValue(session.Player, out var active) && active == session)
                _byPlayer.Remove(session.Player);
            if (_bySite.TryGetValue(session.Site, out var occupant) && occupant == session)
                _bySite.Remove(session.Site);
        }
    }
}

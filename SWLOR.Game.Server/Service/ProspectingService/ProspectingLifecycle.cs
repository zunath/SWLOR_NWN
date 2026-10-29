using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// The life of a harvester used on a vein or a tree, from the use to the end of the session. A use opens the Prospecting window and
    /// holds the node for the player until the session ends: nobody else can work it meanwhile, and anyone who tries is told it is in
    /// use. Every action takes the seconds a harvest always took, plays the harvest animation, and then resolves in the engine; what it
    /// gathers goes to the player at once, its XP is paid at once, its charges are spent at once, and the node keeps what is left. A
    /// session the player leaves unfinished (window closed, logout, moving away, combat, death) loses nothing already gathered and
    /// leaves the rest of the node as it stands. Quick Harvest resolves a node without a session, one harvest delay for each unit.
    /// </summary>
    public sealed class ProspectingLifecycle
    {
        public const string InUseMessage = "That resource is already being worked by another harvester.";
        public const string SpentMessage = "There is nothing left to harvest there.";
        public const string NoChargesMessage = "Your harvester has no charges left.";
        public const string MinedOutMessage = "You take the last of it. The node is spent.";
        public const string CollapsedMessage = "The node collapses, and what was left in it is lost.";
        public const string OutOfChargesMessage = "Your harvester has no charges left.";
        public const string LeftMessage = "You leave the rest of the node for others.";
        public const string StillWorkingMessage = "You are still working.";
        public const string OverMessage = "The session is over.";
        public const string QuickHarvestTooLateMessage = "Quick Harvest is only offered before you begin working the node.";

        /// <summary>How often a session in progress checks that the player is still there and still.</summary>
        public const float CheckSeconds = 1.0f;

        /// <summary>
        /// How long a window may sit without an action before the session is let go. Only one player works a node at a time, so a window
        /// left open on a node would hold it for everyone else.
        /// </summary>
        public const double IdleSeconds = 120.0;

        private readonly IProspectingWorld _world;
        private readonly IMinigameRandom _random;
        private readonly Dictionary<uint, ProspectingSession> _byPlayer = new();
        private readonly Dictionary<uint, ProspectingSession> _byNode = new();

        public ProspectingLifecycle(IProspectingWorld world, IMinigameRandom random)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>The player's session in progress, or null.</summary>
        public ProspectingSession GetActive(uint player) => _byPlayer.TryGetValue(player, out var session) ? session : null;

        /// <summary>The session holding the node, or null when nobody is working it.</summary>
        public ProspectingSession GetHolder(uint node) => _byNode.TryGetValue(node, out var session) ? session : null;

        /// <summary>True while the session is the player's session in progress.</summary>
        public bool IsActive(ProspectingSession session) =>
            session != null && !session.Settled && _byPlayer.TryGetValue(session.Player, out var active) && active == session;

        /// <summary>
        /// True when another player is working the node. A holder who has gone (logged out, moved away, died) is let go of first, so a
        /// stale session never keeps a node locked.
        /// </summary>
        public bool IsInUseByAnother(uint node, uint player)
        {
            var holder = GetHolder(node);
            if (holder == null || holder.Player == player)
                return false;

            return Revalidate(holder);
        }

        /// <summary>
        /// Begins a session: the node as it stands (or as a node nobody has worked is made), the harvester's charges as the budget, and
        /// the Gathering rank's milestones. Returns what happened; the window opens only for <see cref="ProspectingBeginStatus.Opened"/>.
        /// </summary>
        public ProspectingBegin Begin(ProspectingRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var previous = GetActive(request.Player);
            if (previous != null)
                Abandon(previous, ProspectingEnd.Replaced);

            if (IsInUseByAnother(request.Node, request.Player))
            {
                _world.Message(request.Player, InUseMessage);
                return new ProspectingBegin(ProspectingBeginStatus.InUse, null);
            }

            if (!ProspectingNode.TryParse(_world.LoadNode(request.Node), out var node))
            {
                var gemChance = _world.GemChance(request.LootTable);
                var legacyStock = _world.LegacyStock(request.Node);
                node = legacyStock > 0
                    ? ProspectingNode.Create(_random, gemChance, legacyStock)
                    : ProspectingNode.Create(_random, gemChance);
                _world.SaveNode(request.Node, node.Serialize());
            }

            if (node.IsSpent)
            {
                _world.DestroyNode(request.Node);
                _world.Message(request.Player, SpentMessage);
                return new ProspectingBegin(ProspectingBeginStatus.Spent, null);
            }

            var charges = _world.GetCharges(request.Harvester);
            if (charges < 1)
            {
                _world.Message(request.Player, NoChargesMessage);
                return new ProspectingBegin(ProspectingBeginStatus.NoCharges, null);
            }

            var setup = ProspectingSetup.ForNode(node, charges, request.Rank, request.NodeTier, request.Effects);
            var session = new ProspectingSession(Guid.NewGuid().ToString(), request, new ProspectingEngine(_random, setup), charges);
            session.LastActivityAt = _world.UtcNow;
            _byPlayer[request.Player] = session;
            _byNode[request.Node] = session;
            _world.BeginSession(request.Player);

            bool opened;
            try
            {
                opened = _world.OpenWindow(request.Player, session);
            }
            catch
            {
                Abandon(session, ProspectingEnd.Left);
                throw;
            }

            if (!opened)
            {
                Abandon(session, ProspectingEnd.Left);
                return new ProspectingBegin(ProspectingBeginStatus.WindowFailed, null);
            }

            _world.Schedule(CheckSeconds, () => Check(session));
            return new ProspectingBegin(ProspectingBeginStatus.Opened, session);
        }

        /// <summary>
        /// Rebuilds the session's engine with the player's perks and style as they are now. The harvester's style is chosen in the window,
        /// so it can change until the first action; after that the session keeps the perks it began with. Returns false when the session
        /// is over, has acted, or is working an action.
        /// </summary>
        public bool RefreshEffects(ProspectingSession session, ProspectingEffects effects)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.Settled || session.HasActed || session.Working != null)
                return false;

            var engine = session.Engine;
            var setup = ProspectingSetup.ForNode(engine.ToNode(), engine.ChargesAtStart, session.Request.Rank, session.Request.NodeTier, effects);
            session.Reconfigure(session.Request with { Effects = effects }, new ProspectingEngine(_random, setup));
            return true;
        }

        /// <summary>
        /// Starts an action. It takes the seconds the action takes, with the harvest animation, and then resolves; <paramref name="onResolved"/>
        /// is called with what it did. A refused action (not enough charges, nothing to extract, another action still going) changes nothing.
        /// Leave resolves at once.
        /// </summary>
        public ProspectingActionStart StartAction(ProspectingSession session, string actionId, Action<ProspectingActionReport> onResolved)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.Settled)
                return ProspectingActionStart.Refused(OverMessage);

            if (!Revalidate(session))
                return ProspectingActionStart.Refused(OverMessage);

            if (session.Working != null || session.QuickHarvesting)
                return ProspectingActionStart.Refused(StillWorkingMessage);

            var preview = session.Engine.Preview(actionId);
            if (!preview.Enabled)
                return ProspectingActionStart.Refused(preview.DisabledReason);

            var seconds = session.Engine.SecondsFor(actionId);
            if (seconds <= 0)
            {
                Resolve(session, actionId, onResolved);
                return ProspectingActionStart.Ok;
            }

            session.Working = actionId;
            session.LastActivityAt = _world.UtcNow;
            _world.PlayWork(session.Player, actionId, (float)seconds);
            _world.Schedule((float)seconds, () => Complete(session, actionId, onResolved));
            return ProspectingActionStart.Ok;
        }

        /// <summary>
        /// Begins a Quick Harvest: the node with no seams, the way every harvest was before Prospecting. It takes the units the node has left
        /// as far as the charges go, one harvest delay and one charge for each, paying the XP of a unit at the player's rank for each.
        /// <paramref name="onProgress"/> is called after every unit.
        /// </summary>
        public ProspectingActionStart StartQuickHarvest(ProspectingSession session, Action<QuickHarvestReport> onProgress)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.Settled)
                return ProspectingActionStart.Refused(OverMessage);

            if (!Revalidate(session))
                return ProspectingActionStart.Refused(OverMessage);

            if (session.Working != null || session.QuickHarvesting)
                return ProspectingActionStart.Refused(StillWorkingMessage);

            if (session.HasActed)
                return ProspectingActionStart.Refused(QuickHarvestTooLateMessage);

            var node = session.Engine.ToNode();
            var units = ProspectingQuickHarvest.Units(node, session.HarvesterCharges);
            if (units <= 0)
                return ProspectingActionStart.Refused(NoChargesMessage);

            session.QuickHarvesting = true;
            session.QuickHarvestTotal = units;
            session.LastActivityAt = _world.UtcNow;
            QuickHarvestStep(session, node, onProgress);
            return ProspectingActionStart.Ok;
        }

        /// <summary>
        /// Ends a session the player left unfinished. What was gathered is kept, and the node keeps what is left, with the seams found.
        /// It does nothing once the session is settled, so a window that closes after the last action changes nothing.
        /// </summary>
        public void Abandon(ProspectingSession session, ProspectingEnd reason)
        {
            if (session == null || !session.TrySettle())
                return;

            session.Abandoned = reason;
            session.QuickHarvesting = false;
            session.Working = null;
            Forget(session);
            _world.EndSession(session.Player);

            // The node as the last action left it was saved then; a node the world no longer has needs nothing.
            var state = _world.GetPlayerState(session.Player, session.Request.Harvester);
            var message = AbandonedMessage(reason);
            if (state.IsValid && !state.IsDead && message != null)
                _world.Message(session.Player, message);

            if (reason != ProspectingEnd.WindowClosed && state.IsValid && state.HasWindowOpen)
                _world.CloseWindow(session.Player);
        }

        /// <summary>
        /// Checks the player now: they must still be there, alive, out of combat, holding the harvester, still prospecting, standing where
        /// they began and watching, and the node must still be in the world. A session whose player is not is abandoned, and the method
        /// says so. The window calls it before it resolves a click. Returns false when the session is over or was just abandoned.
        /// </summary>
        public bool Revalidate(ProspectingSession session)
        {
            if (!IsActive(session))
                return false;

            var state = _world.GetPlayerState(session.Player, session.Request.Harvester);
            var start = session.Request.Position;
            ProspectingEnd? end = null;
            if (!state.IsValid)
                end = ProspectingEnd.Left;
            else if (state.IsDead)
                end = ProspectingEnd.Died;
            else if (state.IsInCombat)
                end = ProspectingEnd.Combat;
            else if (!state.HasHarvester)
                end = ProspectingEnd.HarvesterLost;
            else if (!_world.NodeExists(session.Node))
                end = ProspectingEnd.NodeGone;
            else if (!state.OwnsActivity)
                end = ProspectingEnd.Interrupted;
            else if (state.Position.X != start.X || state.Position.Y != start.Y || state.Position.Z != start.Z)
                end = ProspectingEnd.Moved;
            else if (!state.HasWindowOpen)
                end = ProspectingEnd.WindowClosed;
            else if (session.Working == null && !session.QuickHarvesting && (_world.UtcNow - session.LastActivityAt).TotalSeconds > IdleSeconds)
                end = ProspectingEnd.Idle;

            if (!end.HasValue)
                return true;

            Abandon(session, end.Value);
            return false;
        }

        /// <summary>The XP one item pays a session at a vein of the tier: today's XP for the unit times the band's multiplier (1 unless CraftSim's XP-rate check asks for more).</summary>
        public int XpPerItem(ProspectingSession session)
        {
            var tier = session.Request.NodeTier;
            var baseXp = GatheringLevels.XpPerUnit(tier, _world.GetGatheringRank(session.Player));
            return CraftMinigameXp.ScaleBaseXp(baseXp, CraftMinigameXp.MultiplierFor(CraftMinigameType.Prospecting, GatheringLevels.ForTier(tier)));
        }

        private void Complete(ProspectingSession session, string actionId, Action<ProspectingActionReport> onResolved)
        {
            if (session.Settled || session.Working != actionId)
                return;

            session.Working = null;
            if (!Revalidate(session))
                return;

            Resolve(session, actionId, onResolved);
        }

        private void Resolve(ProspectingSession session, string actionId, Action<ProspectingActionReport> onResolved)
        {
            session.LastActivityAt = _world.UtcNow;
            var engine = session.Engine;
            var revealedBefore = engine.RevealedSeams.Count;
            var result = engine.Perform(actionId);
            if (!result.Performed)
            {
                onResolved?.Invoke(new ProspectingActionReport(actionId, result, default, 0, 0, false, result.RejectedReason));
                return;
            }

            var delivery = new HarvestDelivery(
                engine.Units - session.Delivered.Rolls,
                engine.RichRolls - session.Delivered.ExtraOreRolls,
                engine.Gems - session.Delivered.Gems);
            session.Delivered = new HarvestDelivery(engine.Units, engine.RichRolls, engine.Gems);

            // The node's depletion is saved before the harvest is delivered and the XP given, so a restart in between cannot leave paid
            // units on the node. A node the action ends is saved spent, and removed once the harvest is out. The delivery journals the
            // units it put in the inventory (HarvestReturnJournal), so a crash before the character file holds them gives them again at login.
            var ending = engine.Ending;
            var removed = ending is ProspectingEnding.MinedOut or ProspectingEnding.Collapsed;
            var after = engine.ToNode();
            _world.SaveNode(session.Node, (removed ? after.Take(after.Pool) : after).Serialize());
            if (!delivery.IsEmpty)
                _world.GiveHarvest(session.Player, session.Node, session.Request.LootTable, delivery);

            if (removed)
                _world.DestroyNode(session.Node);

            var xp = delivery.Items * XpPerItem(session);
            if (xp > 0)
            {
                _world.GiveGatheringXp(session.Player, xp);
                session.XpPaid += xp;
            }

            string message = null;
            if (ending != ProspectingEnding.InProgress)
            {
                message = EndMessage(ending);
                Settle(session, ending, message);
            }

            SpendCharges(session, engine.Charges);
            onResolved?.Invoke(new ProspectingActionReport(
                actionId, result, delivery, xp, Math.Max(0, engine.RevealedSeams.Count - revealedBefore), ending != ProspectingEnding.InProgress, message));
        }

        private void QuickHarvestStep(ProspectingSession session, ProspectingNode node, Action<QuickHarvestReport> onProgress)
        {
            _world.PlayWork(session.Player, null, (float)ProspectingRules.HarvestSeconds);
            _world.Schedule((float)ProspectingRules.HarvestSeconds, () =>
            {
                if (session.Settled || !session.QuickHarvesting)
                    return;

                if (!Revalidate(session))
                    return;

                var xp = ProspectingQuickHarvest.XpPerUnit(session.Request.NodeTier, _world.GetGatheringRank(session.Player));
                // As with a board action, the node is saved before the unit is delivered and its XP given.
                node = node.Take(1);
                _world.SaveNode(session.Node, node.Serialize());
                _world.GiveHarvest(session.Player, session.Node, session.Request.LootTable, new HarvestDelivery(1, 0, 0));
                session.QuickHarvested++;
                var charges = session.HarvesterCharges - ProspectingQuickHarvest.Charges(1);

                string message = null;
                var finished = false;
                if (node.IsSpent)
                {
                    _world.DestroyNode(session.Node);
                    message = MinedOutMessage;
                    finished = true;
                }
                else if (charges <= 0)
                {
                    message = OutOfChargesMessage;
                    finished = true;
                }

                if (xp > 0)
                {
                    _world.GiveGatheringXp(session.Player, xp);
                    session.XpPaid += xp;
                }

                if (finished)
                {
                    session.QuickHarvesting = false;
                    Settle(session, node.IsSpent ? ProspectingEnding.MinedOut : ProspectingEnding.OutOfCharges, message);
                }

                SpendCharges(session, charges);
                onProgress?.Invoke(new QuickHarvestReport(session.QuickHarvested, session.QuickHarvestTotal, xp, finished, message));
                if (!finished)
                    QuickHarvestStep(session, node, onProgress);
            });
        }

        /// <summary>Sets the harvester's charges, and destroys it when none is left, as a harvester always has been.</summary>
        private void SpendCharges(ProspectingSession session, int charges)
        {
            if (charges == session.HarvesterCharges && charges > 0)
                return;

            session.HarvesterCharges = Math.Max(0, charges);
            if (charges <= 0)
                _world.DestroyHarvester(session.Request.Harvester);
            else
                _world.SetCharges(session.Request.Harvester, charges);
        }

        private void Settle(ProspectingSession session, ProspectingEnding ending, string message)
        {
            if (!session.TrySettle())
                return;

            session.Finished = ending;
            session.EndMessage = message;
            Forget(session);
            _world.EndSession(session.Player);
            if (!string.IsNullOrEmpty(message))
                _world.Message(session.Player, message);
        }

        private void Forget(ProspectingSession session)
        {
            if (_byPlayer.TryGetValue(session.Player, out var active) && active == session)
                _byPlayer.Remove(session.Player);

            if (_byNode.TryGetValue(session.Node, out var holder) && holder == session)
                _byNode.Remove(session.Node);
        }

        /// <summary>One check of a session in progress, repeated every <see cref="CheckSeconds"/> until it is over.</summary>
        private void Check(ProspectingSession session)
        {
            if (Revalidate(session))
                _world.Schedule(CheckSeconds, () => Check(session));
        }

        private static string EndMessage(ProspectingEnding ending) => ending switch
        {
            ProspectingEnding.MinedOut => MinedOutMessage,
            ProspectingEnding.Collapsed => CollapsedMessage,
            ProspectingEnding.OutOfCharges => OutOfChargesMessage,
            ProspectingEnding.Left => LeftMessage,
            _ => null
        };

        private static string AbandonedMessage(ProspectingEnd reason) => reason switch
        {
            ProspectingEnd.Moved => "You moved away and stopped harvesting.",
            ProspectingEnd.Combat => "The fight drew you away and you stopped harvesting.",
            ProspectingEnd.HarvesterLost => "You no longer have your harvester and stopped harvesting.",
            ProspectingEnd.Interrupted => "You stopped harvesting.",
            ProspectingEnd.NodeGone => "The node is gone.",
            ProspectingEnd.Idle => "You stop harvesting after standing idle.",
            ProspectingEnd.Died => null,
            ProspectingEnd.Replaced => null,
            ProspectingEnd.WindowClosed => null,
            _ => null
        };
    }
}

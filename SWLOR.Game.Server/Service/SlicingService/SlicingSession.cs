using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Service.SlicingService
{
    public static class SlicingSession
    {
        public const string BoardNumberVariable = "SLICING_BOARD";
        // Retained only to map targets created before the checked-in board catalog.
        public const string SeedVariable = "SLICING_SEED";
        public const string FailuresVariable = "SLICING_FAILURES";
        public const string IntegrityVariable = "SLICING_INTEGRITY";
        public const string TierVariable = "SLICING_TIER";
        public const string OwnerVariable = "SLICING_OWNER";
        public const string OwnerTimestampVariable = "SLICING_OWNER_AT";
        public const string CommittedVariable = "SLICING_COMMITTED";
        public const string ToolTypeVariable = "SLICING_TOOL_TYPE";
        public const string ToolTierVariable = "SLICING_TOOL_TIER";

        private const int ClaimTimeoutSeconds = 180;
        private static readonly int[] _tierSkillRequirement = { 8, 22, 30, 42, 48 };
        private static readonly Dictionary<string, ActiveSlicingSession> _sessions = new();

        public sealed class ActiveSlicingSession
        {
            public uint Player { get; init; }
            public uint Target { get; init; }
            public SlicingSourceType Source { get; init; }
            public int Tier { get; init; }
            public SlicingBoard Board { get; set; }
            public int TraceRemaining { get; set; }
            public int SelectedIndex { get; set; } = -1;
            public bool HasCommitted { get; set; }
            public bool HasUsedTool { get; set; }
            public SlicingToolType PrimedTool { get; set; }
            public uint PrimedToolItem { get; set; }
            public int FreeActionsRemaining { get; set; }
            public List<SessionSnapshot> History { get; } = new();
        }

        public sealed class SessionSnapshot
        {
            public SlicingBoard Board { get; init; }
            public int TraceRemaining { get; init; }
        }

        public sealed class EligibleSlicingTool
        {
            public uint Item { get; init; }
            public string Name { get; init; }
            public SlicingToolType Type { get; init; }
            public int Tier { get; init; }
        }

        public static ActiveSlicingSession Get(uint player)
        {
            var playerId = GetObjectUUID(player);
            return !string.IsNullOrWhiteSpace(playerId) && _sessions.TryGetValue(playerId, out var session)
                ? session
                : null;
        }

        public static bool TryStart(uint player, uint target, SlicingSourceType source, int tier, out string error)
        {
            error = ValidateStart(player, target, source, tier);
            if (!string.IsNullOrWhiteSpace(error))
                return false;

            var playerId = GetObjectUUID(player);
            if (_sessions.ContainsKey(playerId))
            {
                error = "Finish or abort your current slicing attempt first.";
                return false;
            }

            if (!TryClaim(player, target, source, tier, out error))
                return false;

            var boardCount = Slicing.GetBoardCount(tier);
            var boardNumber = GetLocalInt(target, BoardNumberVariable);
            if (boardNumber < 1 || boardNumber > boardCount)
            {
                var legacySeed = GetLocalInt(target, SeedVariable);
                var selection = legacySeed != 0
                    ? legacySeed
                    : boardNumber != 0
                        ? boardNumber
                        : Random.Next(1, boardCount + 1);
                boardNumber = Slicing.MapSelectionToBoardNumber(tier, selection);
                SetLocalInt(target, BoardNumberVariable, boardNumber);
            }

            if (GetLocalInt(target, IntegrityVariable) <= 0)
                SetLocalInt(target, IntegrityVariable, 100);

            var board = Slicing.GetBoard(tier, boardNumber);
            var lockpicking = Stat.GetStatAdjustment(player, StatType.Lockpicking);
            var perception = Math.Max(0, GetAbilityModifier(AbilityType.Perception, player));
            var slicingRank = Perk.GetPerkLevel(player, PerkType.Slicing);
            var traceBonus = Slicing.GetTraceBonus(slicingRank, lockpicking, perception);

            _sessions[playerId] = new ActiveSlicingSession
            {
                Player = player,
                Target = target,
                Source = source,
                Tier = tier,
                Board = board,
                TraceRemaining = board.BaseTrace + traceBonus,
                PrimedToolItem = OBJECT_INVALID
            };

            return true;
        }

        public static string ValidateStart(uint player, uint target, SlicingSourceType source, int tier)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return "Only players may begin a slicing attempt.";
            if (!GetIsObjectValid(target))
                return "The slicing target is no longer available.";
            if (tier < 1 || tier > 5)
                return "This slicing target has an invalid security profile.";
            if (GetIsInCombat(player))
                return "You cannot begin slicing while in combat.";
            if (Perk.GetPerkLevel(player, PerkType.Slicing) < tier)
                return GetRankRequirementError(tier);
            if (source == SlicingSourceType.Lockbox && GetItemPossessor(target) != player)
                return "The lockbox must remain in your inventory.";
            if (source == SlicingSourceType.Terminal && GetDistanceBetween(player, target) > 5f)
                return "You are too far away from the terminal.";

            return string.Empty;
        }

        private static string GetRankRequirementError(int tier) =>
            ColorToken.Red($"Slicing rank {tier} is required for this target.");

        public static bool SelectTile(uint player, int index, out string error)
        {
            error = ValidateAction(player, out var session);
            if (!string.IsNullOrWhiteSpace(error))
                return false;
            if (index < 0 || index >= session.Board.Tiles.Count)
            {
                error = "That circuit tile is invalid.";
                return false;
            }

            session.SelectedIndex = index;
            Touch(session);
            return true;
        }

        public static bool RotateSelected(uint player, out string message)
        {
            message = ValidateAction(player, out var session);
            if (!string.IsNullOrWhiteSpace(message))
                return false;
            if (session.SelectedIndex < 0)
            {
                message = "Select a tile first.";
                return false;
            }
            if (session.Board.Tiles[session.SelectedIndex].Type is SlicingTileType.Entry or SlicingTileType.Core)
            {
                message = "START and GOAL sockets are fixed and cannot be rotated.";
                return false;
            }

            var cost = GetActionCost(session, SlicingToolType.RatchetBypassPin, 1, out var consumedTool);
            if (!PrepareAction(session, cost, out message))
                return false;

            Slicing.RotateClockwise(session.Board, session.SelectedIndex);
            ApplyFreeActionState(session, consumedTool);
            Commit(session);
            if (consumedTool)
                ConsumePrimedTool(session);

            return ResolveAfterAction(session, cost, out message);
        }

        public static bool SwapSelectedWith(uint player, int secondIndex, out string message)
        {
            message = ValidateAction(player, out var session);
            if (!string.IsNullOrWhiteSpace(message))
                return false;
            if (session.SelectedIndex < 0)
            {
                message = "Select the first tile before choosing an adjacent tile.";
                return false;
            }
            if (secondIndex < 0 || secondIndex >= session.Board.Tiles.Count)
            {
                message = "That circuit tile is invalid.";
                return false;
            }
            if (!Slicing.AreAdjacent(session.Board, session.SelectedIndex, secondIndex))
            {
                message = "Choose a tile directly above, below, left, or right of the selected tile.";
                return false;
            }

            if (session.Board.Tiles[session.SelectedIndex].Type is SlicingTileType.Entry or SlicingTileType.Core ||
                session.Board.Tiles[secondIndex].Type is SlicingTileType.Entry or SlicingTileType.Core)
            {
                message = "Entry and core sockets cannot be displaced.";
                return false;
            }

            var cost = GetActionCost(session, SlicingToolType.PhaseShuntFork, 2, out var consumedTool);
            if (!PrepareAction(session, cost, out message))
                return false;

            Slicing.SwapAdjacent(session.Board, session.SelectedIndex, secondIndex);
            ApplyFreeActionState(session, consumedTool);
            Commit(session);
            if (consumedTool)
                ConsumePrimedTool(session);
            session.SelectedIndex = secondIndex;

            return ResolveAfterAction(session, cost, out message);
        }

        public static bool ActivateTool(uint player, uint item, out string message)
        {
            message = ValidateAction(player, out var session);
            if (!string.IsNullOrWhiteSpace(message))
                return false;
            if (!GetIsObjectValid(item) || GetItemPossessor(item) != player)
            {
                message = "That tool must be in your inventory.";
                return false;
            }
            if (!CanActivateTool(session))
            {
                message = "Only one slicing tool may be used per attempt.";
                return false;
            }

            var toolType = (SlicingToolType)GetLocalInt(item, ToolTypeVariable);
            var toolTier = GetLocalInt(item, ToolTierVariable);
            if (toolType == SlicingToolType.Invalid || toolTier < session.Tier)
            {
                message = "That tool cannot penetrate this security tier.";
                return false;
            }

            if (toolType is SlicingToolType.ContinuitySampler or
                SlicingToolType.JunctionSpectrograph or
                SlicingToolType.ForwardEchoDecoder or
                SlicingToolType.ReversibleServoKey)
            {
                if (session.SelectedIndex < 0)
                {
                    message = "Select a tile before activating that tool.";
                    return false;
                }
            }

            if (toolType == SlicingToolType.MnemonicTraceSplice && session.History.Count == 0)
            {
                message = "There are no circuit actions to rewind.";
                return false;
            }

            session.PrimedTool = toolType;
            session.PrimedToolItem = item;
            session.HasUsedTool = true;

            if (toolType is SlicingToolType.RatchetBypassPin or
                SlicingToolType.PhaseShuntFork or
                SlicingToolType.NullSignatureLattice or
                SlicingToolType.TraceFuse)
            {
                message = toolType == SlicingToolType.TraceFuse
                    ? "Trace fuse primed. It will be consumed on your first move."
                    : "Tool primed. It will be consumed when its effect is used.";
                return true;
            }

            Commit(session);
            Item.ReduceItemStack(item, 1);
            session.PrimedToolItem = OBJECT_INVALID;

            switch (toolType)
            {
                case SlicingToolType.ReversibleServoKey:
                    AddHistory(session);
                    var selected = session.Board.Tiles[session.SelectedIndex];
                    selected.Orientation = selected.SolutionOrientation;
                    message = "The servo key aligns the selected tile to its recovered pattern.";
                    break;
                case SlicingToolType.MnemonicTraceSplice:
                    UndoActions(session, 2);
                    message = "The splice rewinds the last two circuit actions and restores their trace.";
                    break;
                case SlicingToolType.ContinuitySampler:
                    message = RevealMembership(session, session.SelectedIndex)
                        ? "Continuity detected: this tile belongs to the route."
                        : "No continuity: this tile is a decoy.";
                    break;
                case SlicingToolType.JunctionSpectrograph:
                    message = RevealOrientation(session, session.SelectedIndex)
                        ? "Route tile identified; its correct orientation is now marked."
                        : "The selected tile is a decoy.";
                    break;
                case SlicingToolType.ForwardEchoDecoder:
                    RevealForwardRoute(session, session.SelectedIndex, 2);
                    message = "The next two route signatures are marked.";
                    break;
                case SlicingToolType.RouteOverlayPrism:
                    RevealAllRouteTiles(session);
                    message = "The complete route is overlaid without orientation data.";
                    break;
                case SlicingToolType.CorePatternOracle:
                    RevealAllRouteTiles(session);
                    foreach (var index in session.Board.Tiles
                                 .Select((tile, index) => (tile, index))
                                 .Where(x => x.tile.RouteOrder >= 0)
                                 .OrderBy(x => x.tile.RouteOrder)
                                 .Take(3)
                                 .Select(x => x.index))
                    {
                        session.Board.Tiles[index].IsOrientationRevealed = true;
                    }
                    message = "The route and three correct orientations are revealed.";
                    break;
            }

            if (Slicing.IsSolved(session.Board))
            {
                Complete(session);
                message = "Circuit linked. The security core yields.";
            }

            return true;
        }

        public static IReadOnlyList<EligibleSlicingTool> GetEligibleTools(uint player)
        {
            var session = Get(player);
            if (session == null || !CanActivateTool(session))
                return Array.Empty<EligibleSlicingTool>();

            var tools = new List<EligibleSlicingTool>();
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                var type = (SlicingToolType)GetLocalInt(item, ToolTypeVariable);
                var tier = GetLocalInt(item, ToolTierVariable);
                if (type == SlicingToolType.Invalid || tier < session.Tier)
                    continue;

                tools.Add(new EligibleSlicingTool
                {
                    Item = item,
                    Name = GetName(item),
                    Type = type,
                    Tier = tier
                });
            }

            return tools;
        }

        public static void Abort(uint player)
        {
            var session = Get(player);
            if (session == null)
                return;

            if (session.HasCommitted)
                Fail(session, true);
            else
                Release(session);
        }

        public static int GetFailures(uint target)
        {
            return GetLocalInt(target, FailuresVariable);
        }

        public static int GetIntegrity(uint target)
        {
            var integrity = GetLocalInt(target, IntegrityVariable);
            return integrity <= 0 && GetLocalInt(target, FailuresVariable) == 0 ? 100 : integrity;
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnPlayerExit()
        {
            var player = GetExitingObject();
            if (GetIsPC(player))
                Abort(player);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath()
        {
            var player = GetLastPlayerDied();
            Abort(player);

            // Abort() only tears down the session bookkeeping; it never closes the NUI window.
            // Gui.TogglePlayerWindow is a strict toggle, so leaving the window open here would
            // make the player's next lockbox/terminal use open a fresh session and then
            // immediately close it again via the stale window's OnWindowClosed -> Abort.
            if (Gui.IsWindowOpen(player, GuiWindowType.Slicing))
                Gui.CloseWindow(player, GuiWindowType.Slicing, player);
        }

        private static string ValidateAction(uint player, out ActiveSlicingSession session)
        {
            session = Get(player);
            if (session == null)
                return "There is no active slicing attempt.";
            if (!GetIsObjectValid(session.Target))
            {
                Release(session);
                return "The slicing target is no longer available.";
            }
            if (GetIsInCombat(player))
                return "You cannot continue slicing while in combat.";

            var playerId = GetObjectUUID(player);
            var owner = GetLocalString(session.Target, OwnerVariable);
            if (!IsClaimOwner(playerId, owner))
            {
                RemoveSession(session);
                return "Your slicing claim is no longer active.";
            }
            if (session.Source == SlicingSourceType.Lockbox && GetItemPossessor(session.Target) != player)
                return "The lockbox must remain in your inventory.";
            if (session.Source == SlicingSourceType.Terminal && GetDistanceBetween(player, session.Target) > 5f)
                return "You are too far away from the terminal.";
            if (session.PrimedToolItem != OBJECT_INVALID &&
                (!GetIsObjectValid(session.PrimedToolItem) || GetItemPossessor(session.PrimedToolItem) != player))
            {
                session.PrimedTool = SlicingToolType.Invalid;
                session.PrimedToolItem = OBJECT_INVALID;
                return "The primed slicing tool must remain in your inventory until its effect is used.";
            }

            Touch(session);
            return string.Empty;
        }

        private static bool TryClaim(
            uint player,
            uint target,
            SlicingSourceType source,
            int tier,
            out string error)
        {
            error = string.Empty;
            var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var playerId = GetObjectUUID(player);
            var owner = GetLocalString(target, OwnerVariable);
            var ownerAt = GetLocalInt(target, OwnerTimestampVariable);

            if (!string.IsNullOrWhiteSpace(owner) && owner != playerId)
            {
                if (now - ownerAt < ClaimTimeoutSeconds)
                {
                    error = "Another slicer currently has control of this target.";
                    return false;
                }

                _sessions.Remove(owner);
            }

            // TryStart already verified this player has no live in-memory session before calling
            // TryClaim, so any existing owner record here (whether it's this player or another)
            // is stale. Resolve a stale committed attempt the same way regardless of who owned it,
            // so a same-player reclaim (e.g. after a server restart wiped the session) can't dodge
            // the abandonment penalty on a committed attempt.
            if (!string.IsNullOrWhiteSpace(owner) &&
                GetLocalInt(target, CommittedVariable) == 1 &&
                ResolveAbandonedFailure(target, source, tier))
            {
                error = "The abandoned intrusion destabilizes and destroys the target.";
                return false;
            }

            SetLocalString(target, OwnerVariable, playerId);
            SetLocalInt(target, OwnerTimestampVariable, now);
            DeleteLocalInt(target, CommittedVariable);
            return true;
        }

        private static bool PrepareAction(ActiveSlicingSession session, int cost, out string message)
        {
            if (session.PrimedTool == SlicingToolType.TraceFuse)
            {
                session.TraceRemaining += 1;
                ConsumePrimedTool(session);
                Commit(session);
            }

            if (session.TraceRemaining < cost)
            {
                message = "Insufficient trace remains for that action.";
                return false;
            }

            AddHistory(session);
            message = string.Empty;
            return true;
        }

        private static int GetActionCost(
            ActiveSlicingSession session,
            SlicingToolType matchingFreeTool,
            int normalCost,
            out bool consumesTool)
        {
            consumesTool = false;
            if (session.FreeActionsRemaining > 0)
                return 0;

            if (session.PrimedTool == SlicingToolType.NullSignatureLattice)
            {
                consumesTool = true;
                return 0;
            }

            if (session.PrimedTool == matchingFreeTool)
            {
                consumesTool = true;
                return 0;
            }

            return normalCost;
        }

        private static void ApplyFreeActionState(ActiveSlicingSession session, bool consumesTool)
        {
            if (session.FreeActionsRemaining > 0)
                session.FreeActionsRemaining--;
            else if (consumesTool && session.PrimedTool == SlicingToolType.NullSignatureLattice)
                session.FreeActionsRemaining = 2;
        }

        private static bool ResolveAfterAction(ActiveSlicingSession session, int cost, out string message)
        {
            session.TraceRemaining -= cost;
            Touch(session);

            if (Slicing.IsSolved(session.Board))
            {
                Complete(session);
                message = "Circuit linked. The security core yields.";
                return true;
            }

            if (session.TraceRemaining <= 0)
            {
                Fail(session, true);
                message = "The trace closes before the route reaches the core.";
                return false;
            }

            message = cost == 0 ? "Action completed without trace." : $"Action completed. Trace remaining: {session.TraceRemaining}.";
            return true;
        }

        private static void Complete(ActiveSlicingSession session)
        {
            GrantXP(session.Player, session.Tier);

            var reward = SlicingReward.Roll(
                session.Source,
                session.Tier,
                Random.Next(10000),
                Random.Next(int.MaxValue));
            CreateItemOnObject(reward.Resref, session.Player, reward.Quantity);

            var playerId = GetObjectUUID(session.Player);
            Log.Write(LogGroup.Crafting,
                $"Player '{GetName(session.Player)}' ({playerId}) completed {session.Source} slicing board {session.Board.BoardId} and received {reward.Quantity}x '{reward.Resref}'.");
            SendMessageToPC(session.Player, $"You recover {reward.Quantity}x {Cache.GetItemNameByResref(reward.Resref)}.");

            Release(session);
            if (session.Source == SlicingSourceType.Lockbox)
                DestroyObject(session.Target);
            else
                Spawn.DespawnAndQueueRespawn(session.Target);
        }

        private static void Fail(ActiveSlicingSession session, bool notifyPlayer)
        {
            var destroyed = ResolveFailure(session.Target, session.Source, session.Tier);
            var failures = GetLocalInt(session.Target, FailuresVariable);
            var nextChance = Slicing.GetDestructionChance(failures + 1);

            if (notifyPlayer && GetIsObjectValid(session.Player))
            {
                SendMessageToPC(session.Player, destroyed
                    ? "The intrusion overloads the target. Its contents are destroyed."
                    : $"The attempt fails. The next failed attempt has a {nextChance}% destruction risk.");
            }

            Log.Write(LogGroup.Crafting,
                $"Player '{GetName(session.Player)}' ({GetObjectUUID(session.Player)}) failed {session.Source} slicing board {session.Board.BoardId} (failure {failures}, destroyed={destroyed}).");
            Release(session);
        }

        private static bool ResolveAbandonedFailure(uint target, SlicingSourceType source, int tier)
        {
            return ResolveFailure(target, source, tier);
        }

        private static bool ResolveFailure(uint target, SlicingSourceType source, int tier)
        {
            var failures = GetLocalInt(target, FailuresVariable) + 1;
            SetLocalInt(target, FailuresVariable, failures);
            var chance = Slicing.GetDestructionChance(failures);
            SetLocalInt(target, IntegrityVariable, Math.Max(0, 100 - chance));
            DeleteLocalString(target, OwnerVariable);
            DeleteLocalInt(target, OwnerTimestampVariable);
            DeleteLocalInt(target, CommittedVariable);

            var destroyed = chance >= 100 || Random.Next(100) < chance;
            if (!destroyed)
                return false;

            SetLocalInt(target, IntegrityVariable, 0);
            if (source == SlicingSourceType.Lockbox)
                DestroyObject(target);
            else
                Spawn.DespawnAndQueueRespawn(target);
            return true;
        }

        private static void Commit(ActiveSlicingSession session)
        {
            session.HasCommitted = true;
            SetLocalInt(session.Target, CommittedVariable, 1);
            Touch(session);
        }

        private static void Touch(ActiveSlicingSession session)
        {
            SetLocalInt(session.Target, OwnerTimestampVariable, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        private static void Release(ActiveSlicingSession session)
        {
            var playerId = GetObjectUUID(session.Player);
            RemoveSession(session);

            if (GetIsObjectValid(session.Target) &&
                IsClaimOwner(playerId, GetLocalString(session.Target, OwnerVariable)))
            {
                DeleteLocalString(session.Target, OwnerVariable);
                DeleteLocalInt(session.Target, OwnerTimestampVariable);
                DeleteLocalInt(session.Target, CommittedVariable);
            }
        }

        private static void RemoveSession(ActiveSlicingSession session)
        {
            var playerId = GetObjectUUID(session.Player);
            if (!string.IsNullOrWhiteSpace(playerId))
                _sessions.Remove(playerId);
        }

        private static bool IsClaimOwner(string playerId, string owner)
        {
            return !string.IsNullOrWhiteSpace(playerId) && playerId == owner;
        }

        private static bool CanActivateTool(ActiveSlicingSession session)
        {
            return !session.HasUsedTool;
        }

        private static void AddHistory(ActiveSlicingSession session)
        {
            session.History.Add(new SessionSnapshot
            {
                Board = session.Board.Clone(),
                TraceRemaining = session.TraceRemaining
            });
        }

        private static void UndoActions(ActiveSlicingSession session, int count)
        {
            var undoCount = Math.Min(count, session.History.Count);
            var snapshotIndex = session.History.Count - undoCount;
            var snapshot = session.History[snapshotIndex];
            session.Board = snapshot.Board.Clone();
            session.TraceRemaining = snapshot.TraceRemaining;
            session.History.RemoveRange(snapshotIndex, undoCount);
        }

        private static void ConsumePrimedTool(ActiveSlicingSession session)
        {
            if (GetIsObjectValid(session.PrimedToolItem) && GetItemPossessor(session.PrimedToolItem) == session.Player)
                Item.ReduceItemStack(session.PrimedToolItem, 1);

            session.PrimedTool = SlicingToolType.Invalid;
            session.PrimedToolItem = OBJECT_INVALID;
            Commit(session);
        }

        private static bool RevealMembership(ActiveSlicingSession session, int index)
        {
            if (!IsRouteTile(session, index))
                return false;

            session.Board.Tiles[index].IsRouteRevealed = true;
            return true;
        }

        private static bool RevealOrientation(ActiveSlicingSession session, int index)
        {
            if (!RevealMembership(session, index))
                return false;

            session.Board.Tiles[index].IsOrientationRevealed = true;
            return true;
        }

        private static bool IsRouteTile(ActiveSlicingSession session, int index)
        {
            return index >= 0 && index < session.Board.Tiles.Count && session.Board.Tiles[index].SolutionIndex >= 0;
        }

        private static void RevealForwardRoute(ActiveSlicingSession session, int selectedIndex, int count)
        {
            var selected = session.Board.Tiles[selectedIndex];
            if (selected.SolutionIndex < 0)
                return;

            var ordered = session.Board.Tiles
                .Select((tile, index) => (tile, index))
                .Where(x => x.tile.RouteOrder >= 0)
                .OrderBy(x => x.tile.RouteOrder)
                .ToList();
            var selectedRouteIndex = ordered.FindIndex(x => x.index == selectedIndex);
            for (var offset = 1; offset <= count && selectedRouteIndex + offset < ordered.Count; offset++)
                ordered[selectedRouteIndex + offset].tile.IsRouteRevealed = true;
        }

        private static void RevealAllRouteTiles(ActiveSlicingSession session)
        {
            for (var index = 0; index < session.Board.Tiles.Count; index++)
            {
                if (IsRouteTile(session, index))
                    session.Board.Tiles[index].IsRouteRevealed = true;
            }
        }

        private static void GrantXP(uint player, int tier)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbSkill = dbPlayer.Skills[SkillType.Espionage];
            var delta = _tierSkillRequirement[tier - 1] - dbSkill.Rank;
            Skill.GiveSkillXP(player, SkillType.Espionage, Skill.GetDeltaXP(delta), false, false);
        }
    }
}

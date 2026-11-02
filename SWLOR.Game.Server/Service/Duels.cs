using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.DuelService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Duels at a city's Dueling Ring. One player challenges another inside the ring and the target accepts. While the
    /// duel lasts, a blow that would kill a duelist subdues them instead, whoever lands it. A third party's blow voids the
    /// duel but leaves both duelists protected for a few seconds, so that blow still only subdues. Leaving the ring or
    /// logging out forfeits, and a finished duel is posted to the city's Gazette. Duels live in memory, so a restart
    /// voids them. The rules live in <see cref="DuelRules"/>.
    /// </summary>
    public static class Duels
    {
        /// <summary>The tag every ring terminal carries.</summary>
        public const string RingTag = "DUELING_RING";

        private const string RingName = "Dueling Ring";
        private const string RingModelResref = "_mdrn_pl_tilec01";
        private const string ModelVariable = "DUELING_RING_MODEL";
        private const float MonitorSeconds = 1.0f;

        private static readonly Dictionary<uint, DuelRecord> _duelsByDuelist = new();
        private static readonly Dictionary<uint, DuelChallenge> _challengesByTarget = new();
        private static IDisposable _monitor;

        // The ring

        /// <summary>
        /// A ring terminal was placed, moved or loaded: it gets its name and use script, and the marked ring stands under it.
        /// </summary>
        public static void OnRingPlaced(WorldProperty building, uint console)
        {
            SetName(console, RingName);
            SetTag(console, RingTag);
            SetUseableFlag(console, true);
            SetEventScript(console, EventScript.Placeable_OnUsed, ScriptName.OnDuelingRingUsed);

            DestroyModel(console);
            var model = CreateObject(ObjectType.Placeable, RingModelResref, GetLocation(console));
            SetName(model, RingName);
            SetPlotFlag(model, true);
            SetUseableFlag(model, false);
            SetLocalObject(console, ModelVariable, model);
        }

        /// <summary>A ring was picked up: its marked ring goes with it, and any duel there is called off.</summary>
        public static void OnRingRetrieved(WorldProperty building, uint console)
        {
            DestroyModel(console);

            foreach (var record in _duelsByDuelist.Values.Distinct().Where(x => x.Ring == console).ToList())
                Cancel(record, "The Dueling Ring was taken down, so the duel is called off.");

            foreach (var key in _challengesByTarget.Where(x => x.Value.Ring == console).Select(x => x.Key).ToList())
                _challengesByTarget.Remove(key);
        }

        /// <summary>A ring terminal was used: open its menu.</summary>
        [NWNEventHandler(ScriptName.OnDuelingRingUsed)]
        public static void UseRing()
        {
            var player = GetLastUsedBy();
            if (!PvpCombat.IsPlayer(player))
                return;

            ConversationMenu.Start(player, OBJECT_SELF, nameof(DuelingRingDialog));
        }

        private static void DestroyModel(uint console)
        {
            var model = GetLocalObject(console, ModelVariable);
            if (GetIsObjectValid(model))
                DestroyObject(model);
            DeleteLocalObject(console, ModelVariable);
        }

        /// <summary>Whether a creature stands inside the ring's radius, in the ring's area.</summary>
        public static bool IsInsideRing(uint ring, uint creature)
        {
            if (!GetIsObjectValid(ring) || !GetIsObjectValid(creature) || GetArea(ring) != GetArea(creature))
                return false;

            var center = GetPosition(ring);
            var position = GetPosition(creature);
            return DuelRules.IsInsideRing(center.X, center.Y, position.X, position.Y);
        }

        /// <summary>The players, other than the challenger, who stand inside the ring and can be challenged.</summary>
        public static List<uint> GetChallengeablePlayers(uint challenger, uint ring)
        {
            if (!GetIsObjectValid(ring))
                return new List<uint>();

            return Area.GetPlayersInArea(GetArea(ring))
                .Where(x => x != challenger && PvpCombat.IsPlayer(x) && IsInsideRing(ring, x))
                .ToList();
        }

        // Challenges

        /// <summary>Why the challenger cannot challenge the target now, or an empty string.</summary>
        public static string GetChallengeError(uint challenger, uint target, uint ring)
        {
            if (!PvpCombat.IsPlayer(challenger))
                return "Only players can duel.";
            if (!PvpCombat.IsPlayer(target) || target == challenger)
                return "Only another player can be challenged.";
            if (!GetIsObjectValid(ring))
                return "The Dueling Ring is gone.";
            if (GetIsDead(challenger) || GetIsDead(target))
                return "A fallen player cannot duel.";
            if (!IsInsideRing(ring, challenger))
                return "You must stand inside the ring to challenge someone.";

            var name = PlayerName.GetDisplayName(challenger, target);
            if (!IsInsideRing(ring, target))
                return $"{name} is not inside the ring.";
            if (GetDuel(challenger) != null)
                return "You are already in a duel.";
            if (GetDuel(target) != null)
                return $"{name} is already in a duel.";
            if (GetChallenge(target) != null)
                return $"{name} already has a challenge waiting.";

            return string.Empty;
        }

        /// <summary>
        /// The challenger challenges the target. The target gets a menu to accept or decline, and a challenger can
        /// have one challenge waiting at a time.
        /// </summary>
        /// <returns>False, after telling the challenger why, when the challenge was not made.</returns>
        public static bool Challenge(uint challenger, uint target, uint ring)
        {
            var error = GetChallengeError(challenger, target, ring);
            if (!string.IsNullOrEmpty(error))
            {
                FloatingTextStringOnCreature(error, challenger, false);
                return false;
            }

            foreach (var key in _challengesByTarget.Where(x => x.Value.Challenger == challenger).Select(x => x.Key).ToList())
                _challengesByTarget.Remove(key);

            _challengesByTarget[target] = new DuelChallenge(
                challenger, target, ring, DateTime.UtcNow.AddSeconds(DuelRules.ChallengeSeconds));

            SendMessageToPC(challenger,
                $"You challenge {PlayerName.GetDisplayName(challenger, target)} to a duel. They have {DuelRules.ChallengeSeconds:0} seconds to answer.");
            ConversationMenu.Start(target, target, nameof(DuelChallengeDialog));
            return true;
        }

        /// <summary>The challenge waiting for this target, or null when there is none or it ran out.</summary>
        public static DuelChallenge GetChallenge(uint target)
        {
            if (!_challengesByTarget.TryGetValue(target, out var challenge))
                return null;

            if (challenge.ExpiresUtc >= DateTime.UtcNow)
                return challenge;

            _challengesByTarget.Remove(target);
            return null;
        }

        /// <summary>The target turned the challenge down.</summary>
        public static void Decline(uint target)
        {
            var challenge = GetChallenge(target);
            if (challenge == null)
                return;

            _challengesByTarget.Remove(target);
            if (GetIsObjectValid(challenge.Challenger))
                SendMessageToPC(challenge.Challenger, $"{PlayerName.GetDisplayName(challenge.Challenger, target)} declined your challenge.");
        }

        /// <summary>The target accepted: everything is checked again and the duel begins.</summary>
        public static void Accept(uint target)
        {
            var challenge = GetChallenge(target);
            if (challenge == null)
            {
                FloatingTextStringOnCreature("The challenge has expired.", target, false);
                return;
            }

            _challengesByTarget.Remove(target);
            var error = GetChallengeError(challenge.Challenger, target, challenge.Ring);
            if (!string.IsNullOrEmpty(error))
            {
                FloatingTextStringOnCreature("The challenge is no longer valid.", target, false);
                if (GetIsObjectValid(challenge.Challenger))
                    FloatingTextStringOnCreature(error, challenge.Challenger, false);
                return;
            }

            StartDuel(challenge.Challenger, target, challenge.Ring);
        }

        // Duels

        /// <summary>
        /// Begins a duel. Both players are made hostile to each other for it, as the Player List does, and put back as
        /// they were when it ends. The caller has checked <see cref="GetChallengeError"/>.
        /// </summary>
        public static DuelRecord StartDuel(uint challenger, uint target, uint ring)
        {
            var record = new DuelRecord(challenger, target, ring, GetRingCityId(ring), DateTime.UtcNow)
            {
                ChallengerLabel = Disguise.GetDisplayDescriptor(challenger),
                TargetLabel = Disguise.GetDisplayDescriptor(target),
                ChallengerWasHostile = PlayerList.IsHostileTo(challenger, target),
                TargetWasHostile = PlayerList.IsHostileTo(target, challenger)
            };

            PlayerList.SetHostile(challenger, target, true);
            PlayerList.SetHostile(target, challenger, true);

            _duelsByDuelist[challenger] = record;
            _duelsByDuelist[target] = record;
            _monitor ??= Scheduler.ScheduleRepeating(Monitor, TimeSpan.FromSeconds(MonitorSeconds));

            const string Rules = "The duel begins. A blow that would kill either of you subdues instead. Leaving the ring or logging out forfeits.";
            SendMessageToPC(challenger, ColorToken.Combat(Rules));
            SendMessageToPC(target, ColorToken.Combat(Rules));
            return record;
        }

        /// <summary>The duel a player is in, or null. A voided duel whose protection ran out is dropped here.</summary>
        public static DuelRecord GetDuel(uint player)
        {
            if (!_duelsByDuelist.TryGetValue(player, out var record))
                return null;

            if (!DuelRules.IsOver(record, DateTime.UtcNow))
                return record;

            Forget(record);
            return null;
        }

        /// <summary>
        /// A blow is about to land on a creature. When it is a duelist and the source is not one of the two duelists,
        /// the duel is voided and both stay protected, before the damage lands and before <see cref="Death"/> can run.
        /// A duelist's own pets and the effects they applied count as the duelist.
        /// </summary>
        /// <param name="defender">The creature about to be hit.</param>
        /// <param name="attacker">The creature, or damaging effect's creator, behind the blow.</param>
        public static void RecordDamage(uint defender, uint attacker)
        {
            if (_duelsByDuelist.Count == 0)
                return;

            var record = GetDuel(defender);
            if (record == null || record.State != DuelState.Active)
                return;

            var source = ResolveSource(attacker);
            if (DuelRules.ClassifyBlow(record, GetIsObjectValid(source), source) == DuelBlowType.Interference)
                DuelRules.Interrupt(record, DateTime.UtcNow);
        }

        /// <summary>
        /// A player fell. Returns true when a duel, or the protection after one was voided, turns the fall into a
        /// subdual. A fall from a duelist's blow counts toward the result; any other fall that the damage hook did not
        /// see voids the duel and protects both duelists.
        /// </summary>
        /// <param name="duelist">The player who fell.</param>
        /// <param name="hostile">Whoever the game credits with the blow.</param>
        public static bool TakeFall(uint duelist, uint hostile)
        {
            var record = GetDuel(duelist);
            if (record == null)
                return false;

            var now = DateTime.UtcNow;
            if (record.State == DuelState.Active)
            {
                var source = ResolveSource(hostile);
                if (DuelRules.ClassifyBlow(record, GetIsObjectValid(source), source) == DuelBlowType.Duel)
                {
                    // A second fall in the same pass makes it a draw, so the result waits for that pass to end.
                    if (DuelRules.RecordFall(record, DuelRules.GetSide(record, duelist), now))
                        DelayCommand((float)DuelRules.SamePassSeconds, () => Finish(record));
                }
                else
                {
                    DuelRules.Interrupt(record, now);
                    TellInterruption(record);
                }
            }

            return DuelRules.ShouldSubdue(record, now);
        }

        /// <summary>A duelist or a player in a waiting challenge logged out.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForfeitOnExit()
        {
            var player = GetExitingObject();
            foreach (var key in _challengesByTarget
                         .Where(x => x.Key == player || x.Value.Challenger == player)
                         .Select(x => x.Key)
                         .ToList())
            {
                _challengesByTarget.Remove(key);
            }

            var record = GetDuel(player);
            if (record == null)
                return;

            if (record.State == DuelState.Active)
            {
                DuelRules.RecordForfeit(record, DuelRules.GetSide(record, player));
                Finish(record);
            }
            else
            {
                Forget(record);
            }
        }

        /// <summary>
        /// Runs each second while a duel exists: tells duelists a duel was voided, drops duels whose protection ran out,
        /// calls off duels whose ring is gone, and forfeits a duelist who left the ring.
        /// </summary>
        private static void Monitor()
        {
            var now = DateTime.UtcNow;
            foreach (var record in _duelsByDuelist.Values.Distinct().ToList())
            {
                if (record.State == DuelState.Interrupted)
                {
                    TellInterruption(record);
                    if (DuelRules.IsOver(record, now))
                        Forget(record);
                    continue;
                }

                if (!GetIsObjectValid(record.Ring))
                {
                    Cancel(record, "The Dueling Ring is gone, so the duel is called off.");
                    continue;
                }

                RefreshLabels(record);

                // A result is waiting only for the end of the pass in which the first duelist fell.
                if (record.FirstFallUtc.HasValue)
                {
                    if (!DuelRules.IsSamePass(record.FirstFallUtc.Value, now))
                        Finish(record);
                    continue;
                }

                if (!IsInsideRing(record.Ring, record.Challenger))
                    DuelRules.RecordForfeit(record, DuelSide.Challenger);
                if (!IsInsideRing(record.Ring, record.Target))
                    DuelRules.RecordForfeit(record, DuelSide.Target);

                if (record.ChallengerForfeited || record.TargetForfeited)
                    Finish(record);
            }

            foreach (var key in _challengesByTarget.Where(x => x.Value.ExpiresUtc < now).Select(x => x.Key).ToList())
                _challengesByTarget.Remove(key);

            if (_duelsByDuelist.Count == 0 && _challengesByTarget.Count == 0)
            {
                _monitor?.Dispose();
                _monitor = null;
            }
        }

        // Endings

        /// <summary>
        /// Ends a duel that has a result: tells the duelists and posts it to the Gazette. A duel nobody lost, or that
        /// is already over, ends without one. A duel a third party voided never has a result; it stays only for the
        /// protection it gave, until that lapses.
        /// </summary>
        private static void Finish(DuelRecord record)
        {
            if (record.State != DuelState.Active || !Forget(record))
                return;

            var outcome = DuelRules.GetOutcome(record);
            if (outcome == null)
                return;

            RefreshLabels(record);
            TellOutcome(record, outcome.Value);
            CityGazette.Post(
                record.CityId,
                CityGazetteEntryType.Notice,
                DuelRules.BuildResultText(outcome.Value, record.ChallengerLabel, record.TargetLabel));
        }

        /// <summary>Ends a duel with no result.</summary>
        private static void Cancel(DuelRecord record, string message)
        {
            if (!Forget(record))
                return;

            foreach (var duelist in new[] { record.Challenger, record.Target })
            {
                if (GetIsObjectValid(duelist))
                    SendMessageToPC(duelist, ColorToken.Combat(message));
            }
        }

        /// <summary>
        /// Drops a duel and puts both players' attitudes back. Returns false when the duel was already dropped.
        /// </summary>
        private static bool Forget(DuelRecord record)
        {
            var wasKnown = false;
            foreach (var duelist in new[] { record.Challenger, record.Target })
            {
                if (_duelsByDuelist.TryGetValue(duelist, out var known) && known == record)
                {
                    _duelsByDuelist.Remove(duelist);
                    wasKnown = true;
                }
            }

            RestoreHostility(record);
            return wasKnown;
        }

        private static void RestoreHostility(DuelRecord record)
        {
            if (record.HostilityRestored)
                return;

            record.HostilityRestored = true;
            if (!GetIsObjectValid(record.Challenger) || !GetIsObjectValid(record.Target))
                return;

            if (!record.ChallengerWasHostile)
                PlayerList.SetHostile(record.Challenger, record.Target, false);
            if (!record.TargetWasHostile)
                PlayerList.SetHostile(record.Target, record.Challenger, false);
        }

        // Who dealt a blow

        /// <summary>
        /// Traces a blow back to who is behind it. The creator of a damaging effect arrives here as the attacker.
        /// </summary>
        private static uint ResolveSource(uint attacker)
        {
            return DuelRules.TraceSource(attacker, GetIsObjectValid, PvpCombat.GetControllingPlayer, GetMaster);
        }

        private static string GetRingCityId(uint ring)
        {
            var propertyId = Property.GetPropertyId(ring);
            var structure = string.IsNullOrWhiteSpace(propertyId) ? null : DB.Get<WorldProperty>(propertyId);
            return structure?.ParentPropertyId ?? string.Empty;
        }

        /// <summary>How each duelist looks to strangers now. A duelist who is gone keeps the last look.</summary>
        private static void RefreshLabels(DuelRecord record)
        {
            if (GetIsObjectValid(record.Challenger))
                record.ChallengerLabel = Disguise.GetDisplayDescriptor(record.Challenger);
            if (GetIsObjectValid(record.Target))
                record.TargetLabel = Disguise.GetDisplayDescriptor(record.Target);
        }

        // Messages

        private static void TellInterruption(DuelRecord record)
        {
            if (!record.InterruptionNoticePending)
                return;

            record.InterruptionNoticePending = false;
            RestoreHostility(record);

            var message = $"Someone else joined the fight, so the duel is void. You stay protected for {DuelRules.ProtectionSeconds:0} seconds: " +
                          "a blow that would kill you subdues you instead.";
            foreach (var duelist in new[] { record.Challenger, record.Target })
            {
                if (GetIsObjectValid(duelist))
                    SendMessageToPC(duelist, ColorToken.Combat(message));
            }
        }

        private static void TellOutcome(DuelRecord record, DuelOutcome outcome)
        {
            foreach (var side in new[] { DuelSide.Challenger, DuelSide.Target })
            {
                var duelist = side == DuelSide.Challenger ? record.Challenger : record.Target;
                var opponent = side == DuelSide.Challenger ? record.Target : record.Challenger;
                if (!GetIsObjectValid(duelist))
                    continue;

                var name = GetIsObjectValid(opponent) ? PlayerName.GetDisplayName(duelist, opponent) : "your opponent";
                string message;
                if (outcome.Type == DuelOutcomeType.Draw)
                    message = $"The duel against {name} ends in a draw.";
                else if (outcome.Winner == side)
                    message = outcome.Type == DuelOutcomeType.Forfeit
                        ? ColorToken.Green($"{name} forfeited the duel. You win.")
                        : ColorToken.Green($"You won the duel against {name}.");
                else
                    message = outcome.Type == DuelOutcomeType.Forfeit
                        ? $"You forfeited the duel against {name}."
                        : $"You lost the duel against {name}.";

                SendMessageToPC(duelist, message);
            }
        }
    }
}

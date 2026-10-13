using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Aggregates a recording's events into per-participant totals. Runs on the recording writer
    /// thread; events must be added in the order they were recorded.
    /// </summary>
    public sealed class CombatRecordSummaryBuilder
    {
        // A snapshot gap longer than this means the participant left the area or the server
        // stalled; the gap is not credited as observed or combat time.
        private const long MaximumSnapshotGapMs = 5000;

        private readonly Dictionary<int, CombatRecordParticipantSummary> _participants = new();
        private readonly Dictionary<int, long> _lastSnapshotMs = new();
        private readonly Dictionary<int, bool> _lastSnapshotInCombat = new();
        private readonly CombatRecordSummary _summary = new();

        public void Add(in CombatRecordEvent e)
        {
            _summary.EventCount++;

            switch (e.Type)
            {
                case CombatRecordEventType.RecordingStarted:
                    _summary.Label = e.Text;
                    _summary.AreaName = e.Text2;
                    _summary.AreaResref = e.SourceName;
                    _summary.StartedUtc = DateTimeOffset.FromUnixTimeMilliseconds(e.TimestampMs).UtcDateTime;
                    break;
                case CombatRecordEventType.RecordingStopped:
                    _summary.StoppedUtc = DateTimeOffset.FromUnixTimeMilliseconds(e.TimestampMs).UtcDateTime;
                    break;
                case CombatRecordEventType.Participant:
                    var participant = GetParticipant(e.ActorId);
                    participant.Name = e.Text;
                    participant.IsPlayer = e.Flag;
                    participant.Resref = string.IsNullOrEmpty(e.Text2) ? null : e.Text2;
                    break;
                case CombatRecordEventType.AbilityCast:
                case CombatRecordEventType.AbilityQueued:
                    var abilityName = CombatRecordSourceLabel.Format(CombatRecordSourceKind.Ability, e.SourceName);
                    var uses = GetParticipant(e.ActorId).AbilityUses;
                    uses.TryGetValue(abilityName, out var count);
                    uses[abilityName] = count + 1;
                    break;
                case CombatRecordEventType.Damage:
                    AddDamage(e);
                    break;
                case CombatRecordEventType.TemporaryHitPointsAbsorbed:
                    var grantLabel = CombatRecordSourceLabel.Format(e.SourceKind, e.SourceName, e.SourceCode);
                    Add(GetParticipant(e.TargetId).TemporaryHitPointsAbsorbed, grantLabel, e.Value0);
                    if (e.Value1 > 0)
                        Add(GetParticipant(e.Value1).TemporaryHitPointsGrantedAbsorbed, grantLabel, e.Value0);
                    break;
                case CombatRecordEventType.Healing:
                    AddHealing(e);
                    break;
                case CombatRecordEventType.TemporaryHitPointsGranted:
                    var label = CombatRecordSourceLabel.Format(e.SourceKind, e.SourceName, e.SourceCode);
                    if (e.ActorId > 0)
                        Add(GetParticipant(e.ActorId).TemporaryHitPointsGranted, label, e.Value0);
                    Add(GetParticipant(e.TargetId).TemporaryHitPointsReceived, label, e.Value0);
                    break;
                case CombatRecordEventType.Resource:
                    AddResource(e);
                    break;
                case CombatRecordEventType.Death:
                    GetParticipant(e.TargetId).Deaths++;
                    if (e.ActorId > 0 && e.ActorId != e.TargetId)
                        GetParticipant(e.ActorId).Kills++;
                    break;
                case CombatRecordEventType.Subdual:
                    GetParticipant(e.TargetId).Subdued++;
                    break;
                case CombatRecordEventType.Resurrection:
                    GetParticipant(e.TargetId).RevivesReceived++;
                    if (e.ActorId > 0 && e.ActorId != e.TargetId)
                        GetParticipant(e.ActorId).RevivesPerformed++;
                    break;
                case CombatRecordEventType.Snapshot:
                    AddSnapshot(e);
                    break;
            }
        }

        public CombatRecordSummary Build(DateTime stoppedUtc)
        {
            if (_summary.StoppedUtc == default)
                _summary.StoppedUtc = stoppedUtc;

            _summary.Participants.Clear();
            var ids = new List<int>(_participants.Keys);
            ids.Sort();
            foreach (var id in ids)
            {
                // Id 0 collects events whose creature was never registered.
                if (id > 0)
                    _summary.Participants.Add(_participants[id]);
            }

            return _summary;
        }

        private void AddDamage(in CombatRecordEvent e)
        {
            var hitPointsLost = Math.Max(0, e.Value1 - e.Value2);
            var absorbed = Math.Max(0, e.Value3 - e.Value4);
            var label = CombatRecordSourceLabel.Format(e.SourceKind, e.SourceName, e.SourceCode);

            // Hit points paid for an ability are a cost, not damage dealt to an enemy.
            if (e.SourceKind == CombatRecordSourceKind.AbilityCost)
            {
                Add(GetParticipant(e.TargetId).HitPointsSpent, label, hitPointsLost + absorbed);
                return;
            }

            if (e.ActorId > 0)
                AddDamage(GetParticipant(e.ActorId).DamageDealt, label, e.Value0, hitPointsLost, absorbed);
            AddDamage(GetParticipant(e.TargetId).DamageTaken, label, e.Value0, hitPointsLost, absorbed);
        }

        private static void AddDamage(
            Dictionary<string, CombatRecordDamageTotals> totals, string label, int total, int hitPointsLost, int absorbed)
        {
            if (!totals.TryGetValue(label, out var damage))
            {
                damage = new CombatRecordDamageTotals();
                totals[label] = damage;
            }

            damage.Hits++;
            damage.Total += total;
            damage.HitPointsLost += hitPointsLost;
            damage.TemporaryHitPointsAbsorbed += absorbed;
        }

        private void AddHealing(in CombatRecordEvent e)
        {
            var label = CombatRecordSourceLabel.Format(e.SourceKind, e.SourceName, e.SourceCode);
            if (e.ActorId > 0)
                AddHealing(GetParticipant(e.ActorId).HealingDone, label, e.Value0, e.Value1);
            AddHealing(GetParticipant(e.TargetId).HealingReceived, label, e.Value0, e.Value1);
        }

        private static void AddHealing(
            Dictionary<string, CombatRecordHealingTotals> totals, string label, int requested, int effective)
        {
            if (!totals.TryGetValue(label, out var healing))
            {
                healing = new CombatRecordHealingTotals();
                totals[label] = healing;
            }

            healing.Count++;
            healing.Requested += requested;
            healing.Effective += effective;
        }

        private void AddResource(in CombatRecordEvent e)
        {
            var delta = e.Value1;
            if (delta == 0)
                return;

            var participant = GetParticipant(e.ActorId);
            var resource = (CombatRecordResource)e.Value0;
            if (resource == CombatRecordResource.Item)
            {
                if (delta < 0)
                    Add(participant.ItemsConsumed, e.Text ?? "unknown", -delta);
                return;
            }

            var label = CombatRecordSourceLabel.Format(e.SourceKind, e.SourceName, e.SourceCode);
            if (resource == CombatRecordResource.FP)
                Add(delta < 0 ? participant.FPSpent : participant.FPRegained, label, Math.Abs(delta));
            else
                Add(delta < 0 ? participant.StaminaSpent : participant.StaminaRegained, label, Math.Abs(delta));
        }

        private void AddSnapshot(in CombatRecordEvent e)
        {
            var participant = GetParticipant(e.ActorId);
            if (_lastSnapshotMs.TryGetValue(e.ActorId, out var lastMs))
            {
                var gapMs = e.TimestampMs - lastMs;
                if (gapMs > 0 && gapMs <= MaximumSnapshotGapMs)
                {
                    participant.SecondsObserved += gapMs / 1000d;
                    if (_lastSnapshotInCombat[e.ActorId])
                        participant.SecondsInCombat += gapMs / 1000d;
                }
            }

            _lastSnapshotMs[e.ActorId] = e.TimestampMs;
            _lastSnapshotInCombat[e.ActorId] = e.Flag;
        }

        private static void Add(Dictionary<string, CombatRecordAmountTotals> totals, string label, long amount)
        {
            if (!totals.TryGetValue(label, out var total))
            {
                total = new CombatRecordAmountTotals();
                totals[label] = total;
            }

            total.Add(amount);
        }

        private CombatRecordParticipantSummary GetParticipant(int participantId)
        {
            if (!_participants.TryGetValue(participantId, out var participant))
            {
                participant = new CombatRecordParticipantSummary { Id = participantId };
                _participants[participantId] = participant;
            }

            return participant;
        }
    }
}

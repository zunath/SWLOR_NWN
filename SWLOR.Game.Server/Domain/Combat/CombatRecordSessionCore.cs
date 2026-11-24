using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The state and the event queue of a combat recording, with no services: the queue and the held events, the damage and
    /// heal application scopes, the participants, the recorded area, the damage waiting to be attributed and whether a
    /// recording runs. The recorder reads and writes it through <see cref="ICombatRecordSession"/>; the platform asks
    /// <see cref="ICombatRecordingState"/> whether a recording runs.
    /// </summary>
    public class CombatRecordSessionCore : ICombatRecordSession
    {
        private readonly Dictionary<uint, int> _participantIds = new();
        private readonly List<uint> _participants = new();
        private readonly List<CombatRecordPendingDamage> _pendingDamage = new();
        private readonly Dictionary<uint, uint> _lastDamageSources = new();
        // One frame per heal the native hook is applying, innermost last, so a heal nested inside
        // another on the same target keeps its own PvP reduction.
        private readonly List<(uint Target, int PvpReduced)> _healApplications = new();
        private readonly List<CombatRecordDamageApplication> _damageApplications = new();
        private readonly List<CombatRecordEvent> _heldEvents = new();
        private readonly List<CombatRecordEvent> _awaitingHeal = new();
        private uint _awaitingHealTarget = OBJECT_INVALID;
        private ulong _awaitingHealFrame;
        private bool _isInHealingReaction;
        private int _heldInsertIndex = -1;
        private long _heldTimestampMs;

        private CombatRecordWriter _writer;
        private uint _area;
        private int _eventsEnqueued;

        /// <summary>
        /// True while a recording runs. Hooks check this before doing any work.
        /// </summary>
        public bool IsRecording { get; set; }

        /// <summary>
        /// The writer of the current or most recent recording. Its Completion task finishes once
        /// the summary has been written.
        /// </summary>
        public CombatRecordWriter LastWriter { get; set; }

        public CombatRecordWriter Writer
        {
            get => _writer;
            set => _writer = value;
        }

        public uint Area
        {
            get => _area;
            set => _area = value;
        }

        public int EventsEnqueued
        {
            get => _eventsEnqueued;
            set => _eventsEnqueued = value;
        }

        public IReadOnlyList<uint> Participants => _participants;

        public IReadOnlyDictionary<uint, int> ParticipantIds => _participantIds;

        public List<CombatRecordPendingDamage> PendingDamage => _pendingDamage;

        public Dictionary<uint, uint> LastDamageSources => _lastDamageSources;

        public List<(uint Target, int PvpReduced)> HealApplications => _healApplications;

        public List<CombatRecordDamageApplication> DamageApplications => _damageApplications;

        public int HeldInsertIndex
        {
            get => _heldInsertIndex;
            set => _heldInsertIndex = value;
        }

        public long HeldTimestampMs
        {
            get => _heldTimestampMs;
            set => _heldTimestampMs = value;
        }

        public int AwaitingHealCount => _awaitingHeal.Count;

        public uint AwaitingHealTarget
        {
            get => _awaitingHealTarget;
            set => _awaitingHealTarget = value;
        }

        public ulong AwaitingHealFrame
        {
            get => _awaitingHealFrame;
            set => _awaitingHealFrame = value;
        }

        public bool IsInHealingReaction
        {
            get => _isInHealingReaction;
            set => _isInHealingReaction = value;
        }

        /// <summary>
        /// Clears what a recording keeps: the participants, the damage waiting to be attributed, the damage sources, the
        /// damage and heal applications, the held and awaiting events and the healing reaction.
        /// </summary>
        public void Reset()
        {
            _participantIds.Clear();
            _participants.Clear();
            _pendingDamage.Clear();
            _lastDamageSources.Clear();
            _damageApplications.Clear();
            _heldEvents.Clear();
            _heldInsertIndex = -1;
            _awaitingHeal.Clear();
            _healApplications.Clear();
            _isInHealingReaction = false;
        }

        /// <summary>
        /// Ends what is in flight when a recording stops: the damage applications and the healing reaction, then the
        /// events still waiting for their heal and the held events, which are written.
        /// </summary>
        public void Flush()
        {
            _damageApplications.Clear();
            _isInHealingReaction = false;
            ReleaseAwaitingHeal();
            WriteHeldEvents();
        }


        /// <summary>
        /// Events raised inside an engine damage call (reactions, death scripts) are held until
        /// that call's damage event is in place ahead of them.
        /// </summary>
        public void Enqueue(CombatRecordEvent recordEvent)
        {
            if (_isInHealingReaction)
            {
                recordEvent.Frame = CurrentFrame();
                recordEvent.TimestampMs = UnixTimeMilliseconds();
                _awaitingHeal.Add(recordEvent);
                return;
            }

            // A heal that never came: its reactions are written once that frame has passed.
            if (_awaitingHeal.Count > 0 && _awaitingHealFrame != CurrentFrame())
                ReleaseAwaitingHeal();

            recordEvent.Frame = CurrentFrame();
            if (_heldInsertIndex >= 0)
            {
                recordEvent.TimestampMs = _heldTimestampMs;
                _heldEvents.Insert(_heldInsertIndex++, recordEvent);
                return;
            }

            recordEvent.TimestampMs = UnixTimeMilliseconds();
            if (_damageApplications.Count > 0)
                _heldEvents.Add(recordEvent);
            else
                Write(recordEvent);
        }

        private void Write(in CombatRecordEvent recordEvent)
        {
            _eventsEnqueued++;
            _writer.Enqueue(recordEvent);
        }

        private void WriteHeldEvents()
        {
            _heldInsertIndex = -1;
            if (IsRecording)
            {
                foreach (var recordEvent in _heldEvents)
                    Write(recordEvent);
            }

            _heldEvents.Clear();
        }

        public bool IsInRecordedArea(uint creature)
        {
            return creature != OBJECT_INVALID && GetIsObjectValid(creature) && GetArea(creature) == _area;
        }

        /// <summary>
        /// Events are kept when either side is in the recorded area.
        /// </summary>
        public bool IsRecorded(uint subject, uint other = OBJECT_INVALID)
        {
            return IsInRecordedArea(subject) || IsInRecordedArea(other);
        }

        /// <summary>
        /// Returns the creature's stable id for this recording, registering it on first sight.
        /// Returns 0 for anything that is not a creature.
        /// </summary>
        public int GetParticipantId(uint creature)
        {
            if (creature == OBJECT_INVALID || creature == 0)
                return 0;

            if (_participantIds.TryGetValue(creature, out var participantId))
                return participantId;

            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return 0;

            participantId = _participantIds.Count + 1;
            _participantIds[creature] = participantId;
            _participants.Add(creature);

            // Character names only; never account names, CD keys or IP addresses. A declaration is
            // written at once, never held, so it precedes every event that refers to it.
            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            var declaration = CombatRecordEvent.Participant(
                participantId,
                GetName(creature),
                isPlayer,
                isPlayer ? null : GetResRef(creature));
            declaration.TimestampMs = UnixTimeMilliseconds();
            declaration.Frame = CurrentFrame();
            Write(declaration);
            return participantId;
        }

        /// <summary>
        /// Writes reaction events still waiting for their heal, in case it was never applied.
        /// </summary>
        public void ReleaseAwaitingHeal()
        {
            if (_awaitingHeal.Count == 0)
                return;

            var awaiting = _awaitingHeal.ToArray();
            _awaitingHeal.Clear();
            _healApplications.Clear();
            foreach (var recordEvent in awaiting)
                Enqueue(recordEvent);
        }

        public bool AnyInRecordedArea(IReadOnlyCollection<uint> creatures)
        {
            if (creatures == null)
                return false;

            foreach (var creature in creatures)
            {
                if (IsInRecordedArea(creature))
                    return true;
            }

            return false;
        }

        public CombatRecordEvent WithTargetLocation(CombatRecordEvent recordEvent, uint target, Location targetLocation)
        {
            if (GetIsObjectValid(target) || !GetIsObjectValid(GetAreaFromLocation(targetLocation)))
                return recordEvent;

            var position = GetPositionFromLocation(targetLocation);
            return recordEvent.WithLocation(position.X, position.Y, position.Z);
        }

        public void AddPendingDamage(uint source, uint target, int amount, bool isWeaponHit, CombatRecordAttribution attribution)
        {
            _pendingDamage.Add(new CombatRecordPendingDamage
            {
                Source = source,
                Target = target,
                Amount = amount,
                IsWeaponHit = isWeaponHit,
                Frame = CurrentFrame(),
                TimestampMs = UnixTimeMilliseconds(),
                Attribution = attribution
            });
        }

        /// <summary>
        /// Called by the native damage hook before the engine applies a damage effect. Lethal
        /// damage runs the death scripts inside the engine call, so the source is known to them
        /// and their death event waits for this damage event. Returns the application's index
        /// for <see cref="RecordDamageApplied"/> and <see cref="EndDamageApplication"/>.
        /// </summary>
        public int BeginDamageApplication(uint source, uint target, int amount, int hitPointsBefore)
        {
            // Only damage the recording keeps (it deals damage and involves the area) can name a
            // death's last damage source.
            if (amount > 0 && source != target && source != 0 && source != OBJECT_INVALID && IsRecorded(target, source))
                _lastDamageSources[target] = source;

            // A reaction hitting the same creature again ends what the outer hit left.
            var outer = FindDamageApplication(target);
            if (outer >= 0 && !_damageApplications[outer].HasHitPointsLeftByHit)
                KeepHitPointsLeftByHit(outer, hitPointsBefore);

            _damageApplications.Add(new CombatRecordDamageApplication
            {
                Target = target,
                HeldEventStart = _heldEvents.Count,
                StartedMs = UnixTimeMilliseconds()
            });
            return _damageApplications.Count - 1;
        }

        /// <summary>
        /// Releases a damage application, and writes the held events once the outermost one ends.
        /// </summary>
        public void EndDamageApplication(int applicationIndex)
        {
            if (applicationIndex >= 0 && applicationIndex < _damageApplications.Count)
                _damageApplications.RemoveRange(applicationIndex, _damageApplications.Count - applicationIndex);

            if (_damageApplications.Count == 0 && _heldEvents.Count > 0)
                WriteHeldEvents();
        }

        /// <summary>Opens the frame for a heal the native hook is about to apply to <paramref name="target"/>.</summary>
        public void BeginHealApplication(uint target)
        {
            _healApplications.Add((target, 0));
        }

        /// <summary>Closes the innermost frame for <paramref name="target"/> and returns its PvP reduction.</summary>
        public int EndHealApplication(uint target)
        {
            for (var index = _healApplications.Count - 1; index >= 0; index--)
            {
                if (_healApplications[index].Target != target)
                    continue;

                var pvpReduced = _healApplications[index].PvpReduced;
                _healApplications.RemoveAt(index);
                return pvpReduced;
            }

            return 0;
        }

        public void KeepHitPointsLeftByHit(int applicationIndex, int hitPoints)
        {
            var application = _damageApplications[applicationIndex];
            application.HasHitPointsLeftByHit = true;
            application.HitPointsLeftByHit = hitPoints;
            _damageApplications[applicationIndex] = application;
        }

        public int FindDamageApplication(uint target)
        {
            for (var index = _damageApplications.Count - 1; index >= 0; index--)
            {
                if (_damageApplications[index].Target == target)
                    return index;
            }

            return -1;
        }

        // Each wrapper's body is the call the static class made; a test answers it by overriding the method.

        public virtual ulong CurrentFrame() => ServerFrame.Current;

        public virtual long UnixTimeMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public virtual DateTime UtcNow() => DateTime.UtcNow;

        public virtual void WriteLog(LogGroup group, string details, bool printToConsole = false) =>
            Log.Write(group, details, printToConsole);
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The state and the event queue of a combat recording, which the recorder and the parts built on it share. The
    /// recorder and the native damage and heal hooks reach it; the domains below combat see only
    /// <c>ICombatRecordingState</c>, which the same instance implements.
    /// </summary>
    public interface ICombatRecordSession : ICombatRecordingState
    {
        /// <summary>
        /// True while a recording runs. Hooks check this before doing any work.
        /// </summary>
        new bool IsRecording { get; set; }

        /// <summary>
        /// The writer of the current or most recent recording. Its Completion task finishes once
        /// the summary has been written.
        /// </summary>
        CombatRecordWriter LastWriter { get; set; }

        /// <summary>The writer of the current recording.</summary>
        CombatRecordWriter Writer { get; set; }

        /// <summary>The recorded area.</summary>
        uint Area { get; set; }

        /// <summary>The events queued since the recording started.</summary>
        int EventsEnqueued { get; set; }

        /// <summary>The creatures registered in this recording, in the order they were first seen.</summary>
        IReadOnlyList<uint> Participants { get; }

        /// <summary>The participant id of each registered creature.</summary>
        IReadOnlyDictionary<uint, int> ParticipantIds { get; }

        /// <summary>Damage registered ahead of the engine applying it, waiting to be matched to a damage effect.</summary>
        List<CombatRecordPendingDamage> PendingDamage { get; }

        /// <summary>The last creature that damaged each recorded creature.</summary>
        Dictionary<uint, uint> LastDamageSources { get; }

        /// <summary>One frame per heal the native hook is applying, innermost last.</summary>
        List<(uint Target, int PvpReduced)> HealApplications { get; }

        /// <summary>One entry per damage effect the engine is applying, innermost last.</summary>
        List<CombatRecordDamageApplication> DamageApplications { get; }

        /// <summary>Where the next event is inserted among the held events, or -1 to append.</summary>
        int HeldInsertIndex { get; set; }

        /// <summary>The time stamp given to events inserted among the held events.</summary>
        long HeldTimestampMs { get; set; }

        /// <summary>How many reaction events wait for the heal they follow.</summary>
        int AwaitingHealCount { get; }

        /// <summary>The creature whose heal the awaiting events follow.</summary>
        uint AwaitingHealTarget { get; set; }

        /// <summary>The frame the awaiting events were queued in.</summary>
        ulong AwaitingHealFrame { get; set; }

        /// <summary>True while the reactions to a heal are queued behind it.</summary>
        bool IsInHealingReaction { get; set; }

        /// <summary>
        /// Clears what a recording keeps when it starts and when it ends: the participants, the damage waiting to be
        /// attributed, the damage sources, the damage and heal applications, the held and awaiting events and the
        /// healing reaction.
        /// </summary>
        void Reset();

        /// <summary>
        /// Ends what is in flight when a recording stops, then writes the events still waiting for their heal and the
        /// held events.
        /// </summary>
        void Flush();

        /// <summary>
        /// Events raised inside an engine damage call (reactions, death scripts) are held until
        /// that call's damage event is in place ahead of them.
        /// </summary>
        void Enqueue(CombatRecordEvent recordEvent);

        /// <summary>
        /// Writes reaction events still waiting for their heal, in case it was never applied.
        /// </summary>
        void ReleaseAwaitingHeal();

        bool IsInRecordedArea(uint creature);

        /// <summary>
        /// Events are kept when either side is in the recorded area.
        /// </summary>
        bool IsRecorded(uint subject, uint other = OBJECT_INVALID);

        bool AnyInRecordedArea(IReadOnlyCollection<uint> creatures);

        /// <summary>
        /// Returns the creature's stable id for this recording, registering it on first sight.
        /// Returns 0 for anything that is not a creature.
        /// </summary>
        int GetParticipantId(uint creature);

        CombatRecordEvent WithTargetLocation(CombatRecordEvent recordEvent, uint target, Location targetLocation);

        void AddPendingDamage(uint source, uint target, int amount, bool isWeaponHit, CombatRecordAttribution attribution);

        /// <summary>
        /// Called by the native damage hook before the engine applies a damage effect. Lethal
        /// damage runs the death scripts inside the engine call, so the source is known to them
        /// and their death event waits for this damage event. Returns the application's index
        /// for <c>RecordDamageApplied</c> and <see cref="EndDamageApplication"/>.
        /// </summary>
        int BeginDamageApplication(uint source, uint target, int amount, int hitPointsBefore);

        /// <summary>
        /// Releases a damage application, and writes the held events once the outermost one ends.
        /// </summary>
        void EndDamageApplication(int applicationIndex);

        /// <summary>Opens the frame for a heal the native hook is about to apply to <paramref name="target"/>.</summary>
        void BeginHealApplication(uint target);

        /// <summary>Closes the innermost frame for <paramref name="target"/> and returns its PvP reduction.</summary>
        int EndHealApplication(uint target);

        /// <summary>The index of the innermost damage application on <paramref name="target"/>, or -1.</summary>
        int FindDamageApplication(uint target);

        /// <summary>Keeps the hit points a hit left on a damage application.</summary>
        void KeepHitPointsLeftByHit(int applicationIndex, int hitPoints);

        ulong CurrentFrame();

        long UnixTimeMilliseconds();

        DateTime UtcNow();

        void WriteLog(LogGroup group, string details, bool printToConsole = false);
    }
}

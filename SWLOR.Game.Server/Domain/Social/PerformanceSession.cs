using System.Collections.Generic;
using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// One live performance: the cue strip, scoring and meter. Holds no engine calls so it can be tested directly.
    /// </summary>
    public sealed class PerformanceSession
    {
        public const int VisibleBeatCount = 6;

        private readonly System.Random _random;
        private readonly Queue<PerformanceBeat> _upcoming = new();
        private readonly Queue<bool> _recent = new();
        private int _recentHits;
        private int _pressedMask;
        private PerformanceInputResult? _currentResult;
        private PerformanceBeat _lastGenerated;

        public PerformanceSession(
            uint performer,
            PerformanceKind kind,
            PerformanceInstrumentFamily instrumentFamily,
            PerformanceTempo tempo,
            DateTime startedUtc,
            System.Random random)
        {
            Performer = performer;
            Kind = kind;
            InstrumentFamily = instrumentFamily;
            Tempo = tempo;
            StartedUtc = startedUtc;
            LastInputUtc = startedUtc;
            _random = random;

            Current = Generate(null);
            for (var index = 1; index < VisibleBeatCount; index++)
                _upcoming.Enqueue(Generate(_lastGenerated));
        }

        public uint Performer { get; }
        public string PlayerId { get; init; } = string.Empty;
        public uint Area { get; init; }
        public Vector3 StartPosition { get; init; }
        public FeatType ToggleFeat { get; init; }
        public int PerkLevel { get; init; }
        public InventorySlot InstrumentSlot { get; init; } = InventorySlot.Invalid;
        public uint Instrument { get; init; }
        public uint InstrumentBow { get; init; } = OBJECT_INVALID;
        public PerformanceKind Kind { get; }
        public PerformanceInstrumentFamily InstrumentFamily { get; }
        public PerformanceRoutineType Routine { get; set; }
        public PerformanceSetType Set { get; set; }
        public PerformanceTempo Tempo { get; set; }
        public DateTime StartedUtc { get; }
        public DateTime LastInputUtc { get; private set; }
        public DateTime? LastFlourishUtc { get; private set; }
        public IDisposable BeatSchedule { get; set; }

        /// <summary>The performer's latest named clip, so a finished accent never replaces a newer one.</summary>
        public string AnimationToken { get; set; }

        /// <summary>When the beat loop restarts the routine clip: before its playback lapses, or if an accent never handed back.</summary>
        public DateTime AnimationRenewUtc { get; set; }

        public int FlourishesPlayed { get; set; }

        /// <summary>The performer's cue window while one is attached; the beat loop pushes to it instead of floating text.</summary>
        public IPerformanceView View { get; set; }

        /// <summary>How many of the beat's steps have passed. The loop steps inside a beat so the window can show the time left.</summary>
        public int BeatStep { get; set; }

        /// <summary>
        /// How many equal steps the loop divides a beat into. Only a dancer's window has a beat bar, so only dances step;
        /// a musician's loop runs once per beat.
        /// </summary>
        public int StepsPerBeat => Kind == PerformanceKind.Dance ? DanceStepsPerBeat : 1;

        public const int DanceStepsPerBeat = 3;

        /// <summary>
        /// Roleplay mode: no cues to follow, scored at <see cref="PerformanceMath.RoleplayQuality"/>, and kept going by
        /// talking or emoting (<see cref="NoteActivity"/>) instead of cue presses.
        /// </summary>
        public bool IsRoleplay { get; private set; }

        public PerformanceBeat Current { get; private set; }
        public IReadOnlyCollection<PerformanceBeat> Upcoming => _upcoming;
        public int BeatNumber { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int FlourishMeter { get; private set; }
        public int RecordedCount => _recent.Count;
        public int RecentHits => _recentHits;
        public double Accuracy => _recent.Count == 0 ? 1.0 : (double)_recentHits / _recent.Count;
        public PerformanceQuality Quality => IsRoleplay
            ? PerformanceMath.RoleplayQuality
            : PerformanceMath.QualityFromRecord(_recentHits, _recent.Count);

        /// <summary>Copies the upcoming beats, next first, into a caller-owned array so a window can read them without allocating.</summary>
        public void CopyUpcomingTo(PerformanceBeat[] destination)
        {
            _upcoming.CopyTo(destination, 0);
        }

        public bool IsIdle(DateTime nowUtc)
        {
            var timeout = IsRoleplay ? PerformanceMath.RoleplayIdleTimeoutSeconds : PerformanceMath.IdleTimeoutSeconds;
            return (nowUtc - LastInputUtc).TotalSeconds >= timeout;
        }

        /// <summary>
        /// Switches between active play and Roleplay mode. The idle timer restarts so the switch never ends the show, and
        /// the beat in progress starts clean. The quality record of active play is kept for when the performer returns to it.
        /// </summary>
        public void SetRoleplay(bool roleplay, DateTime nowUtc)
        {
            IsRoleplay = roleplay;
            LastInputUtc = nowUtc;
            _pressedMask = 0;
            _currentResult = null;
        }

        /// <summary>The performer talked or emoted. Only Roleplay mode counts it; active play is kept going by cue presses.</summary>
        public void NoteActivity(DateTime nowUtc)
        {
            if (IsRoleplay)
                LastInputUtc = nowUtc;
        }

        /// <summary>The first press of a beat decides it; later presses on the same beat are ignored. Roleplay mode has no cues.</summary>
        public PerformanceInputResult RegisterCue(PerformanceCue cue, DateTime nowUtc)
        {
            if (IsRoleplay)
                return PerformanceInputResult.Ignored;

            LastInputUtc = nowUtc;

            if (Current.IsRest || _currentResult.HasValue)
                return PerformanceInputResult.Ignored;

            if (!Current.Requires(cue))
            {
                _currentResult = PerformanceInputResult.Miss;
                return PerformanceInputResult.Miss;
            }

            var lane = 1 << (int)cue;
            if ((_pressedMask & lane) != 0)
                return PerformanceInputResult.Ignored;

            _pressedMask |= lane;
            if (_pressedMask != Current.LaneMask)
                return PerformanceInputResult.Partial;

            _currentResult = PerformanceInputResult.Hit;
            return PerformanceInputResult.Hit;
        }

        /// <summary>Scores the current beat, then moves the strip forward. Held rests score nothing.</summary>
        public PerformanceBeatOutcome AdvanceBeat()
        {
            PerformanceBeatOutcome outcome;
            if (Current.IsRest)
                outcome = PerformanceBeatOutcome.Rest;
            else if (_currentResult == PerformanceInputResult.Hit)
                outcome = PerformanceBeatOutcome.Hit;
            else
                outcome = PerformanceBeatOutcome.Miss;

            Record(outcome);

            Current = _upcoming.Dequeue();
            _upcoming.Enqueue(Generate(_lastGenerated));
            _pressedMask = 0;
            _currentResult = null;
            BeatNumber++;
            return outcome;
        }

        public bool TryConsumeFlourish(DateTime nowUtc)
        {
            if (IsRoleplay || FlourishMeter < PerformanceMath.FlourishMeterMax)
                return false;

            FlourishMeter = 0;
            LastFlourishUtc = nowUtc;
            LastInputUtc = nowUtc;
            return true;
        }

        private void Record(PerformanceBeatOutcome outcome)
        {
            if (outcome == PerformanceBeatOutcome.Rest)
                return;

            var hit = outcome == PerformanceBeatOutcome.Hit;
            _recent.Enqueue(hit);
            if (hit)
                _recentHits++;

            if (_recent.Count > PerformanceMath.QualityWindowSize && _recent.Dequeue())
                _recentHits--;

            if (hit)
            {
                Streak++;
                BestStreak = Math.Max(BestStreak, Streak);
                FlourishMeter = Math.Min(PerformanceMath.FlourishMeterMax, FlourishMeter + 1);
            }
            else
            {
                Streak = 0;
            }
        }

        private PerformanceBeat Generate(PerformanceBeat? previous)
        {
            _lastGenerated = PerformanceCueGenerator.Next(_random, Kind, Tempo, InstrumentFamily, previous);
            return _lastGenerated;
        }
    }
}

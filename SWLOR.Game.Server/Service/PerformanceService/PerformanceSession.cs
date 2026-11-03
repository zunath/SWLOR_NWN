using System.Collections.Generic;
using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.PerformanceService
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

        public PerformanceBeat Current { get; private set; }
        public IReadOnlyCollection<PerformanceBeat> Upcoming => _upcoming;
        public int BeatNumber { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int FlourishMeter { get; private set; }
        public int RecordedCount => _recent.Count;
        public int RecentHits => _recentHits;
        public double Accuracy => _recent.Count == 0 ? 1.0 : (double)_recentHits / _recent.Count;
        public PerformanceQuality Quality => PerformanceMath.QualityFromRecord(_recentHits, _recent.Count);

        /// <summary>Copies the upcoming beats, next first, into a caller-owned array so a window can read them without allocating.</summary>
        public void CopyUpcomingTo(PerformanceBeat[] destination)
        {
            _upcoming.CopyTo(destination, 0);
        }

        public bool IsIdle(DateTime nowUtc)
        {
            return (nowUtc - LastInputUtc).TotalSeconds >= PerformanceMath.IdleTimeoutSeconds;
        }

        /// <summary>The first press of a beat decides it; later presses on the same beat are ignored.</summary>
        public PerformanceInputResult RegisterCue(PerformanceCue cue, DateTime nowUtc)
        {
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
            if (FlourishMeter < PerformanceMath.FlourishMeterMax)
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

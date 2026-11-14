using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Social
{
    public static class PerformanceCueGenerator
    {
        public const double StringsRepeatChance = 0.25;
        public const double HornsRestChance = 0.40;
        public const double ChordChance = 0.20;

        public static IReadOnlyList<PerformanceCue> GetLanes(PerformanceKind kind)
        {
            return kind == PerformanceKind.Dance
                ? new[] { PerformanceCue.Left, PerformanceCue.Up, PerformanceCue.Down, PerformanceCue.Right }
                : new[] { PerformanceCue.Low, PerformanceCue.Mid, PerformanceCue.High };
        }

        /// <summary>
        /// Builds the beat that follows <paramref name="previous"/>, or the opening beat when it is null.
        /// Strings play a cue every beat with occasional same-lane repeats; horns hold some beats as rests
        /// (never two in a row, never the opening beat). Vocals use breath rests and neighboring pitches;
        /// chords appear only on instruments at Upbeat.
        /// </summary>
        public static PerformanceBeat Next(
            System.Random random,
            PerformanceKind kind,
            PerformanceTempo tempo,
            PerformanceInstrumentFamily family,
            PerformanceBeat? previous)
        {
            var isMusic = kind == PerformanceKind.Music;
            var lastNote = previous is { IsRest: false } note ? note.Primary : (PerformanceCue?)null;

            if (isMusic &&
                family is PerformanceInstrumentFamily.Horns or PerformanceInstrumentFamily.Vocals or PerformanceInstrumentFamily.Pipes &&
                lastNote.HasValue &&
                random.NextDouble() < HornsRestChance)
            {
                return PerformanceBeat.Rest;
            }

            var lanes = GetLanes(kind);
            var primary = PickLane(random, isMusic, family, lanes, lastNote);

            if (isMusic && family is not (PerformanceInstrumentFamily.Vocals or PerformanceInstrumentFamily.Pipes) &&
                tempo == PerformanceTempo.Upbeat && random.NextDouble() < ChordChance)
                return PerformanceBeat.Chord(primary, PickOtherLane(random, lanes, primary));

            return PerformanceBeat.Single(primary);
        }

        private static PerformanceCue PickLane(
            System.Random random,
            bool isMusic,
            PerformanceInstrumentFamily family,
            IReadOnlyList<PerformanceCue> lanes,
            PerformanceCue? lastNote)
        {
            if (!isMusic || !lastNote.HasValue)
                return lanes[random.Next(lanes.Count)];

            if (family is PerformanceInstrumentFamily.Strings or PerformanceInstrumentFamily.BowedStrings or PerformanceInstrumentFamily.Percussion)
            {
                return random.NextDouble() < StringsRepeatChance
                    ? lastNote.Value
                    : PickOtherLane(random, lanes, lastNote.Value);
            }

            if (family == PerformanceInstrumentFamily.Vocals)
            {
                if (random.NextDouble() < StringsRepeatChance)
                    return lastNote.Value;

                return lastNote == PerformanceCue.Mid
                    ? (random.Next(2) == 0 ? PerformanceCue.Low : PerformanceCue.High)
                    : PerformanceCue.Mid;
            }

            return family is PerformanceInstrumentFamily.Horns or PerformanceInstrumentFamily.Pipes
                ? PickOtherLane(random, lanes, lastNote.Value)
                : lanes[random.Next(lanes.Count)];
        }

        private static PerformanceCue PickOtherLane(System.Random random, IReadOnlyList<PerformanceCue> lanes, PerformanceCue excluded)
        {
            var index = random.Next(lanes.Count - 1);
            var chosen = 0;
            for (var lane = 0; lane < lanes.Count; lane++)
            {
                if (lanes[lane] == excluded)
                    continue;

                if (chosen == index)
                    return lanes[lane];

                chosen++;
            }

            return lanes[0];
        }
    }
}

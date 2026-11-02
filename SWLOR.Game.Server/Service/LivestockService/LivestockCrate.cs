using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.LivestockService
{
    /// <summary>
    /// An animal packed into a crate: the species and the timers it carries, each as the time that was left on it. A crated
    /// animal is paused, with no hunger, growth or products, so placing it again restores every timer where it stopped and
    /// crating cannot clear a penalty. The meal timers need no entry: the feed interval and the 24-hour window for products
    /// both count from the last meal, which the hunger timer pins. <see cref="LivestockRules.Pack"/> and
    /// <see cref="LivestockRules.Unpack"/> convert; the crate item keeps these values as local variables.
    /// </summary>
    public class LivestockCrate
    {
        public const string SpeciesLocal = "LIVESTOCK_SPECIES";
        public const string FeedingsLocal = "LIVESTOCK_FEEDINGS";
        public const string MaturitySecondsLocal = "LIVESTOCK_MATURITY_S";
        public const string HungerSecondsLocal = "LIVESTOCK_HUNGER_S";
        public const string CollectSecondsLocal = "LIVESTOCK_COLLECT_S";
        public const string PenaltySecondsLocal = "LIVESTOCK_PENALTY_S";

        public LivestockType Species { get; set; }

        public int Feedings { get; set; }

        /// <summary>Seconds left until the animal is 72 hours old. 0 once it is.</summary>
        public int MaturitySeconds { get; set; }

        /// <summary>Seconds left until the animal wanders off, counting from its last meal (or from its placing, if it never ate).</summary>
        public int HungerSeconds { get; set; }

        /// <summary>Seconds left until the animal can be collected from again. 0 when it can.</summary>
        public int CollectSeconds { get; set; }

        /// <summary>Seconds left of the predators' penalty. 0 when it has none.</summary>
        public int PenaltySeconds { get; set; }

        /// <summary>The crate's values as the local variables the item holds.</summary>
        public Dictionary<string, int> ToLocals() => new()
        {
            [SpeciesLocal] = (int)Species,
            [FeedingsLocal] = Feedings,
            [MaturitySecondsLocal] = MaturitySeconds,
            [HungerSecondsLocal] = HungerSeconds,
            [CollectSecondsLocal] = CollectSeconds,
            [PenaltySecondsLocal] = PenaltySeconds,
        };

        /// <summary>Reads a crate from the item's local variables. False when the item holds no animal.</summary>
        public static bool TryFromLocals(System.Func<string, int> read, out LivestockCrate crate)
        {
            var species = (LivestockType)read(SpeciesLocal);
            crate = new LivestockCrate
            {
                Species = species,
                Feedings = System.Math.Max(0, read(FeedingsLocal)),
                MaturitySeconds = System.Math.Max(0, read(MaturitySecondsLocal)),
                HungerSeconds = System.Math.Max(0, read(HungerSecondsLocal)),
                CollectSeconds = System.Math.Max(0, read(CollectSecondsLocal)),
                PenaltySeconds = System.Math.Max(0, read(PenaltySecondsLocal)),
            };

            return species != LivestockType.Invalid && System.Enum.IsDefined(species);
        }
    }
}

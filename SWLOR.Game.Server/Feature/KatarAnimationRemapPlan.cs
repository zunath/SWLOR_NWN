using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// The katar and Power Glove remap of one-handed combat animations to unarmed ones, plus the
    /// Power Glove carry layers. The engine saves a creature's animation mappings with the character,
    /// so the mappings the creature already has, not session state, decide which writes bring it in
    /// line with its equipment.
    /// </summary>
    public static class KatarAnimationRemapPlan
    {
        public static IReadOnlyList<(string Source, string Replacement)> Pairs { get; } = new[]
        {
            // Core 1h attack chain -> unarmed equivalents.
            ("1hreadyr", "nwreadyr"),
            ("1hreadyl", "nwreadyl"),
            ("1hslashl", "nwslashl"),
            ("1hslashr", "nwslashr"),
            ("1hstab", "nwstab"),
            ("1hcloseh", "nwcloseh"),
            ("1hclosel", "nwclosel"),
            ("1hreach", "nwreach"),
            ("1hslasho", "nwslasho"),
            // 1h parries are mapped to unarmed-style dodges.
            ("1hparryl", "dodges"),
            ("1hparryr", "dodgelr"),
        };

        /// <summary>
        /// The Power Glove is a standard one-handed weapon, so the engine layers the sword-carry pose
        /// over the right arm while walking and running. Mapping those layers to a clip that does not
        /// exist drops them, and the arm swings with the walk or run as a bare hand does.
        /// </summary>
        public static IReadOnlyList<(string Source, string Replacement)> PowerGloveCarryPairs { get; } = new[]
        {
            ("walk_swordr", NamedAnimationPlayback.NoHoldName),
            ("run_swordr", NamedAnimationPlayback.NoHoldName),
        };

        /// <summary>
        /// The mappings to write, where an empty replacement restores the native animation.
        /// Applying overwrites every key except one in <paramref name="activeSwingKeys"/> that another
        /// mapping holds, so a waiting queued ability keeps its swing clip while a clip saved without
        /// its playback is replaced. Restoring removes every key still carrying this plan's
        /// animation, including ones a saved character brought back after the equipment that
        /// needed them was gone. <paramref name="carryLayersInUse"/> leaves the carry layers to a
        /// movement that holds them, such as a weapon repositioning dash.
        /// </summary>
        public static IReadOnlyList<(string Source, string Replacement)> GetChanges(
            bool useRemap, bool usePowerGloveCarry, IReadOnlyDictionary<string, string> current,
            IReadOnlyCollection<string> activeSwingKeys, bool carryLayersInUse = false)
        {
            var changes = GetChanges(Pairs, useRemap, current, activeSwingKeys).ToList();
            if (!carryLayersInUse)
                changes.AddRange(GetChanges(PowerGloveCarryPairs, usePowerGloveCarry, current, Array.Empty<string>()));
            return changes;
        }

        private static IEnumerable<(string Source, string Replacement)> GetChanges(
            IEnumerable<(string Source, string Replacement)> pairs, bool apply,
            IReadOnlyDictionary<string, string> current, IReadOnlyCollection<string> activeSwingKeys) =>
            apply
                ? pairs.Where(pair => !current.TryGetValue(pair.Source, out var replacement) ||
                        replacement != pair.Replacement && !activeSwingKeys.Contains(pair.Source))
                : pairs.Where(pair => current.TryGetValue(pair.Source, out var replacement) && replacement == pair.Replacement)
                    .Select(pair => (pair.Source, string.Empty));
    }
}

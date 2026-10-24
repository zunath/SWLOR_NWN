using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// The katar and Power Glove remap of one-handed combat animations to unarmed ones. The engine
    /// saves a creature's animation mappings with the character, so the mappings the creature
    /// already has, not session state, decide which writes bring it in line with its equipment.
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
        /// The mappings to write, where an empty replacement restores the native animation.
        /// Applying overwrites every key except one in <paramref name="activeSwingKeys"/> that another
        /// mapping holds, so a waiting queued ability keeps its swing clip while a clip saved without
        /// its playback is replaced. Restoring removes every key still carrying this remap's
        /// animation, including ones a saved character brought back after the equipment that
        /// needed them was gone.
        /// </summary>
        public static IReadOnlyList<(string Source, string Replacement)> GetChanges(
            bool useRemap, IReadOnlyDictionary<string, string> current, IReadOnlyCollection<string> activeSwingKeys) =>
            useRemap
                ? Pairs.Where(pair => !current.TryGetValue(pair.Source, out var replacement) ||
                        replacement != pair.Replacement && !activeSwingKeys.Contains(pair.Source)).ToList()
                : Pairs.Where(pair => current.TryGetValue(pair.Source, out var replacement) && replacement == pair.Replacement)
                    .Select(pair => (pair.Source, string.Empty)).ToList();
    }
}

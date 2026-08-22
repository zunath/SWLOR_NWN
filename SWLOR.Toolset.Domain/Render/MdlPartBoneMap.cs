// SPDX-License-Identifier: MIT

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Maps Aurora segmented-creature part categories to the skeleton nodes that anchor them.
    /// </summary>
    /// <remarks>
    /// The short category names are the values emitted by <see cref="BlueprintModelResolver"/>.
    /// Candidate aliases cover skeletons that use the descriptive shoulder/forearm spelling while
    /// retaining the compact names used by the standard player skeletons.
    /// </remarks>
    public static class MdlPartBoneMap
    {
        private static readonly IReadOnlyDictionary<string, string[]> Candidates =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["head"] = ["head_g", "head"],
                ["neck"] = ["neck_g", "neck"],
                ["chest"] = ["torso_g", "chest_g", "torso"],
                ["belt"] = ["belt_g", "pelvis_g", "pelvis"],
                ["pelvis"] = ["pelvis_g", "pelvis"],
                ["shol"] = ["lshoul_g", "lshoulder_g", "lshoul"],
                ["shor"] = ["rshoul_g", "rshoulder_g", "rshoul"],
                ["bicepl"] = ["lbicep_g", "lbicep"],
                ["bicepr"] = ["rbicep_g", "rbicep"],
                ["forel"] = ["lforearm_g", "lfore_g", "lforearm"],
                ["forer"] = ["rforearm_g", "rfore_g", "rforearm"],
                ["handl"] = ["lhand_g", "lhand"],
                ["handr"] = ["rhand_g", "rhand"],
                ["legl"] = ["lthigh_g", "lthigh"],
                ["legr"] = ["rthigh_g", "rthigh"],
                ["shinl"] = ["lshin_g", "lshin"],
                ["shinr"] = ["rshin_g", "rshin"],
                ["footl"] = ["lfoot_g", "lfoot"],
                ["footr"] = ["rfoot_g", "rfoot"],
                ["robe"] = ["torso_g", "pelvis_g", "torso", "pelvis"],

                // Equipped item models use their own authored origin and hang directly from the
                // creature skeleton's visible attachment bones. Distinct part names keep robe
                // coverage from mistaking a held weapon for the hand body part beneath it.
                ["helmet"] = ["head_g", "head"],
                ["weaponl"] = ["lhand_g", "lhand"],
                ["weaponr"] = ["rhand_g", "rhand"],

                // A cloak hangs from the skeleton's own Cloak_g, which sits under torso_g and carries
                // the CL*/CM*/CR* chain the cloak's skinmesh is weighted to. Verified against pmh0.
                ["cloak"] = ["Cloak_g", "cloak_g", "torso_g"],
            };

        /// <summary>All supported part categories and their preferred skeleton bone.</summary>
        public static IReadOnlyDictionary<string, string> Bones { get; } =
            Candidates.ToDictionary(
                pair => pair.Key,
                pair => pair.Value[0],
                StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns the preferred skeleton bone for a part category, or null when unknown.</summary>
        public static string? GetBoneName(string? partType) =>
            partType != null && Candidates.TryGetValue(partType.Trim(), out var names)
                ? names[0]
                : null;

        /// <summary>Attempts to return the preferred skeleton bone for a part category.</summary>
        public static bool TryGetBoneName(string? partType, out string boneName)
        {
            boneName = GetBoneName(partType) ?? string.Empty;
            return boneName.Length > 0;
        }

        /// <summary>
        /// Returns the ordered bone-name candidates for a category. The first entry is the canonical
        /// standard-skeleton name.
        /// </summary>
        public static IReadOnlyList<string> GetBoneCandidates(string? partType) =>
            partType != null && Candidates.TryGetValue(partType.Trim(), out var names)
                ? names
                : Array.Empty<string>();
    }
}

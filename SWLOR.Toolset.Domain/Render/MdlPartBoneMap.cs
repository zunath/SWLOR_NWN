// SPDX-License-Identifier: GPL-3.0-or-later
//
// Vendored verbatim from Radoub.UI.Services.MdlPartBoneMap
// (https://github.com/LordOfMyatar/Radoub), which is GPL-3.0, so this file is GPL-3.0 even though
// the rest of the SWLOR Toolset's own source is MIT. Copied rather than reimplemented so the
// composed geometry is bit-identical to what shipped; only the namespace differs. Copying it out
// drops the Radoub.UI project reference (and its Avalonia version pin) without pretending the code
// became ours. See SWLOR.Toolset/LICENSE-NOTICE.md.
namespace SWLOR.Toolset.Domain.Render;

/// <summary>
/// Maps NWN body-part type names (e.g., "head", "chest", "shol") to the corresponding
/// skeleton bone names (with the "_g" suffix used by Aurora Engine MDL skeletons).
///
/// Used by <see cref="MdlPartComposer"/> to attach part meshes onto the correct bone
/// in a creature or armor mannequin skeleton, so that animation pose lookup walks the
/// parent chain through the supermodel correctly (#2124).
/// </summary>
public static class MdlPartBoneMap
{
    /// <summary>
    /// Default mapping from part type to skeleton bone name.
    /// Unknown part types fall back to <c>{partType}_g</c>.
    /// </summary>
    public static string GetBoneNameForPart(string partType) => partType switch
    {
        "head" => "head_g",
        "neck" => "neck_g",
        "chest" => "torso_g",
        "robe" => "torso_g",
        "pelvis" => "pelvis_g",
        "belt" => "belt_g",
        "shol" => "lshoulder_g",
        "shor" => "rshoulder_g",
        "bicepl" => "lbicep_g",
        "bicepr" => "rbicep_g",
        "forel" => "lforearm_g",
        "forer" => "rforearm_g",
        "handl" => "lhand_g",
        "handr" => "rhand_g",
        "legl" => "lthigh_g",
        "legr" => "rthigh_g",
        "shinl" => "lshin_g",
        "shinr" => "rshin_g",
        "footl" => "lfoot_g",
        "footr" => "rfoot_g",
        _ => partType + "_g"
    };
}

/// <summary>
/// String-formatting helpers for NWN body-part MDL ResRefs.
/// </summary>
public static class MdlPartNaming
{
    /// <summary>
    /// Build a body-part MDL ResRef from a creature/mannequin prefix, part type, and part number.
    /// Format: <c>{prefix}_{partType}{partNumber:D3}</c>, e.g., <c>pmh0_chest005</c>.
    /// </summary>
    public static string BuildBodyPartName(string prefix, string partType, byte partNumber)
        => $"{prefix}_{partType}{partNumber:D3}";
}

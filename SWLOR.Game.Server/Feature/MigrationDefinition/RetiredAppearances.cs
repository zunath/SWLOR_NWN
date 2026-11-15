using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// The armor parts, heads, helmets and cloaks removed from the haks to shrink them. No player, NPC, module
/// blueprint or DM template used any of them in the 2026-11-13 production data.
/// </summary>
public static class RetiredAppearances
{
    /// <summary>Retired armor part styles by part 2DA (parts_chest.2da is Torso, parts_legs.2da is Thigh).</summary>
    public static IReadOnlyDictionary<RetiredArmorPart, IReadOnlySet<int>> ArmorParts { get; } =
        new Dictionary<RetiredArmorPart, IReadOnlySet<int>>
        {
            [RetiredArmorPart.Torso] = new HashSet<int>
            {
                80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 95, 97, 164, 165, 169, 174, 192, 194, 195, 300,
            },
            [RetiredArmorPart.Neck] = new HashSet<int>
            {
                60, 120, 181, 182, 211, 212, 218, 219, 223, 232, 299, 300,
            },
            [RetiredArmorPart.Belt] = new HashSet<int>
            {
                18, 120, 121, 167, 168,
            },
            [RetiredArmorPart.Pelvis] = new HashSet<int>
            {
                54, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 99, 100, 107, 112, 113, 114,
                115, 116, 118, 120, 121, 124, 150, 159, 162, 175, 176,
            },
            [RetiredArmorPart.Robe] = new HashSet<int>
            {
                44, 52, 95, 97, 99, 100, 109, 125, 129, 133, 136, 139, 142, 160, 161, 163, 167, 168, 172, 175,
                176, 177, 178, 179, 180, 181, 188, 189, 211, 212, 213, 228, 229, 255, 258,
            },
            [RetiredArmorPart.Shoulder] = new HashSet<int>
            {
                120, 121, 180, 255,
            },
            [RetiredArmorPart.Bicep] = new HashSet<int>
            {
                18, 19, 74, 76, 95, 97, 98, 99, 100, 120, 175, 180, 181, 184, 185, 187, 245, 255, 270,
            },
            [RetiredArmorPart.Forearm] = new HashSet<int>
            {
                100, 120, 140, 175, 181, 182, 183, 184, 187, 255,
            },
            [RetiredArmorPart.Hand] = new HashSet<int>
            {
                11, 13, 46, 112, 175, 181, 218, 255,
            },
            [RetiredArmorPart.Thigh] = new HashSet<int>
            {
                55, 56, 57, 76, 77, 78, 79, 99, 100, 112, 113, 114, 115, 120, 175, 176, 180, 181, 197, 209,
            },
            [RetiredArmorPart.Shin] = new HashSet<int>
            {
                52, 53, 76, 99, 100, 101, 112, 120, 159, 175, 181, 182, 200, 209,
            },
            [RetiredArmorPart.Foot] = new HashSet<int>
            {
                13, 16, 54, 55, 56, 76, 77, 78, 79, 100, 112, 120, 150, 153, 175, 180, 181, 209,
            },
        };

    /// <summary>Retired head numbers by model prefix: p, then m or f for the gender, then the model letter.</summary>
    public static IReadOnlyDictionary<string, IReadOnlySet<int>> Heads { get; } =
        new Dictionary<string, IReadOnlySet<int>>
        {
            ["pfa"] = new HashSet<int>
            {
                12, 13, 14, 15, 17, 20, 21, 22, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173,
                174, 175, 176, 192, 193,
            },
            ["pfd"] = new HashSet<int>
            {
                13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 104, 105, 109,
            },
            ["pfe"] = new HashSet<int>
            {
                2, 3, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 42, 43,
                44, 46, 47, 48, 49, 54, 100, 101, 102, 103, 105, 106, 107, 111, 112, 121, 122, 127, 128, 129,
                133, 134, 135, 136, 137, 139, 140, 143, 144, 145, 146, 147, 148, 149, 150, 152, 153, 154, 155,
                156, 158, 159, 161, 163, 164, 165, 167, 168, 171, 179, 180, 183, 184, 186, 190, 194, 196, 197,
                198, 199,
            },
            ["pfg"] = new HashSet<int>
            {
                10, 15, 18, 19, 20,
            },
            ["pfh"] = new HashSet<int>
            {
                26, 59, 60, 62, 63, 64, 65, 66, 67, 68, 74, 75, 76, 77, 78, 83, 84, 85, 86, 87, 88, 89, 90, 91,
                92, 93, 94, 95, 96, 97, 98, 99, 128, 138, 146, 147, 148, 149, 153, 154, 155, 156, 158, 159, 160,
                161, 163, 166, 169, 170, 176, 179, 181, 182, 197, 198, 246, 257, 258, 278, 294, 302, 305,
            },
            ["pfo"] = new HashSet<int>
            {
                2, 15, 21, 22, 23, 192, 193, 194,
            },
            ["pfz"] = new HashSet<int>
            {
                13, 23, 24, 25, 26, 27, 28, 29, 30, 31, 37, 39, 40, 42,
            },
            ["pma"] = new HashSet<int>
            {
                11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 161, 162, 163, 164, 165, 166,
                167, 168, 169, 192, 193,
            },
            ["pmd"] = new HashSet<int>
            {
                14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 102,
            },
            ["pme"] = new HashSet<int>
            {
                19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30, 36, 38, 42, 44, 46, 47, 48, 103, 104, 105, 106, 108,
                109, 112, 113, 114, 115, 116, 158, 194, 195,
            },
            ["pmg"] = new HashSet<int>
            {
                14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37,
                38, 39,
            },
            ["pmh"] = new HashSet<int>
            {
                31, 55, 79, 80, 81, 82, 85, 86, 87, 89, 90, 91, 93, 94, 95, 122, 133, 139, 144, 149, 151, 154,
                157, 159, 169, 172, 173, 174, 175, 181, 185, 210, 211, 238, 264, 268, 285, 291, 294, 297, 301,
            },
            ["pmo"] = new HashSet<int>
            {
                14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 30, 31, 32, 34, 35, 36, 37, 38, 39, 40, 41, 42,
                43, 44, 45, 46, 47, 48, 49, 105, 106, 109, 114, 115, 116, 117, 118, 119, 121, 126, 129, 142,
                143, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 167, 191, 192, 193, 194,
                195, 196, 197, 198, 199,
            },
            ["pmz"] = new HashSet<int>
            {
                23, 24, 29, 30, 41,
            },
        };

    public static IReadOnlySet<int> Helmets { get; } = new HashSet<int>
    {
        144, 146, 149, 150, 151, 211, 256, 257, 258, 281, 282, 283, 286, 287, 288, 297,
    };

    /// <summary>Retired cloakmodel.2da rows.</summary>
    public static IReadOnlySet<int> Cloaks { get; } = new HashSet<int>
    {
        33, 41, 61, 104, 106, 107, 108, 110, 111, 124, 127, 129, 134, 138, 141, 142, 148, 149,
    };
}

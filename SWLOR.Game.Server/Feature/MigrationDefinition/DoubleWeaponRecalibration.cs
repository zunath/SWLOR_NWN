using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Rebases existing double weapon ratings while retaining damage added by upgrades and enhancements.</summary>
internal static class DoubleWeaponRecalibration
{
    private static readonly Dictionary<string, (int Previous, int Current)> DamageByResref = new(StringComparer.OrdinalIgnoreCase)
    {
        ["asc_trnsabstaff"] = (23, 19),
        ["asc_twinblade"] = (23, 19),
        ["asc_twinelec"] = (23, 19),
        ["b_twinblade"] = (7, 5),
        ["bloodprice_edge"] = (41, 41),
        ["byysk_twinblade"] = (17, 15),
        ["cap_sabstaff"] = (14, 11),
        ["cap_twinblade"] = (14, 11),
        ["chi_twinblade"] = (29, 24),
        ["chi_twinelec"] = (29, 24),
        ["circle_twin"] = (27, 23),
        ["crosswind_edge"] = (23, 23),
        ["del_twinblade"] = (16, 13),
        ["duel_splitter"] = (27, 23),
        ["fld_trnsabstaff"] = (10, 7),
        ["fld_twinblade"] = (10, 7),
        ["fld_twinelec"] = (10, 7),
        ["h_twinblade_1"] = (9, 6),
        ["h_twinblade_2"] = (14, 10),
        ["h_twinblade_3"] = (18, 14),
        ["h_twinblade_4"] = (23, 18),
        ["h_twinblade_5"] = (26, 22),
        ["h_twinelec_1"] = (9, 6),
        ["h_twinelec_2"] = (14, 10),
        ["h_twinelec_3"] = (18, 14),
        ["h_twinelec_4"] = (23, 18),
        ["h_twinelec_5"] = (26, 22),
        ["infconduit_l1"] = (23, 23),
        ["infconduit_l2"] = (23, 23),
        ["infconduit_w1"] = (41, 41),
        ["kwi_twinblade"] = (27, 23),
        ["kwi_twinelec"] = (27, 23),
        ["lastcall_edge"] = (23, 23),
        ["mando_sabstaff"] = (14, 11),
        ["mando_twinblade"] = (14, 11),
        ["oph_twinblade"] = (25, 21),
        ["poach_twinblade"] = (17, 14),
        ["prm_trnsabstaff"] = (18, 15),
        ["prm_twinblade"] = (18, 15),
        ["prm_twinelec"] = (18, 15),
        ["proto_twinblade"] = (20, 17),
        ["raider_twinblade"] = (22, 18),
        ["sabcycl_l1"] = (23, 23),
        ["sabcycl_l2"] = (23, 23),
        ["sabcycl_w1"] = (41, 41),
        ["sc_twinblade"] = (25, 21),
        ["scarlet_blades"] = (23, 23),
        ["sith_twinblade"] = (14, 10),
        ["slw_crosswind"] = (16, 10),
        ["squall_blades"] = (23, 23),
        ["ss_custom"] = (7, 5),
        ["stormcall_blades"] = (41, 41),
        ["stw_zerostate"] = (41, 22),
        ["t_twin_elec"] = (6, 5),
        ["t_twinblade"] = (6, 4),
        ["tit_twinblade"] = (12, 9),
        ["trn_saberstaff_1"] = (7, 5),
        ["trn_saberstaff_2"] = (12, 9),
        ["trn_saberstaff_3"] = (16, 13),
        ["trn_saberstaff_4"] = (20, 17),
        ["trn_saberstaff_5"] = (25, 21),
        ["twin_elec_1"] = (7, 5),
        ["twin_elec_2"] = (12, 9),
        ["twin_elec_3"] = (16, 13),
        ["twin_elec_4"] = (20, 17),
        ["twin_elec_5"] = (25, 21),
        ["vet_trnsabstaff"] = (14, 11),
        ["vet_twinblade"] = (14, 11),
        ["vet_twinelec"] = (14, 11),
    };

    private static readonly int[] PreviousTierDamage = { 7, 12, 16, 20, 25, 29 };
    private static readonly int[] CurrentTierDamage = { 5, 9, 13, 17, 21, 24 };
    // Upgrade kits changed these same item instances without changing their resrefs.
    // The former kit curve differs from the authored training blueprint curve.
    private static readonly int[] PreviousUpgradeDamage = { 7, 11, 15, 19, 25, 29 };
    private static readonly Dictionary<string, int> UpgradableSaberstaffBaseTiers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ss_custom"] = 1,
        ["trn_saberstaff_1"] = 1,
        ["trn_saberstaff_2"] = 2,
        ["trn_saberstaff_3"] = 3,
        ["trn_saberstaff_4"] = 4,
        ["trn_saberstaff_5"] = 5,
    };

    public static int CalculateDamage(string resref, int tier, int currentDamage)
    {
        if (!DamageByResref.TryGetValue(resref ?? string.Empty, out var baseline))
        {
            if (tier <= 0)
            {
                tier = 1;
                for (var index = 1; index < PreviousTierDamage.Length; index++)
                    if (Math.Abs((long)currentDamage - PreviousTierDamage[index]) <
                        Math.Abs((long)currentDamage - PreviousTierDamage[tier - 1]))
                        tier = index + 1;
            }
            var tierIndex = Math.Clamp(tier - 1, 0, PreviousTierDamage.Length - 1);
            baseline = (PreviousTierDamage[tierIndex], CurrentTierDamage[tierIndex]);
        }
        else if (tier > 0 && tier <= PreviousUpgradeDamage.Length &&
                 UpgradableSaberstaffBaseTiers.TryGetValue(resref, out var baseTier) && tier >= baseTier)
        {
            // Reconstruct both tier baselines from the actual kit deltas. Any
            // authored blueprint offset and added enhancements remain above them.
            baseline = (
                baseline.Previous + PreviousUpgradeDamage[tier - 1] - PreviousUpgradeDamage[baseTier - 1],
                baseline.Current + CurrentTierDamage[tier - 1] - CurrentTierDamage[baseTier - 1]);
        }
        return (int)Math.Clamp((long)currentDamage + baseline.Current - baseline.Previous, 1, int.MaxValue);
    }
}

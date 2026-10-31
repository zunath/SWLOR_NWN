using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// The retired ship deeds, ship modules and configuration modules, and what each becomes (plan section 15): hulls map
/// to the ship the hull table names, module tiers Basic, I, II, III and IV map to their family's rank 0, 10, 20, 30
/// and 40 items, capital modules (all needing the old perk's fifth level) to their family's rank 40 item, and
/// configuration modules are refunded at their value. Retired items keep their resref as their tag, except the two
/// store starter deeds.
/// </summary>
public static class LegacySpaceItemCatalog
{
    public static IReadOnlyList<LegacyShipHull> Hulls { get; } = new List<LegacyShipHull>
    {
        // The two store starters become Small ships, flown at Piloting 0.
        new("sdeed_freighter", "ShipDeedLightFreighter", "sd_striker", 10056),
        new("sdeed_escort", "ShipDeedLightEscort", "sd_condor", 10054),
        new("sdeed_striker", "sdeed_striker", "sd_striker", 10057),
        new("sdeed_condor", "sdeed_condor", "sd_condor", 10055),
        new("sdeed_hound", "sdeed_hound", "sd_republicstrik", 10073),
        new("sdeed_panther", "sdeed_panther", "sd_sithscout", 10086),
        new("sdeed_falchion", "sdeed_falchion", "sd_sithscout", 10088),
        new("sdeed_saber", "sdeed_saber", "sd_republicscout", 10070),
        new("sdeed_mule", "sdeed_mule", "sd_republicgunsh", 10063),
        new("sdeed_consular", "sdeed_consular", "sd_republicgunsh", 10063),
        new("sdeed_merchant", "sdeed_merchant", "sd_sithgunship", 10082),
        new("sdeed_throne", "sdeed_throne", "sd_sithinfiltrat", 10083),
        new("sdeed_aurek", "sdeed_aurek", "sd_aurekstrikefi", 10074),
        new("sdeed_sfight", "sdeed_sfight", "sd_sithfighter", 10092),
        new("sdeed_cutla", "sdeed_cutla", "sd_cutlass", 10161),
        new("sdeed_basi", "sdeed_basi", "sd_basiliskwardr", 10120),
        new("capdeed_rthran", "capdeed_rthran", "sd_republicthran", 10123),
        new("capdeed_sthran", "capdeed_sthran", "sd_siththranta", 10139),
        new("capdeed_nthran", "capdeed_nthran", "sd_neutralthrant", 10135),
        new("capdeed_huttco", "capdeed_huttco", "sd_huttcorvette", 10108),
        new("capdeed_jfrigate", "capdeed_jfrigate", "sd_jehaveyir", 10154),
        new("capdeed_chisst", "capdeed_chisst", "sd_chisstrireme", 10162),
        new("capdeed_cgunb", "capdeed_cgunb", "sd_corelliangunb", 10145),
        new("capdeed_hvycor", "capdeed_hvycor", "sd_czerkaarmored", 10218),
        new("capdeed_corsa", "capdeed_corsa", "sd_terminuscorsa", 10207),
        new("capdeed_cruscor", "capdeed_cruscor", "sd_crusadercorve", 10260),
    };

    /// <summary>Each retired module family's items in tier order (Basic, I, II, III, IV) and the family it joins.</summary>
    private static readonly (ShipModuleFamily Family, string[] Tiers)[] TieredModules =
    {
        (ShipModuleFamily.LaserCannon, new[] { "com_laser_b", "com_laser_1", "com_laser_2", "com_laser_3", "com_laser_4" }),
        (ShipModuleFamily.IonCannon, new[] { "ion_cann_b", "ion_cann_1", "ion_cann_2", "ion_cann_3", "ion_cann_4" }),
        (ShipModuleFamily.BeamCannon, new[] { "beamcannon1", "beamcannon2", "beamcannon3", "beamcannon4", "beamcannon5" }),
        (ShipModuleFamily.MissileLauncher, new[] { "msl_launch_b", "msl_launch_1", "msl_launch_2", "msl_launch_3", "msl_launch_4" }),
        (ShipModuleFamily.MiningLaser, new[] { "min_laser_b", "min_laser_1", "min_laser_2", "min_laser_3", "min_laser_4" }),
        (ShipModuleFamily.ShieldBooster, new[] { "shld_boost_b", "shld_boost_1", "shld_boost_2", "shld_boost_3", "shld_boost_4" }),
        (ShipModuleFamily.ShieldRepairer, new[] { "shld_rep_b", "shld_rep_1", "shld_rep_2", "shld_rep_3", "shld_rep_4" }),
        (ShipModuleFamily.ShieldRepairer, new[] { "npc_shld_r_1", "npc_shld_r_2", "npc_shld_r_3", "npc_shld_r_4", "npc_shld_r_5" }),
        (ShipModuleFamily.CapacitorBattery, new[] { "cap_boost_b", "cap_boost_1", "cap_boost_2", "cap_boost_3", "cap_boost_4" }),
        (ShipModuleFamily.HypermatterInjector, new[] { "cap_inject1", "cap_inject2", "cap_inject3", "cap_inject4", "cap_inject5" }),
        (ShipModuleFamily.TargetingComputer, new[] { "tgt_sys_b", "tgt_sys_1", "tgt_sys_2", "tgt_sys_3", "tgt_sys_4" }),
        (ShipModuleFamily.HullPlating, new[] { "hull_boost_b", "hull_boost_1", "hull_boost_2", "hull_boost_3", "hull_boost_4" }),
        (ShipModuleFamily.HullRepairer, new[] { "hull_rep_b", "hull_rep_1", "hull_rep_2", "hull_rep_3", "hull_rep_4" }),
        (ShipModuleFamily.HullRepairer, new[] { "npc_hull_r1", "npc_hull_r2", "npc_hull_r3", "npc_hull_r4", "npc_hull_r5" }),
        (ShipModuleFamily.ThermalPlating, new[] { "las_armor_1", "las_armor_2", "las_armor_3", "las_armor_4", "las_armor_5" }),
        (ShipModuleFamily.EMPlating, new[] { "ion_armor_1", "ion_armor_2", "ion_armor_3", "ion_armor_4", "ion_armor_5" }),
        (ShipModuleFamily.ExplosivePlating, new[] { "exp_armor_1", "exp_armor_2", "exp_armor_3", "exp_armor_4", "exp_armor_5" }),
        (ShipModuleFamily.AdaptivePlating, new[] { "hvy_armor_1", "hvy_armor_2", "hvy_armor_3", "hvy_armor_4", "hvy_armor_5" }),
        (ShipModuleFamily.EMAmplifier, new[] { "em_amp_b", "em_amp_1", "em_amp_2", "em_amp_3", "em_amp_4" }),
        (ShipModuleFamily.ThermalAmplifier, new[] { "therm_amp_b", "therm_amp_1", "therm_amp_2", "therm_amp_3", "therm_amp_4" }),
        (ShipModuleFamily.ExplosiveAmplifier, new[] { "exp_amp_b", "exp_amp_1", "exp_amp_2", "exp_amp_3", "exp_amp_4" }),
        (ShipModuleFamily.Thrusters, new[] { "eva_boost_b", "eva_boost_1", "eva_boost_2", "eva_boost_3", "eva_boost_4" }),
    };

    /// <summary>Retired modules that needed the fifth level of their perk (Piloting 40), and the family they join.</summary>
    private static readonly (ShipModuleFamily Family, string[] Items)[] RankFortyModules =
    {
        (ShipModuleFamily.TorpedoLauncher, new[] { "protbomblnch" }),
        (ShipModuleFamily.StripMiner, new[] { "cap_minlas" }),
        (ShipModuleFamily.Turbolaser, new[] { "turbolas1", "turbolas2", "turbolas3" }),
        (ShipModuleFamily.QuadLaser, new[] { "cap_quadlas1", "cap_quadlas2", "cap_quadlas3" }),
        (ShipModuleFamily.QuadLaser, Enumerable.Range(1, 12).Select(tier => $"npc_quadlas{tier}").ToArray()),
        (ShipModuleFamily.LaserBattery, new[] { "lasbattery1" }),
        (ShipModuleFamily.LaserBattery, Enumerable.Range(1, 12).Select(tier => $"npcautolas{tier}").ToArray()),
        (ShipModuleFamily.AssaultConcussionMissile, new[] { "acm_launch_1" }),
        (ShipModuleFamily.StormCannon, new[] { "storm_cann" }),
        (ShipModuleFamily.BulwarkShieldGenerator, new[] { "bulwarkgen" }),
        (ShipModuleFamily.RepairField, new[] { "repairfield" }),
        (ShipModuleFamily.EWarArray, new[] { "cap_ewar" }),
        (ShipModuleFamily.AdvancedThrusters, new[] { "cap_thrust1" }),
        (ShipModuleFamily.RedundantShieldGenerator, new[] { "cap_shields1" }),
        (ShipModuleFamily.ReinforcedPlating, new[] { "cap_armor1" }),
        (ShipModuleFamily.TargetingArray, new[] { "cap_target1" }),
        (ShipModuleFamily.WeaponsComputer, new[] { "cap_wcomp1" }),
        (ShipModuleFamily.PowerDiverter, new[] { "cap_pwdiv" }),
    };

    /// <summary>
    /// Configuration modules and the credits each is refunded at: its blueprint value. The NPC-only configurations
    /// (no blueprint) are worth nothing.
    /// </summary>
    public static IReadOnlyDictionary<string, int> ConfigurationModules { get; } = BuildConfigurationModules();

    /// <summary>Retired module tag (its resref) to the revamp module tag it becomes.</summary>
    public static IReadOnlyDictionary<string, string> Modules { get; } = BuildModules();

    private static readonly Dictionary<string, LegacyShipHull> _hullsByResref =
        Hulls.ToDictionary(x => x.Resref, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, LegacyShipHull> _hullsByTag =
        Hulls.ToDictionary(x => x.Tag, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every item a retired item becomes, for loading their blueprints.</summary>
    public static IEnumerable<string> ReplacementResrefs =>
        Hulls.Select(x => x.ShipTag).Concat(Modules.Values).Distinct(StringComparer.OrdinalIgnoreCase);

    public static bool TryGetHullByResref(string resref, out LegacyShipHull hull) =>
        _hullsByResref.TryGetValue(resref ?? string.Empty, out hull);

    /// <summary>A registered ship's retired hull, by the deed tag its status stores.</summary>
    public static bool TryGetHullByTag(string tag, out LegacyShipHull hull) =>
        _hullsByTag.TryGetValue(tag ?? string.Empty, out hull);

    public static bool TryGetModule(string resref, out string replacement) =>
        Modules.TryGetValue(resref ?? string.Empty, out replacement);

    public static bool TryGetConfigurationModule(string resref, out int credits) =>
        ConfigurationModules.TryGetValue(resref ?? string.Empty, out credits);

    /// <summary>The revamp item a retired hull or module becomes. False for configuration modules and other items.</summary>
    public static bool TryGetReplacement(string resref, out string replacement)
    {
        if (TryGetHullByResref(resref, out var hull))
        {
            replacement = hull.ShipTag;
            return true;
        }
        return TryGetModule(resref, out replacement);
    }

    /// <summary>Whether an item is a retired deed, module or configuration module.</summary>
    public static bool IsLegacySpaceItem(string resref) =>
        TryGetReplacement(resref, out _) || TryGetConfigurationModule(resref, out _);

    private static Dictionary<string, string> BuildModules()
    {
        var steps = RevampModuleCatalog.Steps.Where(step => !step.Named)
            .ToDictionary(step => (step.Family, step.Rank), step => step.Tag);
        string Step(ShipModuleFamily family, int rank) => steps.TryGetValue((family, rank), out var tag)
            ? tag
            : throw new InvalidOperationException($"The module catalog has no {family} item at rank {rank}.");

        var modules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (family, tiers) in TieredModules)
            for (var tier = 0; tier < tiers.Length; tier++)
                modules.Add(tiers[tier], Step(family, tier * 10));
        foreach (var (family, items) in RankFortyModules)
            foreach (var item in items)
                modules.Add(item, Step(family, 40));
        // Revamp steps a family dropped (the Target Painter starts at rank 10) become its first remaining step.
        foreach (var (retired, replacement) in RevampModuleCatalog.RetiredTags)
            modules.Add(retired, replacement);
        return modules;
    }

    private static Dictionary<string, int> BuildConfigurationModules()
    {
        var values = new[] { 200, 400, 750, 1500, 2500 };
        var configurations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["cap_indus"] = 20000,
            ["cap_skirm"] = 20000,
            ["cap_warship"] = 20000,
        };
        foreach (var role in new[] { "fig", "int", "bmb", "ind" })
            for (var tier = 1; tier <= values.Length; tier++)
                configurations.Add($"config_{role}{tier}", values[tier - 1]);
        foreach (var role in new[] { "fig", "int", "bmb", "hvy" })
            for (var tier = 1; tier <= 6; tier++)
                configurations.Add($"con_{role}{tier}", 0);
        for (var tier = 1; tier <= 7; tier++)
            configurations.Add($"con_cap{tier}", 0);
        for (var tier = 1; tier <= 5; tier++)
            configurations.Add($"npc_cap{tier}", 0);
        return configurations;
    }
}

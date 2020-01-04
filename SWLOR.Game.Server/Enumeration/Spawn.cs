using System;
using System.Collections.Generic;
using SWLOR.Game.Server.AI;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Enumeration
{
    public enum Spawn
    {
        [Spawn("Invalid", ObjectType.Invalid)] 
        None = 0,
        [Spawn("Resources - Tier 1", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 10, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 30, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier1 = 1,
        [Spawn("Resources - Tier 2", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier2 = 2,
        [Spawn("Resources - Tier 3", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier3 = 3,
        [Spawn("Resources - Tier 4", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier4 = 4,
        [Spawn("Resources - Tier 5", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier5 = 5,
        [Spawn("Resources - Tier 6", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier6 = 6,
        [Spawn("Resources - Tier 7", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier7 = 7,
        [Spawn("Resources - Tier 8", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier8 = 8,
        [Spawn("Resources - Tier 9", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier9 = 9,
        [Spawn("Resources - Tier 10", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 10, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 7, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier10 = 10,
        [Spawn("CZ-220 Maintenance Level - Droids", ObjectType.Creature)]
        [SpawnObject("malsecdroid", 50, "", NPCGroup.CZ220MalfunctioningDroids, "StandardBehaviour", Vfx.None, (AIFlags)13)]
        [SpawnObject("malspiderdroid", 50, "", NPCGroup.CZ220MalfunctioningDroids, "StandardBehaviour", Vfx.None, (AIFlags)13)]
        CZ220Droids = 11,
        [Spawn("CZ-220 Maintenance Level - Mynocks", ObjectType.Creature)]
        [SpawnObject("mynock", 100, "", NPCGroup.CZ220Mynocks, "StandardBehaviour", Vfx.None, (AIFlags)13)]
        CZ220Mynocks = 12,
        [Spawn("CZ-220 Scavenge Points", ObjectType.Placeable)]
        [SpawnObject("cz220_junk", 10, "", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        CZ220ScavengePoints = 13,
        [Spawn("CZ-220 Ore Veins", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        CZ220OreVeins = 14,
        [Spawn("CZ-220 Colicoid Experiment Spawn", ObjectType.Creature)]
        [SpawnObject("colicoidexp", 10, "", NPCGroup.CZ220ColicoidExperiment, "StandardBehaviour", Vfx.None, (AIFlags)13)]
        CZ220Colicoid = 15,
        [Spawn("Resources - Tier 1 (Tree/Fiberplast Heavy)", ObjectType.Placeable)]
        [SpawnObject("herbs_patch", 30, "", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("tree", 30, "TreeSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("ore_vein", 10, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 70, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier1TreeFiberplastHeavy = 16,
        [Spawn("Resources - Tier 1 (Ore Heavy)", ObjectType.Placeable)]
        [SpawnObject("herbs_patch", 30, "", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("ore_vein", 70, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("fiberplast_shrub", 30, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ResourcesTier1OreHeavy = 17,
        [Spawn("Viscara - Kath Hounds (Wildlands)", ObjectType.Creature)]
        [SpawnObject("warocas", 40, "", NPCGroup.ViscaraWarocas, "StandardBehaviour", Vfx.None, (AIFlags)13)]
        [SpawnObject("kath_hound", 70, "", NPCGroup.ViscaraWildlandKathHounds, "StandardBehaviour", Vfx.None, (AIFlags)13)]
        ViscaraKathHounds = 18,
        [Spawn("Mandalorian Raiders", ObjectType.Creature)]
        [SpawnObject("man_warrior_1", 30, "", NPCGroup.MandalorianWarriors, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        [SpawnObject("man_warrior_2", 30, "", NPCGroup.MandalorianWarriors, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        [SpawnObject("man_ranger_1", 30, "", NPCGroup.MandalorianRangers, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        [SpawnObject("man_ranger_2", 30, "", NPCGroup.MandalorianRangers, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        MandalorianRaiders = 19,
        [Spawn("Mandalorian Facility Scavenge Sites", ObjectType.Placeable)]
        [SpawnObject("supp_crate", 50, "", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        MandalorianFacilityScavengeSites = 20,
        [Spawn("Mandalorian Leader", ObjectType.Creature)]
        [SpawnObject("man_leader", 99, "", NPCGroup.MandalorianLeader, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        MandalorianLeader = 21,
        [Spawn("Viscara - Wildwoods Looters", ObjectType.Creature)]
        [SpawnObject("looter_1", 30, "", NPCGroup.WildwoodsOutlaws, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        [SpawnObject("looter_2", 30, "", NPCGroup.WildwoodsOutlaws, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        ViscaraLooters = 22,
        [Spawn("Viscara - Wildwoods Gimpassa", ObjectType.Creature)]
        [SpawnObject("ww_gimpassa", 30, "", NPCGroup.WildwoodsGimpassas, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        ViscaraGimpassa = 23,
        [Spawn("Viscara - Wildwoods Kinrath", ObjectType.Creature)]
        [SpawnObject("ww_kinrath", 30, "", NPCGroup.WildwoodsKinraths, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        ViscaraKinrath = 24,
        [Spawn("Viscara - Valley Cairnmogs", ObjectType.Creature)]
        [SpawnObject("vall_nashtah", 20, "", NPCGroup.ValleyNashtah, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        [SpawnObject("valley_cairnmog", 50, "", NPCGroup.ValleyCairnmogs, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        [SpawnObject("valley_cairnmog2", 50, "", NPCGroup.ValleyCairnmogs, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        ViscaraCairnmogs = 25,
        [Spawn("Viscara - Coxxion Base (Instance)", ObjectType.Creature)]
        [SpawnObject("v_flesheater", 10, "", NPCGroup.Invalid, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        [SpawnObject("v_flesheater2", 10, "", NPCGroup.Invalid, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        ViscaraCoxxionBaseInstance = 26,
        [Spawn("Viscara - Coxxion Base Boss", ObjectType.Creature)]
        [SpawnObject("v_fleshleader", 100, "", NPCGroup.VellenFleshleader, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        ViscaraCoxxionBaseBoss = 27,
        [Spawn("Viscara - Deep Mountains", ObjectType.Creature)]
        [SpawnObject("v_raivor", 10, "", NPCGroup.ViscaraDeepMountainRaivors, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        [SpawnObject("v_raivor2", 10, "", NPCGroup.ViscaraDeepMountainRaivors, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        ViscaraDeepMountains = 28,
        [Spawn("Viscara - Cavern Resources", ObjectType.Placeable)]
        [SpawnObject("ore_vein", 30, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_cluster", 70, "CrystalClusterSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        ViscaraCavernResources = 29,
        [Spawn("Viscara - Crystal Cavern", ObjectType.Creature)]
        [SpawnObject("crystalspider", 10, "", NPCGroup.ViscaraCrystalSpider, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        ViscaraCrystalCavern = 30,
        [Spawn("Hutlar - Byysk", ObjectType.Creature)]
        [SpawnObject("byysk_warrior", 10, "", NPCGroup.HutlarByysk, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        [SpawnObject("byysk_warrior2", 10, "", NPCGroup.HutlarByysk, "StandardBehaviour", Vfx.None, (AIFlags)11)]
        HutlarByysk = 31,
        [Spawn("Hutlar - Qion Animals", ObjectType.Creature)]
        [SpawnObject("qion_slug", 10, "", NPCGroup.HutlarQionSlugs, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        [SpawnObject("qion_tiger", 8, "", NPCGroup.QionTiger, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        HutlarQionAnimals = 32,
        [Spawn("Hutlar - Resources", ObjectType.Placeable)]
        [SpawnObject("crystal_clusterb", 10, "ColoredCrystalSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_clusterr", 8, "ColoredCrystalSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_clustery", 6, "ColoredCrystalSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("crystal_clusterg", 12, "ColoredCrystalSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        [SpawnObject("ore_vein", 8, "OreSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        HutlarResources = 33,
        [Spawn("Mon Cala Coral Isles", ObjectType.Creature)]
        [SpawnObject("viper", 20, "", NPCGroup.Invalid, "StandardBehaviour", Vfx.None, (AIFlags)13)]
        [SpawnObject("mc_aradile", 40, "", NPCGroup.Invalid, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        [SpawnObject("mc_amphihydrus", 10, "", NPCGroup.Invalid, "DarkForceUser", Vfx.None, (AIFlags)15)]
        MonCalaCoralIsles = 40,
        [Spawn("Mon Cala Eco-Terrorists", ObjectType.Creature)]
        [SpawnObject("ecoterr_1", 50, "", NPCGroup.Invalid, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        [SpawnObject("ecoterr_2", 50, "", NPCGroup.Invalid, "StandardBehaviour", Vfx.None, (AIFlags)15)]
        MonCalaEcoTerrorists = 41,
        [Spawn("Tatooine Womprat", ObjectType.Creature)]
        [SpawnObject("womprat", 50, "", NPCGroup.Womprat, "StandardBehaviour", Vfx.None, (AIFlags)7)]
        TatooineWomprat = 42,
        [Spawn("Tatooine Sandswimmer", ObjectType.Creature)]
        [SpawnObject("sandswimmer", 50, "", NPCGroup.Sandswimmer, "StandardBehaviour", Vfx.None, (AIFlags)7)]
        TatooineSandswimmer = 43,
        [Spawn("Tatooine Wraid", ObjectType.Creature)]
        [SpawnObject("sandbeetle", 50, "", NPCGroup.Wraid, "StandardBehaviour", Vfx.None, (AIFlags)7)]
        TatooineWraid = 44,
        [Spawn("Tatooine Sand Demon", ObjectType.Creature)]
        [SpawnObject("sanddemon", 50, "", NPCGroup.SandDemon, "StandardBehaviour", Vfx.None, (AIFlags)7)]
        TatooineSandDemon = 45,
        [Spawn("Tatooine Tusken Raider", ObjectType.Creature)]
        [SpawnObject("ext_tusken_tr003", 50, "", NPCGroup.TuskenRaider, "StandardBehaviour", Vfx.None, (AIFlags)7)]
        TatooineTuskenRaider = 46,
        [Spawn("Tatooine Exterior Resources", ObjectType.Placeable)]
        [SpawnObject("fiberplast_shrub", 100, "FiberplastSpawnRule", NPCGroup.Invalid, "", Vfx.None, (AIFlags)0)]
        TatooineExteriorResources = 47
    }

    public class SpawnAttribute : Attribute
    {
        public string Name { get; set; }
        public ObjectType SpawnObjectType { get; set; }
        public HashSet<SpawnObjectAttribute> SpawnObjects { get; set; }

        public SpawnAttribute(string name, ObjectType spawnObjectType)
        {
            Name = name;
            SpawnObjectType = spawnObjectType;
            SpawnObjects = new HashSet<SpawnObjectAttribute>();
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class SpawnObjectAttribute: Attribute
    {
        public string Resref { get; set; }
        public int Weight { get; set; }
        public string SpawnRule { get; set; }
        public NPCGroup NPCGroup { get; set; }
        public string BehaviourScript { get; set; }
        public Vfx DeathVFX { get; set; }
        public AIFlags AIFlags { get; set; }

        public SpawnObjectAttribute(string resref, int weight, string spawnRule, NPCGroup npcGroup, string behaviourScript, Vfx deathVFX, AIFlags aiFlags)
        {
            Resref = string.IsNullOrWhiteSpace(resref) ? string.Empty : resref;
            Weight = weight;
            SpawnRule = spawnRule;
            NPCGroup = npcGroup;
            BehaviourScript = string.IsNullOrWhiteSpace(behaviourScript) ? string.Empty : BehaviourScript;
            DeathVFX = deathVFX;
            AIFlags = aiFlags;
        }
    }
}

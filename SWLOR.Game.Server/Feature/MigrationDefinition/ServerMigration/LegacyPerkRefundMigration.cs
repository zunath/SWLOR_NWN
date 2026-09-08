using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    internal static class LegacyPerkRefundMigration
    {
        // Historical player perk IDs and per-rank prices from master ce4f91749c2e.
        // Keep these independent of the current enum: retired IDs have been reused.
        private static readonly (int Id, string Name, int[] Prices)[] LegacyPerks =
        {
            (1, "Doublehand", new[] { 1, 1, 1, 1, 2 }),
            (2, "DualWield", new[] { 4 }),
            (4, "WeaponFocusVibroblades", new[] { 3, 4 }),
            (5, "ImprovedCriticalVibroblades", new[] { 3 }),
            (6, "VibrobladeProficiency", new[] { 2, 2, 2, 2, 2 }),
            (7, "VibrobladeMastery", new[] { 8, 8 }),
            (8, "HackingBlade", new[] { 3, 3, 3 }),
            (9, "RiotBlade", new[] { 2, 3, 3 }),
            (10, "WeaponFocusFinesseVibroblades", new[] { 3, 4 }),
            (11, "ImprovedCriticalFinesseVibroblades", new[] { 3 }),
            (12, "FinesseVibrobladeProficiency", new[] { 2, 2, 2, 2, 2 }),
            (13, "FinesseVibrobladeMastery", new[] { 8, 8 }),
            (14, "PoisonStab", new[] { 3, 3, 3 }),
            (15, "Backstab", new[] { 2, 3, 3 }),
            (16, "WeaponFocusLightsabers", new[] { 3, 4 }),
            (17, "ImprovedCriticalLightsabers", new[] { 3 }),
            (18, "LightsaberProficiency", new[] { 2, 2, 2, 2, 2 }),
            (19, "LightsaberMastery", new[] { 8, 8 }),
            (20, "ForceLeap", new[] { 3, 3, 3 }),
            (21, "SaberStrike", new[] { 2, 3, 3 }),
            (22, "PowerAttack", new[] { 3, 4 }),
            (23, "SuperiorWeaponFocus", new[] { 5 }),
            (24, "IncreasedMultiplier", new[] { 6 }),
            (25, "Cleave", new[] { 3 }),
            (26, "WeaponFocusHeavyVibroblades", new[] { 3, 4 }),
            (27, "ImprovedCriticalHeavyVibroblades", new[] { 3 }),
            (28, "HeavyVibrobladeProficiency", new[] { 2, 2, 2, 2, 2 }),
            (29, "HeavyVibrobladeMastery", new[] { 8, 8 }),
            (30, "CrescentMoon", new[] { 3, 3, 3 }),
            (31, "HardSlash", new[] { 2, 3, 3 }),
            (32, "WeaponFocusPolearms", new[] { 3, 4 }),
            (33, "ImprovedCriticalPolearms", new[] { 3 }),
            (34, "PolearmProficiency", new[] { 2, 2, 2, 2, 2 }),
            (35, "PolearmMastery", new[] { 8, 8 }),
            (36, "Skewer", new[] { 3, 3, 3 }),
            (37, "DoubleThrust", new[] { 2, 3, 3 }),
            (38, "WeaponFocusTwinBlades", new[] { 3, 4 }),
            (39, "ImprovedCriticalTwinBlades", new[] { 3 }),
            (40, "TwinBladeProficiency", new[] { 2, 2, 2, 2, 2 }),
            (41, "TwinBladeMastery", new[] { 8, 8 }),
            (42, "LegSweep", new[] { 3, 3, 3 }),
            (43, "CrossCut", new[] { 2, 3, 3 }),
            (44, "WeaponFocusSaberstaffs", new[] { 3, 4 }),
            (45, "ImprovedCriticalSaberstaffs", new[] { 3 }),
            (46, "SaberstaffProficiency", new[] { 2, 2, 2, 2, 2 }),
            (47, "SaberstaffMastery", new[] { 8, 8 }),
            (48, "CircleSlash", new[] { 3, 3, 3 }),
            (49, "DoubleStrike", new[] { 2, 3, 3 }),
            (50, "Knockdown", new[] { 3 }),
            (51, "Furor", new[] { 4 }),
            (52, "InnerStrength", new[] { 5, 6 }),
            (53, "ImprovedTwoWeaponFightingOneHanded", new[] { 4 }),
            (54, "WeaponFocusKatars", new[] { 3, 4 }),
            (55, "ImprovedCriticalKatars", new[] { 3 }),
            (56, "KatarProficiency", new[] { 2, 2, 2, 2, 2 }),
            (57, "KatarMastery", new[] { 8, 8 }),
            (58, "ElectricFist", new[] { 3, 3, 3 }),
            (59, "StrikingCobra", new[] { 2, 3, 3 }),
            (60, "WeaponFocusStaves", new[] { 3, 4 }),
            (61, "ImprovedCriticalStaves", new[] { 3 }),
            (62, "StaffProficiency", new[] { 2, 2, 2, 2, 2 }),
            (63, "StaffMastery", new[] { 8, 8 }),
            (64, "Slam", new[] { 2, 3, 3 }),
            (65, "SpinningWhirl", new[] { 3, 3, 3 }),
            (66, "RapidShot", new[] { 3, 5 }),
            (67, "RapidReload", new[] { 3 }),
            (68, "ZenMarksmanship", new[] { 3 }),
            (69, "PrecisionAim", new[] { 3, 3 }),
            (70, "PointBlankShot", new[] { 3 }),
            (71, "WeaponFocusPistols", new[] { 3, 4 }),
            (72, "ImprovedCriticalPistols", new[] { 3 }),
            (73, "PistolProficiency", new[] { 2, 2, 2, 2, 2 }),
            (74, "PistolMastery", new[] { 8, 8 }),
            (75, "QuickDraw", new[] { 3, 3, 3 }),
            (76, "DoubleShot", new[] { 2, 3, 3 }),
            (77, "WeaponFocusThrowingWeapons", new[] { 3, 4 }),
            (78, "ImprovedCriticalThrowingWeapons", new[] { 3 }),
            (79, "ThrowingWeaponProficiency", new[] { 2, 2, 2, 2, 2 }),
            (80, "ThrowingWeaponMastery", new[] { 8, 8 }),
            (81, "ExplosiveToss", new[] { 3, 3, 3 }),
            (82, "PiercingToss", new[] { 2, 3, 3 }),
            (89, "WeaponFocusRifles", new[] { 3, 4 }),
            (90, "ImprovedCriticalRifles", new[] { 3 }),
            (91, "RifleProficiency", new[] { 2, 2, 2, 2, 2 }),
            (92, "RifleMastery", new[] { 8, 8 }),
            (93, "TranquilizerShot", new[] { 3, 3, 3 }),
            (94, "CripplingShot", new[] { 2, 3, 3 }),
            (95, "ForcePush", new[] { 1, 2, 2, 3 }),
            (96, "BurstOfSpeed", new[] { 2, 2 }),
            (97, "ThrowLightsaber", new[] { 2, 2, 2 }),
            (98, "ForceStun", new[] { 2, 2, 3 }),
            (99, "ComprehendSpeech", new[] { 1, 1, 1, 1 }),
            (100, "BattleInsight", new[] { 2, 2 }),
            (101, "MindTrick", new[] { 2, 2 }),
            (102, "CloakProficiency", new[] { 1, 1, 1, 1, 1 }),
            (103, "BeltProficiency", new[] { 1, 1, 1, 1, 1 }),
            (104, "RingProficiency", new[] { 1, 1, 1, 1, 1 }),
            (105, "NecklaceProficiency", new[] { 1, 1, 1, 1, 1 }),
            (106, "BreastplateProficiency", new[] { 1, 1, 1, 1, 1 }),
            (107, "HelmetProficiency", new[] { 1, 1, 1, 1, 1 }),
            (108, "BracerProficiency", new[] { 1, 1, 1, 1, 1 }),
            (109, "LeggingProficiency", new[] { 1, 1, 1, 1, 1 }),
            (110, "ShieldProficiency", new[] { 1, 1, 1, 1, 1 }),
            (111, "TunicProficiency", new[] { 1, 1, 1, 1, 1 }),
            (112, "CapProficiency", new[] { 1, 1, 1, 1, 1 }),
            (113, "GloveProficiency", new[] { 1, 1, 1, 1, 1 }),
            (114, "BootProficiency", new[] { 1, 1, 1, 1, 1 }),
            (115, "Resuscitation", new[] { 4, 4, 4 }),
            (116, "Starships", new[] { 1, 1, 2, 3, 4 }),
            (117, "DefensiveModules", new[] { 1, 1, 2, 3, 3 }),
            (118, "OffensiveModules", new[] { 1, 1, 2, 3, 3 }),
            (119, "EnergyManagement", new[] { 5, 5 }),
            (120, "MiningModules", new[] { 1, 1, 2, 3, 3 }),
            (121, "StarshipMining", new[] { 5, 5 }),
            (122, "RangedHealing", new[] { 2, 3, 4, 5 }),
            (123, "FrugalMedic", new[] { 1, 2, 2 }),
            (124, "MedKit", new[] { 1, 2, 3, 4, 4 }),
            (125, "TreatmentKit", new[] { 2, 2 }),
            (126, "Shielding", new[] { 2, 3, 3, 4 }),
            (127, "OneHandedBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (128, "TwoHandedBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (129, "MartialBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (130, "RangedBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (131, "CityManagement", new[] { 2, 3, 4, 5 }),
            (132, "ArmorBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (133, "AccessoryBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (134, "Upkeep", new[] { 3, 3 }),
            (135, "ImprovedTwoWeaponFightingTwoHanded", new[] { 4 }),
            (136, "RapidSynthesisSmithery", new[] { 1 }),
            (137, "CarefulSynthesisSmithery", new[] { 1 }),
            (138, "BasicTouchSmithery", new[] { 1 }),
            (139, "StandardTouchSmithery", new[] { 1 }),
            (140, "PreciseTouchSmithery", new[] { 1 }),
            (141, "MastersMendSmithery", new[] { 1 }),
            (142, "SteadyHandSmithery", new[] { 1 }),
            (143, "MuscleMemorySmithery", new[] { 1 }),
            (144, "VenerationSmithery", new[] { 1 }),
            (145, "WasteNotSmithery", new[] { 1 }),
            (146, "RapidSynthesisFabrication", new[] { 1 }),
            (147, "CarefulSynthesisFabrication", new[] { 1 }),
            (148, "BasicTouchFabrication", new[] { 1 }),
            (149, "FurnitureBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (150, "StructureBlueprints", new[] { 6, 6 }),
            (151, "Chi", new[] { 3, 4, 4 }),
            (152, "StarshipBlueprints", new[] { 2, 2, 2, 3, 3 }),
            (153, "StandardTouchFabrication", new[] { 1 }),
            (154, "PreciseTouchFabrication", new[] { 1 }),
            (155, "MastersMendFabrication", new[] { 1 }),
            (156, "SteadyHandFabrication", new[] { 1 }),
            (157, "Harvesting", new[] { 1, 1, 2, 3, 3 }),
            (158, "Refining", new[] { 1, 1, 2, 3, 3 }),
            (159, "RefineryManagement", new[] { 1, 1, 2, 2, 2, 2 }),
            (160, "Scavenging", new[] { 1, 1, 2, 3, 3 }),
            (161, "HardLook", new[] { 1, 1, 2, 3, 3 }),
            (162, "ForceHeal", new[] { 2, 2, 2, 3, 3 }),
            (163, "ForceBurst", new[] { 2, 2, 3, 3 }),
            (164, "ForceBody", new[] { 3, 4 }),
            (165, "ForceDrain", new[] { 2, 2, 2, 3, 3 }),
            (166, "ForceLightning", new[] { 2, 2, 3, 3 }),
            (167, "ForceMind", new[] { 3, 4 }),
            (169, "KoltoRecovery", new[] { 3, 4, 5 }),
            (170, "StasisField", new[] { 2, 3, 4 }),
            (171, "CombatEnhancement", new[] { 3, 3, 4 }),
            (172, "Provoke", new[] { 2, 3 }),
            (177, "MuscleMemoryFabrication", new[] { 1 }),
            (178, "VenerationFabrication", new[] { 1 }),
            (179, "WasteNotFabrication", new[] { 1 }),
            (180, "Dash", new[] { 2, 3 }),
            (181, "TreasureHunter", new[] { 3, 3, 4 }),
            (182, "CreditFinder", new[] { 3, 3, 4 }),
            (183, "RapidSynthesisCooking", new[] { 1 }),
            (184, "CarefulSynthesisCooking", new[] { 1 }),
            (185, "BasicTouchCooking", new[] { 1 }),
            (186, "StandardTouchCooking", new[] { 1 }),
            (187, "PreciseTouchCooking", new[] { 1 }),
            (188, "MastersMendCooking", new[] { 1 }),
            (189, "SteadyHandCooking", new[] { 1 }),
            (190, "MuscleMemoryCooking", new[] { 1 }),
            (191, "VenerationCooking", new[] { 1 }),
            (192, "WasteNotCooking", new[] { 1 }),
            (193, "CookingRecipes", new[] { 1, 1, 2, 3, 3 }),
            (194, "RapidSynthesisEngineering", new[] { 1 }),
            (195, "CarefulSynthesisEngineering", new[] { 1 }),
            (196, "BasicTouchEngineering", new[] { 1 }),
            (197, "StandardTouchEngineering", new[] { 1 }),
            (198, "PreciseTouchEngineering", new[] { 1 }),
            (199, "MastersMendEngineering", new[] { 1 }),
            (200, "SteadyHandEngineering", new[] { 1 }),
            (201, "MuscleMemoryEngineering", new[] { 1 }),
            (202, "VenerationEngineering", new[] { 1 }),
            (203, "WasteNotEngineering", new[] { 1 }),
            (204, "FragGrenade", new[] { 2, 3, 3 }),
            (205, "ConcussionGrenade", new[] { 2, 3, 3 }),
            (206, "FlashbangGrenade", new[] { 2, 3, 3 }),
            (207, "IonGrenade", new[] { 2, 3, 3 }),
            (208, "KoltoGrenade", new[] { 2, 3, 3 }),
            (209, "AdhesiveGrenade", new[] { 2, 3, 3 }),
            (210, "SmokeBomb", new[] { 2, 3, 3 }),
            (211, "KoltoBomb", new[] { 2, 3, 3 }),
            (212, "IncendiaryBomb", new[] { 2, 3, 3 }),
            (213, "GasBomb", new[] { 2, 3, 3 }),
            (214, "StealthGenerator", new[] { 2, 3, 3 }),
            (215, "Flamethrower", new[] { 2, 3, 3 }),
            (216, "WristRocket", new[] { 2, 3, 3 }),
            (217, "DeflectorShield", new[] { 2, 3, 3 }),
            (218, "DemolitionExpert", new[] { 1, 2, 3 }),
            (219, "GuildRelations", new[] { 2, 2, 3, 3 }),
            (220, "SmitheryEquipment", new[] { 2, 3, 4, 4, 5 }),
            (221, "EngineeringEquipment", new[] { 2, 3, 4, 4, 5 }),
            (222, "FabricationEquipment", new[] { 2, 3, 4, 4, 5 }),
            (223, "AgricultureEquipment", new[] { 2, 3, 4, 4, 5 }),
            (224, "EnhancementBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (225, "StrongStyleLightsaber", new[] { 1 }),
            (226, "StrongStyleSaberstaff", new[] { 1 }),
            (227, "Premonition", new[] { 2, 2 }),
            (228, "Disturbance", new[] { 2, 2, 2 }),
            (229, "Benevolence", new[] { 2, 2, 3 }),
            (230, "ForceValor", new[] { 2, 3 }),
            (231, "ForceSpark", new[] { 2, 2, 2 }),
            (232, "CreepingTerror", new[] { 2, 2, 3 }),
            (233, "ForceRage", new[] { 2, 3 }),
            (234, "DroidEquipmentBlueprints", new[] { 1, 1, 2, 3, 3 }),
            (235, "ThrowRock", new[] { 1, 2, 2, 2, 3 }),
            (236, "FlurryStyle", new[] { 1, 4 }),
            (237, "CrushingStyle", new[] { 1, 4 }),
            (238, "Duelist", new[] { 3 }),
            (239, "ShieldMaster", new[] { 4 }),
            (240, "WailingBlows", new[] { 4 }),
            (241, "DirtyBlow", new[] { 4 }),
            (242, "ShieldBash", new[] { 2, 3, 3 }),
            (243, "ForceInspiration", new[] { 2, 3, 4 }),
            (244, "Bulwark", new[] { 3 }),
            (245, "RousingShout", new[] { 2, 2, 2 }),
            (246, "Dedication", new[] { 1, 2, 2 }),
            (247, "SoldiersSpeed", new[] { 2, 2, 2 }),
            (248, "SoldiersStrike", new[] { 2, 2, 2 }),
            (249, "Charge", new[] { 2, 2 }),
            (250, "SoldiersPrecision", new[] { 2, 2, 2 }),
            (251, "ShockingShout", new[] { 3 }),
            (252, "Rejuvenation", new[] { 2, 2, 2 }),
            (253, "FrenziedShout", new[] { 2, 2, 2 }),
            (254, "IncubationManagement", new[] { 2, 3 }),
            (255, "IntuitivePiloting", new[] { 3 }),
            (256, "ScientificNetworking", new[] { 3, 3 }),
            (257, "FishingRods", new[] { 1, 1, 1, 1, 2 }),
            (258, "ShoutRange", new[] { 2, 2 }),
            (259, "ShieldResistance", new[] { 2, 3 }),
            (260, "Infusion", new[] { 3, 4 }),
            (261, "Alacrity", new[] { 2 }),
            (262, "Clarity", new[] { 2 }),
            (263, "Tame", new[] { 3, 3, 4, 5, 5 }),
            (264, "DroidAssembly", new[] { 3, 3, 3, 3, 3 }),
            (265, "Reward", new[] { 1, 2, 2 }),
            (266, "Stabling", new[] { 1, 1, 1, 1, 1 }),
            (268, "Snarl", new[] { 2 }),
            (269, "Growl", new[] { 2 }),
            (270, "SoothePet", new[] { 2 }),
            (271, "ReviveBeast", new[] { 1, 2, 3 }),
            (272, "DNAManipulation", new[] { 2, 2, 2, 3, 3 }),
            (273, "IncubationProcessing", new[] { 2, 2, 3, 3 }),
            (274, "ErraticGenius", new[] { 2, 3, 3 }),
            (297, "Research", new[] { 2, 2, 3, 3, 3 }),
            (298, "AdrenalStim", new[] { 2, 2, 3 }),
            (299, "ResearchProjects", new[] { 2, 3 }),
            (300, "PommelStrike", new[] { 2, 3, 3 }),
        };

        private static readonly Dictionary<int, string[]> Aliases = new()
        {
            [127] = new[] { "BladeBlueprints" },
            [128] = new[] { "HeavyWeaponBlueprints" },
            [130] = new[] { "ProjectileBlueprints" }
        };

        public static int Migrate(JObject player, IEnumerable<PerkType> removedPerks)
        {
            var removed = removedPerks.ToHashSet();
            var learned = player[nameof(Player.Perks)] as JObject;
            var unlocked = player[nameof(Player.UnlockedPerks)] as JObject;
            var refund = 0;

            foreach (var (id, name, prices) in LegacyPerks)
            {
                var keys = new[] { id.ToString(), name }
                    .Concat(Aliases.GetValueOrDefault(id) ?? Array.Empty<string>()).ToArray();
                var rank = keys.Where(key => learned?[key] != null)
                    .Select(key => learned[key].Value<int>()).DefaultIfEmpty(0).Max();
                // A reused name with a different ID identifies a replacement perk.
                // Refund its historical investment before the full rebuild prices it
                // using the new definition, just as for a removed perk.
                var retired = !Enum.TryParse(name, out PerkType currentType) ||
                              (int)currentType != id || removed.Contains(currentType);
                if (retired)
                {
                    refund += prices.Take(Math.Max(0, rank)).Sum();
                    foreach (var key in keys)
                    {
                        learned?.Remove(key);
                        unlocked?.Remove(key);
                    }
                }
                else
                {
                    // Canonicalize aliases for perks whose identity is unchanged.
                    if (rank > 0)
                        learned[name] = rank;
                    foreach (var key in keys.Where(key => key != name))
                    {
                        learned?.Remove(key);
                        if (unlocked?[key] == null)
                            continue;
                        unlocked[name] ??= unlocked[key].DeepClone();
                        unlocked.Remove(key);
                    }
                }
            }

            return refund;
        }
    }
}

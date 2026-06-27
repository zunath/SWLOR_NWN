using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.CraftService;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _22_CombatSystemReplacement : ServerMigrationBase, IServerMigration
    {
        private const int LegacyFlurryStylePerkId = 236;
        private static readonly string CurrentFlurryStyleKey = nameof(PerkType.FlurryStyle);

        private readonly Dictionary<(PerkType, int), int> _refundMap = new()
        {
            {(LegacyPerkType(180), 1), 2},
            {(LegacyPerkType(180), 2), 3},
            {(LegacyPerkType(242), 1), 2},
            {(LegacyPerkType(242), 2), 3},
            {(LegacyPerkType(242), 3), 3},
            {(LegacyPerkType(244), 1), 3},
            {(LegacyPerkType(261), 1), 2},
            {(LegacyPerkType(8), 1), 3},
            {(LegacyPerkType(8), 2), 3},
            {(LegacyPerkType(8), 3), 3},
            {(LegacyPerkType(9), 1), 2},
            {(LegacyPerkType(9), 2), 3},
            {(LegacyPerkType(9), 3), 3},
            {(LegacyPerkType(15), 1), 2},
            {(LegacyPerkType(15), 2), 3},
            {(LegacyPerkType(15), 3), 3},
            {(LegacyPerkType(48), 1), 3},
            {(LegacyPerkType(48), 2), 3},
            {(LegacyPerkType(48), 3), 3},
            {(LegacyPerkType(49), 1), 2},
            {(LegacyPerkType(49), 2), 3},
            {(LegacyPerkType(49), 3), 3},
            {(LegacyPerkType(65), 1), 3},
            {(LegacyPerkType(65), 2), 3},
            {(LegacyPerkType(65), 3), 3},
            {(LegacyPerkType(43), 1), 2},
            {(LegacyPerkType(43), 2), 3},
            {(LegacyPerkType(43), 3), 3},
            {(LegacyPerkType(59), 1), 2},
            {(LegacyPerkType(59), 2), 3},
            {(LegacyPerkType(59), 3), 3},
            {(LegacyPerkType(64), 1), 2},
            {(LegacyPerkType(64), 2), 3},
            {(LegacyPerkType(64), 3), 3},
            {(LegacyPerkType(42), 1), 3},
            {(LegacyPerkType(42), 2), 3},
            {(LegacyPerkType(42), 3), 3},
            {(LegacyPerkType(237), 1), 1},
            {(LegacyPerkType(237), 2), 4},
            {(LegacyPerkType(75), 1), 3},
            {(LegacyPerkType(75), 2), 3},
            {(LegacyPerkType(75), 3), 3},
            {(LegacyPerkType(76), 1), 2},
            {(LegacyPerkType(76), 2), 3},
            {(LegacyPerkType(76), 3), 3},
            {(LegacyPerkType(93), 1), 3},
            {(LegacyPerkType(93), 2), 3},
            {(LegacyPerkType(93), 3), 3},
            {(LegacyPerkType(94), 1), 2},
            {(LegacyPerkType(94), 2), 3},
            {(LegacyPerkType(94), 3), 3},
            {(LegacyPerkType(81), 1), 3},
            {(LegacyPerkType(81), 2), 3},
            {(LegacyPerkType(81), 3), 3},
            {(LegacyPerkType(82), 1), 2},
            {(LegacyPerkType(82), 2), 3},
            {(LegacyPerkType(82), 3), 3},
            {(LegacyPerkType(53), 1), 4},
            {(LegacyPerkType(135), 1), 4},
            {(LegacyPerkType(51), 1), 4},
            {(LegacyPerkType(239), 1), 4},
            {(LegacyPerkType(7), 1), 8},
            {(LegacyPerkType(7), 2), 8},
            {(LegacyPerkType(13), 1), 8},
            {(LegacyPerkType(13), 2), 8},
            {(LegacyPerkType(19), 1), 8},
            {(LegacyPerkType(19), 2), 8},
            {(LegacyPerkType(29), 1), 8},
            {(LegacyPerkType(29), 2), 8},
            {(LegacyPerkType(35), 1), 8},
            {(LegacyPerkType(35), 2), 8},
            {(LegacyPerkType(41), 1), 8},
            {(LegacyPerkType(41), 2), 8},
            {(LegacyPerkType(47), 1), 8},
            {(LegacyPerkType(47), 2), 8},
            {(LegacyPerkType(57), 1), 8},
            {(LegacyPerkType(57), 2), 8},
            {(LegacyPerkType(63), 1), 8},
            {(LegacyPerkType(63), 2), 8},
            {(LegacyPerkType(66), 1), 3},
            {(LegacyPerkType(66), 2), 5},
            {(LegacyPerkType(74), 1), 8},
            {(LegacyPerkType(74), 2), 8},
            {(LegacyPerkType(80), 1), 8},
            {(LegacyPerkType(80), 2), 8},
            {(LegacyPerkType(67), 1), 3},
            {(LegacyPerkType(92), 1), 8},
            {(LegacyPerkType(92), 2), 8},
        };

        private readonly Dictionary<string, int> _legacyPerkIdByName = new()
        {
            {"Doublehand", 1},
            {"DualWield", 2},
            {"WeaponFinesse", 3},
            {"WeaponFocusVibroblades", 4},
            {"ImprovedCriticalVibroblades", 5},
            {"VibrobladeProficiency", 6},
            {"VibrobladeMastery", 7},
            {"HackingBlade", 8},
            {"RiotBlade", 9},
            {"WeaponFocusFinesseVibroblades", 10},
            {"ImprovedCriticalFinesseVibroblades", 11},
            {"FinesseVibrobladeProficiency", 12},
            {"FinesseVibrobladeMastery", 13},
            {"PoisonStab", 14},
            {"Backstab", 15},
            {"WeaponFocusLightsabers", 16},
            {"ImprovedCriticalLightsabers", 17},
            {"LightsaberProficiency", 18},
            {"LightsaberMastery", 19},
            {"SaberStrike", 21},
            {"PowerAttack", 22},
            {"SuperiorWeaponFocus", 23},
            {"IncreasedMultiplier", 24},
            {"Cleave", 25},
            {"WeaponFocusHeavyVibroblades", 26},
            {"ImprovedCriticalHeavyVibroblades", 27},
            {"HeavyVibrobladeProficiency", 28},
            {"HeavyVibrobladeMastery", 29},
            {"CrescentMoon", 30},
            {"HardSlash", 31},
            {"WeaponFocusPolearms", 32},
            {"ImprovedCriticalPolearms", 33},
            {"PolearmProficiency", 34},
            {"PolearmMastery", 35},
            {"Skewer", 36},
            {"DoubleThrust", 37},
            {"WeaponFocusTwinBlades", 38},
            {"ImprovedCriticalTwinBlades", 39},
            {"TwinBladeProficiency", 40},
            {"TwinBladeMastery", 41},
            {"LegSweep", 42},
            {"CrossCut", 43},
            {"WeaponFocusSaberstaffs", 44},
            {"ImprovedCriticalSaberstaffs", 45},
            {"SaberstaffProficiency", 46},
            {"SaberstaffMastery", 47},
            {"CircleSlash", 48},
            {"DoubleStrike", 49},
            {"Knockdown", 50},
            {"Furor", 51},
            {"InnerStrength", 52},
            {"ImprovedTwoWeaponFightingOneHanded", 53},
            {"WeaponFocusKatars", 54},
            {"ImprovedCriticalKatars", 55},
            {"KatarProficiency", 56},
            {"KatarMastery", 57},
            {"ElectricFist", 58},
            {"StrikingCobra", 59},
            {"WeaponFocusStaves", 60},
            {"ImprovedCriticalStaves", 61},
            {"StaffProficiency", 62},
            {"StaffMastery", 63},
            {"Slam", 64},
            {"SpinningWhirl", 65},
            {"RapidShot", 66},
            {"RapidReload", 67},
            {"ZenMarksmanship", 68},
            {"PrecisionAim", 69},
            {"PointBlankShot", 70},
            {"WeaponFocusPistols", 71},
            {"ImprovedCriticalPistols", 72},
            {"PistolProficiency", 73},
            {"PistolMastery", 74},
            {"QuickDraw", 75},
            {"DoubleShot", 76},
            {"WeaponFocusThrowingWeapons", 77},
            {"ImprovedCriticalThrowingWeapons", 78},
            {"ThrowingWeaponProficiency", 79},
            {"ThrowingWeaponMastery", 80},
            {"ExplosiveToss", 81},
            {"PiercingToss", 82},
            {"WeaponFocusCannons", 83},
            {"ImprovedCriticalCannons", 84},
            {"CannonProficiency", 85},
            {"CannonMastery", 86},
            {"FullAuto", 87},
            {"HammerShot", 88},
            {"WeaponFocusRifles", 89},
            {"ImprovedCriticalRifles", 90},
            {"RifleProficiency", 91},
            {"RifleMastery", 92},
            {"TranquilizerShot", 93},
            {"CripplingShot", 94},
            {"Traumatize", 168},
            {"Cover", 173},
            {"ShieldExpertise", 174},
            {"Ironclad", 175},
            {"Mobility", 176},
            {"Dash", 180},
            {"StrongStyleLightsaber", 225},
            {"StrongStyleSaberstaff", 226},
            {"FlurryStyle", 236},
            {"CrushingStyle", 237},
            {"Duelist", 238},
            {"ShieldMaster", 239},
            {"WailingBlows", 240},
            {"DirtyBlow", 241},
            {"ShieldBash", 242},
            {"Bulwark", 244},
            {"ShieldResistance", 259},
            {"Alacrity", 261},
            {"Clarity", 262},
            {"PommelStrike", 300},
        };

        private static readonly Dictionary<PerkType, int[]> PlayerRemovedPerks = new()
        {
            { PerkType.DemolitionExpert, new[] { 1, 2, 3 } },
            { PerkType.FlashbangGrenade, new[] { 2, 3, 3 } },
            { PerkType.KoltoGrenade, new[] { 2, 3, 3 } },
            { PerkType.KoltoBomb, new[] { 2, 3, 3 } },
            { PerkType.IncendiaryBomb, new[] { 2, 3, 3 } },
            { PerkType.GasBomb, new[] { 2, 3, 3 } },
            { PerkType.StealthGenerator, new[] { 2, 3, 3 } },

            { PerkType.RangedHealing, new[] { 2, 3, 4, 5 } },
            { PerkType.FrugalMedic, new[] { 1, 2, 2 } },
            { PerkType.KoltoRecovery, new[] { 3, 4, 5 } },
            { PerkType.StasisField, new[] { 2, 3, 4 } },
            { PerkType.CombatEnhancement, new[] { 3, 3, 4 } },

            { PerkType.ForceHeal, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.ForceBurst, new[] { 2, 2, 3, 3 } },
            { PerkType.Disturbance, new[] { 2, 2, 2 } },
            { PerkType.ForceValor, new[] { 2, 3 } },
            { PerkType.ThrowRock, new[] { 1, 2, 2, 2, 3 } },
            { PerkType.BurstOfSpeed, new[] { 2, 2 } },
            { PerkType.ThrowLightsaber, new[] { 2, 2, 2 } },
            { PerkType.ForceStun, new[] { 2, 2, 3 } },
            { PerkType.BattleInsight, new[] { 2, 2 } },
            { PerkType.ForceMind, new[] { 3, 4 } },
            { PerkType.Premonition, new[] { 2, 2 } },
            { PerkType.ForceInspiration, new[] { 2, 3, 4 } },

            { PerkType.Dedication, new[] { 1, 2, 2 } },
            { PerkType.SoldiersSpeed, new[] { 2, 2, 2 } },
            { PerkType.SoldiersStrike, new[] { 2, 2, 2 } },
            { PerkType.Charge, new[] { 2, 2 } },
            { PerkType.SoldiersPrecision, new[] { 2, 2, 2 } },
            { PerkType.ShockingShout, new[] { 3 } },
            { PerkType.Rejuvenation, new[] { 2, 2, 2 } },
            { PerkType.FrenziedShout, new[] { 2, 2, 2 } },
            { PerkType.ShoutRange, new[] { 2, 2 } },

            { PerkType.CapacitorRig, new[] { 2, 2, 4 } },
            { PerkType.PulseRelay, new[] { 3, 3 } },

            { PerkType.Decoy, new[] { 3 } },
            { LegacyPerkType(388), new[] { 3 } },
            { PerkType.WhirlwindAssault, new[] { 3, 3 } },

            { PerkType.WeaponBlueprints, new[] { 2, 3, 4, 5, 6 } },
            { PerkType.ArmorBlueprints, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.AccessoryBlueprints, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.FurnitureBlueprints, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.StructureBlueprints, new[] { 6, 6 } },
            { PerkType.StarshipBlueprints, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.EnhancementBlueprints, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.DroidEquipmentBlueprints, new[] { 1, 1, 2, 3, 3 } },

            { PerkType.BasicSynthesis, new[] { 0, 0, 0, 0 } },
            { PerkType.RapidSynthesisSmithery, new[] { 1 } },
            { PerkType.CarefulSynthesisSmithery, new[] { 1 } },
            { PerkType.BasicTouchSmithery, new[] { 1 } },
            { PerkType.StandardTouchSmithery, new[] { 1 } },
            { PerkType.PreciseTouchSmithery, new[] { 1 } },
            { PerkType.MastersMendSmithery, new[] { 1 } },
            { PerkType.SteadyHandSmithery, new[] { 1 } },
            { PerkType.MuscleMemorySmithery, new[] { 1 } },
            { PerkType.VenerationSmithery, new[] { 1 } },
            { PerkType.WasteNotSmithery, new[] { 1 } },
            { PerkType.RapidSynthesisFabrication, new[] { 1 } },
            { PerkType.CarefulSynthesisFabrication, new[] { 1 } },
            { PerkType.BasicTouchFabrication, new[] { 1 } },
            { PerkType.StandardTouchFabrication, new[] { 1 } },
            { PerkType.PreciseTouchFabrication, new[] { 1 } },
            { PerkType.MastersMendFabrication, new[] { 1 } },
            { PerkType.SteadyHandFabrication, new[] { 1 } },
            { PerkType.MuscleMemoryFabrication, new[] { 1 } },
            { PerkType.VenerationFabrication, new[] { 1 } },
            { PerkType.WasteNotFabrication, new[] { 1 } },
            { PerkType.RapidSynthesisCooking, new[] { 1 } },
            { PerkType.CarefulSynthesisCooking, new[] { 1 } },
            { PerkType.BasicTouchCooking, new[] { 1 } },
            { PerkType.StandardTouchCooking, new[] { 1 } },
            { PerkType.PreciseTouchCooking, new[] { 1 } },
            { PerkType.MastersMendCooking, new[] { 1 } },
            { PerkType.SteadyHandCooking, new[] { 1 } },
            { PerkType.MuscleMemoryCooking, new[] { 1 } },
            { PerkType.VenerationCooking, new[] { 1 } },
            { PerkType.WasteNotCooking, new[] { 1 } },
            { PerkType.CookingRecipes, new[] { 1, 1, 2, 3, 3 } },
            { PerkType.RapidSynthesisEngineering, new[] { 1 } },
            { PerkType.CarefulSynthesisEngineering, new[] { 1 } },
            { PerkType.BasicTouchEngineering, new[] { 1 } },
            { PerkType.StandardTouchEngineering, new[] { 1 } },
            { PerkType.PreciseTouchEngineering, new[] { 1 } },
            { PerkType.MastersMendEngineering, new[] { 1 } },
            { PerkType.SteadyHandEngineering, new[] { 1 } },
            { PerkType.MuscleMemoryEngineering, new[] { 1 } },
            { PerkType.VenerationEngineering, new[] { 1 } },
            { PerkType.WasteNotEngineering, new[] { 1 } },
        };

        private static readonly Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> PlayerTrimmedPerks = new()
        {
            { PerkType.IonGrenade, (2, new[] { 2, 3, 3 }) },
            { PerkType.AdhesiveGrenade, (2, new[] { 2, 3, 3 }) },
            { PerkType.MedKit, (4, new[] { 1, 2, 3, 4, 4 }) },
            { PerkType.Resuscitation, (2, new[] { 4, 4, 4 }) },
            { PerkType.Shielding, (3, new[] { 2, 3, 3, 4 }) },
        };

        private static readonly Dictionary<PerkType, int[]> BeastRemovedPerks = new()
        {
            { PerkType.FlameBreath, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.ShockingSlash, new[] { 1, 1, 1, 2, 2 } },
            { PerkType.DiseasedTouch, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.Clip, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.SpinningClaw, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.BeastSpeed, new[] { 3, 3, 3 } },
            { PerkType.BolsterArmor, new[] { 1, 1, 1, 2, 2 } },
            { PerkType.PredatorRush, new[] { 4 } },
        };

        private static readonly Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> BeastTrimmedPerks = new()
        {
            { PerkType.Bite, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Claw, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.BolsterAttack, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Hasten, (2, new[] { 4, 4, 4 }) },
            { PerkType.PoisonBreath, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.IceBreath, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.EvasiveManeuver, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Assault, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.ForceTouch, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Innervate, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Anger, (2, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.FocusAttention, (3, new[] { 2, 2, 2, 3, 3 }) },
        };

        private static readonly HashSet<RecastGroup> ObsoleteRecastGroups = new()
        {
            (RecastGroup)1,
            (RecastGroup)2,
            (RecastGroup)3,
            (RecastGroup)4,
            (RecastGroup)5,
            (RecastGroup)6,
            (RecastGroup)7,
            (RecastGroup)8,
            (RecastGroup)9,
            (RecastGroup)10,
            (RecastGroup)11,
            (RecastGroup)12,
            (RecastGroup)13,
            (RecastGroup)14,
            (RecastGroup)15,
            (RecastGroup)25,
            (RecastGroup)26,
            (RecastGroup)27,
            (RecastGroup)28,
            (RecastGroup)29,
            (RecastGroup)30,
            (RecastGroup)31,
            (RecastGroup)32,
            (RecastGroup)33,
            (RecastGroup)34,
            (RecastGroup)35,
            (RecastGroup)36,
            (RecastGroup)39,
            (RecastGroup)40,
            (RecastGroup)41,
            (RecastGroup)42,
            (RecastGroup)43,
            (RecastGroup)44,
            (RecastGroup)45,
            (RecastGroup)46,
            (RecastGroup)47,
            (RecastGroup)48,
            (RecastGroup)49,
            (RecastGroup)61,
            (RecastGroup)64,
            (RecastGroup)65,
            (RecastGroup)66,
            (RecastGroup)67,
            (RecastGroup)68,
            (RecastGroup)70,
            (RecastGroup)71,
            (RecastGroup)72,
            (RecastGroup)79,
            (RecastGroup)80,
            (RecastGroup)81,
            (RecastGroup)82,
            (RecastGroup)83,
            (RecastGroup)84,
            (RecastGroup)85,
            (RecastGroup)86,
            (RecastGroup)87,
            (RecastGroup)88,
            (RecastGroup)89,
            (RecastGroup)90,
            (RecastGroup)91,
            (RecastGroup)92,
            (RecastGroup)93,
            (RecastGroup)94,
            (RecastGroup)95,
            (RecastGroup)96,
            (RecastGroup)97,
        };

        private static readonly IReadOnlyDictionary<string, ResistanceType> ResistanceKeyMap =
            new Dictionary<string, ResistanceType>
            {
                { ((int)ResistanceType.Fire).ToString(), ResistanceType.Fire },
                { ((int)ResistanceType.Poison).ToString(), ResistanceType.Poison },
                { ((int)ResistanceType.Electrical).ToString(), ResistanceType.Electrical },
                { ((int)ResistanceType.Ice).ToString(), ResistanceType.Ice },
                { "5", ResistanceType.Mind },
                { "6", ResistanceType.Mobility },
                { "7", ResistanceType.Trauma },
                { "8", ResistanceType.Disruption },
                { ((int)ResistanceType.Mind).ToString(), ResistanceType.Mind },
                { ((int)ResistanceType.Mobility).ToString(), ResistanceType.Mobility },
                { ((int)ResistanceType.Trauma).ToString(), ResistanceType.Trauma },
                { ((int)ResistanceType.Disruption).ToString(), ResistanceType.Disruption },
            };

        private const string SavingThrowPuritiesKey = "SavingThrowPurities";
        private const string SavingThrowWillKey = "Will";
        private const string SavingThrowReflexKey = "Reflex";
        private const string SavingThrowFortitudeKey = "Fortitude";
        private const string SavingThrowWillValue = "3";
        private const string SavingThrowReflexValue = "2";
        private const string SavingThrowFortitudeValue = "1";

        public int Version => 22;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            LogProgress("Starting consolidated server migration.");
            MigratePlayers();
            MigrateBeasts();
            MigrateIncubationJobs();
            StoredItemDataMigration.Migrate();
            LogProgress("Finished consolidated server migration.");
        }

        private static PerkType LegacyPerkType(int perkId)
        {
            return (PerkType)perkId;
        }

        private void MigratePlayers()
        {
            var dbQuery = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(dbQuery);
            var dbPlayersRaw = DB.SearchRawJson(dbQuery
                .AddPaging(playerCount, 0));
            var progress = new MigrationProgress("players", playerCount);
            progress.Begin();

            foreach (var dbPlayerJson in dbPlayersRaw)
            {
                var jObject = JObject.Parse(dbPlayerJson);
                var refundAmount = CalculateRefundAmount(jObject["Perks"] as JObject);
                WeaponBlueprintPerkMigration.CollapsePlayerPerks(jObject, out var weaponBlueprintRefundAmount);
                refundAmount += weaponBlueprintRefundAmount;
                DroidBoostRecipeMigration.ExpandPlayerRecipeDictionaries(jObject);
                NormalizeLegacyPerkKeys(jObject);
                SplitDefensesAndResistances(jObject);
                NormalizeResistanceDictionary(jObject, nameof(Player.Resistances));

                RemoveInvalidEnumDictionaryKeys<PerkType>(jObject["Perks"] as JObject);
                RemoveInvalidEnumDictionaryKeys<PerkType>(jObject["UnlockedPerks"] as JObject);
                RemoveInvalidEnumDictionaryKeys<RecastGroup>(jObject["RecastTimes"] as JObject);
                RemoveInvalidEnumDictionaryKeys<RecipeType>(jObject["UnlockedRecipes"] as JObject);
                RemoveInvalidEnumDictionaryKeys<RecipeType>(jObject["CraftedRecipes"] as JObject);
                RemoveInvalidSkillDictionaryKeys(jObject);

                var hasOriginalAppearanceType = jObject[nameof(Player.OriginalAppearanceType)] != null;
                var dbPlayer = jObject.ToObject<Player>();
                if (!hasOriginalAppearanceType)
                    dbPlayer.OriginalAppearanceType = AppearanceType.Invalid;

                EnsureDefinedPlayerSkills(dbPlayer);
                CombatReadinessMigration.ResetCombatReadiness(dbPlayer);
                EnsureUnknownDisplayName(dbPlayer);
                dbPlayer.RebuildComplete = false;

                refundAmount += CleanPerks(
                    dbPlayer.Perks,
                    PlayerRemovedPerks,
                    PlayerTrimmedPerks,
                    out _);
                RemoveUnlockedPerks(dbPlayer);
                RemoveRecastTimes(dbPlayer);

                if (refundAmount > 0)
                    dbPlayer.UnallocatedSP += refundAmount;

                DB.Set(dbPlayer);

                if (refundAmount > 0)
                    Log.Write(LogGroup.Migration, $"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP.");

                progress.RecordProcessed(true);
            }

            progress.Finish($"{playerCount} players migrated.");
        }

        private static void EnsureUnknownDisplayName(Player dbPlayer)
        {
            if (dbPlayer == null ||
                !string.IsNullOrWhiteSpace(PlayerName.SanitizeKnownName(dbPlayer.UnknownDisplayName)))
            {
                return;
            }

            var generatedDisplayName = PlayerDescriptor.GenerateUnknownDisplayName(dbPlayer);
            if (string.IsNullOrWhiteSpace(generatedDisplayName))
                return;

            dbPlayer.UnknownDisplayName = generatedDisplayName;
        }

        private static void MigrateBeasts()
        {
            var query = new DBQuery<Beast>();
            var count = (int)DB.SearchCount(query);
            var beasts = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;
            var totalRefund = 0;
            var progress = new MigrationProgress("beasts", count);
            progress.Begin();

            foreach (var rawBeast in beasts)
            {
                var jObject = JObject.Parse(rawBeast);
                var migrated = false;
                migrated |= AddResistancePurities(jObject);
                migrated |= jObject.Remove(SavingThrowPuritiesKey);
                migrated |= NormalizeResistanceDictionary(jObject, nameof(Beast.ResistancePurities));
                migrated |= MigratePurities(jObject);

                var beast = jObject.ToObject<Beast>();
                var refund = RefundBeastPerks(beast, out var perkChanged);
                migrated |= perkChanged;

                if (refund > 0)
                {
                    totalRefund += refund;
                    migrated = true;
                }

                if (!migrated)
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                DB.Set(beast);
                migratedCount++;
                progress.RecordProcessed(true);
            }

            Log.Write(LogGroup.Migration, $"Migration #22: Migrated beast combat data for {migratedCount} beasts and refunded {totalRefund} SP.", true);
            progress.Finish($"{migratedCount}/{count} beasts changed. Refunded {totalRefund} SP.");
        }

        private static void MigrateIncubationJobs()
        {
            var query = new DBQuery<IncubationJob>();
            var count = (int)DB.SearchCount(query);
            var jobs = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;
            var progress = new MigrationProgress("incubation jobs", count);
            progress.Begin();

            foreach (var rawJob in jobs)
            {
                var jObject = JObject.Parse(rawJob);
                var migrated = false;
                migrated |= AddResistancePurities(jObject);
                migrated |= jObject.Remove(SavingThrowPuritiesKey);
                migrated |= NormalizeResistanceDictionary(jObject, nameof(IncubationJob.ResistancePurities));
                migrated |= MigratePurities(jObject);

                if (!migrated)
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                var job = jObject.ToObject<IncubationJob>();
                DB.Set(job);
                migratedCount++;
                progress.RecordProcessed(true);
            }

            Log.Write(LogGroup.Migration, $"Migration #22: Migrated incubation job combat data for {migratedCount} jobs.", true);
            progress.Finish($"{migratedCount}/{count} incubation jobs changed.");
        }

        private static bool SplitDefensesAndResistances(JObject player)
        {
            var migrated = false;
            var defenses = player[nameof(Player.Defenses)] as JObject;
            var resistances = player[nameof(Player.Resistances)] as JObject;

            if (defenses == null)
            {
                defenses = new JObject();
                player[nameof(Player.Defenses)] = defenses;
                migrated = true;
            }

            if (resistances == null)
            {
                resistances = new JObject();
                player[nameof(Player.Resistances)] = resistances;
                migrated = true;
            }

            migrated |= MoveDefenseValue(defenses, resistances, CombatDamageType.Physical);
            migrated |= MoveDefenseValue(defenses, resistances, CombatDamageType.Force);

            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= MoveLegacyElementalDefense(defenses, resistances, CombatDamageType.Ice, ResistanceType.Ice);

            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= NormalizeLegacyElementalResistance(resistances, CombatDamageType.Ice, ResistanceType.Ice);

            migrated |= RemoveResistanceKeys(resistances, CombatDamageType.Physical);
            migrated |= RemoveResistanceKeys(resistances, CombatDamageType.Force);

            foreach (var type in Enum.GetValues(typeof(CombatDamageType)).Cast<CombatDamageType>())
            {
                if (!type.IsDefenseDamageType())
                    continue;

                migrated |= NormalizeDefenseValue(defenses, type);

                if (defenses[type.ToString()] != null)
                    continue;

                defenses[type.ToString()] = 0;
                migrated = true;
            }

            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                if (resistances[type.ToString()] != null)
                    continue;

                resistances[type.ToString()] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveDefenseValue(JObject defenses, JObject resistances, CombatDamageType type)
        {
            var migrated = false;
            var key = type.ToString();
            var resistanceToken = GetToken(resistances, key, (int)type);
            var defenseToken = GetToken(defenses, key, (int)type);

            if (defenseToken == null && resistanceToken != null)
            {
                defenses[key] = resistanceToken.DeepClone();
                migrated = true;
            }

            return migrated;
        }

        private static bool NormalizeDefenseValue(JObject defenses, CombatDamageType type)
        {
            var migrated = false;
            var key = type.ToString();
            var numericKey = ((int)type).ToString();
            var token = defenses[key] ?? defenses[numericKey];

            if (defenses[key] == null && token != null)
            {
                defenses[key] = token.DeepClone();
                migrated = true;
            }

            if (defenses.Remove(numericKey))
                migrated = true;

            return migrated;
        }

        private static bool MoveLegacyElementalDefense(
            JObject defenses,
            JObject resistances,
            CombatDamageType legacyType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var targetKey = resistanceType.ToString();
            var legacyNameKey = legacyType.ToString();
            var legacyNumericKey = ((int)legacyType).ToString();
            var legacyToken = GetToken(defenses, legacyType.ToString(), (int)legacyType);

            migrated |= MergeResistanceValue(resistances, targetKey, resistances[legacyNameKey]);
            migrated |= MergeResistanceValue(resistances, targetKey, resistances[legacyNumericKey]);
            migrated |= MergeResistanceValue(resistances, targetKey, legacyToken);

            foreach (var key in new[] { legacyNameKey, legacyNumericKey })
            {
                if (defenses.Remove(key))
                    migrated = true;
            }

            return migrated;
        }

        private static bool NormalizeLegacyElementalResistance(
            JObject resistances,
            CombatDamageType legacyType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var key = resistanceType.ToString();
            var legacyNumericKey = ((int)legacyType).ToString();

            migrated |= MergeResistanceValue(resistances, key, resistances[legacyType.ToString()]);
            migrated |= MergeResistanceValue(resistances, key, resistances[legacyNumericKey]);

            if (resistances.Remove(legacyNumericKey))
                migrated = true;

            return migrated;
        }

        private static bool RemoveResistanceKeys(JObject resistances, CombatDamageType type)
        {
            var migrated = false;

            foreach (var key in new[] { type.ToString(), ((int)type).ToString() })
            {
                if (resistances.Remove(key))
                    migrated = true;
            }

            return migrated;
        }

        private static bool AddResistancePurities(JObject entity)
        {
            var migrated = false;
            var defensePurities = entity[nameof(Beast.DefensePurities)] as JObject;
            var savingThrowPurities = entity[SavingThrowPuritiesKey] as JObject;
            var resistancePurities = entity[nameof(Beast.ResistancePurities)] as JObject;

            if (resistancePurities == null)
            {
                resistancePurities = new JObject();
                entity[nameof(Beast.ResistancePurities)] = resistancePurities;
                migrated = true;
            }

            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Fire, GetToken(defensePurities, CombatDamageType.Fire));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Poison, GetToken(defensePurities, CombatDamageType.Poison));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Electrical, GetToken(defensePurities, CombatDamageType.Electrical));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Ice, GetToken(defensePurities, CombatDamageType.Ice));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Mind, GetToken(savingThrowPurities, SavingThrowWillKey, SavingThrowWillValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Mobility, GetToken(savingThrowPurities, SavingThrowReflexKey, SavingThrowReflexValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Trauma, GetToken(savingThrowPurities, SavingThrowFortitudeKey, SavingThrowFortitudeValue));
            migrated |= AddResistancePurity(resistancePurities, ResistanceType.Disruption, GetToken(defensePurities, CombatDamageType.Force));

            return migrated;
        }

        private static bool AddResistancePurity(JObject resistancePurities, ResistanceType type, JToken legacyToken)
        {
            var key = type.ToString();
            if (resistancePurities[key] != null)
                return false;

            resistancePurities[key] = legacyToken?.DeepClone() ?? 0;
            return true;
        }

        private static bool NormalizeResistanceDictionary(JObject entity, string propertyName)
        {
            var migrated = false;
            var resistances = entity[propertyName] as JObject;

            if (resistances == null)
            {
                resistances = new JObject();
                entity[propertyName] = resistances;
                migrated = true;
            }

            foreach (var pair in ResistanceKeyMap)
            {
                migrated |= MoveResistanceValue(resistances, pair.Key, pair.Value.ToString());
            }

            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                var key = type.ToString();
                if (resistances[key] != null)
                    continue;

                resistances[key] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveResistanceValue(JObject resistances, string sourceKey, string targetKey)
        {
            if (sourceKey == targetKey)
                return false;

            var sourceToken = resistances[sourceKey];
            if (sourceToken == null)
                return false;

            MergeResistanceValue(resistances, targetKey, sourceToken);

            resistances.Remove(sourceKey);
            return true;
        }

        private static bool MergeResistanceValue(JObject resistances, string targetKey, JToken sourceToken)
        {
            if (sourceToken == null)
                return false;

            var sourceValue = GetInt(sourceToken);
            var targetToken = resistances[targetKey];
            if (targetToken == null)
            {
                resistances[targetKey] = sourceToken.DeepClone();
                return true;
            }

            var targetValue = GetInt(targetToken);
            var mergedValue = MergeResistanceValues(targetValue, sourceValue);
            if (targetValue == mergedValue)
                return false;

            resistances[targetKey] = mergedValue;
            return true;
        }

        private static int MergeResistanceValues(int targetValue, int sourceValue)
        {
            if (targetValue == 0)
                return sourceValue;

            if (sourceValue == 0)
                return targetValue;

            return Math.Max(targetValue, sourceValue);
        }

        private static bool MigratePurities(JObject entity)
        {
            var migrated = false;
            var defensePurities = GetOrCreateObject(entity, nameof(Beast.DefensePurities), ref migrated);
            var resistancePurities = GetOrCreateObject(entity, nameof(Beast.ResistancePurities), ref migrated);

            migrated |= NormalizeDefensePurity(defensePurities, CombatDamageType.Physical);
            migrated |= NormalizeDefensePurity(defensePurities, CombatDamageType.Force);

            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Fire, ResistanceType.Fire);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Poison, ResistanceType.Poison);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Electrical, ResistanceType.Electrical);
            migrated |= MoveDefensePurityToResistance(defensePurities, resistancePurities, CombatDamageType.Ice, ResistanceType.Ice);

            foreach (var resistanceType in Resistance.GetAllResistanceTypes())
            {
                migrated |= NormalizeResistancePurity(resistancePurities, resistanceType);
            }

            return migrated;
        }

        private static JObject GetOrCreateObject(JObject entity, string propertyName, ref bool migrated)
        {
            if (entity[propertyName] is JObject existing)
                return existing;

            var created = new JObject();
            entity[propertyName] = created;
            migrated = true;

            return created;
        }

        private static bool NormalizeDefensePurity(JObject defensePurities, CombatDamageType type)
        {
            var migrated = false;
            var targetKey = type.ToString();
            var numericKey = ((int)type).ToString();
            var targetToken = defensePurities[targetKey];
            var numericToken = defensePurities[numericKey];

            if (targetToken == null && numericToken != null)
            {
                defensePurities[targetKey] = numericToken.DeepClone();
                migrated = true;
            }
            else if (targetToken != null && numericToken != null)
            {
                defensePurities[targetKey] = Math.Max(GetInt(targetToken), GetInt(numericToken));
                migrated = true;
            }

            if (numericToken != null)
            {
                defensePurities.Remove(numericKey);
                migrated = true;
            }

            if (defensePurities[targetKey] == null)
            {
                defensePurities[targetKey] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveDefensePurityToResistance(
            JObject defensePurities,
            JObject resistancePurities,
            CombatDamageType damageType,
            ResistanceType resistanceType)
        {
            var migrated = false;
            var nameKey = damageType.ToString();
            var numericKey = ((int)damageType).ToString();
            var sourceToken = defensePurities[nameKey] ?? defensePurities[numericKey];

            if (sourceToken != null)
            {
                migrated |= MergeResistancePurity(resistancePurities, resistanceType, GetInt(sourceToken));
            }

            if (defensePurities.Remove(nameKey))
                migrated = true;

            if (defensePurities.Remove(numericKey))
                migrated = true;

            return migrated;
        }

        private static bool NormalizeResistancePurity(JObject resistancePurities, ResistanceType type)
        {
            var migrated = false;
            var targetKey = type.ToString();
            var numericKey = ((int)type).ToString();
            var targetToken = resistancePurities[targetKey];
            var numericToken = resistancePurities[numericKey];

            if (targetToken == null && numericToken != null)
            {
                resistancePurities[targetKey] = numericToken.DeepClone();
                migrated = true;
            }
            else if (targetToken != null && numericToken != null)
            {
                resistancePurities[targetKey] = Math.Max(GetInt(targetToken), GetInt(numericToken));
                migrated = true;
            }

            if (numericToken != null)
            {
                resistancePurities.Remove(numericKey);
                migrated = true;
            }

            if (resistancePurities[targetKey] == null)
            {
                resistancePurities[targetKey] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MergeResistancePurity(JObject resistancePurities, ResistanceType type, int value)
        {
            var key = type.ToString();
            var existingValue = GetInt(resistancePurities[key]);
            var newValue = Math.Max(existingValue, value);

            if (resistancePurities[key] != null && existingValue == newValue)
                return false;

            resistancePurities[key] = newValue;
            return true;
        }

        private static int CleanPerks(
            Dictionary<PerkType, int> perks,
            Dictionary<PerkType, int[]> removedPerks,
            Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> trimmedPerks,
            out bool changed)
        {
            changed = false;
            if (perks == null)
                return 0;

            var refund = 0;

            foreach (var (perkType, pricesByLevel) in removedPerks)
            {
                if (!perks.TryGetValue(perkType, out var purchasedLevel))
                    continue;

                refund += CalculateRefund(pricesByLevel, 1, purchasedLevel);
                perks.Remove(perkType);
                changed = true;
            }

            foreach (var (perkType, trim) in trimmedPerks)
            {
                if (!perks.TryGetValue(perkType, out var purchasedLevel) ||
                    purchasedLevel <= trim.MaxLevel)
                    continue;

                refund += CalculateRefund(trim.PricesByLevel, trim.MaxLevel + 1, purchasedLevel);
                perks[perkType] = trim.MaxLevel;
                changed = true;
            }

            return refund;
        }

        private static int RefundBeastPerks(Beast beast, out bool changed)
        {
            changed = false;
            if (beast == null)
                return 0;

            var totalSkillPoints = Math.Clamp(beast.Level, 0, BeastMastery.MaxLevel);
            var refund = Math.Max(0, totalSkillPoints - beast.UnallocatedSP);

            if (beast.Perks == null)
            {
                beast.Perks = new Dictionary<PerkType, int>();
                changed = true;
            }
            else if (beast.Perks.Count > 0)
            {
                beast.Perks.Clear();
                changed = true;
            }

            if (beast.UnallocatedSP != totalSkillPoints)
            {
                beast.UnallocatedSP = totalSkillPoints;
                changed = true;
            }

            return refund;
        }

        private static int CalculateRefund(int[] pricesByLevel, int fromLevel, int purchasedLevel)
        {
            var refund = 0;
            var maxLevel = purchasedLevel > pricesByLevel.Length
                ? pricesByLevel.Length
                : purchasedLevel;

            for (var level = fromLevel; level <= maxLevel; level++)
            {
                refund += pricesByLevel[level - 1];
            }

            return refund;
        }

        private static bool RemoveUnlockedPerks(Player player)
        {
            if (player.UnlockedPerks == null)
                return false;

            var changed = false;
            foreach (var perkType in PlayerRemovedPerks.Keys)
            {
                changed |= player.UnlockedPerks.Remove(perkType);
            }

            return changed;
        }

        private static bool RemoveRecastTimes(Player player)
        {
            if (player.RecastTimes == null)
                return false;

            var changed = false;
            foreach (var recastGroup in ObsoleteRecastGroups)
            {
                changed |= player.RecastTimes.Remove(recastGroup);
            }

            return changed;
        }

        private static JToken GetToken(JObject obj, string name, int value)
        {
            return obj?[name] ?? obj?[value.ToString()];
        }

        private static JToken GetToken(JObject obj, CombatDamageType type)
        {
            return obj?[type.ToString()] ?? obj?[((int)type).ToString()];
        }

        private static JToken GetToken(JObject obj, string nameKey, string numericKey)
        {
            return obj?[nameKey] ?? obj?[numericKey];
        }

        private static int GetInt(JToken token)
        {
            return int.TryParse(token?.ToString(), out var value)
                ? value
                : 0;
        }

        private static void LogProgress(string message)
        {
            Log.Write(LogGroup.Migration, $"Migration #22: {message}", true);
        }

        private sealed class MigrationProgress
        {
            private const int PercentReportStep = 5;
            private const int RecordReportStep = 500;

            private readonly string _sectionName;
            private readonly int _totalCount;
            private int _nextPercentReport = PercentReportStep;
            private int _lastRecordReport;

            private int ProcessedCount { get; set; }
            private int ChangedCount { get; set; }

            public MigrationProgress(string sectionName, int totalCount)
            {
                _sectionName = sectionName;
                _totalCount = totalCount;
            }

            public void Begin()
            {
                LogProgress($"Scanning {_sectionName} ({_totalCount} records). {BuildProgressText()}");
            }

            public void RecordProcessed(bool changed)
            {
                ProcessedCount++;

                if (changed)
                    ChangedCount++;

                if (ShouldReportProgress())
                    ReportProgress();
            }

            public void Finish(string details)
            {
                LogProgress($"Finished {_sectionName}. {details} {BuildProgressText()}");
            }

            private bool ShouldReportProgress()
            {
                if (_totalCount <= 0)
                    return false;

                var percent = GetPercent();
                if (percent >= _nextPercentReport)
                {
                    while (_nextPercentReport <= percent)
                    {
                        _nextPercentReport += PercentReportStep;
                    }

                    return true;
                }

                if (ProcessedCount - _lastRecordReport < RecordReportStep)
                    return false;

                _lastRecordReport = ProcessedCount;
                return true;
            }

            private void ReportProgress()
            {
                _lastRecordReport = ProcessedCount;
                LogProgress(BuildProgressText());
            }

            private string BuildProgressText()
            {
                return _totalCount <= 0
                    ? $"Current migration progress: {_sectionName} 0/0 records (100.0%), 0 changed."
                    : $"Current migration progress: {_sectionName} {ProcessedCount}/{_totalCount} records ({GetPercent():0.0}%), {ChangedCount} changed.";
            }

            private double GetPercent()
            {
                return _totalCount <= 0
                    ? 100.0
                    : ProcessedCount * 100.0 / _totalCount;
            }
        }

        private int CalculateRefundAmount(JObject perks)
        {
            if (perks == null)
                return 0;

            var refundAmount = 0;

            foreach (var ((type, level), sp) in _refundMap)
            {
                var perkId = (int)type;

                foreach (var property in perks.Properties())
                {
                    if (!TryGetLegacyPerkId(property.Name, out var playerPerkId) ||
                        playerPerkId != perkId)
                        continue;

                    var playerPerkLevel = property.Value.Value<int>();
                    if (playerPerkLevel >= level)
                        refundAmount += sp;
                }
            }

            return refundAmount;
        }

        private void NormalizeLegacyPerkKeys(JObject player)
        {
            var perks = player["Perks"] as JObject;
            var unlockedPerks = player["UnlockedPerks"] as JObject;

            PreserveLegacyPerkRank(perks, LegacyFlurryStylePerkId.ToString(), CurrentFlurryStyleKey);
            PreserveLegacyDictionaryValue(unlockedPerks, LegacyFlurryStylePerkId.ToString(), CurrentFlurryStyleKey);

            RemoveRefundedLegacyPerkKeys(perks);
            RemoveRefundedLegacyPerkKeys(unlockedPerks);
        }

        private static void PreserveLegacyPerkRank(JObject perks, string legacyKey, string currentKey)
        {
            if (perks == null)
                return;

            var legacyToken = perks[legacyKey];
            if (legacyToken == null)
                return;

            if (perks[currentKey] == null)
            {
                perks[currentKey] = legacyToken.DeepClone();
            }
            else
            {
                perks[currentKey] = Math.Max(perks[currentKey].Value<int>(), legacyToken.Value<int>());
            }

            perks.Remove(legacyKey);
        }

        private static void PreserveLegacyDictionaryValue(JObject dictionary, string legacyKey, string currentKey)
        {
            if (dictionary == null)
                return;

            var legacyToken = dictionary[legacyKey];
            if (legacyToken == null)
                return;

            if (dictionary[currentKey] == null)
                dictionary[currentKey] = legacyToken.DeepClone();

            dictionary.Remove(legacyKey);
        }

        private void RemoveRefundedLegacyPerkKeys(JObject dictionary)
        {
            if (dictionary == null)
                return;

            var refundedLegacyIds = _refundMap.Keys
                .Select(key => (int)key.Item1)
                .Distinct()
                .ToHashSet();

            foreach (var legacyId in refundedLegacyIds)
            {
                dictionary.Remove(legacyId.ToString());
            }

            foreach (var (legacyName, legacyId) in _legacyPerkIdByName)
            {
                if (refundedLegacyIds.Contains(legacyId))
                    dictionary.Remove(legacyName);
            }
        }

        private bool TryGetLegacyPerkId(string key, out int perkId)
        {
            if (int.TryParse(key, out perkId))
                return true;

            if (_legacyPerkIdByName.TryGetValue(key, out perkId))
                return true;

            if (Enum.TryParse(key, out PerkType currentPerkType) &&
                Enum.IsDefined(typeof(PerkType), currentPerkType))
            {
                perkId = (int)currentPerkType;
                return true;
            }

            return false;
        }

        private static void RemoveInvalidEnumDictionaryKeys<TEnum>(JObject dictionary)
            where TEnum : struct, Enum
        {
            if (dictionary == null)
                return;

            var keysToRemove = new List<JProperty>();

            foreach (var property in dictionary.Properties())
            {
                if (Enum.TryParse(property.Name, out TEnum value) &&
                    Enum.IsDefined(typeof(TEnum), value))
                {
                    continue;
                }

                keysToRemove.Add(property);
            }

            foreach (var property in keysToRemove)
            {
                property.Remove();
            }
        }

        private static void RemoveInvalidSkillDictionaryKeys(JObject player)
        {
            RemoveInvalidEnumDictionaryKeys<SkillType>(player["Skills"] as JObject);
            RemoveInvalidEnumDictionaryKeys<SkillType>(player["Control"] as JObject);
            RemoveInvalidEnumDictionaryKeys<SkillType>(player["Craftsmanship"] as JObject);
            RemoveInvalidEnumDictionaryKeys<SkillType>(player["CPBonus"] as JObject);

            if (player["Settings"] is JObject settings)
                RemoveInvalidEnumDictionaryKeys<SkillType>(settings["LanguageChatColors"] as JObject);
        }

        private static void EnsureDefinedPlayerSkills(Player dbPlayer)
        {
            dbPlayer.Skills ??= new Dictionary<SkillType, PlayerSkill>();
            dbPlayer.Control ??= new Dictionary<SkillType, int>();
            dbPlayer.Craftsmanship ??= new Dictionary<SkillType, int>();
            dbPlayer.CPBonus ??= new Dictionary<SkillType, int>();
            dbPlayer.Settings ??= new PlayerSettings();
            dbPlayer.Settings.LanguageChatColors ??= new Dictionary<SkillType, PlayerColor>();

            foreach (SkillType skillType in Enum.GetValues(typeof(SkillType)))
            {
                if (skillType == SkillType.Invalid)
                    continue;

                dbPlayer.Skills.TryAdd(skillType, new PlayerSkill());
            }
        }
    }
}

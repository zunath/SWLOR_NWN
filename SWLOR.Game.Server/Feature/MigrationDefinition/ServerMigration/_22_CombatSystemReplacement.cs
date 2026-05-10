using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.CraftService;

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

        public int Version => 22;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            MigratePlayers();
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

            foreach (var dbPlayerJson in dbPlayersRaw)
            {
                var jObject = JObject.Parse(dbPlayerJson);
                var refundAmount = CalculateRefundAmount(jObject["Perks"] as JObject);
                WeaponBlueprintPerkMigration.CollapsePlayerPerks(jObject, out var weaponBlueprintRefundAmount);
                refundAmount += weaponBlueprintRefundAmount;
                DroidBoostRecipeMigration.ExpandPlayerRecipeDictionaries(jObject);
                NormalizeLegacyPerkKeys(jObject);

                RemoveInvalidEnumDictionaryKeys<PerkType>(jObject["Perks"] as JObject);
                RemoveInvalidEnumDictionaryKeys<PerkType>(jObject["UnlockedPerks"] as JObject);
                RemoveInvalidEnumDictionaryKeys<RecastGroup>(jObject["RecastTimes"] as JObject);
                RemoveInvalidEnumDictionaryKeys<RecipeType>(jObject["UnlockedRecipes"] as JObject);
                RemoveInvalidEnumDictionaryKeys<RecipeType>(jObject["CraftedRecipes"] as JObject);
                RemoveInvalidSkillDictionaryKeys(jObject);

                var dbPlayer = jObject.ToObject<Player>();
                EnsureDefinedPlayerSkills(dbPlayer);
                dbPlayer.RebuildComplete = false;

                if (refundAmount > 0)
                    dbPlayer.UnallocatedSP += refundAmount;

                DB.Set(dbPlayer);

                if (refundAmount > 0)
                    Log.Write(LogGroup.Migration, $"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP.");
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

using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static class Combat
    {
        private const float DamageStatDeltaMultiplier = 0.35f;
        public const int BaseGuardDamageReductionPercent = 20;
        public const int MaximumGuardDamageReductionPercent = 55;
        public const int MaximumNormalDamageReductionPercent = 95;
        public const int MaximumDamageBonusPercent = 100;
        private const int MaximumCrossResourceRestorePercent = 95;
        public const int MaximumCombinedDamageReductionPercent = 85;
        public const int MaximumAttackDelayAdjustmentPercent = 50;
        public const int BaseHitRate = 75;
        public const int MinimumHitRate = 20;
        public const int MaximumHitRate = 95;
        public const int MinimumCriticalRate = 5;
        public const int MaximumCriticalRate = 50;
        public const int MaximumDamageDerivedHealingPercentPerHit = 50;
        public const int MaximumCriticalDamagePercentAdjustment = 200;

        public const int StandardCriticalRating = 2;
        public const int BaseAttackDelayMilliseconds = 1750;
        public const float MeleeWeaponEngagementRange = 1.5f;
        public const float RangedWeaponEngagementRange = 10f;

        // The engine/client cannot play swing animations faster than the base attack delay.
        // Delays below it are honored by resolving multiple attack rolls within a single swing,
        // mirroring the stock engine's flurry behavior for high attacks-per-round builds.
        public const int MaxAttacksPerSwing = 3;
        public const int MinimumAttackDelayMilliseconds =
            (BaseAttackDelayMilliseconds + MaxAttacksPerSwing - 1) / MaxAttacksPerSwing;

        private const int AttackDelayUnitsPerSecond = 60;
        private const int MillisecondsPerSecond = 1000;
        private const int BaseAttackDelayUnits = BaseAttackDelayMilliseconds * AttackDelayUnitsPerSecond / MillisecondsPerSecond;

        private static readonly List<CombatDamageType> _allValidDamageTypes = new();
        private static readonly List<CombatDamageType> _allDefenseDamageTypes = new();
        private static readonly Dictionary<(uint, StatType), DateTime> _statTriggerCooldowns = new();
        private static readonly Dictionary<uint, DamageDerivedHealingState> _damageDerivedHealingStates = new();
        private static readonly Dictionary<(uint, uint), DateTime> _recentDamageTargets = new();
        private static readonly Dictionary<uint, DateTime> _recentDamageTaken = new();
        private static readonly Dictionary<uint, DateTime> _recentGuardedHits = new();
        private static readonly Dictionary<(uint Creature, DeflectionSource Source), DateTime> _recentDeflections = new();
        private static readonly Dictionary<uint, DateTime> _lastCombatActivity = new();
        private static readonly Dictionary<uint, DateTime> _lastHostileAbilityAttemptActivity = new();
        private static readonly Dictionary<uint, DateTime> _lastHostileIncomingActivity = new();
        private static readonly HashSet<uint> _firstCombatAttackConsumed = new();
        private static readonly Dictionary<uint, DateTime> _lastAttackActivity = new();
        private static readonly Dictionary<uint, DateTime> _lastCombatAbilityUse = new();
        private static readonly Dictionary<(uint, uint), SuppressionAbilityUseState> _pendingSuppressionAbilityUses = new();
        private static readonly Dictionary<uint, HostileAbilitySequenceState> _hostileAbilitySequenceStates = new();
        private static readonly Dictionary<uint, CriticalHitSequenceState> _criticalHitSequenceStates = new();
        private static readonly Dictionary<(uint, uint), int> _sameTargetHostileAbilityHitCounts = new();
        private static readonly Dictionary<uint, int> _autoAttackCycleCounts = new();
        private static readonly Dictionary<uint, int> _autoAttackCycleCriticalCounts = new();
        private static readonly Dictionary<uint, TargetHitSequenceState> _firstHostileAbilityHitCounts = new();
        private static readonly Dictionary<(uint, uint, StatusEffectCategory), int> _sourceStatusAutoAttackCycleCounts = new();
        private static readonly Dictionary<uint, DateTime> _stealthOpeningWindows = new();
        private static readonly Dictionary<(uint, uint), TargetHitSequenceState> _areaAbilityTargetHitSequences = new();
        private static readonly Dictionary<uint, float> _attackSwingDebts = new();
        private static readonly Dictionary<uint, float> _attackSwingDebtsWithoutLimitedReduction = new();
        private static readonly Dictionary<uint, RepeatedTargetDamageState> _repeatedTargetDamageStates = new();
        private static readonly Dictionary<uint, RepeatedTargetDamageState> _meleeRepeatedTargetDamageStates = new();
        private static readonly Dictionary<uint, RepeatedTargetDamageState> _rangedRepeatedTargetDamageStates = new();
        private static readonly Dictionary<uint, int> _meleeAutoAttackCycleCounts = new();
        private static readonly Dictionary<uint, SameTargetPressureState> _sameTargetPressureStates = new();
        private static readonly Dictionary<(uint Creature, AbilityDetail Ability), AbilityStaminaCostState> _abilityStaminaCosts = new();
        private static bool _damageTypesCached;

        private sealed class HostileAbilitySequenceState
        {
            public FeatType LastFeat { get; init; }
            public DateTime LastUse { get; init; }
        }

        private sealed class CriticalHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
        }

        private sealed class TargetHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
            public DateTime? RechargeAvailableAt { get; init; }
        }

        private sealed class SameTargetPressureState
        {
            public uint Target { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime LastBuildHitAt { get; set; }
            public DateTime ReadyUntil { get; set; }
        }

        private sealed class AbilityStaminaCostState
        {
            public int Cost { get; init; }
            public DateTime SpentAt { get; init; }
            public bool StaminaRestoreApplied { get; set; }
            public int DeferredImpactCount { get; set; }
        }

        private sealed class SuppressionAbilityUseState
        {
            public DateTime Expiration { get; init; }
            public HashSet<string> SuppressionEffectIds { get; init; } = new();
        }

        /// <summary>
        /// Cache all valid character and defense damage types before module load.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadDamageTypes()
        {
            _allValidDamageTypes.Clear();
            _allDefenseDamageTypes.Clear();

            var allValues = Enum.GetValues(typeof(CombatDamageType)).Cast<CombatDamageType>();

            foreach (var type in allValues)
            {
                if (type.IsCharacterDamageType())
                    _allValidDamageTypes.Add(type);

                if (type.IsDefenseDamageType())
                    _allDefenseDamageTypes.Add(type);
            }

            _damageTypesCached = true;
        }

        /// <summary>
        /// When a player enters the server, apply any defense and resistance entries they don't already have.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void AddDamageTypeResistances()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var foundNewType = false;
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            if (dbPlayer.Defenses == null)
            {
                foundNewType = true;
                dbPlayer.Defenses = CreateDefaultDefenseValues();
            }

            if (dbPlayer.Resistances == null)
            {
                foundNewType = true;
                dbPlayer.Resistances = Resistance.CreateDefaultResistanceValues();
            }

            foundNewType |= EnsureDefenseValues(dbPlayer.Defenses);

            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                if (!dbPlayer.Resistances.ContainsKey(type))
                {
                    foundNewType = true;
                    dbPlayer.Resistances[type] = 0;
                }
            }

            if (foundNewType)
            {
                DB.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        public static IReadOnlyList<CombatDamageType> GetAllDamageTypes()
        {
            EnsureDamageTypesCached();
            return _allValidDamageTypes;
        }

        /// <summary>
        /// Retrieves all damage types which use a defense rating.
        /// </summary>
        /// <returns>A list of defense damage types</returns>
        public static IReadOnlyList<CombatDamageType> GetDefenseDamageTypes()
        {
            EnsureDamageTypesCached();
            return _allDefenseDamageTypes;
        }

        public static Dictionary<CombatDamageType, int> CreateDefaultDefenseValues(int defaultValue = 0)
        {
            return GetDefenseDamageTypes()
                .ToDictionary(type => type, _ => defaultValue);
        }

        public static bool EnsureDefenseValues(Dictionary<CombatDamageType, int> defenses, int defaultValue = 0)
        {
            if (defenses == null)
                throw new ArgumentNullException(nameof(defenses));

            var foundNewType = false;
            foreach (var type in GetDefenseDamageTypes())
            {
                if (defenses.ContainsKey(type))
                    continue;

                defenses[type] = defaultValue;
                foundNewType = true;
            }

            return foundNewType;
        }

        private static void EnsureDamageTypesCached()
        {
            if (!_damageTypesCached)
            {
                LoadDamageTypes();
            }
        }

        /// <summary>
        /// Calculates the minimum and maximum damage possible with the provided stats.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's raw defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A minimum and maximum damage range</returns>
        public static (int, int) CalculateDamageRange(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            const float RatioMax = 3.625f;
            const float RatioMin = 0.01f;

            if (defenderDefense < 1)
                defenderDefense = 1;

            var statDelta = (attackerStat - defenderStat) * DamageStatDeltaMultiplier;
            if (deltaCap > 0) statDelta = Math.Clamp(statDelta, -deltaCap, 8 + deltaCap);
            var baseDamage = attackerDMG + statDelta;
            var ratio = (float)attackerAttack / (float)defenderDefense;

            if (ratio > RatioMax)
                ratio = RatioMax;
            else if (ratio < RatioMin)
                ratio = RatioMin;

            var maxDamage = baseDamage * ratio;
            var minDamage = maxDamage * 0.70f;

            Log.Write(LogGroup.Attack, $"attackerAttack = {attackerAttack}, attackerDMG = {attackerDMG}, attackerStat = {attackerStat}, defenderDefense = {defenderDefense}, defenderStat = {defenderStat}, critical = {critical}");
            Log.Write(LogGroup.Attack, $"statDelta = {statDelta}, baseDamage = {baseDamage}, ratio = {ratio}, minDamage = {minDamage}, maxDamage = {maxDamage}");

            // Criticals - 25% bonus to damage range per multiplier point.
            if (critical > 0)
            {
                minDamage = maxDamage;
                maxDamage *= ((critical - 1) / 4.0f) + 1.0f;
                Log.Write(LogGroup.Attack, $"Critical Rating: {critical}, minDamage = {minDamage}, maxDamage = {maxDamage}");
            }

            var roundedMinDamage = (int)minDamage;
            var roundedMaxDamage = (int)maxDamage;
            if (attackerDMG > 0)
            {
                roundedMinDamage = Math.Max(1, roundedMinDamage);
                roundedMaxDamage = Math.Max(roundedMinDamage, roundedMaxDamage);
            }

            return (roundedMinDamage, roundedMaxDamage);
        }

        /// <summary>
        /// Calculates the hit rate against a given target.
        /// Range is clamped to values between 20 and 95, inclusive.
        /// </summary>
        /// <param name="attackerAccuracy">The total accuracy of the attacker.</param>
        /// <param name="defenderEvasion">The total evasion of the defender.</param>
        /// <param name="percentageModifier">Modifies the raw hit change by a certain percentage. This is done after all prior calculations.</param>
        /// <returns>The hit rate, clamped between 20 and 95, inclusive.</returns>
        public static int CalculateHitRate(
            int attackerAccuracy,
            int defenderEvasion,
            int percentageModifier)
        {
            var hitRate = BaseHitRate + (int)Math.Floor((attackerAccuracy - defenderEvasion) / 2.0f) + percentageModifier;

            if (hitRate < MinimumHitRate)
                hitRate = MinimumHitRate;
            else if (hitRate > MaximumHitRate)
                hitRate = MaximumHitRate;

            return hitRate;
        }

        /// <summary>
        /// Calculates the critical hit rate against a given target.
        /// </summary>
        /// <param name="attackerPER">The attacker's perception stat.</param>
        /// <param name="defenderVIT">The defender's vitality stat.</param>
        /// <param name="skillRank">The attacker's relevant weapon skill rank.</param>
        /// <param name="criticalModifier">A modifier to the critical rating based on external factors.</param>
        /// <returns>The critical rate, in a percentage</returns>
        public static int CalculateCriticalRate(int attackerPER, int defenderVIT, int skillRank, int criticalModifier)
        {
            var skillBonus = Math.Max(0, skillRank / 10);
            var statBonus = Math.Clamp((int)Math.Floor((attackerPER - defenderVIT) / 5.0f), 0, 3);

            var criticalRate = MinimumCriticalRate + skillBonus + statBonus + criticalModifier;
            if (criticalRate < MinimumCriticalRate)
                criticalRate = MinimumCriticalRate;
            else if (criticalRate > MaximumCriticalRate)
                criticalRate = MaximumCriticalRate;


            return criticalRate;
        }

        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's raw defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A damage value to apply to the target.</returns>
        public static int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var (minDamage, maxDamage) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            return (int)Random.NextFloat(minDamage, maxDamage);
        }

        public static (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(
            uint defender,
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var isDefenderValid = GetIsObjectValid(defender);
            var usedPendingCriticalDowngrade = isDefenderValid &&
                TemporaryStatModifier.Consume(
                    defender,
                    StatType.CurrentIncomingAttackMinimumDamage,
                    StatType.CurrentIncomingAttackMinimumDamage) > 0;
            var wasCriticalDowngraded = !usedPendingCriticalDowngrade &&
                TryUseIncomingCriticalHitDowngrade(defender, critical);
            var forceMinimumNormalDamage = wasCriticalDowngraded || usedPendingCriticalDowngrade;
            var effectiveCritical = forceMinimumNormalDamage ? 0 : critical;
            var (minDamage, maxDamage) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                effectiveCritical,
                deltaCap);
            var damage = forceMinimumNormalDamage
                ? minDamage
                : (int)Random.NextFloat(minDamage, maxDamage);

            return (damage, effectiveCritical, wasCriticalDowngraded || usedPendingCriticalDowngrade);
        }

        internal static bool TryUseIncomingCriticalHitDowngrade(uint defender, int criticalRating)
        {
            if (!GetIsObjectValid(defender) ||
                criticalRating <= 0 ||
                Stat.GetStatAdjustment(defender, StatType.IncomingCriticalHitDowngradeToMinimumDamage) <= 0)
            {
                return false;
            }

            var cooldownMilliseconds = Stat.GetStatAdjustment(
                defender,
                StatType.IncomingCriticalHitDowngradeCooldownMilliseconds);
            return TryUseStatTrigger(
                defender,
                StatType.IncomingCriticalHitDowngradeToMinimumDamage,
                TimeSpan.FromMilliseconds(cooldownMilliseconds));
        }

        public static int ApplyCriticalDamageModifier(
            uint attacker,
            int damage,
            int criticalRating,
            SkillType skillType = SkillType.Invalid,
            uint defender = OBJECT_INVALID,
            int criticalDamagePercentAdjustment = 0)
        {
            if (criticalRating <= 0 || damage <= 0)
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamagePercentAdjustment);
            adjustment += GetSkillCriticalDamagePercentAdjustment(attacker, skillType);
            adjustment += GetHighHPTargetCriticalDamageAdjustment(attacker, defender);
            adjustment += GetTargetStatusCriticalDamageAdjustment(attacker, defender);
            adjustment += criticalDamagePercentAdjustment;
            adjustment = ClampCriticalDamagePercentAdjustment(adjustment);
            if (adjustment == 0)
                return damage;

            return damage + damage * adjustment / 100;
        }

        public static int ClampCriticalDamagePercentAdjustment(int adjustment)
        {
            return Math.Min(adjustment, MaximumCriticalDamagePercentAdjustment);
        }

        private static int GetHighHPTargetCriticalDamageAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var threshold = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageHighHPTargetThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageHighHPTargetPercentAdjustment);
            var maximumHP = GetMaxHitPoints(defender);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            if (GetCurrentHitPoints(defender) < maximumHP * (threshold / 100f))
                return 0;

            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"High Noon +{adjustment}% critical damage"),
                    attacker,
                    false);
            }

            return adjustment;
        }

        private static int GetTargetStatusCriticalDamageAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.CriticalDamageTargetStatusCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageTargetStatusPercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

        private static int GetSkillCriticalDamagePercentAdjustment(uint attacker, SkillType skillType)
        {
            if (IsRangedWeaponSkill(skillType))
                return Stat.GetStatAdjustment(attacker, StatType.RangedCriticalDamagePercentAdjustment);

            return skillType switch
            {
                SkillType.Staff => Stat.GetStatAdjustment(attacker, StatType.StaffCriticalDamagePercentAdjustment),
                _ => 0
            };
        }

        public static int GetSkillCriticalRatePercentAdjustment(uint attacker, SkillType skillType)
        {
            var adjustment = skillType switch
            {
                SkillType.Staff => Stat.GetStatAdjustment(attacker, StatType.StaffCriticalRatePercentAdjustment),
                _ => 0
            };

            if (IsRangedWeaponSkill(skillType))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.RangedCriticalRatePercentAdjustment);

            adjustment += GetLowHPCriticalRateAdjustment(attacker);
            return adjustment;
        }

        public static int GetAutoAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return GetTargetStatusCriticalRateAdjustment(attacker, defender) +
                   PrepareAutoAttackCycleCriticalRate(attacker, skillType);
        }

        public static int GetRangedAttackDamageFlatAdjustment(uint attacker, SkillType skillType)
        {
            return IsRangedWeaponSkill(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.RangedAttackDamageFlatAdjustment)
                : 0;
        }

        public static int GetRangedAttackDefenseIgnorePercentAdjustment(uint attacker, SkillType skillType)
        {
            return IsRangedWeaponSkill(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.RangedAttackDefenseIgnorePercentAdjustment)
                : 0;
        }

        public static int ApplyRangedAttackDefenseIgnore(uint attacker, int defense, SkillType skillType)
        {
            return ApplyDefenseIgnore(defense, GetRangedAttackDefenseIgnorePercentAdjustment(attacker, skillType));
        }

        public static int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            int? preTargetStatusStageDamage = null,
            bool isLandedAttack = true,
            bool typedLeadershipReductionAlreadyApplied = false)
        {
            if (damage <= 0)
                return damage;

            if (HasDamageImmunity(defender, damageType))
                return 0;

            var leadershipPhysicalAdjustment = !typedLeadershipReductionAlreadyApplied &&
                                               damageType.IsPhysicalDamageType()
                ? Stat.GetStatAdjustment(defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment)
                : 0;
            var leadershipForceAdjustment = !typedLeadershipReductionAlreadyApplied &&
                                            damageType == CombatDamageType.Force
                ? Stat.GetStatAdjustment(defender, StatType.LeadershipForceDamageTakenPercentAdjustment)
                : 0;
            var leadershipOtherAdjustment = !damageType.IsPhysicalDamageType() &&
                                            damageType != CombatDamageType.Force
                ? Stat.GetStatAdjustment(defender, StatType.LeadershipOtherDamageTakenPercentAdjustment)
                : 0;
            damage = ApplyDamageTakenPercentageModifiers(
                damage,
                damageType,
                leadershipPhysicalAdjustment,
                leadershipForceAdjustment,
                leadershipOtherAdjustment,
                Stat.GetStatAdjustment(defender, StatType.DamageTakenPercentAdjustment),
                typedLeadershipReductionAlreadyApplied);

            damage += Stat.GetStatAdjustment(defender, StatType.DamageTakenFlatAdjustment);

            if (preTargetStatusStageDamage.HasValue)
            {
                var minimumCombinedDamage = (int)Math.Ceiling(
                    preTargetStatusStageDamage.Value * ((100 - MaximumCombinedDamageReductionPercent) / 100f));
                if (damage < minimumCombinedDamage)
                    damage = minimumCombinedDamage;
            }

            damage = Math.Max(1, damage);

            // The math above is pure; everything below consumes one-shot effects or deals real
            // damage to third parties. A swing the engine later discards must not burn a redirect
            // or fatal-prevention charge, transfer damage to a protector, or grant temporary HP.
            if (!isLandedAttack)
                return damage;

            damage = ApplyDamageTakenRedirectToStatusSource(defender, attacker, damage, damageType);
            if (deliveryType != CombatDamageDeliveryType.Transferred)
                damage = ApplyDamageTakenShareToStatusSource(defender, attacker, damage, damageType);

            if (damage <= 0)
                return 0;

            if (TryPreventFatalDamageAndGrantTemporaryHP(defender, damage, restoreToOneHP: false))
                return 0;

            ApplyLowHPTemporaryHPBeforeFatalDamage(defender, damage);
            return damage;
        }

        private static int ApplyDamageTakenPercentageModifiers(
            int damage,
            CombatDamageType damageType,
            int leadershipPhysicalAdjustment,
            int leadershipForceAdjustment,
            int leadershipOtherAdjustment,
            int genericAdjustment,
            bool typedLeadershipReductionAlreadyApplied)
        {
            // Direct damage applies this channel explicitly after physical-to-Force conversion.
            // Damage that bypasses that pipeline must still use the same separate stage so its
            // Leadership and generic reductions remain multiplicative.
            if (!typedLeadershipReductionAlreadyApplied)
            {
                damage = ApplyTypedLeadershipDamageTakenPercentageModifier(
                    damage,
                    damageType,
                    leadershipPhysicalAdjustment,
                    leadershipForceAdjustment);
            }

            if (!damageType.IsPhysicalDamageType() && damageType != CombatDamageType.Force)
                genericAdjustment += leadershipOtherAdjustment;

            return genericAdjustment == 0
                ? damage
                : ApplyPercentDamageAdjustment(damage, genericAdjustment);
        }

        public static int ApplyTypedLeadershipDamageTakenModifier(
            uint defender,
            int damage,
            CombatDamageType damageType)
        {
            var leadershipPhysicalAdjustment = damageType.IsPhysicalDamageType()
                ? Stat.GetStatAdjustment(defender, StatType.LeadershipPhysicalDamageTakenPercentAdjustment)
                : 0;
            var leadershipForceAdjustment = damageType == CombatDamageType.Force
                ? Stat.GetStatAdjustment(defender, StatType.LeadershipForceDamageTakenPercentAdjustment)
                : 0;
            return ApplyTypedLeadershipDamageTakenPercentageModifier(
                damage,
                damageType,
                leadershipPhysicalAdjustment,
                leadershipForceAdjustment);
        }

        private static int ApplyTypedLeadershipDamageTakenPercentageModifier(
            int damage,
            CombatDamageType damageType,
            int leadershipPhysicalAdjustment,
            int leadershipForceAdjustment)
        {
            var adjustment = damageType.IsPhysicalDamageType()
                ? leadershipPhysicalAdjustment
                : damageType == CombatDamageType.Force
                    ? leadershipForceAdjustment
                    : 0;
            return adjustment == 0
                ? damage
                : ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private static int ApplyDamageTakenRedirectToStatusSource(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var redirectPercent = Stat.GetStatAdjustment(defender, StatType.DamageTakenRedirectToStatusSourcePercent);
            if (redirectPercent <= 0)
                return damage;

            var redirectTarget = StatusEffect.GetStatusEffectSourceWithStat(
                defender,
                StatType.DamageTakenRedirectToStatusSourcePercent);
            if (!GetIsObjectValid(redirectTarget) || GetIsDead(redirectTarget) || redirectTarget == defender)
                return damage;

            var redirectedDamage = Math.Min(
                damage,
                GameMath.PercentOf(damage, Math.Min(100, redirectPercent)));

            StatusEffect.RemoveStatusEffectsWithStat(defender, StatType.DamageTakenRedirectToStatusSourcePercent, false);

            // The redirect target takes a real hit from the attacker, so it gets the same
            // treatment as the Share sibling below: the target's own damage-taken mitigation
            // applies, and the damage dispatches under the attacker so the combat log does not
            // show the covered ally hurting their protector.
            var finalRedirectedDamage = ApplyDamageTakenModifiers(
                redirectTarget,
                redirectedDamage,
                attacker,
                damageType,
                CombatDamageDeliveryType.Transferred);

            if (finalRedirectedDamage > 0)
            {
                var damageSource = GetIsObjectValid(attacker) ? attacker : defender;
                AssignCommand(
                    damageSource,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(finalRedirectedDamage, damageType.GetNWScriptDamageType()),
                        redirectTarget));
            }
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), redirectTarget);

            if (finalRedirectedDamage > 0 &&
                GetIsObjectValid(attacker) &&
                GetIsReactionTypeHostile(attacker, redirectTarget))
            {
                Enmity.ModifyEnmity(redirectTarget, attacker, finalRedirectedDamage);
            }

            return damage - redirectedDamage;
        }

        private static int ApplyDamageTakenShareToStatusSource(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var sharePercent = Stat.GetStatAdjustment(defender, StatType.DamageTakenShareToStatusSourcePercent);
            if (sharePercent <= 0)
                return damage;

            var shareTarget = StatusEffect.GetStatusEffectSourceWithStat(
                defender,
                StatType.DamageTakenShareToStatusSourcePercent);
            if (!GetIsObjectValid(shareTarget) ||
                GetIsDead(shareTarget) ||
                shareTarget == defender ||
                GetArea(shareTarget) != GetArea(defender))
            {
                return damage;
            }

            var sharedDamage = Math.Min(
                damage,
                GameMath.PercentOf(damage, Math.Min(100, sharePercent)));

            // The bond itself carries the damage across, so the redirected portion always
            // arrives as Force damage regardless of what type the original hit dealt. The
            // share target mitigates it with Force defense, not the attacker's damage type.
            const CombatDamageType SharedDamageType = CombatDamageType.Force;
            var finalSharedDamage = ApplyDamageTakenModifiers(
                shareTarget,
                sharedDamage,
                attacker,
                SharedDamageType,
                CombatDamageDeliveryType.Transferred);

            if (finalSharedDamage > 0)
            {
                // The attacker dealt this damage, so it has to be dispatched under them for the
                // combat log to attribute it correctly - the mitigation above already treats them
                // as the source. Running it under the defender instead makes a warded ally look
                // like they damaged their own bond target. Fall back only if the attacker is gone.
                var damageSource = GetIsObjectValid(attacker) ? attacker : defender;
                AssignCommand(
                    damageSource,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(finalSharedDamage, SharedDamageType.GetNWScriptDamageType()),
                        shareTarget));
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), shareTarget);
            }

            if (finalSharedDamage > 0 &&
                GetIsObjectValid(attacker) &&
                GetIsReactionTypeHostile(attacker, shareTarget))
            {
                Enmity.ModifyEnmity(shareTarget, attacker, finalSharedDamage);
            }

            return damage - sharedDamage;
        }

        public static void ApplyDamageReflectionEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0 ||
                attacker == defender ||
                !GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetObjectType(attacker) != SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature ||
                GetObjectType(defender) != SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature)
            {
                return;
            }

            var adjustment = 0;
            if (damageType == CombatDamageType.Force)
            {
                ApplyForceDamageTakenEffects(defender);
                adjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageReflectionPercentAdjustment);
            }

            if (damageType == CombatDamageType.Physical)
                adjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalDamageReflectionPercentAdjustment);

            if (damageType.IsElementalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.ElementalDamageReflectionPercentAdjustment);

            if (adjustment <= 0)
                return;

            var reflectedDamage = GameMath.PercentOf(damage, adjustment);
            ApplyTriggeredDamage(defender, attacker, reflectedDamage, damageType);
        }

        private static void ApplyForceDamageTakenEffects(uint defender)
        {
            var forceDefense = Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenForceDefense);
            var durationSeconds = Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenForceDefenseDurationSeconds);
            if (forceDefense <= 0 || durationSeconds <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                defender,
                defender,
                new ForceWardingStatusEffect(forceDefense),
                durationSeconds,
                CombatDamageType.Force);
        }

        public static int ApplyDamageOverTimeTakenModifiers(
            uint defender,
            int damage,
            CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var adjustment = 0;
            if (damageType.IsPhysicalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalDamageOverTimeTakenPercentAdjustment);

            if (damageType == CombatDamageType.Force)
                adjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenPercentAdjustment);

            if (adjustment <= -100)
                return 0;

            if (adjustment != 0)
                damage += (int)Math.Ceiling(damage * (adjustment / 100f));

            return Math.Max(0, damage);
        }

        public static bool TryPreventFatalDamageAndGrantTemporaryHP(
            uint defender,
            int damage,
            bool restoreToOneHP)
        {
            if (!GetIsObjectValid(defender) || (damage <= 0 && !restoreToOneHP))
                return false;

            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPDurationSeconds);
            if (temporaryHPPercent <= 0 || duration <= 0)
                return false;

            var currentHP = GetCurrentHitPoints(defender);
            var isIncomingDamageFatal = damage > 0 && currentHP > 0 && damage >= currentHP;
            var isDyingFallback = restoreToOneHP && currentHP <= 0;
            if (!isIncomingDamageFatal && !isDyingFallback)
                return false;

            var cooldown = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPCooldownSeconds);
            if (!TryUseStatTrigger(defender, StatType.FatalDamageTemporaryHPPercent, cooldown))
                return false;

            var scalingAbilityScore = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPScalingAbilityScore);
            var tempHP = GameMath.PercentOf(GetMaxHitPoints(defender), temporaryHPPercent);
            if (scalingAbilityScore > 0)
                tempHP = AbilityEffectScaling.ScaleDirectEffect(tempHP, scalingAbilityScore);

            StatusEffect.RemoveStatusEffectsWithStat(defender, StatType.FatalDamageTemporaryHPPercent, false);

            if (restoreToOneHP && currentHP <= 0)
                SetCurrentHitPoints(defender, 1);

            TemporaryHitPointEffects.ApplyFlat(defender, "FATAL_DAMAGE_SAVE", tempHP, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), defender);

            return true;
        }

        public static int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType)
        {
            if (attack <= 0)
                return attack;

            var adjustment = 0;

            if (skillType == SkillType.Vibroblade &&
                StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.AttackToBleedingTargetPercentAdjustment);
            }

            if (adjustment == 0)
                return attack;

            return Math.Max(0, attack + (int)Math.Ceiling(attack * (adjustment / 100f)));
        }

        public static int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            bool isAbilityDamage,
            bool canApplyRandomFlatBonuses,
            bool isLandedAttack,
            out int damageBeforeTargetStatusStage)
        {
            damageBeforeTargetStatusStage = 0;

            if (damage <= 0)
                return damage;

            if (HasDamageImmunity(defender, damageType))
                return 0;

            var damageBeforePercentStages = damage;

            damage = ApplySkillAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);
            damage = ApplyOutgoingDamageModifier(attacker, damage);
            damage = ApplyDamageTypeDealtModifiers(attacker, damage, damageType);
            damage = ApplyWeaponAndForceDamageModifier(attacker, damage, skillType, damageType);
            damage = ApplyTargetLowHPDamageModifier(attacker, defender, damage);

            damageBeforeTargetStatusStage = damage;

            damage = ApplyTargetStatusDamageModifiers(
                attacker,
                defender,
                damage,
                skillType,
                damageType,
                isAbilityDamage,
                canApplyRandomFlatBonuses);
            // The repeated-target modifiers keep per-attacker stack state, so a swing the engine
            // later discards must not advance (or reset) their counters - the damage value itself
            // is thrown away with the swing.
            if (isLandedAttack)
            {
                damage = ApplyRepeatedTargetDamageModifier(attacker, defender, skillType, damage, isAbilityDamage);
                damage = ApplyMeleeRepeatedTargetDamageModifier(attacker, defender, skillType, damage, isAbilityDamage);
                damage = ApplyRangedRepeatedTargetDamageModifier(attacker, defender, skillType, damage);
            }

            var maxBonusDamage = damageBeforePercentStages +
                (int)Math.Ceiling(damageBeforePercentStages * (MaximumDamageBonusPercent / 100f));
            if (damage > maxBonusDamage)
                damage = maxBonusDamage;

            return Math.Max(1, damage);
        }

        public static int ApplySkillAbilityDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            bool isAbilityDamage)
        {
            if (damage <= 0 || !isAbilityDamage)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAbilityDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.SkillAbilityDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            bool isAbilityDamage = false,
            bool canApplyRandomFlatBonuses = true,
            bool isLandedAttack = true)
        {
            return ApplyDamageDealtModifiers(
                attacker,
                defender,
                damage,
                skillType,
                damageType,
                isAbilityDamage,
                canApplyRandomFlatBonuses,
                isLandedAttack,
                out _);
        }

        public static int ApplyAutoAttackDamageModifiers(uint attacker, uint defender, int damage, SkillType skillType = SkillType.Invalid)
        {
            if (damage <= 0)
                return damage;

            damage += TemporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);

            var chance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackDamageBonusChance);
            var bonus = Stat.GetStatAdjustment(attacker, StatType.AutoAttackDamageBonus);

            if (chance > 0 && bonus != 0 && Random.D100(1) <= chance)
                damage += bonus;

            damage += ConsumeNextSkillAutoAttackDamageBonus(attacker, skillType);

            var nextAutoAttackBonus = TemporaryStatModifier.Consume(
                attacker,
                StatType.NextAutoAttackDamageBonus,
                StatType.NextAutoAttackDamageBonus);
            if (nextAutoAttackBonus != 0)
            {
                damage += nextAutoAttackBonus;
            }

            var staminaRestoreChance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackStaminaRestore);
            if (staminaRestoreChance > 0 && staminaRestore > 0 && Random.D100(1) <= staminaRestoreChance)
            {
                Stat.RestoreStamina(attacker, staminaRestore);
            }

            var fpRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackFPRestore);
            var fpRestoreCooldown = Stat.GetStatAdjustment(attacker, StatType.AutoAttackFPRestoreCooldownSeconds);
            if (fpRestore > 0 && TryUseStatTrigger(attacker, StatType.AutoAttackFPRestore, fpRestoreCooldown))
            {
                Stat.RestoreFP(attacker, fpRestore);
            }

            var skillFpRestore = Stat.GetStatAdjustment(attacker, StatType.SkillAutoAttackFPRestore);
            var skillFpRestoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAutoAttackFPRestoreSkillType));
            if (skillFpRestore > 0 && SkillTypeMatches(skillType, skillFpRestoreSkillType))
            {
                Stat.RestoreFP(attacker, skillFpRestore);
            }

            ApplyAutoAttackMasterResourceRestore(attacker);

            var accuracyPenaltyChance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentChance);
            var accuracyPenalty = Stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment);
            var accuracyPenaltyDuration = Stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds);
            if (accuracyPenaltyChance > 0 &&
                accuracyPenalty != 0 &&
                accuracyPenaltyDuration > 0 &&
                Random.D100(1) <= accuracyPenaltyChance)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.AccuracyPercentAdjustment,
                    accuracyPenalty,
                    accuracyPenaltyDuration,
                    StatType.AutoAttackTargetAccuracyPercentAdjustment);
            }

            damage += GetDirectDamageToStatusCategoryOrStealthBonus(attacker, defender);
            ApplyAutoAttackHamstringEffect(attacker, defender, skillType, CombatDamageType.Physical);
            ApplyAutoAttackSunderedTargetFPRestore(attacker, defender);
            ApplySourceStatusStackEffects(attacker, defender);
            ApplyAutoAttackCycleDamage(attacker, defender, skillType);
            ApplySourceStatusAutoAttackCycleDamage(attacker, defender, skillType);

            return damage;
        }

        public static void ConsumeSuppressedAutoAttackDamageBonuses(uint attacker, SkillType skillType)
        {
            TemporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);
            ConsumeNextSkillAutoAttackDamageBonus(attacker, skillType);
            TemporaryStatModifier.Consume(
                attacker,
                StatType.NextAutoAttackDamageBonus,
                StatType.NextAutoAttackDamageBonus);
        }

        private static void ApplyAutoAttackMasterResourceRestore(uint attacker)
        {
            var master = GetMaster(attacker);
            if (!GetIsObjectValid(master))
                return;

            var staminaRestoreChance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterStaminaRestore);
            if (staminaRestoreChance > 0 && staminaRestore > 0 && Random.D100(1) <= staminaRestoreChance)
            {
                Stat.RestoreStamina(master, staminaRestore);
            }

            var fpRestoreChance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterFPRestoreChance);
            var fpRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterFPRestore);
            if (fpRestoreChance > 0 && fpRestore > 0 && Random.D100(1) <= fpRestoreChance)
            {
                Stat.RestoreFP(master, fpRestore);
            }
        }

        private static void ApplyFirstCombatAttackStaminaRestore(uint attacker)
        {
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.FirstCombatAttackStaminaRestore);
            var cooldownSeconds = Stat.GetStatAdjustment(attacker, StatType.FirstCombatAttackStaminaRestoreCooldownSeconds);
            if (staminaRestore <= 0 || cooldownSeconds <= 0)
                return;

            if (!_firstCombatAttackConsumed.Add(attacker))
                return;

            if (!TryUseStatTrigger(attacker, StatType.FirstCombatAttackStaminaRestore, cooldownSeconds))
                return;

            Stat.RestoreStamina(attacker, staminaRestore);
        }

        private static void ApplyAutoAttackHamstringEffect(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.AutoAttackHamstringSkillType));
            var duration = Stat.GetStatAdjustment(attacker, StatType.AutoAttackHamstringDurationSeconds);
            if (!SkillTypeMatches(skillType, requiredSkillType) || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                typeof(HamstringStatusEffect),
                duration,
                damageType);
        }

        private static int ConsumeNextSkillAutoAttackDamageBonus(uint attacker, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                attacker,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                StatType.NextSkillAutoAttackDamageBonusSkillType));
            if (storedSkillType != skillType)
                return 0;

            var bonus = TemporaryStatModifier.Consume(
                attacker,
                StatType.NextSkillAutoAttackDamageBonus,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
            TemporaryStatModifier.Consume(
                attacker,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                StatType.NextSkillAutoAttackDamageBonusSkillType);

            return bonus;
        }

        /// <summary>
        /// Advances the auto-attack cycle counter for cycle perks with no radius (Follow-Through) and,
        /// on the attack that completes the cycle, returns the cycle bonus as DMG so it feeds the
        /// standard attack damage formula and scales with the attacker's stats. Radius-based cycle
        /// perks (Edge Rhythm) hit a nearby enemy instead and are handled after the damage roll by
        /// ApplyAutoAttackCycleDamage.
        /// </summary>
        public static int ConsumeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return 0;

            var damageBonus = ConsumeMeleeAutoAttackCycleDamageBonus(attacker, skillType);
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamageSkillType));
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRequiredCount);
            var cycleDamage = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamage);
            var radius = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRadiusMeters);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                cycleDamage <= 0 ||
                radius > 0)
                return damageBonus;

            _autoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _autoAttackCycleCounts[attacker] = count;
                return damageBonus;
            }

            _autoAttackCycleCounts[attacker] = 0;
            return damageBonus + cycleDamage;
        }

        private static int ConsumeMeleeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType)
        {
            if (!IsMeleeWeaponSkill(skillType))
                return 0;

            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.MeleeAutoAttackCycleRequiredCount);
            var cycleDamage = Stat.GetStatAdjustment(attacker, StatType.MeleeAutoAttackCycleDamage);
            if (requiredCount <= 0 || cycleDamage <= 0)
                return 0;

            _meleeAutoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _meleeAutoAttackCycleCounts[attacker] = count;
                return 0;
            }

            _meleeAutoAttackCycleCounts[attacker] = 0;
            return cycleDamage;
        }

        private static void ApplyAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return;

            // No declared skill means every auto-attack advances the cycle - "every third
            // auto-attack" wording carries no weapon qualifier.
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamageSkillType));
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRequiredCount);
            var cycleDamage = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamage);
            var radius = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRadiusMeters);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                cycleDamage <= 0 ||
                radius <= 0)
                return;

            _autoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _autoAttackCycleCounts[attacker] = count;
                return;
            }

            _autoAttackCycleCounts[attacker] = 0;
            var target = radius > 0
                ? GetNearestHostileCreatureWithinRange(attacker, defender, radius, defender)
                : defender;
            if (!GetIsObjectValid(target))
                return;

            var appliedDamage = ApplyTriggeredDamage(
                attacker,
                target,
                cycleDamage,
                CombatDamageType.Physical,
                skillType);
            if (appliedDamage <= 0)
                return;

            Enmity.ModifyEnmity(attacker, target, appliedDamage);
        }

        private static void ApplySourceStatusAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
            {
                return;
            }

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleSkillType));
            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleRequiredCategory));
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.SourceStatusAutoAttackCycleRequiredCount);
            var damage = Stat.GetStatAdjustment(attacker, StatType.SourceStatusAutoAttackCycleDamage);
            var damageType = GetCombatDamageTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleDamageType));
            var key = (attacker, defender, requiredCategory);
            if (!SkillTypeMatches(skillType, requiredSkillType) ||
                requiredCategory == 0 ||
                requiredCount <= 0 ||
                damage <= 0 ||
                damageType == CombatDamageType.Invalid ||
                !TargetHasSourceAppliedStatusCategory(defender, attacker, requiredCategory))
            {
                _sourceStatusAutoAttackCycleCounts.Remove(key);
                return;
            }

            _sourceStatusAutoAttackCycleCounts.TryGetValue(key, out var count);
            count++;
            if (count < requiredCount)
            {
                _sourceStatusAutoAttackCycleCounts[key] = count;
                return;
            }

            _sourceStatusAutoAttackCycleCounts[key] = 0;
            var appliedDamage = ApplyTriggeredDamage(attacker, defender, damage, damageType, skillType);
            if (appliedDamage > 0)
            {
                Enmity.ModifyEnmity(attacker, defender, appliedDamage);
            }
        }

        public static void ApplyDamageDealtEffects(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            bool isAbilityDamage = false)
        {
            if (damage <= 0)
                return;

            var appliesDirectDamageEffects = deliveryType == CombatDamageDeliveryType.Direct;

            TrackCombatActivity(attacker);
            TrackRecentDamageTarget(attacker, defender);

            if (!appliesDirectDamageEffects)
                return;

            // The combat-entry tracker resets this rider's consumed state before the first landed
            // direct attack. Missed casts and incoming hostile actions keep that state alive without
            // consuming it, so Venatic Recovery cannot trigger twice during one prolonged engagement.
            ApplyFirstCombatAttackStaminaRestore(attacker);

            ApplySameTargetPressureDamageEffects(attacker, defender, skillType);
            ApplySideAttackDamageEffects(attacker, defender, skillType, damage);
            ApplyDamageDealtStaminaRestore(attacker, skillType);
            ApplyDamageDealtAttackDelayReduction(attacker, skillType);
            if (isAbilityDamage)
                ApplyPredatorsMarkEffects(attacker, defender, skillType);
            else
                ApplyAutoAttackSuppressionStack(attacker, defender, skillType, damageType);

            ApplyDamageDealtForceErosionEffect(attacker, defender, deliveryType);
            ApplyDamageDealtHamstringEffect(attacker, defender, skillType, damageType);
            ApplyDamageDealtMimicryTraitProcs(attacker, defender);
            ApplyNextDamageDealtBleedEffect(attacker, defender, damageType);
            ApplyRangedHitSuppressionStack(attacker, defender, skillType, damageType);
            ApplyBleedingTargetStaminaRestore(attacker, defender, skillType, isAbilityDamage);
            ApplyToxicRushDamageDealtEffects(attacker, defender, deliveryType);
            ApplyHeavyVibrobladeDefenseDamageRecovery(attacker, damage);
            ApplyFrenzySlashHasteRefresh(attacker);

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
            }

            if (damageType.IsPhysicalDamageType())
            {
                hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.PhysicalDamageDealtHPPercentRestore);
                if (hpRestorePercent > 0)
                {
                    ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
                }
            }

            ApplyLowHPDamageDealtHPRestore(attacker, damage);
        }

        private static void ApplyHeavyVibrobladeDefenseDamageRecovery(uint attacker, int damage)
        {
            if (Stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow) <= 0)
                return;

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore);
            if (hpRestorePercent <= 0)
                return;

            ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
        }

        private static void ApplyPredatorsMarkEffects(uint attacker, uint defender, SkillType skillType)
        {
            if (skillType != SkillType.BeastMastery ||
                !GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetIsDead(attacker) ||
                GetIsDead(defender))
            {
                return;
            }

            if (StatusEffect.HasStatusEffect(defender, typeof(PredatorsMark1StatusEffect), attacker))
            {
                ApplyPredatorsMarkFollowUp(attacker);
                return;
            }

            var damageTakenFromBeastPercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkDamageTakenFromBeastPercent);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkDurationSeconds);
            if (damageTakenFromBeastPercent <= 0 || durationSeconds <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new PredatorsMark1StatusEffect(damageTakenFromBeastPercent),
                durationSeconds,
                ResistanceType.Trauma);
        }

        private static void ApplyPredatorsMarkFollowUp(uint attacker)
        {
            var hastePercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkHastePercentPerStack);
            var abilityHitChancePercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkAbilityHitChancePercentPerStack);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkFollowUpDurationSeconds);
            var maximumStacks = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkFollowUpMaximumStacks);

            if (durationSeconds <= 0 || maximumStacks <= 0)
                return;

            if (hastePercent > 0)
            {
                TemporaryStatModifier.AddCapped(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    durationSeconds,
                    hastePercent * maximumStacks,
                    StatType.PredatorsMarkHastePercentPerStack,
                    1);
            }

            if (abilityHitChancePercent <= 0)
                return;

            TemporaryStatModifier.AddCapped(
                attacker,
                StatType.AbilityHitChancePercentAdjustment,
                abilityHitChancePercent,
                durationSeconds,
                abilityHitChancePercent * maximumStacks,
                StatType.PredatorsMarkAbilityHitChancePercentPerStack,
                1);
            TemporaryStatModifier.Replace(
                attacker,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                (int)SkillType.BeastMastery,
                durationSeconds,
                StatType.PredatorsMarkAbilityHitChancePercentPerStack);
        }

        private static void ApplyLowHPDamageDealtHPRestore(uint attacker, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPDamageDealtHPRestoreThresholdPercent);
            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.LowHPDamageDealtHPPercentRestore);
            if (threshold <= 0 || hpRestorePercent <= 0)
                return;

            var maxHP = GetMaxHitPoints(attacker);
            if (maxHP <= 0 || GetCurrentHitPoints(attacker) >= maxHP * (threshold / 100f))
                return;

            ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
        }

        private static void ApplyBleedingTargetStaminaRestore(
            uint attacker,
            uint defender,
            SkillType skillType,
            bool isAbilityDamage)
        {
            if (!GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
                return;

            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                SkillType.Invalid,
                StatType.DamageDealtBleedingTargetStaminaRestoreChance,
                StatType.DamageDealtBleedingTargetStaminaRestore);

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillDamageBleedingTargetStaminaRestoreSkillType));
            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                requiredSkillType,
                StatType.SkillDamageBleedingTargetStaminaRestoreChance,
                StatType.SkillDamageBleedingTargetStaminaRestore,
                StatType.SkillDamageBleedingTargetStaminaRestoreCooldownSeconds);

            if (!isAbilityDamage)
                return;

            requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAbilityBleedingTargetStaminaRestoreSkillType));
            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                requiredSkillType,
                StatType.SkillAbilityBleedingTargetStaminaRestoreChance,
                StatType.SkillAbilityBleedingTargetStaminaRestore,
                StatType.SkillAbilityBleedingTargetStaminaRestoreCooldownSeconds);
        }

        private static void ApplyBleedingTargetStaminaRestoreChannel(
            uint attacker,
            SkillType skillType,
            SkillType requiredSkillType,
            StatType chanceStat,
            StatType restoreStat,
            StatType cooldownStat = StatType.Invalid)
        {
            var chance = Stat.GetStatAdjustment(attacker, chanceStat);
            var staminaRestore = Stat.GetStatAdjustment(attacker, restoreStat);
            if (chance <= 0 ||
                staminaRestore <= 0 ||
                !SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                Random.D100(1) > chance)
            {
                return;
            }

            var cooldown = cooldownStat == StatType.Invalid
                ? 0
                : Stat.GetStatAdjustment(attacker, cooldownStat);
            if (!TryUseStatTrigger(attacker, restoreStat, cooldown))
                return;

            Stat.RestoreStamina(attacker, staminaRestore);
        }

        private static void ApplyDamageDealtForceErosionEffect(
            uint attacker,
            uint defender,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct)
                return;

            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionDurationSeconds);
            if (duration <= 0)
                return;

            var fpLossPerTick = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionFPLossPerTick);
            var staminaLossPerTick = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionStaminaLossPerTick);
            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ForceErosionStatusEffect(fpLossPerTick, staminaLossPerTick),
                duration,
                CombatDamageType.Physical);
        }

        private static void ApplyDamageDealtHamstringEffect(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtHamstringSkillType));
            var chance = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHamstringChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHamstringDurationSeconds);

            if (chance <= 0 ||
                duration <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                Random.D100(1) > chance)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                typeof(HamstringStatusEffect),
                duration,
                damageType);
        }

        /// <summary>
        /// On-hit proc traits mimicked from enemies. Each equipped proc trait grants a
        /// DamageDealt&lt;Effect&gt;Chance stat (via its trait status effect); on a landed direct hit
        /// each chance is rolled independently and, on success, applies the matching status effect
        /// for a fixed duration. Mirrors <see cref="ApplyDamageDealtHamstringEffect"/> but is not
        /// weapon-skill gated, since the analyzer replicates the trait regardless of armament.
        /// </summary>
        private static readonly (StatType Chance, Type Effect, CombatDamageType Damage, float Duration, StatType DurationOverride)[] MimicryTraitProcs =
        {
            (StatType.DamageDealtBleedChance, typeof(BleedStatusEffect), CombatDamageType.Physical, 12f, StatType.Invalid),
            (StatType.DamageDealtFreezingChance, typeof(FreezingStatusEffect), CombatDamageType.Ice, 6f, StatType.Invalid),
            (StatType.DamageDealtShockChance, typeof(ShockStatusEffect), CombatDamageType.Electrical, 10f, StatType.Invalid),
            (StatType.DamageDealtSunderChance, typeof(SunderStatusEffect), CombatDamageType.Physical, 14f, StatType.Invalid),
            (StatType.DamageDealtHemorrhageChance, typeof(HemorrhageStatusEffect), CombatDamageType.Physical, 12f, StatType.Invalid),
            (StatType.DamageDealtPoisonChance, typeof(PoisonStatusEffect), CombatDamageType.Poison, 12f, StatType.DamageDealtPoisonDurationSeconds),
        };

        private static void ApplyDamageDealtMimicryTraitProcs(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(defender))
                return;

            foreach (var proc in MimicryTraitProcs)
            {
                var chance = Stat.GetStatAdjustment(attacker, proc.Chance);
                if (chance <= 0 || Random.D100(1) > chance)
                    continue;

                var durationOverride = proc.DurationOverride == StatType.Invalid
                    ? 0
                    : Stat.GetStatAdjustment(attacker, proc.DurationOverride);
                var duration = durationOverride > 0 ? durationOverride : proc.Duration;

                StatusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    proc.Effect,
                    duration,
                    proc.Damage);
            }
        }

        private static void ApplySideAttackDamageEffects(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !IsMatchingSideAttack(attacker, defender, skillType))
                return;

            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.SideAttackStaminaRestore);
            var staminaCooldown = Stat.GetStatAdjustment(attacker, StatType.SideAttackStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 && TryUseStatTrigger(attacker, StatType.SideAttackStaminaRestore, staminaCooldown))
            {
                Stat.RestoreStamina(attacker, staminaRestore);
            }

            var delayReduction = Stat.GetStatAdjustment(attacker, StatType.SideAttackDelayReductionPercent);
            var duration = Stat.GetStatAdjustment(attacker, StatType.SideAttackDelayReductionDurationSeconds);
            if (delayReduction != 0 && duration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    delayReduction,
                    duration,
                StatType.SideAttackDelayReductionPercent);
            }
        }

        private static void ApplyDamageDealtStaminaRestore(uint attacker, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtStaminaRestoreSkillType));
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.DamageDealtStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.DamageDealtStaminaRestoreCooldownSeconds);

            if (staminaRestore <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                !TryUseStatTrigger(attacker, StatType.DamageDealtStaminaRestore, cooldown))
            {
                return;
            }

            Stat.RestoreStamina(attacker, staminaRestore);
        }

        private static void ApplyDamageDealtAttackDelayReduction(uint attacker, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtAttackDelayReductionSkillType));
            var delayReduction = Stat.GetStatAdjustment(attacker, StatType.DamageDealtAttackDelayReductionPercent);
            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds);

            if (delayReduction == 0 ||
                duration <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType))
            {
                return;
            }

            TemporaryStatModifier.Replace(
                attacker,
                StatType.AttackDelayReductionPercent,
                delayReduction,
                duration,
                StatType.DamageDealtAttackDelayReductionPercent);
        }

        public static int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !IsMatchingSideAttack(attacker, defender, skillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.SideAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        public static int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return IsMatchingSideAttack(attacker, defender, skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.SideAttackHitChancePercentAdjustment)
                : 0;
        }

        public static int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return IsMatchingSideAttack(attacker, defender, skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.SideAttackCriticalRatePercentAdjustment)
                : 0;
        }

        private static bool IsMatchingBackAttack(uint attacker, uint defender, SkillType skillType)
        {
            return skillType != SkillType.Invalid &&
                   !IsRangedWeaponSkill(skillType) &&
                   IsAttackerBehindTarget(attacker, defender);
        }

        public static int ApplyBackAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !IsMatchingBackAttack(attacker, defender, skillType))
                return damage;

            ApplyBackAttackExposed(attacker, defender);

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.BackAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        // A primed back attack (Ghost Protocol) inflicts Exposed on the landed hit. Both halves of
        // the primer are consumed together so the rider fires exactly once per priming.
        private static void ApplyBackAttackExposed(uint attacker, uint defender)
        {
            var exposedPercent = Stat.GetStatAdjustment(attacker, StatType.BackAttackExposedPercent);
            var exposedDuration = Stat.GetStatAdjustment(attacker, StatType.BackAttackExposedDurationSeconds);
            if (exposedPercent <= 0 || exposedDuration <= 0)
                return;

            TemporaryStatModifier.Consume(attacker, StatType.BackAttackExposedPercent);
            TemporaryStatModifier.Consume(attacker, StatType.BackAttackExposedDurationSeconds);

            // BackAttackExposedPercent is a reduction magnitude (declared BeneficialWhenPositive and
            // guarded above as positive), but ExposedStatusEffect writes its argument straight into
            // DefensePercentAdjustment. Passing the magnitude unchanged granted the target +20%
            // Defense instead of taking it away, so it must be negated here.
            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(-exposedPercent),
                exposedDuration,
                CombatDamageType.Physical);
        }

        public static int GetBackAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return IsMatchingBackAttack(attacker, defender, skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.BackAttackCriticalRatePercentAdjustment)
                : 0;
        }

        public static int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion)
        {
            if (evasion <= 0 || !IsMatchingSideAttack(attacker, defender, skillType))
                return evasion;

            var chance = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChance);
            if (chance <= 0)
                return evasion;

            var scalingAbility = GetAbilityTypeFromStatPlusOne(
                Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                chance += Math.Max(0, GetAbilityScore(attacker, scalingAbility));
            }

            var chanceMaximum = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceMaximum);
            if (chanceMaximum > 0)
            {
                chance = Math.Min(chance, chanceMaximum);
            }

            var ignorePercent = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnorePercent);
            if (ignorePercent <= 0 || Random.D100(1) > chance)
                return evasion;

            return Math.Max(0, evasion - (int)Math.Ceiling(evasion * (Math.Min(100, ignorePercent) / 100f)));
        }

        private static bool IsMatchingSideAttack(uint attacker, uint defender, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.SideAttackSkillType));
            return SkillTypeMatches(skillType, requiredSkillType) && IsAttackerBesideTarget(attacker, defender);
        }

        public static void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid)
        {
            if (criticalRating <= 0)
                return;

            // Limited Haste is earned by the critical result itself, even when mitigation reduces
            // that critical's damage to zero. Damage-derived critical riders remain below.
            ApplyCriticalHitLimitedHaste(attacker, skillType);

            if (damage <= 0)
                return;

            if (GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender) > 0)
            {
                FloatingTextStringOnCreature(ColorToken.Combat("Weak Points"), attacker, false);
            }

            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestore);
            var staminaRestoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreSkillType));
            var staminaRestoreCooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 &&
                SkillTypeMatches(skillType, staminaRestoreSkillType) &&
                TryUseStatTrigger(attacker, StatType.CriticalStaminaRestore, staminaRestoreCooldown))
            {
                Stat.RestoreStamina(attacker, staminaRestore);
            }

            ApplyCriticalNextAbilityDamageBonus(attacker, skillType);
            ApplyCriticalNextSkillAbilityDefenseIgnore(attacker, skillType);
            ApplyCriticalNextAutoAttackNoDelay(attacker, skillType);
            ApplyCriticalSideAttackStaminaRestore(attacker, defender);
            ApplyCriticalBleedingStatusDurationExtension(attacker, defender);
            ApplyCriticalHitSequenceEffects(attacker);
            ApplyCriticalHitSelfEffects(attacker);

            var poisonedTargetStaminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalPoisonedTargetStaminaRestore);
            if (poisonedTargetStaminaRestore > 0 && StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                Stat.RestoreStamina(attacker, poisonedTargetStaminaRestore);
            }

            var markedTargetStaminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalMarkedTargetStaminaRestore);
            if (markedTargetStaminaRestore > 0 && StatusEffect.HasStatusEffect(defender, typeof(MarkingTossStatusEffect), attacker))
            {
                Stat.RestoreStamina(attacker, markedTargetStaminaRestore);
            }

            var targetFPLossPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetFPLossPercentOfDamage);
            if (targetFPLossPercent > 0)
            {
                var fpLoss = GameMath.PercentOf(damage, targetFPLossPercent);
                Stat.ReduceFP(defender, fpLoss);
            }

            var targetStaminaLossPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetStaminaLossPercentOfDamage);
            if (targetStaminaLossPercent > 0)
            {
                var staminaLoss = GameMath.PercentOf(damage, targetStaminaLossPercent);
                Stat.ReduceStamina(defender, staminaLoss);
            }

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestore);
            var hpRestoreCooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds);
            if (hpRestorePercent > 0 &&
                TryUseStatTrigger(attacker, StatType.CriticalHPPercentOfDamageRestore, hpRestoreCooldown))
            {
                ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
            }

            var accuracyPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyPercentAdjustment);
            var accuracyDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyDurationSeconds);
            if (accuracyPercent != 0 && accuracyDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AccuracyPercentAdjustment,
                    accuracyPercent,
                    accuracyDuration,
                    StatType.CriticalAccuracyPercentAdjustment);
            }

            var targetEvasionPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionPercentAdjustment);
            var targetEvasionDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionDurationSeconds);
            if (targetEvasionPercent != 0 && targetEvasionDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    targetEvasionPercent,
                    targetEvasionDuration,
                    StatType.CriticalTargetEvasionPercentAdjustment);
            }

            var targetDefensePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefenseDurationSeconds);
            if (skillType == SkillType.Staff)
            {
                targetDefensePercent += Stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefensePercentAdjustment);
                targetDefenseDuration = Math.Max(
                    targetDefenseDuration,
                    Stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefenseDurationSeconds));
            }
            if (targetDefensePercent != 0 && targetDefenseDuration > 0)
            {
                StatusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    new ExposedStatusEffect(targetDefensePercent),
                    targetDefenseDuration,
                    CombatDamageType.Physical);
            }

            if (isSingleTargetImpact)
            {
                ApplySingleTargetCriticalTargetDefenseEffect(attacker, defender);
            }
        }

        private static void ApplyCriticalHitSelfEffects(uint attacker)
        {
            var evasion = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionPercentAdjustment);
            var evasionDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionDurationSeconds);
            if (evasion != 0 && evasionDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    evasionDuration,
                    StatType.CriticalHitSelfEvasionPercentAdjustment);
            }

            var haste = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHastePercentAdjustment);
            var hasteDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHasteDurationSeconds);
            if (haste != 0 && hasteDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    hasteDuration,
                    StatType.CriticalHitSelfHastePercentAdjustment);
            }
        }

        public static void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType));
            var criticalRate = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment);
            var maximum = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds);
            if (!SkillTypeMatches(skillType, requiredSkillType) ||
                criticalRate <= 0 ||
                maximum <= 0 ||
                duration <= 0)
            {
                return;
            }

            TemporaryStatModifier.Replace(
                activator,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                duration,
                StatType.NextSkillAbilitySkillType);
            TemporaryStatModifier.AddCapped(
                activator,
                StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                criticalRate,
                duration,
                maximum,
                StatType.NextSkillAbilitySkillType,
                1);

            var currentTotal = TemporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            if (GetIsPC(activator) && currentTotal > 0)
            {
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Deadeye Reload +{currentTotal}% Critical Rate"),
                    activator,
                    false);
            }
        }

        private static void ApplyCriticalBleedingStatusDurationExtension(uint attacker, uint defender)
        {
            var extensionSeconds = Stat.GetStatAdjustment(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds);
            if (extensionSeconds <= 0 ||
                !GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker) &&
                !StatusEffect.HasStatusEffect(defender, typeof(HemorrhageStatusEffect), attacker))
            {
                return;
            }

            var cooldownSeconds = Stat.GetStatAdjustment(
                attacker,
                StatType.CriticalBleedingStatusDurationExtensionCooldownSeconds);
            if (!TryUseStatTrigger(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds, cooldownSeconds))
                return;

            StatusEffect.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
            StatusEffect.ExtendStatusEffectDuration(defender, typeof(HemorrhageStatusEffect), attacker, extensionSeconds);
        }

        private static void ApplyCriticalHitSequenceEffects(uint attacker)
        {
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceCountRequired);
            var windowSeconds = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceWindowSeconds);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceStaminaRestore);
            if (requiredCount <= 0 || windowSeconds <= 0 || staminaRestore <= 0)
                return;

            var now = DateTime.UtcNow;
            var count = 1;
            if (_criticalHitSequenceStates.TryGetValue(attacker, out var state) &&
                (now - state.LastHit).TotalSeconds <= windowSeconds)
            {
                count = state.Count + 1;
            }

            if (count >= requiredCount)
            {
                _criticalHitSequenceStates.Remove(attacker);
                Stat.RestoreStamina(attacker, staminaRestore);
                return;
            }

            _criticalHitSequenceStates[attacker] = new CriticalHitSequenceState
            {
                Count = count,
                LastHit = now
            };
        }

        private static void ApplyCriticalNextSkillAbilityDefenseIgnore(uint attacker, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return;

            var defenseIgnore = Stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment);
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds);
            if (defenseIgnore <= 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                duration,
                StatType.NextSkillAbilitySkillType);

            TemporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                defenseIgnore,
                duration,
                StatType.NextSkillAbilitySkillType);
        }

        private static void ApplyCriticalNextAbilityDamageBonus(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType));
            if (!SkillTypeMatches(skillType, triggerSkillType))
                return;

            var perkType = GetPerkTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusPerkType));
            var bonus = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonus);
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusCooldownSeconds);
            if (perkType == PerkType.Invalid || bonus == 0 || duration <= 0)
                return;

            if (TryUseStatTrigger(attacker, StatType.CriticalNextAbilityDamageBonus, cooldown))
            {
                GrantNextAbilityDamageBonus(attacker, perkType, bonus, duration);
            }
        }

        private static void ApplyCriticalHitLimitedHaste(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteTriggerSkillType));
            if (!SkillTypeMatches(skillType, triggerSkillType))
                return;

            var hastePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHastePercentAdjustment);
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteDurationSeconds);
            var attackCount = Stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteAttackCount);
            var statusEffectIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.CriticalHitLimitedHasteStatusEffectIcon));
            if (hastePercent <= 0 ||
                duration <= 0 ||
                attackCount <= 0 ||
                statusEffectIcon == EffectIconType.Invalid)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new LimitedHasteStatusEffect(
                    hastePercent,
                    attackCount,
                    SkillType.Invalid,
                    statusEffectIcon,
                    Ability.GetActiveAbilityImpactSummary(attacker)),
                duration);
        }

        private static void ApplyCriticalNextAutoAttackNoDelay(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType));
            if (!SkillTypeMatches(skillType, triggerSkillType))
                return;

            var noDelaySkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType));
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayCooldownSeconds);
            if (noDelaySkillType == SkillType.Invalid || duration <= 0)
                return;

            if (TryUseStatTrigger(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType, cooldown))
            {
                GrantNextAutoAttackNoDelay(attacker, noDelaySkillType, duration);
            }
        }

        private static void ApplyCriticalSideAttackStaminaRestore(uint attacker, uint defender)
        {
            var chance = Stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestore);
            if (chance <= 0 || staminaRestore <= 0 || !IsAttackerBesideTarget(attacker, defender))
                return;

            if (Random.D100(1) <= chance)
            {
                Stat.RestoreStamina(attacker, staminaRestore);
            }
        }

        private static void ApplySingleTargetCriticalTargetDefenseEffect(uint attacker, uint defender)
        {
            var targetDefensePercent = Stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = Stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefenseDurationSeconds);
            if (targetDefensePercent == 0 || targetDefenseDuration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(targetDefensePercent),
                targetDefenseDuration,
                CombatDamageType.Physical);
        }

        // Angle in degrees between the defender's facing and the direction to the attacker:
        // 0 = attacker directly in front, 180 = directly behind. Returns null when the pair is
        // not comparable (invalid, cross-area, or overlapping). Shared by every positional check
        // so their thresholds stay the single source of difference.
        private static double? GetFacingAngleDegrees(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetArea(attacker) != GetArea(defender))
                return null;

            var defenderPosition = GetPosition(defender);
            var attackerPosition = GetPosition(attacker);
            var deltaX = attackerPosition.X - defenderPosition.X;
            var deltaY = attackerPosition.Y - defenderPosition.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (distance <= 0.001)
                return null;

            var facingRadians = GetFacing(defender) * Math.PI / 180.0;
            var forwardX = Math.Cos(facingRadians);
            var forwardY = Math.Sin(facingRadians);
            var dot = Math.Clamp((forwardX * deltaX + forwardY * deltaY) / distance, -1.0, 1.0);
            return Math.Acos(dot) * 180.0 / Math.PI;
        }

        public static bool IsAttackerBesideTarget(uint attacker, uint defender)
        {
            var angleDegrees = GetFacingAngleDegrees(attacker, defender);
            return angleDegrees is >= 45.0 and <= 135.0;
        }

        public static bool IsAttackerBehindTarget(uint attacker, uint defender)
        {
            return GetFacingAngleDegrees(attacker, defender) > 135.0;
        }

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void ApplyDamageTakenEffects()
        {
            var defender = OBJECT_SELF;
            var attacker = GetLastDamager(defender);
            var damage = GetTotalDamageDealt();

            ApplyDamageTakenEffects(defender, attacker, damage);
        }

        public static void ApplyDamageTakenEffects(uint defender, uint attacker, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            var attackPercent = Stat.GetStatAdjustment(defender, StatType.DamageTakenAttackPercentAdjustment);
            var attackDuration = Stat.GetStatAdjustment(defender, StatType.DamageTakenAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DamageTakenAttackPercentAdjustment);
            }

            ApplyLowHPDamageTakenEffects(defender, damage);
            ApplyDamageTakenNextSkillAbilityDamage(defender);
            ApplyReversalCutReady(defender);
            TrackRecentDamageTaken(defender);
            ApplyRecentDamageTargetHitEffects(defender, attacker);

            if (GetIsObjectValid(attacker) && GetIsReactionTypeHostile(attacker, defender))
            {
                TrackHostileDefensiveCombatEntryActivity(defender, attacker);
                EmbattledStatusEffect.Refresh(defender, attacker);
            }
        }

        private static void ApplyDamageTakenNextSkillAbilityDamage(uint defender)
        {
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                defender,
                StatType.DamageTakenNextSkillAbilitySkillType));
            var damageBonus = Stat.GetStatAdjustment(defender, StatType.DamageTakenNextSkillAbilityDamageBonus);
            var window = Stat.GetStatAdjustment(defender, StatType.DamageTakenNextSkillAbilityWindowSeconds);
            if (damageBonus <= 0 || window <= 0)
                return;

            GrantNextSkillAbilityBonuses(defender, skillType, damageBonus, 0, window);
        }

        public static void ApplyLowHPDamageTakenEffects(uint defender, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            ApplyLowHPPhysicalDefenseEffect(defender, damage);
            ApplyLowHPEvasionEffect(defender, damage);
            ApplyLowHPNextAbilityNoStaminaCostEffect(defender, damage);
            ApplyLowHPTemporaryHPEffect(defender, damage);
            ApplyLowHPNoSaveTemporaryHPEffect(defender, damage);
            ApplyLowHPGuardEffect(defender, damage);
        }

        private static void ApplyReversalCutReady(uint defender)
        {
            if (Stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCut) <= 0)
                return;

            var window = Stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCutWindowSeconds);
            if (window <= 0)
                return;

            var damageBonus = Stat.GetStatAdjustmentExcludingTemporaryModifiers(
                defender,
                StatType.TwinBladeDuelistReversalCutDamageBonus);
            if (damageBonus > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.TwinBladeDuelistReversalCutDamageBonus,
                    damageBonus,
                    window,
                    StatType.TwinBladeDuelistReversalCut);
            }

            var dazedDuration = Stat.GetStatAdjustmentExcludingTemporaryModifiers(
                defender,
                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds);
            if (dazedDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.TwinBladeDuelistReversalCutDazedDurationSeconds,
                    dazedDuration,
                    window,
                    StatType.TwinBladeDuelistReversalCut);
            }
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ApplyDefeatedEnemyEffects()
        {
            var defeated = OBJECT_SELF;
            var killer = GetLastKiller();

            if (GetIsObjectValid(killer) && killer != defeated)
            {
                ApplyDefeatedEnemyEffects(killer, defeated);
            }

            ApplyRecentDamagerDefeatedEnemyEffects(defeated);
            RemoveStatTriggerCooldowns(defeated);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearStatTriggerState()
        {
            var creature = GetExitingObject();
            if (!GetIsObjectValid(creature))
                return;

            RemoveStatTriggerCooldowns(creature);
        }

        public static void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
                return;

            if (GetIsObjectValid(defeated))
            {
                ApplyPoisonedDefeatedEnemySpread(defeated, creature);
                ApplyDefeatedBleedingEnemySpread(creature, defeated);
            }

            var staminaRestore = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(creature, staminaRestore);
            }

            var fpRestore = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyFPRestore);
            if (fpRestore > 0)
            {
                Stat.RestoreFP(creature, fpRestore);
            }

            var hpRestorePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                HealFromMaxHP(creature, hpRestorePercent);
            }

            var attackPercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackPercentAdjustment);
            var attackDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DefeatedEnemyAttackPercentAdjustment);
            }

            var hastePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionPercent);
            var hasteDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionDurationSeconds);
            if (hastePercent != 0 && hasteDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    hasteDuration,
                    StatType.DefeatedEnemyAttackDelayReductionPercent);
            }

            var allyDefensePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            var allyDefenseDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds);
            if (allyDefensePercent != 0 && allyDefenseDuration > 0)
            {
                ApplyDefeatedEnemyNearbyAllyDefense(creature, allyDefensePercent, allyDefenseDuration);
            }

            ApplyHitPointSpendDefeatedEnemyEffects(creature);
        }

        private static void ApplyHitPointSpendDefeatedEnemyEffects(uint creature)
        {
            if (Stat.GetStatAdjustment(creature, StatType.HeavyVibrobladeOffenseSoulAscension) <= 0)
                return;

            var marker = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.HeavyVibrobladeOffenseSoulAscension,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            if (marker <= 0)
                return;

            StatusEffect.ApplyStatusEffect(creature, creature, typeof(SoulAscensionStatusEffect), 30f);
        }

        private static void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated)
        {
            const float RecentDamageWindowSeconds = 6f;

            if (!GetIsObjectValid(defeated))
                return;

            var now = DateTime.UtcNow;
            var recentDamagers = _recentDamageTargets
                .Where(x => x.Key.Item2 == defeated)
                .ToList();

            foreach (var ((source, target), lastDamaged) in recentDamagers)
            {
                if ((now - lastDamaged).TotalSeconds > RecentDamageWindowSeconds)
                {
                    _recentDamageTargets.Remove((source, target));
                    continue;
                }

                ApplyCruelMomentumEffect(source);
            }
        }

        private static void ApplyCruelMomentumEffect(uint creature)
        {
            if (!GetIsObjectValid(creature) ||
                GetIsDead(creature) ||
                Stat.GetStatAdjustment(creature, StatType.CruelMomentum) <= 0 ||
                !TryUseStatTrigger(creature, StatType.CruelMomentum, 10))
            {
                return;
            }

            Stat.RestoreFP(creature, 2);
            StatusEffect.ApplyStatusEffect(creature, creature, typeof(CruelMomentumStatusEffect), 30f);
        }

        private static void ApplyDefeatedEnemyNearbyAllyDefense(
            uint creature,
            int physicalDefensePercent,
            int durationSeconds)
        {
            const float Range = 5f;

            foreach (var member in Party.GetAllPartyMembersWithinRange(creature, Range))
            {
                if (member == creature)
                    continue;

                TemporaryStatModifier.Replace(
                    member,
                    StatType.PhysicalDefensePercentAdjustment,
                    physicalDefensePercent,
                    durationSeconds,
                StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            }
        }

        private static void ApplyPoisonedDefeatedEnemySpread(uint defeated, uint fallbackSource)
        {
            var poisonEffects = StatusEffect.GetCreatureStatusEffects(defeated)
                .GetAllEffects()
                .Where(effect => effect is PoisonStatusEffect)
                .ToList();
            if (poisonEffects.Count <= 0)
                return;

            foreach (var poisonEffect in poisonEffects)
            {
                var source = GetIsObjectValid(poisonEffect.Source)
                    ? poisonEffect.Source
                    : fallbackSource;
                var radius = Stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadRadiusMeters);
                var duration = Stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadDurationSeconds);
                if (radius <= 0 || duration <= 0)
                    continue;

                var target = GetNearestHostileCreatureWithinRange(source, defeated, radius, defeated);
                if (!GetIsObjectValid(target))
                    continue;

                StatusEffect.ApplyStatusEffect(source, target, typeof(PoisonStatusEffect), duration, CombatDamageType.Physical);
                return;
            }
        }

        private static void ApplyDefeatedBleedingEnemySpread(uint creature, uint defeated)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(defeated) ||
                !StatusEffect.HasStatusEffectCategory(defeated, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(creature, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds);
            if (duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         creature,
                         GetLocation(defeated),
                         5f,
                         3,
                         defeated))
            {
                StatusEffect.ApplyStatusEffect(creature, nearby, typeof(BleedStatusEffect), duration, CombatDamageType.Physical);
            }
        }

        private static uint GetNearestHostileCreatureWithinRange(
            uint source,
            uint origin,
            float radius,
            uint excludedTarget = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(source) || !GetIsObjectValid(origin) || radius <= 0f)
                return OBJECT_INVALID;

            var originLocation = GetLocation(origin);
            var nearest = OBJECT_INVALID;
            var nearestDistance = float.MaxValue;
            var creature = GetFirstObjectInShape(Shape.Sphere, radius, originLocation, true);
            while (GetIsObjectValid(creature))
            {
                if (creature != excludedTarget &&
                    GetIsReactionTypeHostile(creature, source) &&
                    !GetIsDead(creature))
                {
                    var distance = GetDistanceBetween(origin, creature);
                    if (distance < nearestDistance)
                    {
                        nearest = creature;
                        nearestDistance = distance;
                    }
                }

                creature = GetNextObjectInShape(Shape.Sphere, radius, originLocation, true);
            }

            return nearest;
        }

        public static IDisposable BeginDamageDerivedHealing(uint creature)
        {
            if (!_damageDerivedHealingStates.TryGetValue(creature, out var state))
            {
                state = new DamageDerivedHealingState();
                _damageDerivedHealingStates[creature] = state;
            }

            state.Depth++;
            return new DamageDerivedHealingScope(creature);
        }

        public static int CalculateCappedDamageDerivedHealingAmount(
            int damage,
            int healingAlreadyApplied,
            int requestedHealing)
        {
            if (damage <= 0 || requestedHealing <= 0)
                return 0;

            var cap = GameMath.PercentOf(damage, MaximumDamageDerivedHealingPercentPerHit);
            var remaining = Math.Max(0, cap - Math.Max(0, healingAlreadyApplied));
            return Math.Min(requestedHealing, remaining);
        }

        public static int ApplyDamageDerivedHealing(
            uint creature,
            int damage,
            int percent,
            bool applyCombatReadiness = false)
        {
            if (damage <= 0 || percent <= 0)
                return 0;

            var amount = GameMath.PercentOf(damage, percent);
            if (applyCombatReadiness)
                amount = Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(creature, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount);

            if (_damageDerivedHealingStates.TryGetValue(creature, out var state))
            {
                if (state.Damage <= 0)
                    state.Damage = damage;

                amount = CalculateCappedDamageDerivedHealingAmount(
                    state.Damage,
                    state.HealingApplied,
                    amount);
                state.HealingApplied += amount;
            }
            else
            {
                amount = CalculateCappedDamageDerivedHealingAmount(damage, 0, amount);
            }

            if (amount <= 0)
                return 0;

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
            return amount;
        }

        private static void EndDamageDerivedHealing(uint creature)
        {
            if (!_damageDerivedHealingStates.TryGetValue(creature, out var state))
                return;

            state.Depth--;
            if (state.Depth <= 0)
                _damageDerivedHealingStates.Remove(creature);
        }

        private sealed class DamageDerivedHealingState
        {
            public int Depth { get; set; }
            public int Damage { get; set; }
            public int HealingApplied { get; set; }
        }

        private sealed class DamageDerivedHealingScope : IDisposable
        {
            private readonly uint _creature;
            private bool _disposed;

            public DamageDerivedHealingScope(uint creature)
            {
                _creature = creature;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                EndDamageDerivedHealing(_creature);
            }
        }

        private static void ApplyLowHPPhysicalDefenseEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseThresholdPercent);
            var defensePercent = Stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseCooldownSeconds);

            if (threshold <= 0 ||
                defensePercent == 0 ||
                duration <= 0 ||
                !DidCrossHPThreshold(defender, damage, threshold) ||
                !TryUseStatTrigger(defender, StatType.LowHPPhysicalDefensePercentAdjustment, cooldown))
                return;

            // A visible status effect carries the defense bonus so the player can track the trigger.
            StatusEffect.ApplyStatusEffect(
                defender,
                defender,
                new UnbreakableStatusEffect(defensePercent),
                duration);
        }

        private static void ApplyLowHPEvasionEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPEvasionThresholdPercent);
            var evasionPercent = Stat.GetStatAdjustment(defender, StatType.LowHPEvasionPercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPEvasionDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPEvasionCooldownSeconds);

            if (threshold <= 0 ||
                evasionPercent == 0 ||
                duration <= 0 ||
                !DidCrossHPThreshold(defender, damage, threshold) ||
                !TryUseStatTrigger(defender, StatType.LowHPEvasionPercentAdjustment, cooldown))
                return;

            TemporaryStatModifier.Replace(
                defender,
                StatType.EvasionPercentAdjustment,
                evasionPercent,
                duration,
                StatType.LowHPEvasionPercentAdjustment);
        }

        private static void ApplyLowHPNextAbilityNoStaminaCostEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostThresholdPercent);
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostSkillType));
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostCooldownSeconds);

            if (threshold <= 0 ||
                skillType == SkillType.Invalid ||
                duration <= 0 ||
                !DidCrossHPThreshold(defender, damage, threshold) ||
                !TryUseStatTrigger(defender, StatType.LowHPNextAbilityNoStaminaCostSkillType, cooldown))
                return;

            TemporaryStatModifier.Replace(
                defender,
                StatType.NextAbilityNoStaminaCostSkillType,
                (int)skillType,
                duration,
                StatType.NextAbilityNoStaminaCostSkillType);
        }

        private static void ApplyLowHPTemporaryHPEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPThresholdPercent);
            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPCooldownSeconds);

            if (threshold <= 0 ||
                temporaryHPPercent <= 0 ||
                duration <= 0 ||
                !DidCrossHPThreshold(defender, damage, threshold) ||
                !TryUseStatTrigger(defender, StatType.LowHPTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = GameMath.PercentOf(GetMaxHitPoints(defender), temporaryHPPercent);
            TemporaryHitPointEffects.ApplyFlat(defender, "LOW_HP_SHIELD", temporaryHP, duration);
        }

        private static void ApplyLowHPTemporaryHPBeforeFatalDamage(uint defender, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPThresholdPercent);
            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPCooldownSeconds);
            if (threshold <= 0 || temporaryHPPercent <= 0 || duration <= 0)
                return;

            var maxHP = GetMaxHitPoints(defender);
            var currentHP = GetCurrentHitPoints(defender);
            if (maxHP <= 0 || currentHP <= 0)
                return;

            var thresholdHP = maxHP * (threshold / 100f);
            var projectedHP = currentHP - damage;
            if (currentHP < thresholdHP || projectedHP >= thresholdHP || projectedHP > 0)
                return;

            if (!TryUseStatTrigger(defender, StatType.LowHPTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = GameMath.PercentOf(maxHP, temporaryHPPercent);
            TemporaryHitPointEffects.ApplyFlat(defender, "LOW_HP_SHIELD", temporaryHP, duration);
        }

        private static void ApplyLowHPNoSaveTemporaryHPEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPThresholdPercent);
            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPCooldownSeconds);

            if (threshold <= 0 ||
                temporaryHPPercent <= 0 ||
                duration <= 0 ||
                !DidCrossHPThreshold(defender, damage, threshold) ||
                !TryUseStatTrigger(defender, StatType.LowHPNoSaveTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = GameMath.PercentOf(GetMaxHitPoints(defender), temporaryHPPercent);
            TemporaryHitPointEffects.ApplyFlat(defender, "LOW_HP_SHIELD_NO_SAVE", temporaryHP, duration);
        }

        private static void ApplyLowHPGuardEffect(uint defender, int damage)
        {
            ApplyLowHPGuardEffect(defender, damage, defender);
        }

        public static void ApplyLowHPGuardEffectFromProtectedTarget(uint guardRecipient, uint protectedTarget, int damage)
        {
            ApplyLowHPGuardEffect(protectedTarget, damage, guardRecipient);
        }

        private static void ApplyLowHPGuardEffect(uint thresholdCreature, int damage, uint guardRecipient)
        {
            if (!GetIsObjectValid(thresholdCreature) || !GetIsObjectValid(guardRecipient))
                return;

            var threshold = Stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardThresholdPercent);
            var guardChance = Stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuard);
            var duration = Stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardCooldownSeconds);

            if (threshold <= 0 ||
                guardChance <= 0 ||
                duration <= 0 ||
                !DidCrossHPThreshold(thresholdCreature, damage, threshold) ||
                !TryUseStatTrigger(guardRecipient, StatType.LowHPGuard, cooldown))
                return;

            StatusEffect.ApplyStatusEffect(
                guardRecipient,
                guardRecipient,
                new GuardianReflexesStatusEffect(guardChance),
                duration);

            if (GetIsPC(guardRecipient))
                FloatingTextStringOnCreature(ColorToken.Combat("Guardian Reflexes"), guardRecipient, false);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), guardRecipient);
        }

        private static void TrackRecentDamageTarget(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            _recentDamageTargets[(attacker, defender)] = DateTime.UtcNow;
        }

        private static void TrackRecentDamageTaken(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            _recentDamageTaken[creature] = DateTime.UtcNow;
        }

        public static bool HasRecentDamageTaken(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            if (!_recentDamageTaken.TryGetValue(creature, out var lastDamaged))
                return false;

            var isRecent = (DateTime.UtcNow - lastDamaged).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentDamageTaken.Remove(creature);

            return isRecent;
        }

        private static void TrackCombatActivity(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            var now = DateTime.UtcNow;
            ReportCombatEntryIfNeeded(creature, now);
            _lastCombatActivity[creature] = now;
        }

        public static void TrackAttackActivity(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            _lastAttackActivity[creature] = DateTime.UtcNow;
            TrackCombatActivity(creature);
        }

        public static void TrackHostileAbilityActivity(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            var now = DateTime.UtcNow;
            ReportCombatEntryIfNeeded(creature, now);
            // Keep cast attempts separate from landed combat activity. Opening-hit riders such as
            // Venatic Recovery must still observe the previous landed-combat timestamp.
            _lastHostileAbilityAttemptActivity[creature] = now;
        }

        public static void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(attacker) ||
                !GetIsReactionTypeHostile(attacker, creature))
                return;

            var now = DateTime.UtcNow;
            ReportCombatEntryIfNeeded(creature, now);
            // Incoming hostile actions start combat for First Strike visibility without consuming the
            // landed-opening timestamp that Venatic Recovery reads when the defender retaliates.
            _lastHostileIncomingActivity[creature] = now;
        }

        private static void ReportCombatEntryIfNeeded(uint creature, DateTime now)
        {
            if (HasRecentCombatEntryActivity(creature, now))
                return;

            _firstCombatAttackConsumed.Remove(creature);
            ReportFirstStrikeCombatEntry(creature, now);
        }

        private static bool HasRecentCombatEntryActivity(uint creature, DateTime now)
        {
            return (_lastCombatActivity.TryGetValue(creature, out var lastCombatActivity) &&
                    (now - lastCombatActivity).TotalSeconds <= 30) ||
                (_lastHostileAbilityAttemptActivity.TryGetValue(creature, out var lastHostileAbilityAttempt) &&
                    (now - lastHostileAbilityAttempt).TotalSeconds <= 30) ||
                (_lastHostileIncomingActivity.TryGetValue(creature, out var lastHostileIncomingActivity) &&
                    (now - lastHostileIncomingActivity).TotalSeconds <= 30);
        }

        public static void TrackStealthOpeningWindow(uint creature)
        {
            if (!GetIsObjectValid(creature) || !IsStealthedOrInvisible(creature))
                return;

            _stealthOpeningWindows[creature] = DateTime.UtcNow.AddSeconds(6);
        }

        private static bool ConsumeStealthOpeningWindow(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return false;

            if (IsStealthedOrInvisible(creature))
                return true;

            if (!_stealthOpeningWindows.TryGetValue(creature, out var expiresAt))
                return false;

            var isActive = expiresAt >= DateTime.UtcNow;
            _stealthOpeningWindows.Remove(creature);
            return isActive;
        }

        private static bool IsStealthedOrInvisible(uint creature)
        {
            return GetActionMode(creature, ActionMode.Stealth) ||
                   GetHasEffect(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility);
        }

        private static bool GetHasEffect(uint creature, EffectTypeScript effectType, params EffectTypeScript[] otherEffectTypes)
        {
            var effect = GetFirstEffect(creature);
            while (GetIsEffectValid(effect))
            {
                var type = GetEffectType(effect);
                if (type == effectType || otherEffectTypes.Contains(type))
                    return true;

                effect = GetNextEffect(creature);
            }

            return false;
        }

        public static bool HasRecentAttackActivity(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            if (!_lastAttackActivity.TryGetValue(creature, out var lastAttack))
                return false;

            var isRecent = (DateTime.UtcNow - lastAttack).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _lastAttackActivity.Remove(creature);

            return isRecent;
        }

        public static int PrepareOpeningAutoAttack(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var idleSeconds = Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackIdleSeconds);
            if (idleSeconds <= 0)
                return 0;

            var now = DateTime.UtcNow;
            if (_lastCombatActivity.TryGetValue(attacker, out var lastActivity) &&
                (now - lastActivity).TotalSeconds < idleSeconds)
                return 0;

            TrackCombatActivity(attacker);

            var damageBonus = Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackDamageBonus);
            if (damageBonus != 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.CurrentAutoAttackDamageBonus,
                    damageBonus,
                    6,
                    StatType.CurrentAutoAttackDamageBonus);
            }

            return Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment);
        }

        private static int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return 0;

            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.RangedAutoAttackCycleCriticalRateRequiredCount);
            var criticalRate = Stat.GetStatAdjustment(attacker, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment);
            if (!IsRangedWeaponSkill(skillType) || requiredCount <= 0 || criticalRate <= 0)
                return 0;

            _autoAttackCycleCriticalCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _autoAttackCycleCriticalCounts[attacker] = count;
                return 0;
            }

            _autoAttackCycleCriticalCounts[attacker] = 0;
            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Lucky Chamber +{criticalRate}% Critical Rate"),
                    attacker,
                    false);
            }
            return criticalRate;
        }

        private static int GetLowHPCriticalRateAdjustment(uint attacker)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.LowHPCriticalRatePercentAdjustment);
            var maximumHP = GetMaxHitPoints(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(attacker) <= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        public static int GetLowHPAttackAdjustment(uint attacker)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackPercentAdjustment);
            var maximumHP = GetMaxHitPoints(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(attacker) <= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        public static int GetLowFPAttackAdjustment(uint attacker)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowFPAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.LowFPAttackPercentAdjustment);
            var maximumFP = Stat.GetMaxFP(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumFP <= 0)
                return 0;

            return Stat.GetCurrentFP(attacker) <= maximumFP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        private static int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.TargetStatusCriticalRateStatusCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetStatusCriticalRatePercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

        public static bool HasRecentDamageTarget(uint attacker, uint defender, float windowSeconds)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender || windowSeconds <= 0f)
                return false;

            if (!_recentDamageTargets.TryGetValue((attacker, defender), out var lastDamaged))
                return false;

            var isRecent = (DateTime.UtcNow - lastDamaged).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentDamageTargets.Remove((attacker, defender));

            return isRecent;
        }

        public static void TrackGuardedHit(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            _recentGuardedHits[creature] = DateTime.UtcNow;
            ApplyGuardedHitNextAttackEffects(creature);
            ApplyGuardedHitNextSkillAbilityEffects(creature);
            ApplyGuardedHitNextSkillAbilityStatusEffects(creature);
        }

        public static void TrackAvoidedAttack(uint creature, uint attacker)
        {
            if (!GetIsObjectValid(creature))
                return;

            TrackHostileDefensiveCombatEntryActivity(creature, attacker);

            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilitySkillType));
            var adjustment = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment);
            var damageBonus = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilityDamageBonus);
            var window = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilityWindowSeconds);
            GrantNextSkillAbilityStaminaCostAdjustment(creature, skillType, adjustment, window);
            GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, 0, window);

            var chance = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestore);
            var staminaRestoreCooldown = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackStaminaRestoreCooldownSeconds);
            if (chance > 0 &&
                staminaRestore > 0 &&
                Random.D100(1) <= chance &&
                TryUseStatTrigger(creature, StatType.AvoidedAttackStaminaRestore, staminaRestoreCooldown))
            {
                Stat.RestoreStamina(creature, staminaRestore);
            }

            var singleChance = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackSingleStaminaRestoreChance);
            var singleStaminaRestore = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackSingleStaminaRestore);
            if (singleChance > 0 && singleStaminaRestore > 0 && Random.D100(1) <= singleChance)
            {
                Stat.RestoreStamina(creature, singleStaminaRestore);
                StatusEffect.RemoveStatusEffectsWithStat(creature, StatType.AvoidedAttackSingleStaminaRestore, false);
            }

            ApplyAvoidedAttackAbilityUsedRangedDeflectionRefresh(creature);
            ApplyAvoidedAttackNextAutoAttackNoDelay(creature);
            ApplyAvoidedAttackAccuracy(creature);
        }

        private static void ApplyAvoidedAttackAccuracy(uint creature)
        {
            var accuracy = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackAccuracyPercentAdjustment);
            var duration = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackAccuracyDurationSeconds);
            if (accuracy == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.AccuracyPercentAdjustment,
                accuracy,
                duration,
                StatType.AvoidedAttackAccuracyPercentAdjustment);
        }

        private static void ApplyAvoidedAttackAbilityUsedRangedDeflectionRefresh(uint creature)
        {
            var duration = Stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds);
            if (duration <= 0)
                return;

            var deflection = Stat.GetStatAdjustment(
                creature,
                StatType.AbilityUsedRangedDeflection);
            var statusEffectIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityUsedRangedDeflectionStatusEffectIcon));
            if (deflection <= 0 || statusEffectIcon == EffectIconType.Invalid)
                return;

            if (StatusEffect.ApplyStatusEffect(
                    creature,
                    creature,
                    new RangedDeflectionStatusEffect(deflection, statusEffectIcon),
                    duration))
            {
                var source = Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedRangedDeflection);
                ApplyAbilityGrantedAttackDeflectionEffects(creature, source);
            }
        }

        private static void ApplyAvoidedAttackNextAutoAttackNoDelay(uint creature)
        {
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackNextAutoAttackNoDelaySkillType));
            var duration = Stat.GetStatAdjustment(
                creature,
                StatType.AvoidedAttackNextAutoAttackNoDelayDurationSeconds);

            GrantNextAutoAttackNoDelay(creature, skillType, duration);
        }

        public static void ApplyMeleeDamageTakenEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(attacker))
                return;

            ApplyMeleeDamageTakenPoisonDamage(defender, attacker);

            var chance = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenStaminaRestoreChance);
            if (chance <= 0 || Random.D100(1) > chance)
                return;

            var staminaRestore = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(defender, staminaRestore);
            }

            var evasion = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenEvasionPercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenEvasionDurationSeconds);
            if (evasion != 0 && duration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    duration,
                    StatType.MeleeDamageTakenEvasionPercentAdjustment);
            }
        }

        private static void ApplyMeleeDamageTakenPoisonDamage(uint defender, uint attacker)
        {
            var chance = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamageChance);
            var damage = Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamage);
            if (chance <= 0 || damage <= 0 || Random.D100(1) > chance)
                return;

            var scalingAbility = GetAbilityTypeFromStatPlusOne(
                Stat.GetStatAdjustment(defender, StatType.MeleeDamageTakenPoisonDamageScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                damage = AbilityEffectScaling.ScaleDirectEffect(
                    damage,
                    GetAbilityScore(defender, scalingAbility),
                    source: defender);
            }

            var appliedDamage = ApplyTriggeredDamage(defender, attacker, damage, CombatDamageType.Poison);
            if (appliedDamage <= 0)
                return;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), attacker);
        }

        public static int ApplyGuardedHitModifiers(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            bool isLandedAttack)
        {
            if (!isLandedAttack ||
                !GetIsObjectValid(defender) ||
                !GetIsObjectValid(attacker) ||
                defender == attacker ||
                damage <= 0 ||
                !damageType.IsPhysicalDamageType() ||
                !IsHostileAttackSource(defender, attacker))
                return damage;

            var guardChance = Stat.GetGuardChance(defender);
            if (guardChance <= 0 || Random.D100(1) > guardChance)
                return damage;

            var reductionPercent = GetGuardDamageReductionPercent(defender);
            var preventedDamage = Math.Min(damage, GameMath.PercentOf(damage, reductionPercent));
            var adjustedDamage = Math.Max(0, damage - preventedDamage);

            TrackGuardedHit(defender);
            StatusEffect.OnGuardedHit(defender, attacker, preventedDamage);
            ApplyGuardedHitRecovery(defender);
            ApplyGuardedHitRetaliation(attacker, defender, damage);
            ApplyGuardedHitEnmity(attacker, defender, damage);
            SendGuardedHitFeedback(defender, attacker, preventedDamage);

            return adjustedDamage;
        }

        internal static bool IsHostileAttackSource(uint defender, uint attacker)
        {
            // Preserve PvP and DM-driven testing while rejecting clearly non-hostile NPC swings.
            if (GetIsPC(attacker) || GetIsDM(attacker) || GetIsDMPossessed(attacker))
                return true;

            return GetIsReactionTypeHostile(attacker, defender) ||
                   GetIsReactionTypeHostile(defender, attacker) ||
                   GetIsEnemy(attacker, defender) ||
                   GetIsEnemy(defender, attacker);
        }

        private static void SendGuardedHitFeedback(uint defender, uint attacker, int preventedDamage)
        {
            if (GetIsPC(defender))
            {
                var feedback = BuildGuardedHitFeedback(defender, defender, attacker, preventedDamage);
                SendMessageToPC(defender, feedback);
                FloatingTextStringOnCreature(ColorToken.Combat($"Guard (-{preventedDamage})"), defender, false);
            }

            if (GetIsPC(attacker))
            {
                var feedback = BuildGuardedHitFeedback(attacker, defender, attacker, preventedDamage);
                SendMessageToPC(attacker, feedback);
            }
        }

        private static string BuildGuardedHitFeedback(uint observer, uint defender, uint attacker, int preventedDamage)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);
            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName} guards against {attackerName}'s attack, preventing {preventedDamage} damage.");
        }

        public static void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(defender))
                return;

            if (GetIsPC(defender))
            {
                var feedback = BuildIncomingCriticalHitDowngradeCombatLogMessage(defender, attacker, defender);
                SendMessageToPC(defender, feedback);
                FloatingTextStringOnCreature(ColorToken.Combat("Critical Ward"), defender, false);
            }

            if (GetIsObjectValid(attacker) &&
                attacker != defender &&
                GetIsPC(attacker))
            {
                var feedback = BuildIncomingCriticalHitDowngradeCombatLogMessage(attacker, attacker, defender);
                SendMessageToPC(attacker, feedback);
            }
        }

        private static string BuildIncomingCriticalHitDowngradeCombatLogMessage(uint observer, uint attacker, uint defender)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            if (!GetIsObjectValid(attacker) || attacker == defender)
                return ColorToken.Combat($"{defenderName}'s Critical Ward negates the critical hit.");

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName}'s Critical Ward negates {attackerName}'s critical hit.");
        }

        /// <summary>
        /// Retrieves the percentage of damage a successful Guard removes from an incoming hit,
        /// including stat adjustments and the effective minimum and maximum bounds.
        /// </summary>
        /// <param name="defender">The creature to check.</param>
        /// <returns>The damage reduction percentage applied on a guarded hit.</returns>
        public static int GetGuardDamageReductionPercent(uint defender)
        {
            var adjustment = Stat.GetStatAdjustment(defender, StatType.GuardDamageReductionPercentAdjustment);
            return Math.Clamp(
                BaseGuardDamageReductionPercent + adjustment,
                0,
                MaximumGuardDamageReductionPercent);
        }

        private static void ApplyGuardedHitRecovery(uint defender)
        {
            var staminaRestore = Stat.GetStatAdjustment(defender, StatType.GuardStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(defender, staminaRestore);
            }
        }

        private static void ApplyGuardedHitRetaliation(uint attacker, uint defender, int incomingDamage)
        {
            var skillType = GetEquippedWeaponSkillType(defender);
            var retaliationDMG = Stat.GetStatAdjustment(defender, StatType.GuardRetaliationDMG);
            var pulseDMG = Stat.GetStatAdjustment(defender, StatType.GuardedHitPulseDMG);

            // DMG is an input to the attack-versus-defense damage range, not already-resolved HP damage.
            // Resolve each retaliation before using the non-recursive triggered-damage delivery path.
            if (retaliationDMG > 0)
            {
                var retaliationDamage = ResolveGuardRetaliationDamage(
                    defender,
                    attacker,
                    retaliationDMG,
                    skillType);
                ApplyTriggeredDamage(defender, attacker, retaliationDamage, CombatDamageType.Physical, skillType);
            }

            if (pulseDMG <= 0)
                return;

            var radius = Stat.GetStatAdjustment(defender, StatType.GuardedHitPulseRadiusMeters);
            if (radius <= 0)
            {
                var pulseDamage = ResolveGuardRetaliationDamage(defender, attacker, pulseDMG, skillType);
                ApplyTriggeredDamage(defender, attacker, pulseDamage, CombatDamageType.Physical, skillType);
                return;
            }

            ApplyGuardedHitRetaliationPulse(defender, attacker, incomingDamage, pulseDMG, radius, skillType);
        }

        private static void ApplyGuardedHitRetaliationPulse(
            uint defender,
            uint originalAttacker,
            int incomingDamage,
            int dmg,
            float radius,
            SkillType skillType)
        {
            var enmityPercent = Stat.GetStatAdjustment(
                defender,
                StatType.GuardedHitPulseEnmityPercentOfIncomingDamage);
            var additionalTargetEnmity = enmityPercent > 0
                ? Math.Max(1, GameMath.PercentOf(incomingDamage, enmityPercent))
                : 0;
            var location = GetLocation(defender);
            var applied = false;

            // The creature whose hit was guarded is always affected, even when attacking from
            // beyond the nearby-enemy radius. Normal Guard handling supplies this target's Enmity.
            if (GetIsObjectValid(originalAttacker) &&
                !GetIsDead(originalAttacker) &&
                GetCurrentHitPoints(originalAttacker) > 0 &&
                GetIsReactionTypeHostile(originalAttacker, defender))
            {
                var damage = ResolveGuardRetaliationDamage(defender, originalAttacker, dmg, skillType);
                ApplyTriggeredDamage(defender, originalAttacker, damage, CombatDamageType.Physical, skillType);
                applied = true;
            }

            var target = GetFirstObjectInShape(
                Shape.Sphere,
                radius,
                location,
                true,
                SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != originalAttacker &&
                    !GetIsDead(target) &&
                    GetCurrentHitPoints(target) > 0 &&
                    GetIsReactionTypeHostile(target, defender))
                {
                    var damage = ResolveGuardRetaliationDamage(defender, target, dmg, skillType);
                    ApplyTriggeredDamage(defender, target, damage, CombatDamageType.Physical, skillType);
                    if (additionalTargetEnmity > 0)
                        Enmity.ModifyEnmity(defender, target, additionalTargetEnmity);

                    applied = true;
                }

                target = GetNextObjectInShape(
                    Shape.Sphere,
                    radius,
                    location,
                    true,
                    SWLOR.NWN.API.NWScript.Enum.ObjectType.Creature);
            }

            if (applied && GetIsPC(defender))
                FloatingTextStringOnCreature(ColorToken.Combat("Retaliation Pulse"), defender, false);
        }

        private static int ResolveGuardRetaliationDamage(
            uint source,
            uint target,
            int dmg,
            SkillType skillType)
        {
            if (!GetIsObjectValid(source) || !GetIsObjectValid(target) || dmg <= 0)
                return 0;

            const CombatDamageType damageType = CombatDamageType.Physical;
            var damageAbility = GetGuardRetaliationDamageAbility(source, skillType);
            var attack = Stat.GetAttack(source, damageAbility, skillType);
            attack = ApplyTargetStatusAttackModifiers(source, target, attack, skillType);
            var attackStat = GetAbilityScore(source, damageAbility);
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = Stat.GetDefense(target, damageType, defenseAbility);
            defense = ApplyStatusSourceDefenseModifiers(source, target, defense);
            defense = ApplyIncomingPhysicalToForceDefenseConversion(
                target,
                damageType,
                defense,
                () => ApplyStatusSourceDefenseModifiers(
                    source,
                    target,
                    Stat.GetDefense(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            var defenderStat = GetAbilityScore(target, defenseAbility);

            return CalculateDamage(
                attack,
                dmg,
                attackStat,
                defense,
                defenderStat,
                0);
        }

        private static AbilityType GetGuardRetaliationDamageAbility(uint defender, SkillType skillType)
        {
            var weapon = GetRelevantSkillWeapon(defender, skillType);
            if (!GetIsObjectValid(weapon))
                return AbilityType.Might;

            var ability = GetWeaponDamageAbilityType(defender, GetBaseItemType(weapon));
            return ability == AbilityType.Invalid
                ? AbilityType.Might
                : ability;
        }

        private static void ApplyGuardedHitEnmity(uint attacker, uint defender, int damage)
        {
            var enmity = Math.Max(1, damage);
            var percentAdjustment = Stat.GetStatAdjustment(defender, StatType.GuardEnmityPercentAdjustment);
            if (percentAdjustment != 0)
            {
                enmity = Math.Max(1, (int)Math.Ceiling(enmity * ((100 + percentAdjustment) / 100f)));
            }

            Enmity.ModifyEnmity(defender, attacker, enmity);
        }

        public static bool HasRecentGuardedHit(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            if (!_recentGuardedHits.TryGetValue(creature, out var lastGuardedHit))
                return false;

            var isRecent = (DateTime.UtcNow - lastGuardedHit).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentGuardedHits.Remove(creature);

            return isRecent;
        }

        public static void TrackDeflection(uint creature, DeflectionSource source)
        {
            if (!GetIsObjectValid(creature) || source == DeflectionSource.None)
                return;

            _recentDeflections[(creature, source)] = DateTime.UtcNow;
            ApplyDeflectionNearbyAllyGuard(creature, source);
        }

        private static void ApplyDeflectionNearbyAllyGuard(uint creature, DeflectionSource source)
        {
            if (Stat.GetStatTypeDeflectionSource(StatType.DeflectionNearbyAllyGuard) != source)
                return;

            var guard = Stat.GetStatAdjustment(creature, StatType.DeflectionNearbyAllyGuard);
            var duration = Stat.GetStatAdjustment(creature, StatType.DeflectionNearbyAllyGuardDurationSeconds);
            if (guard <= 0 || duration <= 0)
                return;

            var ally = AbilityTargeting.GetFriendlyTargetsNearLocation(creature, GetLocation(creature), 5f, false)
                .Where(target => target != creature)
                .OrderBy(target => GetDistanceBetween(creature, target))
                .FirstOrDefault();
            if (!GetIsObjectValid(ally))
                return;

            TemporaryStatModifier.Replace(
                ally,
                StatType.Guard,
                guard,
                duration,
                StatType.DeflectionNearbyAllyGuard);
        }

        public static bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || source == DeflectionSource.None || windowSeconds <= 0f)
                return false;

            var key = (creature, source);
            if (!_recentDeflections.TryGetValue(key, out var lastDeflection))
                return false;

            var isRecent = (DateTime.UtcNow - lastDeflection).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentDeflections.Remove(key);

            return isRecent;
        }

        private static void ApplyGuardedHitNextSkillAbilityEffects(uint creature)
        {
            var primary = GetGuardedHitNextSkillAbilityBonuses(
                creature,
                StatType.GuardedHitNextSkillAbilitySkillType,
                StatType.GuardedHitNextSkillAbilityDamageBonus,
                StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.GuardedHitNextSkillAbilityWindowSeconds);
            var secondary = GetGuardedHitNextSkillAbilityBonuses(
                creature,
                StatType.GuardedHitSecondaryNextSkillAbilitySkillType,
                StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus,
                StatType.GuardedHitSecondaryNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.GuardedHitSecondaryNextSkillAbilityWindowSeconds);

            var selected = primary;
            if (primary.SkillType == SkillType.Invalid)
            {
                selected = secondary;
            }
            // Guarded-hit bonuses sharing a skill are one window and intentionally stack their payloads.
            // Keeping their selector/window providers independent prevents integer selector values and
            // durations from being accidentally added by Stat.GetStatAdjustment.
            else if (secondary.SkillType == SkillType.Invalid || secondary.SkillType == primary.SkillType)
            {
                selected = (
                    primary.SkillType,
                    primary.DamageBonus + secondary.DamageBonus,
                    primary.CriticalRate + secondary.CriticalRate,
                    Math.Max(primary.Window, secondary.Window));
            }
            else
            {
                // Different weapon selectors cannot both be consumed by one "next skill ability" slot.
                // Prefer the currently equipped weapon's channel; otherwise preserve the primary channel.
                var equippedSkillType = GetEquippedWeaponSkillType(creature);
                if (secondary.SkillType == equippedSkillType)
                    selected = secondary;
            }

            GrantNextSkillAbilityBonuses(
                creature,
                selected.SkillType,
                selected.DamageBonus,
                selected.CriticalRate,
                selected.Window);

            if (selected.SkillType != SkillType.Invalid &&
                selected.Window > 0 &&
                (selected.DamageBonus != 0 || selected.CriticalRate != 0) &&
                GetIsPC(creature))
            {
                var criticalText = selected.CriticalRate != 0
                    ? $", +{selected.CriticalRate}% Crit"
                    : string.Empty;
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Counter Ready: +{selected.DamageBonus} DMG{criticalText}"),
                    creature,
                    false);
            }
        }

        private static (SkillType SkillType, int DamageBonus, int CriticalRate, int Window)
            GetGuardedHitNextSkillAbilityBonuses(
            uint creature,
            StatType skillTypeStat,
            StatType damageBonusStat,
            StatType criticalRateStat,
            StatType windowStat)
        {
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(creature, skillTypeStat));
            var criticalRate = Stat.GetStatAdjustment(creature, criticalRateStat);
            var damageBonus = Stat.GetStatAdjustment(creature, damageBonusStat);
            var window = Stat.GetStatAdjustment(creature, windowStat);
            return (skillType, damageBonus, criticalRate, window);
        }

        private static void ApplyGuardedHitNextSkillAbilityStatusEffects(uint creature)
        {
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType));
            var duration = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            var damageBonus = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityExposedDamageBonus);
            if (skillType == SkillType.Invalid || duration <= 0 && damageBonus <= 0)
                return;

            var window = Math.Max(1, duration);
            TemporaryStatModifier.Replace(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                (int)skillType,
                window,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);

            if (duration > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds,
                    duration,
                    window,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            }

            if (damageBonus > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                    damageBonus,
                    window,
                    StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            }
        }

        private static int ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(uint creature, SkillType skillType)
        {
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
        }

        private static void ApplyRecentDamageTargetHitEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            var chance = Stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance);
            var window = Stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetWindowSeconds);
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType));
            if (chance <= 0 || window <= 0 || skillType == SkillType.Invalid)
                return;

            if (!_recentDamageTargets.TryGetValue((defender, attacker), out var lastDamaged) ||
                (DateTime.UtcNow - lastDamaged).TotalSeconds > window ||
                Random.D100(1) > chance)
                return;

            GrantNextAbilityNoDelay(defender, skillType, window);
        }

        private static bool DidCrossHPThreshold(uint creature, int damage, int thresholdPercent)
        {
            var maxHP = GetMaxHitPoints(creature);
            var currentHP = GetCurrentHitPoints(creature);
            if (maxHP <= 0 || currentHP <= 0)
                return false;

            var thresholdHP = maxHP * (thresholdPercent / 100f);
            var previousHP = currentHP + damage;
            return previousHP >= thresholdHP && currentHP < thresholdHP;
        }

        private static void HealFromMaxHP(uint creature, int percent)
        {
            if (percent <= 0)
                return;

            var amount = GameMath.PercentOf(GetMaxHitPoints(creature), percent);
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
        }

        private static int ApplyOutgoingDamageModifier(uint attacker, int damage)
        {
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.DamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private static int ApplyWeaponAndForceDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!IsWeaponOrForceDamage(skillType, damageType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.WeaponAndForceDamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyDamageTypeDealtModifiers(uint attacker, int damage, CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var adjustment = damageType == CombatDamageType.Poison
                ? Stat.GetStatAdjustment(attacker, StatType.PoisonDamageDealtPercentAdjustment)
                : 0;
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private static int ApplyTargetLowHPDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamageThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamagePercentAdjustment);

            if (threshold > 0 && adjustment != 0)
            {
                damage = ApplyTargetHPDamageAdjustment(
                    damage,
                    GetCurrentHitPoints(defender),
                    GetMaxHitPoints(defender),
                    threshold,
                    adjustment);
            }

            return ApplyTargetLowHPStatusDamageModifier(attacker, defender, damage);
        }

        public static int ApplyTargetHPDamageAdjustment(
            int damage,
            int currentHP,
            int maxHP,
            int thresholdPercent,
            int adjustmentPercent)
        {
            if (damage <= 0 ||
                maxHP <= 0 ||
                thresholdPercent <= 0 ||
                adjustmentPercent == 0 ||
                currentHP > maxHP * (thresholdPercent / 100f))
            {
                return damage;
            }

            return ApplyPercentDamageAdjustment(damage, adjustmentPercent);
        }

        private static int ApplyTargetLowHPStatusDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPStatusDamageThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPStatusDamagePercentAdjustment);
            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.TargetLowHPStatusDamageStatusCategory));

            if (threshold <= 0 || adjustment == 0 || category == 0 || !TargetHasAnyStatusEffectCategory(defender, category))
                return damage;

            var maxHP = GetMaxHitPoints(defender);
            if (maxHP <= 0 || GetCurrentHitPoints(defender) > maxHP * (threshold / 100f))
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int GetNearbyStatusTargetAttackAdjustment(uint creature)
        {
            var percentPerTarget = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackPercentPerTarget);
            var radius = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackRadiusMeters);
            var maximum = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackPercentMaximum);
            var categoryValue = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackStatusCategory);
            if (percentPerTarget == 0 || radius <= 0 || categoryValue <= 0)
                return 0;

            var category = (StatusEffectCategory)categoryValue;
            var count = 0;
            var location = GetLocation(creature);
            var target = GetFirstObjectInShape(Shape.Sphere, radius, location, true);
            while (GetIsObjectValid(target))
            {
                if (target != creature &&
                    GetIsReactionTypeHostile(target, creature) &&
                    StatusEffect.HasStatusEffectCategory(target, category))
                {
                    count++;
                }

                target = GetNextObjectInShape(Shape.Sphere, radius, location, true);
            }

            var adjustment = count * percentPerTarget;
            return maximum > 0
                ? Math.Min(maximum, adjustment)
                : adjustment;
        }

        private static int ApplyTargetStatusDamageModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            bool isAbilityDamage,
            bool canApplyRandomFlatBonuses)
        {
            var adjustment = 0;
            adjustment += GetStatusSourceStatAdjustment(
                attacker,
                defender,
                StatType.DamageToStatusSourcePercentAdjustment);
            adjustment += GetStatusSourceStatAdjustment(
                defender,
                attacker,
                StatType.DamageTakenFromStatusSourcePercentAdjustment);
            adjustment += GetStatusSourcePartyStatAdjustment(
                defender,
                attacker,
                StatType.DamageTakenFromStatusSourcePartyPercentAdjustment);

            if (StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect)))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToSunderedTargetPercentAdjustment);

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToBleedingTargetPercentAdjustment);
                if (isAbilityDamage &&
                    SkillTypeMatches(
                        skillType,
                        GetSkillTypeFromStat(Stat.GetStatAdjustment(
                            attacker,
                            StatType.AbilityDamageToBleedingTargetSkillType))))
                {
                    damage += Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToBleedingTargetBonus);
                }
            }

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Debuff))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToDebuffedTargetPercentAdjustment);

            adjustment += GetDamageToSourceAppliedStatusTargetAdjustment(attacker, defender);
            if (isAbilityDamage)
            {
                adjustment += GetAbilityDamageToSourceAppliedStatusTargetAdjustment(attacker, defender, skillType);
                damage += GetAbilityDamageToSourceAppliedStatusTargetBonus(attacker, defender);
            }

            if (isAbilityDamage &&
                skillType == SkillType.Katar &&
                StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect), typeof(DisorientedStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment);
            }

            if (canApplyRandomFlatBonuses &&
                skillType == SkillType.Katar &&
                StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                var flatBonusChance = Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedTargetFlatBonusChance);
                var flatBonus = Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedTargetFlatBonus);
                if (flatBonusChance > 0 && flatBonus != 0 && Random.D100(1) <= flatBonusChance)
                {
                    damage += flatBonus;
                }
            }

            if (StatusEffect.HasStatusEffect(defender, typeof(WeakenedStatusEffect), typeof(HamstringStatusEffect)))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment);

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Control))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToControlTargetPercentAdjustment);

            adjustment += GetSuppressionDamageDealtToOtherTargetsAdjustment(attacker, defender);

            if (isAbilityDamage &&
                IsCurrentFPAndStaminaAtOrAbovePercent(
                    attacker,
                    Stat.GetStatAdjustment(
                        attacker,
                        StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent)))
            {
                adjustment += Stat.GetStatAdjustment(
                    attacker,
                    StatType.HighFPAndStaminaAbilityDamagePercentAdjustment);
            }

            if (skillType == SkillType.Rifle &&
                StatusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect), typeof(DazedStatusEffect), typeof(TranquilizedStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToDisorientedDazedTargetPercentAdjustment);
            }

            if (damageType.IsPhysicalDamageType())
            {
                adjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalDamageTakenPercentAdjustment);
            }

            if (isAbilityDamage && damageType.IsPhysicalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalAbilityDamageTakenPercentAdjustment);

            if (damageType == CombatDamageType.Force)
            {
                adjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenPercentAdjustment);
            }

            if (damageType.IsPhysicalDamageType() && IsRangedDamageSkill(skillType))
                adjustment += Stat.GetStatAdjustment(defender, StatType.RangedPhysicalDamageTakenPercentAdjustment);

            if (skillType == SkillType.Throwing)
                adjustment += Stat.GetStatAdjustment(defender, StatType.ThrowingDamageTakenPercentAdjustment);

            if (IsRangedWeaponSkill(skillType) && IsNearbyTargetWithinDistance(attacker, defender, 8f))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.RangedDamageToNearbyTargetPercentAdjustment);

            if (skillType == SkillType.Pistol &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(DisorientedStatusEffect),
                    typeof(KnockdownStatusEffect),
                    typeof(TranquilizedStatusEffect)))
            {
                damage += Stat.GetStatAdjustment(
                    attacker,
                    StatType.PistolDamageToDisorientedKnockdownOrTranquilizedTargetBonus);
            }

            if (isAbilityDamage &&
                skillType == SkillType.Staff &&
                StatusEffect.HasStatusEffect(defender, typeof(KnockdownStatusEffect), typeof(BlindStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToKnockdownOrBlindTargetPercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyTwinBladeAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isSingleTargetAbility,
            bool isAreaAbility)
        {
            if (damage <= 0 || skillType != SkillType.TwinBlade)
                return damage;

            var adjustment = 0;
            if (isSingleTargetAbility)
            {
                adjustment += Stat.GetStatAdjustment(
                    attacker,
                    StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment);
            }

            if (isAreaAbility)
            {
                adjustment += Stat.GetStatAdjustment(
                    attacker,
                    StatType.TwinBladeAreaAbilityDamagePercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyThrowingAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || skillType != SkillType.Throwing || !isAreaAbility)
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.ThrowingAreaAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyPhysicalAbilityShapeDamageModifier(
            uint attacker,
            CombatDamageType damageType,
            int damage,
            bool isSingleTargetAbility)
        {
            if (damage <= 0 || !isSingleTargetAbility || !damageType.IsPhysicalDamageType())
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplySkillAreaAbilityDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || !isAreaAbility)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAreaAbilityDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAreaAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyAreaAbilityAfterDeflectionDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || !isAreaAbility)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AreaAbilityAfterDeflectionDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var window = Stat.GetStatAdjustment(attacker, StatType.AreaAbilityAfterDeflectionWindowSeconds);
            var requiredSource = Stat.GetStatTypeDeflectionSource(
                StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment);
            if (window <= 0 ||
                requiredSource == DeflectionSource.None ||
                !HasRecentDeflection(attacker, requiredSource, window))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment);
            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private static int ApplyPercentDamageAdjustment(int damage, int adjustment)
        {
            if (damage <= 0 || adjustment == 0)
                return damage;

            adjustment = Math.Max(adjustment, -MaximumNormalDamageReductionPercent);
            return Math.Max(1, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        public static bool HasDamageImmunity(uint defender, CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return false;

            return damageType.IsPhysicalDamageType() &&
                   Stat.GetStatAdjustment(defender, StatType.PhysicalDamageImmunity) > 0;
        }

        private static int ApplyRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageSkillType));
            var autoAttackOnly = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageAutoAttackOnly) > 0;
            var percentPerHit = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentPerHit);
            var maxPercent = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentMax);
            var bonusPerHit = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusPerHit);
            var maxBonus = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusMax);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageDurationSeconds);
            var hasPercentBonus = percentPerHit > 0 && maxPercent > 0;
            var hasFlatBonus = bonusPerHit > 0 && maxBonus > 0;
            if (autoAttackOnly && isAbilityDamage ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                (!hasPercentBonus && !hasFlatBonus))
            {
                _repeatedTargetDamageStates.Remove(attacker);
                return damage;
            }

            var now = DateTime.UtcNow;
            if (!_repeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0 && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = 1;
            if (hasPercentBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxPercent / (float)percentPerHit));
            if (hasFlatBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));

            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _repeatedTargetDamageStates[attacker] = state;

            if (hasPercentBonus)
            {
                var adjustment = Math.Min(maxPercent, state.Stacks * percentPerHit);
                damage += (int)Math.Ceiling(damage * (adjustment / 100f));
            }

            if (hasFlatBonus)
            {
                damage += Math.Min(maxBonus, state.Stacks * bonusPerHit);
            }

            return damage;
        }

        private static int ApplyMeleeRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var bonusPerHit = Stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusPerHit);
            var maxBonus = Stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusMax);
            var statusEffectIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.MeleeRepeatedTargetDamageStatusEffectIcon));
            if (isAbilityDamage ||
                !IsMeleeWeaponSkill(skillType) ||
                bonusPerHit <= 0 ||
                maxBonus <= 0)
            {
                _meleeRepeatedTargetDamageStates.Remove(attacker);
                MeleeRepeatedTargetDamageStatusEffect.Refresh(attacker, 0, statusEffectIcon);
                return damage;
            }

            if (!_meleeRepeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = Math.Max(1, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));
            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = DateTime.UtcNow;
            _meleeRepeatedTargetDamageStates[attacker] = state;

            MeleeRepeatedTargetDamageStatusEffect.Refresh(attacker, state.Stacks, statusEffectIcon);
            return damage + Math.Min(maxBonus, state.Stacks * bonusPerHit);
        }

        /// <summary>
        /// Cross-skill ranged sibling of the melee modifier above: "each consecutive ranged hit"
        /// builds and benefits regardless of which ranged weapon dealt it, so switching from rifle
        /// to pistol keeps the stacks instead of clearing them. Unlike the melee variant, ability
        /// hits count too - the wording is "hit", not "attack" - and stacks expire on their own
        /// timer.
        /// </summary>
        private static int ApplyRangedRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var bonusPerHit = Stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusPerHit);
            var maxBonus = Stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusMax);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageDurationSeconds);
            if (!IsRangedWeaponSkill(skillType) ||
                bonusPerHit <= 0 ||
                maxBonus <= 0)
            {
                _rangedRepeatedTargetDamageStates.Remove(attacker);
                return damage;
            }

            var now = DateTime.UtcNow;
            if (!_rangedRepeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0 && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = Math.Max(1, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));
            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _rangedRepeatedTargetDamageStates[attacker] = state;

            return damage + Math.Min(maxBonus, state.Stacks * bonusPerHit);
        }

        private static void ApplySameTargetPressureDamageEffects(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            var buildSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureBuildSkillType));
            var buildSeconds = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureBuildSeconds);
            var graceSeconds = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureGraceSeconds);
            var readyDurationSeconds = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureReadyDurationSeconds);
            var damageBonus = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus);
            if (buildSkillType == SkillType.Invalid ||
                buildSeconds <= 0 ||
                graceSeconds <= 0 ||
                readyDurationSeconds <= 0 ||
                damageBonus <= 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (_sameTargetPressureStates.TryGetValue(attacker, out var state) &&
                state.ReadyUntil != default)
            {
                if (IsSameTargetPressureReadyStateActive(attacker, state, now))
                    return;

                ClearSameTargetPressureState(attacker);
                state = null;
            }

            if (!IsWeaponSkillType(skillType))
                return;

            if (!SkillTypeMatches(skillType, buildSkillType))
            {
                if (state != null && state.Target != defender)
                    ClearSameTargetPressureState(attacker);

                return;
            }

            if (state == null || state.Target != defender)
            {
                state = CreateSameTargetPressureBuildState(defender, now);
            }
            else if ((now - state.LastBuildHitAt).TotalSeconds > graceSeconds)
            {
                state = CreateSameTargetPressureBuildState(defender, now);
            }
            else
            {
                state.LastBuildHitAt = now;
            }

            if ((now - state.StartedAt).TotalSeconds >= buildSeconds)
            {
                ReadySameTargetPressure(attacker, state, now, readyDurationSeconds);
                return;
            }

            _sameTargetPressureStates[attacker] = state;
        }

        public static int GetSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType) ||
                !TryGetReadySameTargetPressureState(attacker, target, out _))
            {
                return 0;
            }

            return Math.Max(0, Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus));
        }

        public static void ConsumeSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 ||
                GetSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType) <= 0)
            {
                return;
            }

            ClearSameTargetPressureState(attacker);

            if (GetIsPC(attacker))
            {
                var bonus = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus);
                FloatingTextStringOnCreature(ColorToken.Combat($"Spotter's Rhythm (+{bonus} DMG)"), attacker, false);
            }
        }

        private static SameTargetPressureState CreateSameTargetPressureBuildState(uint target, DateTime now)
        {
            return new SameTargetPressureState
            {
                Target = target,
                StartedAt = now,
                LastBuildHitAt = now,
                ReadyUntil = default
            };
        }

        private static void ReadySameTargetPressure(
            uint attacker,
            SameTargetPressureState state,
            DateTime now,
            int readyDurationSeconds)
        {
            state.ReadyUntil = now.AddSeconds(readyDurationSeconds);
            _sameTargetPressureStates[attacker] = state;

            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                typeof(SpottersRhythmStatusEffect),
                readyDurationSeconds);

            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(ColorToken.Combat("Spotter's Rhythm"), attacker, false);
            }
        }

        private static bool TryGetReadySameTargetPressureState(
            uint attacker,
            uint target,
            out SameTargetPressureState state)
        {
            state = null;
            if (!_sameTargetPressureStates.TryGetValue(attacker, out var existing) ||
                existing.ReadyUntil == default)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            if (!IsSameTargetPressureReadyStateActive(attacker, existing, now))
            {
                ClearSameTargetPressureState(attacker);
                return false;
            }

            if (existing.Target != target)
                return false;

            state = existing;
            return true;
        }

        private static bool IsSameTargetPressureReadyStateActive(
            uint attacker,
            SameTargetPressureState state,
            DateTime now)
        {
            return state.ReadyUntil > now &&
                   StatusEffect.HasStatusEffect(attacker, typeof(SpottersRhythmStatusEffect), attacker);
        }

        private static void ClearSameTargetPressureState(uint attacker)
        {
            _sameTargetPressureStates.Remove(attacker);
            StatusEffect.RemoveStatusEffect(
                attacker,
                typeof(SpottersRhythmStatusEffect),
                attacker,
                false);
        }

        public static int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense)
        {
            if (defense <= 0)
                return defense;

            var adjustment = GetStatusSourceStatAdjustment(
                defender,
                attacker,
                StatType.DefenseAgainstStatusSourcePercentAdjustment);

            return adjustment == 0
                ? defense
                : Math.Max(0, defense + (int)Math.Ceiling(defense * (adjustment / 100f)));
        }

        /// <summary>
        /// First half of Saber Ward / Aegis Eternal conversion: give the converted share its Force Defense
        /// mitigation. The physical hit's damage roll uses a defense value blended between the defender's
        /// Physical Defense and Force Defense by the conversion percent, so the converted share is mitigated
        /// as Force. The single damage roll is RNG-based and consumes stateful modifiers, so it cannot be run
        /// twice per hit; blending the defense is how the Force-Defense mitigation is applied without a second
        /// roll. The re-typing itself — removing the converted share from the physical hit and dealing it as a
        /// real Force instance (Force resistance, combat-log visibility) — is done afterward by
        /// <see cref="ApplyIncomingPhysicalToForceConversion"/>. The percentage is read from
        /// <see cref="StatType.IncomingPhysicalToForceConversionPercent"/> on the defender, and only
        /// physical-category damage is affected. The Force Defense value is resolved lazily because the
        /// auto-attack and ability damage paths look it up through different (native vs managed) helpers.
        /// </summary>
        public static int ApplyIncomingPhysicalToForceDefenseConversion(
            uint defender,
            CombatDamageType damageType,
            int physicalDefense,
            Func<int> forceDefenseProvider)
        {
            if (!damageType.IsPhysicalDamageType())
                return physicalDefense;

            var conversionPercent = Stat.GetStatAdjustment(defender, StatType.IncomingPhysicalToForceConversionPercent);
            if (conversionPercent <= 0)
                return physicalDefense;

            conversionPercent = Math.Clamp(conversionPercent, 0, 100);
            var forceDefense = forceDefenseProvider();
            return (physicalDefense * (100 - conversionPercent) + forceDefense * conversionPercent) / 100;
        }

        /// <summary>
        /// The pure split math for <see cref="ApplyIncomingPhysicalToForceConversion"/>: how much of an
        /// incoming physical hit is re-typed to Force at the given conversion percent. Rounded to the
        /// nearest point (away from zero) and never more than the physical damage available.
        /// </summary>
        public static int GetIncomingPhysicalToForceConversionPortion(int physicalDamage, int conversionPercent)
        {
            if (physicalDamage <= 0)
                return 0;

            conversionPercent = Math.Clamp(conversionPercent, 0, 100);
            if (conversionPercent <= 0)
                return 0;

            var forcePortion = (int)Math.Round(physicalDamage * (conversionPercent / 100f), MidpointRounding.AwayFromZero);
            return Math.Clamp(forcePortion, 0, physicalDamage);
        }

        /// <summary>
        /// Saber Ward (and Aegis Eternal) re-type a percentage of an incoming physical hit into a real
        /// Force damage instance. The converted share's Force Defense mitigation is already reflected in
        /// <paramref name="physicalDamage"/> because <see cref="ApplyIncomingPhysicalToForceDefenseConversion"/>
        /// blends the defense used for the damage roll toward Force Defense by the same percent. This removes
        /// the converted share from <paramref name="physicalDamage"/> (so it is not also dealt as physical) and
        /// deals it as Force damage, so it is reduced by the defender's Force resistance and shown as Force in
        /// the combat log. Runs before the physical-resistance stage; the Force portion is routed through
        /// <see cref="ApplyTriggeredDamage"/>, which applies Force resistance, the Force-specific Leadership
        /// channel, and the remaining damage-taken pipeline.
        /// Returns the pre-resistance Force amount split off (0 when nothing converts).
        /// </summary>
        public static int ApplyIncomingPhysicalToForceConversion(
            uint attacker,
            uint defender,
            CombatDamageType damageType,
            ref int physicalDamage)
        {
            if (physicalDamage <= 0 || !damageType.IsPhysicalDamageType())
                return 0;

            var conversionPercent = Stat.GetStatAdjustment(defender, StatType.IncomingPhysicalToForceConversionPercent);
            var forcePortion = GetIncomingPhysicalToForceConversionPortion(physicalDamage, conversionPercent);
            if (forcePortion <= 0)
                return 0;

            physicalDamage -= forcePortion;

            // This method runs inside the native GetDamageRoll hook and the ability damage paths, i.e. while
            // the current attack's own damage has not yet resolved. Applying a fully dispatched damage event
            // (ApplyTriggeredDamage) synchronously here re-enters the engine's OnCreatureDamaged / AI / enmity
            // chain mid-attack and clobbers shared combat state (GetLastDamager / OBJECT_SELF), which caused
            // runaway reflect cascades and mis-targeted reflects with effects such as Blazing Spikes. Defer the
            // Force portion to the next frame so it lands as a clean, separate damage instance off the attack's
            // stack; the physical carve above stays synchronous so the physical hit is correctly reduced.
            DelayCommand(0.0f, () =>
            {
                if (GetIsObjectValid(attacker) && GetIsObjectValid(defender))
                    ApplyTriggeredDamage(
                        attacker,
                        defender,
                        forcePortion,
                        CombatDamageType.Force,
                        typedLeadershipReductionAlreadyApplied: false);
            });
            return forcePortion;
        }

        public static int ApplyStatusSourceAccuracyModifiers(uint attacker, uint defender, int accuracy)
        {
            if (accuracy <= 0)
                return accuracy;

            var adjustment = GetStatusSourceStatAdjustment(
                attacker,
                defender,
                StatType.AccuracyToStatusSourcePercentAdjustment);

            return adjustment == 0
                ? accuracy
                : Math.Max(1, accuracy + (int)Math.Ceiling(accuracy * (adjustment / 100f)));
        }

        private static bool IsNearbyTargetWithinDistance(uint attacker, uint defender, float distance)
        {
            return GetIsObjectValid(attacker) &&
                   GetIsObjectValid(defender) &&
                   GetArea(attacker) == GetArea(defender) &&
                   GetDistanceBetween(attacker, defender) <= distance;
        }

        public static bool IsRangedDamageSkill(SkillType skillType)
        {
            return skillType == SkillType.Pistol ||
                   skillType == SkillType.Rifle ||
                   skillType == SkillType.Throwing ||
                   skillType == SkillType.Devices;
        }

        public static bool IsRangedWeaponSkill(SkillType skillType)
        {
            return skillType == SkillType.Pistol ||
                   skillType == SkillType.Rifle ||
                   skillType == SkillType.Throwing;
        }

        public static float GetWeaponEngagementRange(SkillType skillType)
        {
            return IsRangedWeaponSkill(skillType)
                ? RangedWeaponEngagementRange
                : MeleeWeaponEngagementRange;
        }

        public static bool IsMeleeWeaponSkill(SkillType skillType)
        {
            return IsWeaponSkillType(skillType) && !IsRangedWeaponSkill(skillType);
        }

        private static int GetStatusSourceStatAdjustment(uint creature, uint source, StatType statType)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(source))
                return 0;

            var adjustment = 0;
            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                if (effect.Source != source)
                    continue;

                if (effect.StatGroup.Stats.TryGetValue(statType, out var value))
                    adjustment += value;
            }

            return adjustment;
        }

        private static int GetStatusSourcePartyStatAdjustment(uint creature, uint attacker, StatType statType)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(attacker))
                return 0;

            var adjustment = 0;
            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                if (!GetIsObjectValid(effect.Source) ||
                    (effect.Source != attacker && !Party.IsInParty(effect.Source, attacker)))
                    continue;

                if (effect.StatGroup.Stats.TryGetValue(statType, out var value))
                    adjustment += value;
            }

            return adjustment;
        }

        internal static bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
        {
            return TryUseStatTrigger(creature, statType, TimeSpan.FromSeconds(cooldownSeconds));
        }

        internal static bool TryUseStatTrigger(uint creature, StatType statType, TimeSpan cooldown)
        {
            if (cooldown <= TimeSpan.Zero)
                return true;

            var key = (creature, statType);
            var now = DateTime.UtcNow;
            if (_statTriggerCooldowns.TryGetValue(key, out var nextAvailable) && nextAvailable > now)
                return false;

            _statTriggerCooldowns[key] = now.Add(cooldown);
            return true;
        }

        private static void RemoveStatTriggerCooldowns(uint creature)
        {
            foreach (var key in _statTriggerCooldowns.Keys.Where(x => x.Item1 == creature).ToList())
            {
                _statTriggerCooldowns.Remove(key);
            }

            foreach (var key in _recentDamageTargets.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _recentDamageTargets.Remove(key);
            }

            _recentDamageTaken.Remove(creature);
            _recentGuardedHits.Remove(creature);
            foreach (var key in _recentDeflections.Keys.Where(x => x.Creature == creature).ToList())
            {
                _recentDeflections.Remove(key);
            }
            _lastCombatActivity.Remove(creature);
            _lastHostileAbilityAttemptActivity.Remove(creature);
            _lastHostileIncomingActivity.Remove(creature);
            _firstCombatAttackConsumed.Remove(creature);
            _lastAttackActivity.Remove(creature);
            _lastCombatAbilityUse.Remove(creature);
            foreach (var key in _pendingSuppressionAbilityUses.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _pendingSuppressionAbilityUses.Remove(key);
            }

            _hostileAbilitySequenceStates.Remove(creature);
            _criticalHitSequenceStates.Remove(creature);
            _firstHostileAbilityHitCounts.Remove(creature);
            foreach (var key in _sameTargetHostileAbilityHitCounts.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _sameTargetHostileAbilityHitCounts.Remove(key);
            }

            _autoAttackCycleCounts.Remove(creature);
            _autoAttackCycleCriticalCounts.Remove(creature);
            foreach (var key in _sourceStatusAutoAttackCycleCounts.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _sourceStatusAutoAttackCycleCounts.Remove(key);
            }

            _stealthOpeningWindows.Remove(creature);
            foreach (var key in _abilityStaminaCosts.Keys.Where(x => x.Creature == creature).ToList())
            {
                _abilityStaminaCosts.Remove(key);
            }
            foreach (var key in _areaAbilityTargetHitSequences.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _areaAbilityTargetHitSequences.Remove(key);
            }

            _attackSwingDebts.Remove(creature);
            _attackSwingDebtsWithoutLimitedReduction.Remove(creature);
            _repeatedTargetDamageStates.Remove(creature);
            _meleeRepeatedTargetDamageStates.Remove(creature);
            _rangedRepeatedTargetDamageStates.Remove(creature);
            _meleeAutoAttackCycleCounts.Remove(creature);
            ClearSameTargetPressureState(creature);
            foreach (var pressureState in _sameTargetPressureStates.Where(x => x.Value.Target == creature).Select(x => x.Key).ToList())
            {
                ClearSameTargetPressureState(pressureState);
            }
            TemporaryStatModifier.Clear(creature);
        }

        public static void ApplyAbilityActivatedEffects(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || ability == null || summary == null)
                return;

            ApplyAbilityUsedRecastReduction(activator, ability);
            ApplyAbilityUsedNextSkillAutoAttackDamageBonus(activator, ability);
            ApplyAbilityUsedNextSkillFPCostAdjustment(activator, ability);
            ApplyAbilityUsedMasterAbilityHitChance(activator);
            ApplyForceFPCostActivatedEffects(activator, ability);

            var skillType = ResolveActivatedAbilitySkillType(activator, ability, summary);
            var isSingleTargetAbility = summary.IsSingleTargetAbility ||
                summary.SkillType == SkillType.Invalid &&
                ability.IsHostileAbility &&
                ability.IsSingleTargetAbility;

            ApplyAbilityUsedSkillEvasion(activator, ability);
            ApplyHostileAbilityUsedEvasion(activator, ability, skillType);
            ApplyCostlyAbilityUsedEvasion(activator, ability, skillType);
            ApplyAbilityUsedSkillRangedDeflection(activator, ability);
            ApplyAbilityUsedMovementSpeed(activator, ability, skillType);
            ApplyAbilityUsedSkillAttackDeflection(activator, ability);
            ApplyAbilityUsedPerkCategoryAttackDeflection(activator, ability);
            ApplySingleTargetAbilityUsedAttackDeflection(activator, ability, isSingleTargetAbility);
            ApplyAreaAbilityUsedEvasion(activator, ability, skillType);
            ApplyHostileAbilityForceAttack(activator, ability);
            ApplyHostileAbilityFPSpendForceAttack(activator, ability);
            ApplyAbilityUsedNearbyAllyDefense(activator);
            ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(activator, ability);
            ApplyAbilityUsedPerkCategorySelfDefense(activator, ability);
            ApplyHostileAbilityUsedAttackAdjustment(activator, ability);
            ApplyAbilityActivatedRiders(activator, target, ability, skillType);
            ApplyHostileAbilitySequenceEffects(activator, feat, ability);
            ApplyHostileAbilityResourceRestoreEffects(activator, ability);

            TrackCombatAbilityUse(activator, ability);
        }

        private static SkillType ResolveActivatedAbilitySkillType(
            uint activator,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            return summary.SkillType != SkillType.Invalid
                ? summary.SkillType
                : GetAbilitySkillType(activator, ability);
        }

        private static void ApplyAreaAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsAreaAbility)
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return;

            var evasion = Stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedEvasionPercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedEvasionDurationSeconds);
            if (evasion == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.EvasionPercentAdjustment,
                evasion,
                duration,
                StatType.AreaAbilityUsedEvasionPercentAdjustment);
        }

        private static void ApplyHostileAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return;

            ApplyAbilityUsedEvasion(
                activator,
                StatType.HostileAbilityUsedEvasionPercentAdjustment,
                StatType.HostileAbilityUsedEvasionDurationSeconds);
        }

        private static void ApplyCostlyAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !TryGetAbilityStaminaCostState(activator, ability, out var costState))
            {
                return;
            }

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType));
            var minimumCost = Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityUsedEvasionMinimumStaminaCost);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                costState.Cost < minimumCost)
            {
                return;
            }

            ApplyAbilityUsedEvasion(
                activator,
                StatType.CostlyAbilityUsedEvasionPercentAdjustment,
                StatType.CostlyAbilityUsedEvasionDurationSeconds);
        }

        private static void ApplyAbilityUsedMovementSpeed(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return;

            var movementSpeed = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMovementSpeedPercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMovementSpeedDurationSeconds);
            if (movementSpeed == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.MovementSpeedPercentAdjustment,
                movementSpeed,
                duration,
                StatType.AbilityUsedMovementSpeedPercentAdjustment);
        }

        private static void ApplyHostileAbilityForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentPerStack);
            var duration = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackDurationSeconds);
            var maximum = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentMax);
            if (forceAttack == 0 || duration <= 0 || maximum <= 0)
                return;

            var current = StatusEffect.GetStatusEffect(
                activator,
                typeof(HostileAbilityForceAttackStatusEffect)) as HostileAbilityForceAttackStatusEffect;
            var total = Math.Min(maximum, (current?.ForceAttack ?? 0) + forceAttack);

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new HostileAbilityForceAttackStatusEffect(total),
                duration);
        }

        /// <summary>
        /// Grants stacking Force Attack when the activated hostile ability costs at least the
        /// configured minimum FP. Drives the Lightsaber Severance "Overpower" trait.
        /// </summary>
        private static void ApplyHostileAbilityFPSpendForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackPercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds);
            var maximum = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent);
            var minimumFPCost = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost);
            if (forceAttack <= 0 || duration <= 0 || maximum <= 0 || minimumFPCost <= 0)
                return;

            var fpCost = ability.Requirements
                .OfType<AbilityRequirementFP>()
                .Sum(x => x.RequiredFP);
            if (fpCost < minimumFPCost)
                return;

            TemporaryStatModifier.AddCapped(
                activator,
                StatType.ForceAttackPercentAdjustment,
                forceAttack,
                duration,
                maximum,
                StatType.HostileAbilityFPSpendForceAttackPercent,
                1,
                refreshExistingStacks: true);
        }

        private static void ApplyHostileAbilityUsedAttackAdjustment(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var attack = Stat.GetStatAdjustment(activator, StatType.HostileAbilityUsedAttackPercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds);
            var maximum = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum);
            if (attack <= 0 || duration <= 0 || maximum <= 0)
                return;

            TemporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attack,
                duration,
                maximum,
                StatType.HostileAbilityUsedAttackPercentAdjustment,
                1,
                refreshExistingStacks: true);

            var currentTotal = TemporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.AttackPercentAdjustment,
                StatType.HostileAbilityUsedAttackPercentAdjustment);
            if (currentTotal > 0)
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    activator,
                    new ButchersTempoStatusEffect(currentTotal),
                    duration);
            }
        }

        private static void ApplyAbilityUsedNearbyAllyDefense(uint activator)
        {
            var defense = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
            var forceDefense = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds);
            if (duration <= 0 || defense == 0 && forceDefense == 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f, false))
            {
                if (defense != 0)
                {
                    TemporaryStatModifier.Replace(
                        friendly,
                        StatType.PhysicalDefensePercentAdjustment,
                        defense,
                        duration,
                        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
                }

                if (forceDefense != 0)
                {
                    TemporaryStatModifier.Replace(
                        friendly,
                        StatType.ForceDefensePercentAdjustment,
                        forceDefense,
                        duration,
                        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
                }
            }
        }

        private static void ApplyAbilityActivatedRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null)
                return;

            switch (skillType)
            {
                case SkillType.HeavyVibroblade:
                    ApplyHeavyVibrobladeActivatedEffects(activator, target, ability);
                    break;
                case SkillType.BeastMastery:
                    ApplyBeastBalancedAbilityRecovery(activator, ability);
                    break;
                case SkillType.Vibroknife:
                    ApplyVibroknifeShadowActivatedEffects(activator, ability);
                    break;
                case SkillType.Pistol:
                    ApplyPistolSkirmisherActivatedEffects(activator, target, ability);
                    break;
                case SkillType.Lightsaber:
                    ApplyLightsaberOffenseActivatedEffects(activator, target);
                    ApplyLightsaberDefenseActivatedEffects(activator);
                    ApplyLightsaberWardActivatedEffects(activator, ability);
                    break;
            }
        }

        public static int GetAbilityImpactBaseDamageBonus(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !GetIsObjectValid(activator))
                return 0;

            var bonus = 0;

            switch (skillType)
            {
                case SkillType.Lightsaber:
                    if (ability.IsAreaAbility)
                    {
                        bonus += Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseAreaDamageBonus);
                    }

                    if (ability.IsSingleTargetAbility &&
                        GetIsObjectValid(target) &&
                        StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                    {
                        bonus += Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDebuffedTargetDamageBonus);
                    }
                    break;
                case SkillType.Vibroknife when ability.IsHostileAbility:
                    var toxicCoatingRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
                    if (toxicCoatingRank > 0)
                    {
                        bonus += toxicCoatingRank >= 2 ? 22 : 10;
                    }
                    break;
                case SkillType.Staff when ability.IsHostileAbility:
                    bonus += Stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDamageBonus);
                    break;
                case SkillType.Saberstaff when ability.IsHostileAbility:
                    bonus += Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareDamageBonus);
                    break;
                case SkillType.TwinBlade when ability.IsHostileAbility &&
                    AbilityMatchesReversalCutTrigger(activator, ability):
                    bonus += TemporaryStatModifier.Consume(
                        activator,
                        StatType.TwinBladeDuelistReversalCutDamageBonus,
                        StatType.TwinBladeDuelistReversalCut);
                    break;
            }

            if (ability.IsHostileAbility)
            {
                bonus += GetFirstHostileAbilityHitDamageBonus(activator, ability);
                bonus += GetDirectDamageToStatusCategoryOrStealthBonus(activator, target);
            }

            bonus += ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(activator, skillType);

            if (ability.IsHostileAbility &&
                IsCurrentFPAndStaminaAtOrAbovePercent(
                    activator,
                    Stat.GetStatAdjustment(activator, StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent)))
            {
                bonus += Stat.GetStatAdjustment(activator, StatType.HighFPAndStaminaAbilityDamageBonus);
            }

            return bonus;
        }

        public static int GetCostlyAbilityDamageBonus(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (!TryGetAbilityStaminaCostState(activator, ability, out var costState))
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageBonusSkillType));
            var minimumCost = Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageMinimumStaminaCost);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                costState.Cost < minimumCost)
            {
                return 0;
            }

            return Stat.GetStatAdjustment(activator, StatType.CostlyAbilityDamageBonus);
        }

        public static void ApplySuccessfulAbilityImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            bool firstHostileAbilityHitDamageBonusApplied,
            bool isFirstSuccessfulTarget)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || ability == null)
                return;

            if (firstHostileAbilityHitDamageBonusApplied)
                ApplyFirstHostileAbilityHitCount(activator, ability);
            if (ability.IsHostileAbility && !ability.SuppressesSourceStatusStackRiders)
            {
                ApplySourceStatusStackEffects(activator, target);
            }

            ApplyHostileAbilityHitNextAutoAttackNoDelay(activator, ability);
            ApplySameTargetHostileAbilityHitEffects(activator, target, ability);
            ApplyAbilityStatusRiders(
                activator,
                target,
                ability,
                skillType,
                damage,
                statusApplied,
                primaryStatusEffect,
                additionalStatusEffects,
                isFirstSuccessfulTarget);
            ApplyStatusAppliedEffects(
                activator,
                target,
                statusApplied,
                primaryStatusEffect,
                additionalStatusEffects);
            ApplyAbilityTargetStatusEffects(activator, target, ability);
            ApplyRangedAbilityHitNearTargetEffects(activator, target, ability, skillType);
            ApplyCostlyAbilityHitEffects(activator, target, ability, skillType);
            ApplyAbilityDamageRiders(activator, target, ability, skillType, damageType, damage);
            ApplyAreaAbilityTargetHitSequenceEffects(activator, target, ability, skillType);
        }

        private static void ApplyCostlyAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !TryGetAbilityStaminaCostState(activator, ability, out var costState))
            {
                return;
            }

            var staminaRestoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreSkillType));
            var statusSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusSkillType));
            var staminaRestoreMinimumCost = Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost);
            var statusMinimumCost = Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusMinimumStaminaCost);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.CostlyAbilityHitStaminaRestore);
            var exposedDuration = Stat.GetStatAdjustment(activator, StatType.CostlyAbilityExposedDurationSeconds);

            if (staminaRestore > 0 &&
                staminaRestoreMinimumCost > 0 &&
                costState.Cost >= staminaRestoreMinimumCost &&
                !costState.StaminaRestoreApplied &&
                SkillTypeMatchesOrGlobal(skillType, staminaRestoreSkillType))
            {
                Stat.RestoreStamina(activator, staminaRestore);
                costState.StaminaRestoreApplied = true;
            }

            if (exposedDuration > 0 &&
                statusMinimumCost > 0 &&
                costState.Cost >= statusMinimumCost &&
                SkillTypeMatchesOrGlobal(skillType, statusSkillType))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
            }
        }

        private static void ApplyRangedAbilityHitNearTargetEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null ||
                !ability.IsHostileAbility ||
                !IsRangedWeaponSkill(skillType) ||
                !GetIsObjectValid(target))
            {
                return;
            }

            var range = Stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetRangeMeters);
            var damageDealt = Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetDurationSeconds);
            var nameStrRef = Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef);
            var icon = (EffectIconType)Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectIcon);
            var cleanseTypes = (StatusEffectCleanseType)Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes);
            var resistanceType = (ResistanceType)Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectResistanceType);
            if (range <= 0 ||
                damageDealt == 0 ||
                duration <= 0 ||
                nameStrRef <= 0 ||
                icon == EffectIconType.Invalid)
            {
                return;
            }

            if (GetDistanceBetween(activator, target) > range)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new DamageDealtAdjustmentStatusEffect(
                    damageDealt,
                    nameStrRef,
                    icon,
                    cleanseTypes,
                    resistanceType),
                duration,
                CombatDamageType.Physical);
        }

        private static void ApplySameTargetHostileAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var requiredCount = Stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityHitCountRequired);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityStaminaRestore);
            if (requiredCount <= 0 || staminaRestore <= 0)
                return;

            var key = (activator, target);
            _sameTargetHostileAbilityHitCounts.TryGetValue(key, out var count);
            count++;

            if (count < requiredCount)
            {
                _sameTargetHostileAbilityHitCounts[key] = count;
                return;
            }

            _sameTargetHostileAbilityHitCounts[key] = 0;
            Stat.RestoreStamina(activator, staminaRestore);
        }

        private static void ApplyHostileAbilityHitNextAutoAttackNoDelay(
            uint activator,
            AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var appliesToAllSkills = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills) > 0;
            var autoAttackSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelaySkillType));
            if (!appliesToAllSkills && autoAttackSkillType == SkillType.Invalid)
                return;

            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelayDurationSeconds);
            if (duration <= 0)
                return;

            if (appliesToAllSkills)
                GrantNextAutoAttackNoDelay(activator, duration);
            else
                GrantNextAutoAttackNoDelay(activator, autoAttackSkillType, duration);
        }

        private static void ApplyAbilityStatusRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            bool isFirstSuccessfulTarget)
        {
            switch (skillType)
            {
                case SkillType.HeavyVibroblade:
                    ApplyHeavyVibrobladeDefenseImpactRiders(activator, target, ability);
                    break;
                case SkillType.Force:
                    ApplyForceDarkImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
                case SkillType.Katar:
                    ApplyKatarVenomCurrentImpactRiders(activator, target);
                    break;
                case SkillType.Leadership:
                    if (isFirstSuccessfulTarget)
                        ApplyLeadershipVanguardImpactRiders(activator);
                    break;
                case SkillType.Lightsaber:
                    ApplyLightsaberOffenseImpactRiders(activator, target, ability);
                    break;
                case SkillType.Pistol:
                    ApplyPistolSkirmisherImpactRiders(activator, target, ability);
                    break;
                case SkillType.Rifle:
                    ApplyRiflePacificationImpactRiders(activator, target);
                    break;
                case SkillType.Saberstaff:
                    ApplySaberstaffConduitImpactRiders(activator, target, ability);
                    ApplySaberstaffTempestImpactRiders(activator, target, ability);
                    break;
                case SkillType.Spear:
                    ApplySpearDamageImpactRiders(activator, target, ability);
                    ApplySpearDisablerImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
                case SkillType.Staff:
                    ApplyStaffCrusherImpactRiders(activator, target);
                    break;
                case SkillType.Throwing:
                    ApplyThrowingDeadeyeImpactRiders(activator, target, ability);
                    break;
                case SkillType.TwinBlade:
                    ApplyTwinBladeDuelistImpactRiders(activator, target, ability);
                    break;
                case SkillType.Vibroknife:
                    ApplyVibroknifeShadowImpactRiders(activator, target, ability);
                    ApplyVibroknifeSaboteurImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
            }

            ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(activator, target, ability);
            ApplyGuardedHitNextSkillAbilityExposedStatus(activator, target, skillType);
        }

        private static void ApplyAbilityDamageRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage)
        {
            if (damage <= 0)
                return;

            ApplyFoggyMindResourceDrain(activator, target, ability);
            ApplyBleedingTargetAbilityBleedRefresh(activator, target, skillType);
            ApplyBleedingTargetAbilityBleedSpread(activator, target, skillType, damageType);
            ApplyAreaAbilityFragmentation(activator, target, ability, skillType, damageType);

            switch (skillType)
            {
                case SkillType.Katar when ability.IsSingleTargetAbility &&
                    Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentSecondStrikeDamageBonus) > 0:
                    var bonus = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentSecondStrikeDamageBonus);
                    ApplyTriggeredDamage(activator, target, bonus, damageType);

                    if (StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
                    {
                        StatusEffect.ApplyStatusEffect(activator, target, typeof(BleedStatusEffect), 30f, damageType);
                    }
                    break;
                case SkillType.Pistol when ability.IsSingleTargetAbility:
                    ApplyRicochetDamage(
                        activator,
                        target,
                        damageType,
                        StatType.PistolSkirmisherRicochetDamageBonus,
                        StatType.PistolSkirmisherRicochetMaximumTargets,
                        StatType.PistolSkirmisherRicochetCooldownSeconds);
                    break;
                case SkillType.Throwing:
                    if (ability.IsSingleTargetAbility)
                    {
                        ApplyRicochetDamage(
                            activator,
                            target,
                            damageType,
                            StatType.ThrowingDeadeyeRicochetDamageBonus,
                            StatType.ThrowingDeadeyeRicochetMaximumTargets);
                    }

                    if (ability.IsAreaAbility)
                    {
                        ApplyClusterStormDamage(activator, target, damageType);
                        ApplySaturationToss(activator, target);
                    }
                    break;
            }
        }

        private static void ApplyFoggyMindResourceDrain(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null ||
                !ability.IsHostileAbility ||
                !GetIsObjectValid(target) ||
                !StatusEffect.HasStatusEffect(target, typeof(FoggyMindStatusEffect)))
            {
                return;
            }

            var fpDrain = Stat.GetStatAdjustment(activator, StatType.AbilityResourceDrainFoggyMindFP);
            if (fpDrain > 0)
                Stat.ReduceFP(target, fpDrain);

            var staminaDrain = Stat.GetStatAdjustment(activator, StatType.AbilityResourceDrainFoggyMindStamina);
            if (staminaDrain > 0)
                Stat.ReduceStamina(target, staminaDrain);
        }

        private static void ApplyAreaAbilityFragmentation(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (ability == null || !ability.IsAreaAbility || !GetIsObjectValid(target))
                return;

            var damage = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFragmentationDamage);
            var duration = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFragmentationDurationSeconds);
            var pulse = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFragmentationPulseSeconds);
            if (damage <= 0 || duration <= 0 || pulse <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new FragmentationStatusEffect(damage, pulse),
                duration,
                damageType);
        }

        private static void ApplyRicochetDamage(
            uint activator,
            uint target,
            CombatDamageType damageType,
            StatType damageStatType,
            StatType maximumTargetsStatType,
            StatType cooldownStatType = StatType.Invalid)
        {
            var bonus = Stat.GetStatAdjustment(activator, damageStatType);
            var maximumTargets = Stat.GetStatAdjustment(activator, maximumTargetsStatType);
            if (bonus <= 0 || maximumTargets <= 0)
                return;

            if (cooldownStatType != StatType.Invalid)
            {
                var cooldown = Stat.GetStatAdjustment(activator, cooldownStatType);
                if (!TryUseStatTrigger(activator, damageStatType, cooldown))
                    return;
            }

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, maximumTargets, OBJECT_INVALID))
            {
                if (nearby == target)
                    continue;

                ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        private static void ApplyClusterStormDamage(
            uint activator,
            uint target,
            CombatDamageType damageType)
        {
            var bonus = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierClusterStormDamageBonus);
            var maximumTargets = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierClusterStormMaximumTargets);
            if (bonus <= 0 || maximumTargets <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, maximumTargets, OBJECT_INVALID))
            {
                if (nearby == target)
                    continue;

                ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        private static void ApplySaturationToss(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationToss) <= 0)
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossDurationSeconds);
            var damage = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossDamage);
            var pulse = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossPulseSeconds);
            if (duration <= 0 || damage <= 0 || pulse <= 0)
                return;

            var applied = StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new SaturationTossStatusEffect(damage, pulse),
                duration,
                CombatDamageType.Fire);
            if (!applied)
                return;

            ApplyEffectAtLocation(
                DurationType.Temporary,
                EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Fire),
                GetLocation(target),
                duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S), target);
        }

        private static void ApplyHeavyVibrobladeDefenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(activator, ability))
                return;

            var enmityBonus = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityEnmityBonus);
            if (enmityBonus > 0)
            {
                Enmity.ModifyEnmity(activator, target, enmityBonus);
            }

            if (AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType) &&
                Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityCrushingBlow) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(CrushingBlowStatusEffect), 30f, CombatDamageType.Physical);
            }
        }

        private static void ApplyForceDarkImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            if (Stat.GetStatAdjustment(activator, StatType.DarkManipulatorCollapseWill) <= 0 ||
                !AbilityAppliedAnyStatus(
                    primaryStatusEffect,
                    additionalStatusEffects,
                    typeof(NightmareField1StatusEffect),
                    typeof(EclipseOfResolve1StatusEffect)))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), 30f, CombatDamageType.Force);
            StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), 30f, CombatDamageType.Force);
        }

        private static void ApplyLightsaberOffenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var sunderDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSunderDurationSeconds);
            if (sunderDuration > 0)
            {
                ApplyLightsaberOffenseSunder(activator, target, sunderDuration);
            }

            var disorientedDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDisorientedDurationSeconds);
            if (disorientedDuration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }

            if (ability.IsSingleTargetAbility)
            {
                var disruptionDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSingleTargetForceDisruptionDurationSeconds);
                if (disruptionDuration > 0)
                {
                    StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), disruptionDuration, CombatDamageType.Force);
                }
            }

            ApplyLightsaberOffensePurify(activator, target);
        }

        private static void ApplyLightsaberOffenseSunder(uint activator, uint target, int duration)
        {
            const int DefensePenaltyPercent = 15;

            if (HasSunderPenaltyAtLeast(target, DefensePenaltyPercent))
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new SunderStatusEffect(DefensePenaltyPercent),
                duration,
                CombatDamageType.Physical);
        }

        private static void ApplyNextDamageDealtBleedEffect(
            uint attacker,
            uint defender,
            CombatDamageType damageType)
        {
            var duration = TemporaryStatModifier.Consume(
                attacker,
                StatType.NextDamageDealtBleedDurationSeconds,
                StatType.NextDamageDealtBleedDurationSeconds);
            if (duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(attacker, defender, typeof(BleedStatusEffect), duration, damageType);
        }

        private static void ApplyBleedingTargetAbilityBleedRefresh(uint attacker, uint defender, SkillType skillType)
        {
            if (!StatusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker))
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToBleedingTargetSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return;

            var extensionSeconds = Stat.GetStatAdjustment(
                attacker,
                StatType.BleedingTargetAbilityBleedDurationExtensionSeconds);
            if (extensionSeconds <= 0)
                return;

            StatusEffect.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
        }

        private static void ApplyBleedingTargetAbilityBleedSpread(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!damageType.IsPhysicalDamageType() ||
                !IsWeaponSkillType(skillType) ||
                !StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var chance = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadDurationSeconds);
            if (chance <= 0 || duration <= 0 || Random.D100(1) > chance)
                return;

            var maximumTargets = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadMaxTargets);
            maximumTargets = maximumTargets <= 0 ? 1 : maximumTargets;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         attacker,
                         GetLocation(defender),
                         5f,
                         maximumTargets,
                         defender))
            {
                StatusEffect.ApplyStatusEffect(attacker, nearby, typeof(BleedStatusEffect), duration, damageType);
            }
        }

        private static void ApplyAutoAttackSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!IsRangedWeaponSkill(skillType))
                return;

            var chance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackDurationSeconds);
            if (chance <= 0 || duration <= 0 || Random.D100(1) > chance)
                return;

            ApplySuppressionStack(
                attacker,
                defender,
                Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackEvasionPenaltyPercent),
                duration,
                damageType);
        }

        private static void ApplyRangedHitSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!IsRangedWeaponSkill(skillType))
                return;

            var duration = Stat.GetStatAdjustment(attacker, StatType.RangedHitSuppressionStackDurationSeconds);
            if (duration <= 0)
                return;

            ApplySuppressionStack(
                attacker,
                defender,
                Stat.GetStatAdjustment(attacker, StatType.RangedHitSuppressionStackEvasionPenaltyPercent),
                duration,
                damageType);
        }

        public static void ApplySuppressionStack(
            uint attacker,
            uint defender,
            int evasionPenaltyPercent,
            int durationSeconds,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                durationSeconds <= 0)
            {
                return;
            }

            var adjustedEvasionPenaltyPercent = Math.Max(
                0,
                evasionPenaltyPercent +
                Stat.GetStatAdjustment(attacker, StatType.SuppressionStackEvasionPenaltyPercentAdjustment));
            if (adjustedEvasionPenaltyPercent <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new SuppressionStatusEffect(adjustedEvasionPenaltyPercent),
                durationSeconds,
                damageType);
        }

        public static int GetSuppressionStackCount(uint target, uint source = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(target))
                return 0;

            return StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Count(effect => !GetIsObjectValid(source) || effect.Source == source);
        }

        private static int GetSuppressionDamageDealtToOtherTargetsAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var adjustment = 0;
            foreach (var group in StatusEffect.GetCreatureStatusEffects(attacker)
                         .GetAllEffects()
                         .OfType<SuppressionStatusEffect>()
                         .Where(effect => GetIsObjectValid(effect.Source) && effect.Source != defender)
                         .GroupBy(effect => effect.Source))
            {
                var requiredStacks = Stat.GetStatAdjustment(
                    group.Key,
                    StatType.SuppressionStackDamageDealtToOtherTargetsRequiredStacks);
                var percentAdjustment = Stat.GetStatAdjustment(
                    group.Key,
                    StatType.SuppressionStackDamageDealtToOtherTargetsPercentAdjustment);
                if (requiredStacks > 0 && percentAdjustment != 0 && group.Count() >= requiredStacks)
                {
                    adjustment += percentAdjustment;
                }
            }

            return adjustment;
        }

        private static int GetDamageToSourceAppliedStatusTargetAdjustment(uint attacker, uint defender)
        {
            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageToSourceAppliedStatusTargetCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.DamageToSourceAppliedStatusTargetPercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return adjustment;
        }

        private static int GetDirectDamageToStatusCategoryOrStealthBonus(uint attacker, uint defender)
        {
            var bonus = Stat.GetStatAdjustment(attacker, StatType.DirectDamageToStatusCategoryOrStealthBonus);
            if (bonus == 0)
                return 0;

            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DirectDamageToStatusCategoryOrStealthBonusCategory));
            if (category != 0 && TargetHasAnyStatusEffectCategory(defender, category))
                return bonus;

            return ConsumeStealthOpeningWindow(attacker)
                ? bonus
                : 0;
        }

        private static int GetFirstHostileAbilityHitDamageBonus(uint attacker, AbilityDetail ability)
        {
            var bonus = Stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitDamageBonus);
            var maximumCount = Stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitMaximumCount);
            if (bonus <= 0 || maximumCount <= 0 || ability?.IsHostileAbility != true)
                return 0;

            var state = EnsureFirstHostileAbilityHitState(attacker, maximumCount, bonus, out _);
            return state.Count < maximumCount
                ? bonus
                : 0;
        }

        private static void ApplyFirstHostileAbilityHitCount(uint attacker, AbilityDetail ability)
        {
            var maximumCount = Stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitMaximumCount);
            if (maximumCount <= 0 || ability?.IsHostileAbility != true)
                return;

            var damageBonus = Stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitDamageBonus);
            var state = EnsureFirstHostileAbilityHitState(attacker, maximumCount, damageBonus, out _);

            var now = DateTime.UtcNow;
            if (ability.IsAreaAbility &&
                (now - state.LastHit).TotalSeconds <= 1)
            {
                return;
            }

            var currentCount = state?.Count ?? 0;
            if (currentCount >= maximumCount)
                return;

            var newCount = Math.Min(maximumCount, currentCount + 1);
            var rechargeAvailableAt = state?.RechargeAvailableAt;
            if (newCount >= maximumCount)
            {
                // The final stack was just consumed; open the recharge window.
                var cooldownSeconds = Stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitCooldownSeconds);
                rechargeAvailableAt = cooldownSeconds > 0
                    ? now.AddSeconds(cooldownSeconds)
                    : null;
            }

            _firstHostileAbilityHitCounts[attacker] = new TargetHitSequenceState
            {
                Count = newCount,
                LastHit = now,
                RechargeAvailableAt = rechargeAvailableAt
            };

            Log.WriteStructured(
                LogGroup.Attack,
                "First Strike stack consumed: Attacker={Attacker} Count={Count} MaximumCount={MaximumCount} DamageBonus={DamageBonus} RechargeAvailableAt={RechargeAvailableAt}",
                attacker,
                newCount,
                maximumCount,
                damageBonus,
                rechargeAvailableAt);

            if (damageBonus > 0 && GetIsPC(attacker))
            {
                var remaining = maximumCount - newCount;
                var stackLabel = remaining == 1 ? "stack" : "stacks";
                var cooldownSeconds = Stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitCooldownSeconds);
                var rechargeText = remaining == 0
                    ? cooldownSeconds > 0
                        ? $"; recharges in {cooldownSeconds} seconds"
                        : "; recharges after combat"
                    : string.Empty;
                var feedback = ColorToken.Combat(
                    $"First Strike deals +{damageBonus} DMG ({remaining} {stackLabel} remaining{rechargeText}).");

                SendMessageToPC(attacker, feedback);
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"First Strike +{damageBonus} DMG ({remaining} {stackLabel} remaining)"),
                    attacker,
                    false);
            }
        }

        private static TargetHitSequenceState EnsureFirstHostileAbilityHitState(
            uint attacker,
            int maximumCount,
            int damageBonus,
            out bool becameReady)
        {
            RechargeFirstHostileAbilityHitStacks(attacker, maximumCount);
            if (_firstHostileAbilityHitCounts.TryGetValue(attacker, out var state))
            {
                becameReady = false;
                return state;
            }

            state = new TargetHitSequenceState
            {
                Count = 0,
                LastHit = DateTime.MinValue
            };
            _firstHostileAbilityHitCounts[attacker] = state;
            becameReady = true;

            Log.WriteStructured(
                LogGroup.Attack,
                "First Strike ready: Attacker={Attacker} Count={Count} MaximumCount={MaximumCount} DamageBonus={DamageBonus}",
                attacker,
                state.Count,
                maximumCount,
                damageBonus);

            if (damageBonus > 0 && GetIsPC(attacker))
            {
                var stackLabel = maximumCount == 1 ? "stack" : "stacks";
                var feedback = ColorToken.Combat(
                    $"First Strike ready: {maximumCount} {stackLabel} (+{damageBonus} DMG each).");
                SendMessageToPC(attacker, feedback);
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"First Strike ready ({maximumCount} {stackLabel})"),
                    attacker,
                    false);
            }

            return state;
        }

        private static void ReportFirstStrikeCombatEntry(uint attacker, DateTime now)
        {
            var maximumCount = Stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitMaximumCount);
            var damageBonus = Stat.GetStatAdjustment(attacker, StatType.FirstHostileAbilityHitDamageBonus);
            if (maximumCount <= 0 || damageBonus <= 0)
                return;

            var state = EnsureFirstHostileAbilityHitState(attacker, maximumCount, damageBonus, out var becameReady);
            if (becameReady ||
                state.Count < maximumCount ||
                state.RechargeAvailableAt == null ||
                !GetIsPC(attacker))
            {
                return;
            }

            var remainingSeconds = Math.Max(1, (int)Math.Ceiling((state.RechargeAvailableAt.Value - now).TotalSeconds));
            SendMessageToPC(
                attacker,
                ColorToken.Combat($"First Strike is recharging ({remainingSeconds} seconds remaining)."));
        }

        private static void RechargeFirstHostileAbilityHitStacks(uint attacker, int maximumCount)
        {
            if (!_firstHostileAbilityHitCounts.TryGetValue(attacker, out var state))
                return;

            // Exhausted stacks recharge strictly on the cooldown timer, even across separate engagements.
            if (state.Count >= maximumCount && state.RechargeAvailableAt != null)
            {
                if (DateTime.UtcNow >= state.RechargeAvailableAt.Value)
                {
                    _firstHostileAbilityHitCounts.Remove(attacker);
                    Log.WriteStructured(
                        LogGroup.Attack,
                        "First Strike recharged: Attacker={Attacker} PreviousCount={PreviousCount} MaximumCount={MaximumCount} RechargeAvailableAt={RechargeAvailableAt}",
                        attacker,
                        state.Count,
                        maximumCount,
                        state.RechargeAvailableAt.Value);
                }

                return;
            }

            // With stacks remaining, refresh to a full set once the wielder drops out of combat so each new engagement opens with all stacks.
            if (HasRecentCombatEntryActivity(attacker, DateTime.UtcNow))
            {
                return;
            }

            _firstHostileAbilityHitCounts.Remove(attacker);
            Log.WriteStructured(
                LogGroup.Attack,
                "First Strike reset after combat: Attacker={Attacker} PreviousCount={PreviousCount} MaximumCount={MaximumCount}",
                attacker,
                state.Count,
                maximumCount);
        }

        private static int GetAbilityDamageToSourceAppliedStatusTargetAdjustment(
            uint attacker,
            uint defender,
            SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return 0;

            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return adjustment;
        }

        private static int GetAbilityDamageToSourceAppliedStatusTargetBonus(uint attacker, uint defender)
        {
            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetBonusCategory));
            var bonus = Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToSourceAppliedStatusTargetBonus);
            if (category == 0 || bonus == 0 || !TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return bonus;
        }

        private static bool TargetHasSourceAppliedStatusCategory(
            uint defender,
            uint source,
            StatusEffectCategory category)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(source) || category == 0)
                return false;

            foreach (var effect in StatusEffect.GetCreatureStatusEffects(defender).GetAllEffects())
            {
                if (effect.Source == source && (effect.Categories & category) != 0)
                    return true;
            }

            return false;
        }

        private static void ApplySourceStatusStackEffects(uint attacker, uint defender)
        {
            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusStackRequiredCategory));
            var appliedCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusStackAppliedCategory));
            var maximumStacks = Stat.GetStatAdjustment(attacker, StatType.SourceStatusStackMaximum);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.SourceStatusStackDurationSeconds);
            if (requiredCategory == 0 ||
                appliedCategory == 0 ||
                maximumStacks <= 0 ||
                durationSeconds <= 0 ||
                !TargetHasSourceAppliedStatusCategory(defender, attacker, requiredCategory))
            {
                return;
            }

            var statusEffectType = GetSourceStatusStackEffectType(appliedCategory);
            if (statusEffectType == null)
                return;

            if (statusEffectType == typeof(InfectionStatusEffect))
            {
                ApplyInfectionStack(attacker, defender, maximumStacks, durationSeconds);
            }
        }

        private static Type GetSourceStatusStackEffectType(StatusEffectCategory category)
        {
            return category switch
            {
                StatusEffectCategory.Infection => typeof(InfectionStatusEffect),
                _ => null
            };
        }

        private static void ApplyInfectionStack(
            uint attacker,
            uint defender,
            int maximumStacks,
            int durationSeconds)
        {
            var existing = StatusEffect.GetStatusEffect(defender, typeof(InfectionStatusEffect), attacker) as InfectionStatusEffect;
            if (existing == null)
            {
                StatusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    new InfectionStatusEffect(1),
                    durationSeconds,
                    CombatDamageType.Poison);
                return;
            }

            existing.AddStack(maximumStacks);
            StatusEffect.RefreshStatusEffectDuration(
                defender,
                typeof(InfectionStatusEffect),
                attacker,
                durationSeconds,
                sourceDamageType: CombatDamageType.Poison);
        }

        private static bool TargetHasAnyStatusEffectCategory(uint creature, StatusEffectCategory category)
        {
            if (!GetIsObjectValid(creature) || category == 0)
                return false;

            return StatusEffect.GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Any(effect => (effect.Categories & category) != 0);
        }

        private static void ApplyFrenzySlashHasteRefresh(uint attacker)
        {
            var duration = Stat.GetStatAdjustment(attacker, StatType.FrenzySlashHasteRefreshDurationSeconds);
            if (duration <= 0)
                return;

            TemporaryStatModifier.Refresh(
                attacker,
                StatType.AttackDelayReductionPercent,
                duration,
                StatType.AttackDelayReductionPercent);
        }

        private static bool HasSunderPenaltyAtLeast(uint target, int defensePenaltyPercent)
        {
            return StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<SunderStatusEffect>()
                .Select(effect => Math.Abs(effect.StatGroup.Stats.GetValueOrDefault(StatType.PhysicalDefensePercentAdjustment)))
                .DefaultIfEmpty(0)
                .Max() >= defensePenaltyPercent;
        }

        private static void ApplyLightsaberOffensePurify(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.LightsaberOffensePurify) <= 0)
                return;

            var cooldown = Stat.GetStatAdjustment(activator, StatType.LightsaberOffensePurifyCooldownSeconds);
            if (!TryUseStatTrigger(activator, StatType.LightsaberOffensePurify, cooldown))
                return;

            var effect = StatusEffect.GetCreatureStatusEffects(activator)
                .GetAllEffects()
                .FirstOrDefault(IsTransferableHarmfulStatus);
            if (effect == null)
                return;

            var transferred = effect.Clone();
            StatusEffect.RemoveStatusEffect(activator, effect.GetType(), effect.Source, false);
            StatusEffect.ApplyStatusEffect(activator, target, transferred, 30f, CombatDamageType.Force);
        }

        private static bool IsTransferableHarmfulStatus(IStatusEffect effect)
        {
            return effect != null &&
                   (effect.Categories & (StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.Bleeding)) != 0;
        }

        private static void ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryTargetEnmityToSourceCategoryId))
            {
                return;
            }

            var enmity = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds);
            if (enmity <= 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new CoveringClawsStatusEffect(enmity),
                duration,
                CombatDamageType.Physical);
        }

        private static void ApplyKatarVenomCurrentImpactRiders(uint activator, uint target)
        {
            if (StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
            {
                SpreadPoisonFromTarget(activator, target);
            }
        }

        private static void ApplyToxicRushDamageDealtEffects(
            uint attacker,
            uint defender,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType == CombatDamageDeliveryType.DamageOverTime)
                return;

            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
                return;

            var haste = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushHastePercentPerStack);
            var attack = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushAttackPercentPerStack);
            var maxStacks = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushMaximumStacks);
            var duration = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushDurationSeconds);
            if (maxStacks <= 0 || duration <= 0 || (haste <= 0 && attack <= 0))
                return;

            var currentStacks = StatusEffect.GetStatusEffect<ToxicRushStatusEffect>(attacker)?.Stacks ?? 0;
            var stacks = Math.Min(maxStacks, currentStacks + 1);
            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new ToxicRushStatusEffect(stacks, haste, attack),
                duration);

            if (stacks >= maxStacks)
            {
                Stat.RestoreStamina(attacker, 2);
            }
        }

        private static void SpreadPoisonFromTarget(uint activator, uint target)
        {
            var radius = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadRadiusMeters);
            var duration = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds);
            if (radius <= 0 || duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), radius, 0, target))
            {
                StatusEffect.ApplyStatusEffect(activator, nearby, typeof(PoisonStatusEffect), duration, CombatDamageType.Poison);
            }
        }

        public static void ApplyLeadershipVanguardImpactRiders(uint activator)
        {
            var rank = Stat.GetStatAdjustment(activator, StatType.LeadershipVanguardMarkTargetRank);
            if (rank <= 0)
                return;

            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyLeadershipCommandDurationBonus(activator, 30f);
            var statusEffectType = rank >= 2
                ? typeof(MarkTarget2StatusEffect)
                : typeof(MarkTarget1StatusEffect);

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, activator, true, radius))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, statusEffectType, duration);
            }
        }

        private static void ApplyPistolSkirmisherImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var disorientedDuration = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherDisorientedDurationSeconds);
            if (disorientedDuration > 0 && (ability.IsAreaAbility || ability.MaxRange <= 5f))
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }
        }

        private static void ApplyRiflePacificationImpactRiders(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.RiflePacificationNeutralizingShot) > 0)
            {
                StatusEffect.RemoveFirstBeneficialCombatStatusEffect(target, false);
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), 30f, ResistanceType.Mind);
            }

            if (Stat.GetStatAdjustment(activator, StatType.RiflePacificationOverwatch) > 0)
            {
                AssignCommand(target, () => ClearAllActions());
                StatusEffect.ApplyStatusEffect(activator, target, new FoggyMindStatusEffect(2), 30f, ResistanceType.Mind);
            }

            var pinningRank = Stat.GetStatAdjustment(activator, StatType.RiflePacificationPinningFireRank);
            if (pinningRank >= 2)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(KnockdownStatusEffect), 6f, ResistanceType.Trauma);
            }
            else if (pinningRank == 1)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), 30f, ResistanceType.Mind);
            }
        }

        private static void ApplySaberstaffConduitImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility ||
                Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitAreaConduitFlare) <= 0)
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        private static void ApplySaberstaffTempestImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsAreaAbility ||
                Stat.GetStatAdjustment(activator, StatType.SaberstaffTempestForceGyre) <= 0)
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(activator, StatType.SaberstaffTempestForceGyreDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        private static void ApplySpearDamageImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (Stat.GetStatAdjustment(activator, StatType.SpearDamageBreachStrike) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(BreachStatusEffect), 30f, CombatDamageType.Physical);
            }

            if (ability.IsAreaAbility && Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefense) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(CrippledDefenseStatusEffect), 45f, CombatDamageType.Physical);
            }
        }

        private static void ApplySpearDisablerImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var appliesDisruption = AbilityAppliedAnyStatus(
                primaryStatusEffect,
                additionalStatusEffects,
                typeof(ForceDisruptionStatusEffect));
            var appliesSuppression = AbilityAppliedAnyStatus(
                primaryStatusEffect,
                additionalStatusEffects,
                typeof(ForceSuppressionStatusEffect),
                typeof(DisruptionFieldStatusEffect),
                typeof(ForceDisruptionStatusEffect));

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForceNullification) > 0 && appliesDisruption)
            {
                StatusEffect.ApplyStatusEffect(activator, target, new ForceDisruptionStatusEffect(), 30f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0 && appliesSuppression)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0 && appliesDisruption)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }
        }

        public static void ApplySpearDisablerSuppressionRiders(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target))
                return;

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }
        }

        private static void ApplyStaffCrusherImpactRiders(uint activator, uint target)
        {
            var duration = Stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDazedDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        private static void ApplyThrowingDeadeyeImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                Stat.GetStatAdjustment(activator, StatType.ThrowingDeadeyeMarkingToss) <= 0)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(MarkingTossStatusEffect), 30f, CombatDamageType.Physical);
        }

        private static void ApplyTwinBladeDuelistImpactRiders(uint activator, uint target, AbilityDetail ability)
        {
            if (!AbilityMatchesReversalCutTrigger(activator, ability))
            {
                return;
            }

            var duration = TemporaryStatModifier.Consume(
                activator,
                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds,
                StatType.TwinBladeDuelistReversalCut);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        private static void ApplyVibroknifeShadowImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                Stat.GetStatAdjustment(activator, StatType.VibroknifeShadowMarkedForDeath) <= 0)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(MarkedForDeathStatusEffect), 30f, CombatDamageType.Physical);
        }

        private static void ApplyVibroknifeSaboteurImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var toxicCoatingRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
            if (toxicCoatingRank > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ToxinStatusEffect), 30f, CombatDamageType.Poison);
            }

            var sapRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurSapVitalityRank);
            if (sapRank <= 0 ||
                !AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, StatusEffectCategory.Debuff) ||
                !TryUseStatTrigger(target, StatType.VibroknifeSaboteurSapVitalityRank, 6))
            {
                return;
            }

            Stat.ReduceStamina(target, sapRank);
        }

        private static bool AbilityAppliedAnyStatus(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, params Type[] matches)
        {
            if (primaryStatusEffect != null && matches.Contains(primaryStatusEffect))
                return true;

            return additionalStatusEffects?.Any(matches.Contains) ?? false;
        }

        private static bool AbilityAppliedAnyStatusCategory(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, StatusEffectCategory category)
        {
            if (StatusEffectTypeHasCategory(primaryStatusEffect, category))
                return true;

            return additionalStatusEffects?.Any(statusEffect => StatusEffectTypeHasCategory(statusEffect, category)) ?? false;
        }

        private static bool StatusEffectTypeHasCategory(Type statusEffectType, StatusEffectCategory category)
        {
            if (statusEffectType == null || !typeof(IStatusEffect).IsAssignableFrom(statusEffectType))
                return false;

            var statusEffect = (IStatusEffect)Activator.CreateInstance(statusEffectType);
            return (statusEffect.Categories & category) != 0;
        }

        private static void ApplyStatusAppliedEffects(
            uint activator,
            uint target,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            if (!statusApplied)
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedRequiredCategory));
            if (requiredCategory == 0 ||
                !AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, requiredCategory))
            {
                return;
            }

            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilitySkillType));
            var damageBonus = Stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityDamageBonus);
            var criticalRate = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment);
            var window = Stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityWindowSeconds);
            GrantNextSkillAbilityBonuses(activator, skillType, damageBonus, criticalRate, window);

            var nextAttackDamage = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextAttackDamageBonus);
            var nextAttackWindow = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextAttackWindowSeconds);
            GrantStatusAppliedNextAttackDamageBonus(activator, nextAttackDamage, nextAttackWindow);

            ApplyStatusAppliedSelfEffects(activator);
            ApplyStatusAppliedTargetEffects(activator, target);
        }

        private static void ApplyStatusAppliedSelfEffects(uint activator)
        {
            var duration = Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDurationSeconds);
            if (duration <= 0)
                return;

            ReplaceTemporaryStat(
                activator,
                Stat.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection),
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfAttackDeflection),
                duration,
                StatType.StatusAppliedSelfAttackDeflection);
            ReplaceTemporaryStat(
                activator,
                StatType.PhysicalDefensePercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfDefensePercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.EvasionPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEvasionPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEvasionPercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.ForceAttackPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfForceAttackPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfForceAttackPercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.AttackDelayReductionPercent,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfHastePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfHastePercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.EnmityPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEnmityPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEnmityPercentAdjustment);

            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfStaminaRestore);
            if (staminaRestore > 0)
                Stat.RestoreStamina(activator, staminaRestore);
        }

        private static void ApplyStatusAppliedTargetEffects(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetDurationSeconds);
            if (duration <= 0)
                return;

            ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment);
            ReplaceTemporaryStat(
                target,
                StatType.AccuracyPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetAccuracyPercentAdjustment),
                duration,
                StatType.StatusAppliedTargetAccuracyPercentAdjustment);
        }

        private static void ReplaceTemporaryStat(
            uint target,
            StatType statType,
            int amount,
            int durationSeconds,
            StatType group)
        {
            if (statType == StatType.Invalid || amount == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(target, statType, amount, durationSeconds, group);
        }

        private static void ApplyAbilityTargetStatusEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility || !GetIsObjectValid(target))
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusRequiredCategory));
            if (requiredCategory == 0 || !StatusEffect.HasStatusEffectCategory(target, requiredCategory))
                return;

            var physicalDefense = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityTargetStatusDurationSeconds);
            ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                physicalDefense,
                duration,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
        }

        public static void ApplyStatusAppliedTargetStaminaDrain(
            uint activator,
            uint target,
            StatusEffectCategory appliedCategories)
        {
            if (!GetIsObjectValid(activator) ||
                !GetIsObjectValid(target) ||
                activator == target)
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetStaminaDrainRequiredCategory));
            var staminaDrain = Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetStaminaDrain);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetStaminaDrainCooldownSeconds);
            if (requiredCategory == 0 ||
                staminaDrain <= 0 ||
                cooldown <= 0 ||
                (appliedCategories & requiredCategory) == 0 ||
                !TryUseStatTrigger(activator, StatType.StatusAppliedTargetStaminaDrain, cooldown))
            {
                return;
            }

            var staminaBefore = Stat.GetCurrentStamina(target);
            Stat.ReduceStamina(target, staminaDrain);
            var staminaDrained = Math.Max(0, staminaBefore - Stat.GetCurrentStamina(target));
            FloatingTextStringOnCreature(
                ColorToken.Combat($"-{staminaDrained} STM"),
                target,
                false);
        }

        private static void ApplyAreaAbilityTargetHitSequenceEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsAreaAbility || !GetIsObjectValid(target))
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityTargetHitSequenceSkillType));
            var requiredCount = Stat.GetStatAdjustment(activator, StatType.AreaAbilityTargetHitSequenceCountRequired);
            var windowSeconds = Stat.GetStatAdjustment(activator, StatType.AreaAbilityTargetHitSequenceWindowSeconds);
            var exposedDuration = Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds);
            if (!SkillTypeMatches(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                windowSeconds <= 0 ||
                exposedDuration <= 0)
            {
                return;
            }

            var key = (activator, target);
            var now = DateTime.UtcNow;
            var count = 1;
            if (_areaAbilityTargetHitSequences.TryGetValue(key, out var state) &&
                (now - state.LastHit).TotalSeconds <= windowSeconds)
            {
                count = state.Count + 1;
            }

            if (count >= requiredCount)
            {
                _areaAbilityTargetHitSequences.Remove(key);
                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
                return;
            }

            _areaAbilityTargetHitSequences[key] = new TargetHitSequenceState
            {
                Count = count,
                LastHit = now
            };
        }

        private static void ApplyGuardedHitNextSkillAbilityExposedStatus(uint activator, uint target, SkillType skillType)
        {
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return;

            var duration = TemporaryStatModifier.Consume(
                activator,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            if (duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), duration, CombatDamageType.Physical);
        }

        public static int ApplyTriggeredDamage(
            uint activator,
            uint target,
            int damage,
            CombatDamageType damageType,
            SkillType skillType = SkillType.Invalid,
            bool typedLeadershipReductionAlreadyApplied = false)
        {
            if (damage <= 0)
                return 0;

            damage = ApplyDamageTypeDealtModifiers(activator, damage, damageType);
            damage = Resistance.ApplyResistanceToDamage(target, damageType, damage);
            if (damage <= 0)
                return 0;

            damage = ApplyDamageTakenModifiers(
                target,
                damage,
                activator,
                damageType,
                typedLeadershipReductionAlreadyApplied: typedLeadershipReductionAlreadyApplied);
            if (damage <= 0)
                return 0;

            var effectDamageType = damageType.GetNWScriptDamageType();
            if (!Ability.TryQueueTrackedDamageEffect(activator, target, damage, effectDamageType))
            {
                AssignCommand(
                    activator,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(damage, effectDamageType),
                        target));
            }

            ApplyDamageDealtEffects(activator, target, damage, skillType, damageType, CombatDamageDeliveryType.Triggered);
            StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered);
            return damage;
        }

        private const int DeflectingReturnCooldownSeconds = 6;

        /// <summary>
        /// Pure math for Deflecting Return: reflect <paramref name="reflectPercent"/>% of the deflected
        /// ranged attack's weapon damage, capped at <paramref name="capPercent"/>% of the deflector's own
        /// weapon damage. Both damage inputs are the SWLOR weapon DMG values, so they share a scale.
        /// </summary>
        public static int GetRangedDeflectionReflectionAmount(
            int attackWeaponDamage,
            int reflectPercent,
            int deflectorWeaponDamage,
            int capPercent)
        {
            if (attackWeaponDamage <= 0 || reflectPercent <= 0)
                return 0;

            var reflected = attackWeaponDamage * reflectPercent / 100;
            if (capPercent > 0 && deflectorWeaponDamage > 0)
                reflected = Math.Min(reflected, deflectorWeaponDamage * capPercent / 100);

            return Math.Max(0, reflected);
        }

        public static (int ReflectPercent, int CapPercent) GetRangedDeflectionReflectionRates(
            int baseReflectPercent,
            int baseCapPercent,
            int embattledStacks,
            int embattledHighStackThreshold,
            int embattledHighStackBonusPercent,
            int overrideReflectPercent,
            int overrideCapPercent)
        {
            if (overrideReflectPercent > 0)
            {
                return (
                    overrideReflectPercent,
                    overrideCapPercent > 0 ? overrideCapPercent : baseCapPercent);
            }

            var reflectPercent = baseReflectPercent;
            if (embattledHighStackThreshold > 0 && embattledStacks >= embattledHighStackThreshold)
                reflectPercent += Math.Max(0, embattledHighStackBonusPercent);

            return (Math.Max(0, reflectPercent), Math.Max(0, baseCapPercent));
        }

        /// <summary>
        /// Deflecting Return: when the defender deflects a directly targeted ranged attack, reflect a capped
        /// share of weapon damage back to the attacker as Force damage. Fires at most once every
        /// <see cref="DeflectingReturnCooldownSeconds"/> seconds. Reflection amount is driven by the
        /// <see cref="StatType.RangedDeflectionReflectionPercent"/> / <see cref="StatType.RangedDeflectionReflectionCapPercent"/>
        /// stats the Deflecting Return perk grants. Embattled high-stack bonuses and finite capstone
        /// overrides are also stat-driven.
        /// </summary>
        public static int ApplyRangedDeflectionReflection(uint defender, uint attacker, SkillType attackerWeaponSkill)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var (reflectPercent, capPercent) = GetRangedDeflectionReflectionRates(
                Stat.GetStatAdjustment(defender, StatType.RangedDeflectionReflectionPercent),
                Stat.GetStatAdjustment(defender, StatType.RangedDeflectionReflectionCapPercent),
                EmbattledStatusEffect.GetStackCount(defender),
                Stat.GetStatAdjustment(defender, StatType.EmbattledHighStackThreshold),
                Stat.GetStatAdjustment(defender, StatType.EmbattledHighStackDeflectionReflectionBonusPercent),
                Stat.GetStatAdjustment(defender, StatType.RangedDeflectionReflectionOverridePercent),
                Stat.GetStatAdjustment(defender, StatType.RangedDeflectionReflectionCapOverridePercent));
            if (reflectPercent <= 0)
                return 0;

            var reflected = GetRangedDeflectionReflectionAmount(
                GetCombatImpactWeaponDamage(attacker, attackerWeaponSkill),
                reflectPercent,
                GetCombatImpactWeaponDamage(defender, GetEquippedWeaponSkillType(defender)),
                capPercent);
            if (reflected <= 0)
                return 0;

            // Consume the shared cooldown only when a hit will actually be reflected.
            if (!TryUseStatTrigger(defender, StatType.RangedDeflectionReflectionPercent, DeflectingReturnCooldownSeconds))
                return 0;

            var appliedDamage = ApplyTriggeredDamage(defender, attacker, reflected, CombatDamageType.Force);
            if (appliedDamage <= 0)
                return 0;

            Messaging.SendMessageNearbyToPlayers(
                defender,
                observer => BuildDeflectingReturnCombatLogMessage(observer, defender, attacker, appliedDamage),
                60f);
            return appliedDamage;
        }

        public static string BuildDeflectingReturnCombatLogMessage(
            uint observer,
            uint defender,
            uint attacker,
            int damage)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);
            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{defenderName}'s Deflecting Return reflects {damage} Force damage to {attackerName}.");
        }

        private static void ApplyGuardiansResolve(uint activator)
        {
            var shieldPercent = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds);
            if (shieldPercent <= 0 || duration <= 0 || !TryUseStatTrigger(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, cooldown))
                return;

            var shieldAmount = GameMath.PercentOf(GetMaxHitPoints(activator), shieldPercent);
            TemporaryHitPointEffects.ApplyFlat(activator, "GUARDIANS_RESOLVE", shieldAmount, duration);
            StatusEffect.ApplyStatusEffect(activator, activator, new GuardiansResolveStatusEffect(shieldAmount), duration);
        }

        private static void ApplyHeavyVibrobladeActivatedEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType))
            {
                ApplyNextAutoAttackDamageBonus(
                    activator,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
            }

            if (AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType))
            {
                ApplyGuardiansResolve(activator);
            }

        }

        private static void ApplyBeastBalancedAbilityRecovery(uint activator, AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.BeastBalancedAbilityStaminaRestoreCategoryId))
            {
                return;
            }

            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.BeastBalancedAbilityStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.BeastBalancedAbilityStaminaRestoreCooldownSeconds);
            if (staminaRestore <= 0 || !TryUseStatTrigger(activator, StatType.BeastBalancedAbilityStaminaRestore, cooldown))
                return;

            Stat.RestoreStamina(activator, staminaRestore);

            var master = GetMaster(activator);
            if (GetIsObjectValid(master))
            {
                Stat.RestoreStamina(master, staminaRestore);
            }
        }

        private static void ApplyVibroknifeShadowActivatedEffects(
            uint activator,
            AbilityDetail ability)
        {
            var rank = Stat.GetStatAdjustment(activator, StatType.VibroknifeShadowEvasiveCombatRank);
            if (rank <= 0 || !ability.IsAreaAbility && !ability.IsSingleTargetAbility)
                return;

            var evasion = rank >= 2 ? 20 : 10;
            var enmity = rank >= 2 ? -25 : -15;
            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new EvasiveCombatStatusEffect(-15, evasion, enmity),
                8f);
        }

        private static void ApplyPistolSkirmisherActivatedEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            var duration = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds);
            if (duration <= 0 || !ability.IsAreaAbility && !ability.IsSingleTargetAbility)
                return;

            var evasion = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent);
            if (evasion != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    duration,
                    StatType.PistolSkirmisherEvasiveAbilityEvasionPercent);
            }

            var nextDamage = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus);
            if (nextDamage > 0)
            {
                GrantNextSkillAbilityBonuses(activator, SkillType.Pistol, nextDamage, 0, duration);
            }

            var reduction = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent);
            if (reduction > 0 && GetIsObjectValid(target))
            {
                Enmity.ReduceEnmity(activator, target, reduction);
            }
        }

        private static void ApplyLightsaberOffenseActivatedEffects(uint activator, uint target)
        {
            ApplyLightsaberOffenseCentering(activator, target);
            ApplyLightsaberOffenseSecondWind(activator);
        }

        private static void ApplyLightsaberOffenseCentering(uint activator, uint target)
        {
            var accuracy = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringAccuracyPercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringCooldownSeconds);
            if (accuracy <= 0 ||
                duration <= 0 ||
                !TryUseStatTrigger(activator, StatType.LightsaberOffenseCenteringAccuracyPercent, cooldown))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, activator, new CenteringStatusEffect(accuracy), duration);

            var enmityReduction = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent);
            if (enmityReduction > 0 && GetIsObjectValid(target))
            {
                Enmity.ReduceEnmity(activator, target, enmityReduction);
            }
        }

        private static void ApplyLightsaberOffenseSecondWind(uint activator)
        {
            var thresholdPercent = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindThresholdPercent);
            var basePercent = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent);
            if (thresholdPercent <= 0 || basePercent <= 0)
                return;

            var maximumStamina = Stat.GetMaxStamina(activator);
            if (maximumStamina <= 0 ||
                Stat.GetCurrentStamina(activator) > maximumStamina * (thresholdPercent / 100f))
            {
                return;
            }

            var cooldown = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindCooldownSeconds);
            if (!TryUseStatTrigger(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = GetAbilityTypeFromStatPlusOne(Stat.GetStatAdjustment(
                activator,
                StatType.LightsaberOffenseSecondWindScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                percent += Math.Max(0, GetAbilityScore(activator, scalingAbility));
            }

            var maximumPercent = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent);
            if (maximumPercent > 0)
            {
                percent = Math.Min(percent, maximumPercent);
            }

            Stat.RestoreStamina(activator, GameMath.PercentOf(maximumStamina, percent));
        }

        private static void ApplyLightsaberDefenseActivatedEffects(uint activator)
        {
            var sourceStatType = StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection;
            var attackDeflection = Stat.GetStatAdjustment(activator, sourceStatType);
            var deflectionStatType = Stat.GetGrantedDeflectionStatType(sourceStatType);
            if (deflectionStatType == StatType.Invalid || attackDeflection <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f, false))
            {
                TemporaryStatModifier.Replace(
                    friendly,
                    deflectionStatType,
                    attackDeflection,
                    12f,
                    sourceStatType);
            }
        }

        private static void ApplyLightsaberWardActivatedEffects(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.WardAbilityDefenseCategoryId))
            {
                return;
            }

            var defense = Stat.GetStatAdjustment(activator, StatType.WardAbilityDefensePercentAdjustment);
            var forceDefense = Stat.GetStatAdjustment(activator, StatType.WardAbilityForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.WardAbilityDefenseDurationSeconds);
            if (duration <= 0 || defense == 0 && forceDefense == 0)
                return;

            if (defense != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.PhysicalDefensePercentAdjustment,
                    defense,
                    duration,
                    StatType.WardAbilityDefensePercentAdjustment);
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.WardTargetPhysicalDefensePercentAdjustment,
                    defense,
                    duration,
                    StatType.WardAbilityDefensePercentAdjustment);
            }

            if (forceDefense != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.WardAbilityForceDefensePercentAdjustment);
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.WardTargetForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.WardAbilityForceDefensePercentAdjustment);
            }
        }

        private static void ApplyAbilityUsedPerkCategorySelfDefense(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategorySelfDefenseCategoryId))
            {
                return;
            }

            var evasion = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment);
            var defense = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment);
            var forceDefense = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds);
            if (duration <= 0 || evasion == 0 && defense == 0 && forceDefense == 0)
                return;

            var cooldown = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefenseCooldownSeconds);
            if (!TryUseStatTrigger(
                    activator,
                    StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment,
                    cooldown))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new GuardingStepStatusEffect(evasion, defense, forceDefense),
                duration);
        }

        private static void ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId))
            {
                return;
            }

            var attackDeflection = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var deflectionStatType = Stat.GetGrantedDeflectionStatType(
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds);
            var selfEnmity = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionSelfEnmityPercentAdjustment);
            if (deflectionStatType == StatType.Invalid || attackDeflection <= 0 || duration <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    friendly,
                    new SentinelGuardStatusEffect(attackDeflection, selfEnmity, deflectionStatType),
                    duration);
            }
        }

        public static void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent = 0)
        {
            var window = Math.Max(1, Stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds));

            if (Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(SoulSacrificeStatusEffect), 30f);
            }

            if (Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseSoulAscension) > 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.HeavyVibrobladeOffenseSoulAscension,
                    1,
                    window,
                    StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            }

            ApplyHitPointSpendStaminaRestore(activator);
            ApplyHitPointSpendTemporaryHitPoints(activator, hitPointsSpent);
        }

        private static void ApplyHitPointSpendTemporaryHitPoints(uint activator, int hitPointsSpent)
        {
            if (hitPointsSpent <= 0)
                return;

            var percent = Stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP);
            var duration = Stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPDurationSeconds);
            if (percent <= 0 || duration <= 0)
                return;

            var temporaryHP = GameMath.PercentOf(hitPointsSpent, percent);
            TemporaryHitPointEffects.ApplyFlat(activator, "HIT_POINT_SPEND", temporaryHP, duration);
        }

        private static void ApplyHitPointSpendStaminaRestore(uint activator)
        {
            var basePercent = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent);
            if (basePercent <= 0)
                return;

            var cooldown = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds);
            if (!TryUseStatTrigger(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = GetAbilityTypeFromStatPlusOne(Stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                percent += Math.Max(0, GetAbilityScore(activator, scalingAbility));
            }

            var maximum = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent);
            if (maximum > 0)
            {
                percent = Math.Min(maximum, percent);
            }

            var stamina = GameMath.PercentOf(Stat.GetMaxStamina(activator), percent);
            Stat.RestoreStamina(activator, stamina);
        }

        private static void ApplyForceFPCostActivatedEffects(uint activator, AbilityDetail ability)
        {
            if (GetAbilitySkillType(activator, ability) != SkillType.Force ||
                !ability.Requirements.OfType<AbilityRequirementFP>().Any(x => x.RequiredFP > 0))
            {
                return;
            }

            if (Stat.GetStatAdjustment(activator, StatType.ForcePrecognition) > 0 &&
                TryUseStatTrigger(activator, StatType.ForcePrecognition, 12))
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(PrecognitionStatusEffect), 30f);
            }

            if (Stat.GetStatAdjustment(activator, StatType.ForceConvergence) > 0 &&
                TryUseStatTrigger(activator, StatType.ForceConvergence, 45))
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(ForceConvergenceStatusEffect), 30f);
            }
        }

        private static void ApplyAbilityUsedMasterAbilityHitChance(uint activator)
        {
            var master = GetMaster(activator);
            if (!GetIsObjectValid(master))
                return;

            var adjustment = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds);
            if (adjustment == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                master,
                StatType.AbilityHitChancePercentAdjustment,
                adjustment,
                duration,
                StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
        }

        public static void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || summary == null || summary.ImpactedTargetCount <= 0)
                return;

            switch (summary.SkillType)
            {
                case SkillType.Throwing:
                    ApplyThrowingAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.Saberstaff:
                    ApplyAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.Spear:
                    ApplySpearAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.TwinBlade:
                    ApplyTwinBladeAbilityImpactEffects(activator, summary);
                    break;
            }
        }

        public static int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            var criticalRate = GetAbilityCriticalRate(
                attacker,
                skillType,
                isAreaAbility,
                criticalRateAdjustment,
                defender);

            return criticalRate > 0 && Random.D100(1) <= criticalRate
                ? StandardCriticalRating
                : 0;
        }

        private static void ApplyGuardedHitNextAttackEffects(uint creature)
        {
            var primaryDMGBonus = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackDMGBonus);
            var criticalRate = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackCriticalRatePercentAdjustment);
            var primaryWindow = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackWindowSeconds);
            var secondaryDMGBonus = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackDMGBonus);
            var enmityBonus = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackEnmityBonus);
            var secondaryWindow = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackWindowSeconds);
            var dmgBonus = primaryDMGBonus + secondaryDMGBonus;
            var window = Math.Max(primaryWindow, secondaryWindow);
            if (window <= 0 || dmgBonus == 0 && criticalRate == 0 && enmityBonus == 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitDMGBonus,
                dmgBonus,
                window,
                StatType.NextAttackGuardedHitDMGBonus);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitCriticalRatePercentAdjustment,
                criticalRate,
                window,
                StatType.NextAttackGuardedHitDMGBonus);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitEnmityBonus,
                enmityBonus,
                window,
                StatType.NextAttackGuardedHitDMGBonus);

            if (GetIsPC(creature))
            {
                var criticalText = criticalRate != 0
                    ? $", +{criticalRate}% Crit"
                    : string.Empty;
                var enmityText = enmityBonus != 0
                    ? $", +{enmityBonus} Enmity"
                    : string.Empty;
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Counter Ready: +{dmgBonus} DMG{criticalText}{enmityText}"),
                    creature,
                    false);
            }
        }

        public static int GetAbilityCriticalRate(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalAdjustment = criticalRateAdjustment;
            totalAdjustment += GetSkillCriticalRatePercentAdjustment(attacker, skillType);
            totalAdjustment += GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                PerkType.Invalid,
                StatType.AbilityCriticalRatePercentAdjustmentSkillType,
                StatType.AbilityCriticalRatePercentAdjustmentPerkType,
                StatType.AbilityCriticalRatePercentAdjustmentSecondaryPerkType,
                StatType.AbilityCriticalRatePercentAdjustment,
                false);

            if (isAreaAbility && skillType == SkillType.TwinBlade)
            {
                totalAdjustment += Stat.GetStatAdjustment(attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment);
            }

            if (skillType == SkillType.Throwing &&
                GetIsObjectValid(defender) &&
                (StatusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect)) ||
                 StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding)))
            {
                totalAdjustment += Stat.GetStatAdjustment(attacker, StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment);
            }

            if (GetIsObjectValid(defender) && IsTargetNotFacingAttacker(attacker, defender))
            {
                totalAdjustment += Stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment);
            }

            totalAdjustment += GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender);
            totalAdjustment += GetTargetStatusCriticalRateAdjustment(attacker, defender);
            totalAdjustment += GetSideAttackCriticalRateAdjustment(attacker, defender, skillType);
            totalAdjustment += GetBackAttackCriticalRateAdjustment(attacker, defender, skillType);

            return CalculateAbilityCriticalChance(totalAdjustment);
        }

        public static int CalculateAbilityCriticalChance(int totalPercentAdjustment)
        {
            return Math.Clamp(
                MinimumCriticalRate + totalPercentAdjustment,
                MinimumCriticalRate,
                MaximumCriticalRate);
        }

        private static bool? _abilityHitResolutionOverride;

        /// <summary>
        /// Forces every TryResolveAbilityHit call to the given outcome instead of rolling.
        /// Intended solely for the in-engine test harness: ability behavior assertions cannot
        /// be made against a hit roll that legitimately misses up to 5% of the time even at
        /// capped hit rates. Pass null to restore normal resolution. Always restore in a
        /// finally block.
        /// </summary>
        public static void SetAbilityHitResolutionOverride(bool? forcedOutcome)
        {
            _abilityHitResolutionOverride = forcedOutcome;
        }

        private static bool? _autoAttackHitResolutionOverride;

        /// <summary>
        /// Forces every native auto-attack roll (ResolveAttackRoll hook) to the given outcome
        /// instead of rolling. Intended solely for the in-engine test harness: ability damage
        /// assertions cannot distinguish ability damage from the activator's resumed
        /// auto-attacks, so behavior sweeps force auto-attack misses. Pass null to restore
        /// normal resolution. Always restore in a finally block.
        /// </summary>
        public static void SetAutoAttackHitResolutionOverride(bool? forcedOutcome)
        {
            _autoAttackHitResolutionOverride = forcedOutcome;
        }

        /// <summary>
        /// The current auto-attack resolution override, if any. Read by the native
        /// ResolveAttackRoll hook.
        /// </summary>
        public static bool? GetAutoAttackHitResolutionOverride()
        {
            return _autoAttackHitResolutionOverride;
        }

        public static bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate,
            int hitChancePercentAdjustment = 0,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid)
        {
            hitRate = 100;
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return true;

            var accuracy = GetAbilityAccuracy(attacker, defender, skillType, skillLevelOverride, statOverride);
            var evasion = Stat.GetEvasion(defender, SkillType.Invalid, skillType);
            evasion = ApplySideAttackEvasionIgnore(attacker, defender, skillType, evasion);

            var modifier = hitChancePercentAdjustment + GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.AbilityHitChancePercentAdjustment,
                false);
            modifier += GetTargetedAbilityAdjustment(
                attacker,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.TargetedAbilityHitChancePercentAdjustment);
            modifier += GetPhysicalAndForceAbilityHitChanceAdjustment(attacker, skillType);
            modifier += GetIncomingAbilityHitChanceAdjustment(defender, skillType);
            modifier += GetSideAttackHitChanceAdjustment(attacker, defender, skillType);
            modifier += GetIdleAbilityHitChanceAdjustment(attacker, skillType);
            modifier += GetSuppressionAbilityHitChanceAdjustment(attacker, defender, skillType);
            modifier += GetHitChanceAgainstSunderedTargetAdjustment(attacker, defender);
            if (skillType == SkillType.Force)
            {
                modifier += Perk.GetForceAffinityHitChanceAdjustment(attacker, perkType);
            }

            hitRate = CalculateHitRate(accuracy, evasion, modifier);
            var isHit = _abilityHitResolutionOverride ?? Random.D100(1) <= hitRate;
            if (!isHit && skillType == SkillType.Force)
            {
                ApplyForceAbilityEvadedEffects(defender);
            }

            return isHit;
        }

        private static int GetAbilityAccuracy(
            uint attacker,
            uint defender,
            SkillType skillType,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid)
        {
            var weapon = GetRelevantSkillWeapon(attacker, skillType);
            var usesForceAccuracy = skillType == SkillType.Force;
            var accuracyStatOverride = usesForceAccuracy ? AbilityType.Willpower : statOverride;
            var accuracy = Stat.GetAccuracy(
                attacker,
                weapon,
                accuracyStatOverride,
                skillType,
                skillLevelOverride,
                ignoreWeaponAccuracyStatOverride: usesForceAccuracy);
            return ApplyStatusSourceAccuracyModifiers(attacker, defender, accuracy);
        }

        private static uint GetRelevantSkillWeapon(uint creature, SkillType skillType)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            if (GetIsObjectValid(rightHand) &&
                (skillType == SkillType.Invalid ||
                 Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(rightHand)) == skillType ||
                 skillType == SkillType.Force))
                return rightHand;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (GetIsObjectValid(leftHand))
                return leftHand;

            return skillType == SkillType.BeastMastery
                ? GetCreatureNaturalWeapon(creature)
                : rightHand;
        }

        private static uint GetCreatureNaturalWeapon(uint creature)
        {
            var creatureRight = GetItemInSlot(InventorySlot.CreatureRight, creature);
            if (GetIsObjectValid(creatureRight))
                return creatureRight;

            var creatureLeft = GetItemInSlot(InventorySlot.CreatureLeft, creature);
            if (GetIsObjectValid(creatureLeft))
                return creatureLeft;

            return GetItemInSlot(InventorySlot.CreatureBite, creature);
        }

        private static int GetAbilityHitOrCriticalAdjustment(
            uint creature,
            SkillType skillType,
            PerkType perkType,
            StatType skillTypeStat,
            StatType primaryPerkStat,
            StatType secondaryPerkStat,
            StatType adjustmentStat,
            bool includePerkTargeting)
        {
            var adjustment = 0;
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(creature, skillTypeStat));
            if (SkillTypeMatches(skillType, requiredSkillType))
            {
                adjustment += Stat.GetStatAdjustment(creature, adjustmentStat);
            }

            if (includePerkTargeting)
            {
                adjustment += GetTargetedAbilityAdjustment(
                    creature,
                    perkType,
                    primaryPerkStat,
                    secondaryPerkStat,
                    adjustmentStat);
            }

            return adjustment;
        }

        private static int GetIncomingAbilityHitChanceAdjustment(uint defender, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                defender,
                StatType.IncomingAbilityHitChancePercentAdjustmentSkillType));

            return SkillTypeMatches(skillType, requiredSkillType)
                ? Stat.GetStatAdjustment(defender, StatType.IncomingAbilityHitChancePercentAdjustment)
                : 0;
        }

        private static int GetSuppressionAbilityHitChanceAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            if (!IsRangedWeaponSkill(skillType))
                return 0;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityHitChanceAgainstSuppressionStackPercentAdjustment);
            if (adjustment == 0 ||
                GetSuppressionStackCount(defender, attacker) <= 0)
            {
                return 0;
            }

            var key = (attacker, defender);
            if (!_pendingSuppressionAbilityUses.TryGetValue(key, out var state))
                return 0;

            if (state.Expiration <= DateTime.UtcNow)
            {
                _pendingSuppressionAbilityUses.Remove(key);
                return 0;
            }

            if (!HasCurrentSuppressionAbilityUseStack(attacker, defender, state.SuppressionEffectIds))
            {
                _pendingSuppressionAbilityUses.Remove(key);
                return 0;
            }

            _pendingSuppressionAbilityUses.Remove(key);
            return adjustment;
        }

        private static bool HasCurrentSuppressionAbilityUseStack(
            uint attacker,
            uint defender,
            HashSet<string> suppressionEffectIds)
        {
            return StatusEffect.GetCreatureStatusEffects(defender)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Any(effect => effect.Source == attacker && suppressionEffectIds.Contains(effect.Id));
        }

        public static int GetHitChanceAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? Stat.GetStatAdjustment(attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        public static int GetCriticalRateAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? Stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        private static void ApplyAutoAttackSunderedTargetFPRestore(uint attacker, uint defender)
        {
            var fpRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackSunderedTargetFPRestore);
            if (fpRestore <= 0 ||
                !GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect)))
            {
                return;
            }

            Stat.RestoreFP(attacker, fpRestore);
        }

        private static int GetPhysicalAndForceAbilityHitChanceAdjustment(uint attacker, SkillType skillType)
        {
            return IsWeaponOrForceAbility(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.PhysicalAndForceAbilityHitChancePercentAdjustment)
                : 0;
        }

        private static void ApplyForceAbilityEvadedEffects(uint defender)
        {
            var forceDefense = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedDurationSeconds);
            var staminaRestore = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedCooldownSeconds);
            if (forceDefense == 0 && staminaRestore <= 0 ||
                duration <= 0 ||
                !TryUseStatTrigger(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, cooldown))
                return;

            if (forceDefense != 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            }

            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(defender, staminaRestore);
            }
        }

        public static int ApplyDefenseIgnore(int defense, int defenseIgnorePercent)
        {
            if (defense <= 0 || defenseIgnorePercent <= 0)
                return defense;

            return Math.Max(0, defense - (int)Math.Ceiling(defense * (Math.Min(100, defenseIgnorePercent) / 100f)));
        }

        public static bool IsTargetNotFacingAttacker(uint attacker, uint defender)
        {
            return GetFacingAngleDegrees(attacker, defender) > 90.0;
        }

        public static bool CanConsumeNextAbilityNoDelay(AbilityDetail ability)
        {
            return ability?.IsHostileAbility == true;
        }

        /// <summary>
        /// Consumes the armed next-ability delay buff and combines it with active limited Haste,
        /// returning the percent (1-100) the activation delay is reduced by, or 0 when neither
        /// applies. An armed buff with no partial-reduction partner removes the delay entirely
        /// (100).
        /// </summary>
        public static int ConsumeNextAbilityDelayReductionPercent(uint creature, AbilityDetail ability)
        {
            if (!CanConsumeNextAbilityNoDelay(ability))
                return 0;

            if (IsAttackDelayReductionSuppressed(creature))
                return 0;

            var skillType = GetAbilitySkillType(creature, ability);
            var hasRangedStatusNoDelay = IsRangedWeaponSkill(skillType) &&
                                         Stat.GetStatAdjustment(creature, StatType.RangedAttackNoDelay) > 0;
            if (hasRangedStatusNoDelay)
                return 100;

            var temporaryReductionPercent = ConsumeNextAbilityDelayReductionPercent(creature, skillType);
            var hasLimitedReduction = StatusEffect.TryGetLimitedAttackDelayReduction(
                creature,
                skillType,
                out var limitedReductionPercent,
                out _);

            return hasLimitedReduction
                ? Math.Clamp(temporaryReductionPercent + limitedReductionPercent, 0, 100)
                : temporaryReductionPercent;
        }

        private static int ConsumeNextAbilityDelayReductionPercent(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackNoDelay,
                StatType.NextAttackNoDelay));
            if (storedSkillType != skillType)
                return 0;

            var reductionPercent = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackDelayReductionPercent,
                StatType.NextAttackNoDelay);

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackNoDelay,
                StatType.NextAttackNoDelay);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackDelayReductionPercent,
                StatType.NextAttackNoDelay);

            return reductionPercent is > 0 and < 100 ? reductionPercent : 100;
        }

        public static bool HasNextAutoAttackNoDelay(uint creature, SkillType skillType)
        {
            if (IsAttackDelayReductionSuppressed(creature))
                return false;

            if (IsRangedWeaponSkill(skillType) &&
                Stat.GetStatAdjustment(creature, StatType.RangedAttackNoDelay) > 0)
            {
                return true;
            }

            return HasTemporaryNextAutoAttackNoDelay(creature, skillType);
        }

        public static bool HasTemporaryNextAutoAttackNoDelay(uint creature, SkillType skillType)
        {
            if (IsAttackDelayReductionSuppressed(creature))
                return false;

            var appliesToAllSkills = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelayAllSkills,
                StatType.NextAutoAttackNoDelayAllSkills) > 0;
            if (appliesToAllSkills)
                return true;

            if (skillType == SkillType.Invalid)
                return false;
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                StatType.NextAutoAttackNoDelaySkillType));

            return storedSkillType == skillType;
        }

        public static bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType)
        {
            var appliesToRangedStatus = IsRangedWeaponSkill(skillType) &&
                                        Stat.GetStatAdjustment(creature, StatType.RangedAttackNoDelay) > 0;
            var appliesToAllSkills = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelayAllSkills,
                StatType.NextAutoAttackNoDelayAllSkills) > 0;
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                StatType.NextAutoAttackNoDelaySkillType));
            var appliesToSkill = skillType != SkillType.Invalid && storedSkillType == skillType;
            if (!appliesToRangedStatus && !appliesToAllSkills && !appliesToSkill)
                return false;

            if (appliesToAllSkills)
            {
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.NextAutoAttackNoDelayAllSkills,
                    StatType.NextAutoAttackNoDelayAllSkills);
            }

            if (appliesToSkill)
            {
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.NextAutoAttackNoDelaySkillType,
                    StatType.NextAutoAttackNoDelaySkillType);
            }

            return true;
        }

        public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                StatType.NextAutoAttackCriticalRateSkillType));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return 0;

            var criticalRate = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAutoAttackCriticalRatePercentAdjustment,
                StatType.NextAutoAttackCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                StatType.NextAutoAttackCriticalRateSkillType);

            return criticalRate;
        }

        public static bool IsAttackDelayReductionSuppressed(uint creature)
        {
            return Stat.GetStatAdjustment(creature, StatType.AttackDelayReductionSuppressed) > 0;
        }

        public static void GrantNextAutoAttackNoDelay(uint creature, SkillType skillType, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAutoAttackNoDelaySkillType);
        }

        public static void GrantNextAutoAttackNoDelay(uint creature, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackNoDelayAllSkills,
                1,
                durationSeconds,
                StatType.NextAutoAttackNoDelayAllSkills);
        }

        public static void GrantNextAutoAttackCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                criticalRatePercentAdjustment == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAutoAttackCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                durationSeconds,
                StatType.NextAutoAttackCriticalRateSkillType);
        }

        public static void GrantNextAbilityNoDelay(uint creature, int skillTypeValue, int durationSeconds)
        {
            var skillType = GetSkillTypeFromStat(skillTypeValue);
            GrantNextAbilityNoDelay(creature, skillType, durationSeconds);
        }

        /// <summary>
        /// Arms the next matching ability's delay buff. A <paramref name="delayReductionPercent"/>
        /// of 1-99 reduces the activation delay by that percent; 0 or 100+ removes it entirely.
        /// </summary>
        public static void GrantNextAbilityNoDelay(
            uint creature,
            SkillType skillType,
            int durationSeconds,
            int delayReductionPercent = 0)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackNoDelay,
                (int)skillType,
                durationSeconds,
                StatType.NextAttackNoDelay);

            if (delayReductionPercent is > 0 and < 100)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextAttackDelayReductionPercent,
                    delayReductionPercent,
                    durationSeconds,
                    StatType.NextAttackNoDelay);
            }
            else
            {
                // A full no-delay arm must not inherit a partial percent left over from an earlier
                // partial arm that has not expired yet.
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.NextAttackDelayReductionPercent,
                    StatType.NextAttackNoDelay);
            }
        }

        public static SkillType GetAbilitySkillType(uint creature, AbilityDetail ability)
        {
            if (ability == null || ability.SkillType != SkillType.Invalid)
                return ability?.SkillType ?? SkillType.Invalid;

            return GetEquippedWeaponSkillType(creature);
        }

        public static SkillType GetEquippedWeaponSkillType(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return SkillType.Invalid;

            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            if (GetIsObjectValid(rightHand))
            {
                var rightSkillType = Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(rightHand));
                if (rightSkillType != SkillType.Invalid)
                    return rightSkillType;
            }

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!GetIsObjectValid(leftHand))
                return SkillType.Invalid;

            return Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(leftHand));
        }

        public static bool HasNextAbilityNoStaminaCost(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return false;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityNoStaminaCostSkillType,
                StatType.NextAbilityNoStaminaCostSkillType));

            return storedSkillType == skillType;
        }

        public static bool ConsumeNextAbilityNoStaminaCost(uint creature, SkillType skillType)
        {
            if (!HasNextAbilityNoStaminaCost(creature, skillType))
                return false;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityNoStaminaCostSkillType,
                StatType.NextAbilityNoStaminaCostSkillType);

            return true;
        }

        public static int GetNextSkillAbilityStaminaCostAdjustment(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType));

            return storedSkillType == skillType
                ? TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.NextSkillAbilityStaminaCostAdjustment,
                    StatType.NextSkillAbilityStaminaCostAdjustmentSkillType)
                : 0;
        }

        public static int ConsumeNextSkillAbilityStaminaCostAdjustment(uint creature, SkillType skillType)
        {
            var adjustment = GetNextSkillAbilityStaminaCostAdjustment(creature, skillType);
            if (adjustment == 0)
                return 0;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustment,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);

            return adjustment;
        }

        public static (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return (0, 0, 0);

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return (0, 0, 0);

            var damageBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDamageBonus,
                StatType.NextSkillAbilitySkillType);
            var criticalRate = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            var defenseIgnore = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType);

            return (damageBonus, criticalRate, defenseIgnore);
        }

        public static (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(
            uint creature)
        {
            var attackBonuses = ConsumeNextAttackGuardedHitAutoAttackBonuses(creature);
            var criticalRate = ConsumeNextAttackGuardedHitCriticalRateBonus(creature);

            return (attackBonuses.DMGBonus, criticalRate, attackBonuses.EnmityBonus);
        }

        public static (int DMGBonus, int EnmityBonus) ConsumeNextAttackGuardedHitAutoAttackBonuses(uint creature)
        {
            var dmgBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitDMGBonus,
                StatType.NextAttackGuardedHitDMGBonus);
            var enmityBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitEnmityBonus,
                StatType.NextAttackGuardedHitDMGBonus);

            return (dmgBonus, enmityBonus);
        }

        public static int ConsumeNextAttackGuardedHitCriticalRateBonus(uint creature)
        {
            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitCriticalRatePercentAdjustment,
                StatType.NextAttackGuardedHitDMGBonus);
        }

        public static int ConsumeStatusAppliedNextAttackDamageBonus(uint creature)
        {
            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackStatusAppliedDMGBonus,
                StatType.NextAttackStatusAppliedDMGBonus);
        }

        public static int GetStatusAppliedNextAttackDamageBonus(uint creature)
        {
            return TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackStatusAppliedDMGBonus,
                StatType.NextAttackStatusAppliedDMGBonus);
        }

        public static void ApplyNextAttackGuardedHitEnmityBonus(
            uint attacker,
            uint defender,
            int enmityBonus)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                enmityBonus <= 0)
            {
                return;
            }

            Enmity.ModifyEnmity(attacker, defender, enmityBonus);
        }

        public static void GrantNextAbilityDamageBonus(uint creature, int perkTypeValue, int bonus, int durationSeconds)
        {
            var perkType = GetPerkTypeFromStat(perkTypeValue);
            GrantNextAbilityDamageBonus(creature, perkType, bonus, durationSeconds);
        }

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            int skillTypeValue,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds)
        {
            var skillType = GetSkillTypeFromStat(skillTypeValue);
            GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRatePercentAdjustment, durationSeconds);
        }

        public static int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityDamageBonus,
                GetPerkTypeGroup(perkType));
        }

        public static int GetNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                GetPerkTypeGroup(perkType));
        }

        public static int ConsumeNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                GetPerkTypeGroup(perkType));
        }

        public static int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType)
        {
            var adjustment = GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDamageFlatAdjustmentPerkType,
                StatType.AbilityDamageFlatAdjustmentSecondaryPerkType,
                StatType.AbilityDamageFlatAdjustment);

            adjustment += GetRangedAttackDamageFlatAdjustment(creature, skillType);

            return adjustment;
        }

        public static int GetAbilityStatusCategoryDamageBonus(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories)
        {
            return GetAbilityStatusCategoryAdjustment(
                creature,
                skillType,
                appliedCategories,
                StatType.AbilityStatusCategoryDamageBonus);
        }

        public static int GetAbilityStatusCategoryHitChancePercentAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories)
        {
            return GetAbilityStatusCategoryAdjustment(
                creature,
                skillType,
                appliedCategories,
                StatType.AbilityStatusCategoryHitChancePercentAdjustment);
        }

        private static int GetAbilityStatusCategoryAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories,
            StatType adjustmentStatType)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || appliedCategories == StatusEffectCategory.None)
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStatusCategoryBonusSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var requiredCategories = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStatusCategoryBonusRequiredCategory));
            if (requiredCategories == StatusEffectCategory.None ||
                (appliedCategories & requiredCategories) == 0)
            {
                return 0;
            }

            return Stat.GetStatAdjustment(creature, adjustmentStatType);
        }

        public static int GetAbilityStaminaCostFlatAdjustment(uint creature, PerkType perkType)
        {
            return GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityStaminaCostFlatAdjustmentPerkType,
                StatType.AbilityStaminaCostFlatAdjustmentSecondaryPerkType,
                StatType.AbilityStaminaCostFlatAdjustment);
        }

        public static int GetAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability)
        {
            if (ability == null)
                return 0;

            var adjustment = GetAbilityStaminaCostFlatAdjustment(creature, ability.EffectiveLevelPerkType);
            if (ability.IsHostileAbility)
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.HostileAbilityStaminaCostFlatAdjustment);
            }

            var skillType = GetAbilitySkillType(creature, ability);
            var flatSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType));
            if (SkillTypeMatches(skillType, flatSkillType))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.SkillAbilityStaminaCostFlatAdjustment);
            }

            var highResourceSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.HighResourceAbilityStaminaCostSkillType));
            var threshold = Stat.GetStatAdjustment(creature, StatType.HighResourceAbilityStaminaCostThresholdPercent);
            if (threshold > 0 &&
                SkillTypeMatches(skillType, highResourceSkillType) &&
                IsCurrentFPAtOrAbovePercent(creature, threshold))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.HighResourceAbilityStaminaCostAdjustment);
            }

            return adjustment;
        }

        public static void ApplyAbilityStaminaCostFPRestore(uint creature, AbilityDetail ability, int staminaCost)
        {
            if (ability == null)
                return;

            var skillType = GetAbilitySkillType(creature, ability);
            TrackAbilityStaminaCost(creature, ability, staminaCost);
            if (staminaCost <= 0)
                return;

            var restoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStaminaCostFPRestorePercentSkillType));
            var restorePercent = Stat.GetStatAdjustment(creature, StatType.AbilityStaminaCostFPRestorePercent);
            if (restorePercent <= 0 || !SkillTypeMatches(skillType, restoreSkillType))
                return;

            var amount = CalculateResourceRestoreFromCost(staminaCost, restorePercent);
            if (amount <= 0)
                return;

            if (Stat.RestoreFP(creature, amount) > 0)
                ApplyAbilityRestoredFPEffects(creature);
        }

        private static void TrackAbilityStaminaCost(uint creature, AbilityDetail ability, int staminaCost)
        {
            if (!GetIsObjectValid(creature) ||
                ability?.IsHostileAbility != true)
            {
                return;
            }

            var key = (creature, ability);
            if (staminaCost <= 0)
            {
                _abilityStaminaCosts.Remove(key);
                return;
            }

            _abilityStaminaCosts[key] = new AbilityStaminaCostState
            {
                Cost = staminaCost,
                SpentAt = DateTime.UtcNow
            };
        }

        private static bool TryGetAbilityStaminaCostState(
            uint creature,
            AbilityDetail ability,
            out AbilityStaminaCostState state)
        {
            state = null;
            if (ability == null)
                return false;

            var key = (creature, ability);
            if (!_abilityStaminaCosts.TryGetValue(key, out state))
                return false;

            if ((DateTime.UtcNow - state.SpentAt).TotalSeconds <= 35)
                return true;

            _abilityStaminaCosts.Remove(key);
            state = null;
            return false;
        }

        public static void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability)
        {
            if (TryGetAbilityStaminaCostState(creature, ability, out var state))
            {
                state.DeferredImpactCount++;
            }
        }

        public static void CompleteAbilityStaminaCostContext(uint creature, AbilityDetail ability)
        {
            if (ability == null)
                return;

            if (_abilityStaminaCosts.TryGetValue((creature, ability), out var state) &&
                state.DeferredImpactCount > 0)
            {
                return;
            }

            _abilityStaminaCosts.Remove((creature, ability));
        }

        public static void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability)
        {
            if (ability == null ||
                !_abilityStaminaCosts.TryGetValue((creature, ability), out var state))
            {
                return;
            }

            state.DeferredImpactCount = Math.Max(0, state.DeferredImpactCount - 1);
            if (state.DeferredImpactCount == 0)
            {
                _abilityStaminaCosts.Remove((creature, ability));
            }
        }

        public static void ApplyAbilityFPCostStaminaRestore(uint creature, AbilityDetail ability, int fpCost)
        {
            if (fpCost <= 0 || ability == null)
                return;

            var skillType = GetAbilitySkillType(creature, ability);
            var restoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityFPCostStaminaRestorePercentSkillType));
            var restorePercent = Stat.GetStatAdjustment(creature, StatType.AbilityFPCostStaminaRestorePercent);
            if (restorePercent <= 0 || !SkillTypeMatches(skillType, restoreSkillType))
                return;

            var amount = CalculateResourceRestoreFromCost(fpCost, restorePercent);
            if (amount > 0)
                Stat.RestoreStamina(creature, amount);
        }

        private static int CalculateResourceRestoreFromCost(int cost, int percent)
        {
            if (cost <= 0 || percent <= 0)
                return 0;

            return cost * Math.Min(percent, MaximumCrossResourceRestorePercent) / 100;
        }

        public static int GetNextAbilityFPCostAdjustment(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                StatType.NextAbilityFPCostAdjustmentSkillType));

            return storedSkillType == skillType
                ? TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.NextAbilityFPCostAdjustment,
                    StatType.NextAbilityFPCostAdjustmentSkillType)
                : 0;
        }

        public static int ConsumeNextAbilityFPCostAdjustment(uint creature, SkillType skillType)
        {
            var adjustment = GetNextAbilityFPCostAdjustment(creature, skillType);
            if (adjustment == 0)
                return 0;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityFPCostAdjustment,
                StatType.NextAbilityFPCostAdjustmentSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                StatType.NextAbilityFPCostAdjustmentSkillType);

            return adjustment;
        }

        public static int GetAbilityStatusDurationPercentAdjustment(
            uint creature,
            PerkType perkType,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var adjustment = GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityStatusDurationPercentAdjustmentPerkType,
                StatType.AbilityStatusDurationPercentAdjustmentSecondaryPerkType,
                StatType.AbilityStatusDurationPercentAdjustment);
            adjustment += GetIdleStatusDurationPercentAdjustment(
                creature,
                skillType,
                primaryStatusEffect,
                additionalStatusEffects,
                statusEffectFactory);

            return adjustment;
        }

        private static int GetIdleStatusDurationPercentAdjustment(
            uint creature,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.IdleStatusDurationPercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var requiredIdleSeconds = Stat.GetStatAdjustment(creature, StatType.IdleStatusDurationRequiredIdleSeconds);
            if (requiredIdleSeconds <= 0 || HasRecentAttackActivity(creature, requiredIdleSeconds))
                return 0;

            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.IdleStatusDurationRequiredCategory));
            if (requiredCategory != 0 &&
                !StatusContextHasCategory(primaryStatusEffect, additionalStatusEffects, statusEffectFactory, requiredCategory))
            {
                return 0;
            }

            return Stat.GetStatAdjustment(creature, StatType.IdleStatusDurationPercentAdjustment);
        }

        private static bool StatusContextHasCategory(
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            StatusEffectCategory category)
        {
            if (AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, category))
                return true;

            var statusEffect = statusEffectFactory?.Invoke();
            return statusEffect != null && (statusEffect.Categories & category) != 0;
        }

        public static int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender)
        {
            var adjustment = GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentSecondaryPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustment);
            adjustment += GetRangedAttackDefenseIgnorePercentAdjustment(creature, skillType);

            var exposedOrSunderedSkillType = GetSkillTypeFromStat(
                Stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType));
            if (SkillTypeMatches(skillType, exposedOrSunderedSkillType) &&
                GetIsObjectValid(defender) &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(ExposedStatusEffect),
                    typeof(ExposeWeakPointStatusEffect),
                    typeof(SunderStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment);
            }

            if (GetIsObjectValid(defender) &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(ForceDisruptionStatusEffect),
                    typeof(FoggyMindStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(
                    creature,
                    StatType.AbilityDefenseIgnoreForceDisruptionOrFoggyMindPercentAdjustment);
            }

            return adjustment;
        }

        public static float GetAbilityRecastDelayFlatAdjustment(uint creature, PerkType perkType)
        {
            return GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityRecastDelayFlatAdjustmentPerkType,
                StatType.AbilityRecastDelayFlatAdjustmentPerkType,
                StatType.AbilityRecastDelayFlatAdjustment);
        }

        public static float ApplyAbilityRecastDelayModifiers(
            uint activator,
            AbilityDetail ability,
            float recastDelaySeconds)
        {
            if (!GetIsObjectValid(activator) || ability?.IsHostileAbility != true || recastDelaySeconds <= 0f)
                return recastDelaySeconds;

            var adjustment = Stat.GetStatAdjustment(activator, StatType.HostileAbilityRecastDelayPercentAdjustment);
            if (adjustment == 0)
                return recastDelaySeconds;

            return Math.Max(0f, recastDelaySeconds + recastDelaySeconds * (adjustment / 100f));
        }

        private static void ApplyAbilityUsedRecastReduction(uint activator, AbilityDetail ability)
        {
            ApplyAbilityUsedRecastReduction(activator, ability?.RecastGroup ?? RecastGroup.Invalid);
        }

        private static void ApplyAbilityUsedRecastReduction(uint activator, RecastGroup activatedRecastGroup)
        {
            var triggerGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTriggerGroup));
            var secondaryTriggerGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSecondaryTriggerGroup));
            if (activatedRecastGroup == RecastGroup.Invalid ||
                activatedRecastGroup != triggerGroup &&
                activatedRecastGroup != secondaryTriggerGroup)
                return;

            var targetGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTargetGroup));
            var seconds = Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSeconds);
            if (targetGroup == RecastGroup.Invalid || seconds <= 0)
                return;

            Recast.ReduceRecastDelay(activator, targetGroup, seconds);
        }

        private static void ApplyAbilityUsedNextSkillAutoAttackDamageBonus(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType));
            var damageBonus = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonus);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds);
            GrantNextSkillAutoAttackDamageBonus(activator, targetSkillType, damageBonus, duration);
        }

        private static void ApplyAbilityUsedNextSkillFPCostAdjustment(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentSkillType));
            var adjustment = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustmentWindowSeconds);
            GrantNextAbilityFPCostAdjustment(activator, targetSkillType, adjustment, duration);
        }

        private static void ApplyAbilityUsedSkillEvasion(uint activator, AbilityDetail ability)
        {
            ApplyAbilityUsedSkillEvasionChannel(
                activator,
                ability,
                StatType.AbilityUsedEvasionPercentAdjustmentSkillType,
                StatType.AbilityUsedEvasionPercentAdjustment,
                StatType.AbilityUsedEvasionDurationSeconds);
            ApplyAbilityUsedSkillEvasionChannel(
                activator,
                ability,
                StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType,
                StatType.SecondaryAbilityUsedEvasionPercentAdjustment,
                StatType.SecondaryAbilityUsedEvasionDurationSeconds);
        }

        private static void ApplyAbilityUsedSkillEvasionChannel(
            uint activator,
            AbilityDetail ability,
            StatType skillTypeStat,
            StatType evasionStat,
            StatType durationStat)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                skillTypeStat));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var evasionPercent = Stat.GetStatAdjustment(
                activator,
                evasionStat);
            var duration = Stat.GetStatAdjustment(
                activator,
                durationStat);
            if (evasionPercent == 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new EvasiveFootworkStatusEffect(evasionPercent),
                duration);
        }

        private static void ApplyAbilityUsedSkillRangedDeflection(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var deflection = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflection);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionDurationSeconds);
            var statusEffectIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionStatusEffectIcon));
            if (deflection <= 0 || duration <= 0 || statusEffectIcon == EffectIconType.Invalid)
                return;

            if (StatusEffect.ApplyStatusEffect(
                    activator,
                    activator,
                    new RangedDeflectionStatusEffect(deflection, statusEffectIcon),
                    duration))
            {
                var source = Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedRangedDeflection);
                ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
            }
        }

        private static void ApplySingleTargetAbilityUsedAttackDeflection(
            uint activator,
            AbilityDetail ability,
            bool isSingleTargetAbility)
        {
            if (!isSingleTargetAbility)
                return;

            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.SingleTargetAbilityAttackDeflectionSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.SingleTargetAbilityAttackDeflection,
                StatType.SingleTargetAbilityAttackDeflectionDurationSeconds);
        }

        private static void ApplyAbilityUsedSkillAttackDeflection(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedAttackDeflectionSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedAttackDeflection,
                StatType.AbilityUsedAttackDeflectionDurationSeconds,
                StatType.AbilityUsedAttackDeflectionFPRestore);
        }

        private static void ApplyAbilityUsedPerkCategoryAttackDeflection(uint activator, AbilityDetail ability)
        {
            var categoryValue = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType);
            if (!Perk.IsPerkInCategory(ability?.EffectiveLevelPerkType ?? PerkType.Invalid, categoryValue))
            {
                return;
            }

            ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflection,
                StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds,
                StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore);
        }

        private static void TrackCombatAbilityUse(uint activator, AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var skillType = GetAbilitySkillType(activator, ability);
            if (skillType == SkillType.Invalid)
                return;

            var now = DateTime.UtcNow;
            _lastCombatAbilityUse[activator] = now;
            TrackSuppressionAbilityUse(activator, now);
        }

        private static void TrackSuppressionAbilityUse(uint activator, DateTime usedAt)
        {
            var suppressionGroups = StatusEffect.GetCreatureStatusEffects(activator)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Where(effect => GetIsObjectValid(effect.Source))
                .GroupBy(effect => effect.Source);

            foreach (var sourceEffects in suppressionGroups)
            {
                var effects = sourceEffects.ToArray();
                var expiration = DateTime.MaxValue;
                if (effects.All(effect => effect.DurationTicks >= 0))
                {
                    var durationSeconds = effects.Max(effect => effect.DurationTicks * effect.Frequency);
                    if (durationSeconds <= 0f)
                        continue;

                    expiration = usedAt.AddSeconds(durationSeconds);
                }

                _pendingSuppressionAbilityUses[(sourceEffects.Key, activator)] = new SuppressionAbilityUseState
                {
                    Expiration = expiration,
                    SuppressionEffectIds = effects.Select(effect => effect.Id).ToHashSet()
                };
            }
        }

        private static void ApplyHostileAbilitySequenceEffects(
            uint activator,
            FeatType feat,
            AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null || !ability.IsHostileAbility)
                return;

            var windowSeconds = Stat.GetStatAdjustment(activator, StatType.HostileAbilitySequenceWindowSeconds);
            var bleedDuration = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds);
            if (windowSeconds <= 0 || bleedDuration <= 0)
            {
                _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
                {
                    LastFeat = feat,
                    LastUse = DateTime.UtcNow
                };
                return;
            }

            var now = DateTime.UtcNow;
            if (_hostileAbilitySequenceStates.TryGetValue(activator, out var state) &&
                state.LastFeat != feat &&
                (now - state.LastUse).TotalSeconds <= windowSeconds)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.NextDamageDealtBleedDurationSeconds,
                    bleedDuration,
                    windowSeconds,
                    StatType.NextDamageDealtBleedDurationSeconds);
            }

            _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
            {
                LastFeat = feat,
                LastUse = now
            };
        }

        private static void ApplyHostileAbilityResourceRestoreEffects(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var fpRestore = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPRestore);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.HostileAbilityStaminaRestore);

            var restoredFP = fpRestore > 0
                ? Stat.RestoreFP(activator, fpRestore)
                : 0;
            if (restoredFP > 0)
                ApplyAbilityRestoredFPEffects(activator);

            var restoredStamina = staminaRestore > 0
                ? Stat.RestoreStamina(activator, staminaRestore)
                : 0;

            if (restoredFP > 0 && restoredStamina > 0)
                ApplyAbilityRestoredBothResourcesEffects(activator);
        }

        public static void ApplyAbilityRestoredFPEffects(uint activator)
        {
            var haste = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredFPHastePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredFPHasteDurationSeconds);
            if (haste == 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new RestoredFPHasteStatusEffect(haste),
                duration);
        }

        public static void ApplyFPRestoredEffects(uint creature)
        {
            var forceAttack = Stat.GetStatAdjustment(creature, StatType.RestoredFPForceAttackPercentAdjustment);
            var duration = Stat.GetStatAdjustment(creature, StatType.RestoredFPForceAttackDurationSeconds);
            if (forceAttack == 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                creature,
                creature,
                new RestoredFPForceAttackStatusEffect(forceAttack),
                duration);
        }

        public static void ApplyStaminaRestoredEffects(uint creature)
        {
            var attack = Stat.GetStatAdjustment(creature, StatType.RestoredStaminaAttackPercentAdjustment);
            var duration = Stat.GetStatAdjustment(creature, StatType.RestoredStaminaAttackDurationSeconds);
            if (attack == 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                creature,
                creature,
                new RestoredStaminaAttackStatusEffect(attack),
                duration);
        }

        public static void ApplyAbilityRestoredBothResourcesEffects(uint activator)
        {
            var haste = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredBothResourcesHastePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredBothResourcesHasteDurationSeconds);
            if (haste == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackDelayReductionPercent,
                haste,
                duration,
                StatType.AbilityRestoredBothResourcesHastePercentAdjustment);
        }

        public static bool HasRecentCombatAbilityUse(uint activator, float windowSeconds)
        {
            if (!_lastCombatAbilityUse.TryGetValue(activator, out var lastUse))
                return false;

            var isRecent = (DateTime.UtcNow - lastUse).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _lastCombatAbilityUse.Remove(activator);

            return isRecent;
        }

        public static bool IsUsingAbility(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (Activity.GetBusyType(creature) == ActivityStatusType.AbilityActivation ||
                    HasRecentCombatAbilityUse(creature, 2f));
        }

        public static (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) GetIdleSkillAbilityBonuses(uint activator, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return (0, 0, 0);

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilitySkillType));
            var requiredIdleSeconds = Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityRequiredIdleSeconds);
            if (!SkillTypeMatches(skillType, requiredSkillType) || requiredIdleSeconds <= 0)
                return (0, 0, 0);

            if (_lastCombatAbilityUse.TryGetValue(activator, out var lastUse) &&
                (DateTime.UtcNow - lastUse).TotalSeconds < requiredIdleSeconds)
                return (0, 0, 0);

            return (
                Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityDamageBonus),
                Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityHitChancePercentAdjustment),
                Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityCriticalDamagePercentAdjustment));
        }

        private static int GetIdleAbilityHitChanceAdjustment(uint activator, SkillType skillType)
        {
            return GetIdleSkillAbilityBonuses(activator, skillType).HitChancePercentAdjustment;
        }

        private static void ApplyNextAutoAttackDamageBonus(
            uint activator,
            StatType bonusStatType,
            StatType durationStatType)
        {
            var bonus = Stat.GetStatAdjustment(activator, bonusStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (bonus == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.NextAutoAttackDamageBonus,
                bonus,
                duration,
                StatType.NextAutoAttackDamageBonus);
        }

        private static void ApplyAbilityUsedEvasion(
            uint activator,
            StatType evasionStatType,
            StatType durationStatType,
            StatType targetStatType = StatType.EvasionPercentAdjustment)
        {
            var evasionPercent = Stat.GetStatAdjustment(activator, evasionStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (evasionPercent == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                targetStatType,
                evasionPercent,
                duration,
                evasionStatType);
        }

        private static void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType,
            StatType deflectionFPRestoreStatType = StatType.Invalid)
        {
            var attackDeflection = Stat.GetStatAdjustment(activator, attackDeflectionStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (attackDeflection == 0 || duration <= 0)
                return;

            var source = Stat.GetStatTypeDeflectionSource(attackDeflectionStatType);
            var targetStatType = Stat.GetGrantedDeflectionStatType(attackDeflectionStatType);
            if (targetStatType == StatType.Invalid)
                return;

            TemporaryStatModifier.Replace(
                activator,
                targetStatType,
                attackDeflection,
                duration,
                attackDeflectionStatType);
            if (deflectionFPRestoreStatType != StatType.Invalid)
            {
                var fpRestore = Stat.GetStatAdjustment(activator, deflectionFPRestoreStatType);
                var fpRestoreSource = Stat.GetStatTypeDeflectionSource(deflectionFPRestoreStatType);
                if (fpRestore > 0 && fpRestoreSource == source)
                {
                    var fpRestoreTargetStatType = fpRestoreSource switch
                    {
                        DeflectionSource.Melee => StatType.MeleeDeflectionFPRestore,
                        DeflectionSource.Ranged => StatType.DeflectionFPRestore,
                        _ => StatType.Invalid
                    };
                    if (fpRestoreTargetStatType != StatType.Invalid)
                    {
                        TemporaryStatModifier.Replace(
                            activator,
                            fpRestoreTargetStatType,
                            fpRestore,
                            duration,
                            deflectionFPRestoreStatType);
                    }
                }
            }

            if (source == DeflectionSource.Ranged)
                ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
        }

        public static void ApplyAbilityGrantedAttackDeflectionEffects(uint activator, DeflectionSource source)
        {
            if (Stat.GetStatTypeDeflectionSource(StatType.AbilityGrantedAttackDeflectionFPRestore) != source)
                return;

            var fpRestore = Stat.GetStatAdjustment(activator, StatType.AbilityGrantedAttackDeflectionFPRestore);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds);
            if (fpRestore <= 0 || !TryUseStatTrigger(activator, StatType.AbilityGrantedAttackDeflectionFPRestore, cooldown))
                return;

            if (Stat.RestoreFP(activator, fpRestore) > 0)
                ApplyAbilityRestoredFPEffects(activator);
        }

        private static void ApplyThrowingAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var staminaThreshold = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestore);
            if (staminaThreshold > 0 && staminaRestore > 0 && summary.ImpactedTargetCount >= staminaThreshold)
            {
                Stat.RestoreStamina(activator, staminaRestore);
            }

            var attackPerTarget = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentPerTarget);
            var attackDuration = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackDurationSeconds);
            var attackMax = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentMax);
            if (attackPerTarget > 0 && attackDuration > 0 && attackMax > 0)
            {
                ApplyStackingAttackBoost(activator, attackPerTarget, attackMax, attackDuration, summary.ImpactedTargetCount);
            }
        }

        private static void ApplyAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var restoreThreshold = Stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsResourceRestoreThreshold);
            var fpRestore = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFPRestore);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.AreaAbilityStaminaRestore);
            var restoreCooldown = Stat.GetStatAdjustment(activator, StatType.AreaAbilityResourceRestoreCooldownSeconds);
            if (restoreThreshold > 0 &&
                summary.ImpactedTargetCount >= restoreThreshold &&
                TryUseStatTrigger(activator, StatType.AreaAbilityFPRestore, restoreCooldown))
            {
                var restoredFP = fpRestore > 0
                    ? Stat.RestoreFP(activator, fpRestore)
                    : 0;
                if (restoredFP > 0)
                    ApplyAbilityRestoredFPEffects(activator);

                var restoredStamina = staminaRestore > 0
                    ? Stat.RestoreStamina(activator, staminaRestore)
                    : 0;

                if (restoredFP > 0 && restoredStamina > 0)
                    ApplyAbilityRestoredBothResourcesEffects(activator);
            }

            var buffThreshold = Stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsBuffThreshold);
            if (buffThreshold <= 0 || summary.ImpactedTargetCount < buffThreshold)
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.AreaAbilityBuffDurationSeconds);
            if (duration <= 0)
                return;

            var haste = Stat.GetStatAdjustment(activator, StatType.AreaAbilityHastePercentAdjustment);
            if (haste != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    duration,
                    StatType.AttackDelayReductionPercent);
            }

            var deflection = Stat.GetStatAdjustment(activator, StatType.AreaAbilityAttackDeflection);
            if (deflection != 0)
            {
                var source = Stat.GetStatTypeDeflectionSource(StatType.AreaAbilityAttackDeflection);
                var deflectionStatType = Stat.GetGrantedDeflectionStatType(StatType.AreaAbilityAttackDeflection);
                if (deflectionStatType == StatType.Invalid)
                    return;

                TemporaryStatModifier.Replace(
                    activator,
                    deflectionStatType,
                    deflection,
                    duration,
                    StatType.AreaAbilityAttackDeflection);
                ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
            }
        }

        private static void ApplySpearAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var minimumTargets = Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseMinimumTargets);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseStaminaRestore);
            if (minimumTargets > 0 &&
                staminaRestore > 0 &&
                summary.ImpactedTargetCount >= minimumTargets)
            {
                Stat.RestoreStamina(activator, staminaRestore);
            }
        }

        private static void ApplyTwinBladeAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (summary.IsSingleTargetAbility)
            {
                var staminaRestore = Stat.GetStatAdjustment(activator, StatType.TwinBladeSingleTargetAbilityStaminaRestore);
                var cooldown = Stat.GetStatAdjustment(activator, StatType.TwinBladeSingleTargetAbilityStaminaRestoreCooldownSeconds);
                if (staminaRestore > 0 && TryUseStatTrigger(activator, StatType.TwinBladeSingleTargetAbilityStaminaRestore, cooldown))
                {
                    Stat.RestoreStamina(activator, staminaRestore);
                }
            }

            if (!summary.IsAreaAbility)
                return;

            ApplyTwinBladeSweepingAdvance(activator, summary);

            var hasteThreshold = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityMinTargetsHasteThreshold);
            var hastePercent = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityHastePercentAdjustment);
            var hasteDuration = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityHasteDurationSeconds);
            var hasteMax = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityHastePercentMax);
            if (hasteThreshold > 0 &&
                hastePercent > 0 &&
                hasteDuration > 0 &&
                hasteMax > 0 &&
                summary.ImpactedTargetCount >= hasteThreshold)
            {
                var stacksGained = ApplyStackingHasteBoost(activator, hastePercent, hasteMax, hasteDuration, 1);
                var staminaOnStack = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityStaminaRestoreOnHasteStack);
                if (staminaOnStack > 0 && stacksGained > 0)
                {
                    Stat.RestoreStamina(activator, staminaOnStack * stacksGained);
                }
            }

            var staminaPerTarget = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityStaminaRestorePerTarget);
            var staminaMax = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityStaminaRestoreMax);
            if (staminaPerTarget > 0 && staminaMax > 0)
            {
                Stat.RestoreStamina(activator, Math.Min(staminaMax, staminaPerTarget * summary.ImpactedTargetCount));
            }

            var cooldownStaminaPerTarget = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityCooldownStaminaRestorePerTarget);
            var cooldownStaminaMax = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityCooldownStaminaRestoreMax);
            var areaStaminaCooldown = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityCooldownStaminaRestoreCooldownSeconds);
            if (cooldownStaminaPerTarget > 0 &&
                cooldownStaminaMax > 0 &&
                TryUseStatTrigger(activator, StatType.TwinBladeAreaAbilityCooldownStaminaRestorePerTarget, areaStaminaCooldown))
            {
                Stat.RestoreStamina(activator, Math.Min(cooldownStaminaMax, cooldownStaminaPerTarget * summary.ImpactedTargetCount));
            }
        }

        private static void ApplyTwinBladeSweepingAdvance(uint activator, AbilityImpactSummary summary)
        {
            if (Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvance) <= 0)
                return;

            var minimumTargets = Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvanceMinimumTargets);
            if (minimumTargets <= 0 || summary.ImpactedTargetCount < minimumTargets)
                return;

            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvanceStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(activator, staminaRestore);
            }

            var hastePercent = Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvanceHastePercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvanceDurationSeconds);
            if (hastePercent > 0 && duration > 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    duration,
                    StatType.TwinBladeCycloneSweepingAdvanceHastePercent);
            }
        }

        private static int ApplyStackingHasteBoost(
            uint activator,
            int hastePercent,
            int maxHastePercent,
            int durationSeconds,
            int requestedStacks)
        {
            return TemporaryStatModifier.AddCapped(
                activator,
                StatType.AttackDelayReductionPercent,
                hastePercent,
                durationSeconds,
                maxHastePercent,
                StatType.TwinBladeAreaAbilityHastePercentAdjustment,
                requestedStacks);
        }

        private static void ApplyStackingAttackBoost(
            uint activator,
            int attackPercent,
            int maxAttackPercent,
            int durationSeconds,
            int requestedStacks)
        {
            TemporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attackPercent,
                durationSeconds,
                maxAttackPercent,
                StatType.ThrowingAreaAbilityAttackPercentPerTarget,
                requestedStacks);
        }

        private static RecastGroup GetRecastGroupFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(RecastGroup), value)
                ? (RecastGroup)value
                : RecastGroup.Invalid;
        }

        private static SkillType GetSkillTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(SkillType), value)
                ? (SkillType)value
                : SkillType.Invalid;
        }

        private static StatusEffectCategory GetStatusEffectCategoryFromStat(int value)
        {
            return value > 0
                ? (StatusEffectCategory)value
                : 0;
        }

        private static EffectIconType GetEffectIconTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(EffectIconType), value)
                ? (EffectIconType)value
                : EffectIconType.Invalid;
        }

        private static CombatDamageType GetCombatDamageTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(CombatDamageType), value)
                ? (CombatDamageType)value
                : CombatDamageType.Invalid;
        }

        private static bool AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(
            uint creature,
            AbilityDetail ability)
        {
            return AbilityMatchesAnyPerkTypeStat(
                       creature,
                       ability,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType) ||
                   AbilityMatchesAnyPerkTypeStat(
                       creature,
                       ability,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType);
        }

        private static bool AbilityMatchesReversalCutTrigger(uint creature, AbilityDetail ability)
        {
            return AbilityMatchesAnyPerkTypeStat(
                creature,
                ability,
                StatType.TwinBladeDuelistReversalCutTriggerPrimaryPerkType,
                StatType.TwinBladeDuelistReversalCutTriggerSecondaryPerkType,
                StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType);
        }

        private static bool AbilityMatchesAnyPerkTypeStat(
            uint creature,
            AbilityDetail ability,
            params StatType[] statTypes)
        {
            var perkType = ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (perkType == PerkType.Invalid)
                return false;

            foreach (var statType in statTypes)
            {
                if (perkType == GetPerkTypeFromStat(Stat.GetStatAdjustment(creature, statType)))
                    return true;
            }

            return false;
        }

        private static bool AbilityMatchesPerkCategoryStat(
            uint creature,
            AbilityDetail ability,
            StatType categoryStatType)
        {
            var perkType = ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (perkType == PerkType.Invalid)
                return false;

            var categoryValue = Stat.GetStatAdjustment(creature, categoryStatType);
            return categoryValue > 0 && Perk.IsPerkInCategory(perkType, categoryValue);
        }

        private static AbilityType GetAbilityTypeFromStatPlusOne(int value)
        {
            var abilityValue = value - 1;
            return value > 0 && Enum.IsDefined(typeof(AbilityType), abilityValue)
                ? (AbilityType)abilityValue
                : AbilityType.Invalid;
        }

        private static bool IsCurrentFPAtOrAbovePercent(uint creature, int thresholdPercent)
        {
            var maxFP = Stat.GetMaxFP(creature);
            if (maxFP <= 0)
                return false;

            return Stat.GetCurrentFP(creature) >= maxFP * (thresholdPercent / 100f);
        }

        public static bool IsCurrentFPAndStaminaAtOrAbovePercent(uint creature, int thresholdPercent)
        {
            if (thresholdPercent <= 0)
                return false;

            var maxFP = Stat.GetMaxFP(creature);
            var maxStamina = Stat.GetMaxStamina(creature);
            if (maxFP <= 0 || maxStamina <= 0)
                return false;

            return Stat.GetCurrentFP(creature) >= maxFP * (thresholdPercent / 100f) &&
                   Stat.GetCurrentStamina(creature) >= maxStamina * (thresholdPercent / 100f);
        }

        public static bool IsCurrentFPAndStaminaAtOrBelowPercent(uint creature, int thresholdPercent)
        {
            if (thresholdPercent <= 0)
                return false;

            var maxFP = Stat.GetMaxFP(creature);
            var maxStamina = Stat.GetMaxStamina(creature);
            if (maxFP <= 0 || maxStamina <= 0)
                return false;

            return Stat.GetCurrentFP(creature) <= maxFP * (thresholdPercent / 100f) &&
                   Stat.GetCurrentStamina(creature) <= maxStamina * (thresholdPercent / 100f);
        }

        private static bool SkillTypeMatches(SkillType actualSkillType, SkillType requiredSkillType)
        {
            return requiredSkillType != SkillType.Invalid && actualSkillType == requiredSkillType;
        }

        private static bool SkillTypeMatchesOrGlobal(SkillType actualSkillType, SkillType requiredSkillType)
        {
            return requiredSkillType == SkillType.Invalid || SkillTypeMatches(actualSkillType, requiredSkillType);
        }

        private static bool IsWeaponOrForceDamage(SkillType skillType, CombatDamageType damageType)
        {
            return skillType == SkillType.Force ||
                   damageType == CombatDamageType.Force ||
                   IsWeaponSkillType(skillType);
        }

        private static bool IsWeaponOrForceAbility(SkillType skillType)
        {
            return skillType == SkillType.Force || IsWeaponSkillType(skillType);
        }

        public static int GetCombatImpactWeaponDamage(uint activator, SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType))
                return 0;

            var weapon = GetCombatImpactWeapon(activator, skillType);
            return GetIsObjectValid(weapon)
                ? Item.GetDMG(weapon)
                : 0;
        }

        private static uint GetCombatImpactWeapon(uint activator, SkillType skillType)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, activator);
            if (CanItemTriggerWeaponAbility(rightHand, skillType))
                return rightHand;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            if (CanItemTriggerWeaponAbility(leftHand, skillType))
                return leftHand;

            return OBJECT_INVALID;
        }

        public static bool HasEquippedWeaponForAbilitySkill(uint creature, SkillType abilitySkillType)
        {
            if (!GetIsObjectValid(creature) || !IsWeaponSkillType(abilitySkillType))
                return false;

            return CanItemTriggerWeaponAbility(GetItemInSlot(InventorySlot.RightHand, creature), abilitySkillType) ||
                   CanItemTriggerWeaponAbility(GetItemInSlot(InventorySlot.LeftHand, creature), abilitySkillType);
        }

        public static bool CanItemTriggerWeaponAbility(uint item, SkillType abilitySkillType)
        {
            if (!GetIsObjectValid(item))
                return false;

            var weaponSkillType = Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(item));
            return CanWeaponSkillTriggerAbility(weaponSkillType, abilitySkillType);
        }

        public static bool CanWeaponSkillTriggerAbility(SkillType weaponSkillType, SkillType abilitySkillType)
        {
            return !IsWeaponSkillType(abilitySkillType) || weaponSkillType == abilitySkillType;
        }

        public static bool IsWeaponSkillType(SkillType skillType)
        {
            return skillType != SkillType.Invalid &&
                   skillType.GetAttribute<SkillType, SkillAttribute>().CombatPointCategory == CombatPointCategoryType.Weapon;
        }

        private static PerkType GetPerkTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(PerkType), value)
                ? (PerkType)value
                : PerkType.Invalid;
        }

        private static int GetTargetedAbilityAdjustment(
            uint creature,
            PerkType perkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            var adjustment = Perk.GetTargetedStatBonus(
                creature,
                perkType,
                primaryPerkStatType,
                secondaryPerkStatType,
                adjustmentStatType);

            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                adjustment += GetTargetedStatGroupAdjustment(
                    effect.StatGroup,
                    perkType,
                    primaryPerkStatType,
                    secondaryPerkStatType,
                    adjustmentStatType);
            }

            return adjustment;
        }

        private static int GetTargetedStatGroupAdjustment(
            StatGroup statGroup,
            PerkType perkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (statGroup == null)
                return 0;

            var primaryPerk = GetPerkTypeFromStat(statGroup.Stats[primaryPerkStatType]);
            var secondaryPerk = GetPerkTypeFromStat(statGroup.Stats[secondaryPerkStatType]);
            if (perkType != primaryPerk && perkType != secondaryPerk)
                return 0;

            return statGroup.Stats[adjustmentStatType];
        }

        private static void GrantNextAbilityDamageBonus(uint creature, PerkType perkType, int bonus, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || perkType == PerkType.Invalid || bonus == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityDamageBonus,
                bonus,
                durationSeconds,
                GetPerkTypeGroup(perkType));
        }

        private static void GrantNextAbilityStaminaCostAdjustment(
            uint creature,
            PerkType perkType,
            int adjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || perkType == PerkType.Invalid || adjustment == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                adjustment,
                durationSeconds,
                GetPerkTypeGroup(perkType));
        }

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds,
            int defenseIgnorePercentAdjustment = 0)
        {
            if (!GetIsObjectValid(creature) ||
                durationSeconds <= 0 ||
                damageBonus == 0 && criticalRatePercentAdjustment == 0 && defenseIgnorePercentAdjustment == 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextSkillAbilitySkillType);

            if (damageBonus != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityDamageBonus,
                    damageBonus,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }

            if (criticalRatePercentAdjustment != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                    criticalRatePercentAdjustment,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }

            if (defenseIgnorePercentAdjustment != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                    defenseIgnorePercentAdjustment,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }
        }

        private static void GrantStatusAppliedNextAttackDamageBonus(
            uint creature,
            int damageBonus,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || damageBonus == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackStatusAppliedDMGBonus,
                damageBonus,
                durationSeconds,
                StatType.NextAttackStatusAppliedDMGBonus);
        }

        public static void GrantNextSkillAbilityStaminaCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                adjustment == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustment,
                adjustment,
                durationSeconds,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);
        }

        public static void ApplyBleedingStatusExpiredEffects(uint source)
        {
            if (!GetIsObjectValid(source))
                return;

            var adjustment = Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment);
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilitySkillType));
            var windowSeconds = Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilityWindowSeconds);

            GrantNextSkillAbilityStaminaCostAdjustment(source, skillType, adjustment, windowSeconds);
        }

        private static void GrantNextSkillAutoAttackDamageBonus(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                damageBonus == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAutoAttackDamageBonus,
                damageBonus,
                durationSeconds,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
        }

        private static void GrantNextAbilityFPCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                adjustment == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAbilityFPCostAdjustmentSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityFPCostAdjustment,
                adjustment,
                durationSeconds,
                StatType.NextAbilityFPCostAdjustmentSkillType);
        }

        private static string GetPerkTypeGroup(PerkType perkType)
        {
            return $"{nameof(PerkType)}:{(int)perkType}";
        }

        /// <summary>
        /// Return a damage bonus equal to 0.15 of the player's relevant skill or an NPC's level.
        /// This helps abilities as the player progresses.
        ///
        /// Global scaling on gear is closer to 0.25 DMG per player skill level so low tier abilities will still
        /// become less useful over time, and get replaced by higher tier ones.  But they will have some utility still.
        /// </summary>
        /// <returns> 0.15 * the player's rank in the specified skill, or the level for NPCs.</returns>

        public static int GetAbilityDamageBonus(uint creature, SkillType skill)
        {
            var level = 0;
            if (!GetIsPC(creature))
            {
                var npcStats = Stat.GetNPCStats(creature);
                level = npcStats.Level;
            }
            else
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                var pcSkill = dbPlayer.Skills[skill];
                level = pcSkill.Rank;
            }


            return (int)(0.15f * level);
        }

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public static void ClearCombatState()
        {
            uint player = OBJECT_SELF;

            // Clear combat state.
            if (!GetIsInCombat(player))
            {
                DeleteLocalFloat(player, "ATTACK_ORIENTATION_X");
                DeleteLocalFloat(player, "ATTACK_ORIENTATION_Y");
            }
        }

        /// <summary>
        /// Builds a combat log message based on the provided information.
        /// </summary>
        /// <param name="attacker">The id of the attacker</param>
        /// <param name="defender">The id of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        public static string BuildCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            int attackResultType,
            int chanceToHit)
        {
            var type = string.Empty;

            switch (attackResultType)
            {
                case 1:
                case 7:
                    type = ": *hit*";
                    break;
                case 3:
                    type = ": *critical*";
                    break;
                case 4:
                    type = ": *miss*";
                    break;
            }

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        public static string BuildAbilityCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            string abilityName,
            int attackResultType,
            int chanceToHit)
        {
            var type = string.Empty;

            switch (attackResultType)
            {
                case 1:
                case 7:
                    type = ": *hit*";
                    break;
                case 3:
                    type = ": *critical*";
                    break;
                case 4:
                    type = ": *miss*";
                    break;
            }

            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "an ability";

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            return ColorToken.Combat($"{attackerName} uses {abilityName} on {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        public static string BuildAbilityNoTargetCombatLogMessage(
            uint observer,
            uint attacker,
            string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "an ability";

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);

            return ColorToken.Combat($"{attackerName} uses {abilityName}, but it hits no targets.");
        }

        public static void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return;

            Messaging.SendMessageNearbyToPlayers(
                defender,
                observer => BuildAbilityCriticalHitCombatLogMessage(
                    observer,
                    attacker,
                    defender,
                    abilityName),
                60f);
        }

        public static string BuildAbilityCriticalHitCombatLogMessage(
            uint observer,
            uint attacker,
            uint defender,
            string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "Ability";

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);
            return ColorToken.Combat($"{attackerName}'s {abilityName} critically hits {defenderName}.");
        }

        public static void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage)
        {
            if (damage <= 0 ||
                !GetIsObjectValid(defender) ||
                !HasTemporaryHitPoints(defender))
            {
                return;
            }

            Messaging.SendMessageNearbyToPlayers(
                defender,
                receiver => BuildTemporaryHitPointDamageCombatLogMessage(receiver, attacker, defender, damage),
                60f);
        }

        private static bool HasTemporaryHitPoints(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints)
                    return true;
            }

            return false;
        }

        private static string BuildTemporaryHitPointDamageCombatLogMessage(uint observer, uint attacker, uint defender, int damage)
        {
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender);

            if (!GetIsObjectValid(attacker) || attacker == defender)
                return ColorToken.Combat($"{defenderName}'s temporary HP absorbs {damage} damage.");

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker);
            return ColorToken.Combat($"{attackerName} deals {damage} damage against {defenderName}'s temporary HP.");
        }

        /// <summary>
        /// Builds a combat log message based on the provided information, for native contexts.
        /// </summary>
        /// <param name="attacker">The CNWSCreature of the attacker</param>
        /// <param name="defender">The CNWSCreature of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        public static string BuildCombatLogMessageNative(
            uint observer,
            CNWSCreature attacker,
            CNWSCreature defender,
            int attackResultType,
            int chanceToHit,
            DeflectionSource deflectionSource = DeflectionSource.None)
        {
            var type = string.Empty;

            switch (attackResultType)
            {
                case 1:
                case 7:
                    type = ": *hit*";
                    break;
                case 3:
                    type = ": *critical*";
                    break;
                case 4:
                    type = ": *miss*";
                    break;
                case 2:
                    type = $": *{GetDeflectionResultName(deflectionSource)}*";
                    break;
            }

            var attackerName = PlayerName.GetColoredDisplayName(observer, attacker.m_idSelf);
            var defenderName = PlayerName.GetColoredDisplayName(observer, defender.m_idSelf);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        internal static string GetDeflectionResultName(DeflectionSource source)
        {
            return source switch
            {
                DeflectionSource.Melee => "melee deflect",
                DeflectionSource.Ranged => "ranged deflect",
                DeflectionSource.Shield => "shield deflect",
                _ => "deflect"
            };
        }

        public static int GetPerkAdjustedAbilityScore(uint attacker)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, attacker);
            if (!GetIsObjectValid(weapon)) return 0;
            var weaponType = GetBaseItemType(weapon);

            return GetAbilityScore(attacker, GetWeaponDamageAbilityType(attacker, weaponType));
        }

        public static AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType)
        {
            var overrideAbility = GetAbilityOverride(
                creature,
                weaponType,
                Item.StaffBaseItemTypes,
                StatType.StaffDamageAbilityOverride);
            if (overrideAbility != AbilityType.Invalid)
                return overrideAbility;

            return Item.GetWeaponDamageAbilityType(weaponType);
        }

        public static AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType)
        {
            var overrideAbility = GetAbilityOverride(
                creature,
                weaponType,
                Item.StaffBaseItemTypes,
                StatType.StaffAccuracyAbilityOverride);
            if (overrideAbility != AbilityType.Invalid)
                return overrideAbility;

            return Item.GetWeaponAccuracyAbilityType(weaponType);
        }

        public static int GetMiscDMGBonus(uint attacker, BaseItem weaponType)
        {
            var bonus = GetPowerAttackDMGBonus(attacker);
            var weaponMightMultiplier = Stat.GetStatAdjustment(attacker, StatType.WeaponMightModifierDamageMultiplier);
            bonus += Math.Max(0, GetAbilityModifier(AbilityType.Might, attacker)) * weaponMightMultiplier;

            if (Item.StaffBaseItemTypes.Contains(weaponType))
            {
                var mightMultiplier = Stat.GetStatAdjustment(attacker, StatType.StaffMightModifierDamageMultiplier);
                bonus += Math.Max(0, GetAbilityModifier(AbilityType.Might, attacker)) * mightMultiplier;
            }

            return bonus;
        }

        private static AbilityType GetAbilityOverride(
            uint creature,
            BaseItem weaponType,
            IReadOnlyCollection<BaseItem> weaponTypes,
            StatType statType)
        {
            if (!weaponTypes.Contains(weaponType))
                return AbilityType.Invalid;

            var value = Stat.GetStatAdjustment(creature, statType);
            if (value <= 0 || value > (int)AbilityType.Social + 1)
                return AbilityType.Invalid;

            return (AbilityType)(value - 1);
        }

        /// <summary>
        /// Retrieves the DMG bonus granted by Power Attack.
        /// </summary>
        /// <param name="attacker">The attacker to check.</param>
        /// <returns>The DMG bonus, or 0 if Power Attack is not enabled.</returns>
        public static int GetPowerAttackDMGBonus(uint attacker)
        {
            if (GetActionMode(attacker, ActionMode.PowerAttack))
                return 3;
            else if (GetActionMode(attacker, ActionMode.ImprovedPowerAttack))
                return 6;
            return 0;
        }

        /// <summary>
        /// Calculates the attack delay for a creature based on equipped weapon delay item properties.
        /// </summary>
        /// <param name="attacker">The creature to calculate delay for.</param>
        /// <returns>Attack delay in milliseconds.</returns>
        public static int CalculateAttackDelay(uint attacker)
        {
            return CalculateAttackDelay(attacker, 0);
        }

        /// <summary>
        /// Calculates attack delay while adjusting the creature's raw attack-delay reduction.
        /// A negative adjustment is used to compare a swing with and without a limited-attack
        /// speed effect without mutating the creature's active status effects.
        /// </summary>
        public static int CalculateAttackDelay(uint attacker, int attackDelayReductionAdjustment)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, attacker);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, attacker);

            var rightHandDelay = GetWeaponDelay(rightHand);
            var leftHandDelay = ApplyOffhandAttackDelayReduction(attacker, GetWeaponDelay(leftHand));

            var delay = CalculateEquippedWeaponDelayUnits(rightHandDelay, leftHandDelay);
            if (delay == 0)
            {
                var creatureRight = GetItemInSlot(InventorySlot.CreatureRight, attacker);
                var creatureLeft = GetItemInSlot(InventorySlot.CreatureLeft, attacker);
                var creatureBite = GetItemInSlot(InventorySlot.CreatureBite, attacker);

                var creatureDelays = new[]
                {
                    GetWeaponDelay(creatureRight),
                    GetWeaponDelay(creatureLeft),
                    GetWeaponDelay(creatureBite)
                };

                delay = creatureDelays
                    .Where(creatureDelay => creatureDelay > 0)
                    .DefaultIfEmpty(0)
                    .Min();
            }

            var finalDelay = ConvertAttackDelayUnitsToMilliseconds(delay);
            var reductionPercentage = Math.Clamp(
                Stat.GetStatAdjustment(attacker, StatType.AttackDelayReductionPercent) +
                attackDelayReductionAdjustment,
                -MaximumAttackDelayAdjustmentPercent,
                MaximumAttackDelayAdjustmentPercent);

            return ApplyAttackDelayReduction(finalDelay, reductionPercentage);
        }

        /// <summary>
        /// Calculates raw attack delay milliseconds from weapon delay units.
        /// </summary>
        /// <param name="rightHandDelayUnits">Main-hand delay in custom delay units.</param>
        /// <param name="leftHandDelayUnits">Offhand delay in custom delay units.</param>
        /// <param name="attackDelayReductionPercent">Overall attack delay reduction percentage.</param>
        /// <param name="offhandAttackDelayReductionPercent">Offhand attack delay reduction percentage.</param>
        /// <returns>Raw attack delay in milliseconds before engine default delay adjustment.</returns>
        public static int CalculateAttackDelayMilliseconds(
            int rightHandDelayUnits,
            int leftHandDelayUnits,
            int attackDelayReductionPercent,
            int offhandAttackDelayReductionPercent)
        {
            attackDelayReductionPercent = Math.Min(attackDelayReductionPercent, MaximumAttackDelayAdjustmentPercent);
            offhandAttackDelayReductionPercent = Math.Min(
                Math.Max(offhandAttackDelayReductionPercent, 0),
                MaximumAttackDelayAdjustmentPercent);
            leftHandDelayUnits = ApplyPercentReduction(leftHandDelayUnits, offhandAttackDelayReductionPercent);

            var delayUnits = CalculateEquippedWeaponDelayUnits(rightHandDelayUnits, leftHandDelayUnits);
            var delayMilliseconds = ConvertAttackDelayUnitsToMilliseconds(delayUnits);

            return ApplyAttackDelayReduction(delayMilliseconds, attackDelayReductionPercent);
        }

        /// <summary>
        /// Calculates the attack delay used by the attack loop after accounting for the engine's default delay.
        /// </summary>
        /// <param name="attackerDelayMilliseconds">The attacker's calculated delay in milliseconds.</param>
        /// <returns>The adjusted delay in milliseconds.</returns>
        public static int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds)
        {
            return CalculateEffectiveAttackDelay(attackerDelayMilliseconds, false);
        }

        /// <summary>
        /// Calculates the attack delay used by the attack loop after accounting for the engine's default delay.
        /// Delays below <see cref="BaseAttackDelayMilliseconds"/> are honored through multi-attack swings and
        /// are clamped to <see cref="MinimumAttackDelayMilliseconds"/>.
        /// </summary>
        /// <param name="attackerDelayMilliseconds">The attacker's calculated delay in milliseconds.</param>
        /// <param name="useDefaultMinimumDelay">If true, ignore weapon delay and use the engine's fastest possible swing floor.</param>
        /// <returns>The adjusted delay in milliseconds.</returns>
        public static int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds, bool useDefaultMinimumDelay)
        {
            if (useDefaultMinimumDelay)
                return MinimumAttackDelayMilliseconds;

            if (attackerDelayMilliseconds <= BaseAttackDelayMilliseconds)
                return BaseAttackDelayMilliseconds;

            var effectiveDelay = attackerDelayMilliseconds - BaseAttackDelayMilliseconds;
            return Math.Max(MinimumAttackDelayMilliseconds, effectiveDelay);
        }

        /// <summary>
        /// Calculates the wall-clock interval between swing animations for a given effective attack delay.
        /// The engine cannot animate swings faster than <see cref="BaseAttackDelayMilliseconds"/>, so faster
        /// delays keep the swing cadence at that floor and resolve additional attacks per swing instead.
        /// </summary>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <returns>The interval between swings in milliseconds.</returns>
        public static int CalculateAttackSwingDelay(int effectiveDelayMilliseconds)
        {
            return Math.Max(BaseAttackDelayMilliseconds, effectiveDelayMilliseconds);
        }

        /// <summary>
        /// Calculates how many attacks a swing should resolve for a given effective attack delay,
        /// carrying fractional attacks between swings so the long-run attack rate matches the delay.
        /// </summary>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <param name="attackDebt">Fractional attacks owed from previous swings.</param>
        /// <param name="updatedAttackDebt">Fractional attacks still owed after this swing.</param>
        /// <returns>The number of attacks to resolve in this swing.</returns>
        public static int CalculateAttacksPerSwing(
            int effectiveDelayMilliseconds,
            float attackDebt,
            out float updatedAttackDebt)
        {
            if (effectiveDelayMilliseconds <= 0)
            {
                updatedAttackDebt = 0f;
                return 1;
            }

            var swingDelay = CalculateAttackSwingDelay(effectiveDelayMilliseconds);
            var attacksOwed = attackDebt + swingDelay / (float)effectiveDelayMilliseconds;
            var attacks = Math.Clamp((int)attacksOwed, 1, MaxAttacksPerSwing);
            updatedAttackDebt = Math.Clamp(attacksOwed - attacks, 0f, MaxAttacksPerSwing);

            return attacks;
        }

        /// <summary>
        /// Determines how many attacks the attacker's next swing should resolve and updates the
        /// attacker's carried fractional attack debt.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <returns>The number of attacks to resolve in this swing.</returns>
        public static int ConsumeAttacksPerSwing(uint attacker, int effectiveDelayMilliseconds)
        {
            return ConsumeAttacksPerSwing(attacker, effectiveDelayMilliseconds, effectiveDelayMilliseconds, false);
        }

        /// <summary>
        /// Determines how many attacks the attacker's next swing should resolve and updates the
        /// attacker's carried fractional attack debt.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <param name="unbuffedDelayMilliseconds">
        /// The effective delay the attacker would have without a no-delay buff, used to size the
        /// guarantee below.
        /// </param>
        /// <param name="hasNoDelayBuff">
        /// Whether a no-delay buff was consumed for this swing. When set, the buff must be worth at
        /// least one extra attack. Without that guarantee a no-delay buff only lowers the delay to
        /// <see cref="MinimumAttackDelayMilliseconds"/>, which does nothing at all for a build
        /// already sitting at that floor (heavily hasted or dual-wielding). This must be passed
        /// explicitly rather than inferred from the two delays differing: a build already at the
        /// floor supplies equal values, which is exactly the case the guarantee exists to fix.
        /// </param>
        /// <returns>The number of attacks to resolve in this swing.</returns>
        public static int ConsumeAttacksPerSwing(
            uint attacker,
            int effectiveDelayMilliseconds,
            int unbuffedDelayMilliseconds,
            bool hasNoDelayBuff)
        {
            return ConsumeAttacksPerSwing(
                attacker,
                effectiveDelayMilliseconds,
                unbuffedDelayMilliseconds,
                hasNoDelayBuff,
                effectiveDelayMilliseconds,
                0);
        }

        /// <summary>
        /// Determines the attacks resolved by a swing while preventing a limited-attack speed
        /// effect from pre-scheduling more accelerated attacks than its remaining charges cover.
        /// </summary>
        public static int ConsumeAttacksPerSwing(
            uint attacker,
            int effectiveDelayMilliseconds,
            int unbuffedDelayMilliseconds,
            bool hasNoDelayBuff,
            int effectiveDelayWithoutLimitedReductionMilliseconds,
            int limitedReductionRemainingAttacks,
            int limitedNoDelayRemainingAttacks = 0)
        {
            var limitedSpeedRemainingAttacks = Math.Max(
                limitedReductionRemainingAttacks,
                limitedNoDelayRemainingAttacks);
            _attackSwingDebts.TryGetValue(attacker, out var attackDebt);
            var hasTrackedBaselineAttackDebt = _attackSwingDebtsWithoutLimitedReduction.TryGetValue(
                attacker,
                out var trackedBaselineAttackDebt);
            if (limitedSpeedRemainingAttacks <= 0 && hasTrackedBaselineAttackDebt)
            {
                // Suppression or an externally removed limited-speed effect must not calculate the
                // current swing from debt created by an acceleration that no longer applies.
                attackDebt = trackedBaselineAttackDebt;
            }

            var attacks = CalculateAttacksPerSwing(effectiveDelayMilliseconds, attackDebt, out var updatedAttackDebt);

            if (hasNoDelayBuff)
            {
                var unbuffedAttacks = CalculateAttacksPerSwing(unbuffedDelayMilliseconds, attackDebt, out _);
                var guaranteedAttacks = Math.Clamp(unbuffedAttacks + 1, 1, MaxAttacksPerSwing);
                if (guaranteedAttacks > attacks)
                {
                    // The extra attack is granted outright rather than drawn from carried debt, so
                    // remove it from the debt the swing would otherwise bank for later swings.
                    updatedAttackDebt = Math.Max(0f, updatedAttackDebt - (guaranteedAttacks - attacks));
                    attacks = guaranteedAttacks;
                }
            }

            if (limitedSpeedRemainingAttacks > 0)
            {
                var baselineAttackDebt = hasTrackedBaselineAttackDebt
                    ? trackedBaselineAttackDebt
                    : attackDebt;
                var baselineAttacks = CalculateAttacksPerSwing(
                    effectiveDelayWithoutLimitedReductionMilliseconds,
                    baselineAttackDebt,
                    out var baselineUpdatedAttackDebt);
                attacks = CapAttacksPerSwingForLimitedAttackEffect(
                    attacks,
                    baselineAttacks,
                    limitedReductionRemainingAttacks,
                    limitedNoDelayRemainingAttacks);

                // Every matching roll in the swing consumes one charge, including rolls the
                // baseline cadence would have scheduled. Once all remaining charges will be spent,
                // discard fractional debt created by the expiring reduction while retaining debt
                // earned without it.
                if (attacks >= limitedSpeedRemainingAttacks)
                {
                    updatedAttackDebt = baselineUpdatedAttackDebt;
                    _attackSwingDebtsWithoutLimitedReduction.Remove(attacker);
                }
                else
                {
                    // Preserve zero as an explicit tracked value. Falling back to the accelerated
                    // ledger on the next swing would re-contaminate this baseline.
                    _attackSwingDebtsWithoutLimitedReduction[attacker] = baselineUpdatedAttackDebt;
                }
            }
            else
            {
                _attackSwingDebtsWithoutLimitedReduction.Remove(attacker);
            }

            if (updatedAttackDebt <= 0f)
                _attackSwingDebts.Remove(attacker);
            else
                _attackSwingDebts[attacker] = updatedAttackDebt;

            return attacks;
        }

        public static int CapAttacksPerSwingForLimitedAttackEffect(
            int acceleratedAttacks,
            int baselineAttacks,
            int remainingAttacks,
            int limitedNoDelayRemainingAttacks = 0)
        {
            acceleratedAttacks = Math.Max(1, acceleratedAttacks);
            baselineAttacks = Math.Clamp(baselineAttacks, 1, acceleratedAttacks);
            remainingAttacks = Math.Max(0, remainingAttacks);
            limitedNoDelayRemainingAttacks = Math.Max(0, limitedNoDelayRemainingAttacks);
            if (remainingAttacks <= 0 && limitedNoDelayRemainingAttacks <= 0)
                return baselineAttacks;

            // Baseline rolls still happen after the limited effect expires, but they must not
            // create extra charged rolls. Only the portion covered by remaining charges may use
            // the accelerated schedule.
            var cappedAttacks = Math.Max(baselineAttacks, remainingAttacks);
            if (limitedNoDelayRemainingAttacks > 0)
            {
                // A no-delay charge guarantees one extra roll even at the swing-delay floor. Keep
                // that single promised roll while still preventing the rest of the accelerated
                // schedule from escaping its remaining charges.
                cappedAttacks = Math.Max(
                    cappedAttacks,
                    Math.Max(baselineAttacks + 1, limitedNoDelayRemainingAttacks));
            }

            return Math.Min(acceleratedAttacks, cappedAttacks);
        }

        /// <summary>
        /// Clears any carried fractional attack debt for a creature. Used when combat ends,
        /// the creature becomes unable to attack, or its equipped weapons change.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        public static void ClearAttackSwingDebt(uint attacker)
        {
            _attackSwingDebts.Remove(attacker);
            _attackSwingDebtsWithoutLimitedReduction.Remove(attacker);
        }

        private static int ApplyOffhandAttackDelayReduction(uint attacker, int offhandDelay)
        {
            if (offhandDelay <= 0)
                return offhandDelay;

            var reductionPercentage = CalculateOffhandAttackDelayReduction(attacker);
            return ApplyPercentReduction(offhandDelay, reductionPercentage);
        }

        private static int CalculateEquippedWeaponDelayUnits(int rightHandDelay, int leftHandDelay)
        {
            rightHandDelay = Math.Max(0, rightHandDelay);
            leftHandDelay = Math.Max(0, leftHandDelay);

            var hasRightHandDelay = rightHandDelay > 0;
            var hasLeftHandDelay = leftHandDelay > 0;
            if (!hasRightHandDelay || !hasLeftHandDelay)
                return rightHandDelay + leftHandDelay;

            // Each equipped weapon delay includes the engine's default attack cadence.
            // The custom delay gate only needs to pay that baseline once for the pair.
            return BaseAttackDelayUnits +
                   Math.Max(0, rightHandDelay - BaseAttackDelayUnits) +
                   Math.Max(0, leftHandDelay - BaseAttackDelayUnits);
        }

        private static int ConvertAttackDelayUnitsToMilliseconds(int delayUnits)
        {
            return (int)(delayUnits / (float)AttackDelayUnitsPerSecond * MillisecondsPerSecond);
        }

        private static int ApplyAttackDelayReduction(int delayMilliseconds, int reductionPercentage)
        {
            if (delayMilliseconds <= 0 || reductionPercentage == 0)
                return delayMilliseconds;

            if (reductionPercentage > 0)
                return ApplyPercentReduction(delayMilliseconds, reductionPercentage);

            var increaseAmount = (int)(delayMilliseconds * (Math.Abs(reductionPercentage) / 100f));
            return delayMilliseconds + increaseAmount;
        }

        private static int ApplyPercentReduction(int value, int reductionPercentage)
        {
            if (value <= 0 || reductionPercentage <= 0)
                return value;

            var reductionAmount = (int)(value * (reductionPercentage / 100f));
            return Math.Max(0, value - reductionAmount);
        }

        /// <summary>
        /// Handles paralyze status effects for a creature before resolving a delayed attack.
        /// </summary>
        /// <param name="attacker">The creature to check for paralyze.</param>
        /// <returns>True if the creature is paralyzed and cannot act.</returns>
        public static bool HandleParalyze(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return false;

            for (var effect = GetFirstEffect(attacker); GetIsEffectValid(effect); effect = GetNextEffect(attacker))
            {
                if (GetEffectType(effect) != EffectTypeScript.Paralyze)
                    continue;

                Messaging.SendMessageNearbyToPlayers(
                    attacker,
                    receiver => $"{PlayerName.GetDisplayName(receiver, attacker)} is paralyzed and cannot act!");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the attack delay reduction percentage based on stat adjustments and active speed effects.
        /// Cumulative reductions are capped at 50%.
        /// </summary>
        /// <param name="attacker">The creature to calculate delay reduction for.</param>
        /// <returns>Attack delay reduction percentage.</returns>
        public static int CalculateAttackDelayReduction(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalReduction = Stat.GetStatAdjustment(attacker, StatType.AttackDelayReductionPercent);

            return Math.Clamp(
                totalReduction,
                -MaximumAttackDelayAdjustmentPercent,
                MaximumAttackDelayAdjustmentPercent);
        }

        public static int CalculateOffhandAttackDelayReduction(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalReduction = Stat.GetStatAdjustment(attacker, StatType.OffhandAttackDelayReductionPercent);

            return Math.Min(
                Math.Max(totalReduction, 0),
                MaximumAttackDelayAdjustmentPercent);
        }

        private static int GetWeaponDelay(uint item)
        {
            if (!GetIsObjectValid(item))
                return 0;

            var delay = 0;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.Delay)
                {
                    delay += GetItemPropertyCostTableValue(ip) * 10;
                }
            }

            return delay;
        }

        private sealed class RepeatedTargetDamageState
        {
            public uint Target { get; }
            public int Stacks { get; set; }
            public DateTime LastHit { get; set; }

            public RepeatedTargetDamageState(uint target)
            {
                Target = target;
                LastHit = DateTime.UtcNow;
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static class Combat
    {
        public const int StandardCriticalRating = 2;

        private static readonly List<CombatDamageType> _allValidDamageTypes = new();
        private static readonly List<CombatDamageType> _allDefenseDamageTypes = new();
        private static readonly Dictionary<(uint, StatType), DateTime> _statTriggerCooldowns = new();
        private static readonly Dictionary<(uint, uint), DateTime> _recentDamageTargets = new();
        private static bool _damageTypesCached;

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
        /// <param name="defenderStat">The defender's defend stat value</param>
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

            var statDelta = attackerStat - defenderStat;
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

            return ((int)minDamage, (int)maxDamage);
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
            const int BaseHitRate = 75;

            var hitRate = BaseHitRate + (int)Math.Floor((attackerAccuracy - defenderEvasion) / 2.0f) + percentageModifier;

            if (hitRate < 20)
                hitRate = 20;
            else if (hitRate > 95)
                hitRate = 95;

            return hitRate;
        }

        /// <summary>
        /// Calculates the critical hit rate against a given target.
        /// </summary>
        /// <param name="attackerPER">The attacker's perception stat</param>
        /// <param name="defenderMGT">The defender's might stat.</param>
        /// <param name="criticalModifier">A modifier to the critical rating based on external factors.</param>
        /// <returns>The critical rate, in a percentage</returns>
        public static int CalculateCriticalRate(int attackerPER, int defenderMGT, int criticalModifier)
        {
            const int BaseCriticalRate = 5;
            var delta = attackerPER - defenderMGT;

            if (delta < 0)
                delta = 0;
            else if (delta > 15)
                delta = 15;

            var criticalRate = BaseCriticalRate + delta + criticalModifier;
            if (criticalRate < BaseCriticalRate)
                criticalRate = BaseCriticalRate;
            else if (criticalRate > 90)
                criticalRate = 90;


            return criticalRate;
        }

        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's defend stat value</param>
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

        public static int ApplyDamageTakenModifiers(uint defender, int damage)
        {
            if (damage <= 0)
                return damage;

            var percentAdjustment = Stat.GetStatAdjustment(defender, StatType.DamageTakenPercentAdjustment);
            if (percentAdjustment <= -100)
                return 0;

            if (percentAdjustment != 0)
                damage += (int)Math.Ceiling(damage * (percentAdjustment / 100f));

            damage += Stat.GetStatAdjustment(defender, StatType.DamageTakenFlatAdjustment);

            return Math.Max(0, damage);
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
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            bool isAbilityDamage = false,
            bool canApplyRandomFlatBonuses = true)
        {
            if (damage <= 0)
                return damage;

            damage = ApplyTargetLowHPDamageModifier(attacker, defender, damage);
            damage = ApplyTargetStatusDamageModifiers(
                attacker,
                defender,
                damage,
                skillType,
                damageType,
                isAbilityDamage,
                canApplyRandomFlatBonuses);

            return Math.Max(0, damage);
        }

        public static int ApplyAutoAttackDamageModifiers(uint attacker, uint defender, int damage)
        {
            if (damage <= 0)
                return damage;

            var chance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackDamageBonusChance);
            var bonus = Stat.GetStatAdjustment(attacker, StatType.AutoAttackDamageBonus);

            if (chance > 0 && bonus != 0 && Random.D100(1) <= chance)
                damage += bonus;

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

            return damage;
        }

        public static void ApplyDamageDealtEffects(uint attacker, uint defender, int damage)
        {
            if (damage <= 0)
                return;

            TrackRecentDamageTarget(attacker, defender);

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                HealFromDamage(attacker, damage, hpRestorePercent);
            }
        }

        public static void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid)
        {
            if (criticalRating <= 0 || damage <= 0)
                return;

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
            ApplyCriticalSideAttackStaminaRestore(attacker, defender);

            var poisonedTargetStaminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalPoisonedTargetStaminaRestore);
            if (poisonedTargetStaminaRestore > 0 && StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                Stat.RestoreStamina(attacker, poisonedTargetStaminaRestore);
            }

            var targetFPLossPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetFPLossPercentOfDamage);
            if (targetFPLossPercent > 0)
            {
                var fpLoss = Math.Max(1, (int)Math.Ceiling(damage * (targetFPLossPercent / 100f)));
                Stat.ReduceFP(defender, fpLoss);
            }

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestore);
            if (hpRestorePercent > 0)
            {
                HealFromDamage(attacker, damage, hpRestorePercent);
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

        private static bool IsAttackerBesideTarget(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetArea(attacker) != GetArea(defender))
                return false;

            var defenderPosition = GetPosition(defender);
            var attackerPosition = GetPosition(attacker);
            var deltaX = attackerPosition.X - defenderPosition.X;
            var deltaY = attackerPosition.Y - defenderPosition.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (distance <= 0.001)
                return false;

            var facingRadians = GetFacing(defender) * Math.PI / 180.0;
            var forwardX = Math.Cos(facingRadians);
            var forwardY = Math.Sin(facingRadians);
            var dot = Math.Clamp((forwardX * deltaX + forwardY * deltaY) / distance, -1.0, 1.0);
            var angleDegrees = Math.Acos(dot) * 180.0 / Math.PI;

            return angleDegrees >= 45.0 && angleDegrees <= 135.0;
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

            ApplyLowHPPhysicalDefenseEffect(defender, damage);
            ApplyLowHPEvasionEffect(defender, damage);
            ApplyLowHPNextAbilityNoStaminaCostEffect(defender, damage);
            ApplyLowHPTemporaryHPEffect(defender, damage);
            ApplyLowHPNoSaveTemporaryHPEffect(defender, damage);
            ApplyRecentDamageTargetHitEffects(defender, attacker);
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ApplyDefeatedEnemyEffects()
        {
            var defeated = OBJECT_SELF;
            var killer = GetLastKiller();

            if (GetIsObjectValid(killer) && killer != defeated)
            {
                ApplyDefeatedEnemyEffects(killer);
            }

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

        public static void ApplyDefeatedEnemyEffects(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
                return;

            var staminaRestore = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(creature, staminaRestore);
            }

            var hpRestorePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                HealPercentOfMaxHP(creature, hpRestorePercent);
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

            var recastReductionGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyRecastReductionGroup));
            var recastReductionSeconds = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyRecastReductionSeconds);
            if (recastReductionGroup != RecastGroup.Invalid && recastReductionSeconds > 0)
            {
                Recast.ReduceRecastDelay(creature, recastReductionGroup, recastReductionSeconds);
            }
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

        private static void HealFromDamage(uint creature, int damage, int percent)
        {
            if (damage <= 0 || percent <= 0)
                return;

            var amount = Math.Max(1, (int)Math.Ceiling(damage * (percent / 100f)));
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
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

            TemporaryStatModifier.Replace(
                defender,
                StatType.PhysicalDefensePercentAdjustment,
                defensePercent,
                duration,
                StatType.LowHPPhysicalDefensePercentAdjustment);
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

            var temporaryHP = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(defender) * (temporaryHPPercent / 100f)));
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(temporaryHP), defender, duration);
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

            var temporaryHP = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(defender) * (temporaryHPPercent / 100f)));
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(temporaryHP), defender, duration);
        }

        private static void TrackRecentDamageTarget(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            _recentDamageTargets[(attacker, defender)] = DateTime.UtcNow;
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

            TemporaryStatModifier.Replace(
                defender,
                StatType.NextAttackNoDelay,
                (int)skillType,
                window,
                StatType.NextAttackNoDelay);
        }

        private static bool DidCrossHPThreshold(uint creature, int damage, int thresholdPercent)
        {
            var maxHP = GetMaxHitPoints(creature);
            var currentHP = GetCurrentHitPoints(creature);
            if (maxHP <= 0 || currentHP <= 0)
                return false;

            var thresholdHP = maxHP * (thresholdPercent / 100f);
            var previousHP = currentHP + damage;
            return previousHP > thresholdHP && currentHP <= thresholdHP;
        }

        private static void HealPercentOfMaxHP(uint creature, int percent)
        {
            if (percent <= 0)
                return;

            var amount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(creature) * (percent / 100f)));
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
        }

        private static int ApplyTargetLowHPDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamageThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamagePercentAdjustment);

            if (threshold <= 0 || adjustment == 0)
                return damage;

            var maxHP = GetMaxHitPoints(defender);
            if (maxHP <= 0 || GetCurrentHitPoints(defender) > maxHP * (threshold / 100f))
                return damage;

            return damage + (int)Math.Ceiling(damage * (adjustment / 100f));
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

            if (StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect)))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToSunderedTargetPercentAdjustment);

            if (skillType == SkillType.Throwing &&
                StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToBleedingTargetPercentAdjustment);
            }

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Debuff))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToDebuffedTargetPercentAdjustment);

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

            if (skillType == SkillType.Rifle && StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Control))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToControlTargetPercentAdjustment);

            if (skillType == SkillType.Rifle &&
                StatusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect), typeof(DazedStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToDisorientedDazedTargetPercentAdjustment);
            }

            if (damageType.IsPhysicalDamageType() &&
                StatusEffect.HasStatusEffect(defender, typeof(ExposeWeakPointStatusEffect)))
            {
                adjustment += 10;
            }

            if (skillType == SkillType.Throwing &&
                StatusEffect.HasStatusEffect(defender, typeof(MarkingTossStatusEffect)))
            {
                adjustment += 10;
            }

            if (skillType == SkillType.Pistol && IsNearbyTargetNotTargetingAttacker(attacker, defender, 8f))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToNearbyNonTargetingTargetPercentAdjustment);

            if (isAbilityDamage &&
                skillType == SkillType.Staff &&
                StatusEffect.HasStatusEffect(defender, typeof(KnockdownStatusEffect), typeof(BlindStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToKnockdownOrBlindTargetPercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return damage + (int)Math.Ceiling(damage * (adjustment / 100f));
        }

        private static bool IsNearbyTargetNotTargetingAttacker(uint attacker, uint defender, float distance)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetArea(attacker) != GetArea(defender) ||
                GetDistanceBetween(attacker, defender) > distance)
                return false;

            var target = GetAttackTarget(defender);
            return GetIsObjectValid(target) && target != attacker;
        }

        private static bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
        {
            if (cooldownSeconds <= 0)
                return true;

            var key = (creature, statType);
            var now = DateTime.UtcNow;
            if (_statTriggerCooldowns.TryGetValue(key, out var nextAvailable) && nextAvailable > now)
                return false;

            _statTriggerCooldowns[key] = now.AddSeconds(cooldownSeconds);
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

            switch (summary.SkillType)
            {
                case SkillType.Pistol:
                    ApplyNextAutoAttackDamageBonus(
                        activator,
                        StatType.PistolAbilityUsedNextAutoAttackDamageBonus,
                        StatType.PistolAbilityUsedNextAutoAttackDamageDurationSeconds);
                    ApplyAbilityUsedEvasion(
                        activator,
                        StatType.PistolAbilityUsedEvasionPercentAdjustment,
                        StatType.PistolAbilityUsedEvasionDurationSeconds);
                    break;
                case SkillType.Throwing:
                    ApplyNextAutoAttackDamageBonus(
                        activator,
                        StatType.ThrowingAbilityUsedNextAutoAttackDamageBonus,
                        StatType.ThrowingAbilityUsedNextAutoAttackDamageDurationSeconds);
                    break;
                case SkillType.TwinBlade:
                    ApplyAbilityUsedEvasion(
                        activator,
                        StatType.TwinBladeAbilityUsedEvasionPercentAdjustment,
                        StatType.TwinBladeAbilityUsedEvasionDurationSeconds);
                    if (summary.IsSingleTargetAbility)
                    {
                        ApplyAbilityUsedAttackDeflection(
                            activator,
                            StatType.TwinBladeSingleTargetAbilityAttackDeflection,
                            StatType.TwinBladeSingleTargetAbilityAttackDeflectionDurationSeconds);
                    }
                    break;
            }
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
                    ApplySaberstaffAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.TwinBlade:
                    ApplyTwinBladeAbilityImpactEffects(activator, summary);
                    break;
            }
        }

        public static int CalculateAbilityCriticalRating(uint attacker, SkillType skillType, bool isAreaAbility)
        {
            if (!GetIsObjectValid(attacker) || !isAreaAbility)
                return 0;

            var criticalRate = 0;
            if (skillType == SkillType.TwinBlade)
            {
                criticalRate += Stat.GetStatAdjustment(attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment);
            }

            return criticalRate > 0 && Random.D100(1) <= criticalRate
                ? StandardCriticalRating
                : 0;
        }

        public static bool ConsumeNextAbilityNoDelay(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return false;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackNoDelay,
                StatType.NextAttackNoDelay));
            if (storedSkillType != skillType)
                return false;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackNoDelay,
                StatType.NextAttackNoDelay);

            return true;
        }

        public static SkillType GetAbilitySkillType(uint creature, AbilityDetail ability)
        {
            if (ability == null || ability.SkillType != SkillType.Invalid)
                return ability?.SkillType ?? SkillType.Invalid;

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

        public static void GrantNextAbilityDamageBonus(uint creature, int perkTypeValue, int bonus, int durationSeconds)
        {
            var perkType = GetPerkTypeFromStat(perkTypeValue);
            GrantNextAbilityDamageBonus(creature, perkType, bonus, durationSeconds);
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

        private static void ApplyAbilityUsedRecastReduction(uint activator, AbilityDetail ability)
        {
            var triggerGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTriggerGroup));
            var secondaryTriggerGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSecondaryTriggerGroup));
            if (ability.RecastGroup == RecastGroup.Invalid ||
                ability.RecastGroup != triggerGroup &&
                ability.RecastGroup != secondaryTriggerGroup)
                return;

            var targetGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTargetGroup));
            var seconds = Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSeconds);
            if (targetGroup == RecastGroup.Invalid || seconds <= 0)
                return;

            Recast.ReduceRecastDelay(activator, targetGroup, seconds);
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
            StatType durationStatType)
        {
            var evasionPercent = Stat.GetStatAdjustment(activator, evasionStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (evasionPercent == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.EvasionPercentAdjustment,
                evasionPercent,
                duration,
                StatType.EvasionPercentAdjustment);
        }

        private static void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType)
        {
            var attackDeflection = Stat.GetStatAdjustment(activator, attackDeflectionStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (attackDeflection == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackDeflection,
                attackDeflection,
                duration,
                StatType.AttackDeflection);
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

        private static void ApplySaberstaffAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var restoreThreshold = Stat.GetStatAdjustment(activator, StatType.SaberstaffAreaAbilityMinTargetsResourceRestoreThreshold);
            var fpRestore = Stat.GetStatAdjustment(activator, StatType.SaberstaffAreaAbilityFPRestore);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.SaberstaffAreaAbilityStaminaRestore);
            var restoreCooldown = Stat.GetStatAdjustment(activator, StatType.SaberstaffAreaAbilityResourceRestoreCooldownSeconds);
            if (restoreThreshold > 0 &&
                summary.ImpactedTargetCount >= restoreThreshold &&
                TryUseStatTrigger(activator, StatType.SaberstaffAreaAbilityFPRestore, restoreCooldown))
            {
                if (fpRestore > 0)
                    Stat.RestoreFP(activator, fpRestore);
                if (staminaRestore > 0)
                    Stat.RestoreStamina(activator, staminaRestore);
            }

            var buffThreshold = Stat.GetStatAdjustment(activator, StatType.SaberstaffAreaAbilityMinTargetsBuffThreshold);
            if (buffThreshold <= 0 || summary.ImpactedTargetCount < buffThreshold)
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.SaberstaffAreaAbilityBuffDurationSeconds);
            if (duration <= 0)
                return;

            var haste = Stat.GetStatAdjustment(activator, StatType.SaberstaffAreaAbilityHastePercentAdjustment);
            if (haste != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    duration,
                    StatType.AttackDelayReductionPercent);
            }

            var deflection = Stat.GetStatAdjustment(activator, StatType.SaberstaffAreaAbilityAttackDeflection);
            if (deflection != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDeflection,
                    deflection,
                    duration,
                    StatType.AttackDeflection);
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

        private static bool SkillTypeMatches(SkillType actualSkillType, SkillType requiredSkillType)
        {
            return requiredSkillType == SkillType.Invalid || actualSkillType == requiredSkillType;
        }

        private static PerkType GetPerkTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(PerkType), value)
                ? (PerkType)value
                : PerkType.Invalid;
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

            var attackerName = GetIsPC(attacker) ? ColorToken.GetNamePCColor(attacker) : ColorToken.GetNameNPCColor(attacker);
            var defenderName = GetIsPC(defender) ? ColorToken.GetNamePCColor(defender) : ColorToken.GetNameNPCColor(defender);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
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
            CNWSCreature attacker,
            CNWSCreature defender,
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
                case 2:
                    type = ": *deflect*";
                    break;
            }

            var attackerName = ColorToken.GetNameColorNative(attacker);
            var defenderName = ColorToken.GetNameColorNative(defender);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
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
            var rightHand = GetItemInSlot(InventorySlot.RightHand, attacker);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, attacker);

            var delay = GetWeaponDelay(rightHand) + GetWeaponDelay(leftHand);
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

            // Convert delay units to milliseconds: 60 delay units = 1 second.
            var finalDelay = (int)(delay / 60f * 1000);
            var reductionPercentage = CalculateAttackDelayReduction(attacker);

            if (reductionPercentage > 0)
            {
                var reductionAmount = (int)(finalDelay * (reductionPercentage / 100f));
                finalDelay -= reductionAmount;
            }

            return finalDelay;
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

                var creatureName = GetName(attacker);
                Messaging.SendMessageNearbyToPlayers(attacker, $"{creatureName} is paralyzed and cannot act!");
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

            return Math.Min(totalReduction, 50);
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

    }
}

using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static class Combat
    {
        private const float DamageStatDeltaMultiplier = 0.35f;
        private const int BaseGuardDamageReductionPercent = 20;
        private const int MaximumGuardDamageReductionPercent = 40;

        public const int StandardCriticalRating = 2;

        private static readonly List<CombatDamageType> _allValidDamageTypes = new();
        private static readonly List<CombatDamageType> _allDefenseDamageTypes = new();
        private static readonly Dictionary<(uint, StatType), DateTime> _statTriggerCooldowns = new();
        private static readonly Dictionary<(uint, uint), DateTime> _recentDamageTargets = new();
        private static readonly Dictionary<uint, DateTime> _recentDamageTaken = new();
        private static readonly Dictionary<uint, DateTime> _recentGuardedHits = new();
        private static readonly Dictionary<uint, DateTime> _lastCombatActivity = new();
        private static readonly Dictionary<uint, DateTime> _lastCombatAbilityUse = new();
        private static readonly Dictionary<uint, int> _autoAttackCycleCounts = new();
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
        /// <param name="attackerPER">The attacker's perception stat.</param>
        /// <param name="defenderVIT">The defender's vitality stat.</param>
        /// <param name="skillRank">The attacker's relevant weapon skill rank.</param>
        /// <param name="criticalModifier">A modifier to the critical rating based on external factors.</param>
        /// <returns>The critical rate, in a percentage</returns>
        public static int CalculateCriticalRate(int attackerPER, int defenderVIT, int skillRank, int criticalModifier)
        {
            const int BaseCriticalRate = 5;
            const int MaxCriticalRate = 50;
            var skillBonus = Math.Max(0, skillRank / 10);
            var statBonus = Math.Clamp((int)Math.Floor((attackerPER - defenderVIT) / 5.0f), 0, 3);

            var criticalRate = BaseCriticalRate + skillBonus + statBonus + criticalModifier;
            if (criticalRate < BaseCriticalRate)
                criticalRate = BaseCriticalRate;
            else if (criticalRate > MaxCriticalRate)
                criticalRate = MaxCriticalRate;


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

        public static (int Damage, int CriticalRating) CalculateDamageWithCriticalMitigation(
            uint defender,
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var forceMinimumNormalDamage = GetIsObjectValid(defender) &&
                (critical > 0 && Stat.GetStatAdjustment(defender, StatType.IncomingCriticalHitDowngradeToMinimumDamage) > 0 ||
                 TemporaryStatModifier.Consume(
                     defender,
                     StatType.CurrentIncomingAttackMinimumDamage,
                     StatType.CurrentIncomingAttackMinimumDamage) > 0);
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

            return (damage, effectiveCritical);
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

            damage = Math.Max(0, damage);
            return PreventFatalDamageAndGrantTemporaryHP(defender, damage);
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
                adjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageReflectionPercentAdjustment);

            if (damageType.IsElementalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.ElementalDamageReflectionPercentAdjustment);

            if (adjustment <= 0)
                return;

            var reflectedDamage = Math.Max(1, (int)Math.Ceiling(damage * (adjustment / 100f)));
            reflectedDamage = Resistance.ApplyResistanceToDamage(attacker, damageType, reflectedDamage);
            if (reflectedDamage <= 0)
                return;

            ApplyEffectToObject(
                DurationType.Instant,
                EffectDamage(reflectedDamage, damageType.GetNWScriptDamageType()),
                attacker);
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

            if (adjustment <= -100)
                return 0;

            if (adjustment != 0)
                damage += (int)Math.Ceiling(damage * (adjustment / 100f));

            return Math.Max(0, damage);
        }

        private static int PreventFatalDamageAndGrantTemporaryHP(uint defender, int damage)
        {
            if (damage <= 0)
                return damage;

            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPDurationSeconds);
            if (temporaryHPPercent <= 0 || duration <= 0)
                return damage;

            var currentHP = GetCurrentHitPoints(defender);
            if (currentHP <= 0 || damage < currentHP)
                return damage;

            var scalingAbilityScore = Stat.GetStatAdjustment(defender, StatType.FatalDamageTemporaryHPScalingAbilityScore);
            var tempHP = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(defender) * (temporaryHPPercent / 100f)));
            if (scalingAbilityScore > 0)
                tempHP = AbilityEffectScaling.ScaleDirectEffect(tempHP, scalingAbilityScore);

            StatusEffect.RemoveStatusEffectsWithStat(defender, StatType.FatalDamageTemporaryHPPercent, false);

            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(tempHP), defender, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), defender);

            return 0;
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

            damage = ApplyOutgoingDamageModifier(attacker, damage);
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

            ApplyAutoAttackCycleDamage(attacker, defender, skillType);

            return damage;
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

        private static void ApplyAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamageSkillType));
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRequiredCount);
            var cycleDamage = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamage);
            var radius = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRadiusMeters);
            if (!SkillTypeMatches(skillType, requiredSkillType) ||
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
            var target = GetNearestHostileCreatureWithinRange(attacker, defender, radius, defender);
            if (!GetIsObjectValid(target))
                return;

            ApplyEffectToObject(DurationType.Instant, EffectDamage(cycleDamage), target);
            ApplyDamageDealtEffects(attacker, target, cycleDamage, skillType);
            Enmity.ModifyEnmity(attacker, target, cycleDamage);
        }

        public static void ApplyDamageDealtEffects(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical)
        {
            if (damage <= 0)
                return;

            TrackCombatActivity(attacker);
            TrackRecentDamageTarget(attacker, defender);
            ApplySideAttackDamageEffects(attacker, defender, skillType, damage);
            ApplyDamageDealtForceErosionEffect(attacker, defender);

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                HealFromDamage(attacker, damage, hpRestorePercent);
            }

            if (damageType.IsPhysicalDamageType())
            {
                hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.PhysicalDamageDealtHPPercentRestore);
                if (hpRestorePercent > 0)
                {
                    HealFromDamage(attacker, damage, hpRestorePercent);
                }
            }
        }

        private static void ApplyDamageDealtForceErosionEffect(uint attacker, uint defender)
        {
            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionDurationSeconds);
            if (duration <= 0)
                return;

            var fpLossPerTick = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionFPLossPerTick);
            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ForceErosionStatusEffect(fpLossPerTick),
                duration,
                CombatDamageType.Physical);
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
            if (requiredSkillType == SkillType.Invalid)
            {
                requiredSkillType = GetEquippedWeaponSkillType(attacker);
            }

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
            ApplyCriticalNextSkillAbilityDefenseIgnore(attacker, skillType);
            ApplyCriticalNextAbilityNoDelay(attacker, skillType);
            ApplyCriticalSideAttackStaminaRestore(attacker, defender);

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

        private static void ApplyCriticalNextAbilityNoDelay(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityNoDelayTriggerSkillType));
            if (!SkillTypeMatches(skillType, triggerSkillType))
                return;

            var noDelaySkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityNoDelaySkillType));
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityNoDelayDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityNoDelayCooldownSeconds);
            if (noDelaySkillType == SkillType.Invalid || duration <= 0)
                return;

            if (TryUseStatTrigger(attacker, StatType.CriticalNextAbilityNoDelaySkillType, cooldown))
            {
                GrantNextAbilityNoDelay(attacker, noDelaySkillType, duration);
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

        public static bool IsAttackerBesideTarget(uint attacker, uint defender)
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
            ApplyLowHPGuardEffect(defender, damage);
            TrackRecentDamageTaken(defender);
            ApplyRecentDamageTargetHitEffects(defender, attacker);
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

        private static void HealFromDamage(uint creature, int damage, int percent)
        {
            if (damage <= 0 || percent <= 0)
                return;

            var amount = Math.Max(1, (int)Math.Ceiling(damage * (percent / 100f)));
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount);
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

        private static void ApplyLowHPGuardEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPGuardThresholdPercent);
            var guardChance = Stat.GetStatAdjustment(defender, StatType.LowHPGuard);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPGuardDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPGuardCooldownSeconds);

            if (threshold <= 0 ||
                guardChance <= 0 ||
                duration <= 0 ||
                !DidCrossHPThreshold(defender, damage, threshold) ||
                !TryUseStatTrigger(defender, StatType.LowHPGuard, cooldown))
                return;

            TemporaryStatModifier.Replace(
                defender,
                StatType.Guard,
                guardChance,
                duration,
                StatType.LowHPGuard);
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

            _lastCombatActivity[creature] = DateTime.UtcNow;
        }

        public static void TrackAttackActivity(uint creature)
        {
            TrackCombatActivity(creature);
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
            ApplyGuardedHitNextSkillAbilityEffects(creature);
            ApplyGuardedHitNextMatchingAbilityEffects(creature);
        }

        public static void TrackAvoidedAttack(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilitySkillType));
            var adjustment = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment);
            var window = Stat.GetStatAdjustment(creature, StatType.AvoidedAttackNextSkillAbilityWindowSeconds);
            GrantNextSkillAbilityStaminaCostAdjustment(creature, skillType, adjustment, window);
        }

        public static void ApplyMeleeDamageTakenEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(attacker))
                return;

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

        public static int ApplyGuardedHitModifiers(uint defender, uint attacker, int damage)
        {
            if (!GetIsObjectValid(defender) ||
                !GetIsObjectValid(attacker) ||
                defender == attacker ||
                damage <= 0)
                return damage;

            var guardChance = Stat.GetGuardChance(defender);
            if (guardChance <= 0 || Random.D100(1) > guardChance)
                return damage;

            var reductionPercent = GetGuardDamageReductionPercent(defender);
            var preventedDamage = Math.Min(damage, Math.Max(1, (int)Math.Ceiling(damage * (reductionPercent / 100f))));
            var adjustedDamage = Math.Max(0, damage - preventedDamage);

            TrackGuardedHit(defender);
            ApplyGuardedHitRecovery(defender);
            ApplyGuardedHitRetaliation(attacker, defender);
            ApplyGuardedHitEnmity(attacker, defender, damage);

            return adjustedDamage;
        }

        private static int GetGuardDamageReductionPercent(uint defender)
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

        private static void ApplyGuardedHitRetaliation(uint attacker, uint defender)
        {
            var retaliationDamage = Stat.GetStatAdjustment(defender, StatType.GuardRetaliationDamage);
            if (retaliationDamage <= 0)
                return;

            AssignCommand(defender, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(retaliationDamage), attacker));
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

        private static void ApplyGuardedHitNextSkillAbilityEffects(uint creature)
        {
            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilitySkillType));
            var criticalRate = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment);
            var damageBonus = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityDamageBonus);
            var window = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextSkillAbilityWindowSeconds);

            GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRate, window);
        }

        private static void ApplyGuardedHitNextMatchingAbilityEffects(uint creature)
        {
            var perkType = GetPerkTypeFromStat(Stat.GetStatAdjustment(creature, StatType.GuardedHitNextMatchingAbilityPerkType));
            var damageBonus = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextMatchingAbilityDamageBonus);
            var staminaCostAdjustment = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextMatchingAbilityStaminaCostAdjustment);
            var window = Stat.GetStatAdjustment(creature, StatType.GuardedHitNextMatchingAbilityWindowSeconds);

            GrantNextAbilityDamageBonus(creature, perkType, damageBonus, window);
            GrantNextAbilityStaminaCostAdjustment(creature, perkType, staminaCostAdjustment, window);
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
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
        }

        private static int ApplyOutgoingDamageModifier(uint attacker, int damage)
        {
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.DamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return damage + (int)Math.Ceiling(damage * (adjustment / 100f));
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

            if (damageType.IsPhysicalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalDamageTakenPercentAdjustment);

            if (damageType == CombatDamageType.Force)
                adjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenPercentAdjustment);

            if (damageType.IsPhysicalDamageType() && IsRangedDamageSkill(skillType))
                adjustment += Stat.GetStatAdjustment(defender, StatType.RangedPhysicalDamageTakenPercentAdjustment);

            if (skillType == SkillType.Throwing)
                adjustment += Stat.GetStatAdjustment(defender, StatType.ThrowingDamageTakenPercentAdjustment);

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

            return Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
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

        private static bool IsRangedDamageSkill(SkillType skillType)
        {
            return skillType == SkillType.Pistol ||
                   skillType == SkillType.Rifle ||
                   skillType == SkillType.Throwing ||
                   skillType == SkillType.Devices;
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

            _recentDamageTaken.Remove(creature);
            _recentGuardedHits.Remove(creature);
            _lastCombatActivity.Remove(creature);
            _lastCombatAbilityUse.Remove(creature);
            _autoAttackCycleCounts.Remove(creature);
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

            TrackCombatAbilityUse(activator, ability);
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

        public static int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var criticalRate = criticalRateAdjustment;
            criticalRate += GetAbilityHitOrCriticalAdjustment(
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
                criticalRate += Stat.GetStatAdjustment(attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment);
            }

            if (skillType == SkillType.Throwing &&
                GetIsObjectValid(defender) &&
                (StatusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect)) ||
                 StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding)))
            {
                criticalRate += Stat.GetStatAdjustment(attacker, StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment);
            }

            if (GetIsObjectValid(defender) && IsTargetNotFacingAttacker(attacker, defender))
            {
                criticalRate += Stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment);
            }

            criticalRate += GetSideAttackCriticalRateAdjustment(attacker, defender, skillType);

            return criticalRate > 0 && Random.D100(1) <= criticalRate
                ? StandardCriticalRating
                : 0;
        }

        public static bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate)
        {
            hitRate = 100;
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return true;

            var accuracy = GetAbilityAccuracy(attacker, defender, skillType);
            var evasion = Stat.GetEvasion(defender, SkillType.Invalid);
            evasion = ApplySideAttackEvasionIgnore(attacker, defender, skillType, evasion);

            var modifier = GetAbilityHitOrCriticalAdjustment(
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
            modifier += GetIncomingAbilityHitChanceAdjustment(defender, skillType);
            modifier += GetSideAttackHitChanceAdjustment(attacker, defender, skillType);
            modifier += GetIdleAbilityHitChanceAdjustment(attacker, skillType);
            if (skillType == SkillType.Force)
            {
                modifier += Perk.GetForceAffinityHitChanceAdjustment(attacker, perkType);
            }

            hitRate = CalculateHitRate(accuracy, evasion, modifier);
            var isHit = Random.D100(1) <= hitRate;
            if (!isHit && skillType == SkillType.Force)
            {
                ApplyForceAbilityEvadedEffects(defender);
            }

            return isHit;
        }

        private static int GetAbilityAccuracy(uint attacker, uint defender, SkillType skillType)
        {
            var weapon = GetRelevantSkillWeapon(attacker, skillType);
            var statOverride = skillType == SkillType.Force
                ? AbilityType.Willpower
                : AbilityType.Invalid;

            var accuracy = Stat.GetAccuracy(attacker, weapon, statOverride, skillType);
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
            return GetIsObjectValid(leftHand) ? leftHand : rightHand;
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

            return angleDegrees > 90.0;
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

        public static void GrantNextAbilityNoDelay(uint creature, int skillTypeValue, int durationSeconds)
        {
            var skillType = GetSkillTypeFromStat(skillTypeValue);
            GrantNextAbilityNoDelay(creature, skillType, durationSeconds);
        }

        public static void GrantNextAbilityNoDelay(uint creature, SkillType skillType, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackNoDelay,
                (int)skillType,
                durationSeconds,
                StatType.NextAttackNoDelay);
        }

        public static SkillType GetAbilitySkillType(uint creature, AbilityDetail ability)
        {
            if (ability == null || ability.SkillType != SkillType.Invalid)
                return ability?.SkillType ?? SkillType.Invalid;

            return GetEquippedWeaponSkillType(creature);
        }

        private static SkillType GetEquippedWeaponSkillType(uint creature)
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
            if (storedSkillType != skillType)
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

        public static int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType)
        {
            return GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDamageFlatAdjustmentPerkType,
                StatType.AbilityDamageFlatAdjustmentSecondaryPerkType,
                StatType.AbilityDamageFlatAdjustment);
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
            if (staminaCost <= 0 || ability == null)
                return;

            var skillType = GetAbilitySkillType(creature, ability);
            var restoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStaminaCostFPRestorePercentSkillType));
            var restorePercent = Stat.GetStatAdjustment(creature, StatType.AbilityStaminaCostFPRestorePercent);
            if (restorePercent <= 0 || !SkillTypeMatches(skillType, restoreSkillType))
                return;

            Stat.RestoreFP(creature, CalculateResourceRestoreFromCost(staminaCost, restorePercent));
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

            Stat.RestoreStamina(creature, CalculateResourceRestoreFromCost(fpCost, restorePercent));
        }

        private static int CalculateResourceRestoreFromCost(int cost, int percent)
        {
            if (cost <= 0 || percent <= 0)
                return 0;

            return Math.Max(1, (int)Math.Ceiling(cost * (percent / 100f)));
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

        public static int GetAbilityStatusDurationPercentAdjustment(uint creature, PerkType perkType)
        {
            return GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityStatusDurationPercentAdjustmentPerkType,
                StatType.AbilityStatusDurationPercentAdjustmentSecondaryPerkType,
                StatType.AbilityStatusDurationPercentAdjustment);
        }

        public static int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender)
        {
            var adjustment = GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentSecondaryPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustment);

            var exposedOrSunderedSkillType = GetSkillTypeFromStat(
                Stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType));
            if (SkillTypeMatches(skillType, exposedOrSunderedSkillType) &&
                GetIsObjectValid(defender) &&
                StatusEffect.HasStatusEffect(defender, typeof(ExposedStatusEffect), typeof(SunderStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment);
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

        public static void ApplyCriticalAbilityStatusEffects(uint attacker, uint defender, PerkType perkType, CombatDamageType sourceDamageType)
        {
            if (perkType == PerkType.Invalid || !GetIsObjectValid(defender))
                return;

            var knockdownPerk = GetPerkTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalAbilityKnockdownPerkType));
            var knockdownDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalAbilityKnockdownDurationSeconds);
            if (knockdownPerk != perkType || knockdownDuration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                typeof(KnockdownStatusEffect),
                knockdownDuration,
                sourceDamageType);
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

        private static void TrackCombatAbilityUse(uint activator, AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var skillType = GetAbilitySkillType(activator, ability);
            if (skillType == SkillType.Invalid)
                return;

            _lastCombatAbilityUse[activator] = DateTime.UtcNow;
        }

        public static (int DamageBonus, int HitChancePercentAdjustment) GetIdleSkillAbilityBonuses(uint activator, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return (0, 0);

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilitySkillType));
            var requiredIdleSeconds = Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityRequiredIdleSeconds);
            if (!SkillTypeMatches(skillType, requiredSkillType) || requiredIdleSeconds <= 0)
                return (0, 0);

            if (_lastCombatAbilityUse.TryGetValue(activator, out var lastUse) &&
                (DateTime.UtcNow - lastUse).TotalSeconds < requiredIdleSeconds)
                return (0, 0);

            return (
                Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityDamageBonus),
                Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityHitChancePercentAdjustment));
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
                skillType == SkillType.Invalid ||
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

        private static void GrantNextSkillAbilityStaminaCostAdjustment(
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

        public static string BuildAbilityCombatLogMessage(
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

            var attackerName = GetIsPC(attacker) ? ColorToken.GetNamePCColor(attacker) : ColorToken.GetNameNPCColor(attacker);
            var defenderName = GetIsPC(defender) ? ColorToken.GetNamePCColor(defender) : ColorToken.GetNameNPCColor(defender);

            return ColorToken.Combat($"{attackerName} uses {abilityName} on {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        public static string BuildAbilityNoTargetCombatLogMessage(
            uint attacker,
            string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                abilityName = "an ability";

            var attackerName = GetIsPC(attacker) ? ColorToken.GetNamePCColor(attacker) : ColorToken.GetNameNPCColor(attacker);

            return ColorToken.Combat($"{attackerName} uses {abilityName}, but it hits no targets.");
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

            var rightHandDelay = GetWeaponDelay(rightHand);
            var leftHandDelay = ApplyOffhandAttackDelayReduction(attacker, GetWeaponDelay(leftHand));

            var delay = rightHandDelay + leftHandDelay;
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

        private static int ApplyOffhandAttackDelayReduction(uint attacker, int offhandDelay)
        {
            if (offhandDelay <= 0)
                return offhandDelay;

            var reductionPercentage = CalculateOffhandAttackDelayReduction(attacker);
            if (reductionPercentage <= 0)
                return offhandDelay;

            var reductionAmount = (int)(offhandDelay * (reductionPercentage / 100f));
            return Math.Max(0, offhandDelay - reductionAmount);
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

        public static int CalculateOffhandAttackDelayReduction(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalReduction = Stat.GetStatAdjustment(attacker, StatType.OffhandAttackDelayReductionPercent);

            return Math.Min(Math.Max(totalReduction, 0), 50);
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

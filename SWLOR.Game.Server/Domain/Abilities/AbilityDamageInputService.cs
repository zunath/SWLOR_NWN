using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The damage inputs of an ability hit (part 16 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>AbilityService</c> unchanged apart from one split. The player and NPC damage formulas reached more than ten
    /// services between them, so each was split where its inputs end (the defense ignore) and its stages begin (the critical
    /// rating): the inputs are here and hand their values over in an <see cref="AbilityDamageInputs"/>, and
    /// <see cref="IAbilityDamageStageService"/> runs each formula, asking for its inputs first and then running its stages, in
    /// the original order. The unscaled formula, the NPC scaling helpers and the weapon trait damage bonus are here unchanged.
    /// Calls into other services sit behind <c>protected virtual</c> methods whose bodies are the original calls, so a unit
    /// test can answer them.
    /// </summary>
    public class AbilityDamageInputService : IAbilityDamageInputService
    {
        private const int MinNPCAbilityScalingRank = 1;
        private const int MaxNPCAbilityScalingRank = 100;

        private readonly IAbilityImpactState _impactState;
        private readonly IStatService _stat;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatQueriesService _queries;
        private readonly ICombatAbilityBonusService _abilityBonus;
        private readonly ICombatAbilityImpactService _abilityImpact;
        private readonly ICombatDamageModifiersService _damageModifiers;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly ICombatAbilityMagnitudeService _magnitude;
        private readonly IResistanceService _resistance;

        public AbilityDamageInputService(
            IAbilityImpactState impactState,
            IStatService stat,
            ICombatActivityService combatActivity,
            ICombatQueriesService queries,
            ICombatAbilityBonusService abilityBonus,
            ICombatAbilityImpactService abilityImpact,
            ICombatDamageModifiersService damageModifiers,
            ICombatDamageTakenService damageTaken,
            ICombatAbilityMagnitudeService magnitude,
            IResistanceService resistance)
        {
            _impactState = impactState;
            _stat = stat;
            _combatActivity = combatActivity;
            _queries = queries;
            _abilityBonus = abilityBonus;
            _abilityImpact = abilityImpact;
            _damageModifiers = damageModifiers;
            _damageTaken = damageTaken;
            _magnitude = magnitude;
            _resistance = resistance;
        }

        public bool ShouldUseNPCStatScaling(uint activator, bool useNPCStatScaling)
        {
            return useNPCStatScaling &&
                   GetIsObjectValid(activator) &&
                   !GetIsPC(activator);
        }

        public bool UsesQueuedNaturalWeapon(AbilityImpactTracker trackedImpact, SkillType skillType)
        {
            return trackedImpact?.Ability?.ActivationType == AbilityActivationType.Weapon &&
                   skillType == SkillType.BeastMastery;
        }

        private int ScaleImpactWeaponDamage(int damage, AbilityImpactTracker impact)
        {
            return impact?.Ability.MeleeTechnique is { } technique
                ? TechniqueSequence.ScaleWeaponDamage(damage, technique.WeaponDamagePercent) : damage;
        }

        private bool HasCombatImpactDamage(int baseDamage, int capturedDamageBonus, bool usesWeaponDamage)
        {
            return baseDamage > 0 || capturedDamageBonus > 0 || usesWeaponDamage;
        }

        private int TakeWeaponTraitDamageBonus(uint activator, SkillType skill, AbilityImpactTracker impact)
        {
            if (!IsWeaponSkillType(skill) || impact?.Sequence.WeaponTraits is not { IsCommitted: true } traits) return 0;
            var profile = WeaponImpactProfile.Get(activator);
            return traits.TryTakeDamageBonus(profile?.Weapon.Weapon ?? traits.PrimaryWeapon,
                profile?.VolleyIndex ?? 0, profile?.VolleyCount ?? 1);
        }

        public int CalculateUnscaledCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (!HasCombatImpactDamage(baseDamage, trackedImpact?.NextAbilityDamageBonus ?? 0, false))
                return 0;

            var damage = baseDamage + (trackedImpact?.NextAbilityDamageBonus ?? 0);
            damage = ApplyDamageDealtModifiers(
                activator,
                target,
                damage,
                skillType,
                damageType,
                isAbilityDamage: true,
                canApplyRandomFlatBonuses: true,
                isLandedAttack: true,
                ability: trackedImpact?.Ability,
                targetStatusDamagePercentAdjustment: out var targetStatusDamageAdjustment,
                abilityDamagePercentAdjustment,
                lowHPAbilityDamagePercentAdjustment);
            damage = _magnitude.ApplyCombatReadinessToActivatedAbilityMagnitude(activator, damage);
            ApplyIncomingPhysicalToForceConversion(activator, target, damageType, ref damage, targetStatusDamageAdjustment);
            damage = ApplyTypedLeadershipDamageTakenModifier(target, damage, damageType);
            damage = _resistance.ApplyResistanceToDamage(target, damageType, damage);
            damage = ApplyDamageTakenModifiers(
                target,
                damage,
                activator,
                damageType,
                targetStatusDamagePercentAdjustment: targetStatusDamageAdjustment,
                typedLeadershipReductionAlreadyApplied: true);

            return damage;
        }

        public bool TryGetCombatImpactDamageInputs(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            AbilityType combatImpactDamageAbility,
            out AbilityDamageInputs inputs)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            var usesQueuedNaturalWeapon = UsesQueuedNaturalWeapon(trackedImpact, skillType);
            if (!HasCombatImpactDamage(baseDamage, trackedImpact?.NextAbilityDamageBonus ?? 0,
                    IsWeaponSkillType(skillType) || usesQueuedNaturalWeapon))
            {
                inputs = default;
                return false;
            }

            var ability = GetCombatImpactDamageAbility(activator, combatImpactDamageAbility);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var idleBonuses = GetIdleSkillAbilityBonuses(activator, skillType);
            var damage = baseDamage +
                ScaleImpactWeaponDamage(
                    WeaponImpactProfile.Get(activator)?.Weapon.Damage ??
                    GetCombatImpactWeaponDamage(activator, skillType, usesQueuedNaturalWeapon, triggeringWeaponDamage: trackedImpact?.TriggeringWeaponDamage, abilityImpact: true),
                    trackedImpact) +
                GetAbilityDamageBonus(activator, skillType) +
                GetAbilityDamageFlatAdjustment(activator, perkType, skillType) +
                GetCostlyAbilityDamageBonus(activator, trackedImpact?.Ability, skillType) +
                GetSameTargetPressureWeaponAbilityDamageBonus(activator, target, skillType) +
                idleBonuses.DamageBonus + TakeWeaponTraitDamageBonus(activator, skillType, trackedImpact);
            if (trackedImpact != null)
            {
                damage += trackedImpact.NextAbilityDamageBonus;
            }

            var attack = GetAttack(activator, ability, skillType);
            attack = ApplyTargetStatusAttackModifiers(activator, target, attack, skillType);
            var attackStat = GetAbilityScore(activator, ability);
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = GetDefense(target, damageType, defenseAbility);
            defense = ApplyStatusSourceDefenseModifiers(activator, target, defense);
            defense = ApplyIncomingPhysicalToForceDefenseConversion(
                target,
                damageType,
                defense,
                () => ApplyStatusSourceDefenseModifiers(
                    activator,
                    target,
                    GetDefense(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            var defenderStat = GetAbilityScore(target, defenseAbility);
            var defenseIgnorePercent =
                GetAbilityDefenseIgnorePercentAdjustment(activator, perkType, skillType, target) +
                (trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0);
            defense = ApplyDefenseIgnore(defense, defenseIgnorePercent);

            inputs = new AbilityDamageInputs(
                trackedImpact,
                perkType,
                idleBonuses.CriticalDamagePercentAdjustment,
                damage,
                attack,
                attackStat,
                defense,
                defenderStat);
            return true;
        }

        private int GetNPCAbilityScalingRank(
            uint activator,
            SkillType skillType,
            CombatDamageType damageType,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid)
        {
            var npcStats = GetNPCStats(activator);
            if (npcStats.Skills.TryGetValue(skillType, out var skillRank) && skillRank > 0)
            {
                return Math.Clamp(skillRank, MinNPCAbilityScalingRank, MaxNPCAbilityScalingRank);
            }

            var ability = GetCombatImpactDamageAbility(activator, combatImpactDamageAbility);
            var abilityScore = ability == AbilityType.Invalid
                ? 0
                : GetAbilityScore(activator, ability);
            var rankFromAbility = Math.Max(MinNPCAbilityScalingRank, abilityScore - 8);
            var rankFromOffense = Math.Max(0, GetNPCAbilityOffenseBonus(npcStats, skillType, damageType) / 2);
            var rankFromDefense = Math.Max(0, GetNPCAbilityRelevantDefense(npcStats, damageType) / 2);
            var rankFromResistance = Math.Max(0, GetNPCAbilityRelevantResistance(npcStats, damageType) / 2);
            var rank = new[]
            {
                rankFromAbility,
                rankFromOffense,
                rankFromDefense,
                rankFromResistance
            }.Max();

            return Math.Clamp(rank, MinNPCAbilityScalingRank, MaxNPCAbilityScalingRank);
        }

        private int GetNPCAbilityOffenseBonus(NPCStats npcStats, SkillType skillType, CombatDamageType damageType)
        {
            return skillType == SkillType.Force || damageType == CombatDamageType.Force
                ? npcStats.ForceAttack
                : npcStats.Attack;
        }

        private int GetNPCAbilityRelevantDefense(NPCStats npcStats, CombatDamageType damageType)
        {
            var defenseType = damageType.GetDefenseDamageType();
            if (npcStats.Defenses.TryGetValue(defenseType, out var defense))
            {
                return defense;
            }

            return npcStats.Defenses.Count > 0
                ? npcStats.Defenses.Values.Max()
                : 0;
        }

        private int GetNPCAbilityRelevantResistance(NPCStats npcStats, CombatDamageType damageType)
        {
            if (damageType.TryGetElementalResistanceType(out var resistanceType) &&
                npcStats.Resistances.TryGetValue(resistanceType, out var resistance))
            {
                return resistance;
            }

            if (damageType.TryGetSourceResistanceType(out resistanceType) &&
                npcStats.Resistances.TryGetValue(resistanceType, out resistance))
            {
                return resistance;
            }

            return 0;
        }

        public bool TryGetNPCCombatImpactDamageInputs(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            AbilityType combatImpactDamageAbility,
            out AbilityDamageInputs inputs)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            var usesQueuedNaturalWeapon = UsesQueuedNaturalWeapon(trackedImpact, skillType);
            if (!HasCombatImpactDamage(baseDamage, trackedImpact?.NextAbilityDamageBonus ?? 0,
                    IsWeaponSkillType(skillType) || usesQueuedNaturalWeapon))
            {
                inputs = default;
                return false;
            }

            var ability = GetCombatImpactDamageAbility(activator, combatImpactDamageAbility);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var idleBonuses = GetIdleSkillAbilityBonuses(activator, skillType);
            var scalingRank = GetNPCAbilityScalingRank(activator, skillType, damageType, combatImpactDamageAbility);
            var damage = baseDamage +
                ScaleImpactWeaponDamage(
                    GetCombatImpactWeaponDamage(activator, skillType, usesQueuedNaturalWeapon, triggeringWeaponDamage: trackedImpact?.TriggeringWeaponDamage, abilityImpact: true),
                    trackedImpact) +
                (int)Math.Ceiling(scalingRank * 0.15f) +
                GetAbilityDamageFlatAdjustment(activator, perkType, skillType) +
                GetCostlyAbilityDamageBonus(activator, trackedImpact?.Ability, skillType) +
                GetSameTargetPressureWeaponAbilityDamageBonus(activator, target, skillType) +
                idleBonuses.DamageBonus + TakeWeaponTraitDamageBonus(activator, skillType, trackedImpact);
            if (trackedImpact != null)
            {
                damage += trackedImpact.NextAbilityDamageBonus;
            }

            var npcStats = GetNPCStats(activator);
            var attackStat = ability == AbilityType.Invalid
                ? 0
                : GetAbilityScore(activator, ability);
            // The creature's own level, matching what its auto-attacks use. The derived scaling
            // rank still contributes the flat damage term above, but feeding it here collapsed the
            // 2 x level component of Attack -- a level 50 enemy resolved ability damage as though
            // it were rank ~16, cutting the attack/defense ratio to roughly half of intended.
            var attack = GetAttack(
                npcStats.Level,
                attackStat,
                GetNPCAbilityOffenseBonus(npcStats, skillType, damageType) + GetStatAdjustment(activator, StatType.Attack));
            attack = ApplyNPCAbilitySourceAttackModifiers(activator, skillType, attack);
            attack = ApplyTargetStatusAttackModifiers(activator, target, attack, skillType);
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = GetDefense(target, damageType, defenseAbility);
            defense = ApplyStatusSourceDefenseModifiers(activator, target, defense);
            defense = ApplyIncomingPhysicalToForceDefenseConversion(
                target,
                damageType,
                defense,
                () => ApplyStatusSourceDefenseModifiers(
                    activator,
                    target,
                    GetDefense(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            var defenderStat = GetAbilityScore(target, defenseAbility);
            var defenseIgnorePercent =
                GetAbilityDefenseIgnorePercentAdjustment(activator, perkType, skillType, target) +
                (trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0);
            defense = ApplyDefenseIgnore(defense, defenseIgnorePercent);

            inputs = new AbilityDamageInputs(
                trackedImpact,
                perkType,
                idleBonuses.CriticalDamagePercentAdjustment,
                damage,
                attack,
                attackStat,
                defense,
                defenderStat);
            return true;
        }

        private int ApplyNPCAbilitySourceAttackModifiers(uint activator, SkillType skillType, int attack)
        {
            var adjustment = GetStatAdjustment(activator, StatType.AttackPercentAdjustment);
            if (skillType == SkillType.Force)
            {
                adjustment += GetStatAdjustment(activator, StatType.ForceAttackPercentAdjustment);
            }

            adjustment += GetHighFPAndStaminaAttackAdjustment(activator);
            adjustment += GetNearbyStatusTargetAttackAdjustment(activator);
            adjustment += GetLowHPAttackAdjustment(activator);

            return Math.Max(1, ApplyPercentAdjustment(attack, adjustment));
        }

        private int GetHighFPAndStaminaAttackAdjustment(uint activator)
        {
            var threshold = GetStatAdjustment(activator, StatType.HighFPAndStaminaAttackThresholdPercent);
            var adjustment = GetStatAdjustment(activator, StatType.HighFPAndStaminaAttackPercentAdjustment);

            if (threshold <= 0 || adjustment == 0)
                return 0;

            var currentFP = GetCurrentFP(activator);
            var maxFP = GetMaxFP(activator);
            var currentStamina = GetCurrentStamina(activator);
            var maxStamina = GetMaxStamina(activator);

            if (maxFP <= 0 || maxStamina <= 0)
                return 0;

            return currentFP >= maxFP * (threshold / 100f) &&
                   currentStamina >= maxStamina * (threshold / 100f)
                ? adjustment
                : 0;
        }

        private int ApplyPercentAdjustment(int value, int percentAdjustment)
        {
            if (percentAdjustment == 0)
                return value;

            var delta = (int)Math.Ceiling(value * (Math.Abs(percentAdjustment) / 100f));
            return percentAdjustment > 0
                ? value + delta
                : value - delta;
        }

        private AbilityType GetCombatImpactDamageAbility(
            uint activator,
            AbilityType combatImpactDamageAbility)
        {
            var ability = combatImpactDamageAbility != AbilityType.Invalid
                ? combatImpactDamageAbility
                : _impactState.GetTrackedAbilityImpact(activator)?.Ability?.CombatImpactDamageAbility ?? AbilityType.Invalid;

            return ability == AbilityType.Invalid
                ? AbilityType.Might
                : ability;
        }

        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.GetStatAdjustment(creature, stat);

        protected virtual int GetAttack(uint creature, AbilityType abilityType, SkillType skillType) => _stat.GetAttack(creature, abilityType, skillType);

        protected virtual int GetAttack(int level, int stat, int bonus) => _stat.GetAttack(level, stat, bonus);

        protected virtual int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType) => _stat.GetDefense(creature, type, abilityType);

        protected virtual NPCStats GetNPCStats(uint npc) => _stat.GetNPCStats(npc);

        protected virtual int GetCurrentFP(uint creature) => _stat.GetCurrentFP(creature);

        protected virtual int GetMaxFP(uint creature) => _stat.GetMaxFP(creature);

        protected virtual int GetCurrentStamina(uint creature) => _stat.GetCurrentStamina(creature);

        protected virtual int GetMaxStamina(uint creature) => _stat.GetMaxStamina(creature);

        protected virtual int GetCombatImpactWeaponDamage(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false, int? triggeringWeaponDamage = null, bool abilityImpact = false) =>
            _queries.GetCombatImpactWeaponDamage(activator, skillType, usesQueuedNaturalWeapon, triggeringWeaponDamage: triggeringWeaponDamage, abilityImpact: abilityImpact);

        protected virtual bool IsWeaponSkillType(SkillType skillType) => CombatRules.IsWeaponSkillType(skillType);

        protected virtual int ApplyDamageDealtModifiers(uint attacker, uint defender, int damage, SkillType skillType, CombatDamageType damageType, bool isAbilityDamage, bool canApplyRandomFlatBonuses, bool isLandedAttack, AbilityDetail ability, out int targetStatusDamagePercentAdjustment, int abilityDamagePercentAdjustment, int lowHPAbilityDamagePercentAdjustment) =>
            _damageModifiers.ApplyDamageDealtModifiers(attacker, defender, damage, skillType, damageType, isAbilityDamage, canApplyRandomFlatBonuses, isLandedAttack, ability, out targetStatusDamagePercentAdjustment, abilityDamagePercentAdjustment, lowHPAbilityDamagePercentAdjustment);

        protected virtual int ApplyIncomingPhysicalToForceConversion(uint attacker, uint defender, CombatDamageType damageType, ref int physicalDamage, int targetStatusDamagePercentAdjustment) =>
            _damageTaken.ApplyIncomingPhysicalToForceConversion(attacker, defender, damageType, ref physicalDamage, targetStatusDamagePercentAdjustment);

        protected virtual int ApplyTypedLeadershipDamageTakenModifier(uint defender, int damage, CombatDamageType damageType) => _damageTaken.ApplyTypedLeadershipDamageTakenModifier(defender, damage, damageType);

        protected virtual int ApplyDamageTakenModifiers(uint defender, int damage, uint attacker, CombatDamageType damageType, int targetStatusDamagePercentAdjustment, bool typedLeadershipReductionAlreadyApplied) =>
            _damageTaken.ApplyDamageTakenModifiers(defender, damage, attacker, damageType, targetStatusDamagePercentAdjustment: targetStatusDamagePercentAdjustment, typedLeadershipReductionAlreadyApplied: typedLeadershipReductionAlreadyApplied);

        protected virtual (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) GetIdleSkillAbilityBonuses(uint activator, SkillType skillType) =>
            _combatActivity.GetIdleSkillAbilityBonuses(activator, skillType);

        protected virtual int GetAbilityDamageBonus(uint creature, SkillType skill) => _abilityBonus.GetAbilityDamageBonus(creature, skill);

        protected virtual int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType) => _abilityBonus.GetAbilityDamageFlatAdjustment(creature, perkType, skillType);

        protected virtual int GetCostlyAbilityDamageBonus(uint activator, AbilityDetail ability, SkillType skillType) => _abilityImpact.GetCostlyAbilityDamageBonus(activator, ability, skillType);

        protected virtual int GetSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType) => _damageModifiers.GetSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType);

        protected virtual int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType) => _damageModifiers.ApplyTargetStatusAttackModifiers(attacker, defender, attack, skillType);

        protected virtual int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense) => _damageModifiers.ApplyStatusSourceDefenseModifiers(attacker, defender, defense);

        protected virtual int ApplyIncomingPhysicalToForceDefenseConversion(uint defender, CombatDamageType damageType, int physicalDefense, Func<int> forceDefenseProvider) =>
            _damageTaken.ApplyIncomingPhysicalToForceDefenseConversion(defender, damageType, physicalDefense, forceDefenseProvider);

        protected virtual int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender) => _abilityBonus.GetAbilityDefenseIgnorePercentAdjustment(creature, perkType, skillType, defender);

        protected virtual int ApplyDefenseIgnore(int defense, int defenseIgnorePercent) => CombatRules.ApplyDefenseIgnore(defense, defenseIgnorePercent);

        protected virtual int GetNearbyStatusTargetAttackAdjustment(uint creature) => _damageModifiers.GetNearbyStatusTargetAttackAdjustment(creature);

        protected virtual int GetLowHPAttackAdjustment(uint attacker) => _damageModifiers.GetLowHPAttackAdjustment(attacker);
    }
}

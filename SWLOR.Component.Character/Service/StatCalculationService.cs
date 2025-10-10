using NWN.Native.API;
using SWLOR.Component.Character.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using System.Buffers.Text;

namespace SWLOR.Component.Character.Service
{
    /// <inheritdoc />
    internal class StatCalculationService: IStatCalculationService
    {
        private readonly ICharacterStatService _characterStatService;
        private readonly IWeaponStatService _weaponStatService;
        private readonly IStatusEffectService _statusEffectService;
        private readonly ISkillService _skillService;

        public StatCalculationService(
            ICharacterStatService characterStatService,
            IWeaponStatService weaponStatService,
            IStatusEffectService statusEffectService,
            ISkillService skillService)
        {
            _characterStatService = characterStatService;
            _weaponStatService = weaponStatService;
            _statusEffectService = statusEffectService;
            _skillService = skillService;
        }
        public int BaseHP => 70;
        public int BaseFP => 10;
        public int BaseSTM => 10;

        /// <inheritdoc />
        public int CalculateLevel(uint creature)
        {
            var level = GetIsPC(creature) 
                ? _skillService.GetSkillRank(creature, SkillType.Armor) 
                : _characterStatService.GetStat(creature, StatType.Level);
            return level;
        }

        /// <inheritdoc />
        public int CalculateMaxHP(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var maxHP = BaseHP +
                        _characterStatService.GetStat(creature, StatType.MaxHP) +
                        effects.GetStat(StatType.MaxHP);

            if (GetIsPC(creature))
            {
                const int MaxPCHP = 10200;
                if (maxHP > MaxPCHP)
                    maxHP = MaxPCHP;
            }
            else
            {
                const int MaxNPCHP = 30000;
                if (maxHP > MaxNPCHP)
                    maxHP = MaxNPCHP;
            }

            if (maxHP < 1)
                maxHP = 1;

            return maxHP;
        }

        /// <inheritdoc />
        public int CalculateMaxFP(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var modifier = _characterStatService.GetStat(creature, StatType.Willpower) * 10;

            var maxFP = BaseFP +
                        modifier +
                        _characterStatService.GetStat(creature, StatType.MaxFP) +
                        effects.GetStat(StatType.MaxFP);

            if (maxFP > 9999)
                maxFP = 9999;
            else if (maxFP < 0)
                maxFP = 0;

            return maxFP;
        }
        /// <inheritdoc />
        public int CalculateMaxSTM(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var modifier = _characterStatService.GetStat(creature, StatType.Perception) * 10;

            var maxSTM = BaseSTM +
                         modifier +
                         _characterStatService.GetStat(creature, StatType.MaxSTM) +
                         effects.GetStat(StatType.MaxSTM);

            if (maxSTM > 999)
                maxSTM = 999;
            else if (maxSTM < 0)
                maxSTM = 0;

            return maxSTM;
        }

        /// <inheritdoc />
        public int CalculateMaxFP(int baseFP, int modifier, int bonus)
        {
            return baseFP + modifier * 10 + bonus;
        }

        /// <inheritdoc />
        public int CalculateMaxSTM(int baseSTM, int modifier, int bonus)
        {
            return baseSTM + modifier * 5 + bonus;
        }

        /// <inheritdoc />
        public int CalculateHPRegen(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var hpRegen = _characterStatService.GetStat(creature, StatType.HPRegen) +
                          effects.GetStat(StatType.HPRegen);
            var vitalityBonus = _characterStatService.GetStat(creature, StatType.Vitality) * 4;
            var totalHPRegen = hpRegen + vitalityBonus;

            if (totalHPRegen > 999)
                totalHPRegen = 999;
            else if (totalHPRegen < 0)
                totalHPRegen = 0;

            return totalHPRegen;
        }

        /// <inheritdoc />
        public int CalculateFPRegen(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var fpRegen = _characterStatService.GetStat(creature, StatType.FPRegen) +
                          effects.GetStat(StatType.FPRegen);
            var vitalityBonus = _characterStatService.GetStat(creature, StatType.Vitality) / 2;
            var totalFPRegen = 1 + fpRegen + vitalityBonus;

            if (totalFPRegen > 999)
                totalFPRegen = 999;
            else if (totalFPRegen < 0)
                totalFPRegen = 0;

            return totalFPRegen;
        }

        /// <inheritdoc />
        public int CalculateSTMRegen(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var stmRegen = _characterStatService.GetStat(creature, StatType.STMRegen) +
                           effects.GetStat(StatType.STMRegen);
            var vitalityBonus = _characterStatService.GetStat(creature, StatType.Vitality) / 2;
            var totalSTMRegen = 1 + stmRegen + vitalityBonus;

            if (totalSTMRegen > 999)
                totalSTMRegen = 999;
            else if (totalSTMRegen < 0)
                totalSTMRegen = 0;

            return totalSTMRegen;
        }

        /// <inheritdoc />
        public float CalculateRecastReduction(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var recastReduction = (_characterStatService.GetStat(creature, StatType.RecastReduction) +
                                   effects.GetStat(StatType.RecastReduction)) * 0.01f;
            if (recastReduction > 0.5f)
                recastReduction = 0.5f;
            else if (recastReduction < 0.0f)
                recastReduction = 0.0f;

            return recastReduction;
        }

        /// <inheritdoc />
        public int CalculateDefense(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, SkillType.Armor);
            var ability = _characterStatService.GetStat(creature, StatType.Vitality);
            var bonus = _characterStatService.GetStat(creature, StatType.Defense) +
                        effects.GetStat(StatType.Defense);

            var defense = CalculateDefense(skill, ability, bonus);

            if (defense > 9999)
                defense = 9999;
            else if (defense < 0)
                defense = 0;

            return defense;
        }

        /// <inheritdoc />
        public int CalculateEvasion(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, SkillType.Armor);
            var ability = _characterStatService.GetStat(creature, StatType.Agility);
            var bonus = _characterStatService.GetStat(creature, StatType.Evasion) +
                        effects.GetStat(StatType.Evasion);

            var evasion = CalculateEvasion(skill, ability, bonus);

            if (evasion > 999)
                evasion = 999;
            else if (evasion < 0)
                evasion = 0;

            return evasion;
        }

        /// <inheritdoc />
        public int CalculateAccuracy(uint creature, AbilityType abilityType, SkillType skillType)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, skillType);
            var ability = _characterStatService.GetStat(creature, abilityType == AbilityType.Might ? StatType.Might :
                                                               abilityType == AbilityType.Perception ? StatType.Perception :
                                                               abilityType == AbilityType.Vitality ? StatType.Vitality :
                                                               abilityType == AbilityType.Agility ? StatType.Agility :
                                                               abilityType == AbilityType.Willpower ? StatType.Willpower :
                                                               StatType.Social);
            var bonus = _characterStatService.GetStat(creature, StatType.Accuracy) + effects.GetStat(StatType.Accuracy);

            var accuracy = CalculateAccuracy(skill, ability, bonus);

            if (accuracy > 999)
                accuracy = 999;
            else if (accuracy < 0)
                accuracy = 0;

            return accuracy;
        }

        /// <inheritdoc />
        public int CalculateAttack(uint creature, AbilityType abilityType, SkillType skillType)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, skillType);
            var ability = _characterStatService.GetStat(creature, abilityType == AbilityType.Might ? StatType.Might :
                                                               abilityType == AbilityType.Perception ? StatType.Perception :
                                                               abilityType == AbilityType.Vitality ? StatType.Vitality :
                                                               abilityType == AbilityType.Agility ? StatType.Agility :
                                                               abilityType == AbilityType.Willpower ? StatType.Willpower :
                                                               StatType.Social);
            var bonus = _characterStatService.GetStat(creature, StatType.Attack) + effects.GetStat(StatType.Attack);

            var attack = CalculateAttack(skill, ability, bonus);

            if (attack > 999)
                attack = 999;
            else if (attack < 0)
                attack = 0;

            return attack;
        }

        /// <inheritdoc />
        public int CalculateForceAttack(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, SkillType.Force);
            var ability = _characterStatService.GetStat(creature, StatType.Willpower);
            var bonus = _characterStatService.GetStat(creature, StatType.ForceAttack) +
                        effects.GetStat(StatType.ForceAttack);

            var forceAttack = 8 + (2 * skill) + ability + bonus;

            if (forceAttack > 999)
                forceAttack = 999;
            else if (forceAttack < 0)
                forceAttack = 0;

            return forceAttack;
        }

        public int CalculateAttribute(uint creature, AbilityType ability)
        {
            switch (ability)
            {
                case AbilityType.Might:
                    return CalculateMight(creature);
                case AbilityType.Perception:
                    return CalculatePerception(creature);
                case AbilityType.Vitality:
                    return CalculateVitality(creature);
                case AbilityType.Agility:
                    return CalculateAgility(creature);
                case AbilityType.Willpower:
                    return CalculateWillpower(creature);
                case AbilityType.Social:
                    return CalculateSocial(creature);
                default:
                    return 0;
            }
        }

        public int CalculateSavingThrow(uint creature, SavingThrowCategoryType type)
        {
            var ability = AbilityType.Invalid;
            switch (type)
            {
                case SavingThrowCategoryType.Fortitude:
                    ability = AbilityType.Might;
                    break;
                case SavingThrowCategoryType.Reflex:
                    ability = AbilityType.Perception;
                    break;
                case SavingThrowCategoryType.Will:
                    ability = AbilityType.Willpower;
                    break;
            }

            var savingThrow = GetAbilityModifier(ability, creature);

            if (savingThrow > 50)
                savingThrow = 50;
            else if (savingThrow < 0)
                savingThrow = 0;

            return savingThrow;
        }

        /// <inheritdoc />
        public int CalculateMight(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var might = _characterStatService.GetStat(creature, StatType.Might) + effects.GetStat(StatType.Might);

            if (might > 100)
                might = 100;

            return might;
        }

        /// <inheritdoc />
        public int CalculatePerception(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var perception = _characterStatService.GetStat(creature, StatType.Perception) + effects.GetStat(StatType.Perception);

            if (perception > 100)
                perception = 100;

            return perception;
        }

        /// <inheritdoc />
        public int CalculateVitality(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var vitality = _characterStatService.GetStat(creature, StatType.Vitality) + effects.GetStat(StatType.Vitality);

            if (vitality > 100)
                vitality = 100;

            return vitality;
        }

        /// <inheritdoc />
        public int CalculateAgility(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var agility = _characterStatService.GetStat(creature, StatType.Agility) + effects.GetStat(StatType.Agility);

            if (agility > 100)
                agility = 100;

            return agility;
        }

        /// <inheritdoc />
        public int CalculateWillpower(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var willpower = _characterStatService.GetStat(creature, StatType.Willpower) + effects.GetStat(StatType.Willpower);

            if (willpower > 100)
                willpower = 100;

            return willpower;
        }

        /// <inheritdoc />
        public int CalculateSocial(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var social = _characterStatService.GetStat(creature, StatType.Social) + effects.GetStat(StatType.Social);

            if (social > 100)
                social = 100;

            return social;
        }

        /// <inheritdoc />
        public int CalculateShieldDeflection(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var shieldDeflection = _characterStatService.GetStat(creature, StatType.ShieldDeflection) + effects.GetStat(StatType.ShieldDeflection);

            if (shieldDeflection > 75)
                shieldDeflection = 75;
            else if (shieldDeflection < 0)
                shieldDeflection = 0;

            return shieldDeflection;
        }

        /// <inheritdoc />
        public int CalculateAttackDeflection(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var attackDeflection = _characterStatService.GetStat(creature, StatType.AttackDeflection) + effects.GetStat(StatType.AttackDeflection);

            if (attackDeflection > 50)
                attackDeflection = 50;
            else if (attackDeflection < 0)
                attackDeflection = 0;

            return attackDeflection;
        }

        /// <inheritdoc />
        public int CalculateCriticalRate(uint creature)
        {
            const int BaseRate = 5;
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var rate = BaseRate +
                       _characterStatService.GetStat(creature, StatType.CriticalRate) +
                       effects.GetStat(StatType.CriticalRate);

            if (rate < 0)
                rate = 0;
            else if (rate > 90)
                rate = 90;

            return rate;
        }

        /// <inheritdoc />
        public int CalculateEnmity(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var enmity = _characterStatService.GetStat(creature, StatType.Enmity) + effects.GetStat(StatType.Enmity);

            if (enmity < -50)
                enmity = -50;
            else if (enmity > 50)
                enmity = 50;

            return enmity;
        }

        /// <inheritdoc />
        public int CalculateHaste(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var haste = _characterStatService.GetStat(creature, StatType.Haste) + effects.GetStat(StatType.Haste);

            if (haste > 50)
                haste = 50;
            else if (haste < 0)
                haste = 0;

            return haste;
        }

        /// <inheritdoc />
        public int CalculateSlow(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var slow = _characterStatService.GetStat(creature, StatType.Slow) + effects.GetStat(StatType.Slow);

            if (slow > 999)
                slow = 999;
            else if (slow < 0)
                slow = 0;

            return slow;
        }

        /// <inheritdoc />
        public int CalculateForceDefense(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            var skill = _skillService.GetSkillRank(creature, SkillType.Armor);
            var ability = _characterStatService.GetStat(creature, StatType.Willpower);
            var bonus = _characterStatService.GetStat(creature, StatType.ForceDefense) + effects.GetStat(StatType.ForceDefense);

            var forceDefense = (int)(8 + (ability * 1.5f) + skill + bonus);

            if (forceDefense > 9999)
                forceDefense = 9999;
            else if (forceDefense < 0)
                forceDefense = 0;

            return forceDefense;
        }

        /// <inheritdoc />
        public int CalculateQueuedDMGBonus(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.QueuedDMGBonus) + effects.GetStat(StatType.QueuedDMGBonus);
        }

        /// <inheritdoc />
        public int CalculateParalysis(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var paralysis = _characterStatService.GetStat(creature, StatType.Paralysis) + effects.GetStat(StatType.Paralysis);
            if (paralysis > 75)
                paralysis = 75;
            else if (paralysis < 0)
                paralysis = 0;

            return paralysis;
        }

        /// <inheritdoc />
        public int CalculateAccuracyModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.AccuracyModifier) + effects.GetStat(StatType.AccuracyModifier);
        }

        /// <inheritdoc />
        public int CalculateRecastReductionModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.RecastReductionModifier) + effects.GetStat(StatType.RecastReductionModifier);
        }

        /// <inheritdoc />
        public int CalculateDefenseBypassModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.DefenseBypassModifier) + effects.GetStat(StatType.DefenseBypassModifier);
        }

        /// <inheritdoc />
        public int CalculateHealingModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.HealingModifier) + effects.GetStat(StatType.HealingModifier);
        }

        /// <inheritdoc />
        public int CalculateFPRestoreOnHit(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.FPRestoreOnHit) + effects.GetStat(StatType.FPRestoreOnHit);
        }

        /// <inheritdoc />
        public int CalculateDefenseModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.DefenseModifier) + effects.GetStat(StatType.DefenseModifier);
        }

        /// <inheritdoc />
        public int CalculateForceDefenseModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.ForceDefenseModifier) + effects.GetStat(StatType.ForceDefenseModifier);
        }

        /// <inheritdoc />
        public int CalculateAttackModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.AttackModifier) + effects.GetStat(StatType.AttackModifier);
        }

        /// <inheritdoc />
        public int CalculateForceAttackModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.ForceAttackModifier) + effects.GetStat(StatType.ForceAttackModifier);
        }

        /// <inheritdoc />
        public int CalculateEvasionModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.EvasionModifier) + effects.GetStat(StatType.EvasionModifier);
        }

        /// <inheritdoc />
        public int CalculateXPModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            return _characterStatService.GetStat(creature, StatType.XPModifier) + effects.GetStat(StatType.XPModifier);
        }

        /// <inheritdoc />
        public int CalculatePoisonResist(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var poisonResist = _characterStatService.GetStat(creature, StatType.PoisonResist) + effects.GetStat(StatType.PoisonResist);

            if (poisonResist > 100)
                poisonResist = 100;
            else if (poisonResist < 0)
                poisonResist = 0;

            return poisonResist;
        }

        /// <inheritdoc />
        public int CalculateFireResist(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var fireResist = _characterStatService.GetStat(creature, StatType.FireResist) + effects.GetStat(StatType.FireResist);

            if (fireResist > 100)
                fireResist = 100;
            else if (fireResist < 0)
                fireResist = 0;

            return fireResist;
        }


        /// <inheritdoc />
        public int CalculateIceResist(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var iceResist = _characterStatService.GetStat(creature, StatType.IceResist) + effects.GetStat(StatType.IceResist);

            if (iceResist > 100)
                iceResist = 100;
            else if (iceResist < 0)
                iceResist = 0;

            return iceResist;
        }


        /// <inheritdoc />
        public int CalculateElectricalResist(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            var electricalResist = _characterStatService.GetStat(creature, StatType.ElectricalResist) + effects.GetStat(StatType.ElectricalResist);

            if (electricalResist > 100)
                electricalResist = 100;
            else if (electricalResist < 0)
                electricalResist = 0;

            return electricalResist;
        }

        /// <inheritdoc />
        public int CalculateResistance(uint creature, CombatDamageType damageType)
        {
            switch (damageType)
            {
                case CombatDamageType.Fire:
                    return CalculateFireResist(creature);
                case CombatDamageType.Poison:
                    return CalculatePoisonResist(creature);
                case CombatDamageType.Electrical:
                    return CalculateElectricalResist(creature);
                case CombatDamageType.Ice:
                    return CalculateIceResist(creature);
                default:
                    return 0;
            }
        }

        private float CalculateAttackSpeedModifier(uint creature)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);

            // Get haste and slow from both stats and status effects
            var haste = _characterStatService.GetStat(creature, StatType.Haste) + effects.GetStat(StatType.Haste);
            if (haste < 0)
                haste = 0;

            var slow = _characterStatService.GetStat(creature, StatType.Slow) + effects.GetStat(StatType.Slow);
            if (slow < 0)
                slow = 0;

            // Haste reduces attack delay (negative modifier), slow increases attack delay (positive modifier)
            // Formula: (slow - haste) * 0.01f
            // This means:
            // - Positive haste values result in negative modifier (faster attacks)
            // - Positive slow values result in positive modifier (slower attacks)
            var modifier = (slow - haste) * 0.01f;

            // Cap only negative modifiers (haste) to prevent extreme values
            // Slow can go beyond +50% to allow for more severe slow effects
            if (modifier < -0.5f)
                modifier = -0.5f;

            return modifier;
        }

        /// <inheritdoc />
        public int CalculateAttackDelay(uint creature)
        {
            var rightHand = _weaponStatService.LoadWeaponStat(GetItemInSlot(InventorySlotType.RightHand, creature));
            var leftHand = _weaponStatService.LoadWeaponStat(GetItemInSlot(InventorySlotType.LeftHand, creature));
            var rightHandDelay = rightHand.Delay;
            var leftHandDelay = leftHand.Delay;
            var delay = rightHandDelay + leftHandDelay;
            var attackSpeedModifier = CalculateAttackSpeedModifier(creature);

            // Convert delay units to milliseconds: 60 delay units = 1 second
            var baseDelay = delay / 60f * 1000;
            
            // Apply attack speed modifier
            // Positive modifier (slow) increases delay, negative modifier (haste) decreases delay
            var finalDelay = (int)(baseDelay * (1.0f + attackSpeedModifier));
            
            return finalDelay;
        }

        /// <inheritdoc />
        public int CalculateControl(uint creature, CraftType craftType)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            StatType statType;

            switch (craftType)
            {
                case CraftType.Smithery:
                    statType = StatType.ControlSmithery;
                    break;
                case CraftType.Engineering:
                    statType = StatType.ControlEngineering;
                    break;
                case CraftType.Fabrication:
                    statType = StatType.ControlFabrication;
                    break;
                case CraftType.Agriculture:
                    statType = StatType.ControlAgriculture;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(craftType), craftType, null);
            }

            var stat = _characterStatService.GetStat(creature, statType);
            var control = stat + effects.GetStat(statType);

            if (control > 999)
                control = 999;
            else if (control < 0)
                control = 0;

            return control;
        }

        /// <inheritdoc />
        public int CalculateCraftsmanship(uint creature, CraftType craftType)
        {
            var effects = _statusEffectService.GetCreatureStatGroup(creature);
            StatType statType;

            switch (craftType)
            {
                case CraftType.Smithery:
                    statType = StatType.CraftsmanshipSmithery;
                    break;
                case CraftType.Engineering:
                    statType = StatType.CraftsmanshipEngineering;
                    break;
                case CraftType.Fabrication:
                    statType = StatType.CraftsmanshipFabrication;
                    break;
                case CraftType.Agriculture:
                    statType = StatType.CraftsmanshipAgriculture;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(craftType), craftType, null);
            }

            var stat = _characterStatService.GetStat(creature, statType);
            var craftsmanship = stat + effects.GetStat(statType);

            if (craftsmanship > 999)
                craftsmanship = 999;
            else if (craftsmanship < 0)
                craftsmanship = 0;

            return craftsmanship;
        }

        /// <inheritdoc />
        public int CalculateAttack(int level, int stat, int bonus)
        {
            return 8 + (2 * level) + stat + bonus;
        }

        /// <inheritdoc />
        public int CalculateDefense(int level, int stat, int bonus)
        {
            return (int)(8 + (stat * 1.5f) + level + bonus);
        }

        /// <inheritdoc />
        public int CalculateAccuracy(int level, int stat, int bonus)
        {
            return stat * 3 + level + bonus;
        }

        /// <inheritdoc />
        public int CalculateEvasion(int level, int stat, int bonus)
        {
            return stat * 3 + level + bonus;
        }


    }
}

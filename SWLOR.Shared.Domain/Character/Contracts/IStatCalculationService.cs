using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Shared.Domain.Character.Contracts
{
    /// <summary>
    /// Service responsible for calculating various character statistics and combat-related values.
    /// All calculations combine base stats, status effects, and other modifiers to provide final values.
    /// </summary>
    public interface IStatCalculationService
    {
        public int BaseHP => 70;
        public int BaseFP => 10;
        public int BaseSTM => 10;

        /// <summary>
        /// Calculates the maximum hit points for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate HP for</param>
        /// <returns>The maximum hit points</returns>
        int CalculateMaxHP(uint creature);

        /// <summary>
        /// Calculates the maximum force points for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate FP for</param>
        /// <returns>The maximum force points</returns>
        int CalculateMaxFP(uint creature);

        /// <summary>
        /// Calculates the maximum stamina for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate STM for</param>
        /// <returns>The maximum stamina</returns>
        int CalculateMaxSTM(uint creature);

        /// <summary>
        /// Calculates the maximum force points using raw inputs.
        /// </summary>
        /// <param name="baseFP">The base FP value</param>
        /// <param name="modifier">The ability modifier</param>
        /// <param name="bonus">Additional FP bonus</param>
        /// <returns>The calculated maximum FP</returns>
        int CalculateMaxFP(int baseFP, int modifier, int bonus);

        /// <summary>
        /// Calculates the maximum stamina using raw inputs.
        /// </summary>
        /// <param name="baseSTM">The base STM value</param>
        /// <param name="modifier">The ability modifier</param>
        /// <param name="bonus">Additional STM bonus</param>
        /// <returns>The calculated maximum STM</returns>
        int CalculateMaxSTM(int baseSTM, int modifier, int bonus);

        /// <summary>
        /// Calculates the hit point regeneration rate per tick.
        /// </summary>
        /// <param name="creature">The creature to calculate HP regen for</param>
        /// <returns>The HP regeneration rate</returns>
        int CalculateHPRegen(uint creature);

        /// <summary>
        /// Calculates the force point regeneration rate per tick.
        /// </summary>
        /// <param name="creature">The creature to calculate FP regen for</param>
        /// <returns>The FP regeneration rate</returns>
        int CalculateFPRegen(uint creature);

        /// <summary>
        /// Calculates the stamina regeneration rate per tick.
        /// </summary>
        /// <param name="creature">The creature to calculate STM regen for</param>
        /// <returns>The STM regeneration rate</returns>
        int CalculateSTMRegen(uint creature);

        /// <summary>
        /// Calculates the recast time reduction percentage (0.0 to 0.5).
        /// </summary>
        /// <param name="creature">The creature to calculate recast reduction for</param>
        /// <returns>The recast reduction percentage (capped at 50%)</returns>
        float CalculateRecastReduction(uint creature);

        /// <summary>
        /// Calculates the defense rating for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate defense for</param>
        /// <returns>The defense rating</returns>
        int CalculateDefense(uint creature);

        /// <summary>
        /// Calculates the evasion rating for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate evasion for</param>
        /// <returns>The evasion rating</returns>
        int CalculateEvasion(uint creature);

        /// <summary>
        /// Calculates the accuracy rating for a specific ability and skill combination.
        /// </summary>
        /// <param name="creature">The creature to calculate accuracy for</param>
        /// <param name="abilityType">The ability type to use in calculation</param>
        /// <param name="skillType">The skill type to use in calculation</param>
        /// <returns>The accuracy rating</returns>
        int CalculateAccuracy(uint creature, AbilityType abilityType, SkillType skillType);

        /// <summary>
        /// Calculates the attack rating for a specific ability and skill combination.
        /// </summary>
        /// <param name="creature">The creature to calculate attack for</param>
        /// <param name="abilityType">The ability type to use in calculation</param>
        /// <param name="skillType">The skill type to use in calculation</param>
        /// <returns>The attack rating</returns>
        int CalculateAttack(uint creature, AbilityType abilityType, SkillType skillType);

        /// <summary>
        /// Calculates the force attack rating for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate force attack for</param>
        /// <returns>The force attack rating</returns>
        int CalculateForceAttack(uint creature);

        /// <summary>
        /// Calculates the Might attribute value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate Might for</param>
        /// <returns>The Might attribute value</returns>
        int CalculateMight(uint creature);

        /// <summary>
        /// Calculates the Perception attribute value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate Perception for</param>
        /// <returns>The Perception attribute value</returns>
        int CalculatePerception(uint creature);

        /// <summary>
        /// Calculates the Vitality attribute value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate Vitality for</param>
        /// <returns>The Vitality attribute value</returns>
        int CalculateVitality(uint creature);

        /// <summary>
        /// Calculates the Agility attribute value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate Agility for</param>
        /// <returns>The Agility attribute value</returns>
        int CalculateAgility(uint creature);

        /// <summary>
        /// Calculates the Willpower attribute value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate Willpower for</param>
        /// <returns>The Willpower attribute value</returns>
        int CalculateWillpower(uint creature);

        /// <summary>
        /// Calculates the Social attribute value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate Social for</param>
        /// <returns>The Social attribute value</returns>
        int CalculateSocial(uint creature);

        /// <summary>
        /// Calculates the specified attribute value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate the attribute for</param>
        /// <param name="attributeType">The type of attribute to calculate</param>
        /// <returns>The attribute value</returns>
        int CalculateAttribute(uint creature, AbilityType attributeType);

        int CalculateSavingThrow(uint creature, SavingThrowCategoryType type);

        /// <summary>
        /// Calculates the shield deflection rating for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate shield deflection for</param>
        /// <returns>The shield deflection rating</returns>
        int CalculateShieldDeflection(uint creature);

        /// <summary>
        /// Calculates the attack deflection rating for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate attack deflection for</param>
        /// <returns>The attack deflection rating</returns>
        int CalculateAttackDeflection(uint creature);

        /// <summary>
        /// Calculates the critical hit rate percentage for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate critical rate for</param>
        /// <returns>The critical hit rate percentage</returns>
        int CalculateCriticalRate(uint creature);

        /// <summary>
        /// Calculates the enmity generation rate for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate enmity for</param>
        /// <returns>The enmity generation rate</returns>
        int CalculateEnmity(uint creature);

        /// <summary>
        /// Calculates the haste effect value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate haste for</param>
        /// <returns>The haste effect value</returns>
        int CalculateHaste(uint creature);

        /// <summary>
        /// Calculates the slow effect value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate slow for</param>
        /// <returns>The slow effect value</returns>
        int CalculateSlow(uint creature);

        /// <summary>
        /// Calculates the force defense rating for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate force defense for</param>
        /// <returns>The force defense rating</returns>
        int CalculateForceDefense(uint creature);

        /// <summary>
        /// Calculates the queued damage bonus for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate queued damage bonus for</param>
        /// <returns>The queued damage bonus</returns>
        int CalculateQueuedDMGBonus(uint creature);

        /// <summary>
        /// Calculates the paralysis resistance for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate paralysis resistance for</param>
        /// <returns>The paralysis resistance value</returns>
        int CalculateParalysis(uint creature);

        /// <summary>
        /// Calculates the accuracy modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate accuracy modifier for</param>
        /// <returns>The accuracy modifier</returns>
        int CalculateAccuracyModifier(uint creature);

        /// <summary>
        /// Calculates the recast reduction modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate recast reduction modifier for</param>
        /// <returns>The recast reduction modifier</returns>
        int CalculateRecastReductionModifier(uint creature);

        /// <summary>
        /// Calculates the defense bypass modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate defense bypass modifier for</param>
        /// <returns>The defense bypass modifier</returns>
        int CalculateDefenseBypassModifier(uint creature);

        /// <summary>
        /// Calculates the healing modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate healing modifier for</param>
        /// <returns>The healing modifier</returns>
        int CalculateHealingModifier(uint creature);

        /// <summary>
        /// Calculates the force point restore on hit value for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate FP restore on hit for</param>
        /// <returns>The FP restore on hit value</returns>
        int CalculateFPRestoreOnHit(uint creature);

        /// <summary>
        /// Calculates the defense modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate defense modifier for</param>
        /// <returns>The defense modifier</returns>
        int CalculateDefenseModifier(uint creature);

        /// <summary>
        /// Calculates the force defense modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate force defense modifier for</param>
        /// <returns>The force defense modifier</returns>
        int CalculateForceDefenseModifier(uint creature);

        /// <summary>
        /// Calculates the attack modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate attack modifier for</param>
        /// <returns>The attack modifier</returns>
        int CalculateAttackModifier(uint creature);

        /// <summary>
        /// Calculates the force attack modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate force attack modifier for</param>
        /// <returns>The force attack modifier</returns>
        int CalculateForceAttackModifier(uint creature);

        /// <summary>
        /// Calculates the evasion modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate evasion modifier for</param>
        /// <returns>The evasion modifier</returns>
        int CalculateEvasionModifier(uint creature);

        /// <summary>
        /// Calculates the experience point modifier for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate XP modifier for</param>
        /// <returns>The XP modifier</returns>
        int CalculateXPModifier(uint creature);

        /// <summary>
        /// Calculates the poison resistance for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate poison resistance for</param>
        /// <returns>The poison resistance value</returns>
        int CalculatePoisonResist(uint creature);

        /// <summary>
        /// Calculates the fire resistance for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate fire resistance for</param>
        /// <returns>The fire resistance value</returns>
        int CalculateFireResist(uint creature);

        /// <summary>
        /// Calculates the ice resistance for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate ice resistance for</param>
        /// <returns>The ice resistance value</returns>
        int CalculateIceResist(uint creature);

        /// <summary>
        /// Calculates the electrical resistance for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate electrical resistance for</param>
        /// <returns>The electrical resistance value</returns>
        int CalculateElectricalResist(uint creature);

        /// <summary>
        /// Calculates the resistance for a specific damage type.
        /// </summary>
        /// <param name="creature">The creature to calculate resistance for</param>
        /// <param name="damageType">The type of damage to calculate resistance for</param>
        /// <returns>The resistance value for the specified damage type</returns>
        int CalculateResistance(uint creature, CombatDamageType damageType);

        /// <summary>
        /// Calculates the level for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate level for</param>
        /// <returns>The creature's level</returns>
        int CalculateLevel(uint creature);

        /// <summary>
        /// Calculates the attack delay in milliseconds for a creature.
        /// </summary>
        /// <param name="creature">The creature to calculate attack delay for</param>
        /// <returns>The attack delay in milliseconds</returns>
        int CalculateAttackDelay(uint creature);

        /// <summary>
        /// Calculates the control rating for a specific crafting type.
        /// Control affects the success rate and quality of crafted items.
        /// </summary>
        /// <param name="creature">The creature to calculate control for</param>
        /// <param name="craftType">The type of crafting to calculate control for (Smithery, Engineering, Fabrication, or Agriculture)</param>
        /// <returns>The control rating for the specified crafting type</returns>
        int CalculateControl(uint creature, CraftType craftType);

        /// <summary>
        /// Calculates the craftsmanship rating for a specific crafting type.
        /// Craftsmanship affects the quality and potential bonuses of crafted items.
        /// </summary>
        /// <param name="creature">The creature to calculate craftsmanship for</param>
        /// <param name="craftType">The type of crafting to calculate craftsmanship for (Smithery, Engineering, Fabrication, or Agriculture)</param>
        /// <returns>The craftsmanship rating for the specified crafting type</returns>
        int CalculateCraftsmanship(uint creature, CraftType craftType);

        float CalculateMovementRate(uint creature);

        /// <summary>
        /// Calculates the attack rating using raw inputs.
        /// </summary>
        /// <param name="level">The level</param>
        /// <param name="stat">The relevant stat value</param>
        /// <param name="bonus">Additional attack bonus</param>
        /// <returns>The calculated attack rating</returns>
        int CalculateAttack(int level, int stat, int bonus);

        /// <summary>
        /// Calculates the accuracy rating using raw inputs.
        /// </summary>
        /// <param name="level">The level</param>
        /// <param name="stat">The relevant stat value</param>
        /// <param name="bonus">Additional accuracy bonus</param>
        /// <returns>The calculated accuracy rating</returns>
        int CalculateAccuracy(int level, int stat, int bonus);

        /// <summary>
        /// Calculates the evasion rating using raw inputs.
        /// </summary>
        /// <param name="level">The level</param>
        /// <param name="stat">The relevant stat value</param>
        /// <param name="bonus">Additional evasion bonus</param>
        /// <returns>The calculated evasion rating</returns>
        int CalculateEvasion(int level, int stat, int bonus);

        /// <summary>
        /// Calculates the defense rating using raw inputs.
        /// </summary>
        /// <param name="stat">The relevant stat value</param>
        /// <param name="level">The level</param>
        /// <param name="bonus">Additional defense bonus</param>
        /// <returns>The calculated defense rating</returns>
        int CalculateDefense(int stat, int level, int bonus);
    }
}

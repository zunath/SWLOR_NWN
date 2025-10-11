using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Shared.Domain.Character.Contracts
{
    /// <summary>
    /// Service for managing character statistics including base stats, modifiers, and NPC stat registration.
    /// </summary>
    public interface ICharacterStatService
    {
        /// <summary>
        /// Registers an NPC to cache its statistics for performance.
        /// </summary>
        /// <param name="npc">The NPC object to register.</param>
        void RegisterNPC(uint npc);

        /// <summary>
        /// Unregisters an NPC, removing it from the stat cache.
        /// </summary>
        /// <param name="npc">The NPC object to unregister.</param>
        void UnregisterNPC(uint npc);

        /// <summary>
        /// Gets the current value of a stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to get the stat for.</param>
        /// <param name="stat">The stat type to retrieve.</param>
        /// <returns>The current stat value.</returns>
        int GetStat(uint creature, StatType stat);

        /// <summary>
        /// Sets the maximum HP stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new maximum HP value.</param>
        void SetMaxHP(uint creature, int value);

        /// <summary>
        /// Sets the maximum FP stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new maximum FP value.</param>
        void SetMaxFP(uint creature, int value);

        /// <summary>
        /// Sets the maximum STM stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new maximum STM value.</param>
        void SetMaxSTM(uint creature, int value);

        /// <summary>
        /// Sets the HP regeneration stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new HP regeneration value.</param>
        void SetHPRegen(uint creature, int value);

        /// <summary>
        /// Sets the FP regeneration stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new FP regeneration value.</param>
        void SetFPRegen(uint creature, int value);

        /// <summary>
        /// Sets the STM regeneration stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new STM regeneration value.</param>
        void SetSTMRegen(uint creature, int value);

        /// <summary>
        /// Sets the defense stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new defense value.</param>
        void SetDefense(uint creature, int value);

        /// <summary>
        /// Sets the attack stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new attack value.</param>
        void SetAttack(uint creature, int value);

        /// <summary>
        /// Sets the recast reduction stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new recast reduction value.</param>
        void SetRecastReduction(uint creature, int value);

        /// <summary>
        /// Sets the evasion stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new evasion value.</param>
        void SetEvasion(uint creature, int value);

        /// <summary>
        /// Sets the accuracy stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new accuracy value.</param>
        void SetAccuracy(uint creature, int value);

        /// <summary>
        /// Sets the force attack stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new force attack value.</param>
        void SetForceAttack(uint creature, int value);

        /// <summary>
        /// Sets the might stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new might value.</param>
        void SetMight(uint creature, int value);

        /// <summary>
        /// Sets the perception stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new perception value.</param>
        void SetPerception(uint creature, int value);

        /// <summary>
        /// Sets the vitality stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new vitality value.</param>
        void SetVitality(uint creature, int value);

        /// <summary>
        /// Sets the agility stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new agility value.</param>
        void SetAgility(uint creature, int value);

        /// <summary>
        /// Sets the willpower stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new willpower value.</param>
        void SetWillpower(uint creature, int value);

        /// <summary>
        /// Sets the social stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new social value.</param>
        void SetSocial(uint creature, int value);

        /// <summary>
        /// Sets the shield deflection stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new shield deflection value.</param>
        void SetShieldDeflection(uint creature, int value);

        /// <summary>
        /// Sets the attack deflection stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new attack deflection value.</param>
        void SetAttackDeflection(uint creature, int value);

        /// <summary>
        /// Sets the critical rate stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new critical rate value.</param>
        void SetCriticalRate(uint creature, int value);

        /// <summary>
        /// Sets the enmity stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new enmity value.</param>
        void SetEnmity(uint creature, int value);

        /// <summary>
        /// Sets the haste stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new haste value.</param>
        void SetHaste(uint creature, int value);

        /// <summary>
        /// Sets the slow stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new slow value.</param>
        void SetSlow(uint creature, int value);

        /// <summary>
        /// Sets the force defense stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new force defense value.</param>
        void SetForceDefense(uint creature, int value);

        /// <summary>
        /// Sets the queued damage bonus stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new queued damage bonus value.</param>
        void SetQueuedDMGBonus(uint creature, int value);

        /// <summary>
        /// Sets the paralysis stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new paralysis value.</param>
        void SetParalysis(uint creature, int value);

        /// <summary>
        /// Sets the accuracy modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new accuracy modifier value.</param>
        void SetAccuracyModifier(uint creature, int value);

        /// <summary>
        /// Sets the recast reduction modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new recast reduction modifier value.</param>
        void SetRecastReductionModifier(uint creature, int value);

        /// <summary>
        /// Sets the defense bypass modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new defense bypass modifier value.</param>
        void SetDefenseBypassModifier(uint creature, int value);

        /// <summary>
        /// Sets the healing modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new healing modifier value.</param>
        void SetHealingModifier(uint creature, int value);

        /// <summary>
        /// Sets the FP restore on hit stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new FP restore on hit value.</param>
        void SetFPRestoreOnHit(uint creature, int value);

        /// <summary>
        /// Sets the defense modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new defense modifier value.</param>
        void SetDefenseModifier(uint creature, int value);

        /// <summary>
        /// Sets the force defense modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new force defense modifier value.</param>
        void SetForceDefenseModifier(uint creature, int value);

        /// <summary>
        /// Sets the attack modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new attack modifier value.</param>
        void SetAttackModifier(uint creature, int value);

        /// <summary>
        /// Sets the force attack modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new force attack modifier value.</param>
        void SetForceAttackModifier(uint creature, int value);

        /// <summary>
        /// Sets the evasion modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new evasion modifier value.</param>
        void SetEvasionModifier(uint creature, int value);

        /// <summary>
        /// Sets the XP modifier stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new XP modifier value.</param>
        void SetXPModifier(uint creature, int value);

        /// <summary>
        /// Sets the poison resistance stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new poison resistance value.</param>
        void SetPoisonResist(uint creature, int value);

        /// <summary>
        /// Sets the level stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new level value.</param>
        void SetLevel(uint creature, int value);

        /// <summary>
        /// Sets the movement speed stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new movement speed value.</param>
        void SetMovementSpeed(uint creature, int value);

        /// <summary>
        /// Sets the control smithery stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new control smithery value.</param>
        void SetControlSmithery(uint creature, int value);

        /// <summary>
        /// Sets the control fabrication stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new control fabrication value.</param>
        void SetControlFabrication(uint creature, int value);

        /// <summary>
        /// Sets the control engineering stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new control engineering value.</param>
        void SetControlEngineering(uint creature, int value);

        /// <summary>
        /// Sets the control agriculture stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new control agriculture value.</param>
        void SetControlAgriculture(uint creature, int value);

        /// <summary>
        /// Sets the craftsmanship smithery stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new craftsmanship smithery value.</param>
        void SetCraftsmanshipSmithery(uint creature, int value);

        /// <summary>
        /// Sets the craftsmanship fabrication stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new craftsmanship fabrication value.</param>
        void SetCraftsmanshipFabrication(uint creature, int value);

        /// <summary>
        /// Sets the craftsmanship engineering stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new craftsmanship engineering value.</param>
        void SetCraftsmanshipEngineering(uint creature, int value);

        /// <summary>
        /// Sets the craftsmanship agriculture stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new craftsmanship agriculture value.</param>
        void SetCraftsmanshipAgriculture(uint creature, int value);

        /// <summary>
        /// Sets the CP smithery stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new CP smithery value.</param>
        void SetCPSmithery(uint creature, int value);

        /// <summary>
        /// Sets the CP fabrication stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new CP fabrication value.</param>
        void SetCPFabrication(uint creature, int value);

        /// <summary>
        /// Sets the CP engineering stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new CP engineering value.</param>
        void SetCPEngineering(uint creature, int value);

        /// <summary>
        /// Sets the CP agriculture stat for a creature.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="value">The new CP agriculture value.</param>
        void SetCPAgriculture(uint creature, int value);

        /// <summary>
        /// Modifies the maximum HP stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current maximum HP.</param>
        void ModifyMaxHP(uint creature, int adjustment);

        /// <summary>
        /// Modifies the maximum FP stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current maximum FP.</param>
        void ModifyMaxFP(uint creature, int adjustment);

        /// <summary>
        /// Modifies the maximum STM stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current maximum STM.</param>
        void ModifyMaxSTM(uint creature, int adjustment);

        /// <summary>
        /// Modifies the HP regeneration stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current HP regeneration.</param>
        void ModifyHPRegen(uint creature, int adjustment);

        /// <summary>
        /// Modifies the FP regeneration stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current FP regeneration.</param>
        void ModifyFPRegen(uint creature, int adjustment);

        /// <summary>
        /// Modifies the STM regeneration stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current STM regeneration.</param>
        void ModifySTMRegen(uint creature, int adjustment);

        /// <summary>
        /// Modifies the defense stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current defense.</param>
        void ModifyDefense(uint creature, int adjustment);

        /// <summary>
        /// Modifies the attack stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current attack.</param>
        void ModifyAttack(uint creature, int adjustment);

        /// <summary>
        /// Modifies the recast reduction stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current recast reduction.</param>
        void ModifyRecastReduction(uint creature, int adjustment);

        /// <summary>
        /// Modifies the evasion stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current evasion.</param>
        void ModifyEvasion(uint creature, int adjustment);

        /// <summary>
        /// Modifies the accuracy stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current accuracy.</param>
        void ModifyAccuracy(uint creature, int adjustment);

        /// <summary>
        /// Modifies the force attack stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current force attack.</param>
        void ModifyForceAttack(uint creature, int adjustment);

        /// <summary>
        /// Modifies the might stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current might.</param>
        void ModifyMight(uint creature, int adjustment);

        /// <summary>
        /// Modifies the perception stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current perception.</param>
        void ModifyPerception(uint creature, int adjustment);

        /// <summary>
        /// Modifies the vitality stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current vitality.</param>
        void ModifyVitality(uint creature, int adjustment);

        /// <summary>
        /// Modifies the agility stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current agility.</param>
        void ModifyAgility(uint creature, int adjustment);

        /// <summary>
        /// Modifies the willpower stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current willpower.</param>
        void ModifyWillpower(uint creature, int adjustment);

        /// <summary>
        /// Modifies the social stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current social.</param>
        void ModifySocial(uint creature, int adjustment);

        /// <summary>
        /// Modifies the shield deflection stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current shield deflection.</param>
        void ModifyShieldDeflection(uint creature, int adjustment);

        /// <summary>
        /// Modifies the attack deflection stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current attack deflection.</param>
        void ModifyAttackDeflection(uint creature, int adjustment);

        /// <summary>
        /// Modifies the critical rate stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current critical rate.</param>
        void ModifyCriticalRate(uint creature, int adjustment);

        /// <summary>
        /// Modifies the enmity stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current enmity.</param>
        void ModifyEnmity(uint creature, int adjustment);

        /// <summary>
        /// Modifies the haste stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current haste.</param>
        void ModifyHaste(uint creature, int adjustment);

        /// <summary>
        /// Modifies the slow stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current slow.</param>
        void ModifySlow(uint creature, int adjustment);

        /// <summary>
        /// Modifies the force defense stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current force defense.</param>
        void ModifyForceDefense(uint creature, int adjustment);

        /// <summary>
        /// Modifies the queued damage bonus stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current queued damage bonus.</param>
        void ModifyQueuedDMGBonus(uint creature, int adjustment);

        /// <summary>
        /// Modifies the paralysis stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current paralysis.</param>
        void ModifyParalysis(uint creature, int adjustment);

        /// <summary>
        /// Modifies the accuracy modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current accuracy modifier.</param>
        void ModifyAccuracyModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the recast reduction modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current recast reduction modifier.</param>
        void ModifyRecastReductionModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the defense bypass modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current defense bypass modifier.</param>
        void ModifyDefenseBypassModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the healing modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current healing modifier.</param>
        void ModifyHealingModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the FP restore on hit stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current FP restore on hit.</param>
        void ModifyFPRestoreOnHit(uint creature, int adjustment);

        /// <summary>
        /// Modifies the defense modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current defense modifier.</param>
        void ModifyDefenseModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the force defense modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current force defense modifier.</param>
        void ModifyForceDefenseModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the attack modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current attack modifier.</param>
        void ModifyAttackModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the force attack modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current force attack modifier.</param>
        void ModifyForceAttackModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the evasion modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current evasion modifier.</param>
        void ModifyEvasionModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the XP modifier stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current XP modifier.</param>
        void ModifyXPModifier(uint creature, int adjustment);

        /// <summary>
        /// Modifies the poison resistance stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current poison resistance.</param>
        void ModifyPoisonResist(uint creature, int adjustment);

        /// <summary>
        /// Modifies the level stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current level.</param>
        void ModifyLevel(uint creature, int adjustment);

        /// <summary>
        /// Modifies the movement speed stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current movement speed.</param>
        void ModifyMovementSpeed(uint creature, int adjustment);

        /// <summary>
        /// Modifies the control smithery stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current control smithery.</param>
        void ModifyControlSmithery(uint creature, int adjustment);

        /// <summary>
        /// Modifies the control fabrication stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current control fabrication.</param>
        void ModifyControlFabrication(uint creature, int adjustment);

        /// <summary>
        /// Modifies the control engineering stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current control engineering.</param>
        void ModifyControlEngineering(uint creature, int adjustment);

        /// <summary>
        /// Modifies the control agriculture stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current control agriculture.</param>
        void ModifyControlAgriculture(uint creature, int adjustment);

        /// <summary>
        /// Modifies the craftsmanship smithery stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current craftsmanship smithery.</param>
        void ModifyCraftsmanshipSmithery(uint creature, int adjustment);

        /// <summary>
        /// Modifies the craftsmanship fabrication stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current craftsmanship fabrication.</param>
        void ModifyCraftsmanshipFabrication(uint creature, int adjustment);

        /// <summary>
        /// Modifies the craftsmanship engineering stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current craftsmanship engineering.</param>
        void ModifyCraftsmanshipEngineering(uint creature, int adjustment);

        /// <summary>
        /// Modifies the craftsmanship agriculture stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current craftsmanship agriculture.</param>
        void ModifyCraftsmanshipAgriculture(uint creature, int adjustment);

        /// <summary>
        /// Modifies the CP smithery stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current CP smithery.</param>
        void ModifyCPSmithery(uint creature, int adjustment);

        /// <summary>
        /// Modifies the CP fabrication stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current CP fabrication.</param>
        void ModifyCPFabrication(uint creature, int adjustment);

        /// <summary>
        /// Modifies the CP engineering stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current CP engineering.</param>
        void ModifyCPEngineering(uint creature, int adjustment);

        /// <summary>
        /// Modifies the CP agriculture stat for a creature by adding the adjustment value.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="adjustment">The value to add to the current CP agriculture.</param>
        void ModifyCPAgriculture(uint creature, int adjustment);
    }
}

using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The player records fed by worn gear and the Combat Readiness equipment: part 3 of the stat split (until then
    /// members of <c>IStatService</c>).
    /// </summary>
    public interface IStatRecordService
    {
        /// <summary>
        /// Sets every player record summed from worn gear (see <see cref="EquipmentRecordStats"/>) to what the worn gear
        /// grants. Like max HP, these records are kept by adding and removing each equipment change, so one missed,
        /// failed or repeated change leaves them off for good.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <param name="stats">The records to rebuild</param>
        /// <returns>The records that were corrected</returns>
        EquipmentRecordStats RefreshPlayerEquipmentStats(uint player, EquipmentRecordStats stats = EquipmentRecordStats.All);

        /// <summary>
        /// Sums the record stats the worn gear grants, counting the same properties equipping does.
        /// </summary>
        EquipmentStatTotals GetEquippedStatTotals(uint creature);

        /// <summary>
        /// Writes the gear totals into the selected records: Defense and Resistance for every type, maximum FP and STM
        /// above their bases, Control, Craftsmanship and CP Bonus for every crafting skill, and each summed number
        /// such as Evasion, Attack or Stealth. Pools are left alone.
        /// </summary>
        /// <returns>The records that changed</returns>
        EquipmentRecordStats ApplyEquipmentStatTotals(Player entity, EquipmentStatTotals totals, EquipmentRecordStats stats);

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player);

        /// <summary>
        /// Modifies a player's maximum STM by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player);

        /// <summary>
        /// Modifies the combat readiness of a player by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustCombatReadiness(Player entity, int adjustBy);

        /// <summary>
        /// Reconciles the equipment cache after login or a completed equipment change.
        /// Keep this outside ability calculations: one scan can serve every target.
        /// </summary>
        void RefreshCombatReadinessEquipment(uint creature);

        int GetEquippedCombatReadiness(uint creature);

        /// <summary>
        /// Modifies a player's HP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustHPRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's FP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustFPRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's STM Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustSTMRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's defense toward a particular damage type by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="type">The type of damage</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustDefense(Player entity, CombatDamageType type, int adjustBy);

        /// <summary>
        /// Modifies a player's evasion by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustEvasion(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's stealth by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustStealth(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's detection by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustDetection(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's trap bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustTrapBonus(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's trap disarm by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustTrapDisarm(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's poison bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPoisonBonus(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's lockpicking by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustLockpicking(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's attack by a certain amount. Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustAttack(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's accuracy granted by non-weapon equipment.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustAccuracy(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's force attack by a certain amount. Force Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustForceAttack(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's control by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustControl(Player entity, SkillType skillType, int adjustBy);

        /// <summary>
        /// Modifies a player's craftsmanship by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy);

        /// <summary>
        /// Modifies a player's CP bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy);
    }
}

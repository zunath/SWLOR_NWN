using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using MovementRate = SWLOR.NWN.API.NWScript.Enum.MovementRate;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The player records fed by worn gear, the Combat Readiness equipment and the writers that add and remove a player's gear
    /// stats: part 3 of the stat split (<c>Readmes/MaintainabilityRefactorStatSplit.md</c>), moved from <c>StatService</c>
    /// unchanged except that the stat read and the pools it called on its own instance are <see cref="IStatService"/> calls.
    /// Static callers reach it through <c>GameServices.StatRecord</c>. Its call into the defense damage type list sits behind a
    /// one-line <c>protected virtual</c> method whose body is the original call.
    /// </summary>
    public class StatRecordService : IStatRecordService
    {
        public const int BaseFP = 10;
        public const int BaseSTM = 10;
        private readonly IDBService _db;
        private readonly IStatService _stat;

        public StatRecordService(
            IDBService db,
            IStatService stat)
        {
            _db = db;
            _stat = stat;
        }

        /// <summary>
        /// Sets every player record summed from worn gear (see <see cref="EquipmentRecordStats"/>) to what the worn gear
        /// grants. Like max HP, these records are kept by adding and removing each equipment change, so one missed,
        /// failed or repeated change leaves them off for good.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <param name="stats">The records to rebuild</param>
        /// <returns>The records that were corrected</returns>
        public EquipmentRecordStats RefreshPlayerEquipmentStats(uint player, EquipmentRecordStats stats = EquipmentRecordStats.All)
        {
            if (stats == EquipmentRecordStats.None ||
                !GetIsObjectValid(player) || !GetIsPC(player) ||
                GetIsDM(player) || GetIsDMPossessed(player))
                return EquipmentRecordStats.None;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return EquipmentRecordStats.None;

            var corrected = ApplyEquipmentStatTotals(dbPlayer, GetEquippedStatTotals(player), stats);
            if (corrected == EquipmentRecordStats.None)
                return corrected;

            // A lower maximum caps the current pool, as removing gear does.
            if (corrected.HasFlag(EquipmentRecordStats.FP))
                dbPlayer.FP = Math.Clamp(dbPlayer.FP, 0, Math.Max(0, _stat.GetMaxFP(player, dbPlayer)));
            if (corrected.HasFlag(EquipmentRecordStats.Stamina))
                dbPlayer.Stamina = Math.Clamp(dbPlayer.Stamina, 0, Math.Max(0, _stat.GetMaxStamina(player, dbPlayer)));
            _db.Set(dbPlayer);
            return corrected;
        }

        /// <summary>
        /// Sums the record stats the worn gear grants, counting the same properties equipping does.
        /// </summary>
        public EquipmentStatTotals GetEquippedStatTotals(uint creature)
        {
            var totals = new EquipmentStatTotals();
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item) || GetBaseItemType(item) == BaseItem.CompanionPistol)
                    continue;

                var baseItem = GetBaseItemType(item);
                var copies = PowerGloveStatCopies.For(baseItem);
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    for (var copy = 0; copy < copies; copy++)
                        totals.Add(GetItemPropertyType(ip), GetItemPropertySubType(ip), GetItemPropertyCostTableValue(ip), baseItem);
                }
            }

            return totals;
        }

        /// <summary>
        /// Writes the gear totals into the selected records: Defense and Resistance for every type, maximum FP and STM
        /// above their bases, Control, Craftsmanship and CP Bonus for every crafting skill, and each summed number
        /// such as Evasion, Attack or Stealth. Pools are left alone.
        /// </summary>
        /// <returns>The records that changed</returns>
        public EquipmentRecordStats ApplyEquipmentStatTotals(Player entity, EquipmentStatTotals totals, EquipmentRecordStats stats)
        {
            var corrected = EquipmentRecordStats.None;

            if (stats.HasFlag(EquipmentRecordStats.Defense))
            {
                // Legacy records can lack the map, and login can run before another handler fills it.
                entity.Defenses ??= new Dictionary<CombatDamageType, int>();
                foreach (var type in GetDefenseDamageTypes())
                {
                    var amount = totals.GetDefense(type);
                    if (entity.Defenses.TryGetValue(type, out var current) && current == amount)
                        continue;

                    entity.Defenses[type] = amount;
                    corrected |= EquipmentRecordStats.Defense;
                }
            }

            if (stats.HasFlag(EquipmentRecordStats.Evasion) && entity.Evasion != totals.Evasion)
            {
                entity.Evasion = totals.Evasion;
                corrected |= EquipmentRecordStats.Evasion;
            }

            if (stats.HasFlag(EquipmentRecordStats.FP) && entity.MaxFP != BaseFP + totals.FP)
            {
                entity.MaxFP = BaseFP + totals.FP;
                corrected |= EquipmentRecordStats.FP;
            }

            if (stats.HasFlag(EquipmentRecordStats.Stamina) && entity.MaxStamina != BaseSTM + totals.Stamina)
            {
                entity.MaxStamina = BaseSTM + totals.Stamina;
                corrected |= EquipmentRecordStats.Stamina;
            }

            if (stats.HasFlag(EquipmentRecordStats.Control))
            {
                entity.Control ??= new Dictionary<SkillType, int>();
                if (ApplyCraftStatTotals(entity.Control, totals.GetControl))
                    corrected |= EquipmentRecordStats.Control;
            }

            if (stats.HasFlag(EquipmentRecordStats.Craftsmanship))
            {
                entity.Craftsmanship ??= new Dictionary<SkillType, int>();
                if (ApplyCraftStatTotals(entity.Craftsmanship, totals.GetCraftsmanship))
                    corrected |= EquipmentRecordStats.Craftsmanship;
            }

            if (stats.HasFlag(EquipmentRecordStats.CPBonus))
            {
                entity.CPBonus ??= new Dictionary<SkillType, int>();
                if (ApplyCraftStatTotals(entity.CPBonus, totals.GetCPBonus))
                    corrected |= EquipmentRecordStats.CPBonus;
            }

            if (stats.HasFlag(EquipmentRecordStats.Resistance))
            {
                entity.Resistances ??= new Dictionary<ResistanceType, int>();
                foreach (var type in DamageTableDefaults.EnumerateResistanceTypes())
                {
                    var amount = totals.GetResistance(type);
                    if ((entity.Resistances.TryGetValue(type, out var current) ? current : 0) == amount)
                        continue;

                    entity.Resistances[type] = amount;
                    corrected |= EquipmentRecordStats.Resistance;
                }
            }

            foreach (var (record, get, set) in _summedEquipmentRecords)
            {
                var amount = totals.GetTotal(record);
                if (!stats.HasFlag(record) || get(entity) == amount)
                    continue;

                set(entity, amount);
                corrected |= record;
            }

            return corrected;
        }

        /// <summary>
        /// The records that hold one number summed from worn gear, with how to read and write each.
        /// </summary>
        private readonly (EquipmentRecordStats Record, Func<Player, int> Get, Action<Player, int> Set)[] _summedEquipmentRecords =
        {
            (EquipmentRecordStats.FPRegen, entity => entity.FPRegen, (entity, amount) => entity.FPRegen = amount),
            (EquipmentRecordStats.STMRegen, entity => entity.STMRegen, (entity, amount) => entity.STMRegen = amount),
            (EquipmentRecordStats.Attack, entity => entity.Attack, (entity, amount) => entity.Attack = amount),
            (EquipmentRecordStats.Accuracy, entity => entity.Accuracy, (entity, amount) => entity.Accuracy = amount),
            (EquipmentRecordStats.ForceAttack, entity => entity.ForceAttack, (entity, amount) => entity.ForceAttack = amount),
            (EquipmentRecordStats.Stealth, entity => entity.Stealth, (entity, amount) => entity.Stealth = amount),
            (EquipmentRecordStats.Detection, entity => entity.Detection, (entity, amount) => entity.Detection = amount),
            (EquipmentRecordStats.TrapBonus, entity => entity.TrapBonus, (entity, amount) => entity.TrapBonus = amount),
            (EquipmentRecordStats.TrapDisarm, entity => entity.TrapDisarm, (entity, amount) => entity.TrapDisarm = amount),
            (EquipmentRecordStats.PoisonBonus, entity => entity.PoisonBonus, (entity, amount) => entity.PoisonBonus = amount),
            (EquipmentRecordStats.Lockpicking, entity => entity.Lockpicking, (entity, amount) => entity.Lockpicking = amount),
        };

        /// <summary>
        /// Sets each crafting skill's record to what the gear grants. A missing entry already reads as zero.
        /// </summary>
        /// <returns>True if any skill changed</returns>
        private bool ApplyCraftStatTotals(Dictionary<SkillType, int> record, Func<SkillType, int> getTotal)
        {
            var changed = false;
            foreach (var skill in EquipmentStatTotals.CraftSkills)
            {
                var amount = getTotal(skill);
                if ((record.TryGetValue(skill, out var current) ? current : 0) == amount)
                    continue;

                record[skill] = amount;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player)
        {
            // Note: It's possible for Max FP to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxFP += adjustBy;

            // Note - must call GetMaxFP here to account for ability-based increase to FP cap.
            if (entity.FP > _stat.GetMaxFP(player))
                entity.FP = _stat.GetMaxFP(player);

            // Current FP, however, should never drop below zero.
            if (entity.FP < 0)
                entity.FP = 0;
        }

        /// <summary>
        /// Modifies a player's maximum STM by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player)
        {
            // Note: It's possible for Max STM to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxStamina += adjustBy;

            // Note - must call GetMaxFP here to account for ability-based increase to STM cap.
            if (entity.Stamina > _stat.GetMaxStamina(player))
                entity.Stamina = _stat.GetMaxStamina(player);

            // Current STM, however, should never drop below zero.
            if (entity.Stamina < 0)
                entity.Stamina = 0;
        }

        /// <summary>
        /// Modifies the combat readiness of a player by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustCombatReadiness(Player entity, int adjustBy)
        {
            entity.CombatReadiness += adjustBy;
        }

        /// <summary>
        /// Reconciles the equipment cache after login or a completed equipment change.
        /// Keep this outside ability calculations: one scan can serve every target.
        /// </summary>
        public void RefreshCombatReadinessEquipment(uint creature)
        {
            if (!GetIsObjectValid(creature) || !GetIsPC(creature) ||
                GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var amount = GetEquippedCombatReadiness(creature);
            if (dbPlayer.CombatReadiness == amount)
                return;

            // Store the uncapped equipment contribution. Perks and food are stat
            // adjustments, and the combined cap belongs to the read calculation.
            dbPlayer.CombatReadiness = amount;
            _db.Set(dbPlayer);
        }

        public int GetEquippedCombatReadiness(uint creature)
        {
            var amount = 0;
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item) || GetBaseItemType(item) == BaseItem.CompanionPistol)
                    continue;

                var copies = PowerGloveStatCopies.For(GetBaseItemType(item));
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    if (GetItemPropertyType(ip) == ItemPropertyType.CombatReadiness)
                        amount += GetItemPropertyCostTableValue(ip) * copies;
                }
            }

            return amount;
        }

        /// <summary>
        /// Modifies a player's HP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustHPRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for HP Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.HPRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's FP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustFPRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for FP Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.FPRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's STM Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustSTMRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for STM Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.STMRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's defense toward a particular damage type by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="type">The type of damage</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustDefense(Player entity, CombatDamageType type, int adjustBy)
        {
            if (!type.IsDefenseDamageType())
                return;

            if (!entity.Defenses.ContainsKey(type))
                entity.Defenses[type] = 0;

            entity.Defenses[type] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's evasion by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustEvasion(Player entity, int adjustBy)
        {
            entity.Evasion += adjustBy;
        }

        /// <summary>
        /// Modifies a player's stealth by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustStealth(Player entity, int adjustBy)
        {
            entity.Stealth += adjustBy;
        }

        /// <summary>
        /// Modifies a player's detection by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustDetection(Player entity, int adjustBy)
        {
            entity.Detection += adjustBy;
        }

        /// <summary>
        /// Modifies a player's trap bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustTrapBonus(Player entity, int adjustBy)
        {
            entity.TrapBonus += adjustBy;
        }

        /// <summary>
        /// Modifies a player's trap disarm by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustTrapDisarm(Player entity, int adjustBy)
        {
            entity.TrapDisarm += adjustBy;
        }

        /// <summary>
        /// Modifies a player's poison bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPoisonBonus(Player entity, int adjustBy)
        {
            entity.PoisonBonus += adjustBy;
        }

        /// <summary>
        /// Modifies a player's lockpicking by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustLockpicking(Player entity, int adjustBy)
        {
            entity.Lockpicking += adjustBy;
        }

        /// <summary>
        /// Modifies a player's attack by a certain amount. Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustAttack(Player entity, int adjustBy)
        {
            entity.Attack += adjustBy;
        }

        /// <summary>
        /// Modifies a player's accuracy granted by non-weapon equipment.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustAccuracy(Player entity, int adjustBy)
        {
            entity.Accuracy += adjustBy;
        }

        /// <summary>
        /// Modifies a player's force attack by a certain amount. Force Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustForceAttack(Player entity, int adjustBy)
        {
            entity.ForceAttack += adjustBy;
        }

        /// <summary>
        /// Modifies a player's control by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustControl(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.Control.ContainsKey(skillType))
                entity.Control[skillType] = 0;

            entity.Control[skillType] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's craftsmanship by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.Craftsmanship.ContainsKey(skillType))
                entity.Craftsmanship[skillType] = 0;

            entity.Craftsmanship[skillType] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's CP bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.CPBonus.ContainsKey(skillType))
                entity.CPBonus[skillType] = 0;

            entity.CPBonus[skillType] += adjustBy;
        }

        // The calls that are not injected, one line each, so a test can answer them. The bodies are the original calls.

        protected virtual ImmutableArray<CombatDamageType> GetDefenseDamageTypes() => DamageTableDefaults.GetDefenseDamageTypes();
    }
}

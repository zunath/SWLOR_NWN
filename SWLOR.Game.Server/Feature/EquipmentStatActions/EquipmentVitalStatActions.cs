using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.EquipmentStatActions
{
    /// <summary>
    /// Equipment actions for hit points, force points, stamina and combat readiness.
    /// </summary>
    public static class EquipmentVitalStatActions
    {
        /// <summary>
        /// Applies or removes an HP bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change.</param>
        /// <param name="isAdding">If true, we're adding the HP, if false we're removing it</param>
        public static void ApplyHPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (isAdding)
                {
                    CreatureStat.AdjustPlayerMaxHP(dbPlayer, creature, amount);
                }
                else
                {
                    CreatureStat.AdjustPlayerMaxHP(dbPlayer, creature, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);

                // Without an NPCHP budget on its skin, the creature's maximum HP comes from its blueprint, so the
                // bonus adjusts that. Starting from zero would leave it with only the item's bonus. The budget stays
                // uncapped so removing gear restores it exactly; SetNPCMaxHitPoints applies the cap.
                var maxHP = Stat.GetNPCSkinHPBudget(skin);
                if (maxHP <= 0)
                    maxHP = GetMaxHitPoints(creature);

                if (isAdding)
                {
                    maxHP += amount;
                }
                else
                {
                    maxHP -= amount;
                }

                var newIP = ItemPropertyCustom(ItemPropertyType.NPCHP, -1, maxHP);
                BiowareXP2.IPSafeAddItemProperty(skin, newIP, 0f, AddItemPropertyPolicy.ReplaceExisting, true, true);

                if (maxHP > 0)
                {
                    CreatureStat.SetNPCMaxHitPoints(creature, maxHP);
                }

                // Base HP only: the native getter includes temporary HP, which must not heal the NPC.
                if (ObjectPlugin.GetCurrentHitPoints(creature) > GetMaxHitPoints(creature))
                {
                    SetCurrentHitPoints(creature, GetMaxHitPoints(creature));
                }
            }
        }

        /// <summary>
        /// Applies or removes an FP bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP, if false we're removing it</param>
        public static void ApplyFPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (isAdding)
                {
                    StatRecord.AdjustPlayerMaxFP(dbPlayer, amount, creature);
                }
                else
                {
                    StatRecord.AdjustPlayerMaxFP(dbPlayer, -amount, creature);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.FP, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes an FP Regen bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP Regen, if false we're removing it</param>
        public static void ApplyFPRegenBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (isAdding)
                {
                    StatRecord.AdjustFPRegen(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustFPRegen(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.FPRegen, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes a STM bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP, if false we're removing it</param>
        public static void ApplySTMBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (isAdding)
                {
                    StatRecord.AdjustPlayerMaxSTM(dbPlayer, amount, creature);
                }
                else
                {
                    StatRecord.AdjustPlayerMaxSTM(dbPlayer, -amount, creature);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.Stamina, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes a STM Regen bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP Regen, if false we're removing it</param>
        public static void ApplySTMRegenBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (isAdding)
                {
                    StatRecord.AdjustSTMRegen(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustSTMRegen(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.STMRegen, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes a combat readiness bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the bonus, if false we're removing it.</param>
        public static void ApplyCombatReadiness(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            // Player equipment totals are refreshed after the native operation.
            // Updating them during validation can persist a change that never occurs.
            if (GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.CombatReadiness, amount, isAdding);
        }
    }
}

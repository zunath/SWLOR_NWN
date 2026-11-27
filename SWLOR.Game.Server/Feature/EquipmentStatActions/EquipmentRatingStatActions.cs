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
    /// Equipment actions for attack, accuracy, force attack, defense, resistance and evasion.
    /// </summary>
    public static class EquipmentRatingStatActions
    {
        /// <summary>
        /// Applies or removes attack bonuses. This affects the end result of the damage calculation (not to be confused with Accuracy, which affects the chance to hit).
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the attack, if false we're removing it.</param>
        public static void ApplyAttack(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustAttack(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustAttack(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.Attack, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes accuracy granted by non-weapon equipment, which applies to every attack.
        /// Accuracy on a weapon only applies to that weapon's attacks, so it is read from the weapon when attacking instead.
        /// Creature skins are skipped because their accuracy is read directly as the NPC's total.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the accuracy, if false we're removing it.</param>
        public static void ApplyAccuracy(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var baseItemType = GetBaseItemType(item);
            if (Item.IsAttackWeaponType(baseItemType) || baseItemType == BaseItem.CreatureItem)
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (isAdding)
                {
                    StatRecord.AdjustAccuracy(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustAccuracy(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.Accuracy, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes force attack bonuses. This affects the end result of the damage calculation (not to be confused with Accuracy, which affects the chance to hit).
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the force attack, if false we're removing it.</param>
        public static void ApplyForceAttack(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustForceAttack(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustForceAttack(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.ForceAttack, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes defense toward a particular damage type on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the defense, if false we're removing it.</param>
        public static void ApplyDefense(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var damageType = (CombatDamageType)GetItemPropertySubType(ip);
            if (!damageType.IsDefenseDamageType())
                return;

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (isAdding)
                {
                    StatRecord.AdjustDefense(dbPlayer, damageType, amount);
                }
                else
                {
                    StatRecord.AdjustDefense(dbPlayer, damageType, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);
                var value = 0;
                for (var defenseIP = GetFirstItemProperty(skin); GetIsItemPropertyValid(defenseIP); defenseIP = GetNextItemProperty(skin))
                {
                    if (GetItemPropertyType(defenseIP) == ItemPropertyType.Defense)
                    {
                        var subType = (CombatDamageType)GetItemPropertySubType(defenseIP);

                        if (subType == damageType)
                        {
                            value += GetItemPropertyCostTableValue(defenseIP);
                        }
                    }
                }

                if (isAdding)
                {
                    value += amount;
                }
                else
                {
                    value -= amount;
                }

                if (value <= 0)
                {
                    BiowareXP2.IPRemoveMatchingItemProperties(skin, ItemPropertyType.Defense, DurationType.Invalid, (int)damageType);
                }
                else
                {
                    var newIP = ItemPropertyCustom(ItemPropertyType.Defense, (int)damageType, value);
                    BiowareXP2.IPSafeAddItemProperty(skin, newIP, 0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
                }
            }
        }

        /// <summary>
        /// Applies or removes resistance toward a particular elemental or status type on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the resistance, if false we're removing it.</param>
        public static void ApplyResistance(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = Resistance.DecodeItemPropertyCostTableValue(GetItemPropertyCostTableValue(ip));
            var resistanceType = (ResistanceType)GetItemPropertySubType(ip);
            if (!Resistance.IsValidResistanceType(resistanceType))
                return;

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (isAdding)
                {
                    Resistance.AdjustResistance(dbPlayer, resistanceType, amount);
                }
                else
                {
                    Resistance.AdjustResistance(dbPlayer, resistanceType, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);
                var value = 0;
                for (var resistanceIP = GetFirstItemProperty(skin); GetIsItemPropertyValid(resistanceIP); resistanceIP = GetNextItemProperty(skin))
                {
                    if (GetItemPropertyType(resistanceIP) == ItemPropertyType.Resistance)
                    {
                        var subType = (ResistanceType)GetItemPropertySubType(resistanceIP);

                        if (subType == resistanceType)
                        {
                            value += Resistance.DecodeItemPropertyCostTableValue(GetItemPropertyCostTableValue(resistanceIP));
                        }
                    }
                }

                if (isAdding)
                {
                    value += amount;
                }
                else
                {
                    value -= amount;
                }

                if (value == 0)
                {
                    BiowareXP2.IPRemoveMatchingItemProperties(skin, ItemPropertyType.Resistance, DurationType.Invalid, (int)resistanceType);
                }
                else
                {
                    var newIP = ItemPropertyCustom(
                        ItemPropertyType.Resistance,
                        (int)resistanceType,
                        Resistance.EncodeItemPropertyCostTableValue(value));
                    BiowareXP2.IPSafeAddItemProperty(skin, newIP, 0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
                }
            }
        }

        /// <summary>
        /// Applies or removes evasion on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the evasion, if false we're removing it.</param>
        public static void ApplyEvasion(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustEvasion(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustEvasion(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.Evasion, amount, isAdding);
            }
        }
    }
}

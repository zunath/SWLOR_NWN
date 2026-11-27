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
    /// Equipment actions for stealth, detection, trap bonus, disarm, poison and lockpicking.
    /// </summary>
    public static class EquipmentFieldStatActions
    {
        /// <summary>
        /// Applies or removes stealth on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the stealth, if false we're removing it.</param>
        public static void ApplyStealth(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustStealth(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustStealth(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.Stealth, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes detection on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the detection, if false we're removing it.</param>
        public static void ApplyDetection(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustDetection(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustDetection(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.Detection, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes trap bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the trap bonus, if false we're removing it.</param>
        public static void ApplyTrapBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustTrapBonus(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustTrapBonus(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.TrapBonus, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes trap disarm on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the trap disarm, if false we're removing it.</param>
        public static void ApplyTrapDisarm(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustTrapDisarm(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustTrapDisarm(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.Disarm, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes poison bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the poison bonus, if false we're removing it.</param>
        public static void ApplyPoisonBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustPoisonBonus(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustPoisonBonus(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.PoisonBonus, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes lockpicking on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the lockpicking, if false we're removing it.</param>
        public static void ApplyLockpicking(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatRecord.AdjustLockpicking(dbPlayer, amount);
                }
                else
                {
                    StatRecord.AdjustLockpicking(dbPlayer, -amount);
                }

                DB.Set(dbPlayer);
            }
            else
            {
                NpcSkinStatWriter.ReapplyNPCStat(creature, ItemPropertyType.Lockpicking, amount, isAdding);
            }
        }
    }
}

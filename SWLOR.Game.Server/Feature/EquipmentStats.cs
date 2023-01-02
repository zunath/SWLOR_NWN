using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using ItemProperty = SWLOR.Game.Server.Core.ItemProperty;

namespace SWLOR.Game.Server.Feature
{
    public static class EquipmentStats
    {
        private delegate void ApplyStatChangeDelegate(uint player, uint item, ItemProperty ip, bool isAdding);
        private static readonly Dictionary<ItemPropertyType, ApplyStatChangeDelegate> _statChangeActions = new Dictionary<ItemPropertyType, ApplyStatChangeDelegate>();

        /// <summary>
        /// When the module loads, cache the actions taken for each type of custom item property.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void RegisterStatActions()
        {
            _statChangeActions[ItemPropertyType.HPBonus] = ApplyHPBonus;
            _statChangeActions[ItemPropertyType.FPBonus] = ApplyFPBonus;
            _statChangeActions[ItemPropertyType.FPRegen] = ApplyFPRegenBonus;
            _statChangeActions[ItemPropertyType.STMBonus] = ApplySTMBonus;
            _statChangeActions[ItemPropertyType.STMRegen] = ApplySTMRegenBonus;
            _statChangeActions[ItemPropertyType.AbilityRecastReduction] = ApplyAbilityRecastReduction;
            _statChangeActions[ItemPropertyType.Attack] = ApplyAttack;
            _statChangeActions[ItemPropertyType.ForceAttack] = ApplyForceAttack;
            _statChangeActions[ItemPropertyType.Defense] = ApplyDefense;
            _statChangeActions[ItemPropertyType.Evasion] = ApplyEvasion;
            _statChangeActions[ItemPropertyType.Control] = ApplyControl;
            _statChangeActions[ItemPropertyType.Craftsmanship] = ApplyCraftsmanship;
            _statChangeActions[ItemPropertyType.CPBonus] = ApplyCPBonus;
        }

        /// <summary>
        /// When an item is equipped, if it has any custom status, apply them now.
        /// This should be run in the "after" event because any restrictions should be checked first.
        /// </summary>
        [NWNEventHandler("item_eqp_bef")]
        public static void ApplyStats()
        {
            var player = OBJECT_SELF;
            if (GetIsDM(player) || GetIsDMPossessed(player)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));

            // The unequip event doesn't fire if an item is being swapped out.
            // If there's an item in the slot, run the stat removals first.
            var existingItemInSlot = GetItemInSlot(slot, player);
            if (GetIsObjectValid(existingItemInSlot))
            {
                for (var ip = GetFirstItemProperty(existingItemInSlot); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(existingItemInSlot))
                {
                    var type = GetItemPropertyType(ip);
                    if (!_statChangeActions.ContainsKey(type)) continue;
                    _statChangeActions[type](player, existingItemInSlot, ip, false);
                }
            }

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (!_statChangeActions.ContainsKey(type)) continue;
                _statChangeActions[type](player, item, ip, true);
            }
        }

        /// <summary>
        /// When an item is unequipped, if it has any custom stats, remove them now.
        /// </summary>
        [NWNEventHandler("item_uneqp_bef")]
        public static void RemoveStats()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (!_statChangeActions.ContainsKey(type)) continue;
                _statChangeActions[type](player, item, ip, false);
            }
        }

        /// <summary>
        /// Applies or removes an HP bonus on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change.</param>
        /// <param name="isAdding">If true, we're adding the HP, if false we're removing it</param>
        private static void ApplyHPBonus(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustPlayerMaxHP(dbPlayer, player, amount);
            }
            else
            {
                Stat.AdjustPlayerMaxHP(dbPlayer, player, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes an FP bonus on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP, if false we're removing it</param>
        private static void ApplyFPBonus(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustPlayerMaxFP(dbPlayer, amount, player);
            }
            else
            {
                Stat.AdjustPlayerMaxFP(dbPlayer, -amount, player);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes an FP Regen bonus on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP Regen, if false we're removing it</param>
        private static void ApplyFPRegenBonus(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustFPRegen(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustFPRegen(dbPlayer, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes a STM bonus on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP, if false we're removing it</param>
        private static void ApplySTMBonus(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustPlayerMaxSTM(dbPlayer, amount, player);
            }
            else
            {
                Stat.AdjustPlayerMaxSTM(dbPlayer, -amount, player);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes a STM Regen bonus on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP Regen, if false we're removing it</param>
        private static void ApplySTMRegenBonus(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustSTMRegen(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustSTMRegen(dbPlayer, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes an ability recast reduction bonus on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the reduction, if false we're removing it.</param>
        private static void ApplyAbilityRecastReduction(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustPlayerRecastReduction(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustPlayerRecastReduction(dbPlayer, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes attack bonuses. This affects the end result of the damage calculation (not to be confused with NWN's Attack Bonus property which is unused).
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the attack, if false we're removing it.</param>
        private static void ApplyAttack(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustAttack(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustAttack(dbPlayer, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes force attack bonuses. This affects the end result of the damage calculation (not to be confused with NWN's Attack Bonus property which is unused).
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the force attack, if false we're removing it.</param>
        private static void ApplyForceAttack(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustForceAttack(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustForceAttack(dbPlayer, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes defense toward a particular damage type on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the defense, if false we're removing it.</param>
        private static void ApplyDefense(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var damageType = (CombatDamageType)GetItemPropertySubType(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustDefense(dbPlayer, damageType, amount);
            }
            else
            {
                Stat.AdjustDefense(dbPlayer, damageType, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes evasion on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the evasion, if false we're removing it.</param>
        private static void ApplyEvasion(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustEvasion(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustEvasion(dbPlayer, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes control on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding control, if false we're removing it.</param>
        private static void ApplyControl(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var subType = GetItemPropertySubType(ip);
            var skillType = SkillType.Invalid;

            // Types are defined in iprp_crafttype.2da
            switch (subType)
            {
                case 1:
                    skillType = SkillType.Smithery;
                    break;
                case 2:
                    skillType = SkillType.Engineering;
                    break;
                case 3:
                    skillType = SkillType.Fabrication;
                    break;
                case 4:
                    skillType = SkillType.Agriculture;
                    break;
            }

            if (skillType == SkillType.Invalid)
            {
                throw new Exception($"Unable to determine skill type for {nameof(ApplyControl)}");
            }

            if (isAdding)
            {
                Stat.AdjustControl(dbPlayer, skillType, amount);
            }
            else
            {
                Stat.AdjustControl(dbPlayer, skillType, -amount);
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Applies or removes craftsmanship on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding craftsmanship, if false we're removing it.</param>
        private static void ApplyCraftsmanship(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var subType = GetItemPropertySubType(ip);
            var skillType = SkillType.Invalid;

            // Types are defined in iprp_crafttype.2da
            switch (subType)
            {
                case 1:
                    skillType = SkillType.Smithery;
                    break;
                case 2:
                    skillType = SkillType.Engineering;
                    break;
                case 3:
                    skillType = SkillType.Fabrication;
                    break;
                case 4:
                    skillType = SkillType.Agriculture;
                    break;
            }
            if (isAdding)
            {
                Stat.AdjustCraftsmanship(dbPlayer, skillType, amount);
            }
            else
            {
                Stat.AdjustCraftsmanship(dbPlayer, skillType, -amount);
            }

            DB.Set(dbPlayer);
        }
        /// <summary>
        /// Applies or removes CP bonuses on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the CP bonus, if false we're removing it.</param>
        private static void ApplyCPBonus(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var subType = GetItemPropertySubType(ip);
            var skillType = SkillType.Invalid;

            // Types are defined in iprp_crafttype.2da
            switch (subType)
            {
                case 1:
                    skillType = SkillType.Smithery;
                    break;
                case 2:
                    skillType = SkillType.Engineering;
                    break;
                case 3:
                    skillType = SkillType.Fabrication;
                    break;
                case 4:
                    skillType = SkillType.Agriculture;
                    break;
            }
            if (isAdding)
            {
                Stat.AdjustCPBonus(dbPlayer, skillType, amount);
            }
            else
            {
                Stat.AdjustCPBonus(dbPlayer, skillType, -amount);
            }

            DB.Set(dbPlayer);
        }
    }
}

using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using ItemProperty = SWLOR.Game.Server.Core.ItemProperty;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            _statChangeActions[ItemPropertyType.STMBonus] = ApplySTMBonus;
            _statChangeActions[ItemPropertyType.AbilityRecastReduction] = ApplyAbilityRecastReduction;
        }

        /// <summary>
        /// When an item is equipped, if it has any custom status, apply them now.
        /// This should be run in the "after" event because any restrictions should be checked first.
        /// </summary>
        [NWNEventHandler("item_eqp_bef")]
        public static void ApplyStats()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var item = StringToObject(Events.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(Events.GetEventData("SLOT"));

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
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var item = StringToObject(Events.GetEventData("ITEM"));

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
            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustPlayerMaxHP(dbPlayer, player, amount);
            }
            else
            {
                Stat.AdjustPlayerMaxHP(dbPlayer, player, -amount);
            }

            DB.Set(playerId, dbPlayer);
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
            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustPlayerMaxFP(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustPlayerMaxFP(dbPlayer, -amount);
            }

            DB.Set(playerId, dbPlayer);
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
            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustPlayerMaxSTM(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustPlayerMaxSTM(dbPlayer, -amount);
            }

            DB.Set(playerId, dbPlayer);
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
            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustPlayerRecastReduction(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustPlayerRecastReduction(dbPlayer, -amount);
            }

            DB.Set(playerId, dbPlayer);
        }
    }
}

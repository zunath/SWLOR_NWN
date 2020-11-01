using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using ItemProperty = NWN.FinalFantasy.Core.ItemProperty;

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
            _statChangeActions[ItemPropertyType.MPBonus] = ApplyMPBonus;
            _statChangeActions[ItemPropertyType.STMBonus] = ApplySTMBonus;
        }

        /// <summary>
        /// When an item is equipped, if it has any custom status, apply them now.
        /// This should be run in the "after" event because any restrictions should be checked first.
        /// </summary>
        [NWNEventHandler("item_eqp_aft")]
        public static void ApplyStats()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var item = StringToObject(Events.GetEventData("ITEM"));

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
        [NWNEventHandler("item_uneqp_aft")]
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
                Stat.AdjustMaxHP(dbPlayer, player, amount);
            }
            else
            {
                Stat.AdjustMaxHP(dbPlayer, player, -amount);
            }

            DB.Set(playerId, dbPlayer);
        }

        /// <summary>
        /// Applies or removes an MP bonus on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the MP, if false we're removing it</param>
        private static void ApplyMPBonus(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustMaxMP(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustMaxMP(dbPlayer, -amount);
            }

            DB.Set(playerId, dbPlayer);
        }

        /// <summary>
        /// Applies or removes a STM bonus on a player.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the MP, if false we're removing it</param>
        private static void ApplySTMBonus(uint player, uint item, ItemProperty ip, bool isAdding)
        {
            var amount = GetItemPropertyCostTableValue(ip);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Entity.Player>(playerId);

            if (isAdding)
            {
                Stat.AdjustMaxSTM(dbPlayer, amount);
            }
            else
            {
                Stat.AdjustMaxSTM(dbPlayer, -amount);
            }

            DB.Set(playerId, dbPlayer);
        }
    }
}

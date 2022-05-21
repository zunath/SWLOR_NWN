using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    public static class HelmetDisplay
    {
        /// <summary>
        /// When a player equips a helmet, set whether it is hidden based on the player's setting.
        /// </summary>
        [NWNEventHandler("mod_equip")]
        public static void EquipHelmet()
        {
            var player = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();

            if (!GetIsPC(player) || GetIsDM(player)) return;
            var itemType = GetBaseItemType(item);

            if (itemType != BaseItem.Helmet) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);

            SetHiddenWhenEquipped(item, !dbPlayer.Settings.ShowHelmet);
        }
    }
}

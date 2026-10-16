using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature
{
    public static class ArmorDisplay
    {
        /// <summary>
        /// When a player equips a type of armor which can be hidden, set whether it is hidden based on the player's setting.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void EquipHelmet()
        {
            var player = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();

            // A creature a DM possesses keeps whatever its own items are set to.
            if (!GetIsPC(player) || GetIsDMPossessed(player)) return;
            var itemType = GetBaseItemType(item);
            if (itemType != BaseItem.Helmet && itemType != BaseItem.Cloak) return;

            bool showHelmet, showCloak;
            if (GetIsDM(player))
            {
                var preferences = DMPreference.Get(player);
                showHelmet = preferences.ShowHelmet;
                showCloak = preferences.ShowCloak;
            }
            else
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);
                showHelmet = dbPlayer.Settings.ShowHelmet;
                showCloak = dbPlayer.Settings.ShowCloak;
            }

            if (itemType == BaseItem.Helmet)
            {
                HelmetModelRenderer.SetShownByOwner(item, showHelmet);
            }
            else
            {
                SetHiddenWhenEquipped(item, !showCloak);
            }
        }
    }
}

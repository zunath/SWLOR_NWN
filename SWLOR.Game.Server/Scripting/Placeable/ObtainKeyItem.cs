using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable
{
    public class ObtainKeyItem: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = _.GetLastUsedBy();

            if (!player.IsPlayer) return;

            NWPlaceable placeable = NWGameObject.OBJECT_SELF;
            var keyItemID = (KeyItem)placeable.GetLocalInt("KEY_ITEM_ID");
            
            if (keyItemID == KeyItem.Invalid) return;

            if (KeyItemService.PlayerHasKeyItem(player, keyItemID))
            {
                player.SendMessage("You already have this key item.");
                return;
            }

            KeyItemService.GivePlayerKeyItem(player, keyItemID);

            string visibilityGUID = placeable.GetLocalString("VISIBILITY_OBJECT_ID");
            if (!string.IsNullOrWhiteSpace(visibilityGUID))
            {
                ObjectVisibilityService.AdjustVisibility(player, placeable, false);
            }
        }
    }
}

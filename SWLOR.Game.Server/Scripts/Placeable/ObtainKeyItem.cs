using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable
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
            NWPlayer player = NWScript.GetLastUsedBy();

            if (!player.IsPlayer) return;

            NWPlaceable placeable = NWScript.OBJECT_SELF;
            var keyItemID = placeable.GetLocalInt("KEY_ITEM_ID");
            
            if (keyItemID <= 0) return;

            if (KeyItemService.PlayerHasKeyItem(player, keyItemID))
            {
                player.SendMessage("You already have this key item.");
                return;
            }

            KeyItemService.GivePlayerKeyItem(player, keyItemID);

            var visibilityGUID = placeable.GetLocalString("VISIBILITY_OBJECT_ID");
            if (!string.IsNullOrWhiteSpace(visibilityGUID))
            {
                ObjectVisibilityService.AdjustVisibility(player, placeable, false);
            }
        }
    }
}

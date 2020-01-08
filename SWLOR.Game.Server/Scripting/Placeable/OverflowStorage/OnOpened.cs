using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Placeable.OverflowStorage
{
    public class OnOpened: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable container = (NWGameObject.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastOpenedBy());
            var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
            foreach (var item in dbPlayer.OverflowItems)
            {
                NWItem oItem = SerializationService.DeserializeItem(item.Value, container);
                oItem.SetLocalString("TEMP_OVERFLOW_ITEM_ID", item.Key.ToString());
            }

            container.IsUseable = false;
        }
    }
}

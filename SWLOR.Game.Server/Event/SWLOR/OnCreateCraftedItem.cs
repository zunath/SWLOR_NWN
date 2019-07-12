using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnCreateCraftedItem
    {
        public NWPlayer Player { get; set; }

        public OnCreateCraftedItem(NWPlayer player)
        {
            Player = player;
        }
    }
}

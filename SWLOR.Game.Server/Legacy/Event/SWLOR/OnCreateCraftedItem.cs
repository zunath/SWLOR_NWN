using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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

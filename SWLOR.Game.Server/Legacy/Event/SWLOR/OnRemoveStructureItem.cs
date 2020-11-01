using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnRemoveStructureItem
    {
        public NWPlayer Player { get; set; }
        public PCBaseStructureItem Entity { get; set; }

        public OnRemoveStructureItem(NWPlayer player, PCBaseStructureItem entity)
        {
            Player = player;
            Entity = entity;
        }
    }
}

using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnStoreStructureItem
    {
        public NWPlayer Player { get; set; }
        public PCBaseStructureItem Entity { get; set; }

        public OnStoreStructureItem(NWPlayer player, PCBaseStructureItem entity)
        {
            Player = player;
            Entity = entity;
        }
    }
}

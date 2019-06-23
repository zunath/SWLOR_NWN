using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.Creature
{
    public class OnCreatureSpawn
    {
        public NWCreature Self { get; }

        public OnCreatureSpawn()
        {
            Self = Object.OBJECT_SELF;
        }
    }
}

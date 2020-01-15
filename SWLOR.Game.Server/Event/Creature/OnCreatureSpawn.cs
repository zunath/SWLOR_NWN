using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;

namespace SWLOR.Game.Server.Event.Creature
{
    public class OnCreatureSpawn
    {
        public NWCreature Self { get; }

        public OnCreatureSpawn()
        {
            Self = NWGameObject.OBJECT_SELF;
        }
    }
}

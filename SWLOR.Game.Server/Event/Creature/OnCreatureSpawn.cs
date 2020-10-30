using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.Creature
{
    public class OnCreatureSpawn
    {
        public NWCreature Self { get; }

        public OnCreatureSpawn()
        {
            Self = NWScript.OBJECT_SELF;
        }
    }
}

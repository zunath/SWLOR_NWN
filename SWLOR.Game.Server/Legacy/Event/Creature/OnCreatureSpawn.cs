using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.Creature
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

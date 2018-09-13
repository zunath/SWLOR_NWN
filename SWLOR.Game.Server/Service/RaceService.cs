using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service
{
    public class RaceService
    {
        private readonly INWScript _;

        public RaceService(INWScript script)
        {
            _ = script;
        }

        public void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;




        }
    }
}

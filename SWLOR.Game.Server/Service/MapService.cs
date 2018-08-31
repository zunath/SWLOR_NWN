using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class MapService : IMapService
    {
        private readonly INWScript _;

        public MapService(INWScript script)
        {
            _ = script;
        }

        public void OnAreaEnter()
        {
            NWArea area = NWArea.Wrap(Object.OBJECT_SELF);
            if (area.GetLocalInt("AUTO_EXPLORED") == NWScript.TRUE)
            {
                _.ExploreAreaForPlayer(area.Object, _.GetEnteringObject());
            }

        }

    }
}

using System.Linq;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class MapService : IMapService
    {
        
        private readonly IDataService _data;

        public MapService(
            
            IDataService data)
        {
            
            _data = data;
        }

        public void OnAreaEnter()
        {
            NWArea area = (Object.OBJECT_SELF);
            NWPlayer player = _.GetEnteringObject();

            if (!player.IsPlayer) return;

            if (area.GetLocalInt("AUTO_EXPLORED") == TRUE)
            {
                _.ExploreAreaForPlayer(area.Object, player);
            }

            LoadMapProgression(area, player);
        }

        public void OnAreaExit()
        {
            NWArea area = Object.OBJECT_SELF;
            NWPlayer player = _.GetExitingObject();
            if (!player.IsPlayer) return;

            SaveMapProgression(area, player);
        }

        public void OnModuleLeave()
        {
            NWPlayer player = _.GetExitingObject();
            NWArea area = _.GetArea(player);
            if (!player.IsPlayer || !area.IsValid) return;

            SaveMapProgression(area, player);
        }

        private void SaveMapProgression(NWArea area, NWPlayer player)
        {
            var map = _data.GetAll<PCMapProgression>().SingleOrDefault(x => x.PlayerID == player.GlobalID && x.AreaResref == area.Resref);
            int areaSize = area.Width * area.Height;
            DatabaseActionType action = DatabaseActionType.Update;

            if (map == null || areaSize != map.Progression.Length)
            {
                if (map != null)
                {
                    _data.SubmitDataChange(map, DatabaseActionType.Delete);
                }

                map = new PCMapProgression
                {
                    PlayerID = player.GlobalID,
                    AreaResref = area.Resref,
                    Progression = string.Empty.PadLeft(areaSize, '0')
                };

                action = DatabaseActionType.Insert;
            }
            
            string progression = string.Empty;
            for (int x = 0; x < area.Width; x++)
            {
                for (int y = 0; y < area.Height; y++)
                {
                    bool visible = _.GetTileExplored(player, area, x, y) == TRUE;
                    progression += visible ? '1' : '0';
                }
            }
            
            map.Progression = progression;
            _data.SubmitDataChange(map, action);
        }

        private void LoadMapProgression(NWArea area, NWPlayer player)
        {
            var map = _data.GetAll<PCMapProgression>().SingleOrDefault(x => x.PlayerID == player.GlobalID && x.AreaResref == area.Resref);
            int areaSize = area.Width * area.Height;

            // No progression set - do a save which will create the record.
            if (map == null)
            {
                SaveMapProgression(area, player);
                return;
            }
            
            // Reset player's progression since the area size has changed.
            if (map.Progression.Length != areaSize)
            {
                SaveMapProgression(area, player);
                return;
            }

            // Otherwise everything checks out. Load the tile visibility.
            int count = 0;
            for (int x = 0; x < area.Width; x++)
            {
                for (int y = 0; y < area.Height; y++)
                {
                    bool visible = map.Progression[count] == '1';
                    _.SetTileExplored(player, area, x, y, visible ? 1 : 0);
                    count++;
                }
            }
        }

    }
}

using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Area;
using SWLOR.Game.Server.NWN.Events.Module;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public static class MapService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnAreaEnter>(message => OnAreaEnter());
            MessageHub.Instance.Subscribe<OnAreaExit>(message => OnAreaExit());
            MessageHub.Instance.Subscribe<OnAreaHeartbeat>(message => OnAreaHeartbeat());
            MessageHub.Instance.Subscribe<OnModuleLeave>(message => OnModuleLeave());
        }
        
        private static void OnAreaEnter()
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

        private static void OnAreaExit()
        {
            NWArea area = Object.OBJECT_SELF;
            NWPlayer player = _.GetExitingObject();
            if (!player.IsPlayer) return;

            SaveMapProgression(area, player);
        }

        private static void OnModuleLeave()
        {
            NWPlayer player = _.GetExitingObject();
            NWArea area = _.GetArea(player);
            if (!player.IsPlayer || !area.IsValid) return;

            SaveMapProgression(area, player);
        }

        private static void SaveMapProgression(NWArea area, NWPlayer player)
        {
            var map = DataService.GetAll<PCMapProgression>().SingleOrDefault(x => x.PlayerID == player.GlobalID && x.AreaResref == area.Resref);
            int areaSize = area.Width * area.Height;
            DatabaseActionType action = DatabaseActionType.Update;

            if (map == null || areaSize != map.Progression.Length)
            {
                if (map != null)
                {
                    DataService.SubmitDataChange(map, DatabaseActionType.Delete);
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
            DataService.SubmitDataChange(map, action);
        }

        private static void LoadMapProgression(NWArea area, NWPlayer player)
        {
            var map = DataService.GetAll<PCMapProgression>().SingleOrDefault(x => x.PlayerID == player.GlobalID && x.AreaResref == area.Resref);
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


        private static void OnAreaHeartbeat()
        {
            NWArea area = Object.OBJECT_SELF;
            
            if (area.GetLocalInt("HIDE_MINIMAP") == _.TRUE)
            {
                var players = NWModule.Get().Players.Where(x => x.Area.Equals(area) && x.IsPlayer);

                foreach (var player in players)
                {
                    _.ExploreAreaForPlayer(area, player, _.FALSE);
                }
            }

        }

    }
}

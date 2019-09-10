using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.ValueObject;
using static NWN._;

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
            using (new Profiler("MapService.OnAreaEnter"))
            {
                NWArea area = (NWGameObject.OBJECT_SELF);
                NWPlayer player = _.GetEnteringObject();

                if (!player.IsPlayer) return;

                if (area.GetLocalInt("AUTO_EXPLORED") == TRUE)
                {
                    _.ExploreAreaForPlayer(area.Object, player);
                }

                LoadMapProgression(area, player);
            }
        }

        private static void OnAreaExit()
        {
            using(new Profiler("MapService.OnAreaExit"))
            {
                NWArea area = NWGameObject.OBJECT_SELF;
                NWPlayer player = _.GetExitingObject();
                if (!player.IsPlayer) return;

                SaveMapProgression(area, player);
            }
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
            var map = DataService.PCMapProgression.GetByPlayerIDAndAreaResrefOrDefault(player.GlobalID, area.Resref);
            DatabaseActionType action = DatabaseActionType.Update;

            if (map == null)
            {
                map = new PCMapProgression
                {
                    PlayerID = player.GlobalID,
                    AreaResref = area.Resref,
                    Progression = string.Empty
                };

                action = DatabaseActionType.Insert;
            }

            map.Progression = NWNXPlayer.GetAreaExplorationState(player, area);
            DataService.SubmitDataChange(map, action);
        }

        private static void LoadMapProgression(NWArea area, NWPlayer player)
        {
            var map = DataService.PCMapProgression.GetByPlayerIDAndAreaResrefOrDefault(player.GlobalID, area.Resref);

            // No progression set - do a save which will create the record.
            if (map == null)
            {
                SaveMapProgression(area, player);
                return;
            }
            
            NWNXPlayer.SetAreaExplorationState(player, area, map.Progression);
        }


        private static void OnAreaHeartbeat()
        {
            NWArea area = NWGameObject.OBJECT_SELF;
            
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

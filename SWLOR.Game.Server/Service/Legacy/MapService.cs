using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
                NWArea area = OBJECT_SELF;
                NWPlayer player = GetEnteringObject();

                if (!player.IsPlayer) return;

                if (GetLocalBool(area, "AUTO_EXPLORED"))
                {
                    ExploreAreaForPlayer(area.Object, player);
                }

                LoadMapProgression(area, player);
            }
        }

        private static void OnAreaExit()
        {
            using(new Profiler("MapService.OnAreaExit"))
            {
                NWArea area = OBJECT_SELF;
                NWPlayer player = GetExitingObject();
                if (!player.IsPlayer) return;

                SaveMapProgression(area, player);
            }
        }

        private static void OnModuleLeave()
        {
            NWPlayer player = GetExitingObject();
            NWArea area = GetArea(player);
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

            map.Progression = Core.NWNX.Player.GetAreaExplorationState(player, area);
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
            
            Core.NWNX.Player.SetAreaExplorationState(player, area, map.Progression);
        }


        private static void OnAreaHeartbeat()
        {
            NWArea area = OBJECT_SELF;
            
            if (GetLocalBool(area, "HIDE_MINIMAP"))
            {
                var players = NWModule.Get().Players.Where(x => x.Area.Equals(area) && x.IsPlayer);

                foreach (var player in players)
                {
                    ExploreAreaForPlayer(area, player, false);
                }
            }

        }

    }
}

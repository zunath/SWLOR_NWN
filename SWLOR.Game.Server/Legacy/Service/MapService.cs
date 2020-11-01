using System.Linq;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Area;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Service
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
                var area = OBJECT_SELF;
                NWPlayer player = GetEnteringObject();

                if (!player.IsPlayer) return;

                if (GetLocalBool(area, "AUTO_EXPLORED"))
                {
                    ExploreAreaForPlayer(area, player);
                }

                LoadMapProgression(area, player);
            }
        }

        private static void OnAreaExit()
        {
            using(new Profiler("MapService.OnAreaExit"))
            {
                var area = OBJECT_SELF;
                NWPlayer player = GetExitingObject();
                if (!player.IsPlayer) return;

                SaveMapProgression(area, player);
            }
        }

        private static void OnModuleLeave()
        {
            NWPlayer player = GetExitingObject();
            var area = GetArea(player);
            if (!player.IsPlayer || !GetIsObjectValid(area)) return;

            SaveMapProgression(area, player);
        }

        private static void SaveMapProgression(uint area, NWPlayer player)
        {
            var areaResref = GetResRef(area);
            var map = DataService.PCMapProgression.GetByPlayerIDAndAreaResrefOrDefault(player.GlobalID, areaResref);
            var action = DatabaseActionType.Update;

            if (map == null)
            {
                map = new PCMapProgression
                {
                    PlayerID = player.GlobalID,
                    AreaResref = areaResref,
                    Progression = string.Empty
                };

                action = DatabaseActionType.Insert;
            }

            map.Progression = Core.NWNX.Player.GetAreaExplorationState(player, area);
            DataService.SubmitDataChange(map, action);
        }

        private static void LoadMapProgression(uint area, NWPlayer player)
        {
            var areaResref = GetResRef(area);
            var map = DataService.PCMapProgression.GetByPlayerIDAndAreaResrefOrDefault(player.GlobalID, areaResref);

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
            var area = OBJECT_SELF;
            
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

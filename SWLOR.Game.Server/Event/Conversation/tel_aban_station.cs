using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using BaseService = SWLOR.Game.Server.Service.Legacy.BaseService;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class tel_aban_station
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            using (new Profiler(nameof(tel_aban_station)))
            {
                NWPlayer player = GetPCSpeaker();
                NWObject talkingTo = OBJECT_SELF;

                var mainLevel = GetOrCreateMainLevel(player);
                NWObject landingWP = GetNearestObjectByTag("ABAN_STATION_LANDING", GetFirstObjectInArea(mainLevel));
                player.AssignCommand(() => { ActionJumpToLocation(landingWP.Location); });
            }
        }

        private static uint GetOrCreateMainLevel(NWPlayer player)
        {
            var memberArea = player
                .PartyMembers
                .FirstOrDefault(x =>
                {
                    var area = GetArea(x);
                    var areaTag = GetTag(area);
                    
                    return areaTag == "AbandonedStationDirectorsChamber" ||
                           areaTag == "zomb_abanstation" ||
                           areaTag == "zomb_abanstatio2";
                })
                ?.Area;

            // No party members are already in an instance. Create a new set of instances and link them up
            // via local objects.
            if(memberArea == null)
            {
                return BuildInstanceGroup();
            }

            return GetLocalObject((uint)memberArea, "MAIN_LEVEL");
        }

        private static uint BuildInstanceGroup()
        {
            var restrictedLevel = CreateArea("zomb_abanstatio2");
            var mainLevel = CreateArea("zomb_abanstation");
            var directorsChambers = CreateArea("zomb_abanstatio3");

            SetLocalObject(restrictedLevel, "RESTRICTED_LEVEL", restrictedLevel);
            SetLocalObject(restrictedLevel, "MAIN_LEVEL", mainLevel);
            SetLocalObject(restrictedLevel, "DIRECTORS_CHAMBERS", directorsChambers);

            SetLocalObject(mainLevel, "RESTRICTED_LEVEL", restrictedLevel);
            SetLocalObject(mainLevel, "MAIN_LEVEL", mainLevel);
            SetLocalObject(mainLevel, "DIRECTORS_CHAMBERS", directorsChambers);

            SetLocalObject(directorsChambers, "RESTRICTED_LEVEL", restrictedLevel);
            SetLocalObject(directorsChambers, "MAIN_LEVEL", mainLevel);
            SetLocalObject(directorsChambers, "DIRECTORS_CHAMBERS", directorsChambers);

            // Set local variables for instances
            SetLocalBool(restrictedLevel, "IS_AREA_INSTANCE", true);
            SetLocalBool(mainLevel, "IS_AREA_INSTANCE", true);
            SetLocalBool(directorsChambers, "IS_AREA_INSTANCE", true);
            BaseService.RegisterAreaStructures(restrictedLevel);
            BaseService.RegisterAreaStructures(mainLevel);
            BaseService.RegisterAreaStructures(directorsChambers);
            
            // Notify of instance creation
            MessageHub.Instance.Publish(new OnAreaInstanceCreated(restrictedLevel));
            MessageHub.Instance.Publish(new OnAreaInstanceCreated(mainLevel));
            MessageHub.Instance.Publish(new OnAreaInstanceCreated(directorsChambers));

            // Spawn key cards randomly on the main level.
            var spawnIDs = new List<int>{1, 2, 3, 4, 5, 6};
            var keyCard1 = spawnIDs.ElementAt(RandomService.Random(1, 6));
            spawnIDs.RemoveAt(keyCard1-1);
            var keyCard2 = spawnIDs.ElementAt(RandomService.Random(1, 5));
            
            NWLocation keyCardLocation1 = GetLocation(GetNearestObjectByTag("KEY_CARD_SPAWN_" + keyCard1, GetFirstObjectInArea(mainLevel)));
            NWLocation keyCardLocation2 = GetLocation(GetNearestObjectByTag("KEY_CARD_SPAWN_" + keyCard2, GetFirstObjectInArea(mainLevel)));
            
            CreateObject(ObjectType.Placeable, "station_key_card", keyCardLocation1);
            CreateObject(ObjectType.Placeable, "station_key_card", keyCardLocation2);

            SetLocalInt(mainLevel, "KEY_CARDS_REMAINING", 2);

            // Now do the same thing on the restricted level.
            spawnIDs = new List<int>{1, 2, 3, 4, 5, 6, 7};
            keyCard1 = spawnIDs.ElementAt(RandomService.Random(1, 7));
            spawnIDs.RemoveAt(keyCard1-1);
            keyCard2 = spawnIDs.ElementAt(RandomService.Random(1, 6));
            
            keyCardLocation1 = GetLocation(GetNearestObjectByTag("KEY_CARD_SPAWN_" + keyCard1, GetFirstObjectInArea(restrictedLevel)));
            keyCardLocation2 = GetLocation(GetNearestObjectByTag("KEY_CARD_SPAWN_" + keyCard2, GetFirstObjectInArea(restrictedLevel)));
            
            CreateObject(ObjectType.Placeable, "station_key_card", keyCardLocation1);
            CreateObject(ObjectType.Placeable, "station_key_card", keyCardLocation2);

            SetLocalInt(restrictedLevel, "KEY_CARDS_REMAINING", 2);

            return mainLevel;
        }
    }
}
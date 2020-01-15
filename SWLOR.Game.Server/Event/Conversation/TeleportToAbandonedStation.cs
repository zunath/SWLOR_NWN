using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Event.Conversation
{
    internal class TeleportToAbandonedStation
    {
        public static void Main()
        {
            NWPlayer player = GetPCSpeaker();
            NWObject talkingTo = NWGameObject.OBJECT_SELF;

            var mainLevel = GetOrCreateMainLevel(player);
            NWObject landingWP = GetNearestObjectByTag("ABAN_STATION_LANDING", GetFirstObjectInArea(mainLevel));
            player.AssignCommand(() => { ActionJumpToLocation(landingWP.Location); });
        }

        private static NWArea GetOrCreateMainLevel(NWPlayer player)
        {
            NWArea memberArea = player
                .PartyMembers
                .FirstOrDefault(x => x.Area.Tag == "AbandonedStationDirectorsChamber" ||
                                     x.Area.Tag == "zomb_abanstation" ||
                                     x.Area.Tag == "zomb_abanstatio2")
                ?.Area;

            // No party members are already in an instance. Create a new set of instances and link them up
            // via local objects.
            if(memberArea == null)
            {
                return BuildInstanceGroup();
            }

            return memberArea.GetLocalObject("MAIN_LEVEL");
        }

        private static NWArea BuildInstanceGroup()
        {
            NWArea restrictedLevel = CreateArea("zomb_abanstatio2");
            NWArea mainLevel = CreateArea("zomb_abanstation");
            NWArea directorsChambers = CreateArea("zomb_abanstatio3");

            restrictedLevel.SetLocalObject("RESTRICTED_LEVEL", restrictedLevel);
            restrictedLevel.SetLocalObject("MAIN_LEVEL", mainLevel);
            restrictedLevel.SetLocalObject("DIRECTORS_CHAMBERS", directorsChambers);

            mainLevel.SetLocalObject("RESTRICTED_LEVEL", restrictedLevel);
            mainLevel.SetLocalObject("MAIN_LEVEL", mainLevel);
            mainLevel.SetLocalObject("DIRECTORS_CHAMBERS", directorsChambers);

            directorsChambers.SetLocalObject("RESTRICTED_LEVEL", restrictedLevel);
            directorsChambers.SetLocalObject("MAIN_LEVEL", mainLevel);
            directorsChambers.SetLocalObject("DIRECTORS_CHAMBERS", directorsChambers);

            // Set local variables for instances
            restrictedLevel.SetLocalBoolean("IS_AREA_INSTANCE", true);
            mainLevel.SetLocalBoolean("IS_AREA_INSTANCE", true);
            directorsChambers.SetLocalBoolean("IS_AREA_INSTANCE", true);
            restrictedLevel.Data["BASE_SERVICE_STRUCTURES"] = new List<AreaStructure>();
            mainLevel.Data["BASE_SERVICE_STRUCTURES"] = new List<AreaStructure>();
            directorsChambers.Data["BASE_SERVICE_STRUCTURES"] = new List<AreaStructure>();
            
            // Notify of instance creation
            MessageHub.Instance.Publish(new OnAreaInstanceCreated(restrictedLevel));
            MessageHub.Instance.Publish(new OnAreaInstanceCreated(mainLevel));
            MessageHub.Instance.Publish(new OnAreaInstanceCreated(directorsChambers));

            // Spawn key cards randomly on the main level.
            List<int> spawnIDs = new List<int>{1, 2, 3, 4, 5, 6};
            int keyCard1 = spawnIDs.ElementAt(RandomService.Random(1, 6));
            spawnIDs.RemoveAt(keyCard1-1);
            int keyCard2 = spawnIDs.ElementAt(RandomService.Random(1, 5));
            
            NWLocation keyCardLocation1 = GetLocation(GetNearestObjectByTag("KEY_CARD_SPAWN_" + keyCard1, GetFirstObjectInArea(mainLevel)));
            NWLocation keyCardLocation2 = GetLocation(GetNearestObjectByTag("KEY_CARD_SPAWN_" + keyCard2, GetFirstObjectInArea(mainLevel)));
            
            CreateObject(ObjectType.Placeable, "station_key_card", keyCardLocation1);
            CreateObject(ObjectType.Placeable, "station_key_card", keyCardLocation2);

            mainLevel.SetLocalInt("KEY_CARDS_REMAINING", 2);

            // Now do the same thing on the restricted level.
            spawnIDs = new List<int>{1, 2, 3, 4, 5, 6, 7};
            keyCard1 = spawnIDs.ElementAt(RandomService.Random(1, 7));
            spawnIDs.RemoveAt(keyCard1-1);
            keyCard2 = spawnIDs.ElementAt(RandomService.Random(1, 6));
            
            keyCardLocation1 = GetLocation(GetNearestObjectByTag("KEY_CARD_SPAWN_" + keyCard1, GetFirstObjectInArea(restrictedLevel)));
            keyCardLocation2 = GetLocation(GetNearestObjectByTag("KEY_CARD_SPAWN_" + keyCard2, GetFirstObjectInArea(restrictedLevel)));
            
            CreateObject(ObjectType.Placeable, "station_key_card", keyCardLocation1);
            CreateObject(ObjectType.Placeable, "station_key_card", keyCardLocation2);

            restrictedLevel.SetLocalInt("KEY_CARDS_REMAINING", 2);

            return mainLevel;
        }
    }
}
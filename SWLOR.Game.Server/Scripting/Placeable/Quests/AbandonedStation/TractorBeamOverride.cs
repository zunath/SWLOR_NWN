﻿using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.Quests.AbandonedStation
{
    public class TractorBeamOverride: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable overridePlaceable = NWGameObject.OBJECT_SELF;
            NWObject door = _.GetNearestObjectByTag("aban_director_exit", overridePlaceable);
            NWPlayer player = _.GetLastUsedBy();
            door.AssignCommand(() =>_.SetLocked(door, false));
            int questID = overridePlaceable.GetLocalInt("QUEST_ID_1");

            _.SpeakString("The tractor beam has been disabled. A door in this room has unlocked.");

            NWArea mainLevel = overridePlaceable.Area.GetLocalObject("MAIN_LEVEL");
            NWArea restrictedLevel = overridePlaceable.Area.GetLocalObject("RESTRICTED_LEVEL");
            NWArea directorsChambers = overridePlaceable.Area.GetLocalObject("DIRECTORS_CHAMBERS");

            // Enable the shuttle back to Viscara object.
            NWPlaceable teleportObject = _.GetNearestObjectByTag("aban_shuttle_exit", mainLevel);
            teleportObject.IsUseable = true;

            var quest = QuestService.GetQuestByID(questID);
            // Advance each party member's quest progression if they are in one of these three instance areas.
            foreach (var member in player.PartyMembers)
            {
                // Not in one of the three areas? Move to the next member.
                NWArea area = member.Area;
                if (area != mainLevel &&
                    area != restrictedLevel &&
                    area != directorsChambers)
                    continue;

                quest.Advance(member.Object, overridePlaceable);
            }

            // Disable this placeable from being used again for this instance.
            overridePlaceable.IsUseable = false;
        }
    }
}

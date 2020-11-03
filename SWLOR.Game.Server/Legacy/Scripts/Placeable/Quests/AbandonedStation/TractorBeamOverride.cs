using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.Quests.AbandonedStation
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
            NWPlaceable overridePlaceable = OBJECT_SELF;
            NWObject door = GetNearestObjectByTag("aban_director_exit", overridePlaceable);
            NWPlayer player = GetLastUsedBy();
            door.AssignCommand(() =>SetLocked(door, false));
            var questID = overridePlaceable.GetLocalInt("QUEST_ID_1");

            SpeakString("The tractor beam has been disabled. A door in this room has unlocked.");

            var placeableArea = GetArea(overridePlaceable);
            uint mainLevel = GetLocalObject(placeableArea, "MAIN_LEVEL");
            uint restrictedLevel = GetLocalObject(placeableArea, "RESTRICTED_LEVEL");
            uint directorsChambers = GetLocalObject(placeableArea, "DIRECTORS_CHAMBERS");

            // Enable the shuttle back to Viscara object.
            NWPlaceable teleportObject = GetNearestObjectByTag("aban_shuttle_exit", mainLevel);
            teleportObject.IsUseable = true;

            var quest = Quest.GetQuestById(questID.ToString()); // todo need to update this to new system
            // Advance each party member's quest progression if they are in one of these three instance areas.
            foreach (var member in player.PartyMembers)
            {
                // Not in one of the three areas? Move to the next member.
                var area = member.Area;
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

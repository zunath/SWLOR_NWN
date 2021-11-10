using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.Quests
{
    public class ForceRitesHolocron : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            const int QuestID = 30;
            NWPlaceable crystal = _.OBJECT_SELF;
            NWPlayer player = _.GetLastUsedBy();

            player.SendMessage("You pick up the strange looking cube which seemingly opens itself at your touch. A stream of swirling blue light" +
                    " shifts into what seems to be a recording of a robed humanoid assembling a weapon. With time, you think you may be able to do this yourself.");

            var quest = QuestService.GetQuestByID(QuestID);
            quest.Advance(player, crystal);

            // Hide the "Source of Power?" placeable so the player can't use it again.
            ObjectVisibilityService.AdjustVisibility(player, "81533EBB-2084-4C97-B004-8E1D8C395F56", false);

            _.PlaySound("dt_ordi_11");
            // Notify the player that new lightsaber perks have unlocked.
            player.FloatingText("You have unlocked the Lightsaber Blueprints perk. Find this under the Engineering category in your perks menu.");
        }
    }
}

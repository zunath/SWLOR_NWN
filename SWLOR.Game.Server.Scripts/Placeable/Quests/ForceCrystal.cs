using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.Quests
{
    public class ForceCrystal: IScript
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
            NWPlaceable crystal = NWGameObject.OBJECT_SELF;
            NWPlayer player = _.GetLastUsedBy();

            // Check player's current quest state. If they aren't on stage 2 of the quest only show a message.
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, QuestID);
            var currentState = DataService.QuestState.GetByID(status.CurrentQuestStateID);

            if (currentState.Sequence != 2)
            {
                player.SendMessage("The crystal glows quietly...");
                return;
            }

            // Player is on stage 2, so they're able to click the crystal, get a cluster, complete the quest, and teleport back to the cavern.
            int type = crystal.GetLocalInt("CRYSTAL_COLOR_TYPE");
            string cluster;

            switch (type)
            {
                case 1: cluster = "c_cluster_blue"; break; // Blue
                case 2: cluster = "c_cluster_red"; break; // Red
                case 3: cluster = "c_cluster_green"; break; // Green 
                case 4: cluster = "c_cluster_yellow"; break; // Yellow
                default: throw new Exception("Invalid crystal color type.");
            }

            _.CreateItemOnObject(cluster, player);
            QuestService.AdvanceQuestState(player, crystal, QuestID);

            // Hide the "Source of Power?" placeable so the player can't use it again.
            ObjectVisibilityService.AdjustVisibility(player, "81533EBB-2084-4C97-B004-8E1D8C395F56", false);

            NWObject tpWP = _.GetObjectByTag("FORCE_QUEST_LANDING");
            player.AssignCommand(() => _.ActionJumpToLocation(tpWP.Location));
            
            // Notify the player that new lightsaber perks have unlocked.
            player.FloatingText("You have unlocked the Lightsaber Blueprints perk. Find this under the Engineering category in your perks menu.");
        }
    }
}

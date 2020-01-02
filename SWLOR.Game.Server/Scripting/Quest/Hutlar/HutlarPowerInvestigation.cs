using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest.Hutlar
{
    public class HutlarPowerInvestigation: AbstractQuest
    {
        public HutlarPowerInvestigation()
        {
            CreateQuest(1003, "Hutlar Power Investigation", "hut_power_invest")
                .AddPrerequisiteQuest(1000)
                .AddPrerequisiteQuest(1001)
                .AddPrerequisiteQuest(1002)
                
                .AddObjectiveUseObject(1)
                .AddObjectiveUseObject(2)
                .AddObjectiveUseObject(3)
                .AddObjectiveUseObject(4)
                .AddObjectiveUseObject(5)
                .AddObjectiveTalkToNPC(6)
                .AddObjectiveUseObject(7)
                .AddObjectiveTalkToNPC(8)
                
                .AddRewardGold(1200)
                .AddRewardItem("xp_tome_4", 1)

                .OnAccepted((player, questSource) =>
                {
                    // Southeast 
                    ObjectVisibilityService.AdjustVisibility(player, "9CD9E7D9-4F10-4A0E-B67D-293CE6EA8EF5", true);
                })

                .OnAdvanced((player, questSource, state) =>
                {
                    string visibilityObject;

                    switch(state)
                    {
                        // Central
                        case 2:
                            visibilityObject = "989B8C42-B4EE-48B7-8426-9D5C20016AEB";
                            break;
                        // Northern
                        case 3:
                            visibilityObject = "4C5721F2-9241-4A6F-9A62-F28CF0525682";
                            break;
                        // Southwestern
                        case 4:
                            visibilityObject = "E9C705B1-2AC9-4F9A-B481-FF3E5E99D8FF";
                            break;
                        // Northwestern
                        case 5:
                            visibilityObject = "83652C7A-7D38-4304-AD4B-92D5783AB279";
                            break;
                        // Northwestern again, Actuator
                        case 7:
                            visibilityObject = "AA0E6798-38E4-4E50-8F0A-C3177FBF2717";
                            break;
                        default: return;
                    }

                    ObjectVisibilityService.AdjustVisibility(player, visibilityObject, true);
                });

        }
    }
}

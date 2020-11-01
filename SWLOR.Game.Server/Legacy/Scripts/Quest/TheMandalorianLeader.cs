using SWLOR.Game.Server.Legacy.Quest;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
{
    public class TheMandalorianLeader: AbstractQuest
    {
        public TheMandalorianLeader()
        {
            CreateQuest(18, "The Mandalorian Leader", "the_manda_leader")
                .AddPrerequisiteQuest(17)
                
                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_MandalorianLeader, 1)
                .AddObjectiveTalkToNPC(2)

                .EnableRewardSelection()
                .AddRewardGold(350)
                .AddRewardFame(3, 40)
                .AddRewardItem("rifle_tran", 1)
                .AddRewardItem("blaster_tran", 1)
                .AddRewardItem("xp_tome_1", 1)
                .AddRewardItem("bst_sword_tran", 1)
                .AddRewardItem("twinblade_tran", 1)
                .AddRewardItem("kukri_tran", 1)
                .AddRewardItem("halberd_tran", 1)
                .AddRewardItem("greataxe_tran", 1)

                .OnAccepted((player, questSource) =>
                {
                    KeyItemService.GivePlayerKeyItem(player, 7);
                });
        }
    }
}

using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class VanquishTheVellenRaiders: AbstractQuest
    {
        public VanquishTheVellenRaiders()
        {
            CreateQuest(29, "Vanquish the Vellen Raiders", "vanquish_vellen")
                .AddPrerequisiteQuest(28)

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_VellenFleshleader, 1)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(750)
                .AddRewardFame(4, 40)
                .AddRewardItem("xp_tome_1", 1)
                
                .OnAccepted((player, o) =>
                {
                    KeyItemService.GivePlayerKeyItem(player, 20);
                });
        }
    }
}

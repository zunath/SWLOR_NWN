using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class PrismLightNecklace: AbstractQuest
    {
        public PrismLightNecklace()
        {
            CreateQuest(215, "Armorsmith Guild Task: 1x Prism Light Necklace", "arm_tsk_215")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "prism_neck_l", 1, true)

                .AddRewardGold(395)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 82);
        }
    }
}

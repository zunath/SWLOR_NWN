using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class PrismForceNecklace: AbstractQuest
    {
        public PrismForceNecklace()
        {
            CreateQuest(213, "Armorsmith Guild Task: 1x Prism Force Necklace", "arm_tsk_213")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "prism_neck_f", 1, true)

                .AddRewardGold(395)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 82);
        }
    }
}

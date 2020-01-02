using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicLightBoots: AbstractQuest
    {
        public BasicLightBoots()
        {
            CreateQuest(109, "Armorsmith Guild Task: 1x Basic Light Boots", "arm_tsk_109")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "light_boots_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 9);
        }
    }
}

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightChokerI: AbstractQuest
    {
        public LightChokerI()
        {
            CreateQuest(139, "Armorsmith Guild Task: 1x Light Choker I", "arm_tsk_139")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lt_choker_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 19);
        }
    }
}

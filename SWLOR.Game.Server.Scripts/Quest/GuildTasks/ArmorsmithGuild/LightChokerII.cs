using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightChokerII: AbstractQuest
    {
        public LightChokerII()
        {
            CreateQuest(163, "Armorsmith Guild Task: 1x Light Choker II", "arm_tsk_163")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lt_choker_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 39);
        }
    }
}

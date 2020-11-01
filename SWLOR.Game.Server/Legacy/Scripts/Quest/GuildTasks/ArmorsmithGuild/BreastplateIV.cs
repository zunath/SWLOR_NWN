using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class BreastplateIV: AbstractQuest
    {
        public BreastplateIV()
        {
            CreateQuest(195, "Armorsmith Guild Task: 1x Breastplate IV", "arm_tsk_195")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "breastplate_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}

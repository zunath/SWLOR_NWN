using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightChokerIV: AbstractQuest
    {
        public LightChokerIV()
        {
            CreateQuest(210, "Armorsmith Guild Task: 1x Light Choker IV", "arm_tsk_210")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lt_choker_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}

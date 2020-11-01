using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightBootsIV: AbstractQuest
    {
        public LightBootsIV()
        {
            CreateQuest(209, "Armorsmith Guild Task: 1x Light Boots IV", "arm_tsk_209")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "light_boots_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}

using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class PowerGloveIV: AbstractQuest
    {
        public PowerGloveIV()
        {
            CreateQuest(212, "Armorsmith Guild Task: 1x Power Glove IV", "arm_tsk_212")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "powerglove_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 79);
        }
    }
}

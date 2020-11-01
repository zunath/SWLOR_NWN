using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class AdditionalStronidiumTankSmall: AbstractQuest
    {
        public AdditionalStronidiumTankSmall()
        {
            CreateQuest(170, "Armorsmith Guild Task: 1x Additional Stronidium Tank (Small)", "arm_tsk_170")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssstron1", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}

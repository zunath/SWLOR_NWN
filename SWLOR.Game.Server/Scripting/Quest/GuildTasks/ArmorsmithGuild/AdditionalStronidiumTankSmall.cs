using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class AdditionalStronidiumTankSmall: AbstractQuest
    {
        public AdditionalStronidiumTankSmall()
        {
            CreateQuest(170, "Armorsmith Guild Task: 1x Additional Stronidium Tank (Small)", "arm_tsk_170")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 3)


                .AddObjectiveCollectItem(1, "ssstron1", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 59);
        }
    }
}

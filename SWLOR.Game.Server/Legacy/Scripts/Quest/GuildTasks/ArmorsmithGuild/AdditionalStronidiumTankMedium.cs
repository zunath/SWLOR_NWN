using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class AdditionalStronidiumTankMedium: AbstractQuest
    {
        public AdditionalStronidiumTankMedium()
        {
            CreateQuest(194, "Armorsmith Guild Task: 1x Additional Stronidium Tank (Medium)", "arm_tsk_194")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ssstron2", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 84);
        }
    }
}

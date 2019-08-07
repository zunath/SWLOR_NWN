using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ArmorsmithI: AbstractQuest
    {
        public ArmorsmithI()
        {
            CreateQuest(365, "Engineering Guild Task: 1x Armorsmith I", "eng_tsk_365")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_armsmth1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class HPRegenIII: AbstractQuest
    {
        public HPRegenIII()
        {
            CreateQuest(530, "Engineering Guild Task: 1x HP Regen III", "eng_tsk_530")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_hpregen3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}

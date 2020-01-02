using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class HPRegen: AbstractQuest
    {
        public HPRegen()
        {
            CreateQuest(425, "Engineering Guild Task: 1x HP Regen", "eng_tsk_425")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_hpregen1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ArmorClassIII: AbstractQuest
    {
        public ArmorClassIII()
        {
            CreateQuest(499, "Engineering Guild Task: 1x Armor Class III", "eng_tsk_499")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_ac3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}

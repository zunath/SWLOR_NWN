using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ImprovedEnmity: AbstractQuest
    {
        public ImprovedEnmity()
        {
            CreateQuest(396, "Engineering Guild Task: 1x Improved Enmity", "eng_tsk_396")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_enmup1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}

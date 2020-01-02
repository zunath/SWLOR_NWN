using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class DexterityIII: AbstractQuest
    {
        public DexterityIII()
        {
            CreateQuest(517, "Engineering Guild Task: 1x Dexterity III", "eng_tsk_517")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_dex3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}

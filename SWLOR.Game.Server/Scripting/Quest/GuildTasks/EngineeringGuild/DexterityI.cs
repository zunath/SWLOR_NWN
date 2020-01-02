using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class DexterityI: AbstractQuest
    {
        public DexterityI()
        {
            CreateQuest(385, "Engineering Guild Task: 1x Dexterity I", "eng_tsk_385")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_dex1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}

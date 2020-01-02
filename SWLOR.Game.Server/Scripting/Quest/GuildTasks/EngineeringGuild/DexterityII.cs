using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class DexterityII: AbstractQuest
    {
        public DexterityII()
        {
            CreateQuest(420, "Engineering Guild Task: 1x Dexterity II", "eng_tsk_420")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_dex2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}

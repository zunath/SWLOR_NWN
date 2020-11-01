using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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

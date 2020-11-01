using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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

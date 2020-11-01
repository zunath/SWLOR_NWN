using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class SaberHilt: AbstractQuest
    {
        public SaberHilt()
        {
            CreateQuest(405, "Engineering Guild Task: 1x Saber Hilt", "eng_tsk_405")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ls_hilt", 1, true)

                .AddRewardGold(110)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 25);
        }
    }
}

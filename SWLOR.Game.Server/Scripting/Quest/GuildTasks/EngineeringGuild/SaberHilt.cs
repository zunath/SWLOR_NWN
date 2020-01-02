using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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

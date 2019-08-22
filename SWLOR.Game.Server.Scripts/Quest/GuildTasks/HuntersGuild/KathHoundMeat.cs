using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class KathHoundMeat: AbstractQuest
    {
        public KathHoundMeat()
        {
            CreateQuest(580, "Hunter's Guild Task: 6x Kath Hound Meat", "hun_tsk_580")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "kath_meat_1", 6, false)

                .AddRewardGold(65)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 12);
        }
    }
}

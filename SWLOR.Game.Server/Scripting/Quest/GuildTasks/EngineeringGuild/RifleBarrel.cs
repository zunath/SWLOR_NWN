using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class RifleBarrel: AbstractQuest
    {
        public RifleBarrel()
        {
            CreateQuest(361, "Engineering Guild Task: 1x Rifle Barrel", "eng_tsk_361")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 0)


                .AddObjectiveCollectItem(1, "rifle_barrel", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}

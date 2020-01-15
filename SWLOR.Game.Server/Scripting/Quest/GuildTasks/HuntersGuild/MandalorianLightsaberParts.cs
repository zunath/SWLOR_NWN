using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MandalorianLightsaberParts: AbstractQuest
    {
        public MandalorianLightsaberParts()
        {
            CreateQuest(596, "Hunter's Guild Task: 6x Mandalorian Lightsaber Parts", "hun_tsk_596")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 1)


                .AddObjectiveCollectItem(1, "m_ls_parts", 6, false)

                .AddRewardGold(83)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 25);
        }
    }
}

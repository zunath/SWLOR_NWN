using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class StarshipAuxiliaryLightCannon: AbstractQuest
    {
        public StarshipAuxiliaryLightCannon()
        {
            CreateQuest(554, "Engineering Guild Task: 1x Starship Auxiliary Light Cannon", "eng_tsk_554")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 4)


                .AddObjectiveCollectItem(1, "sswpn2", 1, true)

                .AddRewardGold(490)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 106);
        }
    }
}

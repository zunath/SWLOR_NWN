using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class TrainingFoilIVBlue: AbstractQuest
    {
        public TrainingFoilIVBlue()
        {
            CreateQuest(558, "Engineering Guild Task: 1x Training Foil IV (Blue)", "eng_tsk_558")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "lightsaber_4", 1, true)

                .AddRewardGold(505)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 105);
        }
    }
}

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class MetalReinforcement: AbstractQuest
    {
        public MetalReinforcement()
        {
            CreateQuest(121, "Armorsmith Guild Task: 1x Metal Reinforcement", "arm_tsk_121")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "padding_metal", 1, true)

                .AddRewardGold(55)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 14);
        }
    }
}

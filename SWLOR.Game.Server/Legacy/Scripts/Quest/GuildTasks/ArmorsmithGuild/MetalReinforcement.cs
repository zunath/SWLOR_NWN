using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
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

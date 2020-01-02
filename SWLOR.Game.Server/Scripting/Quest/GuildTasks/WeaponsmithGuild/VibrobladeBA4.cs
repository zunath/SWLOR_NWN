using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBA4: AbstractQuest
    {
        public VibrobladeBA4()
        {
            CreateQuest(344, "Weaponsmith Guild Task: 1x Vibroblade BA4", "wpn_tsk_344")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "battleaxe_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBA2: AbstractQuest
    {
        public VibrobladeBA2()
        {
            CreateQuest(294, "Weaponsmith Guild Task: 1x Vibroblade BA2", "wpn_tsk_294")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "battleaxe_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}

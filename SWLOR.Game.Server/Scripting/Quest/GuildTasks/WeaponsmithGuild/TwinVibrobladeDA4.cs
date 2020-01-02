using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeDA4: AbstractQuest
    {
        public TwinVibrobladeDA4()
        {
            CreateQuest(341, "Weaponsmith Guild Task: 1x Twin Vibroblade DA4", "wpn_tsk_341")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "doubleaxe_4", 1, true)

                .AddRewardGold(405)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 84);
        }
    }
}

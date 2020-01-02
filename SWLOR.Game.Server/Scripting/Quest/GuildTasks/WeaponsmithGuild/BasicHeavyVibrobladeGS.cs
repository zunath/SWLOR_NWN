using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicHeavyVibrobladeGS: AbstractQuest
    {
        public BasicHeavyVibrobladeGS()
        {
            CreateQuest(230, "Weaponsmith Guild Task: 1x Basic Heavy Vibroblade GS", "wpn_tsk_230")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "greatsword_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}

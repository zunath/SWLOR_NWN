using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonM4: AbstractQuest
    {
        public BatonM4()
        {
            CreateQuest(325, "Weaponsmith Guild Task: 1x Baton M4", "wpn_tsk_325")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "mace_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}

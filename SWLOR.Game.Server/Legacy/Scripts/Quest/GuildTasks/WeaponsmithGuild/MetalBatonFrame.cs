using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class MetalBatonFrame: AbstractQuest
    {
        public MetalBatonFrame()
        {
            CreateQuest(244, "Weaponsmith Guild Task: 1x Metal Baton Frame", "wpn_tsk_244")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "m_baton_frame", 1, true)

                .AddRewardGold(40)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 10);
        }
    }
}

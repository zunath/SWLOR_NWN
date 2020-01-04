using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.Hutlar
{
    public class BeatTheByysk: AbstractQuest
    {
        public BeatTheByysk()
        {
            CreateQuest(1000, "Beat the Byysk", "beat_byysk")
                .AddObjectiveKillTarget(1, NPCGroup.HutlarByysk, 15)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(800);
        }
    }
}

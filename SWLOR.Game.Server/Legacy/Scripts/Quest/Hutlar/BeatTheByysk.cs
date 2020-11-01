using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.Hutlar
{
    public class BeatTheByysk: AbstractQuest
    {
        public BeatTheByysk()
        {
            CreateQuest(1000, "Beat the Byysk", "beat_byysk")
                .AddObjectiveKillTarget(1, NPCGroupType.Hutlar_Byysk, 15)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(800);
        }
    }
}

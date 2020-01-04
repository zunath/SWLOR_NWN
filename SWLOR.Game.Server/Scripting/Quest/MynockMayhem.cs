using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class MynockMayhem: AbstractQuest
    {
        public MynockMayhem()
        {
            CreateQuest(8, "Mynock Mayhem", "mynock_mayhem")
                .AddObjectiveKillTarget(1, NPCGroupType.CZ220_Mynocks, 5)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(100)
                .AddRewardKeyItem(2)
                .AddRewardFame(FameRegion.CZ220, 5);
        }
    }
}

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class MandalorianDogTags: AbstractQuest
    {
        public MandalorianDogTags()
        {
            CreateQuest(19, "Mandalorian Dog Tags", "mand_dog_tags")
                .AddPrerequisiteQuest(17)

                .AddObjectiveCollectItem(1, "man_tags", 5, false)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(350)
                .AddRewardFame(FameRegion.VelesColony, 30);
        }
    }
}

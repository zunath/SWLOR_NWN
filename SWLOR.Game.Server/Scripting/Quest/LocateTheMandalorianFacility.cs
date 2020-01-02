using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class LocateTheMandalorianFacility: AbstractQuest
    {
        public LocateTheMandalorianFacility()
        {
            CreateQuest(16, "Locate the Mandalorian Facility", "locate_m_fac")
                .AddObjectiveEnterTrigger(1)
                .AddObjectiveTalkToNPC(2);
        }
    }
}

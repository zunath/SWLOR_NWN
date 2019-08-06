using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class LocateTheMandalorianFacility: AbstractQuest
    {
        public LocateTheMandalorianFacility()
        {
            CreateQuest(16, "Locate the Mandalorian Facility", "locate_m_fac")
                .AddPrerequisiteFame(3, 35)
                
                .AddObjectiveEnterTrigger(1)
                .AddObjectiveTalkToNPC(2);
        }
    }
}

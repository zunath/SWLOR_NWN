using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
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

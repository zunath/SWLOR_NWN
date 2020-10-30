using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Scripts.Trigger
{
    public class SpaceEncounter: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            // Check for timeout.  Refresh every game hour.
            NWObject self = NWScript.OBJECT_SELF;
            var hour = self.GetLocalInt("HOUR");
            var day = self.GetLocalInt("DAY");
            var month = self.GetLocalInt("MONTH");
            var year = self.GetLocalInt("YEAR");

            if (NWScript.GetTimeHour() > hour || NWScript.GetCalendarDay() > day || NWScript.GetCalendarMonth() > month || NWScript.GetCalendarYear() > year)
            {
                SpaceService.CreateSpaceEncounter(NWScript.OBJECT_SELF, (NWPlayer)NWScript.GetEnteringObject());
                self.SetLocalInt("HOUR", NWScript.GetTimeHour());
                self.SetLocalInt("DAY", NWScript.GetCalendarDay());
                self.SetLocalInt("MONTH", NWScript.GetCalendarMonth());
                self.SetLocalInt("YEAR", NWScript.GetCalendarYear());
            }

        }
    }
}

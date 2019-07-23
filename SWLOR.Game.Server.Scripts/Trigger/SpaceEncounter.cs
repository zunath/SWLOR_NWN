using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

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
            NWObject self = NWGameObject.OBJECT_SELF;
            int hour = self.GetLocalInt("HOUR");
            int day = self.GetLocalInt("DAY");
            int month = self.GetLocalInt("MONTH");
            int year = self.GetLocalInt("YEAR");

            if (_.GetTimeHour() > hour || _.GetCalendarDay() > day || _.GetCalendarMonth() > month || _.GetCalendarYear() > year)
            {
                SpaceService.CreateSpaceEncounter(NWGameObject.OBJECT_SELF, (NWPlayer)_.GetEnteringObject());
                self.SetLocalInt("HOUR", _.GetTimeHour());
                self.SetLocalInt("DAY", _.GetCalendarDay());
                self.SetLocalInt("MONTH", _.GetCalendarMonth());
                self.SetLocalInt("YEAR", _.GetCalendarYear());
            }

        }
    }
}

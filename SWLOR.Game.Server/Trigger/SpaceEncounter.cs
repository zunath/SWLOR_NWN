using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Trigger
{
    public class SpaceEncounter: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ISpaceService _space;

        public SpaceEncounter(INWScript script, 
                              ISpaceService space)
        {
            _ = script;
            _space = space;
        }

        public bool Run(params object[] args)
        {
            // Check for timeout.  Refresh every game hour.
            NWObject self = Object.OBJECT_SELF;
            int hour = self.GetLocalInt("HOUR");
            int day = self.GetLocalInt("DAY");
            int month = self.GetLocalInt("MONTH");
            int year = self.GetLocalInt("YEAR");

            if (_.GetTimeHour() > hour || _.GetCalendarDay() > day || _.GetCalendarMonth() > month || _.GetCalendarYear() > year)
            {
                _space.CreateSpaceEncounter(Object.OBJECT_SELF, (NWPlayer)_.GetEnteringObject());
                self.SetLocalInt("HOUR", _.GetTimeHour());
                self.SetLocalInt("DAY", _.GetCalendarDay());
                self.SetLocalInt("MONTH", _.GetCalendarMonth());
                self.SetLocalInt("YEAR", _.GetCalendarYear());
            }

            return true;
        }
    }
}

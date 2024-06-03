using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class MutationRequirementDayOfWeek: IMutationRequirement
    {
        private readonly List<DayOfWeek> _daysOfWeek;

        public MutationRequirementDayOfWeek(DayOfWeek dayOfWeek, params DayOfWeek[] additionalDaysOfWeek)
        {
            _daysOfWeek = new List<DayOfWeek> { dayOfWeek };

            if(additionalDaysOfWeek != null)
                _daysOfWeek.AddRange(additionalDaysOfWeek);
        }

        public string CheckRequirements(IncubationJob job)
        {
            var now = DateTime.UtcNow;

            if (!_daysOfWeek.Contains(now.DayOfWeek))
            {
                return "Invalid day of the week.";
            }

            return string.Empty;
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// Requires the job to be completed on one of the listed days. Days follow server time (UTC),
    /// which can differ from the player's local day.
    /// </summary>
    public class MutationRequirementDayOfWeek: IMutationRequirement
    {
        private readonly List<DayOfWeek> _daysOfWeek;

        public IReadOnlyList<DayOfWeek> Days => _daysOfWeek;

        public MutationRequirementDayOfWeek(DayOfWeek dayOfWeek, params DayOfWeek[] additionalDaysOfWeek)
        {
            _daysOfWeek = new List<DayOfWeek> { dayOfWeek };

            if(additionalDaysOfWeek != null)
                _daysOfWeek.AddRange(additionalDaysOfWeek);
        }

        public bool IsMetOn(DayOfWeek day)
        {
            return _daysOfWeek.Contains(day);
        }

        public string CheckRequirements(IncubationJob job)
        {
            var now = DateTime.UtcNow;

            if (!IsMetOn(now.DayOfWeek))
            {
                return $"Must be completed on {DescribeDays()} (server time, UTC). Today is {now.DayOfWeek}.";
            }

            return string.Empty;
        }

        public string GetRequirementDescription()
        {
            return $"Only on {DescribeDays()} (server time, UTC)";
        }

        public string DescribeDays()
        {
            if (_daysOfWeek.Count == 1)
                return _daysOfWeek[0].ToString();

            return string.Join(", ", _daysOfWeek.GetRange(0, _daysOfWeek.Count - 1)) + " or " + _daysOfWeek[^1];
        }
    }
}

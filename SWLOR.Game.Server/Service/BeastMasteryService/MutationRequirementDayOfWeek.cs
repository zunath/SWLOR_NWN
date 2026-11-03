using System.Collections.Generic;
using System.Linq;
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

        /// <summary>Lists the days in week order (Monday first), for example "Wednesday or Thursday".</summary>
        public string DescribeDays(bool shortNames = false)
        {
            var names = _daysOfWeek
                .Distinct()
                .OrderBy(day => ((int)day + 6) % 7)
                .Select(day => shortNames ? day.ToString()[..3] : day.ToString())
                .ToList();

            if (names.Count == 1)
                return names[0];

            return string.Join(", ", names.GetRange(0, names.Count - 1)) + " or " + names[^1];
        }
    }
}

using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class MutationRequirementMinimumChance: IMutationRequirement
    {
        private readonly int _minimumChanceRequired;

        public MutationRequirementMinimumChance(int minimumChanceRequired)
        {
            _minimumChanceRequired = minimumChanceRequired;
        }

        public string CheckRequirements(IncubationJob job)
        {
            var jobChance = job.MutationChance / 10;
            if (jobChance < _minimumChanceRequired)
            {
                return $"Minimum chance not met. Required: {_minimumChanceRequired}, you have: {job.MutationChance}.";
            }

            return string.Empty;
        }
    }
}

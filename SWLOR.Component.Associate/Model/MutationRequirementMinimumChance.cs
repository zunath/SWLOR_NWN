using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Crafting.Contracts;
using SWLOR.Shared.Domain.Crafting.ValueObjects;

namespace SWLOR.Component.Associate.Model
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

using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class MutationRequirementEnzyme: IMutationRequirement
    {
        public Dictionary<EnzymeColorType, int> LyaseEnzymeColors { get; set; }
        public Dictionary<EnzymeColorType, int> IsomeraseEnzymeColors { get; set; }
        public Dictionary<EnzymeColorType, int> HydrolaseEnzymeColors { get; set; }

        public MutationRequirementEnzyme()
        {
            LyaseEnzymeColors = new Dictionary<EnzymeColorType, int>();
            IsomeraseEnzymeColors = new Dictionary<EnzymeColorType, int>();
            HydrolaseEnzymeColors = new Dictionary<EnzymeColorType, int>();
        }

        public string CheckRequirements(IncubationJob job)
        {
            foreach (var (color, count) in LyaseEnzymeColors)
            {
                if (job.LyaseColors[color] < count)
                    return $"Required {color} lyase enzymes: {count}. This job only has: {job.LyaseColors[color]}.";
            }
            foreach (var (color, count) in IsomeraseEnzymeColors)
            {
                if (job.IsomeraseColors[color] < count)
                    return $"Required {color} isomerase enzymes: {count}. This job only has: {job.IsomeraseColors[color]}.";
            }
            foreach (var (color, count) in HydrolaseEnzymeColors)
            {
                if (job.HydrolaseColors[color] < count)
                    return $"Required {color} hydrolase enzymes: {count}. This job only has: {job.HydrolaseColors[color]}.";
            }

            return string.Empty;
        }
    }
}

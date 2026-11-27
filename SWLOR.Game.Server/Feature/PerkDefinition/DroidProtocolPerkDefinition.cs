using System.Collections.Generic;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The industrial droid protocols (plan section 27): droid-only AI instructions uploaded from instruction discs.
    /// They compete with combat instructions for AI slots and work where the droid works (a ship's station or droid bay;
    /// holdings and station buildings read them when those exist). Numbers: <see cref="DroidIndustryTuning"/>.
    /// </summary>
    public class DroidProtocolPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            EfficiencyProtocol();
            DamageControlProtocol();
            SurveyProtocol();
            ProspectingProtocol();

            return _builder.Build();
        }

        private void EfficiencyProtocol()
        {
            Protocol(PerkType.EfficiencyProtocol, "ife_droid_asm", "Efficiency Protocol",
                "Raises the Industry a working droid's output reads: droid-bay yields, holdings and drills.",
                "Droid-bay mining, gas and salvage yields, holdings and drills read the droid's Industry {0}% higher.");
        }

        private void DamageControlProtocol()
        {
            Protocol(PerkType.DamageControlProtocol, "ife_repair", "Damage Control Protocol",
                "Raises the repairs of a droid at the Engineer station or in a Logistics droid bay.",
                "Repairers the droid runs at the Engineer station repair {0}% more, and so does a Logistics ship with the droid in its droid bay.");
        }

        private void SurveyProtocol()
        {
            Protocol(PerkType.SurveyProtocol, "iit_smprobe", "Survey Protocol",
                "Lets a droid run a Probe Launcher at its station and raises its scan strength.",
                "The droid switches on a Probe Launcher at its station whenever there is something to scan, and scans {0}% stronger.");
        }

        private void ProspectingProtocol()
        {
            Protocol(PerkType.ProspectingProtocol, "ife_prospect", "Prospecting Protocol",
                "Raises the rare-find chance of a ship's mining and salvage while the droid rides in its droid bay.",
                "From a droid bay, the droid adds {0} to the rare-find chance of the ship's mining and salvage.");
        }

        private void Protocol(PerkType type, string icon, string name, string summary, string rankDescription)
        {
            _builder.Create(PerkCategoryType.DroidProtocols, type)
                .Name(name)
                .Icon(icon)
                .GroupType(PerkGroupType.Droid)
                .Description(summary);

            foreach (var (rank, value) in DroidIndustryTuning.ProtocolValues[type])
            {
                var (slots, engineeringRank) = DroidIndustryTuning.ProtocolRanks[rank];
                _builder.AddPerkLevel()
                    .Description(string.Format(rankDescription, value))
                    .Price(0)
                    .DroidAISlots(slots);

                // The Engineering rank only sets the instruction's tier, as the combat instructions' skill ranks do.
                if (engineeringRank > 0)
                    _builder.RequirementSkill(SkillType.Engineering, engineeringRank);
            }
        }
    }
}

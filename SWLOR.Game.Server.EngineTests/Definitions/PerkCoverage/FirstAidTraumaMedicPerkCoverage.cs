using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class FirstAidTraumaMedicPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.MedKit,
                    MaxLevel = 4,
                    Prices = new[] { 2, 4, 4, 5 },
                    GrantedFeats = new[] { FeatType.MedKit1, FeatType.MedKit2, FeatType.MedKit3, FeatType.MedKit4 },
                },
                new()
                {
                    Perk = PerkType.TreatmentKit,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 4 },
                    GrantedFeats = new[] { FeatType.TreatmentKit1, FeatType.TreatmentKit2, FeatType.TreatmentKit3 },
                },
                new()
                {
                    Perk = PerkType.MedicalInjectorRig,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.MedicalInjectorRigTrait },
                },
                new()
                {
                    Perk = PerkType.EmergencySealant,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.EmergencySealantTrait },
                },
                new()
                {
                    Perk = PerkType.KoltoMist,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.KoltoMist1, FeatType.KoltoMist2 },
                },
                new()
                {
                    Perk = PerkType.Resuscitation,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.Resuscitation1, FeatType.Resuscitation2 },
                },
                new()
                {
                    Perk = PerkType.Infusion,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.Infusion1, FeatType.Infusion2 },
                },
                new()
                {
                    Perk = PerkType.EmergencyTriage,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.EmergencyTriage1 },
                },
            };
        }
    }
}

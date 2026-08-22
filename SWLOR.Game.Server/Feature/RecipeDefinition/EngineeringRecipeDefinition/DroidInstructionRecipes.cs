using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidInstructionRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }

        private void Tier1()
        {
            CreateTier(10, "jade", "elec_ruined", "quadrenium",
                new DroidInstructionRecipe(RecipeType.InstructionAdrenalStim1, "id_adrenal1"),
                new DroidInstructionRecipe(RecipeType.InstructionBlasterBeacon1, "id_blastbeac1"),
                new DroidInstructionRecipe(RecipeType.InstructionConcussionGrenade1, "id_concgren1"),
                new DroidInstructionRecipe(RecipeType.InstructionDeflectorShield1, "id_defshield1"),
                new DroidInstructionRecipe(RecipeType.InstructionFlamethrower1, "id_flamethrow1"),
                new DroidInstructionRecipe(RecipeType.InstructionFragGrenade1, "id_fraggren1"),
                new DroidInstructionRecipe(RecipeType.InstructionIncendiaryField1, "id_incfield1"),
                new DroidInstructionRecipe(RecipeType.InstructionIonGrenade1, "id_iongren1"),
                new DroidInstructionRecipe(RecipeType.InstructionMedKit1, "id_medkit1"),
                new DroidInstructionRecipe(RecipeType.InstructionProvoke1, "id_provoke1"),
                new DroidInstructionRecipe(RecipeType.InstructionShielding1, "id_shielding1"),
                new DroidInstructionRecipe(RecipeType.InstructionSonicBurst1, "id_sonicburst1"),
                new DroidInstructionRecipe(RecipeType.InstructionTreatmentKit1, "id_treatkit1"),
                new DroidInstructionRecipe(RecipeType.InstructionWeaponJam, "id_weaponjam"));
        }

        private void Tier2()
        {
            CreateTier(20, "agate", "elec_flawed", "vintrium",
                new DroidInstructionRecipe(RecipeType.InstructionAdrenalStim2, "id_adrenal2"),
                new DroidInstructionRecipe(RecipeType.InstructionAntitoxin, "id_antitoxin"),
                new DroidInstructionRecipe(RecipeType.InstructionArcProjector1, "id_arcproj1"),
                new DroidInstructionRecipe(RecipeType.InstructionBlasterBeacon2, "id_blastbeac2"),
                new DroidInstructionRecipe(RecipeType.InstructionDeflectorShield2, "id_defshield2"),
                new DroidInstructionRecipe(RecipeType.InstructionFlashGrenade, "id_flashgren"),
                new DroidInstructionRecipe(RecipeType.InstructionFragGrenade2, "id_fraggren2"),
                new DroidInstructionRecipe(RecipeType.InstructionIonLance1, "id_ionlance1"),
                new DroidInstructionRecipe(RecipeType.InstructionKoltoMist1, "id_koltomist1"),
                new DroidInstructionRecipe(RecipeType.InstructionMedKit2, "id_medkit2"),
                new DroidInstructionRecipe(RecipeType.InstructionPainSuppressant1, "id_painsupp1"),
                new DroidInstructionRecipe(RecipeType.InstructionPowerCell1, "id_powercell1"),
                new DroidInstructionRecipe(RecipeType.InstructionProvoke2, "id_provoke2"),
                new DroidInstructionRecipe(RecipeType.InstructionRailDart1, "id_raildart1"),
                new DroidInstructionRecipe(RecipeType.InstructionRemoteCharge1, "id_remcharge1"),
                new DroidInstructionRecipe(RecipeType.InstructionResuscitation1, "id_resusc1"),
                new DroidInstructionRecipe(RecipeType.InstructionShielding2, "id_shielding2"),
                new DroidInstructionRecipe(RecipeType.InstructionSignalJammer, "id_sigjammer"),
                new DroidInstructionRecipe(RecipeType.InstructionTreatmentKit2, "id_treatkit2"),
                new DroidInstructionRecipe(RecipeType.InstructionWristRocket1, "id_wristrck1"));
        }

        private void Tier3()
        {
            CreateTier(30, "citrine", "elec_good", "ionite",
                new DroidInstructionRecipe(RecipeType.InstructionAdhesiveGrenade1, "id_adhgren1"),
                new DroidInstructionRecipe(RecipeType.InstructionFlamethrower2, "id_flamethrow2"),
                new DroidInstructionRecipe(RecipeType.InstructionFocusStim1, "id_focusstim1"),
                new DroidInstructionRecipe(RecipeType.InstructionIncendiaryField2, "id_incfield2"),
                new DroidInstructionRecipe(RecipeType.InstructionInfusion1, "id_infusion1"),
                new DroidInstructionRecipe(RecipeType.InstructionIonGrenade2, "id_iongren2"),
                new DroidInstructionRecipe(RecipeType.InstructionMedKit3, "id_medkit3"),
                new DroidInstructionRecipe(RecipeType.InstructionPowerCell2, "id_powercell2"),
                new DroidInstructionRecipe(RecipeType.InstructionRemoteCharge2, "id_remcharge2"),
                new DroidInstructionRecipe(RecipeType.InstructionShockBeacon1, "id_shockbeac1"),
                new DroidInstructionRecipe(RecipeType.InstructionSonicBurst2, "id_sonicburst2"),
                new DroidInstructionRecipe(RecipeType.InstructionWristRocket2, "id_wristrck2"));
        }

        private void Tier4()
        {
            CreateTier(40, "ruby", "elec_imperfect", "katrium",
                new DroidInstructionRecipe(RecipeType.InstructionAdhesiveGrenade2, "id_adhgren2"),
                new DroidInstructionRecipe(RecipeType.InstructionAdrenalStim3, "id_adrenal3"),
                new DroidInstructionRecipe(RecipeType.InstructionArcProjector2, "id_arcproj2"),
                new DroidInstructionRecipe(RecipeType.InstructionBlasterBeacon3, "id_blastbeac3"),
                new DroidInstructionRecipe(RecipeType.InstructionClusterGrenade, "id_clustgren"),
                new DroidInstructionRecipe(RecipeType.InstructionConcussionGrenade2, "id_concgren2"),
                new DroidInstructionRecipe(RecipeType.InstructionCryoSprayer, "id_cryospray"),
                new DroidInstructionRecipe(RecipeType.InstructionDeflectorShield3, "id_defshield3"),
                new DroidInstructionRecipe(RecipeType.InstructionDisruptionPulse, "id_disrpulse"),
                new DroidInstructionRecipe(RecipeType.InstructionFragGrenade3, "id_fraggren3"),
                new DroidInstructionRecipe(RecipeType.InstructionIonLance2, "id_ionlance2"),
                new DroidInstructionRecipe(RecipeType.InstructionKoltoMist2, "id_koltomist2"),
                new DroidInstructionRecipe(RecipeType.InstructionMedKit4, "id_medkit4"),
                new DroidInstructionRecipe(RecipeType.InstructionPainSuppressant2, "id_painsupp2"),
                new DroidInstructionRecipe(RecipeType.InstructionRailDart2, "id_raildart2"),
                new DroidInstructionRecipe(RecipeType.InstructionResuscitation2, "id_resusc2"),
                new DroidInstructionRecipe(RecipeType.InstructionShielding3, "id_shielding3"),
                new DroidInstructionRecipe(RecipeType.InstructionShockBeacon2, "id_shockbeac2"));
        }

        private void Tier5()
        {
            CreateTier(50, "emerald", "elec_high", "zinsiam",
                new DroidInstructionRecipe(RecipeType.InstructionArcProjector3, "id_arcproj3"),
                new DroidInstructionRecipe(RecipeType.InstructionEmergencyBunker, "id_emgbunker"),
                new DroidInstructionRecipe(RecipeType.InstructionEmergencyCocktail, "id_emgcocktail"),
                new DroidInstructionRecipe(RecipeType.InstructionEmergencyTriage, "id_emgtriage"),
                new DroidInstructionRecipe(RecipeType.InstructionFlamethrower3, "id_flamethrow3"),
                new DroidInstructionRecipe(RecipeType.InstructionFocusStim2, "id_focusstim2"),
                new DroidInstructionRecipe(RecipeType.InstructionGroupDeflector, "id_groupdef"),
                new DroidInstructionRecipe(RecipeType.InstructionIncendiaryField3, "id_incfield3"),
                new DroidInstructionRecipe(RecipeType.InstructionInfusion2, "id_infusion2"),
                new DroidInstructionRecipe(RecipeType.InstructionIonLance3, "id_ionlance3"),
                new DroidInstructionRecipe(RecipeType.InstructionKillzoneBeacon, "id_killbeacon"),
                new DroidInstructionRecipe(RecipeType.InstructionOverloadBarrage, "id_overbarrage"),
                new DroidInstructionRecipe(RecipeType.InstructionPowerCell3, "id_powercell3"),
                new DroidInstructionRecipe(RecipeType.InstructionRailDart3, "id_raildart3"),
                new DroidInstructionRecipe(RecipeType.InstructionSonicBurst3, "id_sonicburst3"),
                new DroidInstructionRecipe(RecipeType.InstructionThermalDetonator, "id_thermdeton"),
                new DroidInstructionRecipe(RecipeType.InstructionTreatmentKit3, "id_treatkit3"),
                new DroidInstructionRecipe(RecipeType.InstructionWristRocket3, "id_wristrck3"));
        }

        private void CreateTier(
            int level,
            string gemResref,
            string electronicResref,
            string metalResref,
            params DroidInstructionRecipe[] recipes)
        {
            foreach (var recipe in recipes)
            {
                _builder.Create(recipe.Type, SkillType.Engineering)
                    .Category(RecipeCategoryType.DroidInstruction)
                    .Resref(recipe.Resref)
                    .Level(level)
                    .Quantity(1)
                    .Component(gemResref, 1)
                    .Component(electronicResref, 2)
                    .Component(metalResref, 1);
            }
        }

        private readonly record struct DroidInstructionRecipe(RecipeType Type, string Resref);
    }
}

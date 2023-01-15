using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using System.Collections.Generic;

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
            // Doublehand I
            _builder.Create(RecipeType.InstructionDoublehand1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_doublehand1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Dual Wield
            _builder.Create(RecipeType.InstructionDualWield, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_dualwield")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Shield Bash I
            _builder.Create(RecipeType.InstructionShieldBash1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shbash1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Backstab I
            _builder.Create(RecipeType.InstructionBackstab1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_backstab1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Hard Slash I
            _builder.Create(RecipeType.InstructionHardSlash1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_hardslash1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Double Thrust I
            _builder.Create(RecipeType.InstructionDoubleThrust1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_dthrust1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Cross Cut I
            _builder.Create(RecipeType.InstructionCrossCut1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_crosscut1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Chi I
            _builder.Create(RecipeType.InstructionChi1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_chi1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Striking Cobra I
            _builder.Create(RecipeType.InstructionStrikingCobra1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_strcobra1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Leg Sweep I
            _builder.Create(RecipeType.InstructionLegSweep1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_legsweep1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Double Shot I
            _builder.Create(RecipeType.InstructionDoubleShot1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_dblshot1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Piercing Toss I
            _builder.Create(RecipeType.InstructionPiercingToss1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_ptoss1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Crippling Shot I
            _builder.Create(RecipeType.InstructionCripplingShot1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_crippshot1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Frag Grenade I
            _builder.Create(RecipeType.InstructionFragGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_fraggren1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Concussion Grenade I
            _builder.Create(RecipeType.InstructionConcussionGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_concgren1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Ion Grenade I
            _builder.Create(RecipeType.InstructionIonGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_iongren1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Kolto Grenade I
            _builder.Create(RecipeType.InstructionKoltoGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_koltgren1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Flamethrower I
            _builder.Create(RecipeType.InstructionFlamethrower1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flamethrow1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Provoke I
            _builder.Create(RecipeType.InstructionProvoke1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_provoke1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Med Kit I
            _builder.Create(RecipeType.InstructionMedKit1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Treatment Kit I
            _builder.Create(RecipeType.InstructionTreatmentKit1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_treatkit1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);

            // Shielding I
            _builder.Create(RecipeType.InstructionShielding1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shielding1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("jade", 1)
                .Component("elec_ruined", 2)
                .Component("quadrenium", 1);
        }
        private void Tier2()
        {
            // Doublehand II
            _builder.Create(RecipeType.InstructionDoublehand2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_doublehand2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Bulwark
            _builder.Create(RecipeType.InstructionBulwark, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_bulwark")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Poison Stab I
            _builder.Create(RecipeType.InstructionPoisonStab1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_pstab1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Power Attack I
            _builder.Create(RecipeType.InstructionPowerAttack1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_pwrattack1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Cleave
            _builder.Create(RecipeType.InstructionCleave, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_cleave")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Crescent Moon I
            _builder.Create(RecipeType.InstructionCrescentMoon1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_cmoon1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Skewer I
            _builder.Create(RecipeType.InstructionSkewer1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_skewer1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Spinning Whirl I
            _builder.Create(RecipeType.InstructionSpinningWhirl1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_spinwhirl1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Knockdown
            _builder.Create(RecipeType.InstructionKnockdown, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_knockdown")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Electric Fist I
            _builder.Create(RecipeType.InstructionElectricFist1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_elecfist1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Slam I
            _builder.Create(RecipeType.InstructionSlam1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_slam1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Quick Draw I
            _builder.Create(RecipeType.InstructionQuickDraw1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_quickdraw1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Explosive Toss I
            _builder.Create(RecipeType.InstructionExplosiveToss1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_exptoss1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Tranquilizer Shot I
            _builder.Create(RecipeType.InstructionTranquilizerShot1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_tranqshot1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Frag Grenade II
            _builder.Create(RecipeType.InstructionFragGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_fraggren2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Flashbang Grenade I
            _builder.Create(RecipeType.InstructionFlashbangGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flashgren1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Wrist Rocket I
            _builder.Create(RecipeType.InstructionWristRocket1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_wristrck1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Provoke II
            _builder.Create(RecipeType.InstructionProvoke2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_provoke2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Med Kit II
            _builder.Create(RecipeType.InstructionMedKit2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Kolto Recovery I
            _builder.Create(RecipeType.InstructionKoltoRecovery1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_kolt1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Resuscitation I
            _builder.Create(RecipeType.InstructionResuscitation1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_resusc1")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Treatment Kit II
            _builder.Create(RecipeType.InstructionTreatmentKit2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_treatkit2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);

            // Shielding II
            _builder.Create(RecipeType.InstructionShielding2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shielding2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("agate", 1)
                .Component("elec_flawed", 2)
                .Component("vintrium", 1);
        }
        private void Tier3()
        {
            // Doublehand III
            _builder.Create(RecipeType.InstructionDoublehand3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_doublehand3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Shield Master
            _builder.Create(RecipeType.InstructionShieldMaster, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shmaster")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Shield Bash II
            _builder.Create(RecipeType.InstructionShieldBash2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shbash2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Shield Resistance I
            _builder.Create(RecipeType.InstructionShieldResistance1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shresist1")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Alacrity
            _builder.Create(RecipeType.InstructionAlacrity, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_alacrity")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Backstab II
            _builder.Create(RecipeType.InstructionBackstab2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_backstab2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Power Attack II
            _builder.Create(RecipeType.InstructionPowerAttack2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_pwrattack2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Hard Slash II
            _builder.Create(RecipeType.InstructionHardSlash2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_hardslash2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Double Thrust II
            _builder.Create(RecipeType.InstructionDoubleThrust2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_dthrust2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Cross Cut II
            _builder.Create(RecipeType.InstructionCrossCut2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_crosscut2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Chi II
            _builder.Create(RecipeType.InstructionChi2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_chi2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Striking Cobra II
            _builder.Create(RecipeType.InstructionStrikingCobra2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_strcobra2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Leg Sweep II
            _builder.Create(RecipeType.InstructionLegSweep2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_legsweep2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Dirty Blow
            _builder.Create(RecipeType.InstructionDirtyBlow, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_dirtyblow")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Double Shot II
            _builder.Create(RecipeType.InstructionDoubleShot2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_dblshot2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Piercing Toss II
            _builder.Create(RecipeType.InstructionPiercingToss2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_ptoss2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Crippling Shot II
            _builder.Create(RecipeType.InstructionCripplingShot2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_crippshot2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Ion Grenade II
            _builder.Create(RecipeType.InstructionIonGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_iongren2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Kolto Grenade II
            _builder.Create(RecipeType.InstructionKoltoGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_koltgren2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Adhesive Grenade I
            _builder.Create(RecipeType.InstructionAdhesiveGrenade1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_adhgren1")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Flamethrower II
            _builder.Create(RecipeType.InstructionFlamethrower2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flamethrow2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Wrist Rocket II
            _builder.Create(RecipeType.InstructionWristRocket2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_wristrck2")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Med Kit III
            _builder.Create(RecipeType.InstructionMedKit3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Stasis Field I
            _builder.Create(RecipeType.InstructionStasisField1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_stasisf1")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Combat Enhancement I
            _builder.Create(RecipeType.InstructionCombatEnhancement1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_combenh1")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);

            // Infusion I
            _builder.Create(RecipeType.InstructionInfusion1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_infusion1")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("citrine", 1)
                .Component("elec_good", 2)
                .Component("ionite", 1);
        }
        private void Tier4()
        {
            // Doublehand IV
            _builder.Create(RecipeType.InstructionDoublehand4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_doublehand4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Shield Bash III
            _builder.Create(RecipeType.InstructionShieldBash3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shbash3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Shield Resistance II
            _builder.Create(RecipeType.InstructionShieldResistance2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shresist2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Poison Stab II
            _builder.Create(RecipeType.InstructionPoisonStab2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_pstab2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Backstab III
            _builder.Create(RecipeType.InstructionBackstab3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_backstab3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Superior Weapon Focus
            _builder.Create(RecipeType.InstructionSuperiorWeaponFocus, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_supweapfoc")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Crescent Moon II
            _builder.Create(RecipeType.InstructionCrescentMoon2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_cmoon2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Hard Slash III
            _builder.Create(RecipeType.InstructionHardSlash3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_hardslash3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Skewer II
            _builder.Create(RecipeType.InstructionSkewer2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_skewer2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Double Thrust III
            _builder.Create(RecipeType.InstructionDoubleThrust3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_dthrust3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Spinning Whirl II
            _builder.Create(RecipeType.InstructionSpinningWhirl2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_spinwhirl2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Cross Cut III
            _builder.Create(RecipeType.InstructionCrossCut3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_crosscut3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Inner Strength I
            _builder.Create(RecipeType.InstructionInnerStrength1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_innstr1")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Electric Fist II
            _builder.Create(RecipeType.InstructionElectricFist2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_elecfist2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Striking Cobra III
            _builder.Create(RecipeType.InstructionStrikingCobra3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_strcobra3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Slam II
            _builder.Create(RecipeType.InstructionSlam2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_slam2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Leg Sweep III
            _builder.Create(RecipeType.InstructionLegSweep3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_legsweep3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Precision Aim I
            _builder.Create(RecipeType.InstructionPrecisionAim1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_precaim1")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Quick Draw II
            _builder.Create(RecipeType.InstructionQuickDraw2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_quickdraw2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Double Shot III
            _builder.Create(RecipeType.InstructionDoubleShot3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_dblshot3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Explosive Toss II
            _builder.Create(RecipeType.InstructionExplosiveToss2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_exptoss2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Piercing Toss III
            _builder.Create(RecipeType.InstructionPiercingToss3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_ptoss3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Tranquilizer Shot II
            _builder.Create(RecipeType.InstructionTranquilizerShot2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_tranqshot2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Crippling Shot III
            _builder.Create(RecipeType.InstructionCripplingShot3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_crippshot3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Frag Grenade III
            _builder.Create(RecipeType.InstructionFragGrenade3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_fraggren3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Concussion Grenade II
            _builder.Create(RecipeType.InstructionConcussionGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_concgren2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Flashbang Grenade II
            _builder.Create(RecipeType.InstructionFlashbangGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flashgren2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Ion Grenade III
            _builder.Create(RecipeType.InstructionIonGrenade3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_iongren3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Kolto Grenade III
            _builder.Create(RecipeType.InstructionKoltoGrenade3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_koltgren3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Adhesive Grenade II
            _builder.Create(RecipeType.InstructionAdhesiveGrenade2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_adhgren2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Med Kit IV
            _builder.Create(RecipeType.InstructionMedKit4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Kolto Recovery II
            _builder.Create(RecipeType.InstructionKoltoRecovery2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_kolt2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Resuscitation II
            _builder.Create(RecipeType.InstructionResuscitation2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_resusc2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Stasis Field II
            _builder.Create(RecipeType.InstructionStasisField2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_stasisf2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Combat Enhancement II
            _builder.Create(RecipeType.InstructionCombatEnhancement2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_combenh2")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);

            // Shielding III
            _builder.Create(RecipeType.InstructionShielding3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shielding3")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("ruby", 1)
                .Component("elec_imperfect", 2)
                .Component("katrium", 1);
        }
        private void Tier5()
        {
            // Doublehand V
            _builder.Create(RecipeType.InstructionDoublehand5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_doublehand5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Poison Stab III
            _builder.Create(RecipeType.InstructionPoisonStab3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_pstab3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Crescent Moon III
            _builder.Create(RecipeType.InstructionCrescentMoon3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_cmoon3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Skewer III
            _builder.Create(RecipeType.InstructionSkewer3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_skewer3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Spinning Whirl III
            _builder.Create(RecipeType.InstructionSpinningWhirl3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_spinwhirl3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Inner Strength II
            _builder.Create(RecipeType.InstructionInnerStrength2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_innstr2")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Chi III
            _builder.Create(RecipeType.InstructionChi3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_chi3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Electric Fist III
            _builder.Create(RecipeType.InstructionElectricFist3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_elecfist3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Slam III
            _builder.Create(RecipeType.InstructionSlam3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_slam3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Precision Aim II
            _builder.Create(RecipeType.InstructionPrecisionAim2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_precaim2")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Quick Draw III
            _builder.Create(RecipeType.InstructionQuickDraw3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_quickdraw3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Explosive Toss III
            _builder.Create(RecipeType.InstructionExplosiveToss3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_exptoss3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Tranquilizer Shot III
            _builder.Create(RecipeType.InstructionTranquilizerShot3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_tranqshot3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Concussion Grenade III
            _builder.Create(RecipeType.InstructionConcussionGrenade3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_concgren3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Flashbang Grenade III
            _builder.Create(RecipeType.InstructionFlashbangGrenade3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flashgren3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Adhesive Grenade III
            _builder.Create(RecipeType.InstructionAdhesiveGrenade3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_adhgren3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Flamethrower III
            _builder.Create(RecipeType.InstructionFlamethrower3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_flamethrow3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Wrist Rocket III
            _builder.Create(RecipeType.InstructionWristRocket3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_wristrck3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Med Kit V
            _builder.Create(RecipeType.InstructionMedKit5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_medkit5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Kolto Recovery III
            _builder.Create(RecipeType.InstructionKoltoRecovery3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_kolt3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Resuscitation III
            _builder.Create(RecipeType.InstructionResuscitation3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_resusc3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Stasis Field III
            _builder.Create(RecipeType.InstructionStasisField3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_stasisf3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Combat Enhancement III
            _builder.Create(RecipeType.InstructionCombatEnhancement3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_combenh3")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Shielding IV
            _builder.Create(RecipeType.InstructionShielding4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_shielding4")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);

            // Infusion II
            _builder.Create(RecipeType.InstructionInfusion2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidInstruction)
                .Resref("id_infusion2")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("emerald", 1)
                .Component("elec_high", 2)
                .Component("zinsiam", 1);
        }
    }
}

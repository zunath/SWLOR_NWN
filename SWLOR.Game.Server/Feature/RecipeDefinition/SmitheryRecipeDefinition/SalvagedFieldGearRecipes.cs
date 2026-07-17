using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    // Field gear crafted from salvage recovered off the CZ-220 Breaker Yard rare elite droids.
    // Each recipe is unlocked from a dropped blueprint and requires an encounter-specific
    // salvage component plus common materials.
    public class SalvagedFieldGearRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Reactor-Forged Plating (chest)
            _builder.Create(RecipeType.SalvagedReactorPlate, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("reactor_plate")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("reactor_core", 1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2);

            // Piston-Driven Gauntlets (gloves)
            _builder.Create(RecipeType.SalvagedPistonGauntlet, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("piston_gaunt")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("crusher_piston", 1)
                .Component("elec_good", 3)
                .Component("fiberp_good", 2);

            // Siege Optics Visor (helmet)
            _builder.Create(RecipeType.SalvagedSiegeOptics, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("siege_optics")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("targeting_lens", 1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2);

            // Precision Optic (helmet) - Czerka Arms Test Range
            _builder.Create(RecipeType.SalvagedPrecisionOptic, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("precision_optic")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("targeting_mod", 1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2);

            // Detonite Knuckle (gloves) - Czerka Arms Test Range
            _builder.Create(RecipeType.SalvagedDetoniteKnuckle, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("detonite_knuck")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("detonite_chg", 1)
                .Component("elec_good", 3)
                .Component("fiberp_good", 2);

            // Jammer Mesh (chest) - Czerka Arms Test Range
            _builder.Create(RecipeType.SalvagedJammerMesh, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("jammer_mesh")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("signal_disr", 1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2);

            // Fight Club Backrooms
            _builder.Create(RecipeType.SalvagedPitCestus, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("pit_cestus").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("arena_token", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedDuelistVest, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("duel_vest").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("spent_charge", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedCharmCowl, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("charm_cowl").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("hex_focus", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Dathomir Grotto Apex Den
            _builder.Create(RecipeType.SalvagedFangGauntlet, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("fang_gaunt").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("alpha_fang", 1).Component("lth_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedRidgebonePlate, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("ridge_plate").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("spine_quill", 1).Component("lth_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedRiteCrown, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("rite_crown").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("spirit_totem", 1).Component("lth_good", 4).Component("fiberp_good", 2);


            // Veles Militia Annex
            _builder.Create(RecipeType.SalvagedInvictusGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("invictuscr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("invictuscm", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedRuptorVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("ruptorvanecr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ruptorvanecm", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedBlackoutCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("blackoutwrdcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("blackoutwrdcm", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Dantooine Jedi Enclave Trial Halls
            _builder.Create(RecipeType.SalvagedSabraeGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("sabraetrialcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("sabraetrialcm", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedSentinelVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("enclavesentlcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("enclavesentlcm", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedCycloneCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("cycloneadptcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("cycloneadptcm", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Korriban Forge Caverns
            _builder.Create(RecipeType.SalvagedForgeGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("forgewrightcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("forgewrightcm", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedFlameVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("flameweavercr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("flameweavercm", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedBaneCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("banecallercr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("banecallercm", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Anchorhead Canyon Range
            _builder.Create(RecipeType.SalvagedCanyonGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("canyonbulwrkcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("canyonbulwrkcm", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedDeadeyeVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("dunedeadeyecr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("dunedeadeyecm", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedDeadHandCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("deadhandzephcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("deadhandzephcm", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Hutlar Qion Test Site
            _builder.Create(RecipeType.SalvagedFlurryGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("flurrychampcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("flurrychampcm", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedThermalVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("thermlancercr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("thermlancercm", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedBarrierCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("barrieroversecr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("barrieroversecm", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Korriban Sith Crypt Depths
            _builder.Create(RecipeType.SalvagedCryptGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("cryptwardencr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("cryptwardencm", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedHungerVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("markahungercr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("markahungercm", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedEclipseCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("eclipseshadecr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("eclipseshadecm", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Viscara Republic Engineering Bunker
            _builder.Create(RecipeType.SalvagedBunkerGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("bunkerbreakcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("bunkerbreakcm", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedBeaconVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("beaconmarkscr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("beaconmarkscm", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedDecurionCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("decurioncmdcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("decurioncmdcm", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Dantooine Medical Sublevel
            _builder.Create(RecipeType.SalvagedTriageGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("triagewardencr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("triagewardencm", 1).Component("elec_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedChemVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("chemslingercr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("chemslingercm", 1).Component("elec_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedConduitCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("conduitmatrncr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("conduitmatrncm", 1).Component("elec_good", 4).Component("fiberp_good", 2);

            // Dathomir Tarn Jungle Preserve
            _builder.Create(RecipeType.SalvagedApexGauntlets, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove).Resref("tarnapexmawcr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("tarnapexmawcm", 1).Component("lth_good", 3).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedQuillVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet).Resref("quillstalkercr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("quillstalkercm", 1).Component("lth_good", 4).Component("fiberp_good", 2);
            _builder.Create(RecipeType.SalvagedRhydelCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic).Resref("rhydelalphacr").Level(50).Quantity(1).RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("rhydelalphacm", 1).Component("lth_good", 4).Component("fiberp_good", 2);
        }
    }
}

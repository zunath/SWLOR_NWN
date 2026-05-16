using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class FirstAidCombatUpgradeTests
{
    [Test]
    public void FirstAidBibleManifest_ContainsBatch()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText((root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv").FullName);
        var perkNames = new[]
        {
            "Med Kit I", "Treatment Kit I", "Medical Injector Rig I", "Emergency Sealant I",
            "Kolto Mist I", "Resuscitation I", "Treatment Kit II", "Med Kit II",
            "Infusion I", "Kolto Mist II", "Resuscitation II", "Medical Injector Rig II",
            "Med Kit III", "Treatment Kit III", "Emergency Triage", "Infusion II", "Med Kit IV",
            "Adrenal Stim I", "Shielding I", "Coagulant I", "Adrenal Stim II",
            "Pain Suppressant I", "Antitoxin I", "Field Pharmacist I", "Shielding II",
            "Focus Stim I", "Adrenal Stim III", "Pain Suppressant II", "Field Pharmacist II",
            "Coagulant II", "Shielding III", "Focus Stim II", "Field Pharmacist III",
            "Emergency Cocktail"
        };

        foreach (var perkName in perkNames)
        {
            manifest.Should().Contain($"\"{perkName}\"");
        }
    }

    [Test]
    public void FirstAidTraumaMedicPerkLevels_MatchCombatBible()
    {
        var perks = BuildPerksWithout2daLookup(
            new FirstAidTraumaMedicPerkDefinition(),
            "MedKit", "TreatmentKit", "MedicalInjectorRig", "EmergencySealant",
            "KoltoMist", "Resuscitation", "Infusion", "EmergencyTriage");

        AssertPerkLevel(perks[PerkType.MedKit], "Med Kit", 1, 2, null, FeatType.MedKit1,
            "Restores 10% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.TreatmentKit], "Treatment Kit", 1, 2, 5, FeatType.TreatmentKit1,
            "Removes Bleed and Poison from a single target. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.MedicalInjectorRig], "Medical Injector Rig", 1, 3, 8, null,
            "Med Kit, Kolto Mist, Emergency Triage, and Infusion healing is increased by 10%.",
            (StatType.FirstAidMedicalHealingPercentAdjustment, 10));
        AssertPerkLevel(perks[PerkType.EmergencySealant], "Emergency Sealant", 1, 3, 12, FeatType.EmergencySealant1,
            "Stops Bleed or Burn on one target and grants HP regeneration equal to 2% of the target's maximum HP plus WIL scaling every 3 seconds for 12 seconds. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.KoltoMist], "Kolto Mist", 1, 3, 15, FeatType.KoltoMist1,
            "Restores HP over time to nearby allies within 3m for 12 seconds. Total healing equals 7% of each target's maximum HP plus WIL scaling. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.Resuscitation], "Resuscitation", 1, 3, 18, FeatType.Resuscitation1,
            "Revives an unconscious target with 1 HP. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.TreatmentKit], "Treatment Kit", 2, 2, 22, FeatType.TreatmentKit2,
            "Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.MedKit], "Med Kit", 2, 4, 25, FeatType.MedKit2,
            "Restores 20% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.Infusion], "Infusion", 1, 3, 28, FeatType.Infusion1,
            "Grants a single target regeneration, healing 3% of maximum HP plus WIL scaling every 3 seconds for 15 seconds. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.KoltoMist], "Kolto Mist", 2, 4, 30, FeatType.KoltoMist2,
            "Restores HP over time to nearby allies within 3m for 12 seconds. Total healing equals 12% of each target's maximum HP plus WIL scaling. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.Resuscitation], "Resuscitation", 2, 3, 35, FeatType.Resuscitation2,
            "Revives an unconscious target with 20% HP plus WIL scaling. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.MedicalInjectorRig], "Medical Injector Rig", 2, 3, 38, null,
            "Med Kit, Kolto Mist, Emergency Triage, and Infusion healing is increased by 20%.",
            (StatType.FirstAidMedicalHealingPercentAdjustment, 20));
        AssertPerkLevel(perks[PerkType.MedKit], "Med Kit", 3, 4, 40, FeatType.MedKit3,
            "Restores 28% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.TreatmentKit], "Treatment Kit", 3, 4, 42, FeatType.TreatmentKit3,
            "Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target and grants 50% Fire, Poison, Electrical, Ice, and Trauma resistance for 8 seconds.");
        AssertPerkLevel(perks[PerkType.EmergencyTriage], "Emergency Triage", 1, 4, 45, FeatType.EmergencyTriage1,
            "Restores 18% of the target's maximum HP plus WIL scaling instantly. Healing is doubled if the target is below 35% HP. Consumes extra medical supplies.");
        AssertPerkLevel(perks[PerkType.Infusion], "Infusion", 2, 3, 48, FeatType.Infusion2,
            "Grants a single target regeneration, healing 5% of maximum HP plus WIL scaling every 3 seconds for 15 seconds. Consumes medical supplies.");
        AssertPerkLevel(perks[PerkType.MedKit], "Med Kit", 4, 5, 50, FeatType.MedKit4,
            "Restores 36% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies.");
    }

    [Test]
    public void FirstAidCombatPharmacologyPerkLevels_MatchCombatBible()
    {
        var perks = BuildPerksWithout2daLookup(
            new FirstAidCombatPharmacologyPerkDefinition(),
            "AdrenalStim", "Shielding", "Coagulant", "PainSuppressant",
            "Antitoxin", "FieldPharmacist", "FocusStim", "EmergencyCocktail");

        AssertPerkLevel(perks[PerkType.AdrenalStim], "Adrenal Stim", 1, 2, null, FeatType.AdrenalStim1,
            "Restores 10% of maximum STM and restores 1 STM every 3 seconds for 12 seconds. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.Shielding], "Shielding", 1, 2, 5, FeatType.Shielding1,
            "Reduces physical and force damage taken by 5% for 3 minutes. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.Coagulant], "Coagulant", 1, 3, 8, FeatType.Coagulant1,
            "Grants 50% Bleed resistance and 10% resistance to incoming physical damage over time effects for 2 minutes. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.AdrenalStim], "Adrenal Stim", 2, 3, 12, FeatType.AdrenalStim2,
            "Restores 18% of maximum STM and restores 1 STM every 3 seconds for 12 seconds. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.PainSuppressant], "Pain Suppressant", 1, 3, 15, FeatType.PainSuppressant1,
            "Grants temporary HP equal to 10% of the target's maximum HP plus WIL scaling and 10% damage reduction for 18 seconds. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.Antitoxin], "Antitoxin", 1, 3, 18, FeatType.Antitoxin1,
            "Grants 50% Poison and Disease resistance for 2 minutes and removes one Poison or Toxin effect. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.FieldPharmacist], "Field Pharmacist", 1, 2, 22, null,
            "Stim pack effects last 15% longer and have a 10% chance not to consume the stim pack.",
            (StatType.StimPackDurationPercentAdjustment, 15));
        AssertPerkLevel(perks[PerkType.Shielding], "Shielding", 2, 4, 25, FeatType.Shielding2,
            "Reduces physical and force damage taken by 8% for 3 minutes. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.FocusStim], "Focus Stim", 1, 3, 28, FeatType.FocusStim1,
            "Increases physical and Force ability Accuracy by 5% for 2 minutes. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.AdrenalStim], "Adrenal Stim", 3, 4, 30, FeatType.AdrenalStim3,
            "Restores 25% of maximum STM and restores 1 STM every 3 seconds for 12 seconds. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.PainSuppressant], "Pain Suppressant", 2, 3, 35, FeatType.PainSuppressant2,
            "Grants temporary HP equal to 15% of the target's maximum HP plus WIL scaling and 15% damage reduction for 18 seconds. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.FieldPharmacist], "Field Pharmacist", 2, 3, 38, null,
            "Stim pack effects last 25% longer and have a 20% chance not to consume the stim pack.",
            (StatType.StimPackDurationPercentAdjustment, 25));
        AssertPerkLevel(perks[PerkType.Coagulant], "Coagulant", 2, 4, 40, FeatType.Coagulant2,
            "Grants Bleed immunity and 20% resistance to physical damage over time effects for 2 minutes. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.Shielding], "Shielding", 3, 4, 42, FeatType.Shielding3,
            "Reduces physical and force damage taken by 11% for 3 minutes. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.FocusStim], "Focus Stim", 2, 4, 45, FeatType.FocusStim2,
            "Increases physical and Force ability Accuracy by 8% for 2 minutes. Consumes a stim pack.");
        AssertPerkLevel(perks[PerkType.FieldPharmacist], "Field Pharmacist", 3, 3, 48, null,
            "Stim pack effects last 35% longer and have a 30% chance not to consume the stim pack.",
            (StatType.StimPackDurationPercentAdjustment, 35));
        AssertPerkLevel(perks[PerkType.EmergencyCocktail], "Emergency Cocktail", 1, 5, 50, FeatType.EmergencyCocktail1,
            "Restores 25% of maximum STM, restores 1 STM every 3 seconds, grants temporary HP equal to 15% of maximum HP plus WIL scaling, reduces damage taken by 15%, grants 50% Poison and Disease resistance, and removes one Poison or Toxin effect for 18 seconds. Consumes extra stim packs.");
    }

    [Test]
    public void FirstAidAbilities_MatchCombatBible()
    {
        var medKit = new MedKitAbilityDefinition().BuildAbilities();
        AssertAbility(medKit[FeatType.MedKit1], "Med Kit I", 1, RecastGroup.MedKit, 6f, 1.5f, 4, "med_supplies", 1, false, true);
        AssertAbility(medKit[FeatType.MedKit2], "Med Kit II", 2, RecastGroup.MedKit, 6f, 1.5f, 6, "med_supplies", 1, false, true);
        AssertAbility(medKit[FeatType.MedKit3], "Med Kit III", 3, RecastGroup.MedKit, 6f, 1.5f, 7, "med_supplies", 1, false, true);
        AssertAbility(medKit[FeatType.MedKit4], "Med Kit IV", 4, RecastGroup.MedKit, 6f, 1.5f, 8, "med_supplies", 1, false, true);

        var treatmentKit = new TreatmentKitAbilityDefinition().BuildAbilities();
        AssertAbility(treatmentKit[FeatType.TreatmentKit1], "Treatment Kit I", 1, RecastGroup.TreatmentKit, 8f, 1f, 3, "med_supplies", 1, false, true);
        AssertAbility(treatmentKit[FeatType.TreatmentKit2], "Treatment Kit II", 2, RecastGroup.TreatmentKit, 8f, 1f, 4, "med_supplies", 1, false, true);
        AssertAbility(treatmentKit[FeatType.TreatmentKit3], "Treatment Kit III", 3, RecastGroup.TreatmentKit, 18f, 1f, 5, null, 0, false, true);

        AssertAbility(new EmergencySealantAbilityDefinition().BuildAbilities()[FeatType.EmergencySealant1], "Emergency Sealant I", 1, RecastGroup.EmergencySealant, 18f, 1f, 4, "med_supplies", 1, false, true);

        var koltoMist = new KoltoMistAbilityDefinition().BuildAbilities();
        AssertAbility(koltoMist[FeatType.KoltoMist1], "Kolto Mist I", 1, RecastGroup.KoltoMist, 30f, 1.5f, 6, "med_supplies", 1, true, false);
        AssertAbility(koltoMist[FeatType.KoltoMist2], "Kolto Mist II", 2, RecastGroup.KoltoMist, 30f, 1.5f, 7, "med_supplies", 1, true, false);

        var resuscitation = new ResuscitationAbilityDefinition().BuildAbilities();
        AssertAbility(resuscitation[FeatType.Resuscitation1], "Resuscitation I", 1, RecastGroup.Resuscitation, 180f, 4f, 10, "med_supplies", 1, false, true);
        AssertAbility(resuscitation[FeatType.Resuscitation2], "Resuscitation II", 2, RecastGroup.Resuscitation, 180f, 4f, 10, "med_supplies", 1, false, true);

        var infusion = new InfusionAbilityDefinition().BuildAbilities();
        AssertAbility(infusion[FeatType.Infusion1], "Infusion I", 1, RecastGroup.Infusion, 45f, 1f, 6, "med_supplies", 1, false, true);
        AssertAbility(infusion[FeatType.Infusion2], "Infusion II", 2, RecastGroup.Infusion, 45f, 1f, 8, "med_supplies", 1, false, true);

        AssertAbility(new EmergencyTriageAbilityDefinition().BuildAbilities()[FeatType.EmergencyTriage1], "Emergency Triage", 1, RecastGroup.EmergencyTriage, 45f, 0f, 8, "med_supplies", 2, false, true);

        var adrenal = new AdrenalStimAbilityDefinition().BuildAbilities();
        AssertAbility(adrenal[FeatType.AdrenalStim1], "Adrenal Stim I", 1, RecastGroup.AdrenalStim, 120f, 1f, null, "stim_pack", 1, false, true, true);
        AssertAbility(adrenal[FeatType.AdrenalStim2], "Adrenal Stim II", 2, RecastGroup.AdrenalStim, 120f, 1f, null, "stim_pack", 1, false, true, true);
        AssertAbility(adrenal[FeatType.AdrenalStim3], "Adrenal Stim III", 3, RecastGroup.AdrenalStim, 120f, 1f, null, "stim_pack", 1, false, true, true);

        var shielding = new ShieldingAbilityDefinition().BuildAbilities();
        AssertAbility(shielding[FeatType.Shielding1], "Shielding I", 1, RecastGroup.Shielding, 30f, 1f, 3, "stim_pack", 1, false, true, true);
        AssertAbility(shielding[FeatType.Shielding2], "Shielding II", 2, RecastGroup.Shielding, 30f, 1f, 4, "stim_pack", 1, false, true, true);
        AssertAbility(shielding[FeatType.Shielding3], "Shielding III", 3, RecastGroup.Shielding, 30f, 1f, 5, "stim_pack", 1, false, true, true);

        var coagulant = new CoagulantAbilityDefinition().BuildAbilities();
        AssertAbility(coagulant[FeatType.Coagulant1], "Coagulant I", 1, RecastGroup.Coagulant, 45f, 1f, 3, "stim_pack", 1, false, true, true);
        AssertAbility(coagulant[FeatType.Coagulant2], "Coagulant II", 2, RecastGroup.Coagulant, 45f, 1f, 4, "stim_pack", 1, false, true, true);

        var pain = new PainSuppressantAbilityDefinition().BuildAbilities();
        AssertAbility(pain[FeatType.PainSuppressant1], "Pain Suppressant I", 1, RecastGroup.PainSuppressant, 60f, 1f, 5, "stim_pack", 1, false, true, true);
        AssertAbility(pain[FeatType.PainSuppressant2], "Pain Suppressant II", 2, RecastGroup.PainSuppressant, 60f, 1f, 6, "stim_pack", 1, false, true, true);

        AssertAbility(new AntitoxinAbilityDefinition().BuildAbilities()[FeatType.Antitoxin1], "Antitoxin I", 1, RecastGroup.Antitoxin, 45f, 1f, 3, "stim_pack", 1, false, true, true);

        var focus = new FocusStimAbilityDefinition().BuildAbilities();
        AssertAbility(focus[FeatType.FocusStim1], "Focus Stim I", 1, RecastGroup.FocusStim, 45f, 1f, 4, "stim_pack", 1, false, true, true);
        AssertAbility(focus[FeatType.FocusStim2], "Focus Stim II", 2, RecastGroup.FocusStim, 45f, 1f, 5, "stim_pack", 1, false, true, true);

        AssertAbility(new EmergencyCocktailAbilityDefinition().BuildAbilities()[FeatType.EmergencyCocktail1], "Emergency Cocktail", 1, RecastGroup.EmergencyCocktail, 300f, 1f, 8, "stim_pack", 2, false, true, true);
    }

    [Test]
    public void FirstAidStatuses_MatchCombatBible()
    {
        AssertStatusStat(new Shielding1StatusEffect(), StatType.PhysicalDamageTakenPercentAdjustment, -5);
        AssertStatusStat(new Shielding1StatusEffect(), StatType.ForceDamageTakenPercentAdjustment, -5);
        AssertStatusStat(new Shielding2StatusEffect(), StatType.PhysicalDamageTakenPercentAdjustment, -8);
        AssertStatusStat(new Shielding2StatusEffect(), StatType.ForceDamageTakenPercentAdjustment, -8);
        AssertStatusStat(new Shielding3StatusEffect(), StatType.PhysicalDamageTakenPercentAdjustment, -11);
        AssertStatusStat(new Shielding3StatusEffect(), StatType.ForceDamageTakenPercentAdjustment, -11);

        AssertStatusStat(new Coagulant1StatusEffect(), StatType.TraumaResistance, 50);
        AssertStatusStat(new Coagulant1StatusEffect(), StatType.PhysicalDamageOverTimeTakenPercentAdjustment, -10);
        AssertStatusStat(new Coagulant2StatusEffect(), StatType.TraumaResistance, 100);
        AssertStatusStat(new Coagulant2StatusEffect(), StatType.PhysicalDamageOverTimeTakenPercentAdjustment, -20);

        AssertStatusStat(new PainSuppressant1StatusEffect(), StatType.DamageTakenPercentAdjustment, -10);
        AssertStatusStat(new PainSuppressant2StatusEffect(), StatType.DamageTakenPercentAdjustment, -15);
        AssertStatusStat(new FocusStim1StatusEffect(), StatType.AccuracyPercentAdjustment, 5);
        AssertStatusStat(new FocusStim2StatusEffect(), StatType.AccuracyPercentAdjustment, 8);

        new Antitoxin1StatusEffect().StatGroup.Resists[ResistanceType.Poison].Should().Be(50);

        var treatmentKit3 = new TreatmentKit3StatusEffect();
        treatmentKit3.StatGroup.Resists[ResistanceType.Fire].Should().Be(50);
        treatmentKit3.StatGroup.Resists[ResistanceType.Poison].Should().Be(50);
        treatmentKit3.StatGroup.Resists[ResistanceType.Electrical].Should().Be(50);
        treatmentKit3.StatGroup.Resists[ResistanceType.Ice].Should().Be(50);
        treatmentKit3.StatGroup.Resists[ResistanceType.Trauma].Should().Be(50);
    }

    [Test]
    public void FirstAidSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();
        var treatmentKit = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "TreatmentKitAbilityDefinition.cs").FullName);
        treatmentKit.Should().Contain("StatusEffectCleanseType.TreatmentKit2");
        treatmentKit.Should().Contain("RemoveCleanseableStatusEffects(friendly, StatusEffectCleanseType.TreatmentKit2, false)");

        var pain = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "PainSuppressantAbilityDefinition.cs").FullName);
        pain.Should().Contain("AbilityEffectScaling.ApplyTemporaryHPPercent(activator, target, percent, durationSeconds)");
        pain.Should().NotContain("HealPercent(activator, friendly");

        var cocktail = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "EmergencyCocktailAbilityDefinition.cs").FullName);
        cocktail.Should().Contain("AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, 15, duration)");
        cocktail.Should().Contain("new[] { typeof(PoisonStatusEffect), typeof(ToxinStatusEffect) }");
    }

    [Test]
    public void FirstAidFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");
        var feats = new[]
        {
            (FeatType.MedKit1, "ife_mdkt1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.TreatmentKit1, "ife_trtmntkt1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.EmergencySealant1, "ife_mrgncyslnt1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.KoltoMist1, "ife_kltmst1", "P", "0x01", "0", "sphere", "3", "****", "17", "1"),
            (FeatType.Resuscitation1, "ife_rsscttn1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.TreatmentKit2, "ife_trtmntkt2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.MedKit2, "ife_mdkt2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Infusion1, "ife_nfsn1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.KoltoMist2, "ife_kltmst2", "P", "0x01", "0", "sphere", "3", "****", "17", "1"),
            (FeatType.Resuscitation2, "ife_rsscttn2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.MedKit3, "ife_mdkt3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.TreatmentKit3, "ife_trtmntkt3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.EmergencyTriage1, "ife_mrgncytrg1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Infusion2, "ife_nfsn2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.MedKit4, "ife_mdkt4", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.AdrenalStim1, "ife_drnlstm1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Shielding1, "ife_shldng1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Coagulant1, "ife_cglnt1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.AdrenalStim2, "ife_drnlstm2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.PainSuppressant1, "ife_pnspprssnt1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Antitoxin1, "ife_nttxn1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Shielding2, "ife_shldng2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.FocusStim1, "ife_focstm1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.AdrenalStim3, "ife_drnlstm3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.PainSuppressant2, "ife_pnspprssnt2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Coagulant2, "ife_cglnt2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Shielding3, "ife_shldng3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.FocusStim2, "ife_focstm2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.EmergencyCocktail1, "ife_mrgncyccktl1", "M", "0x03", "0", "****", "****", "****", "****", "****")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags, targetSelf) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be(targetShape);
            abilityRow["TargetSizeX"].Should().Be(targetSizeX);
            abilityRow["TargetSizeY"].Should().Be(targetSizeY);
            abilityRow["TargetFlags"].Should().Be(targetFlags);
            featRow["TARGETSELF"].Should().Be(targetSelf);
        }
    }

    private static void AssertPerkLevel(
        PerkDetail perk,
        string name,
        int level,
        int price,
        int? skillRank,
        FeatType? grantedFeat,
        string description,
        params (StatType Stat, int Value)[] statBonuses)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.FirstAid);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        perkLevel.Requirements.OfType<PerkRequirementCharacterType>().Should().BeEmpty();

        if (skillRank.HasValue)
            AssertSkillRequirement(perkLevel, SkillType.FirstAid, skillRank.Value);
        else
            perkLevel.Requirements.OfType<PerkRequirementSkill>().Should().BeEmpty();

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statBonuses.Length > 0)
        {
            perkLevel.StatBonuses.Should().HaveCount(statBonuses.Length);
            foreach (var (stat, value) in statBonuses)
            {
                AssertStatBonus(perkLevel, stat, value);
            }
        }
        else
        {
            perkLevel.StatBonuses.Should().BeEmpty();
        }
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int? staminaCost,
        string itemResref,
        int itemQuantity,
        bool isArea,
        bool requiresTarget,
        bool expectsStimPreserve = false)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.FirstAid);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(AbilityActivationType.Casted);
        ability.IsHostileAbility.Should().BeFalse();
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.IsSingleTargetAbility.Should().Be(!isArea);
        ability.BreaksStealth.Should().BeTrue();

        if (requiresTarget)
            ability.CustomValidation.Should().NotBeNull();
        else
            ability.CustomValidation.Should().BeNull();

        if (staminaCost.HasValue)
        {
            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredSTM
                .Should()
                .Be(staminaCost.Value);
        }
        else
        {
            ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
        }

        ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();

        if (itemResref != null)
        {
            var itemRequirement = ability.Requirements
                .OfType<AbilityRequirementItem>()
                .Should()
                .ContainSingle()
                .Which;

            itemRequirement.ItemResref.Should().Be(itemResref);
            itemRequirement.Quantity.Should().Be(itemQuantity);

            if (expectsStimPreserve)
            {
                itemRequirement.PreservePerkType.Should().Be(PerkType.FieldPharmacist);
                itemRequirement.PreserveChancePerLevel.Should().Be(10);
            }
        }
        else
        {
            ability.Requirements.OfType<AbilityRequirementItem>().Should().BeEmpty();
        }
    }

    private static void AssertSkillRequirement(PerkLevel level, SkillType skill, int rank)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementSkill>()
            .Should()
            .ContainSingle()
            .Which;

        requirement.Type.Should().Be(skill);
        requirement.RequiredRank.Should().Be(rank);
    }

    private static void AssertStatBonus(PerkLevel level, StatType statType, int value)
    {
        level.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == statType)
            .Which
            .Calculate(0)
            .Should()
            .Be(value);
    }

    private static void AssertStatusStat(StatusEffectBase statusEffect, StatType statType, int value)
    {
        statusEffect.StatGroup.Stats[statType].Should().Be(value);
    }

    private static Dictionary<PerkType, PerkDetail> BuildPerksWithout2daLookup<T>(T definition, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            typeof(T)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(T)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "swlor2_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}

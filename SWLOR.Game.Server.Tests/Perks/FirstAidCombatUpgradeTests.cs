using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
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
            "Med Kit I", "Treatment Kit I", "Medical Injector Rig I", "Emergency Sealant",
            "Kolto Mist I", "Resuscitation I", "Treatment Kit II", "Med Kit II",
            "Infusion I", "Kolto Mist II", "Resuscitation II", "Medical Injector Rig II",
            "Med Kit III", "Treatment Kit III", "Emergency Triage", "Infusion II", "Med Kit IV",
            "Adrenal Stim I", "Shielding I", "Coagulant I", "Adrenal Stim II",
            "Pain Suppressant I", "Antitoxin", "Field Pharmacist I", "Shielding II",
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
    public void FirstAidAbilities_MatchCombatBible()
    {
        var medKit = new MedKitAbilityDefinition().BuildAbilities();
        AssertAbility(medKit[FeatType.MedKit1], "Med Kit I", 1, RecastGroup.MedKit, 6f, 1.5f, 4, "med_supplies", 1, false, true);
        AssertAbility(medKit[FeatType.MedKit2], "Med Kit II", 2, RecastGroup.MedKit, 6f, 1.5f, 6, "med_supplies", 1, false, true);
        AssertAbility(medKit[FeatType.MedKit3], "Med Kit III", 3, RecastGroup.MedKit, 6f, 1.5f, 7, "med_supplies", 1, false, true);
        AssertAbility(medKit[FeatType.MedKit4], "Med Kit IV", 4, RecastGroup.MedKit, 6f, 1.5f, 8, "med_supplies", 1, false, true);

        var treatmentKit = new TreatmentKitAbilityDefinition().BuildAbilities();
        AssertAbility(treatmentKit[FeatType.TreatmentKit1], "Treatment Kit I", 1, RecastGroup.TreatmentKit, 6f, 1f, 3, "med_supplies", 1, false, true);
        AssertAbility(treatmentKit[FeatType.TreatmentKit2], "Treatment Kit II", 2, RecastGroup.TreatmentKit, 6f, 1f, 4, "med_supplies", 1, false, true);
        AssertAbility(treatmentKit[FeatType.TreatmentKit3], "Treatment Kit III", 3, RecastGroup.TreatmentKit, 12f, 1f, 5, null, 0, false, true);

        var koltoMist = new KoltoMistAbilityDefinition().BuildAbilities();
        AssertAbility(koltoMist[FeatType.KoltoMist1], "Kolto Mist I", 1, RecastGroup.KoltoMist, 18f, 1.5f, 6, "med_supplies", 1, true, false, maxRange: 15f, expectsCustomValidation: true);
        AssertAbility(koltoMist[FeatType.KoltoMist2], "Kolto Mist II", 2, RecastGroup.KoltoMist, 18f, 1.5f, 7, "med_supplies", 1, true, false, maxRange: 15f, expectsCustomValidation: true);

        var resuscitation = new ResuscitationAbilityDefinition().BuildAbilities();
        AssertAbility(resuscitation[FeatType.Resuscitation1], "Resuscitation I", 1, RecastGroup.Resuscitation, 60f, 4f, 10, "med_supplies", 1, false, true);
        AssertAbility(resuscitation[FeatType.Resuscitation2], "Resuscitation II", 2, RecastGroup.Resuscitation, 60f, 4f, 10, "med_supplies", 1, false, true);

        var infusion = new InfusionAbilityDefinition().BuildAbilities();
        AssertAbility(infusion[FeatType.Infusion1], "Infusion I", 1, RecastGroup.Infusion, 24f, 1f, 6, "med_supplies", 1, false, true);
        AssertAbility(infusion[FeatType.Infusion2], "Infusion II", 2, RecastGroup.Infusion, 24f, 1f, 8, "med_supplies", 1, false, true);

        AssertAbility(new EmergencyTriageAbilityDefinition().BuildAbilities()[FeatType.EmergencyTriage1], "Emergency Triage", 1, RecastGroup.EmergencyTriage, 24f, 0f, 8, "med_supplies", 2, false, true, maxRange: 15f);

        var adrenal = new AdrenalStimAbilityDefinition().BuildAbilities();
        AssertAbility(adrenal[FeatType.AdrenalStim1], "Adrenal Stim I", 1, RecastGroup.AdrenalStim, 45f, 1f, null, "stim_pack", 1, false, true, true);
        AssertAbility(adrenal[FeatType.AdrenalStim2], "Adrenal Stim II", 2, RecastGroup.AdrenalStim, 45f, 1f, null, "stim_pack", 1, false, true, true);
        AssertAbility(adrenal[FeatType.AdrenalStim3], "Adrenal Stim III", 3, RecastGroup.AdrenalStim, 45f, 1f, null, "stim_pack", 1, false, true, true);

        var shielding = new ShieldingAbilityDefinition().BuildAbilities();
        AssertAbility(shielding[FeatType.Shielding1], "Shielding I", 1, RecastGroup.Shielding, 18f, 1f, 3, "stim_pack", 1, false, true, true);
        AssertAbility(shielding[FeatType.Shielding2], "Shielding II", 2, RecastGroup.Shielding, 18f, 1f, 4, "stim_pack", 1, false, true, true);
        AssertAbility(shielding[FeatType.Shielding3], "Shielding III", 3, RecastGroup.Shielding, 18f, 1f, 5, "stim_pack", 1, false, true, true);

        var pain = new PainSuppressantAbilityDefinition().BuildAbilities();
        AssertAbility(pain[FeatType.PainSuppressant1], "Pain Suppressant I", 1, RecastGroup.PainSuppressant, 30f, 1f, 5, "stim_pack", 1, false, true, true);
        AssertAbility(pain[FeatType.PainSuppressant2], "Pain Suppressant II", 2, RecastGroup.PainSuppressant, 30f, 1f, 6, "stim_pack", 1, false, true, true);

        AssertAbility(new AntitoxinAbilityDefinition().BuildAbilities()[FeatType.Antitoxin1], "Antitoxin", 1, RecastGroup.Antitoxin, 24f, 1f, 3, "stim_pack", 1, false, true, true);

        var focus = new FocusStimAbilityDefinition().BuildAbilities();
        AssertAbility(focus[FeatType.FocusStim1], "Focus Stim I", 1, RecastGroup.FocusStim, 24f, 1f, 4, "stim_pack", 1, false, true, true);
        AssertAbility(focus[FeatType.FocusStim2], "Focus Stim II", 2, RecastGroup.FocusStim, 24f, 1f, 5, "stim_pack", 1, false, true, true);

        AssertAbility(new EmergencyCocktailAbilityDefinition().BuildAbilities()[FeatType.EmergencyCocktail1], "Emergency Cocktail", 1, RecastGroup.Capstone, 90f, 1f, 15, "stim_pack", 1, false, true, true);
    }

    [Test]
    public void FirstAidSupplyRequirements_MatchMerchantStackTemplates()
    {
        var root = FindRepositoryRoot();
        var medicalSupplies = ReadItemIdentity(root / "Module" / "uti" / "med_supplies_50.uti.json");
        var stimPack = ReadItemIdentity(root / "Module" / "uti" / "stim_pack_50.uti.json");

        medicalSupplies.TemplateResRef.Should().Be("med_supplies_50");
        medicalSupplies.Tag.Should().Be("med_supplies");

        stimPack.TemplateResRef.Should().Be("stim_pack_50");
        stimPack.Tag.Should().Be("stim_pack");

        var requirementSource = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Service" / "AbilityService" / "AbilityRequirementItem.cs").FullName);
        requirementSource.Should().Contain("GetItemPossessedBy(player, ItemResref)");
        requirementSource.Should().NotContain("GetFirstItemInInventory(player)");
        requirementSource.Should().NotContain("CountItems");
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
        new Coagulant1StatusEffect().PersistsOnLogout.Should().BeFalse();
        new Coagulant2StatusEffect().PersistsOnLogout.Should().BeFalse();

        AssertStatusStat(new PainSuppressant1StatusEffect(), StatType.DamageTakenPercentAdjustment, -10);
        AssertStatusStat(new PainSuppressant2StatusEffect(), StatType.DamageTakenPercentAdjustment, -15);
        AssertStatusStat(new FocusStim1StatusEffect(), StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, 5);
        AssertStatusStat(new FocusStim2StatusEffect(), StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, 8);

        new Antitoxin1StatusEffect().StatGroup.Resists[ResistanceType.Poison].Should().Be(50);
        new DiseaseStatusEffect().ResistanceType.Should().Be(ResistanceType.Poison);
        var emergencyCocktail = new EmergencyCocktailStatusEffect();
        emergencyCocktail.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(-12);
        emergencyCocktail.StatGroup.Resists[ResistanceType.Poison].Should().Be(50);

        var treatmentKit3 = new AilmentResistance3StatusEffect();
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
        var medKit = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "MedKitAbilityDefinition.cs").FullName);
        medKit.Should().Contain("CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid);");

        var treatmentKit = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "TreatmentKitAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        treatmentKit.Should().Contain("StatusEffectCleanseType.TreatmentKit2");
        treatmentKit.Should().Contain("RemoveCleanseableStatusEffects(friendly, StatusEffectCleanseType.TreatmentKit2, false)");
        treatmentKit.Split("GrantFirstAidCombatPointIfApplied(activator, affectedCount);").Length.Should().Be(4);
        treatmentKit.Should().Contain(
            "if (affectedCount > 0)\n" +
            "                CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid);");
        treatmentKit.Split("CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid);").Length.Should().Be(2);

        var resuscitation = File.ReadAllText(
                (root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "ResuscitationAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");
        resuscitation.Should().Contain(
            "DelayCommand(0.1f, () =>\n" +
            "            {\n" +
            "                AbilityEffectScaling.ApplyActivatedScaledHeal(activator, target, 20);",
            "the rank-II heal must run after EffectResurrection settles or the engine silently discards it");

        var pain = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "PainSuppressantAbilityDefinition.cs").FullName);
        pain.Should().Contain("AbilityEffectScaling.ApplyTemporaryHPPercent(activator, target, \"PAIN_SUPPRESSANT\", percent, durationSeconds)");
        pain.Should().NotContain("HealPercent(activator, friendly");

        var koltoMist = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "KoltoMistAbilityDefinition.cs").FullName);
        koltoMist.Should().Contain("private const float HealRadiusMeters = 8f;");
        koltoMist.Should().Contain("private const float Rank1HealPercentPerTick = 1f;");
        koltoMist.Should().Contain("private const float Rank2HealPercentPerTick = 2f;");
        koltoMist.Should().Contain("VisualEffect.Vfx_Fnf_Gas_Explosion_Mind");
        koltoMist.Should().Contain("EffectAreaOfEffect(AreaOfEffect.KoltoMistCloud)");
        koltoMist.Should().NotContain("AreaOfEffect.FogMind",
            "the base game FogMind AoE runs the Mind Fog enter/heartbeat spell scripts; Kolto Mist must use the script-free cloud row");
        koltoMist.Should().NotContain("VisualEffect.Vfx_Dur_Aura_Blue_Light",
            "Kolto Mist needs the persistent blue gas cloud used by the live-server Kolto Bomb, not a body aura");
        koltoMist.Should().Contain("GetIsObjectValid(activator)");
        koltoMist.Should().Contain("GetCurrentHitPoints(activator) <= 0");
        koltoMist.Should().Contain("GetIsObjectValid(GetAreaFromLocation(location))");
        koltoMist.Should().Contain("var applied = ApplyKoltoMistPulse(activator, location, percentPerTick);");
        koltoMist.Should().Contain("if (applied && !combatPointAwarded)");
        koltoMist.Should().Contain("visualEffect: VisualEffect.Vfx_Imp_Head_Heal");
        koltoMist.Should().Contain("StatusEffect.ApplyStatusEffect(");
        koltoMist.Should().Contain("typeof(KoltoMistHealingStatusEffect)");
        koltoMist.Should().Contain("StatusRefreshDurationSeconds");
        koltoMist.Should().NotContain("new KoltoMistHealingStatusEffect(totalPercent, 4)");
        koltoMist.Should().NotContain("HealPercent(");

        var koltoMistStatus = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "KoltoMistHealingStatusEffect.cs").FullName);
        koltoMistStatus.Should().Contain("StatusEffectActivationType.Passive");
        koltoMistStatus.Should().Contain("public override bool SendsApplicationMessage => false;");
        koltoMistStatus.Should().Contain("public override bool SendsWornOffMessage => false;");
        koltoMistStatus.Should().NotContain("protected override void Tick");

        var treatmentAdjustments = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "FirstAidTreatmentAdjustments.cs").FullName);
        treatmentAdjustments.Should().Contain("CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid);");
        treatmentAdjustments.Should().Contain("GrantCombatPointIfApplied(uint activator, bool applied)");
        treatmentAdjustments.Should().Contain("if (!removedAilment)");
        treatmentAdjustments.Should().Contain("StatusEffect.ApplyStatusEffect(source, target, typeof(EmergencySealant1StatusEffect), 30f);");
        treatmentAdjustments.IndexOf("if (!removedAilment)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(treatmentAdjustments.IndexOf("StatusEffect.ApplyStatusEffect(source, target, typeof(EmergencySealant1StatusEffect), 30f);", StringComparison.Ordinal));

        var firstAidSupportFiles = new[]
        {
            "AdrenalStimAbilityDefinition.cs",
            "AntitoxinAbilityDefinition.cs",
            "EmergencyCocktailAbilityDefinition.cs",
            "EmergencyTriageAbilityDefinition.cs",
            "FocusStimAbilityDefinition.cs",
            "InfusionAbilityDefinition.cs",
            "PainSuppressantAbilityDefinition.cs",
            "ResuscitationAbilityDefinition.cs",
            "ShieldingAbilityDefinition.cs"
        };

        foreach (var file in firstAidSupportFiles)
        {
            var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / file).FullName);
            source.Should().Contain(
                "FirstAidTreatmentAdjustments.GrantCombatPoint",
                $"{file} should award First Aid combat points after a successful support application");
        }

        var emergencySealantStatus = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "EmergencySealant1StatusEffect.cs").FullName);
        emergencySealantStatus.Should().Contain("AbilityEffectScaling.ApplyScaledHeal(Source, creature, 4);");

        var cocktail = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "EmergencyCocktailAbilityDefinition.cs").FullName);
        cocktail.Should().Contain("AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, \"EMERGENCY_COCKTAIL\", 12, duration)");
        cocktail.Should().Contain("CapstoneAbility.ActiveDurationSeconds");
        cocktail.Should().Contain("new[] { typeof(PoisonStatusEffect), typeof(ToxinStatusEffect) }");
    }

    [Test]
    public void MedicalInjectorRig_AppliesToAbilityHealingAcrossSkillsButNotDamageDerivedHealing()
    {
        const string rank1Description =
            "All direct, area, and periodic healing caused by your abilities is increased by 5%.";
        const string rank2Description =
            "All direct, area, and periodic healing caused by your abilities is increased by 10%.";

        var root = FindRepositoryRoot();
        var perkSource = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "FirstAidTraumaMedicPerkDefinition.cs").FullName);
        perkSource.Should().Contain($".Description(\"{rank1Description}\")");
        perkSource.Should().Contain($".Description(\"{rank2Description}\")");
        perkSource.Should().Contain(
            ".IncreasesStat(StatType.OutgoingAbilityHealingPercentAdjustment, 5)");
        perkSource.Should().Contain(
            ".IncreasesStat(StatType.OutgoingAbilityHealingPercentAdjustment, 10)");

        Stat.CalculateOutgoingAbilityHealingAmount(100, 5).Should().Be(105);
        Stat.CalculateOutgoingAbilityHealingAmount(101, 10).Should().Be(112);
        Stat.CalculateOutgoingAbilityHealingAmount(100, 0).Should().Be(100);

        var includedSources = new[]
        {
            root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "AbilityEffectScaling.cs",
            root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "FirstAidTreatmentAdjustments.cs",
            root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "MedKitAbilityDefinition.cs",
            root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "RewardAbilityDefinition.cs",
            root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "InnervateAbilityDefinition.cs",
            root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Mimicry" / "WardenOrderTechniqueAbilityDefinition.cs"
        };

        foreach (var sourcePath in includedSources)
        {
            File.ReadAllText(sourcePath.FullName)
                .Should()
                .Contain(
                    "Stat.ApplyOutgoingAbilityHealingAdjustment",
                    $"{Path.GetFileName(sourcePath.FullName)} should apply Medical Injector Rig to eligible ability healing");
        }

        var statSource = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Service" / "Stat.cs").FullName);
        statSource.Should().Contain("BeastMastery.IsPlayerBeast(source)");
        statSource.Should().Contain("GetMaster(source)");

        var excludedSources = new[]
        {
            root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceDrainAbilityDefinition.cs",
            root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "WeaponActiveAbilityDefinitionBase.cs",
            root / "SWLOR.Game.Server" / "Service" / "Combat.cs",
            root / "SWLOR.Game.Server" / "Feature" / "NaturalRegeneration.cs",
            root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "RestStatusEffect.cs"
        };

        foreach (var sourcePath in excludedSources)
        {
            File.ReadAllText(sourcePath.FullName)
                .Should()
                .NotContain(
                    "Stat.ApplyOutgoingAbilityHealingAdjustment",
                    $"{Path.GetFileName(sourcePath.FullName)} should not apply Medical Injector Rig to damage-derived or system healing");
        }
    }

    [Test]
    public void FirstAidHealingVisuals_DoNotPlayStockHealSound()
    {
        var root = FindRepositoryRoot();
        var firstAidDirectory = new DirectoryInfo((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid").FullName);

        foreach (var sourcePath in firstAidDirectory.GetFiles("*.cs"))
        {
            var source = File.ReadAllText(sourcePath.FullName);
            source.Should().NotContain("VisualEffect.Vfx_Imp_Healing_M)");
            source.Should().NotContain("VisualEffect.Vfx_Imp_Healing_M,");
        }

        var treatmentAdjustments = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "FirstAid" / "FirstAidTreatmentAdjustments.cs").FullName);
        treatmentAdjustments.Should().Contain("VisualEffect.Vfx_Imp_Healing_M_Silent");

        var visualEffectEnum = File.ReadAllText((root / "SWLOR.NWN.API" / "NWScript" / "Enum" / "VisualEffect" / "VisualEffect.cs").FullName);
        visualEffectEnum.Should().Contain("Vfx_Imp_Healing_M_Silent = 842");

        var visualEffects = Read2da(root / "SWLOR_Haks" / "sw_2da" / "visualeffects.2da");
        visualEffects[842]["Label"].Should().Be("VFX_IMP_HEALING_M_SILENT");
        visualEffects[842]["Imp_HeadCon_Node"].Should().Be("vim_heal04");
        visualEffects[842]["SoundImpact"].Should().Be("****");
    }

    [Test]
    public void KoltoMistPersistentVfx_IsVisualOnlyGasCloud()
    {
        var root = FindRepositoryRoot();
        var persistentVfx = Read2da(root / "SWLOR_Haks" / "sw_2da" / "vfx_persistent.2da");
        var row = persistentVfx[(int)AreaOfEffect.KoltoMistCloud];

        row["LABEL"].Should().Be("AOE_KOLTO_MIST_CLOUD");
        row["SHAPE"].Should().Be("C");
        row["RADIUS"].Should().Be("8");
        row["ONENTER"].Should().Be("****",
            "the cloud must not run the base game Mind Fog enter script");
        row["ONEXIT"].Should().Be("****");
        row["HEARTBEAT"].Should().Be("****",
            "the cloud must not run the base game Mind Fog heartbeat script");
        row["MODEL01"].Should().Be("vps_fogmind");
        row["MODEL02"].Should().Be("vps_fogmind");
        row["MODEL03"].Should().Be("vps_fogmind");
    }

    [Test]
    public void FirstAidFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var feats = new[]
        {
            (FeatType.MedKit1, "ife_mdkt1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.TreatmentKit1, "ife_trtmntkt1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.KoltoMist1, "ife_kltmst1", "M", "0x3E", "0", "sphere", "8", "****", "1", "****"),
            (FeatType.Resuscitation1, "ife_rsscttn1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.TreatmentKit2, "ife_trtmntkt2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.MedKit2, "ife_mdkt2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Infusion1, "ife_nfsn1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.KoltoMist2, "ife_kltmst2", "M", "0x3E", "0", "sphere", "8", "****", "1", "****"),
            (FeatType.Resuscitation2, "ife_rsscttn2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.MedKit3, "ife_mdkt3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.TreatmentKit3, "ife_trtmntkt3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.EmergencyTriage1, "ife_mrgncytrg1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Infusion2, "ife_nfsn2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.MedKit4, "ife_mdkt4", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.AdrenalStim1, "ife_drnlstm1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Shielding1, "ife_shldng1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.AdrenalStim2, "ife_drnlstm2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.PainSuppressant1, "ife_pnspprssnt1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Antitoxin1, "ife_nttxn1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.Shielding2, "ife_shldng2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.FocusStim1, "ife_focstm1", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.AdrenalStim3, "ife_drnlstm3", "M", "0x03", "0", "****", "****", "****", "****", "****"),
            (FeatType.PainSuppressant2, "ife_pnspprssnt2", "M", "0x03", "0", "****", "****", "****", "****", "****"),
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

            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be(targetShape);
            abilityRow["TargetSizeX"].Should().Be(targetSizeX);
            abilityRow["TargetSizeY"].Should().Be(targetSizeY);
            abilityRow["TargetFlags"].Should().Be(targetFlags);
            featRow["TARGETSELF"].Should().Be(targetSelf);
            featRows.Values.Count(row => row["LABEL"] == featRow["LABEL"])
                .Should()
                .Be(1, $"{featType} should not have stale duplicate feat rows");
        }
    }

    [Test]
    public void FirstAidFeatAndAbilityDescriptions_MatchCombatBible()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "sw_tlk" / "sw_tlk.tlk.json");
        const int CustomTlkOffset = 16777216;
        var descriptions = new[]
        {
            (FeatType.MedKit1, "Restores 10% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies."),
            (FeatType.TreatmentKit1, "Removes Bleed and Poison from a single target. Consumes medical supplies."),
            (FeatType.KoltoMist1, "Deploys a 30-second healing mist cloud at a target location up to 15m away. Allies within 8m heal for 1% of maximum HP plus WIL scaling every 3 seconds. Consumes medical supplies."),
            (FeatType.Resuscitation1, "Revives an unconscious target with 1 HP. Consumes medical supplies."),
            (FeatType.TreatmentKit2, "Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target. Consumes medical supplies."),
            (FeatType.MedKit2, "Restores 20% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies."),
            (FeatType.Infusion1, "Grants a single target regeneration, healing 3% of maximum HP plus WIL scaling every 3 seconds for 30 seconds. Consumes medical supplies."),
            (FeatType.KoltoMist2, "Deploys a 30-second healing mist cloud at a target location up to 15m away. Allies within 8m heal for 2% of maximum HP plus WIL scaling every 3 seconds. Consumes medical supplies."),
            (FeatType.Resuscitation2, "Revives an unconscious target with 20% HP plus WIL scaling. Consumes medical supplies."),
            (FeatType.MedKit3, "Restores 28% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies."),
            (FeatType.TreatmentKit3, "Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target and grants 50% Fire Resistance, 50% Poison Resistance, 50% Electrical Resistance, 50% Ice Resistance, and 50% Trauma Resistance for 30 seconds."),
            (FeatType.EmergencyTriage1, "Restores 18% of the target's maximum HP plus WIL scaling instantly. Can target allies up to 15m away. Healing is doubled if the target is below 35% HP. Consumes extra medical supplies."),
            (FeatType.Infusion2, "Grants a single target regeneration, healing 5% of maximum HP plus WIL scaling every 3 seconds for 30 seconds. Consumes medical supplies."),
            (FeatType.MedKit4, "Restores 36% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies."),
            (FeatType.AdrenalStim1, "Restores 10% of maximum STM and restores 1 STM every 3 seconds for 30 seconds. Consumes a stim pack."),
            (FeatType.Shielding1, "Reduces physical and force damage taken by 5% for 3 minutes. Consumes a stim pack."),
            (FeatType.AdrenalStim2, "Restores 18% of maximum STM and restores 1 STM every 3 seconds for 30 seconds. Consumes a stim pack."),
            (FeatType.PainSuppressant1, "Grants temporary HP equal to 10% of the target's maximum HP plus WIL scaling and 10% damage reduction for 30 seconds. Consumes a stim pack."),
            (FeatType.Antitoxin1, "Grants 50% Poison Resistance for 2 minutes and removes one Poison or Toxin effect. Poison Resistance also weakens Disease and Toxin effects. Consumes a stim pack."),
            (FeatType.Shielding2, "Reduces physical and force damage taken by 8% for 3 minutes. Consumes a stim pack."),
            (FeatType.FocusStim1, "Increases physical and Force ability Accuracy by 5% for 2 minutes. Consumes a stim pack."),
            (FeatType.AdrenalStim3, "Restores 25% of maximum STM and restores 1 STM every 3 seconds for 30 seconds. Consumes a stim pack."),
            (FeatType.PainSuppressant2, "Grants temporary HP equal to 15% of the target's maximum HP plus WIL scaling and 15% damage reduction for 30 seconds. Consumes a stim pack."),
            (FeatType.Shielding3, "Reduces physical and force damage taken by 11% for 3 minutes. Consumes a stim pack."),
            (FeatType.FocusStim2, "Increases physical and Force ability Accuracy by 8% for 2 minutes. Consumes a stim pack."),
            (FeatType.EmergencyCocktail1, "Restores 25% of maximum STM, removes one Poison or Toxin effect, then for 45 seconds restores 1 STM every 3 seconds, grants temporary HP equal to 12% of maximum HP plus WIL scaling, reduces damage taken by 12%, and grants 50% Poison Resistance.")
        };

        foreach (var (featType, expectedDescription) in descriptions)
        {
            var featRow = featRows[(int)featType];
            var featDescriptionId = int.Parse(featRow["DESCRIPTION"]) - CustomTlkOffset;
            tlkEntries[featDescriptionId].Should().Be(expectedDescription);

            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var abilityDescriptionId = int.Parse(abilityRow["SpellDesc"]) - CustomTlkOffset;
            tlkEntries[abilityDescriptionId].Should().Be(expectedDescription);
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
        bool expectsStimPreserve = false,
        float maxRange = 5f,
        bool expectsCustomValidation = false)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.FirstAid);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.MaxRange.Should().Be(maxRange);
        ability.ActivationType.Should().Be(AbilityActivationType.Casted);
        ability.IsHostileAbility.Should().BeFalse();
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.IsSingleTargetAbility.Should().Be(!isArea);
        ability.BreaksStealth.Should().BeTrue();

        if (requiresTarget || expectsCustomValidation)
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
                itemRequirement.PreserveChanceStatType.Should().Be(StatType.StimPackPreserveChance);
            }
        }
        else
        {
            ability.Requirements.OfType<AbilityRequirementItem>().Should().BeEmpty();
        }
    }

    private static void AssertPerkCategory(
        Dictionary<PerkType, PerkDetail> perks,
        PerkCategoryType category)
    {
        perks.Values.Should().OnlyContain(perk => perk.Category == category);
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

    private static Dictionary<int, string> ReadTlkEntries(PathInfo path)
    {
        var tlk = JsonSerializer.Deserialize<TlkFile>(File.ReadAllText(path.FullName))!;
        return tlk.Entries.ToDictionary(entry => entry.Id, entry => entry.Text);
    }

    private static (string TemplateResRef, string Tag) ReadItemIdentity(PathInfo path)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(path.FullName));
        var root = json.RootElement;

        return (
            root.GetProperty("TemplateResRef").GetProperty("value").GetString()!,
            root.GetProperty("Tag").GetProperty("value").GetString()!);
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
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

    private sealed record TlkFile([property: JsonPropertyName("entries")] TlkEntry[] Entries);

    private sealed record TlkEntry(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("text")] string Text);
}

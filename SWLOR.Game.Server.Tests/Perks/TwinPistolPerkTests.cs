using FluentAssertions;
using NUnit.Framework;
using System.Reflection;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Perks;

public class TwinPistolPerkTests
{
    [Test]
    public void NativeAllocations_MatchGrantedFeatsAbilityLinksSkillAndCompanion()
    {
        var feats = ReadNativeRows("feat.2da");
        foreach (var feat in ReadPerks().Values.SelectMany(perk => perk.PerkLevels.Values)
                     .SelectMany(level => level.GrantedFeats).Distinct())
            feats.Rows[(int)feat][feats.Columns["LABEL"]].Should().Be(feat.ToString());

        var spells = ReadNativeRows("spells.2da");
        var abilities = typeof(TwinPistolAbilityDefinitionBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(TwinPistolAbilityDefinitionBase).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities());
        foreach (var (feat, ability) in abilities)
        {
            var spell = (Spell)int.Parse(feats.Rows[(int)feat][feats.Columns["SPELLID"]]);
            Enum.IsDefined(spell).Should().BeTrue();
            if (ability.Targeting != null)
                ability.Targeting.Spell.Should().Be(spell);
            spells.Rows[(int)spell][spells.Columns["Label"]].Should().Be(spell.ToString());
            spells.Rows[(int)spell][spells.Columns["FeatID"]].Should().Be(((int)feat).ToString());
        }
        var effects = ReadNativeRows("effecticons.2da");
        foreach (var charges in new[] { 1, 2, 3 })
            effects.Rows[(int)new CadenceStatusEffect(charges).Icon][effects.Columns["Label"]]
                .Should().Be($"Cadence{charges}");
        var baseItems = ReadNativeRows("baseitems.2da");
        baseItems.Rows[(int)BaseItem.CompanionPistol][baseItems.Columns["label"]].Should().Be("companionpistol");
        var skills = ReadNativeRows("iprp_skill.2da");
        skills.Rows[(int)SkillType.TwinPistol][skills.Columns["Label"]].Should().Be("TwinPistol");
    }

    [Test]
    public void Cadence_DisplayNameIsDistinctFromTwinBladeMomentumAndRetiredFlowWording()
    {
        var twinPistol = ReadPerks();
        twinPistol[PerkType.Cadence].Name.Should().Be("Cadence");
        twinPistol.Values.SelectMany(perk => perk.PerkLevels.Values)
            .Select(level => level.Description).Should().NotContain(description =>
                description.Contains("Momentum") || description.Contains("Flow") || description.Contains("move"));
        foreach (var charges in new[] { 1, 2, 3 })
        {
            var effect = new CadenceStatusEffect(charges);
            effect.Name.Should().Be($"Cadence ({charges})");
            effect.Clone().Name.Should().Be(effect.Name);
        }

        var twinBlade = new TwinBladePerkDefinition();
        typeof(TwinBladePerkDefinition).GetMethod("Momentum", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(twinBlade, null);
        var builder = typeof(TwinBladePerkDefinition).GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(twinBlade)!;
        var bladePerks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
        bladePerks[PerkType.Momentum].Name.Should().Be("Momentum");
        bladePerks[PerkType.Momentum].Type.Should().NotBe(PerkType.Cadence);
        typeof(AchievementType).GetField(nameof(AchievementType.FinalSalvo))!
            .GetCustomAttribute<AchievementAttribute>()!.Description.Should().Contain("Cadence").And.NotContain("Momentum");
    }

    [Test]
    public void CadenceTraits_UseTheAuthoredChainChargeAndAccuracyValues()
    {
        var perks = ReadPerks();
        Stats(perks[PerkType.Cadence], 1).Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.CadenceMaximum] = 3,
            [StatType.CadenceDurationSeconds] = 12,
            [StatType.CadenceChainWindowSeconds] = 6,
            [StatType.CadenceAccuracyPerCharge] = 2,
            [StatType.CadenceDamagePerCharge] = 2,
        });
        Stats(perks[PerkType.ChangingAngles], 1).Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.CadenceNextAbilityAccuracy] = 5,
            [StatType.CadenceAccuracyDurationSeconds] = 8,
            [StatType.CadenceAccuracyCooldownSeconds] = 10,
        });
        Stats(perks[PerkType.TwinPistolSecondWind], 1).Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.EvasionCadenceCooldownSeconds] = 10,
        });
        RequiredPerk(perks[PerkType.TwinPistolSecondWind].PerkLevels[1]).Should().Be(PerkType.Cadence);
        Stats(perks[PerkType.TwinPistolFollowThrough], 1).Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.CadenceStaminaRestorePerCharge] = 1,
            [StatType.CadenceStaminaRestoreMaximum] = 3,
            [StatType.CadenceRestoreCooldownSeconds] = 12,
        });
    }

    [TestCase(PerkCategoryType.TwinPistolBarrage)]
    [TestCase(PerkCategoryType.TwinPistolCrossfire)]
    public void IndependentLine_HasTenPerksEighteenPurchasesAndSixtySP(PerkCategoryType category)
    {
        var perks = ReadPerks().Values.Where(x => x.Category == category).ToArray();
        perks.Should().HaveCount(10);
        var levels = perks.SelectMany(x => x.PerkLevels.Values).ToArray();
        levels.Should().HaveCount(18);
        levels.Sum(x => x.Price).Should().Be(60);
        var requirements = levels.SelectMany(x => x.Requirements).OfType<PerkRequirementSkill>().ToArray();
        requirements.Should().OnlyContain(x => x.Type == SkillType.TwinPistol);
        requirements.Select(x => x.RequiredRank).OrderBy(x => x).Should().Equal(
            2, 5, 8, 10, 12, 15, 18, 20, 22, 25, 28, 30, 32, 35, 38, 40, 45, 50);
    }

    [Test]
    public void HandlingSlots_MatchedRhythmAndSteadyCadence_CanBothBePurchased()
    {
        var perks = ReadPerks();
        perks[PerkType.MatchedRhythm].Category.Should().Be(PerkCategoryType.TwinPistolBarrage);
        perks[PerkType.SteadyCadence].Category.Should().Be(PerkCategoryType.TwinPistolCrossfire);
        var speed = perks[PerkType.MatchedRhythm].PerkLevels.Values.ToArray();
        var cadence = perks[PerkType.SteadyCadence].PerkLevels.Values.ToArray();
        speed.SelectMany(x => x.StatBonuses).Should().OnlyContain(x => x.Stat == StatType.DualWieldAttackDelayReductionPercent);
        cadence.SelectMany(x => x.StatBonuses).Should().OnlyContain(x => x.Stat == StatType.CadenceBonusDamagePerCharge);
        speed.Select(x => x.StatBonuses.Single().Calculate(0)).Should().Equal(3, 6, 8);
        cadence.Select(x => x.StatBonuses.Single().Calculate(0)).Should().Equal(1, 2, 3);
        speed.Concat(cadence).SelectMany(x => x.Requirements).OfType<PerkRequirementCannotHavePerk>().Should().BeEmpty(
            "Matched Rhythm no longer excludes the Crossfire handling slot");
        RequiredPerk(perks[PerkType.SteadyCadence].PerkLevels[1]).Should().Be(PerkType.Cadence,
            "Steady Cadence only adds to charges the Cadence trait creates");
        ReadPerks().Values.SelectMany(x => x.PerkLevels.Values).Sum(x => x.Price).Should().Be(120);
    }

    [Test]
    public void CadenceGrantingShots_CarryTheBaseCadencePool()
    {
        var perks = ReadPerks();
        var levels = perks[PerkType.LeadShot].PerkLevels.Values.Concat(perks[PerkType.TempoShot].PerkLevels.Values).ToArray();
        levels.Should().HaveCount(6);
        foreach (var level in levels)
        {
            var stats = level.StatBonuses.ToDictionary(x => x.Stat, x => x.Calculate(0));
            stats.Should().Equal(new Dictionary<StatType, int>
            {
                [StatType.CadenceMaximum] = 3,
                [StatType.CadenceDurationSeconds] = 12,
            }, "the shots' Cadence grant needs a pool even before the Cadence trait is bought");
        }
        var trait = perks[PerkType.Cadence].PerkLevels[1].StatBonuses.ToDictionary(x => x.Stat, x => x.Calculate(0));
        trait[StatType.CadenceMaximum].Should().Be(3, "the pool matches the trait, so owning both changes nothing");
        trait[StatType.CadenceDurationSeconds].Should().Be(12);
    }

    [Test]
    public void PassiveStatBonuses_DoNotRequireAnEquippedPair()
    {
        var bonuses = ReadPerks().Values.SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.StatBonuses).ToArray();
        bonuses.Should().NotBeEmpty();
        bonuses.Select(bonus => bonus.Calculate(0)).Should().OnlyContain(value => value > 0,
            "cross-skill stat bonuses apply without inspecting a character's equipment");
    }

    [Test]
    public void Abilities_KeepTheirSharedPistolCooldowns_AndCrossfireShotsAreStationarySingleTargetAttacks()
    {
        var volley = new PairedVolleyAbilityDefinition().BuildAbilities();
        var lead = new LeadShotAbilityDefinition().BuildAbilities();
        var focus = new FocusedBurstAbilityDefinition().BuildAbilities();
        var closing = new ClosingShotAbilityDefinition().BuildAbilities();
        var controlled = new ControlledBurstAbilityDefinition().BuildAbilities();
        var tempo = new TempoShotAbilityDefinition().BuildAbilities();
        foreach (var ability in volley.Values.Concat(lead.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.TwinVolley);
            ability.RecastDelay(0).Should().Be(18f);
        }
        foreach (var ability in focus.Values.Concat(closing.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.DoubleShot);
            ability.RecastDelay(0).Should().Be(24f);
        }
        foreach (var ability in controlled.Values.Concat(tempo.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.FanTheHammer);
            ability.RecastDelay(0).Should().Be(30f);
        }
        foreach (var ability in lead.Values.Concat(closing.Values).Concat(tempo.Values))
        {
            ability.RequiresTarget.Should().BeTrue();
            ability.IsSingleTargetAbility.Should().BeTrue();
            ability.RequiresLocationTarget.Should().BeFalse("Crossfire shots no longer pick a ground destination");
            ability.MaxRange.Should().Be(25f);
        }
        var all = volley.Values.Concat(lead.Values).Concat(focus.Values).Concat(closing.Values)
            .Concat(controlled.Values).Concat(tempo.Values).ToArray();
        all.Should().OnlyContain(x => x.SkillType == SkillType.TwinPistol && x.IsHostileAbility);
        all.SelectMany(x => x.Requirements).OfType<AbilityRequirementStamina>().Should().HaveCount(all.Length);
        Stamina(volley).Should().Equal(3, 5, 8, 12);
        Stamina(lead).Should().Equal(3, 5, 8, 12);
        Stamina(closing).Should().Equal(4, 8, 10);
        Stamina(tempo).Should().Equal(6, 9);
    }

    [Test]
    public void CrossfireShots_BuildOrSpendCadenceAsAuthored()
    {
        foreach (var ability in new LeadShotAbilityDefinition().BuildAbilities().Values)
        {
            ability.KeepsWeaponAbilityCadence.Should().BeTrue();
            ability.WeaponAbilityCadenceOnHit.Should().Be(1);
        }
        foreach (var ability in new TempoShotAbilityDefinition().BuildAbilities().Values)
        {
            ability.KeepsWeaponAbilityCadence.Should().BeTrue();
            ability.WeaponAbilityCadenceOnHit.Should().Be(2);
        }
        foreach (var ability in new ClosingShotAbilityDefinition().BuildAbilities().Values)
        {
            ability.KeepsWeaponAbilityCadence.Should().BeFalse();
            ability.WeaponAbilityCadenceOnHit.Should().Be(0);
            ability.WeaponAbilityCadenceDamagePerCharge.Should().Be(2, "Closing Shot spends at its own +2 per charge, even without the Cadence trait");
        }
        var salvo = new FinalSalvoAbilityDefinition().BuildAbilities().Values.Single();
        salvo.KeepsWeaponAbilityCadence.Should().BeFalse();
        salvo.WeaponAbilityCadenceDamagePerCharge.Should().Be(4);
        foreach (var ability in new PairedVolleyAbilityDefinition().BuildAbilities().Values
                     .Concat(new FocusedBurstAbilityDefinition().BuildAbilities().Values))
        {
            ability.KeepsWeaponAbilityCadence.Should().BeTrue("Barrage abilities keep Cadence");
            ability.WeaponAbilityCadenceOnHit.Should().Be(0);
        }

        var perks = ReadPerks();
        Descriptions(perks[PerkType.LeadShot]).Should().Equal(
            "Deals weapon DMG + 4. Grants 1 Cadence.", "Deals weapon DMG + 8. Grants 1 Cadence.",
            "Deals weapon DMG + 12. Grants 1 Cadence.", "Deals weapon DMG + 16. Grants 1 Cadence.");
        Descriptions(perks[PerkType.ClosingShot]).Should().Equal(
            "Deals weapon DMG + 6. Spends all Cadence for +2 DMG per charge.",
            "Deals weapon DMG + 10. Spends all Cadence for +2 DMG per charge.",
            "Deals weapon DMG + 14. Spends all Cadence for +2 DMG per charge.");
        Descriptions(perks[PerkType.TempoShot]).Should().Equal(
            "Deals 60% weapon DMG + 6. Grants 2 Cadence.", "Deals 60% weapon DMG + 12. Grants 2 Cadence.");
    }

    [Test]
    public void Capstones_HaveSeparateMasteryGates_AndShareGlobalFinisherCooldown()
    {
        var perks = ReadPerks();
        var sunsGate = perks[PerkType.TwinSuns].PerkLevels[1].Requirements.OfType<PerkRequirementQuest>().Single();
        var salvoGate = perks[PerkType.FinalSalvo].PerkLevels[1].Requirements.OfType<PerkRequirementQuest>().Single();
        sunsGate.QuestId.Should().NotBe(salvoGate.QuestId);
        var quests = new TwinPistolCapstoneQuestDefinition().BuildQuests();
        quests.Keys.Should().Contain(new[] { sunsGate.QuestId, salvoGate.QuestId });
        foreach (var ability in new TwinSunsAbilityDefinition().BuildAbilities().Values
                     .Concat(new FinalSalvoAbilityDefinition().BuildAbilities().Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.Capstone);
            ability.RecastDelay(0).Should().Be(90f);
            ability.ActivationDelay(0, 0, 1).Should().Be(2f);
            ability.MaxRange.Should().Be(25f);
        }
    }

    [Test]
    public void Stances_AreExclusive_AndSeparateOrdinaryPenaltiesFromActivatedDamage()
    {
        var akimbo = new AkimboStanceStatusEffect();
        var crossfire = new CrossfireStanceStatusEffect();
        akimbo.SourceType.Should().Be(StatusEffectSourceType.Stance);
        crossfire.SourceType.Should().Be(StatusEffectSourceType.Stance);
        akimbo.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(10);
        akimbo.StatGroup.Stats[StatType.OrdinaryAttackHasteRampMaximumStacks].Should().Be(5);
        akimbo.StatGroup.Stats[StatType.OrdinaryAttackDamagePercentAdjustment].Should().Be(-30);
        crossfire.StatGroup.Stats[StatType.OrdinaryAttackDamagePercentAdjustment].Should().Be(-20);
        crossfire.StatGroup.Stats[StatType.CadenceExtendedDurationSeconds].Should().Be(18);
        akimbo.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        crossfire.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        akimbo.CanApply(0).Should().BeEmpty("the stance has no equipment gate");
        crossfire.CanApply(0).Should().BeEmpty("the stance has no equipment gate");
        var stances = new AkimboStanceAbilityDefinition().BuildAbilities().Values
            .Concat(new CrossfireStanceAbilityDefinition().BuildAbilities().Values);
        stances.Should().OnlyContain(x => x.CustomValidation == null);
        stances.Should().OnlyContain(x => x.RecastDelay(0) == 30f && x.ActivationDelay(0, 0, 1) == 2f);
    }

    private static int[] Stamina(Dictionary<FeatType, AbilityDetail> abilities) =>
        abilities.Values.OrderBy(x => x.AbilityLevel)
            .Select(x => x.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM).ToArray();

    private static string[] Descriptions(PerkDetail perk) =>
        perk.PerkLevels.OrderBy(level => level.Key).Select(level => level.Value.Description).ToArray();

    private static Dictionary<StatType, int> Stats(PerkDetail perk, int level) =>
        perk.PerkLevels[level].StatBonuses.ToDictionary(bonus => bonus.Stat, bonus => bonus.Calculate(0));

    private static PerkType RequiredPerk(PerkLevel level)
    {
        var requirement = level.Requirements.OfType<PerkRequirementMustHavePerk>().Single();
        return (PerkType)typeof(PerkRequirementMustHavePerk)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(PerkType)).GetValue(requirement)!;
    }

    private static (Dictionary<string, int> Columns, Dictionary<int, string[]> Rows) ReadNativeRows(string name)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        var lines = File.ReadAllLines(Path.Combine(root!.FullName, "SWLOR_Haks", "sw_2da", name));
        var header = lines.Skip(1).First(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("DEFAULT"));
        var columns = header.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
            .Select((column, index) => (column, index: index + 1))
            .ToDictionary(value => value.column, value => value.index, StringComparer.OrdinalIgnoreCase);
        var rows = lines.Select(line => line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length > 0 && int.TryParse(parts[0], out _))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts);
        return (columns, rows);
    }

    private static Dictionary<PerkType, PerkDetail> ReadPerks()
    {
        var definition = new TwinPistolPerkDefinition();
        foreach (var method in typeof(TwinPistolPerkDefinition)
                     .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0 &&
                                      !method.Name.Contains('<')))
            method.Invoke(definition, null);
        var builder = typeof(TwinPistolPerkDefinition).GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
    }
}

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
            effects.Rows[(int)new MomentumStatusEffect(charges).Icon][effects.Columns["Label"]]
                .Should().Be($"Flow{charges}");
        var baseItems = ReadNativeRows("baseitems.2da");
        baseItems.Rows[(int)BaseItem.CompanionPistol][baseItems.Columns["label"]].Should().Be("companionpistol");
        var skills = ReadNativeRows("iprp_skill.2da");
        skills.Rows[(int)SkillType.TwinPistol][skills.Columns["Label"]].Should().Be("TwinPistol");
    }

    [Test]
    public void Flow_DisplayNameIsDistinctFromTwinBladeMomentumWithoutChangingPerkIdentity()
    {
        var twinPistol = ReadPerks();
        twinPistol[PerkType.TwinPistolMomentum].Name.Should().Be("Flow");
        twinPistol.Values.SelectMany(perk => perk.PerkLevels.Values)
            .Select(level => level.Description).Should().NotContain(description => description.Contains("Momentum"));
        twinPistol[PerkType.GrappleShot].PerkLevels.Values.Should()
            .OnlyContain(level => !level.Description.Contains("Spend all Flow"), "Grapple Shot uses the generic Flow bonus");
        foreach (var charges in new[] { 1, 2, 3 })
        {
            var effect = new MomentumStatusEffect(charges);
            effect.Name.Should().Be($"Flow ({charges})");
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
        bladePerks[PerkType.Momentum].Type.Should().NotBe(PerkType.TwinPistolMomentum);
        typeof(AchievementType).GetField(nameof(AchievementType.FinalSalvo))!
            .GetCustomAttribute<AchievementAttribute>()!.Description.Should().Contain("Flow").And.NotContain("Momentum");
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
    public void HandlingBonuses_OfferExclusiveSpeedOrDamagePurchasesAtEveryRank()
    {
        var perks = ReadPerks();
        perks[PerkType.MatchedRhythm].Category.Should().Be(PerkCategoryType.TwinPistolBarrage);
        perks[PerkType.PairedHandling].Category.Should().Be(PerkCategoryType.TwinPistolCrossfire);
        var speed = perks[PerkType.MatchedRhythm].PerkLevels.Values.ToArray();
        var damage = perks[PerkType.PairedHandling].PerkLevels.Values.ToArray();
        speed.SelectMany(x => x.StatBonuses).Should().OnlyContain(x => x.Stat == StatType.DualWieldAttackDelayReductionPercent);
        damage.SelectMany(x => x.StatBonuses).Should().OnlyContain(x => x.Stat == StatType.PairedRangedOrdinaryDamagePercentAdjustment);
        speed.Select(x => x.StatBonuses.Single().Calculate(0)).Should().Equal(3, 6, 8);
        damage.Select(x => x.StatBonuses.Single().Calculate(0)).Should().Equal(5, 8, 10);
        foreach (var (levels, forbidden) in new[] { (speed, PerkType.PairedHandling), (damage, PerkType.MatchedRhythm) })
        foreach (var level in levels)
        {
            var exclusion = level.Requirements.OfType<PerkRequirementCannotHavePerk>().Single();
            typeof(PerkRequirementCannotHavePerk).GetField("_cannotHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(exclusion).Should().Be(forbidden);
        }
        ReadPerks().Values.SelectMany(x => x.PerkLevels.Values).Sum(x => x.Price).Should().Be(120);
        (120 - speed.Sum(x => x.Price)).Should().Be(110, "the handling exclusion limits the legal combined purchase budget");
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
    public void Abilities_KeepTheirSharedPistolCooldowns_AndGroundRushAllowsEmptyDestinations()
    {
        var volley = new PairedVolleyAbilityDefinition().BuildAbilities();
        var slide = new BlasterSlideAbilityDefinition().BuildAbilities();
        var focus = new FocusedBurstAbilityDefinition().BuildAbilities();
        var grapple = new GrappleShotAbilityDefinition().BuildAbilities();
        var controlled = new ControlledBurstAbilityDefinition().BuildAbilities();
        var rush = new PistolRushAbilityDefinition().BuildAbilities();
        foreach (var ability in volley.Values.Concat(slide.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.TwinVolley);
            ability.RecastDelay(0).Should().Be(18f);
        }
        foreach (var ability in focus.Values.Concat(grapple.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.DoubleShot);
            ability.RecastDelay(0).Should().Be(24f);
        }
        foreach (var ability in controlled.Values.Concat(rush.Values))
        {
            ability.RecastGroup.Should().Be(RecastGroup.FanTheHammer);
            ability.RecastDelay(0).Should().Be(30f);
        }
        foreach (var ability in rush.Values)
        {
            ability.RequiresTarget.Should().BeFalse();
            ability.RequiresLocationTarget.Should().BeTrue();
            ability.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Rect);
            ability.Targeting.SizeX.Should().Be(6f);
            ability.Targeting.SizeY.Should().Be(4f);
            ability.MaxRange.Should().Be(6f);
            ability.ClampsLocationTargetToMaxRange.Should().BeTrue("a click past 6m rushes the full 6m toward it");
            ability.Targeting.Spell.Should().NotBe(Spell.Invalid);
        }
        volley.Values.Concat(slide.Values).Concat(focus.Values).Concat(grapple.Values).Concat(controlled.Values)
            .Should().OnlyContain(x => !x.ClampsLocationTargetToMaxRange, "only the free-destination dash clamps its click");
        rush.Values.Select(x => x.Targeting.Spell).Should().OnlyHaveUniqueItems();
        var all = volley.Values.Concat(slide.Values).Concat(focus.Values).Concat(grapple.Values)
            .Concat(controlled.Values).Concat(rush.Values).ToArray();
        all.Should().OnlyContain(x => x.SkillType == SkillType.TwinPistol && x.IsHostileAbility);
        all.SelectMany(x => x.Requirements).OfType<AbilityRequirementStamina>().Should().HaveCount(all.Length);
        volley.Values.OrderBy(x => x.AbilityLevel).Select(x => x.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM)
            .Should().Equal(3, 5, 8, 12);
        grapple.Values.OrderBy(x => x.AbilityLevel).Select(x => x.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM)
            .Should().Equal(4, 8, 10);
        slide.Values.OrderBy(x => x.AbilityLevel).Select(x => x.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM)
            .Should().Equal(3, 5, 8, 12);
        rush.Values.OrderBy(x => x.AbilityLevel).Select(x => x.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM)
            .Should().Equal(6, 9);
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
        crossfire.StatGroup.Stats[StatType.PairedRangedMomentumExtendedDurationSeconds].Should().Be(18);
        akimbo.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        crossfire.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        akimbo.CanApply(0).Should().BeEmpty("the stance has no equipment gate");
        crossfire.CanApply(0).Should().BeEmpty("the stance has no equipment gate");
        var stances = new AkimboStanceAbilityDefinition().BuildAbilities().Values
            .Concat(new CrossfireStanceAbilityDefinition().BuildAbilities().Values);
        stances.Should().OnlyContain(x => x.CustomValidation == null);
        stances.Should().OnlyContain(x => x.RecastDelay(0) == 30f && x.ActivationDelay(0, 0, 1) == 2f);
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

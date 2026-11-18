using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Each one-line call into unconverted code still reaches the code it wraps: the stat service's and, since P5-4c, those of part 5
/// of the stat split, <c>CombatRatingService</c>; since P5-4d, part 6's (<c>CombatDeflectionReactionService</c>) and part 7's
/// (the healing adjustments' temporary modifier in <c>CombatResourcesService</c>).
/// </summary>
[NonParallelizable]
public class StatServiceDefaultSeamsTests
{
    private const uint Creature = 601;

    private FacadeContainer _facades = null!;
    private IDisposable _installed = null!;
    private IDisposable _engineScope = null!;
    private FakeNWScript _engine = null!;
    private DefaultSeams _seams = null!;
    private DefaultCreatureSeams _creatureSeams = null!;
    private DefaultRecordSeams _recordSeams = null!;
    private DefaultRatingSeams _ratingSeams = null!;
    private DefaultDeflectionSeams _deflectionSeams = null!;
    private DefaultResourcesSeams _resourcesSeams = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _installed = _facades.Install();
        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
        _seams = new DefaultSeams(
            _facades.Get<IDBService>(), _facades.Get<ICombatRecorderService>(), _facades.Get<IPlayerFeedbackService>(),
            _facades.Get<IStatusEffectService>(), _facades.Get<IMimicryTraitStatsService>(), _facades.Get<ICompanionLookup>(),
            _facades.Get<IResourceRestoredListener>());
        _creatureSeams = new DefaultCreatureSeams(
            _facades.Get<IDBService>(), _facades.Get<ICombatRecorderService>(), _facades.Get<ICompanionLookup>(),
            _facades.Get<IStatusEffectService>(), _facades.Get<IStatService>());
        _recordSeams = new DefaultRecordSeams(_facades.Get<IDBService>(), _facades.Get<IStatService>());
        _ratingSeams = new DefaultRatingSeams(
            _facades.Get<IDBService>(), _facades.Get<IItemService>(), _facades.Get<ITwinPistolEquipmentService>(), _facades.Get<IStatService>());
        _deflectionSeams = new DefaultDeflectionSeams(
            _facades.Get<ICombatRecorderService>(), _facades.Get<IRecastService>(), _facades.Get<IStatService>());
        _resourcesSeams = new DefaultResourcesSeams(
            _facades.Get<IStatService>(), _facades.Get<IStatusEffectApplicationService>(), _facades.Get<ICombatRecorderService>(),
            _facades.Get<IPvpCombatService>(), _facades.Get<ICompanionLookup>());
    }

    [TearDown]
    public void TearDown()
    {
        _engineScope.Dispose();
        _installed.Dispose();
    }

    [Test]
    public void ThePerkReads_ReachThePerkCatalog()
    {
        _seams.PerkBonus(Creature, StatType.Attack).Should().Be(Perk.GetStatBonus(Creature, StatType.Attack));
        _seams.PerkSources(Creature, StatType.Attack).Should().BeEquivalentTo(Perk.GetStatSources(Creature, StatType.Attack));
    }

    [Test]
    public void TheTemporaryModifierCalls_ReachTemporaryStatModifier()
    {
        try
        {
            _deflectionSeams.ReplaceTemporary(Creature, StatType.Attack, 5, 30f, StatType.Accuracy);

            _seams.TemporaryAdjustment(Creature, StatType.Attack).Should().Be(5);
            _seams.TemporarySources(Creature, StatType.Attack).Should().ContainSingle();

            _resourcesSeams.ReplaceTemporary(Creature, StatType.Attack, 7, 30f, StatType.Accuracy);

            _seams.TemporaryAdjustment(Creature, StatType.Attack).Should().Be(7, "the healing seam replaces the same group");
            _seams.TemporarySources(Creature, StatType.Attack).Should().ContainSingle();
        }
        finally
        {
            TemporaryStatModifier.Clear(Creature);
        }
    }

    [Test]
    public void TheSkillAndCombatTables_AreTheRealOnes()
    {
        using var events = EventsPluginRuntime.Override(new FakeEventsPlugin());
        Skill.CacheData();
        Skill.LoadMappings();

        _seams.SkillDetails(SkillType.Smithery).Should().BeSameAs(Skill.GetSkillDetails(SkillType.Smithery));
        _ratingSeams.BaseItemSkill(BaseItem.Longsword).Should().Be(Skill.GetSkillTypeByBaseItem(BaseItem.Longsword));
        _recordSeams.DefenseDamageTypes().Should().Equal(GameServices.CombatDamageCatalog.GetDefenseDamageTypes());
        _ratingSeams.IsRanged(SkillType.Pistol).Should().BeTrue();
        _ratingSeams.IsRanged(SkillType.Vibroblade).Should().BeFalse();
    }

    [Test]
    public void TheWeaponAbilityReads_ReachCombat()
    {
        _ratingSeams.DamageAbility(Creature, BaseItem.Longsword).Should().Be(GameServices.CombatQueries.GetWeaponDamageAbilityType(Creature, BaseItem.Longsword));
        _ratingSeams.AccuracyAbility(Creature, BaseItem.Longsword).Should().Be(GameServices.CombatQueries.GetWeaponAccuracyAbilityType(Creature, BaseItem.Longsword));
    }

    [Test]
    public void IsDroid_AsksTheDroidService()
    {
        _creatureSeams.CallIsDroid(Creature).Should().BeFalse();

        _engine.CallsTo("GetResRef").Should().ContainSingle();
    }

    [Test]
    public void ScheduleTemporaryHitPointLimitCheck_AsksTheEffectsCode()
    {
        _creatureSeams.ScheduleLimitCheck(Creature);

        _engine.CallsTo("GetIsObjectValid").Should().ContainSingle("an invalid creature has nothing to check");
    }

    [Test]
    public void TheAttackAdjustments_ReachCombat()
    {
        _ratingSeams.NearbyAdjustment(Creature).Should().Be(0);
        _ratingSeams.LowHPAdjustment(Creature).Should().Be(0);
        _ratingSeams.LowFPAdjustment(Creature).Should().Be(0);
    }

    [Test]
    public void GetWeaponSkill_ReachesTheSkillService()
    {
        _ratingSeams.WeaponSkill(Creature, 700).Should().Be(Skill.GetWeaponSkill(Creature, 700));
    }

    [Test]
    public void TheReactions_RunOnACreatureWithNothingToGrant()
    {
        _seams.FPRestored(Creature);
        _seams.StaminaRestored(Creature);
        _deflectionSeams.Track(Creature, DeflectionSource.Melee);
        _deflectionSeams.NextSkillBonuses(Creature, SkillType.Invalid, 0, 0, 0);
        _deflectionSeams.NextAutoAttack(Creature, SkillType.Invalid, 0, 0);
        _deflectionSeams.NextNoDelay(Creature, SkillType.Invalid, 0);

        _deflectionSeams.Trigger(Creature, StatType.Attack, 0).Should().BeTrue("a stat trigger with no cooldown is always open");
    }

    private sealed class DefaultSeams : StatService
    {
        public DefaultSeams(
            IDBService db,
            ICombatRecorderService combatRecorder,
            IPlayerFeedbackService playerFeedback,
            IStatusEffectService statusEffect,
            IMimicryTraitStatsService mimicryTraitStats,
            ICompanionLookup beastMastery,
            IResourceRestoredListener resourceRestored)
            : base(db, combatRecorder, playerFeedback, statusEffect, mimicryTraitStats, beastMastery,
                FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<ISkillService>(), FacadeForwarder.For<ITemporaryStatModifierService>(),
                resourceRestored)
        {
        }

        public int PerkBonus(uint creature, StatType stat) => GetPerkStatBonus(creature, stat);

        public IReadOnlyList<StatAdjustmentSource> PerkSources(uint creature, StatType stat) => GetPerkStatSources(creature, stat);

        public int TemporaryAdjustment(uint creature, StatType stat) => GetTemporaryStatAdjustment(creature, stat);

        public IReadOnlyList<StatAdjustmentSource> TemporarySources(uint creature, StatType stat) => GetTemporaryStatSources(creature, stat);

        public SkillAttribute SkillDetails(SkillType skill) => GetSkillDetails(skill);

        public void FPRestored(uint creature) => ApplyFPRestoredEffects(creature);

        public void StaminaRestored(uint creature) => ApplyStaminaRestoredEffects(creature);
    }

    // Part 2 of the stat split (P5-4e): the creature stat seams with their default bodies.
    private sealed class DefaultCreatureSeams : CreatureStatService
    {
        public DefaultCreatureSeams(
            IDBService db,
            ICharacterRecording combatRecorder,
            ICompanionLookup companions,
            IStatusEffectService statusEffect,
            IStatService stat)
            : base(db, combatRecorder, companions, statusEffect, stat)
        {
        }

        public bool CallIsDroid(uint creature) => IsDroid(creature);

        public void ScheduleLimitCheck(uint creature) => ScheduleTemporaryHitPointLimitCheck(creature);
    }

    // Part 3 of the stat split (P5-4e): the record seam with its default body.
    private sealed class DefaultRecordSeams : StatRecordService
    {
        public DefaultRecordSeams(IDBService db, IStatService stat)
            : base(db, stat)
        {
        }

        public IReadOnlyList<CombatDamageType> DefenseDamageTypes() => GetDefenseDamageTypes();
    }

    // Part 6 of the stat split (P5-4d): the deflection reaction seams with their default bodies.
    private sealed class DefaultDeflectionSeams : CombatDeflectionReactionService
    {
        public DefaultDeflectionSeams(ICombatRecorderService combatRecorder, IRecastService recast, IStatService stat)
            : base(combatRecorder, recast, FacadeForwarder.For<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatAttackTimingService>(),
                FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ITemporaryStatModifierService>(), stat)
        {
        }

        public void ReplaceTemporary(uint creature, StatType stat, int amount, float seconds, StatType group) =>
            ReplaceTemporaryStatModifier(creature, stat, amount, seconds, group);

        public void Track(uint creature, DeflectionSource source) => TrackDeflection(creature, source);

        public bool Trigger(uint creature, StatType stat, int cooldownSeconds) => TryUseStatTrigger(creature, stat, cooldownSeconds);

        public void NextSkillBonuses(uint creature, SkillType skill, int damage, int critical, int seconds) =>
            GrantNextSkillAbilityBonuses(creature, skill, damage, critical, seconds);

        public void NextAutoAttack(uint creature, SkillType skill, int critical, int seconds) =>
            GrantNextAutoAttackCriticalRateBonus(creature, skill, critical, seconds);

        public void NextNoDelay(uint creature, SkillType skill, int seconds) => GrantNextAbilityNoDelay(creature, skill, seconds);
    }

    // Part 7 of the stat split (P5-4d): the healing adjustments' temporary modifier seam with its default body.
    private sealed class DefaultResourcesSeams : CombatResourcesService
    {
        public DefaultResourcesSeams(IStatService stat, IStatusEffectApplicationService statusEffect, ICombatRecorderService combatRecorder,
            IPvpCombatService pvpCombat, ICompanionLookup companions)
            : base(FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.For<IAbilityImpactState>(), stat, statusEffect, combatRecorder,
                FacadeForwarder.For<ITemporaryStatModifierService>(), pvpCombat, companions)
        {
        }

        public void ReplaceTemporary(uint creature, StatType stat, int amount, float seconds, StatType group) =>
            ReplaceTemporaryStatModifier(creature, stat, amount, seconds, group);
    }

    // Part 5 of the stat split (P5-4c): the rating seams with their default bodies.
    private sealed class DefaultRatingSeams : CombatRatingService
    {
        public DefaultRatingSeams(IDBService db, IItemService item, ITwinPistolEquipmentService twinPistolEquipment, IStatService stat)
            : base(db, FacadeForwarder.For<ISkillService>(), item, twinPistolEquipment, FacadeForwarder.For<ICombatQueriesService>(),
                FacadeForwarder.For<ICombatDamageModifiersService>(), stat)
        {
        }

        public SkillType BaseItemSkill(BaseItem item) => GetSkillTypeByBaseItem(item);

        public bool IsRanged(SkillType skill) => IsRangedDamageSkill(skill);

        public AbilityType DamageAbility(uint creature, BaseItem item) => GetWeaponDamageAbilityType(creature, item);

        public AbilityType AccuracyAbility(uint creature, BaseItem item) => GetWeaponAccuracyAbilityType(creature, item);

        public int NearbyAdjustment(uint creature) => GetNearbyStatusTargetAttackAdjustment(creature);

        public int LowHPAdjustment(uint creature) => GetLowHPAttackAdjustment(creature);

        public int LowFPAdjustment(uint creature) => GetLowFPAttackAdjustment(creature);

        public SkillType WeaponSkill(uint creature, uint weapon) => GetWeaponSkill(creature, weapon);
    }
}

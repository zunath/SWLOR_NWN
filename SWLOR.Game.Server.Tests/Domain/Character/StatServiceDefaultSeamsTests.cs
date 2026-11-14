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

/// <summary>Each one-line call into unconverted code still reaches the code it wraps.</summary>
[NonParallelizable]
public class StatServiceDefaultSeamsTests
{
    private const uint Creature = 601;

    private FacadeContainer _facades = null!;
    private IDisposable _installed = null!;
    private IDisposable _engineScope = null!;
    private FakeNWScript _engine = null!;
    private DefaultSeams _seams = null!;

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
            _facades.Get<IStatusEffectService>(), _facades.Get<IMimicryService>(), _facades.Get<ITwinPistolEquipmentService>(),
            _facades.Get<IPowerGloveEquipmentService>(), _facades.Get<IItemService>(), _facades.Get<IRecastService>(),
            _facades.Get<IBeastMasteryService>(), _facades.Get<IEnmityService>(), _facades.Get<IResistanceService>(),
            _facades.Get<IPvpCombatService>());
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
            _seams.ReplaceTemporary(Creature, StatType.Attack, 5, 30f, StatType.Accuracy);

            _seams.TemporaryAdjustment(Creature, StatType.Attack).Should().Be(5);
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
        _seams.BaseItemSkill(BaseItem.Longsword).Should().Be(Skill.GetSkillTypeByBaseItem(BaseItem.Longsword));
        _seams.DefenseDamageTypes().Should().Equal(GameServices.CombatDamageCatalog.GetDefenseDamageTypes());
        _seams.IsRanged(SkillType.Pistol).Should().BeTrue();
        _seams.IsRanged(SkillType.Vibroblade).Should().BeFalse();
    }

    [Test]
    public void TheWeaponAbilityReads_ReachCombat()
    {
        _seams.DamageAbility(Creature, BaseItem.Longsword).Should().Be(GameServices.CombatQueries.GetWeaponDamageAbilityType(Creature, BaseItem.Longsword));
        _seams.AccuracyAbility(Creature, BaseItem.Longsword).Should().Be(GameServices.CombatQueries.GetWeaponAccuracyAbilityType(Creature, BaseItem.Longsword));
    }

    [Test]
    public void IsDroid_AsksTheDroidService()
    {
        _seams.CallIsDroid(Creature).Should().BeFalse();

        _engine.CallsTo("GetResRef").Should().ContainSingle();
    }

    [Test]
    public void ScheduleTemporaryHitPointLimitCheck_AsksTheEffectsCode()
    {
        _seams.ScheduleLimitCheck(Creature);

        _engine.CallsTo("GetIsObjectValid").Should().ContainSingle("an invalid creature has nothing to check");
    }

    [Test]
    public void TheAttackAdjustments_ReachCombat()
    {
        _seams.NearbyAdjustment(Creature).Should().Be(0);
        _seams.LowHPAdjustment(Creature).Should().Be(0);
        _seams.LowFPAdjustment(Creature).Should().Be(0);
    }

    [Test]
    public void GetWeaponSkill_ReachesTheSkillService()
    {
        _seams.WeaponSkill(Creature, 700).Should().Be(Skill.GetWeaponSkill(Creature, 700));
    }

    [Test]
    public void TheReactions_RunOnACreatureWithNothingToGrant()
    {
        _seams.FPRestored(Creature);
        _seams.StaminaRestored(Creature);
        _seams.Track(Creature, DeflectionSource.Melee);
        _seams.NextSkillBonuses(Creature, SkillType.Invalid, 0, 0, 0);
        _seams.NextAutoAttack(Creature, SkillType.Invalid, 0, 0);
        _seams.NextNoDelay(Creature, SkillType.Invalid, 0);

        _seams.Trigger(Creature, StatType.Attack, 0).Should().BeTrue("a stat trigger with no cooldown is always open");
    }

    private sealed class DefaultSeams : StatService
    {
        public DefaultSeams(
            IDBService db,
            ICombatRecorderService combatRecorder,
            IPlayerFeedbackService playerFeedback,
            IStatusEffectService statusEffect,
            IMimicryService mimicry,
            ITwinPistolEquipmentService twinPistolEquipment,
            IPowerGloveEquipmentService powerGloveEquipment,
            IItemService item,
            IRecastService recast,
            IBeastMasteryService beastMastery,
            IEnmityService enmity,
            IResistanceService resistance,
            IPvpCombatService pvpCombat)
            : base(db, combatRecorder, playerFeedback, statusEffect, mimicry, twinPistolEquipment, powerGloveEquipment, item, recast, beastMastery, enmity, resistance, pvpCombat,
                FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<ISkillService>(), FacadeForwarder.For<ITemporaryStatModifierService>(), FacadeForwarder.LazyFor<ICombatAttackTimingService>(), FacadeForwarder.For<ICombatDamageCatalogService>(), FacadeForwarder.LazyFor<ICombatDamageModifiersService>(), FacadeForwarder.LazyFor<ICombatDamageTakenService>(), FacadeForwarder.LazyFor<ICombatQueriesService>(), FacadeForwarder.LazyFor<ICombatResourcesService>(), FacadeForwarder.LazyFor<IDroidService>())
        {
        }

        public int PerkBonus(uint creature, StatType stat) => GetPerkStatBonus(creature, stat);

        public IReadOnlyList<StatAdjustmentSource> PerkSources(uint creature, StatType stat) => GetPerkStatSources(creature, stat);

        public int TemporaryAdjustment(uint creature, StatType stat) => GetTemporaryStatAdjustment(creature, stat);

        public IReadOnlyList<StatAdjustmentSource> TemporarySources(uint creature, StatType stat) => GetTemporaryStatSources(creature, stat);

        public void ReplaceTemporary(uint creature, StatType stat, int amount, float seconds, StatType group) =>
            ReplaceTemporaryStatModifier(creature, stat, amount, seconds, group);

        public SkillAttribute SkillDetails(SkillType skill) => GetSkillDetails(skill);

        public SkillType BaseItemSkill(BaseItem item) => GetSkillTypeByBaseItem(item);

        public IReadOnlyList<CombatDamageType> DefenseDamageTypes() => GetDefenseDamageTypes();

        public bool IsRanged(SkillType skill) => IsRangedDamageSkill(skill);

        public AbilityType DamageAbility(uint creature, BaseItem item) => GetWeaponDamageAbilityType(creature, item);

        public AbilityType AccuracyAbility(uint creature, BaseItem item) => GetWeaponAccuracyAbilityType(creature, item);

        public bool CallIsDroid(uint creature) => IsDroid(creature);

        public void ScheduleLimitCheck(uint creature) => ScheduleTemporaryHitPointLimitCheck(creature);

        public int NearbyAdjustment(uint creature) => GetNearbyStatusTargetAttackAdjustment(creature);

        public int LowHPAdjustment(uint creature) => GetLowHPAttackAdjustment(creature);

        public int LowFPAdjustment(uint creature) => GetLowFPAttackAdjustment(creature);

        public SkillType WeaponSkill(uint creature, uint weapon) => GetWeaponSkill(creature, weapon);

        public void FPRestored(uint creature) => ApplyFPRestoredEffects(creature);

        public void StaminaRestored(uint creature) => ApplyStaminaRestoredEffects(creature);

        public void Track(uint creature, DeflectionSource source) => TrackDeflection(creature, source);

        public bool Trigger(uint creature, StatType stat, int cooldownSeconds) => TryUseStatTrigger(creature, stat, cooldownSeconds);

        public void NextSkillBonuses(uint creature, SkillType skill, int damage, int critical, int seconds) =>
            GrantNextSkillAbilityBonuses(creature, skill, damage, critical, seconds);

        public void NextAutoAttack(uint creature, SkillType skill, int critical, int seconds) =>
            GrantNextAutoAttackCriticalRateBonus(creature, skill, critical, seconds);

        public void NextNoDelay(uint creature, SkillType skill, int seconds) => GrantNextAbilityNoDelay(creature, skill, seconds);
    }
}

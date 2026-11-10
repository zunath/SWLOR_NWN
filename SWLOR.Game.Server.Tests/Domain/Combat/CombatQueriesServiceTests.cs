using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The read-only questions every attack asks: positions and facing, the weapon and skill an attack or ability uses, and
/// the stat and status lookups. The engine is a recording fake, the converted services it reads are scripted proxies over
/// tables the test fills, and a container answers the static helpers that still reach facades (status effects adding
/// their stats, weapon damage reading the item).
/// </summary>
[TestFixture]
public class CombatQueriesServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Ally = 33;
    private const uint Stranger = 44;
    private const uint Area = 90;
    private const uint OtherArea = 91;
    private const uint RightWeapon = 101;
    private const uint LeftWeapon = 102;
    private const uint Claw = 103;
    private const uint Bite = 104;
    private const uint Staff = 105;

    private readonly List<IDisposable> _scopes = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private CombatQueriesService _service = null!;

    private Dictionary<(uint Creature, StatType Stat), int> _stats = null!;
    private Dictionary<(uint Creature, StatType Stat), IReadOnlyList<StatAdjustmentSource>> _sources = null!;
    private Dictionary<uint, CreatureStatusEffect> _statuses = null!;
    private HashSet<(uint, uint)> _parties = null!;
    private Dictionary<uint, SkillType> _weaponSkills = null!;
    private Dictionary<BaseItem, SkillType> _baseItemSkills = null!;
    private HashSet<(PerkType, int)> _perkCategories = null!;
    private int _targetedPerkBonus;
    private ScriptedProxy _perkCalls = null!;
    private uint _companionWeapon;

    private Dictionary<uint, uint> _areas = null!;
    private Dictionary<uint, Vector3> _positions = null!;
    private Dictionary<uint, float> _facings = null!;
    private HashSet<uint> _valid = null!;
    private Dictionary<(uint Creature, InventorySlot Slot), uint> _slots = null!;
    private Dictionary<uint, BaseItem> _baseItems = null!;
    private Dictionary<uint, uint> _possessors = null!;
    private HashSet<(uint, ActionMode)> _actionModes = null!;
    private List<EffectTypeScript> _effects = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _scopes.Add(_facades.Install());
        _stats = new();
        _sources = new();
        _statuses = new();
        _parties = new();
        _weaponSkills = new();
        _baseItemSkills = new()
        {
            [BaseItem.Longsword] = SkillType.Vibroblade,
            [BaseItem.Rifle] = SkillType.Rifle,
            [BaseItem.QuarterStaff] = SkillType.Staff,
            [BaseItem.CompanionPistol] = SkillType.TwinPistol,
        };
        _perkCategories = new();
        _targetedPerkBonus = 0;
        _companionWeapon = 0;
        _areas = new() { [Attacker] = Area, [Defender] = Area, [Ally] = Area, [Stranger] = Area };
        _positions = new();
        _facings = new();
        _valid = new() { Attacker, Defender, Ally, Stranger };
        _slots = new();
        _baseItems = new();
        _possessors = new();
        _actionModes = new();
        _effects = new();

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => _valid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetArea", args => _areas.GetValueOrDefault((uint)args[0]!));
        _engine.SetReturnHandler("GetPosition", args => _positions.GetValueOrDefault((uint)args[0]!));
        _engine.SetReturnHandler("GetFacing", args => _facings.GetValueOrDefault((uint)args[0]!));
        _engine.SetReturnHandler("GetDistanceBetween", args => Vector3.Distance(
            _positions.GetValueOrDefault((uint)args[0]!), _positions.GetValueOrDefault((uint)args[1]!)));
        _engine.SetReturnHandler("GetItemInSlot", args => _slots.GetValueOrDefault(((uint)args[1]!, (InventorySlot)args[0]!)));
        _engine.SetReturnHandler("GetBaseItemType", args => _baseItems.TryGetValue((uint)args[0]!, out var type) ? type : BaseItem.Invalid);
        _engine.SetReturnHandler("GetItemPossessor", args => _possessors.GetValueOrDefault((uint)args[0]!));
        _engine.SetReturnHandler("GetActionMode", args => _actionModes.Contains(((uint)args[0]!, (ActionMode)args[1]!)));
        _engine.SetReturnHandler("GetLocalBool", args => (uint)args[0]! == Defender && (string)args[1]! == BossCreature.Variable);
        var cursor = 0;
        _engine.SetReturnHandler("GetFirstEffect", _ => { cursor = 0; return EngineStructures.Create<Effect>(_effects.Count > 0 ? 1 : 0); });
        _engine.SetReturnHandler("GetNextEffect", _ => { cursor++; return EngineStructures.Create<Effect>(cursor < _effects.Count ? cursor + 1 : 0); });
        _engine.SetReturnHandler("GetIsEffectValid", args => ((Effect)args[0]!).Handle != 0);
        _engine.SetReturnHandler("GetEffectType", _ => _effects[cursor]);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls
            .On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)))
            .On(nameof(IStatService.GetStatSources), args =>
                _sources.TryGetValue(((uint)args[0]!, (StatType)args[1]!), out var list) ? list : Array.Empty<StatAdjustmentSource>());
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls.On(nameof(IStatusEffectService.GetCreatureStatusEffects), args =>
            _statuses.TryGetValue((uint)args[0]!, out var status) ? status : new CreatureStatusEffect());
        var party = ScriptedProxy.Create<IPartyService>(out var partyCalls);
        partyCalls.On(nameof(IPartyService.IsInParty), args => _parties.Contains(((uint)args[0]!, (uint)args[1]!)));
        var skill = ScriptedProxy.Create<ISkillService>(out var skillCalls);
        skillCalls
            .On(nameof(ISkillService.GetWeaponSkill), args => _weaponSkills.GetValueOrDefault((uint)args[1]!, SkillType.Invalid))
            .On(nameof(ISkillService.GetSkillTypeByBaseItem), args => _baseItemSkills.GetValueOrDefault((BaseItem)args[0]!, SkillType.Invalid));
        var perk = ScriptedProxy.Create<IPerkService>(out _perkCalls);
        _perkCalls
            .On(nameof(IPerkService.IsPerkInCategory), args => _perkCategories.Contains(((PerkType)args[0]!, (int)args[1]!)))
            .On(nameof(IPerkService.GetTargetedStatBonus), _ => _targetedPerkBonus);
        var item = ScriptedProxy.Create<IItemService>(out var itemCalls);
        itemCalls
            .On("get_StaffBaseItemTypes", _ => new List<BaseItem> { BaseItem.QuarterStaff })
            .On(nameof(IItemService.GetWeaponDamageAbilityType), _ => AbilityType.Might)
            .On(nameof(IItemService.GetWeaponAccuracyAbilityType), _ => AbilityType.Perception);
        var twinPistol = ScriptedProxy.Create<ITwinPistolEquipmentService>(out var twinCalls);
        twinCalls.On(nameof(ITwinPistolEquipmentService.GetCompanionWeapon), _ => _companionWeapon);

        _service = new CombatQueriesService(stat, statusEffect, party, skill, perk, item, twinPistol);
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    private void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    private void Place(uint creature, float x, float y, float facing = 0f)
    {
        _positions[creature] = new Vector3(x, y, 0f);
        _facings[creature] = facing;
    }

    private void Equip(uint creature, InventorySlot slot, uint item, BaseItem baseItem)
    {
        _slots[(creature, slot)] = item;
        _baseItems[item] = baseItem;
        _possessors[item] = creature;
        _valid.Add(item);
    }

    private static IStatusEffect Status(uint source, StatusEffectCategory categories = 0, params (StatType Stat, int Value)[] stats)
    {
        var group = new StatGroup();
        foreach (var (type, value) in stats)
            group.Stats[type] = value;
        var effect = ScriptedProxy.Create<IStatusEffect>(out var proxy);
        proxy.On("get_Source", _ => source)
            .On("get_Categories", _ => categories)
            .On("get_StatGroup", _ => group)
            .On("get_Id", _ => Guid.NewGuid().ToString())
            .On("get_ActivationType", _ => default(StatusEffectActivationType))
            .On("get_SourceType", _ => default(StatusEffectSourceType));
        return effect;
    }

    private void Give(uint creature, params IStatusEffect[] effects)
    {
        var status = new CreatureStatusEffect();
        foreach (var effect in effects)
            status.Add(effect);
        _statuses[creature] = status;
    }

    private static StatAdjustmentSource Source(string key, params (StatType Stat, int Value)[] stats) =>
        new(key, stats.ToDictionary(stat => stat.Stat, stat => stat.Value));

    [Test]
    public void ABossHasAHigherHitFloorThanOtherCreatures()
    {
        _service.GetMinimumHitRate(Defender).Should().Be(CombatQueriesService.BossMinimumHitRate);
        _service.GetMinimumHitRate(Attacker).Should().Be(CombatRules.MinimumHitRate);
    }

    [Test]
    public void RangedAttackAdjustmentsApplyOnlyToRangedWeaponSkills()
    {
        Stat(Attacker, StatType.RangedAttackDamageFlatAdjustment, 4);
        Stat(Attacker, StatType.RangedAttackDefenseIgnorePercentAdjustment, 12);

        _service.GetRangedAttackDamageFlatAdjustment(Attacker, SkillType.Rifle).Should().Be(4);
        _service.GetRangedAttackDefenseIgnorePercentAdjustment(Attacker, SkillType.Pistol).Should().Be(12);
        _service.GetRangedAttackDamageFlatAdjustment(Attacker, SkillType.Vibroblade).Should().Be(0);
        _service.GetRangedAttackDefenseIgnorePercentAdjustment(Attacker, SkillType.Force).Should().Be(0);
    }

    [Test]
    public void FacingDecidesBehindBesideAndNotFacing()
    {
        // The defender faces east (0 degrees).
        Place(Defender, 0, 0);
        Place(Attacker, -2, 0);
        _service.IsAttackerBehindTarget(Attacker, Defender).Should().BeTrue();
        _service.IsAttackerBesideTarget(Attacker, Defender).Should().BeFalse();
        _service.IsTargetNotFacingAttacker(Attacker, Defender).Should().BeTrue();

        Place(Attacker, 0, 2);
        _service.IsAttackerBehindTarget(Attacker, Defender).Should().BeFalse();
        _service.IsAttackerBesideTarget(Attacker, Defender).Should().BeTrue();
        _service.IsTargetNotFacingAttacker(Attacker, Defender).Should().BeFalse("90 degrees is still facing");

        Place(Attacker, 2, 0);
        _service.IsAttackerBehindTarget(Attacker, Defender).Should().BeFalse();
        _service.IsAttackerBesideTarget(Attacker, Defender).Should().BeFalse();
        _service.IsTargetNotFacingAttacker(Attacker, Defender).Should().BeFalse();

        Place(Defender, 0, 0, 180f);
        _service.IsAttackerBehindTarget(Attacker, Defender).Should().BeTrue("the defender turned around");
    }

    [Test]
    public void FacingIsUnknownForInvalidSeparatedOrOverlappingCreatures()
    {
        Place(Defender, 0, 0);
        Place(Attacker, -2, 0);

        _areas[Attacker] = OtherArea;
        _service.IsAttackerBehindTarget(Attacker, Defender).Should().BeFalse();
        _areas[Attacker] = Area;

        Place(Attacker, 0, 0.0005f);
        _service.IsAttackerBehindTarget(Attacker, Defender).Should().BeFalse();
        _service.IsAttackerBesideTarget(Attacker, Defender).Should().BeFalse();

        Place(Attacker, -2, 0);
        _valid.Remove(Attacker);
        _service.IsTargetNotFacingAttacker(Attacker, Defender).Should().BeFalse();
        _valid.Add(Attacker);
        _valid.Remove(Defender);
        _service.IsAttackerBehindTarget(Attacker, Defender).Should().BeFalse();
    }

    [Test]
    public void BackAndSideAttacksNeedAWeaponSkillAndTheSideSkillSelector()
    {
        Place(Defender, 0, 0);
        Place(Attacker, -2, 0);
        _service.IsMatchingBackAttack(Attacker, Defender, SkillType.Vibroblade).Should().BeTrue();
        _service.IsMatchingBackAttack(Attacker, Defender, SkillType.Force).Should().BeFalse();

        Place(Attacker, 0, -2);
        _service.IsMatchingSideAttack(Attacker, Defender, SkillType.Vibroblade).Should().BeFalse("no side skill is selected");
        Stat(Attacker, StatType.SideAttackSkillType, (int)SkillType.Vibroblade);
        _service.IsMatchingSideAttack(Attacker, Defender, SkillType.Vibroblade).Should().BeTrue();
        _service.IsMatchingSideAttack(Attacker, Defender, SkillType.Rifle).Should().BeFalse();
    }

    [Test]
    public void TheNearestHostileLivingCreatureInRangeIsChosen()
    {
        Place(Attacker, 0, 0);
        Place(Defender, 3, 0);
        Place(Ally, 1, 0);
        Place(Stranger, 2, 0);
        const uint vanished = 555;
        _valid.Add(vanished);
        _engine.SetReturnSequence("GetFirstObjectInShape", Defender);
        _engine.SetReturnSequence("GetNextObjectInShape", Ally, vanished, Stranger, 0u);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", args =>
        {
            // The search found the creature, which was gone by the time the loop reached it.
            _valid.Remove(vanished);
            return (uint)args[0]! != Ally;
        });
        _engine.SetReturnHandler("GetIsDead", _ => false);

        _service.GetNearestHostileCreatureWithinRange(Attacker, Attacker, 5f).Should().Be(Stranger);
        _engine.CallsTo("GetIsReactionTypeHostile").Select(call => call.Arguments[0]).Should().Equal(Defender, Ally, Stranger);
        _engine.CallsTo("GetFirstObjectInShape").Single().Arguments[1].Should().Be(5f);
    }

    [Test]
    public void AnExcludedOrDeadCreatureIsSkippedAndNothingIsFoundWithoutAValidSearch()
    {
        Place(Attacker, 0, 0);
        Place(Defender, 1, 0);
        Place(Stranger, 2, 0);
        _engine.SetReturnSequence("GetFirstObjectInShape", Defender);
        _engine.SetReturnSequence("GetNextObjectInShape", Stranger, 0u);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => true);
        _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Stranger);

        _service.GetNearestHostileCreatureWithinRange(Attacker, Attacker, 5f, Defender).Should().Be(NWScript.OBJECT_INVALID);

        _service.GetNearestHostileCreatureWithinRange(Attacker, Attacker, 0f).Should().Be(NWScript.OBJECT_INVALID);
        _valid.Remove(Attacker);
        _service.GetNearestHostileCreatureWithinRange(Attacker, Defender, 5f).Should().Be(NWScript.OBJECT_INVALID);
        _service.GetNearestHostileCreatureWithinRange(Defender, Attacker, 5f).Should().Be(NWScript.OBJECT_INVALID);
    }

    [Test]
    public void StealthOrEitherInvisibilityCounts()
    {
        _service.IsStealthedOrInvisible(Attacker).Should().BeFalse();

        _actionModes.Add((Attacker, ActionMode.Stealth));
        _service.IsStealthedOrInvisible(Attacker).Should().BeTrue();
        _actionModes.Clear();

        _effects.Add(EffectTypeScript.Haste);
        _service.IsStealthedOrInvisible(Attacker).Should().BeFalse();
        _effects.Add(EffectTypeScript.ImprovedInvisibility);
        _service.IsStealthedOrInvisible(Attacker).Should().BeTrue();
        _effects.Clear();
        _effects.Add(EffectTypeScript.Invisibility);
        _service.IsStealthedOrInvisible(Attacker).Should().BeTrue();
    }

    [Test]
    public void PhysicalImmunityNeedsAPhysicalDamageTypeAndAValidDefender()
    {
        Stat(Defender, StatType.PhysicalDamageImmunity, 1);

        _service.HasDamageImmunity(Defender, CombatDamageType.Physical).Should().BeTrue();
        _service.HasDamageImmunity(Defender, CombatDamageType.Force).Should().BeFalse();
        _service.HasDamageImmunity(Attacker, CombatDamageType.Physical).Should().BeFalse();
        _valid.Remove(Defender);
        _service.HasDamageImmunity(Defender, CombatDamageType.Physical).Should().BeFalse();
    }

    [Test]
    public void ANearbyTargetIsValidInTheSameAreaAndWithinTheDistance()
    {
        Place(Attacker, 0, 0);
        Place(Defender, 3, 0);

        _service.IsNearbyTargetWithinDistance(Attacker, Defender, 3f).Should().BeTrue();
        _service.IsNearbyTargetWithinDistance(Attacker, Defender, 2.5f).Should().BeFalse();
        _areas[Defender] = OtherArea;
        _service.IsNearbyTargetWithinDistance(Attacker, Defender, 5f).Should().BeFalse();
        _areas[Defender] = Area;
        _valid.Remove(Defender);
        _service.IsNearbyTargetWithinDistance(Attacker, Defender, 5f).Should().BeFalse();
        _valid.Add(Defender);
        _valid.Remove(Attacker);
        _service.IsNearbyTargetWithinDistance(Attacker, Defender, 5f).Should().BeFalse();
    }

    [Test]
    public void StatusStatsAreSummedForTheSourceOrItsParty()
    {
        Give(Defender,
            Status(Attacker, 0, (StatType.DamageTakenPercentAdjustment, 5)),
            Status(Ally, 0, (StatType.DamageTakenPercentAdjustment, 7)),
            Status(Stranger, 0, (StatType.DamageTakenPercentAdjustment, 11)),
            Status(999u, 0, (StatType.DamageTakenPercentAdjustment, 13)));
        _parties.Add((Ally, Attacker));

        _service.GetStatusSourceStatAdjustment(Defender, Attacker, StatType.DamageTakenPercentAdjustment).Should().Be(5);
        _service.GetStatusSourcePartyStatAdjustment(Defender, Attacker, StatType.DamageTakenPercentAdjustment).Should().Be(12);
        _service.GetStatusSourceStatAdjustment(Defender, Attacker, StatType.SideAttackSkillType).Should().Be(0);

        _service.GetStatusSourceStatAdjustment(Defender, 555u, StatType.DamageTakenPercentAdjustment).Should().Be(0);
        _service.GetStatusSourcePartyStatAdjustment(Defender, 555u, StatType.DamageTakenPercentAdjustment).Should().Be(0);
        _valid.Remove(Defender);
        _service.GetStatusSourceStatAdjustment(Defender, Attacker, StatType.DamageTakenPercentAdjustment).Should().Be(0);
        _service.GetStatusSourcePartyStatAdjustment(Defender, Attacker, StatType.DamageTakenPercentAdjustment).Should().Be(0);
    }

    [Test]
    public void ABonusAgainstATargetOfTheSourcesStatusNeedsTheCategoryTheValueAndTheStatus()
    {
        Give(Defender, Status(Attacker, StatusEffectCategory.Bleeding), Status(Stranger, StatusEffectCategory.Venom));
        Stat(Attacker, StatType.DamageToSourceAppliedStatusTargetCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.DamageToSourceAppliedStatusTargetPercentAdjustment, 15);

        _service.GetDamageToSourceAppliedStatusTargetAdjustment(Attacker, Defender).Should().Be(15);
        Stat(Attacker, StatType.DamageToSourceAppliedStatusTargetCategory, (int)StatusEffectCategory.Venom);
        _service.GetDamageToSourceAppliedStatusTargetAdjustment(Attacker, Defender).Should().Be(0, "the poison is not the attacker's");
        Stat(Attacker, StatType.DamageToSourceAppliedStatusTargetCategory, 0);
        _service.GetDamageToSourceAppliedStatusTargetAdjustment(Attacker, Defender).Should().Be(0);
    }

    [Test]
    public void TheAbilityVersionAlsoChecksTheSkill()
    {
        Give(Defender, Status(Attacker, StatusEffectCategory.Bleeding));
        Stat(Attacker, StatType.AbilityDamageToSourceAppliedStatusTargetSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.AbilityDamageToSourceAppliedStatusTargetCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment, 20);

        _service.GetAbilityDamageToSourceAppliedStatusTargetAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(20);
        _service.GetAbilityDamageToSourceAppliedStatusTargetAdjustment(Attacker, Defender, SkillType.Pistol).Should().Be(0);
        Stat(Attacker, StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment, 0);
        _service.GetAbilityDamageToSourceAppliedStatusTargetAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);

        Stat(Attacker, StatType.AbilityDamageToSourceAppliedStatusTargetBonusCategory, (int)StatusEffectCategory.Bleeding);
        Stat(Attacker, StatType.AbilityDamageToSourceAppliedStatusTargetBonus, 6);
        _service.GetAbilityDamageToSourceAppliedStatusTargetBonus(Attacker, Defender).Should().Be(6);
        Give(Defender);
        _service.GetAbilityDamageToSourceAppliedStatusTargetBonus(Attacker, Defender).Should().Be(0);
    }

    [Test]
    public void StatusCategoryChecksNeedValidCreaturesAndACategory()
    {
        Give(Defender, Status(Attacker, StatusEffectCategory.Bleeding));

        _service.TargetHasSourceAppliedStatusCategory(Defender, Attacker, StatusEffectCategory.Bleeding).Should().BeTrue();
        _service.TargetHasSourceAppliedStatusCategory(Defender, Attacker, 0).Should().BeFalse();
        _service.TargetHasAnyStatusEffectCategory(Defender, StatusEffectCategory.Bleeding).Should().BeTrue();
        _service.TargetHasAnyStatusEffectCategory(Defender, StatusEffectCategory.Venom).Should().BeFalse();
        _service.TargetHasAnyStatusEffectCategory(Defender, 0).Should().BeFalse();
        _valid.Remove(Attacker);
        _service.TargetHasSourceAppliedStatusCategory(Defender, Attacker, StatusEffectCategory.Bleeding).Should().BeFalse();
        _valid.Remove(Defender);
        _service.TargetHasAnyStatusEffectCategory(Defender, StatusEffectCategory.Bleeding).Should().BeFalse();
    }

    [Test]
    public void AppliedStatusesMatchByTypeOrByCategory()
    {
        var bleed = typeof(BleedStatusEffect);
        var poison = typeof(PoisonStatusEffect);

        _service.AbilityAppliedAnyStatus(bleed, null!, bleed).Should().BeTrue();
        _service.AbilityAppliedAnyStatus(null!, new[] { poison }, bleed, poison).Should().BeTrue();
        _service.AbilityAppliedAnyStatus(poison, null!, bleed).Should().BeFalse();
        _service.AbilityAppliedAnyStatus(null!, new[] { poison }, bleed).Should().BeFalse();

        _service.AbilityAppliedAnyStatusCategory(bleed, null!, StatusEffectCategory.Bleeding).Should().BeTrue();
        _service.AbilityAppliedAnyStatusCategory(null!, new[] { typeof(string), bleed }, StatusEffectCategory.Bleeding).Should().BeTrue();
        _service.AbilityAppliedAnyStatusCategory(typeof(string), null!, StatusEffectCategory.Bleeding).Should().BeFalse();
        _service.AbilityAppliedAnyStatusCategory(null!, new[] { poison }, StatusEffectCategory.Bleeding).Should().BeFalse();
    }

    [Test]
    public void TheAbilityWeaponIsTheFirstEligibleHandThenTheNaturalWeapon()
    {
        Equip(Attacker, InventorySlot.RightHand, RightWeapon, BaseItem.Rifle);
        _service.GetAbilityAttackWeapon(Attacker, SkillType.Force).Should().Be(RightWeapon, "the ability's skill never decides the weapon");
        _service.GetRelevantSkillWeapon(Attacker, SkillType.Rifle).Should().Be(RightWeapon);
        _service.HasEquippedWeaponForAbility(Attacker).Should().BeTrue();

        _baseItems[RightWeapon] = BaseItem.Torch;
        Equip(Attacker, InventorySlot.LeftHand, LeftWeapon, BaseItem.Longsword);
        _service.GetRelevantSkillWeapon(Attacker, SkillType.Vibroblade).Should().Be(LeftWeapon);

        _baseItems[LeftWeapon] = BaseItem.Torch;
        _service.HasEquippedWeaponForAbility(Attacker).Should().BeFalse();
        _service.GetRelevantSkillWeapon(Attacker, SkillType.Vibroblade).Should().Be(RightWeapon, "nothing else is a weapon");

        Equip(Attacker, InventorySlot.CreatureBite, Bite, BaseItem.CreatureSlashWeapon);
        _service.GetRelevantSkillWeapon(Attacker, SkillType.BeastMastery).Should().Be(Bite);
        Equip(Attacker, InventorySlot.CreatureLeft, Claw, BaseItem.CreatureSlashWeapon);
        _service.GetRelevantSkillWeapon(Attacker, SkillType.BeastMastery).Should().Be(Claw);
        Equip(Attacker, InventorySlot.CreatureRight, Staff, BaseItem.CreatureSlashWeapon);
        _service.GetRelevantSkillWeapon(Attacker, SkillType.BeastMastery).Should().Be(Staff);

        _valid.Remove(Attacker);
        _service.HasEquippedWeaponForAbility(Attacker).Should().BeFalse();
    }

    [Test]
    public void ACompanionPistolTriggersAbilitiesOnlyAsTheActiveCompanionWeapon()
    {
        Equip(Attacker, InventorySlot.LeftHand, LeftWeapon, BaseItem.CompanionPistol);

        _service.CanItemTriggerWeaponAbility(LeftWeapon, SkillType.TwinPistol).Should().BeFalse();
        _service.HasEquippedWeaponForAbility(Attacker).Should().BeFalse();
        _companionWeapon = LeftWeapon;
        _service.CanItemTriggerWeaponAbility(LeftWeapon, SkillType.TwinPistol).Should().BeTrue();
        _service.HasEquippedWeaponForAbility(Attacker).Should().BeTrue();
        _service.CanItemTriggerWeaponAbility(LeftWeapon, SkillType.Force).Should().BeTrue("a non-weapon ability takes any weapon");

        Equip(Attacker, InventorySlot.RightHand, RightWeapon, BaseItem.Torch);
        _service.CanItemTriggerWeaponAbility(RightWeapon, SkillType.Rifle).Should().BeFalse("a torch is not a weapon");
        _service.CanItemTriggerWeaponAbility(555u, SkillType.Rifle).Should().BeFalse();
    }

    [Test]
    public void SkillSelectedStatsSumOnlyTheSourcesThatSelectTheSkill()
    {
        _sources[(Attacker, StatType.DamageTakenPercentAdjustment)] = new[]
        {
            Source("a", (StatType.SideAttackSkillType, (int)SkillType.Rifle), (StatType.DamageTakenPercentAdjustment, 3)),
            Source("b", (StatType.SideAttackSkillType, (int)SkillType.Pistol), (StatType.DamageTakenPercentAdjustment, 5)),
            Source("c", (StatType.DamageTakenPercentAdjustment, 7)),
        };

        _service.GetSkillSelectedStatAdjustment(Attacker, SkillType.Rifle, StatType.SideAttackSkillType, StatType.DamageTakenPercentAdjustment)
            .Should().Be(3);
        _service.GetSkillSelectedStatAdjustment(Attacker, SkillType.Rifle, StatType.SideAttackSkillType, StatType.DamageTakenPercentAdjustment, true)
            .Should().Be(10, "an unselected source applies to every skill");
    }

    [Test]
    public void ALongRangeTargetIsARangedAbilityAtTheMinimumRange()
    {
        Place(Attacker, 0, 0);
        Place(Defender, 10, 0);

        _service.IsRangedAbilityLongRangeTarget(Attacker, Defender, SkillType.Rifle).Should().BeFalse("no minimum range is set");
        Stat(Attacker, StatType.RangedAbilityLongRangeMinimumRangeMeters, 10);
        _service.IsRangedAbilityLongRangeTarget(Attacker, Defender, SkillType.Rifle).Should().BeTrue();
        _service.IsRangedAbilityLongRangeTarget(Attacker, Defender, SkillType.Vibroblade).Should().BeFalse();
        Stat(Attacker, StatType.RangedAbilityLongRangeMinimumRangeMeters, 11);
        _service.IsRangedAbilityLongRangeTarget(Attacker, Defender, SkillType.Rifle).Should().BeFalse();
        Stat(Attacker, StatType.RangedAbilityLongRangeMinimumRangeMeters, 10);
        _valid.Remove(Defender);
        _service.IsRangedAbilityLongRangeTarget(Attacker, Defender, SkillType.Rifle).Should().BeFalse();
    }

    [Test]
    public void ThePhysicalAndForceHitAdjustmentCoversWeaponsForceAndBeasts()
    {
        Stat(Attacker, StatType.PhysicalAndForceAbilityHitChancePercentAdjustment, -15);

        _service.GetPhysicalAndForceAbilityHitChanceAdjustment(Attacker, SkillType.Rifle).Should().Be(-15);
        _service.GetPhysicalAndForceAbilityHitChanceAdjustment(Attacker, SkillType.Force).Should().Be(-15);
        _service.GetPhysicalAndForceAbilityHitChanceAdjustment(Attacker, SkillType.BeastMastery).Should().Be(-15);
        _service.GetPhysicalAndForceAbilityHitChanceAdjustment(Attacker, SkillType.Mimicry).Should().Be(0);

        _service.IsWeaponOrForceDamage(SkillType.Force, CombatDamageType.Physical).Should().BeTrue();
        _service.IsWeaponOrForceDamage(SkillType.Mimicry, CombatDamageType.Force).Should().BeTrue();
        _service.IsWeaponOrForceDamage(SkillType.Rifle, CombatDamageType.Fire).Should().BeTrue();
        _service.IsWeaponOrForceDamage(SkillType.BeastMastery, CombatDamageType.Physical).Should().BeFalse();
    }

    [Test]
    public void TheAbilitySkillIsItsOwnOrTheEquippedWeapons()
    {
        _weaponSkills[RightWeapon] = SkillType.Invalid;
        _weaponSkills[LeftWeapon] = SkillType.Pistol;
        Equip(Attacker, InventorySlot.RightHand, RightWeapon, BaseItem.Torch);
        Equip(Attacker, InventorySlot.LeftHand, LeftWeapon, BaseItem.CompanionPistol);

        _service.GetAbilitySkillType(Attacker, new AbilityDetail { SkillType = SkillType.Force }).Should().Be(SkillType.Force);
        _service.GetAbilitySkillType(Attacker, null!).Should().Be(SkillType.Invalid);
        _service.GetAbilitySkillType(Attacker, new AbilityDetail { SkillType = SkillType.Invalid }).Should().Be(SkillType.Pistol);

        _weaponSkills[RightWeapon] = SkillType.Rifle;
        _service.GetEquippedWeaponSkillType(Attacker).Should().Be(SkillType.Rifle);
        _slots.Remove((Attacker, InventorySlot.RightHand));
        _service.GetEquippedWeaponSkillType(Attacker).Should().Be(SkillType.Pistol);
        _slots.Remove((Attacker, InventorySlot.LeftHand));
        _service.GetEquippedWeaponSkillType(Attacker).Should().Be(SkillType.Invalid);
        _valid.Remove(Attacker);
        _service.GetEquippedWeaponSkillType(Attacker).Should().Be(SkillType.Invalid);
    }

    [Test]
    public void AnAbilityMatchesAPerkStatWhenItsLevelPerkIsNamed()
    {
        var ability = new AbilityDetail { EffectiveLevelPerkType = PerkType.AlphaRhythm };

        _service.AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(Attacker, ability).Should().BeFalse();
        Stat(Attacker, StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType, (int)PerkType.AlphaRhythm);
        _service.AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(Attacker, ability).Should().BeTrue();
        Stat(Attacker, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType, (int)PerkType.AlphaRhythm);
        _service.AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(Attacker, ability).Should().BeTrue();

        _service.AbilityMatchesReversalCutTrigger(Attacker, ability).Should().BeFalse();
        Stat(Attacker, StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType, (int)PerkType.AlphaRhythm);
        _service.AbilityMatchesReversalCutTrigger(Attacker, ability).Should().BeTrue();

        _service.AbilityMatchesAnyPerkTypeStat(Attacker, null!, StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType).Should().BeFalse();
        _service.AbilityMatchesAnyPerkTypeStat(Attacker, new AbilityDetail(), StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType).Should().BeFalse();
    }

    [Test]
    public void AnAbilityMatchesAPerkCategoryThroughThePerkService()
    {
        var ability = new AbilityDetail { EffectiveLevelPerkType = PerkType.AlphaRhythm };

        _service.AbilityMatchesPerkCategoryStat(Attacker, ability, StatType.SideAttackSkillType).Should().BeFalse("no category is set");
        Stat(Attacker, StatType.SideAttackSkillType, 3);
        _service.AbilityMatchesPerkCategoryStat(Attacker, ability, StatType.SideAttackSkillType).Should().BeFalse();
        _perkCategories.Add((PerkType.AlphaRhythm, 3));
        _service.AbilityMatchesPerkCategoryStat(Attacker, ability, StatType.SideAttackSkillType).Should().BeTrue();
        _service.AbilityMatchesPerkCategoryStat(Attacker, null!, StatType.SideAttackSkillType).Should().BeFalse();
    }

    [Test]
    public void AnAbilityUsedTriggerReadsItsSkillAndRangedOnlyStats()
    {
        var hostile = new AbilityDetail { IsHostileAbility = true };
        Stat(Attacker, StatType.SideAttackSkillType, (int)SkillType.Rifle);

        _service.AbilityUsedTriggerSkillMatches(Attacker, hostile, SkillType.Rifle, StatType.SideAttackSkillType, null)
            .Should().Be(CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Rifle, SkillType.Rifle, false, true));
        _service.AbilityUsedTriggerSkillMatches(Attacker, null!, SkillType.Pistol, StatType.SideAttackSkillType, StatType.PhysicalDamageImmunity)
            .Should().Be(CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Pistol, SkillType.Rifle, false, false));
        Stat(Attacker, StatType.PhysicalDamageImmunity, 1);
        _service.AbilityUsedTriggerSkillMatches(Attacker, hostile, SkillType.Pistol, StatType.SideAttackSkillType, StatType.PhysicalDamageImmunity)
            .Should().Be(CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Pistol, SkillType.Rifle, true, true));
    }

    [Test]
    public void ImpactWeaponDamageCountsOnlyForWeaponOrQueuedNaturalWeaponImpacts()
    {
        _service.IncludesCombatImpactWeaponDamage(SkillType.Rifle, false).Should().BeTrue();
        _service.IncludesCombatImpactWeaponDamage(SkillType.BeastMastery, false).Should().BeFalse();
        _service.IncludesCombatImpactWeaponDamage(SkillType.BeastMastery, true).Should().BeTrue();
        _service.IncludesCombatImpactWeaponDamage(SkillType.Force, true).Should().BeFalse();

        _service.GetCombatImpactWeaponDamage(Attacker, SkillType.Force).Should().Be(0);
        _service.GetCombatImpactWeaponDamage(Attacker, SkillType.Rifle, triggeringWeaponDamage: 17).Should().Be(17);
        _service.GetCombatImpactWeaponDamage(Attacker, SkillType.Rifle).Should().Be(0, "no hand holds a weapon");
        _service.GetCombatImpactWeaponDamageType(Attacker, SkillType.Force).Should().Be(CombatDamageType.Physical);
        _service.GetCombatImpactWeaponDamageType(Attacker, SkillType.Rifle).Should().Be(CombatDamageType.Physical);
    }

    [Test]
    public void TheImpactWeaponIsTheHandThatCanTriggerTheAbility()
    {
        Equip(Attacker, InventorySlot.RightHand, RightWeapon, BaseItem.Rifle);
        Equip(Attacker, InventorySlot.LeftHand, LeftWeapon, BaseItem.Longsword);
        Equip(Attacker, InventorySlot.CreatureRight, Claw, BaseItem.CreatureSlashWeapon);

        _service.GetCombatImpactWeaponDamage(Attacker, SkillType.Vibroblade, abilityImpact: true)
            .Should().Be(WeaponDamage.GetAbilityDMG(Attacker, RightWeapon, SkillType.Vibroblade), "any weapon hand delivers it");
        _service.GetCombatImpactWeaponDamage(Attacker, SkillType.Vibroblade, requireMatchingSkill: true)
            .Should().Be(WeaponDamage.GetEffectiveDMG(Attacker, LeftWeapon), "only the left hand matches the skill");
        _service.GetCombatImpactWeaponDamage(Attacker, SkillType.BeastMastery, usesQueuedNaturalWeapon: true)
            .Should().Be(WeaponDamage.GetEffectiveDMG(Attacker, Claw));
        _service.GetCombatImpactWeaponDamage(Attacker, SkillType.Staff, requireMatchingSkill: true).Should().Be(0);
        _service.GetCombatImpactWeaponDamageType(Attacker, SkillType.Rifle).Should().Be(WeaponDamage.GetDamageType(RightWeapon));
    }

    [Test]
    public void TargetedAbilityAdjustmentsAddThePerkBonusAndMatchingStatuses()
    {
        _targetedPerkBonus = 4;
        Give(Attacker,
            Status(Ally, 0, (StatType.SideAttackSkillType, (int)PerkType.AlphaRhythm), (StatType.DamageTakenPercentAdjustment, 6)),
            Status(Ally, 0, (StatType.PhysicalDamageImmunity, (int)PerkType.AlphaRhythm), (StatType.DamageTakenPercentAdjustment, 2)),
            Status(Ally, 0, (StatType.DamageTakenPercentAdjustment, 50)));

        _service.GetTargetedAbilityAdjustment(Attacker, PerkType.AlphaRhythm, StatType.SideAttackSkillType,
            StatType.PhysicalDamageImmunity, StatType.DamageTakenPercentAdjustment).Should().Be(12);
        _perkCalls.Calls.Single().Arguments.Should().Equal(Attacker, PerkType.AlphaRhythm, StatType.SideAttackSkillType,
            StatType.PhysicalDamageImmunity, StatType.DamageTakenPercentAdjustment);
        _service.GetTargetedAbilityAdjustment(Attacker, PerkType.Invalid, StatType.SideAttackSkillType,
            StatType.PhysicalDamageImmunity, StatType.DamageTakenPercentAdjustment).Should().Be(0);
    }

    [Test]
    public void StaffOverridesPickTheAbilityForStavesOnly()
    {
        _service.GetWeaponDamageAbilityType(Attacker, BaseItem.QuarterStaff).Should().Be(AbilityType.Might, "no override is set");
        _service.GetWeaponAccuracyAbilityType(Attacker, BaseItem.QuarterStaff).Should().Be(AbilityType.Perception);

        Stat(Attacker, StatType.StaffDamageAbilityOverride, (int)AbilityType.Willpower + 1);
        Stat(Attacker, StatType.StaffAccuracyAbilityOverride, (int)AbilityType.Agility + 1);
        _service.GetWeaponDamageAbilityType(Attacker, BaseItem.QuarterStaff).Should().Be(AbilityType.Willpower);
        _service.GetWeaponAccuracyAbilityType(Attacker, BaseItem.QuarterStaff).Should().Be(AbilityType.Agility);
        _service.GetWeaponDamageAbilityType(Attacker, BaseItem.Longsword).Should().Be(AbilityType.Might);
        _service.GetWeaponAccuracyAbilityType(Attacker, BaseItem.Longsword).Should().Be(AbilityType.Perception);

        Stat(Attacker, StatType.StaffDamageAbilityOverride, (int)AbilityType.Social + 2);
        _service.GetWeaponDamageAbilityType(Attacker, BaseItem.QuarterStaff).Should().Be(AbilityType.Might, "out of range");
    }

    [Test]
    public void QueuedAbilityAccuracyFollowsTheWeaponFamily()
    {
        Stat(Attacker, StatType.StaffAccuracyAbilityOverride, (int)AbilityType.Willpower + 1);

        _service.GetQueuedAbilityAccuracyAbilityType(Attacker, SkillType.Staff).Should().Be(AbilityType.Willpower);
        _service.GetQueuedAbilityAccuracyAbilityType(Attacker, SkillType.Rifle).Should().Be(AbilityType.Agility);
        _service.GetQueuedAbilityAccuracyAbilityType(Attacker, SkillType.Vibroblade).Should().Be(AbilityType.Perception);
        _service.GetQueuedAbilityAccuracyAbilityType(Attacker, SkillType.Force).Should().Be(AbilityType.Invalid);
    }

    [Test]
    public void PowerAttackAndMightMultipliersMakeTheMiscellaneousBonus()
    {
        _engine.SetReturnHandler("GetAbilityModifier", _ => 3);
        _service.GetPowerAttackDMGBonus(Attacker).Should().Be(0);
        _service.GetMiscDMGBonus(Attacker, BaseItem.Longsword).Should().Be(0);

        _actionModes.Add((Attacker, ActionMode.ImprovedPowerAttack));
        _service.GetPowerAttackDMGBonus(Attacker).Should().Be(6);
        _actionModes.Add((Attacker, ActionMode.PowerAttack));
        _service.GetPowerAttackDMGBonus(Attacker).Should().Be(3);

        Stat(Attacker, StatType.WeaponMightModifierDamageMultiplier, 2);
        Stat(Attacker, StatType.StaffMightModifierDamageMultiplier, 1);
        _service.GetMiscDMGBonus(Attacker, BaseItem.Longsword).Should().Be(3 + 3 * 2);
        _service.GetMiscDMGBonus(Attacker, BaseItem.QuarterStaff).Should().Be(3 + 3 * 2 + 3);
        _engine.SetReturnHandler("GetAbilityModifier", _ => -2);
        _service.GetMiscDMGBonus(Attacker, BaseItem.QuarterStaff).Should().Be(3, "a negative Might adds nothing");
    }

    [Test]
    public void ThePerkAdjustedAbilityScoreReadsTheRightHandWeaponsDamageAbility()
    {
        _engine.SetReturnHandler("GetAbilityScore", args => (AbilityType)args[1]! == AbilityType.Might ? 18 : 9);

        _service.GetPerkAdjustedAbilityScore(Attacker).Should().Be(0, "no weapon");
        Equip(Attacker, InventorySlot.RightHand, RightWeapon, BaseItem.Longsword);
        _service.GetPerkAdjustedAbilityScore(Attacker).Should().Be(18);
    }

    [Test]
    public void TheWeaponImpactProfileTableHoldsOneSourcePerActor()
    {
        var first = (WeaponImpactProfile)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(WeaponImpactProfile));
        var second = (WeaponImpactProfile)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(WeaponImpactProfile));

        _service.GetWeaponImpactProfile(Attacker).Should().BeNull();
        _service.SetWeaponImpactProfile(Attacker, first);
        _service.SetWeaponImpactProfile(Defender, second);
        _service.GetWeaponImpactProfile(Attacker).Should().BeSameAs(first);
        _service.SetWeaponImpactProfile(Attacker, second);
        _service.GetWeaponImpactProfile(Attacker).Should().BeSameAs(second);
        _service.RemoveWeaponImpactProfile(Attacker);
        _service.GetWeaponImpactProfile(Attacker).Should().BeNull();
        _service.GetWeaponImpactProfile(Defender).Should().BeSameAs(second);
    }
}

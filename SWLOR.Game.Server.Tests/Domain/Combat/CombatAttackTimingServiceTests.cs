using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The attack timing of <c>Combat</c>: the one-shot no-delay, critical-rate and next-ability buffs it grants and consumes
/// through the temporary stat modifiers, the delay reductions it reads, paralysis before a delayed swing, and the weapon
/// delay of an item. The swing debt itself is covered through the facade by <c>CombatAttackDelayTests</c>. The temporary
/// modifiers, stats and status effects are scripted proxies over tables the test fills; the engine is a recording fake.
/// </summary>
[TestFixture]
public class CombatAttackTimingServiceTests
{
    private const uint Attacker = 11;
    private const uint Target = 22;
    private const uint Gone = 44;

    private readonly List<IDisposable> _scopes = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private CombatAttackTimingService _service = null!;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<(uint, StatType, string), int> _temporary = null!;
    private List<string> _temporaryCalls = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _players = null!;
    private (bool Has, int Percent) _limited;
    private uint _twinCompanion;
    private SkillType? _abilitySkill;
    private bool _bleeding;
    private ScriptedProxy _feedbackCalls = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        // The dual-wield check reads the twin pistol, power glove and item facades.
        _scopes.Add(_facades.Install());
        _stats = new();
        _temporary = new();
        _temporaryCalls = new();
        _invalid = new();
        _players = new();
        _limited = (false, 0);
        _twinCompanion = 0;
        _abilitySkill = null;
        _bleeding = false;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetObjectType", _ => ObjectType.Creature);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var queries = ScriptedProxy.Create<ICombatQueriesService>(out var queryCalls);
        queryCalls.On(nameof(ICombatQueriesService.GetAbilitySkillType), args => _abilitySkill ?? ((AbilityDetail?)args[1])?.SkillType ?? SkillType.Invalid);
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls.On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)));
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.TryGetLimitedAttackDelayReduction), args =>
            {
                args[2] = _limited.Percent;
                args[3] = 3;
                return _limited.Has;
            })
            .On(nameof(IStatusEffectService.HasStatusEffectCategory), _ => _bleeding);
        var temporary = ScriptedProxy.Create<ITemporaryStatModifierService>(out var temporaryCalls);
        temporaryCalls
            .On(nameof(ITemporaryStatModifierService.GetStatAdjustment), args =>
                _temporary.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!, args[2]!.ToString()!)))
            .On(nameof(ITemporaryStatModifierService.Consume), args =>
            {
                _temporaryCalls.Add($"Consume {args[1]} {args[2]}");
                _temporary.Remove(((uint)args[0]!, (StatType)args[1]!, args[2]!.ToString()!), out var value);
                return value;
            })
            .On(nameof(ITemporaryStatModifierService.Replace), args =>
            {
                _temporaryCalls.Add($"Replace {args[1]} {args[2]} {args[3]} {args[4]}");
                _temporary[((uint)args[0]!, (StatType)args[1]!, args[4]!.ToString()!)] = (int)args[2]!;
                return null;
            });
        var twinPistols = ScriptedProxy.Create<ITwinPistolEquipmentService>(out var twinCalls);
        twinCalls
            .On(nameof(ITwinPistolEquipmentService.IsActivePair), _ => _twinCompanion != 0)
            .On(nameof(ITwinPistolEquipmentService.GetCompanionWeapon), _ => _twinCompanion);
        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out _feedbackCalls);
        _feedbackCalls.On(nameof(IPlayerFeedbackService.SendWarningNearby), _ => null);
        var playerName = ScriptedProxy.Create<IPlayerNameService>(out var nameCalls);
        nameCalls.On(nameof(IPlayerNameService.GetDisplayName), args => $"name of {args[1]} for {args[0]}");

        _service = new CombatAttackTimingService(queries, stat, statusEffect, temporary, twinPistols, feedback, playerName);
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    private void Stat(StatType stat, int value) => _stats[(Attacker, stat)] = value;

    private void Temporary(StatType stat, object group, int value) => _temporary[(Attacker, stat, group.ToString()!)] = value;

    private static AbilityDetail Hostile(SkillType skill = SkillType.Rifle) => new() { IsHostileAbility = true, SkillType = skill };

    [Test]
    public void AnArmedNextAbilityBuffIsSpentByTheMatchingHostileAbility()
    {
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, new AbilityDetail { SkillType = SkillType.Rifle }).Should().Be(0);
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(0, "nothing is armed");

        _service.GrantNextAbilityNoDelay(Attacker, (int)SkillType.Rifle, 10);
        _temporaryCalls.Should().Equal(
            $"Replace NextAttackNoDelay {(int)SkillType.Rifle} 10 NextAttackNoDelay",
            "Consume NextAttackDelayReductionPercent NextAttackNoDelay");
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile(SkillType.Pistol)).Should().Be(0, "armed for another skill");
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(100, "a full arm removes the delay");
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(0, "spent");

        _service.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 10, 40);
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(40);

        // Limited Haste adds its percent to the arm, capped at 100.
        _limited = (true, 25);
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(25, "Haste alone");
        _service.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 10, 90);
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(100);
    }

    [Test]
    public void ARangedNoDelayStatusOrSuppressionDecidesTheNextAbilityBeforeTheArm()
    {
        _service.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 10);
        Stat(StatType.WeaponAttackNoDelay, 1);
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(100);
        _temporary.Should().ContainKey((Attacker, StatType.NextAttackNoDelay, StatType.NextAttackNoDelay.ToString()),
            "the arm is kept while the status already removes the delay");

        Stat(StatType.AttackDelayReductionSuppressed, 1);
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(0);
        _service.IsAttackDelayReductionSuppressed(Attacker).Should().BeTrue();
        _service.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeFalse();
        _service.HasTemporaryNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeFalse();
        _service.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeFalse();

        _service.GrantNextAbilityNoDelay(Attacker, SkillType.Invalid, 10);
        _service.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 0);
        _invalid.Add(Gone);
        _service.GrantNextAbilityNoDelay(Gone, SkillType.Rifle, 10);
        _temporaryCalls.Should().HaveCount(2, "only the first grant reached the modifiers");
    }

    [Test]
    public void TheNextAutoAttackNoDelayIsSpentPerArmAndKeptUnderTheRangedStatus()
    {
        _service.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeFalse();
        _service.HasTemporaryNextAutoAttackNoDelay(Attacker, SkillType.Invalid).Should().BeFalse();
        _service.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeFalse();

        _service.GrantNextAutoAttackNoDelay(Attacker, SkillType.Rifle, 6);
        _service.HasTemporaryNextAutoAttackNoDelay(Attacker, SkillType.Pistol).Should().BeFalse();
        _service.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeTrue();
        _service.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Invalid).Should().BeFalse();
        _service.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeTrue();
        _service.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeFalse("spent");

        _service.GrantNextAutoAttackNoDelay(Attacker, 6);
        _service.HasTemporaryNextAutoAttackNoDelay(Attacker, SkillType.Invalid).Should().BeTrue("an all-skills arm needs no skill");
        _service.GrantNextAutoAttackNoDelay(Attacker, SkillType.Rifle, 6);
        _temporaryCalls.Clear();
        _service.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeTrue();
        _temporaryCalls.Should().Equal(
            "Consume NextAutoAttackNoDelayAllSkills NextAutoAttackNoDelayAllSkills",
            "Consume NextAutoAttackNoDelaySkillType NextAutoAttackNoDelaySkillType");

        _service.GrantNextAutoAttackNoDelay(Attacker, 6);
        Stat(StatType.WeaponAttackNoDelay, 1);
        _service.HasNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeTrue();
        _temporaryCalls.Clear();
        _service.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Rifle).Should().BeTrue();
        _temporaryCalls.Should().BeEmpty("the ranged status supplies this swing and the arm waits for a later one");
        _service.ConsumeNextAutoAttackNoDelay(Attacker, SkillType.Force).Should().BeTrue("the all-skills arm is still there");

        _service.GrantNextAutoAttackNoDelay(Attacker, SkillType.Invalid, 6);
        _service.GrantNextAutoAttackNoDelay(Attacker, SkillType.Rifle, 0);
        _service.GrantNextAutoAttackNoDelay(Attacker, 0);
        _invalid.Add(Gone);
        _service.GrantNextAutoAttackNoDelay(Gone, 6);
        _service.GrantNextAutoAttackNoDelay(Gone, SkillType.Rifle, 6);
        _temporaryCalls.Should().HaveCount(1, "the invalid grants change nothing");
    }

    [Test]
    public void TheNextAutoAttackCriticalRateBonusIsSpentByItsSkill()
    {
        _service.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Invalid).Should().Be(0);
        _service.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(0);

        _service.GrantNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle, 12, 6);
        _temporaryCalls.Should().Equal(
            $"Replace NextAutoAttackCriticalRateSkillType {(int)SkillType.Rifle} 6 NextAutoAttackCriticalRateSkillType",
            "Replace NextAutoAttackCriticalRatePercentAdjustment 12 6 NextAutoAttackCriticalRateSkillType");
        _service.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Pistol).Should().Be(0);
        _service.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(12);
        _service.ConsumeNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(0);

        _temporaryCalls.Clear();
        _service.GrantNextAutoAttackCriticalRateBonus(Attacker, SkillType.Invalid, 12, 6);
        _service.GrantNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle, 0, 6);
        _service.GrantNextAutoAttackCriticalRateBonus(Attacker, SkillType.Rifle, 12, 0);
        _invalid.Add(Gone);
        _service.GrantNextAutoAttackCriticalRateBonus(Gone, SkillType.Rifle, 12, 6);
        _temporaryCalls.Should().BeEmpty();
    }

    [Test]
    public void NextSkillAbilityBonusesShareOneSkillAndCategoryGroup()
    {
        _service.GrantNextSkillAbilityBonuses(Attacker, SkillType.Rifle, 0, 0, 10);
        _service.GrantNextSkillAbilityBonuses(Attacker, SkillType.Rifle, 5, 0, 0);
        _invalid.Add(Gone);
        _service.GrantNextSkillAbilityBonuses(Gone, SkillType.Rifle, 5, 0, 10);
        _temporaryCalls.Should().BeEmpty();

        _service.GrantNextSkillAbilityBonuses(Attacker, SkillType.Rifle, 5, 7, 10, 3, StatusEffectCategory.Bleeding);
        _temporaryCalls.Should().Equal(
            $"Replace NextSkillAbilitySkillType {(int)SkillType.Rifle} 10 NextSkillAbilitySkillType",
            $"Replace NextSkillAbilityRequiredCategory {(int)StatusEffectCategory.Bleeding} 10 NextSkillAbilitySkillType",
            "Replace NextSkillAbilityDamageBonus 5 10 NextSkillAbilitySkillType",
            "Replace NextSkillAbilityCriticalRatePercentAdjustment 7 10 NextSkillAbilitySkillType",
            "Replace NextSkillAbilityDefenseIgnorePercentAdjustment 3 10 NextSkillAbilitySkillType");

        _temporaryCalls.Clear();
        _service.GrantNextSkillAbilityBonuses(Attacker, SkillType.Rifle, 0, 0, 10, 4);
        _temporaryCalls.Should().Equal(
            $"Replace NextSkillAbilitySkillType {(int)SkillType.Rifle} 10 NextSkillAbilitySkillType",
            $"Replace NextSkillAbilityRequiredCategory {(int)StatusEffectCategory.None} 10 NextSkillAbilitySkillType",
            "Replace NextSkillAbilityDefenseIgnorePercentAdjustment 4 10 NextSkillAbilitySkillType");
    }

    [Test]
    public void PerkGroupedAndSingleStatGrantsCheckTheirInputs()
    {
        var group = $"{nameof(PerkType)}:{(int)PerkType.AimedShot}";
        _service.GrantNextAbilityDamageBonus(Attacker, PerkType.AimedShot, 4, 10);
        _service.GrantStatusAppliedNextAttackDamageBonus(Attacker, 3, 10);
        _service.GrantNextSkillAutoAttackDamageBonus(Attacker, SkillType.Rifle, 2, 10);
        _service.GrantNextAbilityFPCostAdjustment(Attacker, SkillType.Force, -1, 10);
        InvokePrivate("GrantNextAbilityStaminaCostAdjustment", Attacker, PerkType.AimedShot, -2, 10);
        _temporaryCalls.Should().Equal(
            $"Replace NextAbilityDamageBonus 4 10 {group}",
            "Replace NextAttackStatusAppliedDMGBonus 3 10 NextAttackStatusAppliedDMGBonus",
            $"Replace NextSkillAutoAttackDamageBonusSkillType {(int)SkillType.Rifle} 10 NextSkillAutoAttackDamageBonusSkillType",
            "Replace NextSkillAutoAttackDamageBonus 2 10 NextSkillAutoAttackDamageBonusSkillType",
            $"Replace NextAbilityFPCostAdjustmentSkillType {(int)SkillType.Force} 10 NextAbilityFPCostAdjustmentSkillType",
            "Replace NextAbilityFPCostAdjustment -1 10 NextAbilityFPCostAdjustmentSkillType",
            $"Replace NextAbilityStaminaCostAdjustment -2 10 {group}");

        _temporaryCalls.Clear();
        _invalid.Add(Gone);
        foreach (var (creature, perk, amount, seconds) in new[]
                 {
                     (Gone, PerkType.AimedShot, 4, 10), (Attacker, PerkType.Invalid, 4, 10),
                     (Attacker, PerkType.AimedShot, 0, 10), (Attacker, PerkType.AimedShot, 4, 0),
                 })
        {
            _service.GrantNextAbilityDamageBonus(creature, perk, amount, seconds);
            InvokePrivate("GrantNextAbilityStaminaCostAdjustment", creature, perk, amount, seconds);
        }

        foreach (var (creature, skill, amount, seconds) in new[]
                 {
                     (Gone, SkillType.Rifle, 4, 10), (Attacker, SkillType.Invalid, 4, 10),
                     (Attacker, SkillType.Rifle, 0, 10), (Attacker, SkillType.Rifle, 4, 0),
                 })
        {
            _service.GrantNextSkillAutoAttackDamageBonus(creature, skill, amount, seconds);
            _service.GrantNextAbilityFPCostAdjustment(creature, skill, amount, seconds);
        }

        _service.GrantStatusAppliedNextAttackDamageBonus(Gone, 3, 10);
        _service.GrantStatusAppliedNextAttackDamageBonus(Attacker, 0, 10);
        _service.GrantStatusAppliedNextAttackDamageBonus(Attacker, 3, 0);
        _temporaryCalls.Should().BeEmpty();
    }

    private void InvokePrivate(string name, params object[] arguments) =>
        typeof(CombatAttackTimingService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_service, arguments);

    [Test]
    public void AStaminaDiscountIsGroupedBySkillByDefaultAndCuedOnceToAPlayer()
    {
        _players.Add(Attacker);
        var skillGroup = $"{StatType.NextSkillAbilityStaminaCostAdjustmentSkillType}:{(int)SkillType.Rifle}";

        _service.GrantCuedNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, -3, 0);
        _temporaryCalls.Should().BeEmpty("no window, no grant");

        _service.GrantCuedNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, -3, 8, rangedOnly: true);
        _temporaryCalls.Should().Equal(
            $"Replace NextSkillAbilityStaminaCostAdjustmentSkillType {(int)SkillType.Rifle} 8 {skillGroup}",
            $"Replace NextSkillAbilityStaminaCostAdjustment -3 8 {skillGroup}",
            $"Replace NextSkillAbilityStaminaCostAdjustmentRangedOnly 1 8 {skillGroup}",
            $"Replace NextSkillAbilityStaminaCostAdjustmentHostileOnly 0 8 {skillGroup}");
        _engine.CallsTo("FloatingTextStringOnCreature").Select(call => call.Arguments[0]).Should().Equal(
            SWLOR.Game.Server.Domain.Foundation.ColorToken.Combat("Next Ability -3 STM"));

        _service.GrantCuedNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, -3, 8);
        _engine.CallsTo("FloatingTextStringOnCreature").Should().HaveCount(1, "an armed discount is not cued again");

        _temporaryCalls.Clear();
        _service.GrantCuedNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, -2, 8, "own group", hostileOnly: true);
        _temporaryCalls.Last().Should().Be("Replace NextSkillAbilityStaminaCostAdjustmentHostileOnly 1 8 own group");
        _engine.CallsTo("FloatingTextStringOnCreature").Should().HaveCount(2);

        _temporaryCalls.Clear();
        _service.GrantNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, 0, 8);
        _service.GrantNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, -1, 0);
        _invalid.Add(Gone);
        _service.GrantNextSkillAbilityStaminaCostAdjustment(Gone, SkillType.Rifle, -1, 8);
        _temporaryCalls.Should().BeEmpty();

        _service.ShowReadyCue(Attacker, false, 0, "nothing to cue");
        _players.Clear();
        _service.ShowReadyCue(Attacker, false, 2, "not a player");
        _engine.CallsTo("FloatingTextStringOnCreature").Should().HaveCount(2);
    }

    [Test]
    public void AnExpiredBleedGrantsItsSourcesConfiguredStaminaDiscount()
    {
        Stat(StatType.BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment, -4);
        Stat(StatType.BleedingStatusExpiredNextSkillAbilitySkillType, (int)SkillType.Vibroblade);
        Stat(StatType.BleedingStatusExpiredNextSkillAbilityWindowSeconds, 12);

        _service.ApplyBleedingStatusExpiredEffects(Attacker);

        var group = $"{StatType.NextSkillAbilityStaminaCostAdjustmentSkillType}:{(int)SkillType.Vibroblade}";
        _temporaryCalls.Should().Contain($"Replace NextSkillAbilityStaminaCostAdjustment -4 12 {group}");

        _temporaryCalls.Clear();
        _invalid.Add(Gone);
        _service.ApplyBleedingStatusExpiredEffects(Gone);
        _temporaryCalls.Should().BeEmpty();
    }

    [Test]
    public void DelayReductionsAreCappedAndTheTargetHasteOnlyHelpsThrowingAgainstBleedingTargets()
    {
        Stat(StatType.AttackDelayReductionPercent, 80);
        _service.CalculateAttackDelayReduction(Attacker).Should().Be(CombatRules.MaximumAttackDelayAdjustmentPercent);
        Stat(StatType.AttackDelayReductionPercent, -80);
        _service.CalculateAttackDelayReduction(Attacker).Should().Be(-CombatRules.MaximumAttackDelayAdjustmentPercent);
        _invalid.Add(Gone);
        _service.CalculateAttackDelayReduction(Gone).Should().Be(0);

        Stat(StatType.OffhandAttackDelayReductionPercent, -5);
        _service.CalculateOffhandAttackDelayReduction(Attacker).Should().Be(0, "an offhand penalty does not apply");
        Stat(StatType.OffhandAttackDelayReductionPercent, 90);
        _service.CalculateOffhandAttackDelayReduction(Attacker).Should().Be(CombatRules.MaximumAttackDelayAdjustmentPercent);
        _service.CalculateOffhandAttackDelayReduction(Gone).Should().Be(0);

        Stat(StatType.ThrowingAttackDelayReductionToBleedingTargetPercent, 15);
        _service.GetTargetAttackDelayReduction(Attacker, Target, SkillType.Throwing).Should().Be(0, "the target is not bleeding");
        _bleeding = true;
        _service.GetTargetAttackDelayReduction(Attacker, Target, SkillType.Rifle).Should().Be(0);
        _service.GetTargetAttackDelayReduction(Attacker, Gone, SkillType.Throwing).Should().Be(0);
        _service.GetTargetAttackDelayReduction(Attacker, Target, SkillType.Throwing).Should().Be(15);
        _engine.SetReturnHandler("GetObjectType", _ => ObjectType.Placeable);
        _service.GetTargetAttackDelayReduction(Attacker, Target, SkillType.Throwing).Should().Be(0, "only a creature bleeds");
        _engine.SetReturnHandler("GetObjectType", _ => ObjectType.Creature);
        Stat(StatType.AttackDelayReductionSuppressed, 1);
        _service.GetTargetAttackDelayReduction(Attacker, Target, SkillType.Throwing).Should().Be(0, "suppressed Haste");
        Stat(StatType.ThrowingAttackDelayReductionToBleedingTargetPercent, -10);
        _service.GetTargetAttackDelayReduction(Attacker, Target, SkillType.Throwing).Should().Be(-10, "a penalty still applies");
    }

    [Test]
    public void AnAbilityWhoseSkillCannotBeResolvedSpendsNoArm()
    {
        _service.GrantNextAbilityNoDelay(Attacker, SkillType.Rifle, 10);
        _abilitySkill = SkillType.Invalid;

        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(0);
        _limited = (true, 20);
        _service.ConsumeNextAbilityDelayReductionPercent(Attacker, Hostile()).Should().Be(20, "limited Haste still applies");
        _temporary.Should().ContainKey((Attacker, StatType.NextAttackNoDelay, StatType.NextAttackNoDelay.ToString()));
    }

    [Test]
    public void TheSwingDelayComesFromTheHandsOrElseTheFastestCreatureWeapon()
    {
        const uint rifle = 201;
        const uint companion = 202;
        const uint claw = 203;
        const uint bite = 204;
        const uint slam = 205;
        var items = new ItemWorld(_engine);
        items.Add(Attacker);
        items.Add(rifle, baseItem: BaseItem.Rifle);
        items.AddProperty(rifle, ItemPropertyType.Delay, costTableValue: 3);
        items.Equip(Attacker, InventorySlot.RightHand, rifle);
        Stat(StatType.AttackDelayReductionPercent, 10);
        Stat(StatType.OffhandAttackDelayReductionPercent, 20);

        _service.CalculateAttackDelay(Attacker).Should().Be(CombatRules.CalculateAttackDelayMilliseconds(30, 0, 10, 20));
        _service.CalculateAttackDelay(Attacker, 45).Should().Be(
            CombatRules.CalculateAttackDelayMilliseconds(30, 0, CombatRules.MaximumAttackDelayAdjustmentPercent, 20),
            "the adjustment joins the reduction before the cap");

        items.Add(companion, baseItem: BaseItem.Pistol);
        items.AddProperty(companion, ItemPropertyType.Delay, costTableValue: 1);
        _twinCompanion = companion;
        _service.CalculateAttackDelay(Attacker, -5).Should().Be(CombatRules.CalculateAttackDelayMilliseconds(30, 10, 5, 20),
            "an active twin pistol pair times its companion as the off hand");

        // Without hand weapons the fastest natural weapon sets the pace.
        _twinCompanion = 0;
        items.Invalidate(rifle);
        items.Add(claw);
        items.AddProperty(claw, ItemPropertyType.Delay, costTableValue: 4);
        items.Add(bite);
        items.AddProperty(bite, ItemPropertyType.Delay, costTableValue: 2);
        items.Add(slam);
        items.Equip(Attacker, InventorySlot.CreatureRight, claw);
        items.Equip(Attacker, InventorySlot.CreatureLeft, slam);
        items.Equip(Attacker, InventorySlot.CreatureBite, bite);
        _service.CalculateAttackDelay(Attacker).Should().Be(CombatRules.CalculateAttackDelayMilliseconds(20, 0, 10, 20));

        items.Invalidate(claw);
        items.Invalidate(bite);
        _service.CalculateAttackDelay(Attacker).Should().Be(CombatRules.CalculateAttackDelayMilliseconds(0, 0, 10, 20),
            "no weapon delay at all");
    }

    [Test]
    public void AWeaponsDelayIsTheSumOfItsDelayProperties()
    {
        var items = new ItemWorld(_engine);
        items.Add(101, baseItem: BaseItem.Rifle);
        items.AddProperty(101, ItemPropertyType.Delay, costTableValue: 3);
        items.AddProperty(101, ItemPropertyType.EnhancementBonus, costTableValue: 9);
        items.AddProperty(101, ItemPropertyType.Delay, costTableValue: 2);

        _service.GetWeaponDelay(101).Should().Be(50);
        _service.GetWeaponDelay(102).Should().Be(0, "no item");
        items.Add(103);
        _service.GetWeaponDelay(103).Should().Be(0, "an item without delay");
    }

    [Test]
    public void AParalyzedAttackerWarnsTheCreaturesAroundIt()
    {
        var effects = new Dictionary<nint, EffectTypeScript> { [1] = EffectTypeScript.Haste, [2] = EffectTypeScript.Paralyze };
        var cursor = 0;
        _engine.SetReturnHandler("GetFirstEffect", _ => EngineStructures.Create<Effect>((nint)(cursor = 1)));
        _engine.SetReturnHandler("GetNextEffect", _ => EngineStructures.Create<Effect>(++cursor <= effects.Count ? cursor : 0));
        _engine.SetReturnHandler("GetIsEffectValid", args => ((Effect)args[0]!).Handle != 0);
        _engine.SetReturnHandler("GetEffectType", args => effects[((Effect)args[0]!).Handle]);

        _service.HandleParalyze(Attacker).Should().BeTrue();

        var warning = _feedbackCalls.Calls.Single();
        warning.Arguments[0].Should().Be(Attacker);
        warning.Arguments[1].Should().Be("PARALYZED");
        ((BuildMessageDelegate)warning.Arguments[2]!)(Target).Should().Be($"name of {Attacker} for {Target} is paralyzed and cannot act!");
        warning.Arguments[3].Should().Be(5);

        effects.Remove(2);
        _service.HandleParalyze(Attacker).Should().BeFalse();
        _invalid.Add(Gone);
        _service.HandleParalyze(Gone).Should().BeFalse();
        _feedbackCalls.Calls.Should().HaveCount(1);
    }

    [Test]
    public void ForgettingACreatureDropsBothOfItsSwingDebts()
    {
        _service.ConsumeAttacksPerSwing(Attacker, 900, 1500, false, 1500, 5, 0, 1);
        _service.ConsumeAttacksPerSwing(Target, 1500);
        var debts = Debts("_attackSwingDebts");
        var baseline = Debts("_attackSwingDebtsWithoutLimitedReduction");
        debts.Should().ContainKeys(Attacker, Target);
        baseline.Should().ContainKey(Attacker);

        _service.ForgetAttackTimingState(Attacker);

        debts.Keys.Should().Equal(Target);
        baseline.Should().BeEmpty();
    }

    private Dictionary<uint, float> Debts(string name) =>
        (Dictionary<uint, float>)typeof(CombatAttackTimingService).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_service)!;
}

using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The Suppression stacks of <c>Combat</c>: the stacks hits and auto-attacks apply, the Containment Net statuses they keep in
/// step, and the Overwatch window a suppressed creature's ability use opens for its suppressor. The status effects live in
/// a scripted status service over real effect objects, the stats in a table, and the clock is held still.
/// </summary>
[TestFixture]
public class CombatSuppressionServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Other = 33;
    private const uint Gone = 44;

    private readonly List<IDisposable> _scopes = new();
    private FacadeContainer _facades = null!;
    private FakeNWScript _engine = null!;
    private CombatSuppressionService _service = null!;
    private DateTime _now;
    private Dictionary<(uint, StatType), int> _stats = null!;
    private Dictionary<uint, CreatureStatusEffect> _effects = null!;
    private HashSet<uint> _invalid = null!;
    private HashSet<uint> _players = null!;
    private List<string> _statusCalls = null!;
    private bool _longRange;
    private bool _applySucceeds;
    private Action? _onApply;
    private ScriptedProxy _feedbackCalls = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        // CreatureStatusEffect asks the Stat facade how a stat aggregates.
        _scopes.Add(_facades.Install());
        _now = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
        _scopes.Add(Clock.OverrideForTests(() => _now));
        _stats = new();
        _effects = new();
        _invalid = new();
        _players = new();
        _statusCalls = new();
        _longRange = false;
        _applySucceeds = true;
        _onApply = null;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not (0 or NWScript.OBJECT_INVALID) && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _scopes.Add(NWScriptRuntime.Override(_engine));

        var queries = ScriptedProxy.Create<ICombatQueriesService>(out _);
        ((ScriptedProxy)(object)queries).On(nameof(ICombatQueriesService.IsRangedAbilityLongRangeTarget), _ => _longRange);
        var stat = ScriptedProxy.Create<IStatService>(out var statCalls);
        statCalls.On(nameof(IStatService.GetStatAdjustment), args => _stats.GetValueOrDefault(((uint)args[0]!, (StatType)args[1]!)));
        var statusEffect = ScriptedProxy.Create<IStatusEffectService>(out var statusCalls);
        statusCalls
            .On(nameof(IStatusEffectService.GetCreatureStatusEffects), args => Effects((uint)args[0]!))
            .On(nameof(IStatusEffectService.ApplyStatusEffect), args =>
            {
                var effect = (StatusEffectBase)args[2]!;
                _statusCalls.Add($"Apply {args[0]} {args[1]} {effect.GetType().Name} {args[3]}" +
                                 (args.Length > 4 && args[4] is CombatDamageType damageType ? $" {damageType}" : string.Empty));
                if (!_applySucceeds)
                    return false;
                effect.ReassignSource((uint)args[0]!);
                effect.RestartDuration((int)(float)args[3]!);
                Effects((uint)args[1]!).Add(effect);
                _onApply?.Invoke();
                return true;
            })
            .On(nameof(IStatusEffectService.RemoveStatusEffect), args =>
            {
                var creature = (uint)args[0]!;
                var type = (Type)args[1]!;
                var bySource = args[2] is uint;
                _statusCalls.Add(bySource ? $"Remove {creature} {type.Name} from {args[2]}" : $"Remove {creature} {type.Name}");
                foreach (var effect in Effects(creature).GetAllEffects()
                             .Where(effect => effect.GetType() == type && (!bySource || effect.Source == (uint)args[2]!)).ToList())
                    Effects(creature).Remove(effect);
                return null;
            });
        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out _feedbackCalls);
        _feedbackCalls
            .On(nameof(IPlayerFeedbackService.ShowDiagnosticFloatingText), _ => null)
            .On(nameof(IPlayerFeedbackService.SendDiagnosticToPlayer), _ => null);

        _service = new CombatSuppressionService(queries, stat, statusEffect, feedback);
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
        _scopes.Clear();
    }

    private CreatureStatusEffect Effects(uint creature)
    {
        if (!_effects.TryGetValue(creature, out var effects))
            _effects[creature] = effects = new CreatureStatusEffect { StatGroup = new StatGroup() };
        return effects;
    }

    private void Stat(uint creature, StatType stat, int value) => _stats[(creature, stat)] = value;

    private T Give<T>(uint creature, uint source, T effect, int durationTicks = 30)
        where T : StatusEffectBase
    {
        effect.ReassignSource(source);
        effect.RestartDuration(durationTicks);
        Effects(creature).Add(effect);
        return effect;
    }

    private IEnumerable<T> Active<T>(uint creature) => Effects(creature).GetAllEffects().OfType<T>();

    [Test]
    public void ASuppressionStackCarriesTheAdjustedPenaltyAndOnlyLandsWhenThereIsOne()
    {
        _service.ApplySuppressionStack(Attacker, Defender, 5, 0, CombatDamageType.Physical).Should().BeFalse("no duration");
        _invalid.Add(Gone);
        _service.ApplySuppressionStack(Gone, Defender, 5, 30, CombatDamageType.Physical).Should().BeFalse();
        _service.ApplySuppressionStack(Attacker, Gone, 5, 30, CombatDamageType.Physical).Should().BeFalse();
        Stat(Attacker, StatType.SuppressionStackEvasionPenaltyPercentAdjustment, -3);
        _service.ApplySuppressionStack(Attacker, Defender, 2, 30, CombatDamageType.Physical, 1).Should().BeFalse("2 - 3 + 1 is no penalty");
        _statusCalls.Should().BeEmpty();

        _service.ApplySuppressionStack(Attacker, Defender, 5, 30, CombatDamageType.Fire, 1).Should().BeTrue();

        _statusCalls.Should().Equal($"Apply {Attacker} {Defender} SuppressionStatusEffect 30 Fire");
        Active<SuppressionStatusEffect>(Defender).Single().EvasionPenaltyPercent.Should().Be(3);
        _service.GetSuppressionStackCount(Defender, Attacker).Should().Be(1);

        _applySucceeds = false;
        _service.ApplySuppressionStack(Attacker, Defender, 5, 30, CombatDamageType.Fire).Should().BeFalse();
    }

    [Test]
    public void TheStackCountCountsEverySourceOrOne()
    {
        Give(Defender, Attacker, new SuppressionStatusEffect(2));
        Give(Defender, Attacker, new SuppressionStatusEffect(2));
        Give(Defender, Other, new SuppressionStatusEffect(2));
        Give(Defender, Other, new OverwatchStatusEffect());

        _service.GetSuppressionStackCount(Defender).Should().Be(3);
        _service.GetSuppressionStackCount(Defender, Attacker).Should().Be(2);
        _service.GetSuppressionStackCount(Defender, Other).Should().Be(1);
        _invalid.Add(Gone);
        _service.GetSuppressionStackCount(Gone).Should().Be(0);
    }

    [Test]
    public void AutoAttacksRollTheirChanceBeforeAddingAStack()
    {
        _players.Add(Attacker);
        _service.ApplyAutoAttackSuppressionStack(Attacker, Defender, SkillType.Force, CombatDamageType.Physical);
        Stat(Attacker, StatType.AutoAttackSuppressionStackChance, 100);
        _service.ApplyAutoAttackSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical);
        _statusCalls.Should().BeEmpty("a non-weapon skill or no duration adds nothing");

        Stat(Attacker, StatType.AutoAttackSuppressionStackDurationSeconds, 30);
        Stat(Attacker, StatType.AutoAttackSuppressionStackEvasionPenaltyPercent, 4);
        _service.ApplyAutoAttackSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical);

        _statusCalls.Should().Equal($"Apply {Attacker} {Defender} SuppressionStatusEffect 30 Physical");
        _engine.CallsTo("SendMessageToPC").Select(call => call.Arguments[1]!.ToString()).Should().ContainSingle()
            .Which.Should().Contain("Pinning Fire: Suppression applied.");
        _feedbackCalls.Calls.Select(call => call.Member).Should().Equal(nameof(IPlayerFeedbackService.ShowDiagnosticFloatingText));

        // The roll uses the shared generator: one D100 that must not exceed the chance.
        const int seed = 7;
        new System.Random(seed).Next(1, 101).Should().BeGreaterThan(1, "the seeded roll misses a 1% chance");
        Random.SetSeed(seed);
        Stat(Attacker, StatType.AutoAttackSuppressionStackChance, 1);
        _service.ApplyAutoAttackSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical);
        _statusCalls.Should().HaveCount(1);

        _players.Clear();
        Stat(Attacker, StatType.AutoAttackSuppressionStackChance, 100);
        _service.ApplyAutoAttackSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical);
        _statusCalls.Should().HaveCount(2);
        _engine.CallsTo("SendMessageToPC").Should().HaveCount(1, "only a player hears about the stack");
    }

    [Test]
    public void RangedHitsAddTheirOwnStackAndOneForEveryKillBoxCaster()
    {
        _service.ApplyRangedHitSuppressionStack(Attacker, Defender, SkillType.Force, CombatDamageType.Physical);
        _statusCalls.Should().BeEmpty();

        _invalid.Add(Gone);
        Give(Defender, Other, new KillBoxStatusEffect(0, 2));
        Give(Defender, Gone, new KillBoxStatusEffect(9));
        Stat(Other, StatType.RangedHitSuppressionStackEvasionPenaltyPercent, 3);
        _service.ApplyRangedHitSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Physical);
        _statusCalls.Should().Equal(new[] { $"Apply {Other} {Defender} SuppressionStatusEffect 30 Physical" },
            "the attacker has no stack of its own, and a Kill Box whose caster is gone adds nothing");
        Active<SuppressionStatusEffect>(Defender).Single().EvasionPenaltyPercent.Should().Be(5, "the caster's 3% plus the box's 2");

        _statusCalls.Clear();
        Effects(Defender).Remove(Active<SuppressionStatusEffect>(Defender).Single());
        Effects(Defender).Remove(Active<KillBoxStatusEffect>(Defender).First(effect => effect.Source == Other));
        Give(Defender, Other, new KillBoxStatusEffect(6));
        Stat(Attacker, StatType.RangedHitSuppressionStackDurationSeconds, 12);
        Stat(Attacker, StatType.RangedHitSuppressionStackEvasionPenaltyPercent, 2);
        _service.ApplyRangedHitSuppressionStack(Attacker, Defender, SkillType.Rifle, CombatDamageType.Electrical);
        _statusCalls.Should().Equal(
            $"Apply {Attacker} {Defender} SuppressionStatusEffect 12 Electrical",
            $"Apply {Other} {Defender} SuppressionStatusEffect 30 Electrical");
        Active<SuppressionStatusEffect>(Defender).Select(effect => effect.EvasionPenaltyPercent).Should().BeEquivalentTo(new[] { 2, 6 });
    }

    [Test]
    public void ContainmentNetFollowsEachSourcesStacksWithinTheSharedCap()
    {
        Stat(Attacker, StatType.SuppressionStackDamageDealtRequiredStacks, 2);
        Stat(Attacker, StatType.SuppressionStackDamageDealtPercentAdjustment, -6);
        Stat(Other, StatType.SuppressionStackDamageDealtRequiredStacks, 1);
        Stat(Other, StatType.SuppressionStackDamageDealtPercentAdjustment, -6);

        _service.ApplySuppressionStack(Attacker, Defender, 2, 30, CombatDamageType.Physical);
        Active<ContainmentNetStatusEffect>(Defender).Should().BeEmpty("one stack is below the attacker's requirement");

        _service.ApplySuppressionStack(Attacker, Defender, 2, 30, CombatDamageType.Physical);
        Active<ContainmentNetStatusEffect>(Defender).Single().DamageAdjustmentPercent.Should().Be(-6);

        _service.ApplySuppressionStack(Other, Defender, 2, 30, CombatDamageType.Physical);
        Active<ContainmentNetStatusEffect>(Defender).ToDictionary(effect => effect.Source, effect => effect.DamageAdjustmentPercent)
            .Should().BeEquivalentTo(new Dictionary<uint, int> { [Attacker] = -6, [Other] = -4 },
                "the two nets share the 10% cap in source order");

        _statusCalls.Clear();
        _service.ReconcileContainmentNetStatus(Other, Defender);
        _statusCalls.Should().BeEmpty("nets already at their desired values stay");

        // A source losing its stacks loses its net; the other net takes up the freed share.
        foreach (var stack in Active<SuppressionStatusEffect>(Defender).Where(effect => effect.Source == Attacker).ToList())
            Effects(Defender).Remove(stack);
        _service.ReconcileContainmentNetStatuses(Defender);
        _statusCalls.Should().Equal(
            $"Remove {Defender} ContainmentNetStatusEffect from {Attacker}",
            $"Remove {Defender} ContainmentNetStatusEffect from {Other}",
            $"Apply {Other} {Defender} ContainmentNetStatusEffect -1");
        Active<ContainmentNetStatusEffect>(Defender).Single().DamageAdjustmentPercent.Should().Be(-6);
    }

    [Test]
    public void ANetWhoseSourceIsGoneClearsEveryNetBeforeTheyAreRebuilt()
    {
        Stat(Attacker, StatType.SuppressionStackDamageDealtRequiredStacks, 1);
        Stat(Attacker, StatType.SuppressionStackDamageDealtPercentAdjustment, 4);
        Give(Defender, Attacker, new SuppressionStatusEffect(2));
        Give(Defender, Attacker, new ContainmentNetStatusEffect(4), -1);
        Give(Defender, Gone, new ContainmentNetStatusEffect(-5), -1);
        _invalid.Add(Gone);

        _service.ReconcileContainmentNetStatuses(Defender);

        _statusCalls.Should().Equal(
            $"Remove {Defender} ContainmentNetStatusEffect",
            $"Apply {Attacker} {Defender} ContainmentNetStatusEffect -1");
        Active<ContainmentNetStatusEffect>(Defender).Single().DamageAdjustmentPercent.Should().Be(4, "a bonus is not capped");

        _statusCalls.Clear();
        _service.ReconcileContainmentNetStatus(Attacker, Gone);
        _service.ReconcileContainmentNetStatuses(Gone);
        _statusCalls.Should().BeEmpty("an invalid target is left alone");
    }

    [Test]
    public void DuplicateNetsFromOneSourceAreReplacedByOne()
    {
        Stat(Attacker, StatType.SuppressionStackDamageDealtRequiredStacks, 1);
        Stat(Attacker, StatType.SuppressionStackDamageDealtPercentAdjustment, -3);
        Give(Defender, Attacker, new SuppressionStatusEffect(2));
        Give(Defender, Attacker, new ContainmentNetStatusEffect(-3), -1);
        Give(Defender, Attacker, new ContainmentNetStatusEffect(-3), -1);

        _service.ReconcileContainmentNetStatuses(Defender);

        _statusCalls.Should().Equal(
            $"Remove {Defender} ContainmentNetStatusEffect from {Attacker}",
            $"Apply {Attacker} {Defender} ContainmentNetStatusEffect -1");
        Active<ContainmentNetStatusEffect>(Defender).Should().ContainSingle();
    }

    [Test]
    public void NormalisingATargetDoesNotReenterItself()
    {
        Stat(Attacker, StatType.SuppressionStackDamageDealtRequiredStacks, 1);
        Stat(Attacker, StatType.SuppressionStackDamageDealtPercentAdjustment, -3);
        Give(Defender, Attacker, new SuppressionStatusEffect(2));
        var reentered = 0;
        _onApply = () =>
        {
            reentered++;
            _service.ReconcileContainmentNetStatuses(Defender);
        };

        _service.ReconcileContainmentNetStatuses(Defender);

        reentered.Should().Be(1);
        _statusCalls.Should().Equal($"Apply {Attacker} {Defender} ContainmentNetStatusEffect -1");

        // The guard is released afterwards, also when the normalisation throws.
        _onApply = () => throw new InvalidOperationException("apply failed");
        Effects(Defender).Remove(Active<ContainmentNetStatusEffect>(Defender).Single());
        var failing = () => _service.ReconcileContainmentNetStatuses(Defender);
        failing.Should().Throw<InvalidOperationException>();
        _onApply = null;
        _statusCalls.Clear();
        Effects(Defender).Remove(Active<ContainmentNetStatusEffect>(Defender).Single());
        _service.ReconcileContainmentNetStatuses(Defender);
        _statusCalls.Should().Equal($"Apply {Attacker} {Defender} ContainmentNetStatusEffect -1");
    }

    [Test]
    public void LongRangeAdjustmentsApplyOnlyToLongRangeTargets()
    {
        Stat(Attacker, StatType.RangedAbilityLongRangeHitChancePercentAdjustment, 7);
        Stat(Attacker, StatType.RangedAbilityLongRangeCriticalRatePercentAdjustment, 9);

        _service.GetRangedAbilityLongRangeHitChanceAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        _service.GetRangedAbilityLongRangeCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        _longRange = true;
        _service.GetRangedAbilityLongRangeHitChanceAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(7);
        _service.GetRangedAbilityLongRangeCriticalRateAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(9);
    }

    // Defender holds suppression stacks from Attacker, then uses an ability: Attacker's next ranged attack against it gains
    // Overwatch accuracy once, while the stacks it was armed by last.
    private void ArmOverwatch(int durationTicks = 20)
    {
        Stat(Attacker, StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment, 15);
        Give(Defender, Attacker, new SuppressionStatusEffect(2), durationTicks);
        _service.TrackSuppressionAbilityUse(Defender, _now);
    }

    [Test]
    public void AnAbilityUsedUnderSuppressionArmsOverwatchUntilTheStacksRunOut()
    {
        _players.Add(Attacker);
        ArmOverwatch();

        _statusCalls.Should().Equal($"Remove {Attacker} OverwatchStatusEffect", $"Apply {Attacker} {Attacker} OverwatchStatusEffect 20");
        _statusCalls.Clear();

        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Force).Should().Be(0, "not a weapon attack");
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(15);
        _feedbackCalls.Calls.Select(call => call.Member).Should().Equal(nameof(IPlayerFeedbackService.SendDiagnosticToPlayer));
        _engine.CallsTo("FloatingTextStringOnCreature").Should().ContainSingle();
        _statusCalls.Should().Equal(new[] { $"Remove {Attacker} OverwatchStatusEffect" }, "the window is spent, so the marker goes");

        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0, "spent");
    }

    [Test]
    public void OverwatchIsLostWhenItsWindowOrStacksAreGone()
    {
        ArmOverwatch(durationTicks: 5);
        _now = _now.AddSeconds(6);
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0, "expired");

        ArmOverwatch();
        foreach (var stack in Active<SuppressionStatusEffect>(Defender).ToList())
            Effects(Defender).Remove(stack);
        Give(Defender, Attacker, new SuppressionStatusEffect(2));
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0,
            "a fresh stack is not one the ability was used under");

        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Other, SkillType.Rifle).Should().Be(0, "no stacks on that target");
        Give(Other, Attacker, new SuppressionStatusEffect(2));
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Other, SkillType.Rifle).Should().Be(0, "never armed against it");

        Stat(Attacker, StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment, 0);
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Other, SkillType.Rifle).Should().Be(0, "no accuracy rider");
        _feedbackCalls.Calls.Should().BeEmpty();
    }

    [Test]
    public void TheOverwatchMarkerLastsAsLongAsTheLatestArmedWindow()
    {
        Stat(Attacker, StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment, 15);
        Give(Defender, Attacker, new SuppressionStatusEffect(2), 10);
        Give(Other, Attacker, new SuppressionStatusEffect(2), 25);
        _service.TrackSuppressionAbilityUse(Defender, _now);
        _service.TrackSuppressionAbilityUse(Other, _now);
        _statusCalls.Clear();

        _now = _now.AddSeconds(4.5);
        _service.ReconcileOverwatchStatus(Attacker);
        _statusCalls.Should().Equal($"Remove {Attacker} OverwatchStatusEffect", $"Apply {Attacker} {Attacker} OverwatchStatusEffect 21");

        // The shorter window runs out and is dropped; the marker follows the one left.
        _statusCalls.Clear();
        _now = _now.AddSeconds(6);
        _service.ReconcileOverwatchStatus(Attacker);
        _statusCalls.Should().Equal($"Remove {Attacker} OverwatchStatusEffect", $"Apply {Attacker} {Attacker} OverwatchStatusEffect 15");
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);

        // A permanent stack arms a window with no end.
        _statusCalls.Clear();
        Give(Other, Attacker, new SuppressionStatusEffect(2), -1);
        _service.TrackSuppressionAbilityUse(Other, _now);
        _statusCalls.Should().Equal($"Remove {Attacker} OverwatchStatusEffect", $"Apply {Attacker} {Attacker} OverwatchStatusEffect -1");

        // Losing the rider drops every armed window and the marker.
        _statusCalls.Clear();
        Stat(Attacker, StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment, 0);
        _service.ReconcileOverwatchStatus(Attacker);
        _statusCalls.Should().Equal($"Remove {Attacker} OverwatchStatusEffect");
        Stat(Attacker, StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment, 15);
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Other, SkillType.Rifle).Should().Be(0);

        _invalid.Add(Gone);
        _statusCalls.Clear();
        _service.ReconcileOverwatchStatus(Gone);
        _statusCalls.Should().BeEmpty("an invalid suppressor has no marker to remove");
    }

    [Test]
    public void ALaterTriggerKeepsTheLongerArmedWindowAndItsStacks()
    {
        ArmOverwatch(durationTicks: 20);
        var first = Active<SuppressionStatusEffect>(Defender).Single();
        Effects(Defender).Remove(first);
        Give(Defender, Attacker, new SuppressionStatusEffect(2), 5);
        _service.TrackSuppressionAbilityUse(Defender, _now);
        _statusCalls.Clear();

        _service.ReconcileOverwatchStatus(Attacker);
        _statusCalls.Should().Equal(new[] { $"Remove {Attacker} OverwatchStatusEffect", $"Apply {Attacker} {Attacker} OverwatchStatusEffect 20" },
            "the shorter trigger does not shorten the armed window");

        // The new stack counts for the merged window, so Overwatch still fires.
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(15);
    }

    [Test]
    public void AbilityUseWithoutAnyLiveSuppressionArmsNothing()
    {
        _invalid.Add(Gone);
        Stat(Attacker, StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment, 15);
        Give(Defender, Gone, new SuppressionStatusEffect(2));
        Give(Defender, Attacker, new SuppressionStatusEffect(2), 0);
        _service.TrackSuppressionAbilityUse(Defender, _now);
        _statusCalls.Should().BeEmpty("a gone source and a stack with no time left arm nothing");

        // Without the rider the window is still recorded, but no marker is shown until the rider exists.
        Stat(Other, StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment, 0);
        Give(Defender, Other, new SuppressionStatusEffect(2));
        _service.TrackSuppressionAbilityUse(Defender, _now);
        _statusCalls.Should().BeEmpty();
    }

    [Test]
    public void ForgettingACreatureDropsTheWindowsItArmedOrWasArmedAgainst()
    {
        ArmOverwatch();
        Give(Other, Attacker, new SuppressionStatusEffect(2));
        _service.TrackSuppressionAbilityUse(Other, _now);
        _statusCalls.Clear();

        _service.ForgetSuppressionState(Defender);

        _statusCalls.Should().Equal(new[] { $"Remove {Attacker} OverwatchStatusEffect", $"Apply {Attacker} {Attacker} OverwatchStatusEffect 30" },
            "the suppressor's marker is refreshed for the window left");
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Defender, SkillType.Rifle).Should().Be(0);
        _service.ConsumeSuppressionRangedAttackAccuracyAdjustment(Attacker, Other, SkillType.Rifle).Should().Be(15);

        _statusCalls.Clear();
        _service.ForgetSuppressionState(Gone);
        _statusCalls.Should().BeEmpty("a creature with no windows refreshes nothing");
    }
}

using System.Reflection;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Scenario;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.CombatSim.Engine;

/// <summary>Stationary weapon experiment using production sequence/resource/reaction state and source stat groups.</summary>
public sealed class WeaponCombatState
{
    public TechniqueSequence Sequence { get; } = new();
    public TechniqueResourceState Resources { get; } = new();
    public TechniqueReactiveState Reactions { get; } = new();
    public Dictionary<string, double> Diagnostics { get; } = new();
    public Dictionary<string, int> Casts { get; } = new();
    public List<Dictionary<StatType, int>> Sources { get; }
    public Dictionary<uint, (double Until, int Penalty)> Rattled { get; } = new();
    public Dictionary<uint, (double Until, int Penalty)> LowKick { get; } = new();
    public MeleeTechniqueLine Line { get; private set; }
    public MeleeTechniqueLine WindowLine { get; private set; }
    public Combatant SetupEnemy { get; private set; }
    public int OrdinaryAccuracy { get; private set; }
    public double BraceUntil { get; private set; }
    public double HighGuardUntil { get; private set; }
    public double ControlDefenseUntil { get; private set; }
    public double ReversalUntil { get; private set; }
    private float _attackDebt;
    private ActiveStatus _stance;
    private Combatant _windowEnemy;
    private Combatant _retaliationEnemy;
    public WeaponControlState Control { get; } = new();
    private readonly Combatant _owner;
    public static DateTime At(double seconds) => DateTime.UnixEpoch.AddSeconds(seconds);
    public WeaponCombatState(Combatant owner, List<Dictionary<StatType,int>> sources)
    {
        _owner = owner;
        owner.Spec.WeaponCombat.Validate();
        Sources = sources;
        StatusEffectBase stance = owner.Spec.WeaponCombat.Stance switch
        {
            "Flow" => new FlowStanceStatusEffect(), "Bruiser" => new BruiserStanceStatusEffect(),
            "Berserker" => new BerserkerStanceStatusEffect(), "Scrapper" => new ScrapperStanceStatusEffect(), _ => null
        };
        if (stance is BerserkerStanceStatusEffect)
            typeof(BerserkerStanceStatusEffect).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(stance, new object[] { 0u, 0 });
        if (stance != null)
        {
            _stance = new ActiveStatus
            {
                Stats = stance.StatGroup.Stats.Where(x => x.Value != 0).ToDictionary(x => x.Key, x => x.Value),
                IsStance = true,
                ExpiresAt = double.PositiveInfinity
            };
            owner.Statuses[stance.GetType().Name] = _stance;
        }
        foreach (var stat in AllSources.SelectMany(s => s.Where(x => x.Value != 0).Select(x => x.Key)).Distinct())
            if (!SupportedStats.Contains(stat.ToString()) && !Simulation.SupportsPerkStat(stat))
                throw new InvalidDataException($"WeaponCombat does not implement perk stat {stat}.");
    }
    // Movement and enmity have no effect in stationary fights; resistance and control durations still apply.
    // SingleWeaponDamagePercentAdjustment and ForceAffinity are already included in calibrated scenario inputs.
    private static readonly HashSet<string> SupportedStats = new(StringComparer.Ordinal)
    {
        "Invalid", "ForceAffinity", "MindResistance", "DazeImmunity", "TechniqueAbilityPower", "TechniqueRetaliationPower",
        "TechniqueRetaliationDurationSeconds", "TechniqueRetaliationCooldownSeconds", "TechniqueLine", "TechniqueWeavePower", "TechniqueWeaveEnabled", "TechniqueWindowExtensionSeconds",
        "TechniqueConditionalPower", "TechniqueConditionalAccuracy", "TechniqueSetupAbilityAccuracy",
        "TechniqueRefundStamina", "TechniqueRefundCooldownSeconds", "TechniqueDebuffTargetOrdinaryAccuracy",
        "TechniquePresentationSkillType", "TechniqueOpeningFinisherEvasionPercent", "TechniqueOpeningFinisherMovementSpeedPercent",
        "TechniqueFinishFinisherHastePercent", "TechniqueFinisherWindowDurationSeconds", "TechniqueFinisherWindowCooldownSeconds",
        "TechniqueSetupBraceWindowSeconds", "TechniqueSetupRetaliationPower", "TechniqueSetupRetaliationCooldownSeconds",
        "DirectHostileDamageNextAbilityStaminaDiscount", "DirectHostileDamageNextAbilityDiscountDurationSeconds",
        "DirectHostileDamageNextAbilityDiscountCooldownSeconds", "CostlyAbilityDamageMinimumStaminaCost", "CostlyAbilityDamageBonus",
        "CostlyAbilityUsedEvasionMinimumStaminaCost", "CostlyAbilityUsedEvasionPercentAdjustment", "CostlyAbilityUsedEvasionDurationSeconds",
        "AbilityStatusCategoryBonusSkillType", "AbilityStatusCategoryBonusRequiredCategory", "AbilityStatusCategoryDamageBonus",
        "AbilityStatusCategoryHitChancePercentAdjustment", "DamageTakenNextSkillAbilitySkillType", "DamageTakenNextSkillAbilityDamageBonus",
        "DamageTakenNextSkillAbilityWindowSeconds", "StatusAppliedRequiredCategory", "StatusAppliedSelfDefensePercentAdjustment",
        "StatusAppliedSelfDurationSeconds", "StatusAppliedSelfEnmityPercentAdjustment", "OutgoingDebuffDurationPercentAdjustment",
        "OutgoingControlDurationPercentAdjustment", "HostileAbilityRecastDelayPercentAdjustment", "PhysicalDefensePercentAdjustment",
        "ForceDefensePercentAdjustment", "ForceAttackPercentAdjustment", "AttackPercentAdjustment", "EvasionPercentAdjustment", "AttackDelayReductionPercent",
        "MobilityResistance", "TraumaResistance", "SingleWeaponDamagePercentAdjustment"
    };
    private IEnumerable<IReadOnlyDictionary<StatType, int>> AllSources =>
        Sources.Cast<IReadOnlyDictionary<StatType, int>>().Concat(_owner.Statuses.Values.Select(s => s.Stats));
    public int Stat(StatType stat) => AllSources.Aggregate(0, (sum, s) => SWLOR.Game.Server.Service.Stat.AggregateStatAdjustment(stat, sum, s.GetValueOrDefault(stat)));
    public int LineStat(MeleeTechniqueLine line, StatType stat) => AllSources.Where(s => s.GetValueOrDefault(StatType.TechniqueLine) == (int)line).Sum(s => s.GetValueOrDefault(stat));
    public void Count(string key, double amount = 1) => Diagnostics[key] = Diagnostics.GetValueOrDefault(key) + amount;
    public void ClearSetup(bool spent = false)
    {
        if (!spent && Sequence.Stage != MeleeTechniqueStage.None) Count("SetupWasted");
        SetupEnemy?.Weapon?.Rattled.Remove(_owner.SimId);
        SetupEnemy = null;
        BraceUntil = 0;
        Sequence.Clear();
        Reactions.ClearSetupBenefits();
    }
    public void Refresh(double now)
    {
        var stance = _owner.Statuses.Values.SingleOrDefault(s => s.IsStance);
        if (!ReferenceEquals(_stance, stance))
        {
            _stance = stance;
            ClearSetup();
            Reactions.ClearBenefits();
            _windowEnemy = null;
            BraceUntil = 0;
        }
        if (_retaliationEnemy?.IsDead == true) { Reactions.ClearRetaliation(); _retaliationEnemy = null; }
        if (_windowEnemy?.IsDead == true) { Reactions.ClearWindow(); _windowEnemy = null; }
        if (Sequence.Stage != MeleeTechniqueStage.None && (SetupEnemy == null || SetupEnemy.IsDead || !Sequence.Matches(Sequence.Target, At(now)) ||
            Sequence.Stage == MeleeTechniqueStage.Distraction && (!SetupEnemy.Weapon.Rattled.TryGetValue(_owner.SimId, out var rattled) || rattled.Until <= now)))
        {
            if (SetupEnemy?.IsDead != true) Count("SetupExpired");
            ClearSetup();
        }
        foreach (var key in Rattled.Where(x => x.Value.Until <= now).Select(x => x.Key).ToArray()) Rattled.Remove(key);
        foreach (var key in LowKick.Where(x => x.Value.Until <= now).Select(x => x.Key).ToArray()) LowKick.Remove(key);
        if (Reactions.GetWindowStage(At(now)) == MeleeTechniqueStage.None) Reactions.ClearWindow();
    }
    public void Target(Combatant target, double now)
    {
        Refresh(now);
        if (Sequence.Stage != MeleeTechniqueStage.None && Sequence.Target != target.SimId)
        {
            Count("SetupLostToRetarget");
            ClearSetup();
        }
        if (Reactions.WindowTarget != 0 && Reactions.WindowTarget != target.SimId) Reactions.ClearWindow();
        if (Reactions.RetaliationTarget != 0 && Reactions.RetaliationTarget != target.SimId) Reactions.ClearRetaliation();
    }
    public MeleeTechniqueStage Stage(Combatant target, double now)
    {
        Refresh(now);
        return Sequence.Matches(target.SimId, At(now)) ? Sequence.Stage : MeleeTechniqueStage.None;
    }
    public int SetupAccuracy(Combatant target, double now) => Stage(target, now) is MeleeTechniqueStage.Opening or MeleeTechniqueStage.Finish ? LineStat(Line, StatType.TechniqueSetupAbilityAccuracy) : 0;
    public bool CanUse(AbilityInfo info, AbilityUseSpec use, Combatant target, double now)
    {
        if (_owner.Stm < use.MinimumStaminaBeforeUse) return false;
        if (info.Technique != null && (!_owner.Spec.WeaponCombat.AllowTechniqueAbilities || !Resources.CanActivate(At(now)))) return false;
        var stage = Stage(target, now);
        if (use.RequiredSetup != "Any" && use.RequiredSetup != stage.ToString()) return false;
        if (use.WaitForWeave && stage == MeleeTechniqueStage.Opening && Sequence.WeavePower > 0) return false;
        return true;
    }
    public TechniqueActivation Begin(AbilityInfo info, AbilityUseSpec use, Combatant target, int paid, double now)
    {
        if (info.IsHostile && info.IsAimed) Target(target, now);
        var accuracy = info.IsHostile ? SetupAccuracy(target, now) : 0;
        var stage = MeleeTechniqueStage.None;
        var conditional = 0;
        if (info.Technique is { } d)
        {
            var matching = Line == d.Line && Sequence.Matches(target.SimId, At(now));
            stage = matching && (d.ConsumedStages & Sequence.Stage) != 0 ? Sequence.Stage : MeleeTechniqueStage.None;
            var retaliation = d.ConsumesRetaliation || stage == MeleeTechniqueStage.Distraction && d.CreatedStage == MeleeTechniqueStage.None ? Reactions.TakeRetaliation(target.SimId, At(now)) : 0;
            if (retaliation > 0) Count("RetaliationsSpent");
            if (!d.PreservesMatchingSetup || !matching) ClearSetup(stage != MeleeTechniqueStage.None);
            if (d.ClearSetup) ClearSetup();
            Resources.LockAction(At(now));
            conditional = retaliation + (d.UsesAbilityPower ? LineStat(d.Line, StatType.TechniqueAbilityPower) : 0);
            if (d.UsesAbilityPower) Count("IndependentHeavyCasts");
            if (stage != MeleeTechniqueStage.None)
            {
                conditional += d.GetStagePower(stage) + LineStat(d.Line, StatType.TechniqueConditionalPower);
                accuracy += LineStat(d.Line, StatType.TechniqueConditionalAccuracy);
                Count("SetupSpent");
            }
        }
        var flat = 0; var hit = 0;
        if (info.IsHostile)
        {
            var minimum = Stat(StatType.CostlyAbilityDamageMinimumStaminaCost);
            if (minimum > 0 && paid >= minimum) { flat += Stat(StatType.CostlyAbilityDamageBonus); Count("HighGuardQualifiedCasts"); }
            foreach (var source in AllSources.Where(s => use.IsControl && s.GetValueOrDefault(StatType.AbilityStatusCategoryBonusSkillType) == (int)info.SkillType))
            {
                flat += source.GetValueOrDefault(StatType.AbilityStatusCategoryDamageBonus);
                hit += source.GetValueOrDefault(StatType.AbilityStatusCategoryHitChancePercentAdjustment);
            }
            if (now < ReversalUntil && Stat(StatType.DamageTakenNextSkillAbilitySkillType) == (int)info.SkillType)
            {
                flat += Stat(StatType.DamageTakenNextSkillAbilityDamageBonus); ReversalUntil = 0; Count("ReversalSpent");
            }
        }
        Casts[use.Feat] = Casts.GetValueOrDefault(use.Feat) + 1;
        return new TechniqueActivation { Stage = stage, ConditionalPower = conditional, Accuracy = accuracy, FlatPower = flat, HitPercent = hit, PaidStamina = paid };
    }
    public void Complete(AbilityInfo info, AbilityUseSpec use, TechniqueActivation activation, Combatant target, bool landed, double now)
    {
        if (info.IsHostile && Stat(StatType.CostlyAbilityUsedEvasionMinimumStaminaCost) > 0 && activation.PaidStamina >= Stat(StatType.CostlyAbilityUsedEvasionMinimumStaminaCost))
            HighGuardUntil = now + Stat(StatType.CostlyAbilityUsedEvasionDurationSeconds);
        if (landed && use.IsControl)
            foreach (var source in AllSources.Where(s => s.GetValueOrDefault(StatType.StatusAppliedSelfDefensePercentAdjustment) > 0))
                ControlDefenseUntil = Math.Max(ControlDefenseUntil, now + source.GetValueOrDefault(StatType.StatusAppliedSelfDurationSeconds));
        if (landed && info.ImpactStatusFactory?.Invoke() is { } impactStatus)
        {
            var penalty = impactStatus.StatGroup.Stats.GetValueOrDefault(StatType.OrdinaryPhysicalDamagePenaltyPercent);
            var duration = StatusDuration(impactStatus, info.ImpactStatusDuration, target);
            if (penalty > 0 && duration > 0)
                target.Weapon.LowKick[_owner.SimId] = (now + duration, penalty);
        }
        if (info.Technique is not { } d) return;
        if (!landed) { if (activation.Stage != MeleeTechniqueStage.None) Count("MissedSpenders"); return; }
        if (d.RefundEligible && (!d.RefundRequiresSetup || activation.Stage != MeleeTechniqueStage.None) &&
            Resources.TryRefund(activation.PaidStamina, LineStat(d.Line, StatType.TechniqueRefundStamina), LineStat(d.Line, StatType.TechniqueRefundCooldownSeconds), At(now), out var refund))
        {
            _owner.Stm = Math.Min(_owner.Spec.MaxStm, _owner.Stm + refund); _owner.Stats.StmRefunded += refund; Count("TechniqueRefundedStamina", refund);
        }
        if (activation.Stage != MeleeTechniqueStage.None)
        {
            if (d.CreatedStage == MeleeTechniqueStage.None && !target.IsDead && Reactions.TryOpenWindow(target.SimId, activation.Stage,
                LineStat(d.Line, StatType.TechniqueFinisherWindowDurationSeconds), LineStat(d.Line, StatType.TechniqueFinisherWindowCooldownSeconds), At(now)))
            { WindowLine = d.Line; _windowEnemy = target; Count("FlowWindows"); }
        }
        if (target.IsDead || d.CreatedStage == MeleeTechniqueStage.None || d.CreatesStageOnlyWhenSequenced && activation.Stage == MeleeTechniqueStage.None) return;
        var durationSeconds = d.SetupDurationSeconds + LineStat(d.Line, StatType.TechniqueWindowExtensionSeconds);
        var status = d.SourceOwnedSetupStatusEffectFactory?.Invoke();
        var statusDuration = status == null ? durationSeconds : StatusDuration(status, durationSeconds, target);
        if (statusDuration <= 0) return;
        durationSeconds = (int)Math.Ceiling(statusDuration);
        Line = d.Line; SetupEnemy = target;
        OrdinaryAccuracy = d.OrdinaryAccuracyBonus + (status != null ? LineStat(d.Line, StatType.TechniqueDebuffTargetOrdinaryAccuracy) : 0);
        Sequence.Begin(target.SimId, d.CreatedStage, At(now + durationSeconds), LineStat(d.Line, StatType.TechniqueWeaveEnabled) > 0 ? LineStat(d.Line, StatType.TechniqueWeavePower) : 0);
        if (status != null)
        {
            target.Weapon.Rattled[_owner.SimId] = (now + statusDuration, status.StatGroup.Stats.GetValueOrDefault(StatType.OrdinaryWeaponAccuracyPenalty));
            var braceSeconds = LineStat(d.Line, StatType.TechniqueSetupBraceWindowSeconds);
            BraceUntil = now + Math.Min(durationSeconds, braceSeconds);
            Reactions.BeginBrace(target.SimId, Sequence.ExpiresAt, braceSeconds, At(now));
        }
        Count("SetupCreated");
    }
    private double StatusDuration(IStatusEffect status, int seconds, Combatant target)
    {
        var resistance = status.ResistanceType switch
        {
            ResistanceType.Trauma => target.Spec.WeaponCombat.TraumaResistance + target.Weapon.Stat(StatType.TraumaResistance),
            ResistanceType.Mind => target.Spec.WeaponCombat.MindResistance + target.Weapon.Stat(StatType.MindResistance),
            ResistanceType.Invalid => 0,
            _ => throw new InvalidDataException($"Unsupported weapon status resistance {status.ResistanceType}.")
        };
        return WeaponControlState.Duration(status, seconds, StatusEffect.GetOutgoingDurationPercentAdjustment(status.Categories, Stat), resistance, false);
    }
    public void DirectHpDamage(Combatant attacker, double now)
    {
        Refresh(now);
        if (Sequence.Stage == MeleeTechniqueStage.Distraction && Sequence.Matches(attacker.SimId, At(now)) && Reactions.TryGrantRetaliation(attacker.SimId,
            LineStat(Line, StatType.TechniqueSetupRetaliationPower), Sequence.ExpiresAt, LineStat(Line, StatType.TechniqueSetupRetaliationCooldownSeconds), At(now))) Count("RetaliationsArmed");
        foreach (var source in AllSources.Where(s => s.GetValueOrDefault(StatType.TechniqueLine) != 0))
            if (Reactions.TryGrantIndependentRetaliation(attacker.SimId, source.GetValueOrDefault(StatType.TechniqueRetaliationPower),
                source.GetValueOrDefault(StatType.TechniqueRetaliationDurationSeconds), source.GetValueOrDefault(StatType.TechniqueRetaliationCooldownSeconds), At(now)))
            {
                _retaliationEnemy = attacker;
                Count("RetaliationsArmed");
                break;
            }
        foreach (var s in AllSources)
            if (Resources.TryGrantDiscount(s.GetValueOrDefault(StatType.DirectHostileDamageNextAbilityStaminaDiscount), s.GetValueOrDefault(StatType.DirectHostileDamageNextAbilityDiscountDurationSeconds), s.GetValueOrDefault(StatType.DirectHostileDamageNextAbilityDiscountCooldownSeconds), At(now))) { Count("PaybackArmed"); break; }
        }
    public void AnyDamage(double now)
    {
        if (Stat(StatType.DamageTakenNextSkillAbilityDamageBonus) > 0) ReversalUntil = now + Stat(StatType.DamageTakenNextSkillAbilityWindowSeconds);
    }
    public int EvasionPercent(double now) => Stat(StatType.EvasionPercentAdjustment) +
        (now < HighGuardUntil ? Stat(StatType.CostlyAbilityUsedEvasionPercentAdjustment) : 0) +
        (Reactions.GetWindowStage(At(now)) == MeleeTechniqueStage.Opening ? LineStat(WindowLine, StatType.TechniqueOpeningFinisherEvasionPercent) : 0);
    public int DefensePercent(double now) => Stat(StatType.PhysicalDefensePercentAdjustment) + (now < ControlDefenseUntil ? Stat(StatType.StatusAppliedSelfDefensePercentAdjustment) : 0);
    public int HastePercent(double now) => Stat(StatType.AttackDelayReductionPercent) + _owner.Spec.WeaponCombat.ExternalHastePercent +
        (Reactions.GetWindowStage(At(now)) == MeleeTechniqueStage.Finish ? LineStat(WindowLine, StatType.TechniqueFinishFinisherHastePercent) : 0);
    public double AttackSeconds(double now) => Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(_owner.Spec.WeaponCombat.WeaponDelayUnits, 0, HastePercent(now), 0)) / 1000d;
    public double SwingSeconds(double now) => Combat.CalculateAttackSwingDelay((int)Math.Round(AttackSeconds(now)*1000)) / 1000d;
    public int TakeSwingAttacks(double now)
    {
        var attacks = Combat.CalculateAttacksPerSwing((int)Math.Round(AttackSeconds(now)*1000), _attackDebt, out var debt);
        _attackDebt = debt;
        Count("SwingAnimations");
        Count("BatchedExtraAttacks", attacks-1);
        return attacks;
    }
    public void Integrate(double now, double step)
    {
        Refresh(now);
        if (Control.IsDazed(now)) Count("DazedSeconds", Math.Min(step, Control.DazedUntil-now));
        if (Reactions.GetWindowStage(At(now)) == MeleeTechniqueStage.Opening) Count("FlowEvasionSeconds", Math.Min(step, (Reactions.WindowExpires-At(now)).TotalSeconds));
        if (Reactions.GetWindowStage(At(now)) == MeleeTechniqueStage.Finish) Count("FlowHasteSeconds", Math.Min(step, (Reactions.WindowExpires-At(now)).TotalSeconds));
        if (now < HighGuardUntil) Count("HighGuardEvasionSeconds", Math.Min(step, HighGuardUntil-now));
    }
    public void Death() { Control.Clear(); _retaliationEnemy = null; _attackDebt = 0; _windowEnemy = null; _stance = null; ClearSetup(); Reactions.ClearBenefits(); Resources.ClearBenefits(); Rattled.Clear(); LowKick.Clear(); HighGuardUntil = ControlDefenseUntil = ReversalUntil = 0; }
    public static int Adjust(int value, int percent) => Math.Max(1, value + Math.Sign(percent) * (int)Math.Ceiling(value * Math.Abs(percent) / 100f));
}

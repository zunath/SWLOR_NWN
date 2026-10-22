using System.Reflection;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.CombatSim.Engine;

public sealed class WeaponControlState
{
    private static readonly double PostControlSeconds = Convert.ToDouble(typeof(Ability)
        .GetField("TemporaryImmunityBaseDurationSeconds", BindingFlags.NonPublic | BindingFlags.Static)?.GetRawConstantValue()
        ?? throw new InvalidDataException("Cannot read the production post-control immunity duration."));

    public double DazedUntil { get; private set; }
    public double KnockdownUntil { get; private set; }
    /// <summary>Hamstring is a soft control effect: it stops nothing in a stationary fight but counts as control.</summary>
    public double HamstrungUntil { get; private set; }
    public double HardControlImmunityUntil { get; private set; }
    public bool IsDazed(double now) => now < DazedUntil;
    public bool IsKnockedDown(double now) => now < KnockdownUntil;
    /// <summary>Dazed or knocked down: no new actions or swings (native EffectDazed and EffectKnockdown).</summary>
    public bool IsIncapacitated(double now) => IsDazed(now) || IsKnockedDown(now);
    public double IncapacitatedUntil => Math.Max(DazedUntil, KnockdownUntil);
    /// <summary>StatusEffect.HasStatusEffectCategory(target, Control) for the controls the weapon simulation applies.</summary>
    public bool IsControlled(double now) => IsIncapacitated(now) || now < HamstrungUntil;

    // Ability.HasHardCrowdControlImmunity: shared post-control immunity, or a hard control already active.
    private bool HasHardControlImmunity(double now) => now < HardControlImmunityUntil || IsIncapacitated(now);

    public bool TryDaze(double now, double duration)
    {
        if (duration <= 0 || HasHardControlImmunity(now)) return false;
        DazedUntil = now + duration;
        HardControlImmunityUntil = DazedUntil + PostControlSeconds;
        return true;
    }

    public bool TryKnockdown(double now, double duration)
    {
        if (duration <= 0 || HasHardControlImmunity(now)) return false;
        KnockdownUntil = now + duration;
        HardControlImmunityUntil = KnockdownUntil + PostControlSeconds;
        return true;
    }

    /// <summary>
    /// A conversion (StatusEffect's replacedStatusEffectType) ends the daze without its post-control immunity, so the
    /// daze's own active control and pending immunity do not block it, and the knockdown cannot outlast the daze's
    /// remaining whole ticks (ClampConvertedControlDurationTicks). Knockdown's removal then grants the shared immunity.
    /// </summary>
    public bool TryConvertDazeToKnockdown(double now, double duration, float frequency)
    {
        if (!IsDazed(now)) return false;
        var step = Math.Max(1f, frequency);
        duration = Math.Min(duration, Math.Floor((DazedUntil - now) / step) * step);
        if (duration <= 0) return false;
        DazedUntil = now;
        KnockdownUntil = now + duration;
        HardControlImmunityUntil = KnockdownUntil + PostControlSeconds;
        return true;
    }

    public void Hamstring(double now, double duration) => HamstrungUntil = now + duration;

    public void Clear() => DazedUntil = KnockdownUntil = HamstrungUntil = HardControlImmunityUntil = 0;

    public static double Duration(IStatusEffect status, int baseSeconds, int outgoingPercent, int resistance, bool pvp)
    {
        var multiplier = Resistance.CalculateResistanceDamageMultiplier(resistance);
        if (multiplier <= 0) return 0;
        var seconds = StatusEffect.CalculateAdjustedStatusDurationSeconds(baseSeconds, outgoingPercent);
        var frequency = Math.Max(1f, status.Frequency);
        var ticks = Math.Max(1, (int)Math.Ceiling(seconds / frequency));
        var withoutResistance = StatusEffect.ClampHardCrowdControlDurationTicks(status.Categories, ticks, frequency, isPlayerAgainstPlayer: pvp);
        ticks = Math.Max(1, (int)Math.Round(ticks * multiplier));
        ticks = StatusEffect.ClampHardCrowdControlDurationTicks(status.Categories, ticks, frequency, isPlayerAgainstPlayer: pvp);
        return Math.Max(0.1f, Math.Min(seconds + Math.Max(0, ticks-withoutResistance)*frequency, ticks*frequency));
    }
}

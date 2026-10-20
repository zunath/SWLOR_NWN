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
    public double HardControlImmunityUntil { get; private set; }
    public bool IsDazed(double now) => now < DazedUntil;

    public bool TryDaze(double now, double duration)
    {
        if (duration <= 0 || now < DazedUntil || now < HardControlImmunityUntil) return false;
        DazedUntil = now + duration;
        HardControlImmunityUntil = DazedUntil + PostControlSeconds;
        return true;
    }

    public void Clear() => DazedUntil = HardControlImmunityUntil = 0;

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

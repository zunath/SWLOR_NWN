namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>One bounded meter: a value from 0 to <see cref="Max"/>.</summary>
public readonly record struct MinigameMeter(int Value, int Max)
{
    /// <summary>The value as a share of the maximum, 0 to 1. This is the "quality percent" the outcome contract uses.</summary>
    public float Fraction => Max <= 0 ? 0f : (float)Value / (float)Max;

    /// <summary>The value as a whole percentage, rounded down.</summary>
    public int Percent => Max <= 0 ? 0 : (int)((long)Value * 100L / Max);

    public bool IsFull => Max > 0 && Value >= Max;

    public bool IsEmpty => Value <= 0;

    /// <summary>The meter moved by <paramref name="delta"/> and kept inside 0 to <see cref="Max"/>.</summary>
    public MinigameMeter Add(int delta) => new(Math.Clamp(Value + delta, 0, Math.Max(Max, 0)), Max);
}

using System;

namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>
    /// What a city does to a holding's event rolls while it is advanced (Industrial Charter). Both scales default to 1, which
    /// changes nothing. Only the setback band shrinks: boom, raid and customs keep their chances, and the freed share is no event.
    /// </summary>
    public sealed class HoldingModifiers : IEquatable<HoldingModifiers>
    {
        /// <summary>No change to any roll.</summary>
        public static readonly HoldingModifiers None = new();

        /// <summary>The share of the setback chance that remains (0.5 halves it).</summary>
        public double SetbackChanceScale { get; init; } = 1.0;

        /// <summary>The share of a setback's repair goods that is needed (0.5 halves them), rounded up.</summary>
        public double RepairUnitsScale { get; init; } = 1.0;

        public bool IsNone => Math.Abs(SetbackChanceScale - 1.0) < 1e-9 && Math.Abs(RepairUnitsScale - 1.0) < 1e-9;

        public bool Equals(HoldingModifiers other)
        {
            return other != null &&
                   Math.Abs(SetbackChanceScale - other.SetbackChanceScale) < 1e-9 &&
                   Math.Abs(RepairUnitsScale - other.RepairUnitsScale) < 1e-9;
        }

        public override bool Equals(object obj) => Equals(obj as HoldingModifiers);

        public override int GetHashCode() => HashCode.Combine(Math.Round(SetbackChanceScale, 6), Math.Round(RepairUnitsScale, 6));
    }
}

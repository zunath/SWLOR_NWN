namespace SWLOR.CraftSim.Gates
{
    /// <summary>One balance gate of CraftingRevampPlan.md that a minigame passes before it ships; it drives the real engine and reads the legacy baseline.</summary>
    public interface IBalanceGate
    {
        /// <summary>One of <see cref="GateNames"/>.</summary>
        string Name { get; }

        GateResult Evaluate(GateContext context);
    }
}

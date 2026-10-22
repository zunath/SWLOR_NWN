namespace SWLOR.CraftSim.Gates
{
    public enum GateVerdict
    {
        Passed,
        Failed,

        /// <summary>The gate has no engine to drive yet, or its inputs are missing.</summary>
        NotRun
    }
}

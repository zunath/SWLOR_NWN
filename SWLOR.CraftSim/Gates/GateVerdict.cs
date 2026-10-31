namespace SWLOR.CraftSim.Gates
{
    public enum GateVerdict
    {
        Passed,
        Failed,

        /// <summary>The gate has no engine to drive yet, or its inputs are missing.</summary>
        NotRun,

        /// <summary>The scenario has nothing for the gate to judge, such as no style to compare. It does not block shipping, and the report says why.</summary>
        NotApplicable
    }
}

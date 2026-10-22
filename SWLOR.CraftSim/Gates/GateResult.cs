namespace SWLOR.CraftSim.Gates
{
    public sealed class GateResult
    {
        public GateResult(string gate, GateVerdict verdict, string summary, IReadOnlyList<GateTable> tables = null)
        {
            Gate = gate;
            Verdict = verdict;
            Summary = summary;
            Tables = tables ?? Array.Empty<GateTable>();
        }

        public string Gate { get; }
        public GateVerdict Verdict { get; }
        public string Summary { get; }
        public IReadOnlyList<GateTable> Tables { get; }

        public static GateResult NotRun(string gate, string reason) => new(gate, GateVerdict.NotRun, reason);
    }
}

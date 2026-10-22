using System.Text;
using SWLOR.CraftSim.Report;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>Runs gates and writes their verdicts and evidence as markdown, in the same style as the baseline reports.</summary>
    public static class GateReport
    {
        public static IReadOnlyList<GateResult> Run(IEnumerable<IBalanceGate> gates, GateContext context) =>
            gates.Select(gate => gate.Evaluate(context)).ToList();

        public static string Build(string title, GateContext context, IReadOnlyList<GateResult> results, string command)
        {
            var text = new StringBuilder();
            text.AppendLine($"# {title}");
            text.AppendLine();
            text.AppendLine($"- Command: `{command}`");
            text.AppendLine($"- Seed {context.Seed}; {context.Runs} runs per case.");
            text.AppendLine();
            text.AppendLine("## Verdicts");
            text.AppendLine();
            var verdicts = new MarkdownTable("Gate", "Verdict", "Summary");
            foreach (var result in results)
                verdicts.Add(result.Gate, result.Verdict.ToString(), result.Summary);

            text.AppendLine(verdicts.ToString());
            foreach (var result in results.Where(r => r.Tables.Count > 0))
            {
                text.AppendLine($"## {result.Gate}");
                text.AppendLine();
                foreach (var gateTable in result.Tables)
                {
                    text.AppendLine($"### {gateTable.Title}");
                    text.AppendLine();
                    var table = new MarkdownTable(gateTable.Header.ToArray());
                    foreach (var row in gateTable.Rows)
                        table.Add(row.ToArray());

                    text.AppendLine(table.ToString());
                }
            }

            return text.ToString();
        }

        /// <summary>A minigame ships only when no gate failed and none was left unrun.</summary>
        public static bool AllPassed(IReadOnlyList<GateResult> results) =>
            results.Count > 0 && results.All(r => r.Verdict == GateVerdict.Passed);
    }
}

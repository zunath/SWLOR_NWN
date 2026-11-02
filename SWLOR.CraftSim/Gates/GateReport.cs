using System.Text;
using SWLOR.CraftSim.Report;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>Runs gates and writes their verdicts and evidence as markdown, in the same style as the baseline reports.</summary>
    public static class GateReport
    {
        public static IReadOnlyList<GateResult> Run(IEnumerable<IBalanceGate> gates, GateContext context) =>
            gates.Select(gate => gate.Evaluate(context)).ToList();

        public static string Build(string title, GateContext context, IReadOnlyList<GateResult> results, string command) =>
            Build(title, context, results, command, Array.Empty<string>(), Array.Empty<string>());

        /// <summary>
        /// The report <c>SkillPerkStandards.md</c> asks balance evidence for: the command and seed, the inputs and comparators, the
        /// verdicts, each gate's evidence tables, and the limitations.
        /// </summary>
        /// <param name="inputs">Bullet lines describing what was played and against what, written under "Inputs".</param>
        /// <param name="limitations">Bullet lines saying what the results do not cover, written under "Limitations".</param>
        public static string Build(
            string title,
            GateContext context,
            IReadOnlyList<GateResult> results,
            string command,
            IReadOnlyList<string> inputs,
            IReadOnlyList<string> limitations)
        {
            var text = new StringBuilder();
            text.AppendLine($"# {title}");
            text.AppendLine();
            text.AppendLine($"- Command: `{command}`");
            text.AppendLine($"- Seed {context.Seed}; {context.Runs} runs per case.");
            text.AppendLine();
            if (inputs.Count > 0)
            {
                text.AppendLine("## Inputs");
                text.AppendLine();
                foreach (var input in inputs)
                    text.AppendLine(input);

                text.AppendLine();
            }

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

            if (limitations.Count > 0)
            {
                text.AppendLine("## Limitations");
                text.AppendLine();
                foreach (var limitation in limitations)
                    text.AppendLine(limitation);
            }

            return text.ToString();
        }

        /// <summary>
        /// A minigame ships only when each named gate has exactly one result and it passed. A gate that is not applicable to the
        /// scenario (no style to compare) does not block, but a gate that was not run does.
        /// </summary>
        public static bool AllPassed(IReadOnlyList<GateResult> results) =>
            GateNames.All.All(name =>
                results.Count(r => r.Gate == name) == 1 &&
                results.Single(r => r.Gate == name).Verdict is GateVerdict.Passed or GateVerdict.NotApplicable);
    }
}

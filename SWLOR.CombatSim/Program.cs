using System.Globalization;
using Newtonsoft.Json;
using SWLOR.CombatSim.Cli;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Report;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;

const string Usage = """
SWLOR combat simulator

  run <scenario.json> [--rules current,tuned,tuned-pvp] [--rules-file custom.json]... [--runs 200] [--seed 1] [--out report.md] [--json results.json] [--detail]
      Runs the scenario under each rule set and prints a comparison; --json also writes the raw per-rule-set results.
  rules [--name tuned-pvp]
      Prints a rule preset as JSON, to copy into a --rules-file and edit.
  abilities [--out abilities.csv]
      Writes the ability cost and timing data the simulator reads from the game server.
""";

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    Console.WriteLine(Usage);
    return 0;
}

try
{
    return args[0] switch
    {
        "run" => Run(args.Skip(1).ToArray()),
        "rules" => PrintRules(args.Skip(1).ToArray()),
        "abilities" => DumpAbilities(args.Skip(1).ToArray()),
        _ => Fail($"Unknown command '{args[0]}'.")
    };
}
catch (Exception ex) when (ex is InvalidDataException or ArgumentException or KeyNotFoundException or FileNotFoundException or JsonException)
{
    return Fail(ex.Message);
}

static int Run(string[] args)
{
    if (args.Length == 0)
        return Fail("run needs a scenario file.");

    var options = CommandLineOptions.Parse(
        args.Skip(1).ToArray(),
        new[] { "--rules", "--rules-file", "--runs", "--seed", "--out", "--json" },
        new[] { "--detail" },
        new[] { "--rules", "--rules-file" });
    if (options.TryGetValue("--json", out var jsonPath) && options.TryGetValue("--out", out var outPath) &&
        CommandLineOptions.AreSameFile(jsonPath[0], outPath[0]))
        return Fail("--json and --out name the same file, so the report would overwrite the raw results.");

    var scenario = ScenarioSpec.Load(args[0]);
    var runs = CommandLineOptions.GetInt(options, "--runs", 200, 1, BatchRunner.MaximumRuns);
    var seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue);

    var ruleSets = RuleSetSelection.Resolve(options);

    var catalog = AbilityCatalog.Load();
    var summaries = ruleSets.Select(rules => BatchRunner.Run(scenario, rules, catalog, runs, seed)).ToList();
    var report = MarkdownReport.Build(scenario, summaries, options.ContainsKey("--detail"));

    if (options.TryGetValue("--json", out var json))
        File.WriteAllText(json[0], JsonConvert.SerializeObject(summaries, Formatting.Indented));

    if (options.TryGetValue("--out", out var output))
    {
        File.WriteAllText(output[0], report);
        Console.WriteLine($"Wrote {output[0]}");
    }
    else
    {
        Console.WriteLine(report);
    }

    return 0;
}

static int PrintRules(string[] args)
{
    var options = CommandLineOptions.Parse(args, new[] { "--name" });
    var name = options.GetValueOrDefault("--name", new List<string> { RuleSets.TunedPvpName })[0];
    Console.WriteLine(JsonConvert.SerializeObject(RuleSets.Get(name), Formatting.Indented));
    return 0;
}

static int DumpAbilities(string[] args)
{
    var options = CommandLineOptions.Parse(args, new[] { "--out" });
    var catalog = AbilityCatalog.Load();
    var lines = new List<string> { "feat,name,perk,recast_group,skill_requirement,fp,stm,recast_seconds,cast_seconds,impact_delay_seconds,queued_weapon,hostile,aimed,healing,force_affinity,unreadable_timing" };
    lines.AddRange(catalog.Abilities.Values
        .OrderBy(a => a.PerkName).ThenBy(a => a.Name)
        .Select(a => string.Join(",",
            a.Feat, Csv(a.Name), Csv(a.PerkName), a.RecastGroup, a.SkillRequirement, a.FP, a.STM,
            a.RecastSeconds.ToString(CultureInfo.InvariantCulture), a.CastSeconds.ToString(CultureInfo.InvariantCulture),
            a.ImpactDelaySeconds.ToString(CultureInfo.InvariantCulture),
            a.IsQueuedWeapon, a.IsHostile, a.IsAimed, a.IsHealing, a.ForceAffinity, a.UnreadableTiming)));

    var output = options.GetValueOrDefault("--out", new List<string> { "abilities.csv" })[0];
    File.WriteAllLines(output, lines);
    Console.WriteLine($"Wrote {catalog.Abilities.Count} abilities to {output} ({catalog.LoadErrors.Count} definitions or timings could not be read outside the engine).");
    return 0;
}

static string Csv(string value) => value.Contains(',') ? $"\"{value}\"" : value;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 1;
}

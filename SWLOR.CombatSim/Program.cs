using System.Globalization;
using Newtonsoft.Json;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Report;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;

const string Usage = """
SWLOR combat simulator

  run <scenario.json> [--rules current,wave1,wave2] [--rules-file custom.json]... [--runs 200] [--seed 1] [--out report.md] [--json results.json] [--detail]
      Runs the scenario under each rule set and prints a comparison; --json also writes the raw per-rule-set results.
  rules [--name wave2]
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

    var scenario = ScenarioSpec.Load(args[0]);
    var options = ParseOptions(args.Skip(1).ToArray());
    var runs = int.Parse(options.GetValueOrDefault("--runs", new List<string> { "200" })[0], CultureInfo.InvariantCulture);
    var seed = int.Parse(options.GetValueOrDefault("--seed", new List<string> { "1" })[0], CultureInfo.InvariantCulture);

    var ruleSets = new List<RuleSet>();
    var names = options.TryGetValue("--rules", out var ruleNames)
        ? ruleNames.SelectMany(n => n.Split(',', StringSplitOptions.RemoveEmptyEntries))
        : options.ContainsKey("--rules-file") ? Enumerable.Empty<string>() : RuleSets.Names;
    ruleSets.AddRange(names.Select(RuleSets.Get));
    if (options.TryGetValue("--rules-file", out var files))
        ruleSets.AddRange(files.Select(f => JsonConvert.DeserializeObject<RuleSet>(File.ReadAllText(f))));

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
    var options = ParseOptions(args);
    var name = options.GetValueOrDefault("--name", new List<string> { RuleSets.Wave2Name })[0];
    Console.WriteLine(JsonConvert.SerializeObject(RuleSets.Get(name), Formatting.Indented));
    return 0;
}

static int DumpAbilities(string[] args)
{
    var options = ParseOptions(args);
    var catalog = AbilityCatalog.Load();
    var lines = new List<string> { "feat,name,perk,recast_group,skill_requirement,fp,stm,recast_seconds,cast_seconds,hostile,healing,force_affinity" };
    lines.AddRange(catalog.Abilities.Values
        .OrderBy(a => a.PerkName).ThenBy(a => a.Name)
        .Select(a => string.Join(",",
            a.Feat, Csv(a.Name), Csv(a.PerkName), a.RecastGroup, a.SkillRequirement, a.FP, a.STM,
            a.RecastSeconds.ToString(CultureInfo.InvariantCulture), a.CastSeconds.ToString(CultureInfo.InvariantCulture),
            a.IsHostile, a.IsHealing, a.ForceAffinity)));

    var output = options.GetValueOrDefault("--out", new List<string> { "abilities.csv" })[0];
    File.WriteAllLines(output, lines);
    Console.WriteLine($"Wrote {catalog.Abilities.Count} abilities to {output} ({catalog.LoadErrors.Count} definitions could not be read outside the engine).");
    return 0;
}

static Dictionary<string, List<string>> ParseOptions(string[] args)
{
    var options = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--"))
            throw new ArgumentException($"Unexpected argument '{args[i]}'.");

        if (!options.TryGetValue(args[i], out var values))
            options[args[i]] = values = new List<string>();

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            values.Add(args[++i]);
    }

    return options;
}

static string Csv(string value) => value.Contains(',') ? $"\"{value}\"" : value;

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 1;
}

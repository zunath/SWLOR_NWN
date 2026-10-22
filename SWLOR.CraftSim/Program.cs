using Newtonsoft.Json;
using SWLOR.CraftSim.Cli;

const string Usage = """
SWLOR crafting simulator

  baseline [--section crafting|gathering|all] [--runs 200] [--seed 1] [--gathering-runs 20000] [--skills Smithery,...] [--out report.md] [--json results.json] [--repo path]
      Plays today's rules (the old craft minigame, harvesting, scavenging, refining and the fishing catch roll) and writes the legacy baseline.
  gear [--repo path]
      Prints the gear levels derived from the module's crafting gear.
  gates --scenario <name> [--runs 100] [--seed 1] [--recipes-per-band 6 | --all-recipes] [--gates rotation,styles,perks,length,materials,xp] [--legacy-seconds-per-action 6] [--thorough] [--out report.md] [--json results.json] [--strict] [--repo path]
      Runs the crafting balance gates for a minigame scenario and writes the report (default design/testing/crafting-revamp/<scenario>-gates.md).
      --thorough gives gate 1's fixed-rotation solver a much longer search. --strict exits 2 when a gate failed or was not run.
  gates --list
      Lists the scenarios minigames have added.
  research [--runs 400] [--adaptive-runs 40] [--seed 1] [--seconds-per-action 6] [--job-overhead-seconds 30] [--out report.md] [--repo path]
      Plays the research Experiment and writes its report: length, bonus size against today's dice and the supply change, the time cut, and Fabrication XP per real hour.
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
        "baseline" => BaselineCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "gear" => GearCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "gates" => GatesCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "research" => ResearchCommand.Run(args.Skip(1).ToArray(), Console.Out),
        _ => Fail($"Unknown command '{args[0]}'.")
    };
}
catch (Exception ex) when (ex is InvalidDataException or ArgumentException or KeyNotFoundException or IOException or JsonException)
{
    return Fail(ex.Message);
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 1;
}

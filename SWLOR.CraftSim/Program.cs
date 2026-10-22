using Newtonsoft.Json;
using SWLOR.CraftSim.Cli;

const string Usage = """
SWLOR crafting simulator

  baseline [--section crafting|gathering|all] [--runs 200] [--seed 1] [--gathering-runs 20000] [--skills Smithery,...] [--out report.md] [--json results.json] [--repo path]
      Plays today's rules (the old craft minigame, harvesting, scavenging, refining and the fishing catch roll) and writes the legacy baseline.
  gear [--repo path]
      Prints the gear levels derived from the module's crafting gear.
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

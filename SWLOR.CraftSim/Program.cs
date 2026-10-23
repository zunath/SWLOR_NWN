using Newtonsoft.Json;
using SWLOR.CraftSim.Cli;

const string Usage = """
SWLOR crafting simulator

  baseline [--section crafting|gathering|all] [--runs 200] [--seed 1] [--gathering-runs 20000] [--skills Smithery,...] [--out report.md] [--json results.json] [--repo path]
      Plays today's rules (the old craft minigame, harvesting, scavenging, refining and the fishing catch roll) and writes the legacy baseline.
  progression [--runs 500] [--seed 1] [--skills Smithery,...] [--node-seconds 30] [--overhead-seconds 30] [--might 0] [--out report.md] [--json results.json] [--repo path]
      Models a new crafter's hours to rank 50 in each crafting skill (components gathered included) and in Gathering, the guild task and research cohorts,
      and writes the progression-pace report with the proposals for the owner (default design/testing/crafting-revamp/progression-pace-baseline.md is the committed one).
  legacy-enhancements [--runs 200] [--seed 1] [--recipes-per-band 6] [--skills Smithery,...] [--gear all|best] [--out report.md] [--json results.json] [--repo path]
      Plays today's rules with 1, 2 and 3 enhancements slotted and writes the expected transferred enhancements per band, tier and slot count (the yardstick of gate 7).
  gear [--repo path]
      Prints the gear levels derived from the module's crafting gear.
  gates --scenario <name> [--runs 100] [--seed 1] [--recipes-per-band 6 | --all-recipes] [--gates rotation,styles,perks,length,materials,xp,enhancements] [--legacy-seconds-per-action 6] [--thorough] [--out report.md] [--json results.json] [--strict] [--repo path]
      Runs the crafting balance gates for a minigame scenario and writes the report (default design/testing/crafting-revamp/<scenario>-gates.md).
      --thorough gives gate 1's fixed-rotation solver a much longer search. --strict exits 2 when a gate failed or was not run.
      --gates also takes gate numbers, 1 to 7 (7 is enhancement parity).
  gates --list
      Lists the scenarios minigames have added.
  fishing [--runs 200] [--seed 1] [--seconds-per-turn 8] [--out report.md] [--json results.json] [--strict] [--repo path]
      Plays the Line (the fishing fight) against today's catch roll over a Willpower sweep, runs the XP-rate gate over the fishing cohort,
      and writes the report (default design/testing/crafting-revamp/fishing-line-parity.md).
  search [--runs 4000] [--seed 1] [--seconds-per-action 6] [--site-seconds 30] [--out report.md] [--json results.json] [--strict] [--repo path]
      Plays the Search board (scavenging) against today's search over the Gathering ranks, checks Quick Search against today's expected rolls and XP, runs the economy-parity,
      length and XP-rate gates over the scavenging cohort, and writes the report (default design/testing/crafting-revamp/scavenging-search-parity.md).
  harvesting [--runs 4000] [--seed 1] [--out report.md] [--json results.json] [--strict] [--repo path]
      Plays Quick Harvest and Prospecting sessions against today's harvests over every node tier and Gathering rank, runs the economy-parity, length and
      XP-rate gates over the harvesting cohort, and writes the report (default design/testing/crafting-revamp/harvesting-prospecting-parity.md).
  research [--runs 400] [--adaptive-runs 40] [--seed 1] [--seconds-per-action 6] [--job-overhead-seconds 30] [--out report.md] [--repo path]
      Plays the research Experiment and writes its report: length, reduction size against today's dice and the supply change, the time cut, and Fabrication XP per real hour.
  researcher-perks [--runs 400] [--seed 1] [--seconds-per-action 6] [--craft-seconds-per-action 8] [--job-overhead-seconds 30] [--search | --ablate | --order] [--out report.md] [--repo path]
      Plays the Fabrication Researcher line and writes its evidence: the two styles against each other, each purchase's effect, Peer Review for an undecided player, and Fabrication XP per real hour against the crafting ceiling.
  angler-perks [--runs 400] [--seed 1] [--out report.md] [--repo path]
      Plays the Agriculture Angler line and writes its evidence: each purchase alone, the whole line at every rank against Quick Reel and the unperked fight, the two styles, and the special rare fish.
  chef-perks [--runs 40] [--seed 1] [--recipes-per-band 6] [--styles-only] [--mid-gear-only] [--candidates 40/25/35;...] [--out report.md] [--repo path]
      Plays the Agriculture Chef line and writes its evidence: each purchase's effect on actions, CP and quality at rank 50, the two styles compared, the whole line at every rank, and the styles' magnitudes against gate 2.
  builder-perks [--runs 60] [--seed 1] [--recipes-per-band 8] [--out report.md] [--repo path]
      Plays the Fabrication Builder line and writes its evidence: each purchase's effect on actions, CP and quality at rank 50, the two styles compared, and the whole line at every rank.
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
        "progression" => ProgressionCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "legacy-enhancements" => LegacyEnhancementsCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "gear" => GearCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "gates" => GatesCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "fishing" => FishingCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "search" => SearchCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "research" => ResearchCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "harvesting" => HarvestingCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "builder-perks" => BuilderPerksCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "angler-perks" => AnglerPerksCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "chef-perks" => ChefPerksCommand.Run(args.Skip(1).ToArray(), Console.Out),
        "researcher-perks" => ResearcherPerksCommand.Run(args.Skip(1).ToArray(), Console.Out),
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

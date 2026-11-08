using SWLOR.FacadeRemoval;

/*
 * Facade removal (P2-Z, Readmes/MaintainabilityRefactorFacadeRemoval.md).
 *
 * The facades listed in tools/Refactor/RemovedFacades.json are deleted on the branch; static code reaches their services
 * through SWLOR.Game.Server.Composition.GameServices. This tool keeps the callers in that shape, so it can run again after
 * every merge of master (it is a mechanical script, tools/Refactor/MechanicalScripts.txt):
 *   - a file that calls a removed facade (Taxi.X) gets `using static SWLOR.Game.Server.Composition.GameServices;`, or its
 *     project's GlobalUsings.cs gets the global form when it imports SWLOR.Game.Server.Service globally;
 *   - a qualified call (Service.Taxi.X) loses its qualifier; an explicit name (Combat) is written GameServices.Combat.X;
 *     a moved constant (Combat.MaximumHitRate) is read from its owner (CombatRules.MaximumHitRate);
 *   - each manifest line of SWLOR.Game.Server/Core/ScriptHandlerOrder.txt that names a removed facade's handler takes the
 *     branch name tools/Refactor/HandlerRenames.txt gives it.
 * A tree without SWLOR.Game.Server/Composition/GameServices.cs (master, the parity copy) is skipped entirely, and so is a
 * facade whose source file still exists, so a run there changes nothing.
 *
 * Usage (from the repository root, or use tools/Refactor/Apply-FacadeRemoval.ps1):
 *   dotnet run --project tools/Refactor/SWLOR.FacadeRemoval -- [--repo <root>] [--data <RemovedFacades.json>]
 *       apply; a second run changes nothing
 *   ... --check
 *       write nothing; exit 1 when an apply would change something
 *   ... --reverse --path <folder>
 *       undo the caller rewrites in <folder> (the parity copy of the characterization tests), so it compiles against master
 *
 * Exit codes: 0 success, 1 a check found changes, 2 the input could not be processed (nothing written).
 */

string? root = null;
string? data = null;
string? path = null;
var mode = RunMode.Apply;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--repo" when i + 1 < args.Length:
            root = Path.GetFullPath(args[++i]);
            break;
        case "--data" when i + 1 < args.Length:
            data = Path.GetFullPath(args[++i]);
            break;
        case "--path" when i + 1 < args.Length:
            path = Path.GetFullPath(args[++i]);
            break;
        case "--check":
            mode = RunMode.Check;
            break;
        case "--reverse":
            mode = RunMode.Reverse;
            break;
        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            Console.Error.WriteLine("Usage: [--repo <root>] [--data <RemovedFacades.json>] [--check | --reverse --path <folder>]");
            return FacadeRemovalRunner.Failed;
    }
}

if (mode == RunMode.Reverse && path == null)
{
    Console.Error.WriteLine("--reverse needs --path <folder>: the folder to rewrite back to master's shape.");
    return FacadeRemovalRunner.Failed;
}

var repository = RepositoryLocator.Find(Directory.GetCurrentDirectory()) ?? RepositoryLocator.Find(AppContext.BaseDirectory);
root ??= repository;
data ??= repository == null ? null : Path.Combine(repository, "tools", "Refactor", "RemovedFacades.json");
if ((root == null && mode != RunMode.Reverse) || data == null)
{
    Console.Error.WriteLine("Could not find the repository root (SWLOR.Game.Server.sln). Pass --repo <root> and --data <RemovedFacades.json>.");
    return FacadeRemovalRunner.Failed;
}

return FacadeRemovalRunner.Run(new RunOptions(mode, mode == RunMode.Reverse ? path! : root!, data), Console.Out);

using SWLOR.TypeMoves;

/*
 * Type moves (P2-Z3, Readmes/DependencyInjection.md "No support namespaces").
 *
 * Moves the types tools/Refactor/TypeMoves.json lists out of the namespace SWLOR.Game.Server.Service into their domain
 * namespaces, and keeps every reference in that shape, so it can run again after every merge of master (it is a mechanical
 * script, tools/Refactor/MechanicalScripts.txt, through tools/Refactor/Move-ServiceTypes.ps1):
 *   - each listed file whose old path exists changes its namespace line and moves to the folder of its new namespace;
 *   - every C# file of the server and of every project that references it is compiled in memory (Roslyn, with the
 *     project's references) and its references are found with the semantic model: a qualified name (Service.Log,
 *     SWLOR.Game.Server.Service.Log, using static, alias) gets the new namespace; a simple name gets a using of the new
 *     namespace (at the file's sorted place); a using of SWLOR.Game.Server.Service goes where nothing binds through it any
 *     more (a type that stays there, such as master's facades, keeps it);
 *   - the edited files are compiled again and every name of interest must bind to what it bound to before; a name that would
 *     bind to something else (Random meaning System.Random inside Domain.Foundation, which Random joins) is written
 *     qualified (System.Random), and the run lists each one;
 *   - names and paths in other text (guides, scripts, manifests, path lists, C# comments and literals) follow, the handler
 *     manifest's lines of moved handlers are renamed in place and recorded in tools/Refactor/HandlerRenames.txt.
 * A type that moved already gets a stand-in in the old namespace for the analysis, so master's code merged later (which still
 * names the old place) is moved the same way. A second run changes nothing.
 *
 * Usage (from the repository root, or use tools/Refactor/Move-ServiceTypes.ps1):
 *   dotnet run --project tools/Refactor/SWLOR.TypeMoves -- [--repo <root>] [--data <TypeMoves.json>]
 *       apply; a second run changes nothing
 *   ... --check
 *       write nothing; list what an apply would change and exit 1 when that is anything
 *
 * Exit codes: 0 success, 1 a check found changes, 2 the input could not be processed (nothing written).
 */

string? root = null;
string? data = null;
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
        case "--check":
            mode = RunMode.Check;
            break;
        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            Console.Error.WriteLine("Usage: [--repo <root>] [--data <TypeMoves.json>] [--check]");
            return TypeMoveRunner.Failed;
    }
}

var repository = RepositoryLocator.Find(Directory.GetCurrentDirectory()) ?? RepositoryLocator.Find(AppContext.BaseDirectory);
root ??= repository;
data ??= repository == null ? null : Path.Combine(repository, "tools", "Refactor", "TypeMoves.json");
if (root == null || data == null)
{
    Console.Error.WriteLine("Could not find the repository root (SWLOR.Game.Server.sln). Pass --repo <root> and --data <TypeMoves.json>.");
    return TypeMoveRunner.Failed;
}

return TypeMoveRunner.Run(new RunOptions(mode, root, data), Console.Out);

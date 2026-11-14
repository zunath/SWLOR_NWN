using SWLOR.TypeMoves;

/*
 * Type moves (P2-Z3, Readmes/DependencyInjection.md "No support namespaces"; P5-1c, Readmes/MaintainabilityRefactorDomainOrder.md
 * section 5).
 *
 * Moves the types a data file lists out of the namespace each one leaves into the namespace it joins, and keeps every
 * reference in that shape, so it can run again after every merge of master (a mechanical script,
 * tools/Refactor/MechanicalScripts.txt). Two data files use it: tools/Refactor/TypeMoves.json (P2-Z3, the types left
 * directly in SWLOR.Game.Server.Service, through Move-ServiceTypes.ps1) and tools/Refactor/DomainOrderTypeMoves.json (the
 * domain order's moves, each with its own source, through Move-DomainOrderTypes.ps1, which runs after it). A target is any
 * namespace of the server outside the source (Feature to Domain, Domain.Foundation to Core). A run:
 *   - moves each listed file whose old path exists to the folder of its new namespace and changes its namespace line (a
 *     file moves whole: the data lists every type it declares);
 *   - compiles every C# file of the server and of every project that references it in memory (Roslyn, with the project's
 *     references, documentation comments parsed) and finds its references with the semantic model: a qualified name
 *     (Service.Log, SWLOR.Game.Server.Service.Log, Crafting.RecipeType, in code, nameof, cref, using static and alias
 *     directives) has the segments that spell the source replaced by the target; a simple name, a cref, an attribute or a
 *     call of a moved class's extension method gets a using of the new namespace (at the file's sorted place); a using of
 *     the source goes where nothing binds through it any more when the data asks for that (TypeMoves.json), else only when
 *     the source is left without types (DomainOrderTypeMoves.json keeps it: the smallest edit);
 *   - compiles the edited files again: every name of interest must bind to what it bound to before; a name that would bind
 *     to something else (Random meaning System.Random inside Domain.Foundation, which Random joins) is written qualified
 *     (System.Random), a rewritten qualifier that would reach another namespace is written in full, and the run lists each;
 *   - rewrites names and paths in other text (guides, scripts, manifests, path lists, C# comments and literals), renames the
 *     handler manifest's lines of moved handlers in place and records them in tools/Refactor/HandlerRenames.txt.
 * A type that moved already gets a stand-in in its old namespace for the analysis, so master's code merged later (which
 * still names the old place) is moved the same way. A second run changes nothing.
 *
 * Usage (from the repository root, or use the wrappers):
 *   dotnet run --project tools/Refactor/SWLOR.TypeMoves -- [--repo <root>] [--data <data file>]
 *       apply (default data: tools/Refactor/TypeMoves.json); a second run changes nothing
 *   ... --check
 *       write nothing; list the pending moves and edits and exit 1 when there is anything
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
            Console.Error.WriteLine("Usage: [--repo <root>] [--data <data file>] [--check]");
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

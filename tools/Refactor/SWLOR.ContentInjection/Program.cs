using SWLOR.ContentInjection;

/*
 * Content injection (Readmes/MaintainabilityRefactorContentInjection.md).
 *
 * Content gets its services through its constructor instead of the static GameServices lookup. This tool converts the content files
 * tools/Refactor/ContentInjection.json lists, and keeps master's code in that shape at every sync (a mechanical script,
 * tools/Refactor/MechanicalScripts.txt):
 *   - a static content class that reaches a service becomes a content singleton the container builds: `static` comes off the class
 *     and every member that reaches a service, each service becomes a private get-only property named like the GameServices property
 *     (or <Name>Service when the class has a member of that name), set by a new constructor, and the class is registered in
 *     SWLOR.Game.Server/Composition/ContentRegistrations.cs, which the tool writes;
 *   - made content (any other class of a listed file) takes its services first in each constructor, forwarding a converted base's;
 *   - `new X(values)` of made content with services becomes `Content.Create<X, A...>(values)` everywhere, a class copying itself
 *     passes its services, and the creator gets Content (injected, the temporary GameServices.Content, or a fixture property in a
 *     unit test); a unit test calling a converted singleton gets a fixture property of it from its container;
 *   - what needs a person is refused and listed (cycles, state, static members of made content that reach a service, more than 10
 *     services, named arguments and initializers at creation sites, callers that cannot take a singleton, ...); a file with a refusal
 *     is left as it was.
 * A tree without SWLOR.Game.Server/Core/IContentFactory.cs (master, the parity copy) is skipped, so a run there changes nothing.
 *
 * Usage (from the repository root, or use tools/Refactor/Apply-ContentInjection.ps1):
 *   dotnet run --project tools/Refactor/SWLOR.ContentInjection -- [--repo <root>] [--data <ContentInjection.json>]
 *       apply; a second run changes nothing
 *   ... --check
 *       write nothing; exit 1 when an apply would change something
 *   ... --reverse --path <folder>
 *       turn Content.Create<X, A...>(args) back into new X(args) and remove the fixture properties in <folder> (the parity copy of the
 *       characterization tests), so it compiles against master; needs nothing of the tree
 *
 * Exit codes: 0 success, 1 a check found changes, 2 the input could not be processed (nothing written), 3 something was refused
 * (listed; the rest was converted).
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
            Console.Error.WriteLine("Usage: [--repo <root>] [--data <ContentInjection.json>] [--check | --reverse --path <folder>]");
            return ContentInjectionRunner.Failed;
    }
}

if (mode == RunMode.Reverse)
{
    if (path == null)
    {
        Console.Error.WriteLine("--reverse needs --path <folder>: the folder to turn back into master's form.");
        return ContentInjectionRunner.Failed;
    }

    return ContentInjectionRunner.Run(new RunOptions(mode, path, data ?? string.Empty), Console.Out);
}

var repository = RepositoryLocator.Find(Directory.GetCurrentDirectory()) ?? RepositoryLocator.Find(AppContext.BaseDirectory);
root ??= repository;
data ??= repository == null ? null : Path.Combine(repository, "tools", "Refactor", "ContentInjection.json");
if (root == null || data == null)
{
    Console.Error.WriteLine("Could not find the repository root (SWLOR.Game.Server.sln). Pass --repo <root> and --data <ContentInjection.json>.");
    return ContentInjectionRunner.Failed;
}

return ContentInjectionRunner.Run(new RunOptions(mode, root, data), Console.Out);

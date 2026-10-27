using SWLOR.EngineSeamGenerator;

/*
 * Engine seam generator.
 *
 * Reads the hand-written static wrappers in SWLOR.NWN.API/NWScript and NWScriptCustom and writes the engine seam:
 *   - INWScript: one interface member per wrapper member, with the wrapper's name, parameters, defaults and return type;
 *   - NwnCoreNWScript: the original wrapper bodies, moved verbatim into instance methods;
 *   - NWScriptRuntime: the current INWScript (NwnCoreNWScript by default) with a test-only override scope;
 *   - FakeNWScript (SWLOR.NWN.API.Testing): records every call and returns configurable values;
 *   - and each static wrapper body, rewritten to forward to NWScriptRuntime.Current. Signatures are untouched.
 *
 * Usage (from the repository root, or use tools/Refactor/Apply-EngineSeam.ps1):
 *   dotnet run --project tools/Refactor/SWLOR.EngineSeamGenerator -- [--repo <root>]
 *       write the output; a second run changes nothing
 *   dotnet run --project tools/Refactor/SWLOR.EngineSeamGenerator -- --check
 *       write nothing; exit 1 when the checked-in output differs from what the generator produces
 *   dotnet run --project tools/Refactor/SWLOR.EngineSeamGenerator -- --verify-origin <git-ref>
 *       write nothing; build the output from the wrapper files as committed at <git-ref> (the pristine hand-written code)
 *       and exit 1 when the working tree differs from it
 *
 * Exit codes: 0 success, 1 the working tree differs, 2 the input could not be processed. Nothing is written unless every
 * moved body round-trips textually, every signature matches and a second run is a fixed point.
 *
 * Adding or changing a wrapper: write the static method with its engine call as before, then run the generator; it moves
 * the new body and extends the interface and the fake. Never edit the generated files by hand.
 */

var root = (string?)null;
var mode = GeneratorMode.Apply;
string? revision = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--repo" when i + 1 < args.Length:
            root = Path.GetFullPath(args[++i]);
            break;
        case "--check":
            mode = GeneratorMode.Check;
            break;
        case "--verify-origin" when i + 1 < args.Length:
            mode = GeneratorMode.VerifyOrigin;
            revision = args[++i];
            break;
        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            Console.Error.WriteLine("Usage: [--repo <root>] [--check | --verify-origin <git-ref>]");
            return GeneratorRunner.Failed;
    }
}

root ??= RepositoryLocator.Find(Directory.GetCurrentDirectory())
         ?? RepositoryLocator.Find(AppContext.BaseDirectory);
if (root == null)
{
    Console.Error.WriteLine("Could not find the repository root (SWLOR.Game.Server.sln). Pass --repo <root>.");
    return GeneratorRunner.Failed;
}

return GeneratorRunner.Run(root, mode, revision, Console.Out);

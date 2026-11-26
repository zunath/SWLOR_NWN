using SWLOR.Game.Server.Tests.Service;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of <c>EnmityService</c> wherever it lives, for the tests that guard a rule by reading the code. The combat services split
/// (<c>Readmes/MaintainabilityRefactorCombatServicesSplit.md</c>) moves the text of <c>Domain/Combat/EnmityService.cs</c> into the change and
/// attack parts; the PR that moves a part adds its file to <see cref="PartFiles"/> and its interface to <see cref="PartInterfaces"/>.
/// </summary>
public static class EnmitySource
{
    /// <summary>The files that hold the code of <c>EnmityService.cs</c> as it was before the split, repository-relative, in reading order.</summary>
    public static readonly IReadOnlyList<string> PartFiles = new[]
    {
        "SWLOR.Game.Server/Domain/Combat/EnmityService.cs",
        "SWLOR.Game.Server/Domain/Combat/EnmityChangeService.cs",
        "SWLOR.Game.Server/Domain/Combat/EnmityAttackService.cs",
    };

    /// <summary>The interfaces of the parts: a call through a field of one of these types was a call on the same instance before the move.</summary>
    public static readonly IReadOnlySet<string> PartInterfaces = new HashSet<string>(StringComparer.Ordinal)
    {
        "IEnmityService", "IEnmityChangeService", "IEnmityAttackService",
    };

    private static readonly PartSourceFiles Files = new(PartFiles, PartInterfaces);

    /// <summary>The combined source of <c>EnmityService</c>.</summary>
    public static string Read() => Files.Read();

    /// <summary>The full paths of the files <see cref="Read"/> combines.</summary>
    public static IReadOnlyList<string> Paths() => Files.Paths();

    /// <summary>The method of this name that holds the code.</summary>
    public static Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax Method(string name) => Files.Method(name);

    /// <summary>The method of this name as a static method of a stand-in class, for a harness that compiles production bodies.</summary>
    public static string InstanceMethod(string name) => DMPossessionHarness.InstanceMethod(Files.Trees().Select(tree => tree.GetRoot()), name);
}

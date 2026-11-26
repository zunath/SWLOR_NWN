using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SWLOR.Game.Server.Tests.Service;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of a member of the Character and Companions services (<c>PerkService</c>, <c>SkillService</c>,
/// <c>PlayerNameService</c>) wherever it lives, for the tests that guard a rule by reading the code. The split
/// (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>) moves members into part files; each pull request that moves a
/// part adds its file to the <see cref="ServiceSource.PartFiles"/> of the service it leaves, and the interface of the part to that
/// service's interfaces. A test asks for a member by name (<see cref="ServiceSource.Member"/>, <see cref="Member"/>) or for the file
/// that declares it (<see cref="ServiceSource.RelativePath"/>), or reads the whole text of one service (<see cref="ServiceSource.Read"/>),
/// and keeps its needles. A member is found by its name, so a promoted modifier or a moved declaration changes no test.
/// <para>
/// As in <see cref="StatSource"/>, the text is as it is apart from one rewrite: a call through a field of a part interface of the same
/// original service (<c>_names.GetKnownNames(x)</c>) loses its receiver, because the code called that member on its own instance
/// before the move. A call on any other injected service keeps its receiver.
/// </para>
/// </summary>
public static class CharacterCompanionsSource
{
    /// <summary>The server project's prefix on every repository-relative path.</summary>
    private const string ServerPrefix = "SWLOR.Game.Server/";

    /// <summary>The perk service: <c>PerkService</c> and each part the split moves out.</summary>
    public static readonly ServiceSource Perk = new(
        "PerkService",
        new[]
        {
            "SWLOR.Game.Server/Domain/Character/PerkService.cs",
            "SWLOR.Game.Server/Domain/Character/PerkStatBonusService.cs",
            "SWLOR.Game.Server/Domain/Character/PerkForceAffinityService.cs",
            "SWLOR.Game.Server/Domain/Character/PerkGrantedFeatService.cs",
            "SWLOR.Game.Server/Domain/Character/PerkRefundService.cs",
        },
        new HashSet<string>(StringComparer.Ordinal)
        {
            "IPerkService", "IPerkStatBonusService", "IPerkForceAffinityService", "IPerkGrantedFeatService", "IPerkRefundService",
        });

    /// <summary>The skill service: <c>SkillService</c> and each part the split moves out.</summary>
    public static readonly ServiceSource Skill = new(
        "SkillService",
        new[]
        {
            "SWLOR.Game.Server/Domain/Character/SkillService.cs",
            "SWLOR.Game.Server/Domain/Character/SkillXPService.cs"
        },
        new HashSet<string>(StringComparer.Ordinal) { "ISkillService", "ISkillXPService" });

    /// <summary>The player name service: <c>PlayerNameService</c> and each part the split moves out.</summary>
    public static readonly ServiceSource PlayerName = new(
        "PlayerNameService",
        new[]
        {
            "SWLOR.Game.Server/Domain/Character/PlayerNameService.cs",
            "SWLOR.Game.Server/Domain/Character/PlayerKnownNameService.cs",
            "SWLOR.Game.Server/Domain/Character/PlayerNameOverrideService.cs"
        },
        new HashSet<string>(StringComparer.Ordinal) { "IPlayerNameService", "IPlayerKnownNameService", "IPlayerNameOverrideService" });

    /// <summary>The services this reader covers, in the order the map lists them.</summary>
    public static IReadOnlyList<ServiceSource> Services => new[] { Perk, Skill, PlayerName };

    /// <summary>The text of the method of this name in whichever service declares it (the longest, when it has overloads), attributes included.</summary>
    public static string Member(string name) => Owner(name).Member(name);

    /// <summary>The path, relative to the server project (<c>Domain/Character/PerkService.cs</c>), of the file that declares the method of this name.</summary>
    public static string RelativePath(string name) => Owner(name).RelativePath(name);

    private static ServiceSource Owner(string name)
    {
        var owners = Services.Where(service => service.Declares(name)).ToArray();
        if (owners.Length == 0)
            throw new InvalidOperationException($"No Character file declares a method named {name}.");
        return owners[0];
    }

    /// <summary>One original service: its files in reading order and the interfaces of its parts.</summary>
    public sealed class ServiceSource
    {
        private readonly PartSourceFiles _files;

        public ServiceSource(string service, IReadOnlyList<string> partFiles, IReadOnlySet<string> partInterfaces)
        {
            Service = service;
            PartFiles = partFiles;
            PartInterfaces = partInterfaces;
            _files = new PartSourceFiles(partFiles, partInterfaces);
        }

        /// <summary>The name of the original service.</summary>
        public string Service { get; }

        /// <summary>The files that hold the code of the service as it was before the split, repository-relative, the service itself first.</summary>
        public IReadOnlyList<string> PartFiles { get; }

        /// <summary>The interfaces of the service and its parts: a call through a field of one of these was a call on the same instance before the move.</summary>
        public IReadOnlySet<string> PartInterfaces { get; }

        /// <summary>The combined source of the service.</summary>
        public string Read() => _files.Read();

        /// <summary>The full paths of the files <see cref="Read"/> combines.</summary>
        public IReadOnlyList<string> Paths() => _files.Paths();

        /// <summary>One syntax tree per file.</summary>
        public IReadOnlyList<SyntaxTree> Trees() => _files.Trees();

        /// <summary>The method of this name that holds the code (the longest overload).</summary>
        public MethodDeclarationSyntax Method(string name) => _files.Method(name);

        /// <summary>The text of the method of this name, attributes included, in whichever part file declares it.</summary>
        public string Member(string name) => Method(name).ToString();

        /// <summary>The text of every method with one of these names (all overloads), in the order the names are given.</summary>
        public string Members(params string[] names) =>
            string.Join('\n', names.SelectMany(name => _files.Nodes<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.Text == name)).Select(method => method.ToString()));

        /// <summary>Whether a method of this name is declared in the service's files.</summary>
        public bool Declares(string name) => _files.Nodes<MethodDeclarationSyntax>().Any(method => method.Identifier.Text == name);

        /// <summary>
        /// The path, relative to the server project, of the file that declares the method of this name, for the harnesses that
        /// borrow a method by path.
        /// </summary>
        public string RelativePath(string name)
        {
            var method = Method(name);
            return PartFiles
                .Select(relative => relative[ServerPrefix.Length..])
                .First(relative => method.SyntaxTree.FilePath.Replace('\\', '/').EndsWith("/" + relative, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The method of this name as a static method of a stand-in class, for a harness that compiles production bodies.</summary>
        public string InstanceMethod(string name) =>
            DMPossessionHarness.InstanceMethod(_files.Trees().Select(tree => tree.GetRoot()), name);
    }
}

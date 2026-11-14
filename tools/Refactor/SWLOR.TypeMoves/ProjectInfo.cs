using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SWLOR.TypeMoves;

/// <summary>What the tool reads from one project file to compile the project in memory.</summary>
/// <param name="ProjectFile">Full path of the <c>.csproj</c>.</param>
/// <param name="AssemblyName">The assembly name (it decides <c>InternalsVisibleTo</c>).</param>
/// <param name="OutputKind">Library or console application.</param>
/// <param name="LanguageVersion">The project's C# version.</param>
/// <param name="ProjectReferences">Full paths of the referenced project files inside the repository.</param>
/// <param name="SourceFiles">Full paths of the compiled <c>.cs</c> files, in ordinal order.</param>
/// <param name="GeneratedSource">
/// The source MSBuild would generate: the global usings (from the build's own file under <c>obj/</c>, else from
/// <c>ImplicitUsings</c> and <c>Using</c> items) and the <c>InternalsVisibleTo</c> items.
/// </param>
/// <param name="Web">True for a <c>Microsoft.NET.Sdk.Web</c> project (it compiles against ASP.NET Core).</param>
internal sealed record ProjectInfo(
    string ProjectFile,
    string AssemblyName,
    OutputKind OutputKind,
    LanguageVersion LanguageVersion,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> SourceFiles,
    string GeneratedSource,
    bool Web)
{
    public string Folder => Path.GetDirectoryName(ProjectFile)!;

    public CSharpParseOptions ParseOptions => new(LanguageVersion, DocumentationMode.None);
}

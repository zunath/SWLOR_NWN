using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceEditing;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The appearance editor was split toward the Phase 5 limits (P5-16e): 1,000 lines and 10 services per type, 150 lines per method.
/// The view model is one file and does not grow; its work lives in the services of Feature/AppearanceEditing.
/// </summary>
public class AppearanceEditorSizeTests
{
    private const int MaximumTypeLines = 1000;
    private const int MaximumServices = 10;

    // The view model's bindings alone (745 lines of declared properties) leave it above MaximumTypeLines once everything that can
    // move has moved; its measured size is held here until the owner decides (MaintainabilityRefactorAppearanceEditorSplit.md, section 9).
    private const int MaximumViewModelLines = 1337;
    private const int MaximumMethodLines = 150;

    [Test]
    public void TheViewModelIsOneFileUnderTheLineLimit()
    {
        var directory = Path.Combine(RepositoryRoot(), "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel");
        var files = Directory.GetFiles(directory, "AppearanceEditorViewModel*.cs");
        files.Select(Path.GetFileName).Should().ContainSingle().Which.Should().Be("AppearanceEditorViewModel.cs",
            "the view model is a single type in a single file, with no partial parts");
        File.ReadAllLines(files[0]).Length.Should().BeLessThanOrEqualTo(MaximumViewModelLines);
    }

    [Test]
    public void EveryEditorTypeStaysUnderTheLineAndMethodLimits()
    {
        foreach (var path in EditorSources())
        {
            var lines = File.ReadAllLines(path);
            lines.Length.Should().BeLessThanOrEqualTo(
                Path.GetFileName(path) == "AppearanceEditorViewModel.cs" ? MaximumViewModelLines : MaximumTypeLines, Path.GetFileName(path));
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
            foreach (var method in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
            {
                var span = method.GetLocation().GetLineSpan();
                (span.EndLinePosition.Line - span.StartLinePosition.Line + 1).Should().BeLessThanOrEqualTo(MaximumMethodLines,
                    $"{Path.GetFileName(path)}: {method.ToString().Split('\n')[0].Trim()}");
            }
        }
    }

    [Test]
    public void NoEditorTypeTakesMoreThanTenServices()
    {
        var types = typeof(AppearanceEditorViewModel).Assembly.GetTypes()
            .Where(type => type.Namespace == "SWLOR.Game.Server.Feature.AppearanceEditing" && type.IsClass && !type.IsAbstract &&
                           !type.IsNested)
            .Append(typeof(AppearanceEditorViewModel))
            .Append(typeof(AppearanceItemEligibility))
            .Append(typeof(WeaponAppearanceCatalog));
        foreach (var type in types)
        {
            foreach (var constructor in type.GetConstructors())
                constructor.GetParameters().Length.Should().BeLessThanOrEqualTo(MaximumServices, type.Name);
        }
    }

    private static IEnumerable<string> EditorSources()
    {
        var feature = Path.Combine(RepositoryRoot(), "SWLOR.Game.Server", "Feature");
        return Directory.GetFiles(Path.Combine(feature, "AppearanceEditing"), "*.cs")
            .Append(Path.Combine(feature, "GuiDefinition", "ViewModel", "AppearanceEditorViewModel.cs"))
            .Append(Path.Combine(feature, "AppearanceDefinition", "ItemAppearance", "AppearanceItemEligibility.cs"))
            .Append(Path.Combine(feature, "AppearanceDefinition", "ItemAppearance", "WeaponAppearanceCatalog.cs"));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        return directory!.FullName;
    }
}

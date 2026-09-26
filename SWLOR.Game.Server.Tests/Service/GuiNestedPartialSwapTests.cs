using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A plain ChangePartialView on a nested element can be dropped by NUI mid-redraw, which leaves
/// the content area blank (reported on the Settings Identity and Chat tabs). Nested swaps must go
/// through SwapNestedPartialView (directly or via GuiTabGroup), which redraws the root and
/// re-applies the partial on the next tick.
/// </summary>
[TestFixture]
public class GuiNestedPartialSwapTests
{
    private static readonly Regex ChangePartialViewCall = new(@"\bChangePartialView\(\s*([^,]+?)\s*,", RegexOptions.Compiled);

    // Root swaps are unaffected: the window root is bound to the window geometry.
    private static readonly HashSet<string> RootElementArguments = new()
    {
        "\"_window_\"",
        "MainWindowElement"
    };

    private static readonly HashSet<(string File, string Argument)> AllowedNestedSwaps = new()
    {
        // Palette slot nested inside the equipment tab partial. It is applied from the tab's
        // SwapNestedPartialView callback; a root redraw here would wipe the parent tab.
        ("AppearanceEditorViewModel.cs", "ArmorColorElement"),
        // The gallery deliberately exercises raw swaps as hazard exhibits.
        ("DebugNuiGalleryViewModel.cs", "HazardSlotElement"),
        ("DebugNuiGalleryViewModel.cs", "\"no_such_element_id\""),
        ("DebugNuiGalleryViewModel.cs", "ProbeNestedSlotElement")
    };

    [Test]
    public void ViewModelsDoNotSwapNestedPartialsWithPlainChangePartialView()
    {
        var viewModelDirectory = Path.Combine(FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel");
        var violations = new List<string>();

        foreach (var file in Directory.GetFiles(viewModelDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            foreach (Match match in ChangePartialViewCall.Matches(File.ReadAllText(file)))
            {
                var argument = match.Groups[1].Value;
                if (RootElementArguments.Contains(argument) || AllowedNestedSwaps.Contains((fileName, argument)))
                    continue;

                violations.Add($"{fileName}: ChangePartialView({argument}, ...)");
            }
        }

        violations.Should().BeEmpty(
            "nested partial swaps must use SwapNestedPartialView (or GuiTabGroup) so NUI cannot drop them");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the tests run from inside the repository");
        return directory!;
    }
}

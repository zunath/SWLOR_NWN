using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PropertyPermissionsWindowTests
{
    [Test]
    public void PermissionList_PagesPlayersInsteadOfStoppingAtTheDefaultSearchLimit()
    {
        var viewModel = ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "PropertyPermissionsViewModel.cs");

        // An unpaged DB.Search returns at most 50 rows, which silently dropped players past the 50th.
        var permissionIds = ExtractMethod(viewModel, "private List<string> GetPermissionPlayerIds()");
        permissionIds.Should().Contain("DB.SearchCount(query)");
        permissionIds.Should().Contain("query.AddPaging(permissionCount, 0)");

        var playerList = ExtractMethod(viewModel, "private IEnumerable<Player> SearchPlayersWithPermissions()");
        playerList.Should().Contain("UpdatePagination(DB.SearchCount(query))");
        playerList.Should().Contain("query.AddPaging(PlayersPerPage, SelectedPageIndex * PlayersPerPage)");
        playerList.Should().Contain(".OrderBy(nameof(Entity.Player.Name))", "pages need a stable order");

        var search = ExtractMethod(viewModel, "private void Search()");
        search.Should().NotContain("DB.Search(", "every listing query goes through a paged helper");
    }

    [Test]
    public void PermissionLayout_ShowsPaginationUnderThePlayerList()
    {
        var definition = ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "PropertyPermissionsDefinition.cs");

        definition.Should().MatchRegex(
            @"\.BindRowCount\(model => model\.PlayerNames\);\s*\}\);\s*col2\.AddPagination\(\s*model => model\.PageNumbers,\s*model => model\.SelectedPageIndex,\s*model => model\.OnClickPreviousPage\(\),\s*model => model\.OnClickNextPage\(\)\);");
    }

    [Test]
    public void PermissionWindow_IsWideEnoughForThePageSelector()
    {
        var definition = ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "PropertyPermissionsDefinition.cs");
        var viewModel = ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "PropertyPermissionsViewModel.cs");

        // At the old 545 width the page selector widened the player column and clipped the Grant
        // column in the real client. 620x330 was verified to fit both columns.
        definition.Should().Contain("internal const float WindowWidth = 620f;");
        definition.Should().Contain("internal const float WindowHeight = 330f;");
        definition.Should().Contain(".SetInitialGeometry(0, 0, WindowWidth, WindowHeight)");

        // Saved geometry overrides the initial size, so older smaller sizes are enlarged on open.
        var initialize = ExtractMethod(viewModel, "protected override void Initialize(PropertyPermissionPayload initialPayload)");
        initialize.Should().Contain("EnsureUsableWindowGeometry();");
        initialize.Should().Contain("DelayCommand(0.0f, EnsureUsableWindowGeometry);");
        var ensure = ExtractMethod(viewModel, "private void EnsureUsableWindowGeometry()");
        ensure.Should().Contain("Math.Max(current.Width, PropertyPermissionsDefinition.WindowWidth)");
        ensure.Should().Contain("Math.Max(current.Height, PropertyPermissionsDefinition.WindowHeight)");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"method '{signature}' should exist");

        var rest = source[start..];
        var next = Regex.Match(
            rest[signature.Length..],
            @"\n        (private|public|protected|internal|static)\b",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        return next.Success ? rest[..(signature.Length + next.Index)] : rest;
    }

    private static string ReadSource(params string[] relativePath)
    {
        var segments = new[] { FindRepositoryRoot().FullName }.Concat(relativePath).ToArray();

        return File.ReadAllText(Path.Combine(segments));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}

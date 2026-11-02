using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class RecipesWindowCloseTests
{
    [Test]
    public void TheRecipesWindowRunsItsCloseHandlerSoAQuickCraftBatchEnds()
    {
        var definition = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Feature", "GuiDefinition", "RecipesDefinition.cs");

        File.ReadAllText(definition).Should().Contain(".BindOnClosed(model => model.OnWindowClosed())");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}

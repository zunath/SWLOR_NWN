using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class StablesWindowTests
{
    [Test]
    public void StablingChanges_RefreshTheVisibleCapacityWithoutReloadingTheBeastList()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "StablesViewModel.cs"));
        var purchaseRefresh = ExtractMethod(source, "public void Refresh(PerkAcquiredRefreshEvent payload)");
        var refundRefresh = ExtractMethod(source, "public void Refresh(PerkRefundedRefreshEvent payload)");

        source.Should().Contain("IGuiRefreshable<PerkAcquiredRefreshEvent>");
        source.Should().Contain("IGuiRefreshable<PerkRefundedRefreshEvent>");
        source.Should().Contain("BeastCount = $\"Beasts: {beastCount} / {capacity}\";");

        foreach (var refreshMethod in new[] { purchaseRefresh, refundRefresh })
        {
            refreshMethod.Should().Contain("if (payload.Type != PerkType.Stabling)");
            refreshMethod.Should().Contain("RefreshBeastCount(_beastIds.Count);");
            refreshMethod.Should().NotContain("LoadBeasts();");
        }
    }

    [Test]
    public void BeastHpDisplay_UsesTheFinalLevelBudgetWithoutReapplyingVitality()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "StablesViewModel.cs"));

        source.Should().Contain("HP = $\"{level.HP}\";");
        source.Should().NotContain("level.HP +");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        openBraceIndex.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null &&
               !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root containing SWLOR.Game.Server.sln must be discoverable");
        return directory;
    }
}

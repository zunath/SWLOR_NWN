using FluentAssertions;
using Nwn.Hosting.Contracts.Generations;
using NUnit.Framework;
using SWLOR.Game.Host.Bootstrap;
using SWLOR.Game.Host.Reload;

namespace SWLOR.Game.Host.Tests.Reload;

/// <summary>
/// Where a development reload's files are: candidates under the host's <c>generations/</c> as the hosting package's request
/// convention puts them, the signal and one receipt per candidate in the server home's <c>reload/</c>.
/// </summary>
[TestFixture]
public class ReloadLayoutTests
{
    private static readonly string Home = Path.Combine(Path.GetTempPath(), "nwn", "home");
    private static readonly Guid Id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    [Test]
    public void A_server_homes_layout_is_the_one_the_host_reads_by_default()
    {
        var layout = ReloadLayout.ForServerHome(Home);
        var settings = HostSettings.Read(_ => null, Path.Combine(Home, "dotnet"));

        layout.GenerationRoot.Should().Be(settings.GenerationRoot);
        layout.ReloadDirectory.Should().Be(settings.ReloadDirectory);
    }

    [Test]
    public void Each_candidate_has_its_own_folder_under_the_host_and_its_own_receipt()
    {
        var layout = ReloadLayout.ForServerHome(Home);
        var idText = "0f8fad5bd9cb469fa16570867728950e";

        layout.GenerationsDirectory.Should().Be(Path.Combine(Home, "dotnet", "generations"));
        layout.CandidateDirectory(Id).Should().Be(Path.Combine(Home, "dotnet", "generations", idText));
        layout.CandidateAssembly(Id).Should().Be(Path.Combine(Home, "dotnet", "generations", idText, "SWLOR.Game.Server.dll"));
        layout.CandidateAssembly(Id).Should().Be(Path.Combine(layout.GenerationRoot,
            new DevelopmentReloadRequest(DevelopmentReloadFiles.CurrentFormatVersion, Id).GetAssemblyRelativePath("SWLOR.Game.Server.dll")),
            "the candidate is where the hosting package's request convention puts it");
        layout.SignalPath.Should().Be(Path.Combine(Home, "reload", "signal.json"));
        layout.ReceiptsDirectory.Should().Be(Path.Combine(Home, "reload", "receipts"));
        layout.ReceiptPath(Id).Should().Be(Path.Combine(Home, "reload", "receipts", idText + ".json"));
        ReloadLayout.IdText(Id).Should().Be(idText);
    }

    [Test]
    public void A_layout_needs_both_folders()
    {
        var withoutRoot = () => new ReloadLayout(" ", Home);
        var withoutReload = () => new ReloadLayout(Home, null!);
        var withoutHome = () => ReloadLayout.ForServerHome("");

        withoutRoot.Should().Throw<ArgumentException>();
        withoutReload.Should().Throw<ArgumentException>();
        withoutHome.Should().Throw<ArgumentException>();
    }
}

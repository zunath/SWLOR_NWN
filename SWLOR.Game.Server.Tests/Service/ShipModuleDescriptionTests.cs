using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class ShipModuleDescriptionTests
{
    private const string Module = "Deals 18 thermal DMG to your target.";

    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void UninstallFacades() => _facadeScope.Dispose();

    [Test]
    public void BaseItemPlaceholder_IsReplacedByTheModuleDescription()
    {
        Space.BuildShipModuleDescription("Electronic Components 1", "Electronic Components 1", Module)
            .Should().Be(Module);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  \n")]
    public void EmptyDescription_IsTheModuleDescription(string original)
    {
        Space.BuildShipModuleDescription(original, "Electronic Components 1", Module).Should().Be(Module);
    }

    [Test]
    public void AuthoredDescription_KeepsItsTextOnItsOwnLine()
    {
        Space.BuildShipModuleDescription("A salvaged laser.\n", "Electronic Components 1", Module)
            .Should().Be("A salvaged laser.\n" + Module);
    }
}

using NUnit.Framework;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>
/// Base of the characterization fixtures that call a converted service through its static facade (<c>Skill</c>): it
/// builds the server's registrations once per fixture and installs them for each test.
/// </summary>
public abstract class FacadeFixture
{
    private FacadeServices _facades = null!;
    private IDisposable _facadeScope = null!;

    protected FacadeServices Facades => _facades;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeServices();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();
}

using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Base of the fixtures whose code reaches the static <c>Skill</c> facade (a recipe definition reading a skill's
/// details, an XP award asking for the chart) without being a test of the skill service: it builds the server's
/// registrations once per fixture and installs them for each test. A fixture that must call a facade in its own
/// <c>[OneTimeSetUp]</c> does it inside <c>using (SkillFacades.Install())</c>.
/// </summary>
public abstract class SkillFacadeFixture
{
    private IDisposable _skillFacadeScope = null!;

    /// <summary>The container the facades resolve from during this fixture.</summary>
    protected FacadeContainer SkillFacades { get; private set; } = null!;

    /// <summary>
    /// Runs <paramref name="build"/> in a container of its own, for a <c>[TestCaseSource]</c> that builds definitions
    /// reading the facades: the sources run while NUnit discovers the tests, before any fixture has installed one.
    /// The result must be materialised (a list or an array), not a lazy sequence.
    /// </summary>
    protected static T WithFacades<T>(Func<T> build)
    {
        using var container = new FacadeContainer();
        using (container.Install())
            return build();
    }

    [OneTimeSetUp]
    public void BuildSkillFacades() => SkillFacades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeSkillFacades() => SkillFacades.Dispose();

    [SetUp]
    public void InstallSkillFacades() => _skillFacadeScope = SkillFacades.Install();

    [TearDown]
    public void RemoveSkillFacades() => _skillFacadeScope.Dispose();
}

using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanAccessLockTests
{
    // The closed planets belong to the planet service, which the facade resolves from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [Test]
    public void Eshan_IsClosedWithAsciiMessage()
    {
        Assert.That(Planet.TryGetClosedMessage(PlanetType.Eshan, out var message), Is.True);
        Assert.That(message, Is.Not.Empty);
        Assert.That(message.All(character => character < 128), Is.True);
    }

    [Test]
    public void OtherPlanets_AreOpen()
    {
        foreach (var type in Enum.GetValues<PlanetType>().Where(type => type != PlanetType.Eshan))
        {
            Assert.That(Planet.TryGetClosedMessage(type, out var message), Is.False, type.ToString());
            Assert.That(message, Is.Empty);
        }
    }

    [Test]
    public void DantooineStarlaneToEshan_IsFlaggedWithEshanPlanet()
    {
        using var document = Read("Module/git/dantooineorbit.git.json");
        var starlanes = Value(document.RootElement, "Placeable List").EnumerateArray()
            .Where(item => Value(item, "OnUsed").GetString() == "teleport" &&
                           Local(item, "DESTINATION")?.GetString() == "STARLANE_LANDING_ESHAN_1")
            .ToArray();

        Assert.That(starlanes, Has.Length.EqualTo(1));
        Assert.That(Local(starlanes[0], "DESTINATION_PLANET_ID")?.GetInt32(), Is.EqualTo((int)PlanetType.Eshan));
    }

    private static JsonElement? Local(JsonElement item, string name)
    {
        foreach (var variable in Value(item, "VarTable").EnumerateArray())
        {
            if (Value(variable, "Name").GetString() == name)
                return Value(variable, "Value");
        }

        return null;
    }

    private static JsonElement Value(JsonElement element, string name) => element.GetProperty(name).GetProperty("value");

    private static JsonDocument Read(string path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, path), System.Text.Encoding.Latin1));
    }
}

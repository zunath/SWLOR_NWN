using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>Unit tests of the crafting device and the crafting handlers of the property and login scripts: the engine is a recording fake.</summary>
[TestFixture]
public class CraftDeviceServiceTests
{
    private const string JobId = "job-1";

    private readonly List<IDisposable> _scopes = new();
    private readonly InMemoryDBService _db = new();
    private FacadeContainer _facades = null!;
    private PlayerEngine _engine = null!;
    private FakeEventsPlugin _events = null!;
    private ScriptedProxy _guiScript = null!;
    private ScriptedProxy _propertyScript = null!;
    private ScriptedProxy _experimentScript = null!;
    private ScriptedProxy _styleScript = null!;
    private CraftDeviceService _service = null!;

    [OneTimeSetUp]
    public void BuildTheContainer()
    {
        // The research tier of a player is read through the Skill facade, from the container's database: the one of this fixture.
        _facades = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton<IDBService>(_db)));
    }

    [OneTimeTearDown]
    public void DisposeTheContainer() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _scopes.Add(_facades.Install());
        _engine = new PlayerEngine();
        _events = new FakeEventsPlugin();
        _scopes.Add(EventsPluginRuntime.Override(_events));
        _service = new CraftDeviceService(
            _db,
            ScriptedProxy.Create<IGuiService>(out _guiScript),
            ScriptedProxy.Create<IPropertyService>(out _propertyScript),
            ScriptedProxy.Create<ICraftResearchExperimentService>(out _experimentScript),
            ScriptedProxy.Create<ICraftStyleEffectsService>(out _styleScript));
        _guiScript.On("TogglePlayerWindow", args => null);
    }

    [TearDown]
    public void TearDown()
    {
        // The in-memory database answers every search with every job it holds, so a test removes its job.
        _db.Delete<ResearchJob>(JobId);
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
        _engine.Dispose();
    }

    private void GivePlayerFabricationRank(int rank)
    {
        var player = new Player { Id = PlayerEngine.PlayerId };
        player.Skills[SkillType.Fabrication] = new PlayerSkill { Rank = rank };
        _db.With(player);
    }

    private GuiWindowType OnlyWindowOpened() =>
        (GuiWindowType)_guiScript.Calls.Should().ContainSingle(call => call.Member == "TogglePlayerWindow").Subject.Arguments[1]!;

    [Test]
    public void UseRefinery_opens_the_refinery_window()
    {
        _service.UseRefinery();

        OnlyWindowOpened().Should().Be(GuiWindowType.Refinery);
    }

    [Test]
    public void UseCraftingDevice_opens_the_recipes_window_for_the_skill_of_the_device()
    {
        _service.UseCraftingDevice();

        OnlyWindowOpened().Should().Be(GuiWindowType.Recipes);
    }

    [Test]
    public void UseResearchTerminal_asks_for_fabrication_rank_before_anything_else()
    {
        GivePlayerFabricationRank(0);

        _service.UseResearchTerminal();

        _guiScript.Calls.Should().BeEmpty();
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("is required to use research terminals");
    }

    [Test]
    public void UseResearchTerminal_refuses_a_terminal_that_is_not_part_of_a_property()
    {
        GivePlayerFabricationRank(100);
        _propertyScript.On("GetPropertyId", args => string.Empty);

        _service.UseResearchTerminal();

        _guiScript.Calls.Should().BeEmpty();
        _engine.Messages().Should().ContainSingle().Which.Should().Be("This research terminal cannot be used.");
    }

    [Test]
    public void UseResearchTerminal_opens_the_research_recipes_when_no_job_is_running()
    {
        GivePlayerFabricationRank(100);
        _propertyScript.On("GetPropertyId", args => "prop-free");

        _service.UseResearchTerminal();

        OnlyWindowOpened().Should().Be(GuiWindowType.Recipes);
    }

    [Test]
    public void UseResearchTerminal_opens_the_research_window_for_the_owner_of_the_running_job()
    {
        GivePlayerFabricationRank(100);
        _propertyScript.On("GetPropertyId", args => "prop-own");
        _db.With(new ResearchJob { Id = JobId, ParentPropertyId = "prop-own", PlayerId = PlayerEngine.PlayerId });

        _service.UseResearchTerminal();

        OnlyWindowOpened().Should().Be(GuiWindowType.Research);
    }

    [Test]
    public void UseResearchTerminal_tells_another_player_when_the_job_will_complete()
    {
        GivePlayerFabricationRank(100);
        _propertyScript.On("GetPropertyId", args => "prop-other");
        _db.With(new ResearchJob { Id = JobId, ParentPropertyId = "prop-other", PlayerId = "someone-else", DateCompleted = DateTime.UtcNow.AddHours(2) });

        _service.UseResearchTerminal();

        _guiScript.Calls.Should().BeEmpty();
        _engine.Messages().Should().ContainSingle().Which.Should().StartWith("Another player's research job is active. This job will complete in:");
    }

    [Test]
    public void UseResearchTerminal_tells_another_player_when_the_job_has_completed()
    {
        GivePlayerFabricationRank(100);
        _propertyScript.On("GetPropertyId", args => "prop-done");
        _db.With(new ResearchJob { Id = JobId, ParentPropertyId = "prop-done", PlayerId = "someone-else", DateCompleted = DateTime.UtcNow.AddHours(-2) });

        _service.UseResearchTerminal();

        _engine.Messages().Should().ContainSingle().Which.Should().Be("Another player's research job is active. This job has completed.");
    }

    [Test]
    public void OnRemoveProperty_does_nothing_when_the_property_has_no_research_job()
    {
        _events.SetReturn("GetEventData", "prop-1");
        var queries = _db.Queries.Count;

        _service.OnRemoveProperty();

        _db.Queries.Count.Should().Be(queries + 1);
        _experimentScript.Calls.Should().BeEmpty();
    }

    [Test]
    public void OnRemoveProperty_deletes_the_research_job_of_the_removed_property()
    {
        _events.SetReturn("GetEventData", "prop-gone");
        _db.With(new ResearchJob { Id = JobId, ParentPropertyId = "prop-gone" });
        _experimentScript.On("DeleteJob", args => null);

        _service.OnRemoveProperty();

        _experimentScript.Calls.Should().ContainSingle(call => call.Member == "DeleteJob");
    }

    [Test]
    public void RestoreCraftingStylesOnEnter_applies_the_styles_of_the_entering_player()
    {
        _styleScript.On("Restore", args => null);

        _service.RestoreCraftingStylesOnEnter();

        _styleScript.Calls.Should().ContainSingle(call => call.Member == "Restore");
    }
}

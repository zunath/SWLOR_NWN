using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// The fake engine: NWScript and every NWNX plugin seam, installed for the length of a test. Game code that reaches the
/// engine records its calls and gets the configured answer or an empty default. Nothing here touches the database or the
/// container.
/// </summary>
public sealed class FakeEngineSet : IDisposable
{
    private readonly List<IDisposable> _scopes = new();

    /// <summary>The NWScript fake.</summary>
    public FakeNWScript Script { get; } = new();

    public FakeAdministrationPlugin Administration { get; } = new();
    public FakeAreaPlugin Area { get; } = new();
    public FakeChatPlugin Chat { get; } = new();
    public FakeCreaturePlugin Creature { get; } = new();
    public FakeEventsPlugin Events { get; } = new();
    public FakeFeatPlugin Feat { get; } = new();
    public FakeFeedbackPlugin Feedback { get; } = new();
    public FakeItemPlugin Item { get; } = new();
    public FakeItemPropertyPlugin ItemProperty { get; } = new();
    public FakeObjectPlugin Object { get; } = new();
    public FakePlayerPlugin Player { get; } = new();
    public FakeProfilerPlugin Profiler { get; } = new();
    public FakeRenamePlugin Rename { get; } = new();
    public FakeUtilPlugin Util { get; } = new();
    public FakeVisibilityPlugin Visibility { get; } = new();
    public FakeWeaponPlugin Weapon { get; } = new();

    public FakeEngineSet()
    {
        _scopes.Add(NWScriptRuntime.Override(Script));
        _scopes.Add(AdministrationPluginRuntime.Override(Administration));
        _scopes.Add(AreaPluginRuntime.Override(Area));
        _scopes.Add(ChatPluginRuntime.Override(Chat));
        _scopes.Add(CreaturePluginRuntime.Override(Creature));
        _scopes.Add(EventsPluginRuntime.Override(Events));
        _scopes.Add(FeatPluginRuntime.Override(Feat));
        _scopes.Add(FeedbackPluginRuntime.Override(Feedback));
        _scopes.Add(ItemPluginRuntime.Override(Item));
        _scopes.Add(ItemPropertyPluginRuntime.Override(ItemProperty));
        _scopes.Add(ObjectPluginRuntime.Override(Object));
        _scopes.Add(PlayerPluginRuntime.Override(Player));
        _scopes.Add(ProfilerPluginRuntime.Override(Profiler));
        _scopes.Add(RenamePluginRuntime.Override(Rename));
        _scopes.Add(UtilPluginRuntime.Override(Util));
        _scopes.Add(VisibilityPluginRuntime.Override(Visibility));
        _scopes.Add(WeaponPluginRuntime.Override(Weapon));
    }

    public void Dispose()
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
            _scopes[i].Dispose();
    }
}

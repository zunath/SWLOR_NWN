using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Installs a recording NWScript fake for the length of a test in which the object under test is a player
/// (not a DM) whose UUID is <see cref="PlayerId"/>. Tests change what the engine answers with
/// <c>Engine.SetReturn</c> and read what the code sent to the player with <see cref="Messages"/>.
/// </summary>
public sealed class PlayerEngine : IDisposable
{
    public const string PlayerId = "player-uuid-1";

    private readonly IDisposable _scope;

    public PlayerEngine()
    {
        Engine = new FakeNWScript();
        Engine.SetReturn("GetIsPC", true);
        Engine.SetReturn("GetIsDM", false);
        Engine.SetReturn("GetObjectUUID", PlayerId);
        _scope = NWScriptRuntime.Override(Engine);
    }

    /// <summary>The fake engine behind the static NWScript wrappers.</summary>
    public FakeNWScript Engine { get; }

    /// <summary>The text of every <c>SendMessageToPC</c> call, in order.</summary>
    public IReadOnlyList<string> Messages() =>
        Engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    public void Dispose() => _scope.Dispose();
}

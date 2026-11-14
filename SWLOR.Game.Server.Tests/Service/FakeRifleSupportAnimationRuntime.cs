using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Tests.Service;

internal sealed class FakeRifleSupportAnimationRuntime : IRifleSupportAnimationRuntime
{
    internal bool Valid = true, Temporary, FailSchedule;
    internal int ClearCalls;
    internal readonly Dictionary<string, string> Maps = new();
    internal readonly List<Action> Callbacks = new();
    public bool IsValid(uint creature) => Valid;
    public bool IsTemporaryPlaybackActive(uint creature) => Temporary;
    public string Replacement(uint creature, string source) => Maps.GetValueOrDefault(source, "");
    public void Replace(uint creature, string source, string replacement) => Maps[source] = replacement;
    public void ClearHold(uint creature) => ClearCalls++;
    public void Schedule(float seconds, Action callback)
    {
        if (FailSchedule) throw new InvalidOperationException("Schedule failed");
        Callbacks.Add(callback);
    }
}

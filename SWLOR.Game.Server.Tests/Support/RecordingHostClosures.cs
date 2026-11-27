namespace SWLOR.Game.Server.Tests.Support;

/// <summary>A host closure handler that records what the game schedules instead of asking NWNX.</summary>
public sealed class RecordingHostClosures : global::NWN.Core.ICoreFunctionHandler
{
    public uint ObjectSelf { get; set; } = 0x7F000000;

    public List<(string Kind, uint Object, float Duration, Action Closure)> Scheduled { get; } = new();

    public void ClosureAssignCommand(uint obj, Action func) => Scheduled.Add(("AssignCommand", obj, 0f, func));

    public void ClosureDelayCommand(uint obj, float duration, Action func) => Scheduled.Add(("DelayCommand", obj, duration, func));

    public void ClosureActionDoCommand(uint obj, Action func) => Scheduled.Add(("ActionDoCommand", obj, 0f, func));
}

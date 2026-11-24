#nullable enable
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The session core with the frame, the clock in milliseconds and the log answered by the test.</summary>
internal sealed class RecorderProbeCore : CombatRecordSessionCore
{
    public List<string> WriteLines { get; } = new();
    public ulong Frame { get; set; }
    public long Millis { get; set; } = 1_700_000_000_000;

    public override void WriteLog(LogGroup group, string details, bool printToConsole = false) => WriteLines.Add(details);
    public override ulong CurrentFrame() => Frame;
    public override long UnixTimeMilliseconds() => Millis;
}

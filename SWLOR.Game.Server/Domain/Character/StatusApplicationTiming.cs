using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// How long one application of a status effect lasts, as the timing step works it out and the later steps read it: the duration
    /// in seconds, the tick length, the ticks and the ticks before resistance, and the resistance type the effect is assigned.
    /// </summary>
    public readonly record struct StatusApplicationTiming(
        float DurationSeconds,
        float Frequency,
        int DurationTicks,
        int DurationTicksWithoutResistance,
        ResistanceType ResistanceType);
}

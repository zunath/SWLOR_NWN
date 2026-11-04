namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// One possible result of completing an incubation job and its chance in percent.
    /// </summary>
    public readonly record struct IncubationOutcome(BeastType Type, float Percent);
}

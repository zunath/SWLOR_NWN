using System.Numerics;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Builds performer snapshots for the audience tests without touching the engine.</summary>
internal static class PerformanceTestSnapshots
{
    public static readonly DateTime Start = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    public static PerformerSnapshot Dancer(
        uint id,
        int xpScaled = 6,
        PerformanceRoutineType routine = PerformanceRoutineType.CantinaShuffle,
        PerformanceRoutineType secondRoutine = PerformanceRoutineType.CantinaShuffle,
        float x = 0f,
        int minutesAgo = 0,
        int venue = 0,
        bool hasCapstone = false,
        bool hasPartnerTrait = false,
        bool hasStandingOvation = false,
        int lightFeetSpeedPercent = 0,
        int routineAttributeBonus = 0,
        int crowdPleaserPercent = 0,
        int debtClearPercent = 0,
        int capBonusMinutes = 0,
        PerformanceQuality quality = PerformanceQuality.Steady)
    {
        return new PerformerSnapshot
        {
            Performer = id,
            Kind = PerformanceKind.Dance,
            StartedUtc = Start.AddMinutes(-minutesAgo),
            Position = new Vector3(x, 0f, 0f),
            XpBase = xpScaled,
            XpScaled = xpScaled,
            Routine = routine,
            SecondRoutine = secondRoutine,
            Venue = venue,
            HasCapstone = hasCapstone,
            HasPartnerTrait = hasPartnerTrait,
            HasStandingOvation = hasStandingOvation,
            LightFeetSpeedPercent = lightFeetSpeedPercent,
            RoutineAttributeBonus = routineAttributeBonus,
            BankingBonusPercent = crowdPleaserPercent,
            DebtClearPercent = debtClearPercent,
            CapBonusMinutes = capBonusMinutes,
            Quality = quality
        };
    }

    public static PerformerSnapshot Musician(
        uint id,
        int xpScaled = 6,
        PerformanceSetType set = PerformanceSetType.CantinaStandards,
        PerformanceSetType secondSet = PerformanceSetType.CantinaStandards,
        float x = 0f,
        int minutesAgo = 0,
        int song = 22,
        int venue = 0,
        bool hasCapstone = false,
        bool hasPartnerTrait = false,
        bool hasRequiem = false,
        int steadyHandsBonus = 0,
        int anglersLullabyBonus = 0,
        int setAttributeBonus = 0,
        int harmonyPercent = 0,
        int debtClearPercent = 0,
        PerformanceQuality quality = PerformanceQuality.Steady)
    {
        return new PerformerSnapshot
        {
            Performer = id,
            Kind = PerformanceKind.Music,
            StartedUtc = Start.AddMinutes(-minutesAgo),
            Position = new Vector3(x, 0f, 0f),
            XpBase = xpScaled,
            XpScaled = xpScaled,
            Set = set,
            SecondSet = secondSet,
            SongTrackId = song,
            Venue = venue,
            HasCapstone = hasCapstone,
            HasPartnerTrait = hasPartnerTrait,
            HasRequiem = hasRequiem,
            SteadyHandsBonus = steadyHandsBonus,
            AnglersLullabyBonus = anglersLullabyBonus,
            SetAttributeBonus = setAttributeBonus,
            BankingBonusPercent = harmonyPercent,
            DebtClearPercent = debtClearPercent,
            Quality = quality
        };
    }
}

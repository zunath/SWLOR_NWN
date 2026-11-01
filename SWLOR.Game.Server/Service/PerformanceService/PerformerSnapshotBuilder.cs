using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Reads a live performing session into a snapshot: the performer's perk-granted Performance stats, their unlocks
    /// and their venue, once per audience tick. The bonuses come from the stats the perks grant; perk levels are only
    /// read for unlocks (traits and capstones that grant no stat).
    /// </summary>
    public static class PerformerSnapshotBuilder
    {
        public static PerformerSnapshot Build(PerformanceSession session, int venue, int songTrackId)
        {
            var performer = session.Performer;
            var isDancer = session.Kind == PerformanceKind.Dance;
            var preference = Performance.GetPreference(performer);
            var hasCapstone = Perk.GetPerkLevel(performer, isDancer ? PerkType.Showstopper : PerkType.Virtuoso) > 0;

            // The perk's rank value is the stat; the maximum SOC can scale it to follows from that rank.
            var xpBase = Math.Max(0, Stat.GetStatAdjustment(
                performer, isDancer ? StatType.PerformanceDanceXPBonus : StatType.PerformanceMusicXPBonus));
            var xpScaled = AbilityEffectScaling.ScaleValueBySourceSocial(
                performer, xpBase, PerformanceAudienceRules.XpBonusMaximum(xpBase));

            return new PerformerSnapshot
            {
                Performer = performer,
                Kind = session.Kind,
                StartedUtc = session.StartedUtc,
                Quality = session.Quality,
                Tempo = session.Tempo,
                Position = GetPosition(performer),
                RangeMeters = PerformanceAudienceRules.GetRange(
                    Stat.GetStatAdjustment(performer, StatType.PerformanceRangeBonusMeters)),
                Venue = venue,
                XpBase = xpBase,
                XpScaled = xpScaled,
                DebtClearPercent = Math.Max(0, Stat.GetStatAdjustment(
                    performer, isDancer ? StatType.PerformanceDanceDebtClearPercent : StatType.PerformanceMusicDebtClearPercent)),
                BankingBonusPercent = Math.Max(0, Stat.GetStatAdjustment(
                    performer, isDancer ? StatType.PerformanceBankingBonusPercent : StatType.PerformanceHarmonyBankingBonusPercent)),
                CapBonusMinutes = isDancer
                    ? Math.Max(0, Stat.GetStatAdjustment(performer, StatType.PerformanceInspiredCapBonusMinutes))
                    : 0,
                LightFeetSpeedPercent = isDancer
                    ? Math.Max(0, Stat.GetStatAdjustment(performer, StatType.PerformanceLightFeetSpeedPercent))
                    : 0,
                RoutineAttributeBonus = isDancer
                    ? Math.Max(0, Stat.GetStatAdjustment(performer, StatType.PerformanceRoutineAttributeBonus))
                    : 0,
                SteadyHandsBonus = isDancer
                    ? 0
                    : Math.Max(0, Stat.GetStatAdjustment(performer, StatType.PerformanceSteadyHandsBonus)),
                AnglersLullabyBonus = isDancer
                    ? 0
                    : Math.Max(0, Stat.GetStatAdjustment(performer, StatType.PerformanceAnglersLullabyBonus)),
                SetAttributeBonus = isDancer
                    ? 0
                    : Math.Max(0, Stat.GetStatAdjustment(performer, StatType.PerformanceSetAttributeBonus)),
                HasCapstone = hasCapstone,
                HasPartnerTrait = Perk.GetPerkLevel(performer, isDancer ? PerkType.PartnerDance : PerkType.Duet) > 0,
                HasStandingOvation = isDancer && Perk.GetPerkLevel(performer, PerkType.StandingOvation) > 0,
                HasRequiem = !isDancer && Perk.GetPerkLevel(performer, PerkType.Requiem) > 0,
                // A routine or set perk refunded mid-performance stops granting its bonus at the next tick.
                Routine = isDancer && Performance.IsRoutineUnlocked(performer, session.Routine)
                    ? session.Routine
                    : PerformanceRoutineType.CantinaShuffle,
                SecondRoutine = isDancer && hasCapstone && Performance.IsRoutineUnlocked(performer, preference.SecondRoutine)
                    ? preference.SecondRoutine
                    : PerformanceRoutineType.CantinaShuffle,
                Set = !isDancer && Performance.IsSetUnlocked(performer, session.Set)
                    ? session.Set
                    : PerformanceSetType.CantinaStandards,
                SecondSet = !isDancer && hasCapstone && Performance.IsSetUnlocked(performer, preference.SecondSet)
                    ? preference.SecondSet
                    : PerformanceSetType.CantinaStandards,
                SongTrackId = songTrackId,
                Session = session
            };
        }
    }
}

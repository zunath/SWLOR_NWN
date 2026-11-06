using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Reads a live performing session into a snapshot: the logic that used to be static in
    /// <c>Service.PerformanceService.PerformerSnapshotBuilder</c>, moved here unchanged. The bonuses come from the stats
    /// the perks grant; perk levels are only read for unlocks (traits and capstones that grant no stat).
    /// </summary>
    public class PerformanceSnapshotService : IPerformanceSnapshotService
    {
        public PerformerSnapshot Build(PerformanceSession session, int venue, int songTrackId)
        {
            var performer = session.Performer;
            var isDancer = session.Kind == PerformanceKind.Dance;
            var preference = GetPreference(performer);
            var hasCapstone = GetPerkLevel(performer, isDancer ? PerkType.Showstopper : PerkType.Virtuoso) > 0;

            // The perk's rank value is the stat; the maximum SOC can scale it to follows from that rank.
            var xpBase = Math.Max(0, GetStatAdjustment(
                performer, isDancer ? StatType.PerformanceDanceXPBonus : StatType.PerformanceMusicXPBonus));
            var xpScaled = ScaleBySocial(
                performer, xpBase, PerformanceAudienceRules.XpBonusMaximum(xpBase));

            return new PerformerSnapshot
            {
                Performer = performer,
                Kind = session.Kind,
                StartedUtc = session.StartedUtc,
                Quality = session.Quality,
                IsRoleplay = session.IsRoleplay,
                Tempo = session.Tempo,
                Position = GetPosition(performer),
                RangeMeters = PerformanceAudienceRules.GetRange(
                    GetStatAdjustment(performer, StatType.PerformanceRangeBonusMeters)),
                Venue = venue,
                XpBase = xpBase,
                XpScaled = xpScaled,
                DebtClearPercent = Math.Max(0, GetStatAdjustment(
                    performer, isDancer ? StatType.PerformanceDanceDebtClearPercent : StatType.PerformanceMusicDebtClearPercent)),
                BankingBonusPercent = Math.Max(0, GetStatAdjustment(
                    performer, isDancer ? StatType.PerformanceBankingBonusPercent : StatType.PerformanceHarmonyBankingBonusPercent)),
                CapBonusMinutes = isDancer
                    ? Math.Max(0, GetStatAdjustment(performer, StatType.PerformanceInspiredCapBonusMinutes))
                    : 0,
                LightFeetSpeedPercent = isDancer
                    ? Math.Max(0, GetStatAdjustment(performer, StatType.PerformanceLightFeetSpeedPercent))
                    : 0,
                RoutineAttributeBonus = isDancer
                    ? Math.Max(0, GetStatAdjustment(performer, StatType.PerformanceRoutineAttributeBonus))
                    : 0,
                SteadyHandsBonus = isDancer
                    ? 0
                    : Math.Max(0, GetStatAdjustment(performer, StatType.PerformanceSteadyHandsBonus)),
                AnglersLullabyBonus = isDancer
                    ? 0
                    : Math.Max(0, GetStatAdjustment(performer, StatType.PerformanceAnglersLullabyBonus)),
                SetAttributeBonus = isDancer
                    ? 0
                    : Math.Max(0, GetStatAdjustment(performer, StatType.PerformanceSetAttributeBonus)),
                HasCapstone = hasCapstone,
                HasPartnerTrait = GetPerkLevel(performer, isDancer ? PerkType.PartnerDance : PerkType.Duet) > 0,
                HasStandingOvation = isDancer && GetPerkLevel(performer, PerkType.StandingOvation) > 0,
                HasRequiem = !isDancer && GetPerkLevel(performer, PerkType.Requiem) > 0,
                // A routine or set perk refunded mid-performance stops granting its bonus at the next tick.
                Routine = isDancer && IsRoutineUnlocked(performer, session.Routine)
                    ? session.Routine
                    : PerformanceRoutineType.CantinaShuffle,
                SecondRoutine = isDancer && hasCapstone && IsRoutineUnlocked(performer, preference.SecondRoutine)
                    ? preference.SecondRoutine
                    : PerformanceRoutineType.CantinaShuffle,
                Set = !isDancer && IsSetUnlocked(performer, session.Set)
                    ? session.Set
                    : PerformanceSetType.CantinaStandards,
                SecondSet = !isDancer && hasCapstone && IsSetUnlocked(performer, preference.SecondSet)
                    ? preference.SecondSet
                    : PerformanceSetType.CantinaStandards,
                SongTrackId = songTrackId,
                Session = session
            };
        }

        // The main service of the Performance service is reached through its facade: it takes the services that use this one by constructor.
        protected virtual PerformancePreference GetPreference(uint player) => Performance.GetPreference(player);

        protected virtual bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine) => Performance.IsRoutineUnlocked(player, routine);

        protected virtual bool IsSetUnlocked(uint player, PerformanceSetType set) => Performance.IsSetUnlocked(player, set);

        protected virtual int GetPerkLevel(uint creature, PerkType perk) => Perk.GetPerkLevel(creature, perk);

        protected virtual int GetStatAdjustment(uint creature, StatType stat) => Stat.GetStatAdjustment(creature, stat);

        protected virtual int ScaleBySocial(uint performer, int baseValue, int maximum) =>
            AbilityEffectScaling.ScaleValueBySourceSocial(performer, baseValue, maximum);
    }
}

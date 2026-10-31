using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>Builds the Inspired a watcher gets from the dancer and the musician they follow. Pure.</summary>
    public static class PerformanceInspiredComposer
    {
        /// <param name="dancer">The followed dancer, or null.</param>
        /// <param name="musician">The followed musician, or null.</param>
        /// <param name="venue">The venue rating of the show; each point adds 1% skill XP.</param>
        public static InspiredEffectData Compose(PerformerSnapshot dancer, PerformerSnapshot musician, int venue)
        {
            if (dancer == null && musician == null)
                return new InspiredEffectData();

            var might = 0;
            var agility = 0;
            var vitality = 0;
            var perception = 0;
            var willpower = 0;
            var social = 0;
            var craft = 0;
            var speed = 0;
            var fishing = 0;

            foreach (var (routine, source) in PerformanceRiders.GetRoutineRiders(dancer))
            {
                var attribute = 1 + Math.Max(0, source.RoutineAttributeBonus);
                switch (routine)
                {
                    case PerformanceRoutineType.LightFeet:
                        speed += Math.Max(0, source.LightFeetSpeedPercent);
                        break;
                    case PerformanceRoutineType.PowerStep:
                        might += attribute;
                        break;
                    case PerformanceRoutineType.Quickstep:
                        agility += attribute;
                        break;
                    case PerformanceRoutineType.VigorReel:
                        vitality += attribute;
                        break;
                }
            }

            foreach (var (set, source) in PerformanceRiders.GetSetRiders(musician))
            {
                var attribute = 1 + Math.Max(0, source.SetAttributeBonus);
                switch (set)
                {
                    case PerformanceSetType.SteadyHands:
                        craft += Math.Max(0, source.SteadyHandsBonus);
                        break;
                    case PerformanceSetType.KeenEar:
                        perception += attribute;
                        break;
                    case PerformanceSetType.StillWaters:
                        willpower += attribute;
                        break;
                    case PerformanceSetType.SilverTongue:
                        social += attribute;
                        break;
                    case PerformanceSetType.AnglersLullaby:
                        fishing += Math.Max(0, source.AnglersLullabyBonus);
                        break;
                }
            }

            var experience = Math.Max(0, dancer?.XpScaled ?? 0) +
                             Math.Max(0, musician?.XpScaled ?? 0) +
                             PerformanceMath.ClampVenue(venue);

            return new InspiredEffectData
            {
                XPBonusPercent = experience,
                OutOfCombatMovementSpeedPercent = speed,
                Might = might,
                Agility = agility,
                Vitality = vitality,
                Perception = perception,
                Willpower = willpower,
                Social = social,
                CraftBonus = craft,
                FishingCatchChancePercent = fishing,
                Requiem = musician != null && musician.HasRequiem,
                StandingOvation = dancer != null && dancer.HasStandingOvation
            };
        }
    }
}

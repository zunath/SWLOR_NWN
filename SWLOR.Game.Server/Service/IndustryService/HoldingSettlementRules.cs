using System.Linq;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.HoldingService;

namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>
    /// Advances a holding across a change in the modifiers a city puts on it. A holding settles lazily over whole days, so a bonus
    /// that builds up while nobody watches must count only for the time it was in force: each stretch of days is advanced under the
    /// modifiers that held at its start, and every known change time ends a stretch.
    /// </summary>
    public static class HoldingSettlementRules
    {
        /// <summary>
        /// Brings the holding up to <paramref name="now"/>. The days after the holding's last settlement and up to the first change
        /// time use the modifiers at the holding's last settlement; the days after a change time use the modifiers at that time.
        /// </summary>
        /// <param name="modifiersAt">The modifiers in force from an instant onward.</param>
        /// <param name="changeTimes">Instants at which the modifiers can change by themselves, such as the end of a waiting period.</param>
        public static HoldingAdvanceResult Advance(
            PlanetaryHolding holding,
            HoldingDetail detail,
            DateTime now,
            System.Random random,
            Func<DateTime, HoldingModifiers> modifiersAt,
            IEnumerable<DateTime> changeTimes)
        {
            var result = new HoldingAdvanceResult();
            var lower = holding.LastProcessedAt;
            var stops = (changeTimes ?? Enumerable.Empty<DateTime>())
                .Where(time => time > lower && time < now)
                .Distinct()
                .OrderBy(time => time)
                .Concat(new[] { now });

            foreach (var upper in stops)
            {
                var part = HoldingRules.Advance(holding, detail, upper, random, modifiersAt(lower));
                result.Days += part.Days;
                result.Events.AddRange(part.Events);
                if (part.Foreclosed)
                {
                    result.Foreclosed = true;
                    break;
                }

                lower = upper;
            }

            return result;
        }
    }
}

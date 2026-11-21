using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// The rules of City Affairs situations (Player City Revamp, "City Affairs"): what can be drawn, when, how a choice
    /// is checked and paid for, and how effects are limited. Pure, so every limit can be tested without the game.
    /// </summary>
    public static class SituationRules
    {
        public const int DefaultWeight = 10;

        /// <summary>The most situations a city can have open at once.</summary>
        public const int MaxOpenSituations = 3;

        /// <summary>Hours that must pass after a situation is drawn before a random one is drawn again.</summary>
        public const int DrawIntervalHours = 24;

        /// <summary>Hours an official has to decide before the ignored outcome applies.</summary>
        public const int ExpiryHours = 72;

        /// <summary>Hours a modifier lasts. A repeat restarts them.</summary>
        public const int ModifierHours = 72;

        /// <summary>The most one situation can take from the treasury.</summary>
        public const int MaxCost = 15000;

        /// <summary>The most one situation can add to the treasury.</summary>
        public const int MaxGain = 10000;

        /// <summary>The most situations can add to a city's treasury over a rolling <see cref="GainWindowDays"/> days.</summary>
        public const int WeeklyGainCap = 20000;

        public const int GainWindowDays = 7;

        /// <summary>Days a finished situation is kept, for the gain cap and the draw limit.</summary>
        public const int RecordRetentionDays = 14;

        /// <summary>The share of the skill chart's XP a decision pays, in percent.</summary>
        public const int LeadershipXpPercent = 25;

        /// <summary>The Leadership level of a decision in a level 1 city. Each city level adds the same again.</summary>
        public const int LeadershipLevelPerCityLevel = 10;

        #region Effects

        /// <summary>
        /// The credits an effect list takes from the treasury, as a positive number, up to <see cref="MaxCost"/>. An effect that pays
        /// what the situation's subject owes, such as a customs fine, costs <paramref name="subjectCredits"/>.
        /// </summary>
        public static int GetCost(IEnumerable<SituationEffect> effects, int subjectCredits = 0)
        {
            var list = (effects ?? Enumerable.Empty<SituationEffect>()).ToList();
            var cost = list
                .Where(effect => effect.Type == SituationEffectType.TreasuryChange && effect.Credits < 0)
                .Sum(effect => -effect.Credits);
            if (list.Any(effect => effect.Type == SituationEffectType.HoldingFine))
                cost += Math.Max(0, subjectCredits);

            return Math.Min(cost, MaxCost);
        }

        /// <summary>The credits an effect list adds to the treasury, up to <see cref="MaxGain"/>.</summary>
        public static int GetGain(IEnumerable<SituationEffect> effects)
        {
            var gain = (effects ?? Enumerable.Empty<SituationEffect>())
                .Where(effect => effect.Type == SituationEffectType.TreasuryChange && effect.Credits > 0)
                .Sum(effect => effect.Credits);

            return Math.Min(gain, MaxGain);
        }

        /// <summary>The treasury change an effect list asks for: its gain minus its cost, within the limits.</summary>
        public static int GetRequestedChange(IEnumerable<SituationEffect> effects, int subjectCredits = 0)
        {
            var list = (effects ?? Enumerable.Empty<SituationEffect>()).ToList();
            return Math.Clamp(GetGain(list) - GetCost(list, subjectCredits), -MaxCost, MaxGain);
        }

        /// <summary>
        /// The change the treasury actually takes. A cost never takes more than the treasury holds, and gains stop once
        /// the city has gained <see cref="WeeklyGainCap"/> credits from situations in the last week.
        /// </summary>
        /// <param name="requested">The change an outcome asks for.</param>
        /// <param name="treasury">The credits in the treasury.</param>
        /// <param name="gainsInWindow">The credits situations added in the last <see cref="GainWindowDays"/> days.</param>
        public static int ResolveTreasuryChange(int requested, long treasury, int gainsInWindow)
        {
            var change = Math.Clamp(requested, -MaxCost, MaxGain);
            if (change < 0)
                return -(int)Math.Min(-change, Math.Max(0, treasury));

            if (change > 0)
                return Math.Min(change, Math.Max(0, WeeklyGainCap - gainsInWindow));

            return 0;
        }

        /// <summary>A choice the treasury cannot pay for is disabled.</summary>
        public static bool CanAfford(int cost, long treasury)
        {
            return treasury >= cost;
        }

        /// <summary>The credits situations added to the treasury in the last <see cref="GainWindowDays"/> days.</summary>
        public static int GetGainsInWindow(IEnumerable<CitySituation> situations, DateTime nowUtc)
        {
            var since = nowUtc.AddDays(-GainWindowDays);
            return (situations ?? Enumerable.Empty<CitySituation>())
                .Where(situation => situation.TreasuryGain > 0 &&
                                    situation.ResolvedUtc.HasValue &&
                                    situation.ResolvedUtc.Value > since &&
                                    situation.ResolvedUtc.Value <= nowUtc)
                .Sum(situation => situation.TreasuryGain);
        }

        #endregion

        #region Requirements

        public static bool IsMet(SituationRequirement requirement, SituationContext context)
        {
            switch (requirement.Type)
            {
                case SituationRequirementType.CityLevel:
                    return context.CityLevel >= requirement.Level;
                case SituationRequirementType.Planet:
                    return requirement.Planets != 0 && (context.Planet & requirement.Planets) != 0;
                case SituationRequirementType.PlanetCity:
                    return !context.IsStation;
                case SituationRequirementType.Facility:
                    return context.FacilityLevels.TryGetValue(requirement.Facility, out var facilityLevel) &&
                           facilityLevel >= requirement.Level;
                case SituationRequirementType.ActiveEdict:
                    return context.ActiveEdicts.Contains(requirement.EdictName);
                case SituationRequirementType.SkillRank:
                    return context.SkillRanks.TryGetValue(requirement.Skill, out var rank) && rank >= requirement.Level;
                case SituationRequirementType.Structure:
                    return context.StructureTypes.Contains(requirement.Structure);
                case SituationRequirementType.TravelerHost:
                    return context.HostedTravelers.Contains(requirement.TravelerName);
                default:
                    return false;
            }
        }

        public static bool AreMet(IEnumerable<SituationRequirement> requirements, SituationContext context)
        {
            return (requirements ?? Enumerable.Empty<SituationRequirement>()).All(requirement => IsMet(requirement, context));
        }

        /// <summary>The requirements a choice asks for that the context does not meet, as player text.</summary>
        public static List<string> DescribeUnmet(IEnumerable<SituationRequirement> requirements, SituationContext context)
        {
            return (requirements ?? Enumerable.Empty<SituationRequirement>())
                .Where(requirement => !IsMet(requirement, context))
                .Select(Describe)
                .ToList();
        }

        public static string Describe(SituationRequirement requirement)
        {
            switch (requirement.Type)
            {
                case SituationRequirementType.CityLevel:
                    return $"City level {requirement.Level}";
                case SituationRequirementType.Planet:
                    return $"On {requirement.Planets}";
                case SituationRequirementType.PlanetCity:
                    return "A planet city";
                case SituationRequirementType.Facility:
                    return $"{GetFacilityName(requirement.Facility)} level {requirement.Level}";
                case SituationRequirementType.ActiveEdict:
                    return $"{SplitWords(requirement.EdictName)} in force";
                case SituationRequirementType.SkillRank:
                    return $"{requirement.Skill} {requirement.Level}";
                case SituationRequirementType.Structure:
                    return $"A {SplitWords(requirement.Structure.ToString())} in the city";
                case SituationRequirementType.TravelerHost:
                    return $"The {SplitWords(requirement.TravelerName).ToLowerInvariant()} visiting";
                default:
                    return string.Empty;
            }
        }

        public static string GetFacilityName(PropertyUpgradeType facility)
        {
            switch (facility)
            {
                case PropertyUpgradeType.MedicalCenterLevel: return "Medical Center";
                case PropertyUpgradeType.StarportLevel: return "Starport";
                case PropertyUpgradeType.CantinaLevel: return "Cantina";
                case PropertyUpgradeType.JumpBeaconLevel: return "Jump Beacon";
                default: return "Facility";
            }
        }

        private static string SplitWords(string name)
        {
            return string.IsNullOrEmpty(name)
                ? string.Empty
                : Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
        }

        #endregion

        #region Drawing

        /// <summary>A city is in disrepair while it owes upkeep it did not pay by the next weekly check.</summary>
        public static bool IsInDisrepair(int upkeepOwed, bool hasDisrepairDeadline)
        {
            return upkeepOwed > 0 && hasDisrepairDeadline;
        }

        /// <summary>
        /// Whether the city can draw a random situation: fewer than <see cref="MaxOpenSituations"/> open, none drawn in
        /// the last <see cref="DrawIntervalHours"/> hours, and not in disrepair or paused for neglect.
        /// </summary>
        public static bool CanDrawNew(int openCount, DateTime? lastDrawnUtc, bool isInDisrepair, bool isNeglected, DateTime nowUtc)
        {
            if (isInDisrepair || isNeglected || openCount >= MaxOpenSituations)
                return false;

            return !lastDrawnUtc.HasValue || nowUtc - lastDrawnUtc.Value >= TimeSpan.FromHours(DrawIntervalHours);
        }

        /// <summary>Whether a follow-up that is due can arrive: fewer than <see cref="MaxOpenSituations"/> open, and not in disrepair or paused for neglect.</summary>
        public static bool CanArriveFollowUp(int openCount, bool isInDisrepair, bool isNeglected)
        {
            return !isInDisrepair && !isNeglected && openCount < MaxOpenSituations;
        }

        /// <summary>
        /// The definitions a city can draw at random: not a follow-up or an event-driven situation, requirements met, and no
        /// situation of the type open.
        /// </summary>
        public static List<SituationDetail> SelectEligible(
            IEnumerable<SituationDetail> definitions,
            SituationContext context,
            ISet<SituationType> unavailableTypes)
        {
            return (definitions ?? Enumerable.Empty<SituationDetail>())
                .Where(detail => !detail.IsFollowUp &&
                                 !detail.IsEventDriven &&
                                 detail.Weight > 0 &&
                                 detail.Choices.Count > 0 &&
                                 !unavailableTypes.Contains(detail.Type) &&
                                 AreMet(detail.Requirements, context))
                .ToList();
        }

        /// <summary>Picks one definition by weight. A roll of 0 picks the first, and a roll just under 1 the last.</summary>
        /// <param name="eligible">The definitions to pick from.</param>
        /// <param name="roll">A number from 0 up to, but not including, 1.</param>
        public static SituationDetail PickWeighted(IReadOnlyList<SituationDetail> eligible, double roll)
        {
            if (eligible == null || eligible.Count == 0)
                return null;

            var total = eligible.Sum(detail => detail.Weight);
            if (total <= 0)
                return null;

            var target = Math.Clamp(roll, 0d, 0.999999999d) * total;
            foreach (var detail in eligible)
            {
                target -= detail.Weight;
                if (target < 0)
                    return detail;
            }

            return eligible[^1];
        }

        /// <summary>When the city last drew a situation, random or follow-up. Null when it has none on record.</summary>
        public static DateTime? GetLastDrawnUtc(IEnumerable<CitySituation> situations)
        {
            var drawn = (situations ?? Enumerable.Empty<CitySituation>())
                .Where(situation => situation.State != CitySituationState.Queued && situation.DrawnUtc != default)
                .Select(situation => situation.DrawnUtc)
                .ToList();

            return drawn.Count == 0 ? null : drawn.Max();
        }

        public static int CountOpen(IEnumerable<CitySituation> situations)
        {
            return (situations ?? Enumerable.Empty<CitySituation>()).Count(situation => situation.State == CitySituationState.Open);
        }

        public static HashSet<SituationType> GetOpenTypes(IEnumerable<CitySituation> situations)
        {
            return (situations ?? Enumerable.Empty<CitySituation>())
                .Where(situation => situation.State == CitySituationState.Open)
                .Select(situation => situation.Type)
                .ToHashSet();
        }

        #endregion

        #region Life of a situation

        public static DateTime GetExpiryUtc(DateTime drawnUtc)
        {
            return drawnUtc.AddHours(ExpiryHours);
        }

        /// <summary>An open situation whose time has run out. It takes its ignored outcome at the next server start.</summary>
        public static bool IsOverdue(CitySituation situation, DateTime nowUtc)
        {
            return situation.State == CitySituationState.Open && situation.ExpiresUtc <= nowUtc;
        }

        /// <summary>Officials can decide a situation only while it is open and its time has not run out.</summary>
        public static bool CanDecide(CitySituation situation, DateTime nowUtc)
        {
            return situation != null && situation.State == CitySituationState.Open && situation.ExpiresUtc > nowUtc;
        }

        public static bool IsFollowUpDue(CitySituation situation, DateTime nowUtc)
        {
            return situation.State == CitySituationState.Queued && situation.DueUtc <= nowUtc;
        }

        /// <summary>The queued follow-ups that are due, oldest due date first. A type that is open waits.</summary>
        public static List<CitySituation> SelectDueFollowUps(
            IEnumerable<CitySituation> situations,
            ISet<SituationType> openTypes,
            DateTime nowUtc)
        {
            return (situations ?? Enumerable.Empty<CitySituation>())
                .Where(situation => IsFollowUpDue(situation, nowUtc) && !openTypes.Contains(situation.Type))
                .OrderBy(situation => situation.DueUtc)
                .ThenBy(situation => situation.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>A follow-up is queued unless one of its type is already open or waiting.</summary>
        public static bool CanQueueFollowUp(SituationType type, IEnumerable<CitySituation> situations)
        {
            return !(situations ?? Enumerable.Empty<CitySituation>())
                .Any(situation => situation.Type == type &&
                                  (situation.State == CitySituationState.Open || situation.State == CitySituationState.Queued));
        }

        /// <summary>Whether a follow-up with a one in <paramref name="oneIn"/> chance is queued, for a roll from 0 up to <paramref name="oneIn"/>.</summary>
        public static bool RollsFollowUp(int oneIn, int roll)
        {
            return oneIn <= 1 || roll == 0;
        }

        /// <summary>Whether a finished situation is old enough to delete.</summary>
        public static bool ShouldPrune(CitySituation situation, DateTime nowUtc)
        {
            return (situation.State == CitySituationState.Decided || situation.State == CitySituationState.Expired) &&
                   situation.ResolvedUtc.HasValue &&
                   situation.ResolvedUtc.Value < nowUtc.AddDays(-RecordRetentionDays);
        }

        #endregion

        #region Modifiers

        /// <summary>Applies a modifier, restarting its duration if the city already has it.</summary>
        public static void RefreshModifier(
            IDictionary<CityModifierType, CityModifier> modifiers,
            CityModifierType type,
            SituationType source,
            DateTime nowUtc)
        {
            modifiers[type] = new CityModifier
            {
                ExpiresUtc = nowUtc.AddHours(ModifierHours),
                Source = source
            };
        }

        public static bool IsModifierActive(IReadOnlyDictionary<CityModifierType, CityModifier> modifiers, CityModifierType type, DateTime nowUtc)
        {
            return modifiers != null &&
                   modifiers.TryGetValue(type, out var modifier) &&
                   modifier != null &&
                   modifier.ExpiresUtc > nowUtc;
        }

        /// <summary>Removes modifiers that have run out. Returns true if any were removed.</summary>
        public static bool PruneModifiers(IDictionary<CityModifierType, CityModifier> modifiers, DateTime nowUtc)
        {
            var expired = modifiers
                .Where(pair => pair.Value == null || pair.Value.ExpiresUtc <= nowUtc)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var type in expired)
            {
                modifiers.Remove(type);
            }

            return expired.Count > 0;
        }

        public static string DescribeModifier(CityModifierType type)
        {
            switch (type)
            {
                case CityModifierType.FestivalXpBonus: return "citizens earn 5% more RP XP";
                case CityModifierType.TransportationIncomeHalved: return "transportation tax income is halved";
                case CityModifierType.MedicalCenterBonusHalved: return "the Medical Center bonus is halved";
                case CityModifierType.OpenGatesSuspended: return "Open Gates is suspended";
                case CityModifierType.CropPests: return "crops harvested in the city yield 1 less";
                default: return string.Empty;
            }
        }

        #endregion

        #region Reward

        /// <summary>The Leadership level of a decision, which sets its XP against the decider's rank.</summary>
        public static int GetActivityLevel(int cityLevel)
        {
            return Math.Max(1, cityLevel) * LeadershipLevelPerCityLevel;
        }

        /// <summary>The Leadership XP of a decision: <see cref="LeadershipXpPercent"/> percent of the skill chart's XP for its level against the rank.</summary>
        /// <param name="chartXp">The skill chart's XP for the decision's level minus the decider's rank.</param>
        public static int GetLeadershipXp(int chartXp)
        {
            return Math.Max(0, chartXp) * LeadershipXpPercent / 100;
        }

        #endregion

        #region Display

        public static string FormatTimeLeft(DateTime expiresUtc, DateTime nowUtc)
        {
            var left = expiresUtc - nowUtc;
            if (left <= TimeSpan.Zero)
                return "Out of time";

            if (left.TotalDays >= 1)
                return $"{(int)left.TotalDays}d {left.Hours}h left";

            if (left.TotalHours >= 1)
                return $"{(int)left.TotalHours}h {left.Minutes}m left";

            return $"{Math.Max(1, left.Minutes)}m left";
        }

        public static string FormatCredits(int credits)
        {
            return $"{credits:N0} cr";
        }

        /// <summary>What a choice does, for the tooltip: its cost or gain, modifiers, and follow-ups.</summary>
        public static string DescribeEffects(IEnumerable<SituationEffect> effects, bool isSecret = false, int subjectCredits = 0)
        {
            var parts = new List<string>();
            foreach (var effect in effects ?? Enumerable.Empty<SituationEffect>())
            {
                switch (effect.Type)
                {
                    case SituationEffectType.HoldingHelp:
                        parts.Add("The city's guards drive the raiders off the holding.");
                        break;
                    case SituationEffectType.HoldingFine:
                        parts.Add(subjectCredits > 0
                            ? $"Pays the holding's customs fine from the treasury: {FormatCredits(Math.Min(subjectCredits, MaxCost))}."
                            : $"Pays the holding's customs fine from the treasury, up to {FormatCredits(MaxCost)}.");
                        break;
                    case SituationEffectType.TreasuryChange when effect.Credits < 0:
                        parts.Add($"Costs {FormatCredits(-effect.Credits)}.");
                        break;
                    case SituationEffectType.TreasuryChange when effect.Credits > 0:
                        parts.Add($"Adds {FormatCredits(effect.Credits)} to the treasury.");
                        break;
                    case SituationEffectType.Modifier:
                        parts.Add($"For {ModifierHours} hours, {DescribeModifier(effect.Modifier)}.");
                        break;
                    case SituationEffectType.FollowUp:
                        parts.Add(effect.FollowUpOneIn > 1 ? "It may come back to haunt the city." : "A follow-up situation will arrive.");
                        break;
                    case SituationEffectType.StartHunt:
                        parts.Add("Predators attack the corrals for 30 minutes. If they are not driven off, the city's animals give no products for 72 hours.");
                        break;
                    case SituationEffectType.LivestockNoProducts:
                        parts.Add("The city's animals give no products for 72 hours.");
                        break;
                }
            }

            if (isSecret)
                parts.Add("Kept out of the Gazette for now.");

            return string.Join(" ", parts);
        }

        #endregion
    }
}

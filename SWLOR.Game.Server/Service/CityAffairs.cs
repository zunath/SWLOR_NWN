using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// City Affairs (Player City Revamp): situations arrive at server start, officials decide them in the City Affairs
    /// window, and undecided ones take their ignored outcome. The rules live in <see cref="SituationRules"/> and
    /// <see cref="RecallRules"/>; this class reads and writes the records.
    /// </summary>
    public static class CityAffairs
    {
        private static readonly PropertyUpgradeType[] FacilityTypes =
        {
            PropertyUpgradeType.MedicalCenterLevel,
            PropertyUpgradeType.StarportLevel,
            PropertyUpgradeType.CantinaLevel,
            PropertyUpgradeType.JumpBeaconLevel
        };

        /// <summary>Opens the City Affairs window for the mayor or an officer. Returns the reason it was refused, or empty.</summary>
        public static string TryOpenWindow(uint player, string cityId, uint tetherObject = OBJECT_INVALID)
        {
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            if (city == null || city.PropertyType != PropertyType.City)
                return "This city no longer exists.";

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player) ||
                !CityOfficers.CanHandle(city, GetObjectUUID(player)))
                return "Only the mayor and the city's officers can handle City Affairs.";

            Gui.TogglePlayerWindow(player, GuiWindowType.CityAffairs, new CityAffairsPayload(city.Id), tetherObject);
            return string.Empty;
        }

        /// <summary>The city's situations, open or finished, newest first.</summary>
        public static List<CitySituation> LoadSituations(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return new List<CitySituation>();

            var query = new DBQuery<CitySituation>()
                .AddFieldSearch(nameof(CitySituation.CityId), cityId, false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<CitySituation>();

            return DB.Search(query.AddPaging(count, 0))
                .Where(situation => situation != null)
                .OrderByDescending(situation => situation.DrawnUtc)
                .ThenBy(situation => situation.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>The situations waiting for a decision, soonest to run out first.</summary>
        public static List<CitySituation> LoadOpenSituations(string cityId)
        {
            return LoadSituations(cityId)
                .Where(situation => situation.State == CitySituationState.Open)
                .OrderBy(situation => situation.ExpiresUtc)
                .ToList();
        }

        /// <summary>Whether a situation's modifier is in force in the city. Later systems read the modifiers through this.</summary>
        public static bool IsModifierActive(WorldProperty city, CityModifierType type)
        {
            return city != null && SituationRules.IsModifierActive(city.Modifiers, type, DateTime.UtcNow);
        }

        /// <summary>Deletes every situation of a city that is being deleted.</summary>
        public static void DeleteCityData(string cityId)
        {
            foreach (var situation in LoadSituations(cityId))
            {
                DB.Delete<CitySituation>(situation.Id);
            }
        }

        /// <summary>
        /// What the situation requirements are checked against for a city, and for the deciding official when ranks are given.
        /// </summary>
        public static SituationContext BuildContext(WorldProperty city, IReadOnlyDictionary<SkillType, int> skillRanks = null)
        {
            var facilities = new Dictionary<PropertyUpgradeType, int>();
            foreach (var facility in FacilityTypes)
            {
                var level = Property.GetEffectiveUpgradeLevel(city.Id, facility);
                if (level > 0)
                    facilities[facility] = level;
            }

            return new SituationContext
            {
                CityLevel = CityOfficers.GetCityLevel(city),
                IsStation = Station.IsStation(city),
                Planet = Station.GetCityPlanet(city),
                FacilityLevels = facilities,
                StructureTypes = GetStructureTypes(city),
                HostedTravelers = Travelers.GetHostedBy(city.Id).Select(type => type.ToString()).ToList(),
                ActiveEdicts = GetActiveEdictNames(city),
                SkillRanks = skillRanks ?? new Dictionary<SkillType, int>()
            };
        }

        /// <summary>
        /// The types of structure the city has: those standing on its land, and the garden plots inside its houses, so a city whose
        /// farmers only garden indoors still counts as having gardens. A house is the interior of one of the city's building structures
        /// (<see cref="PropertyHierarchyRules.GetStructureTypes"/>).
        /// </summary>
        private static IReadOnlyCollection<StructureType> GetStructureTypes(WorldProperty city)
        {
            return PropertyHierarchyRules.GetStructureTypes(GetChildren(new[] { city.Id }), GetChildren);
        }

        /// <summary>The properties whose parent is any of the ids.</summary>
        private static List<WorldProperty> GetChildren(IReadOnlyCollection<string> parentIds)
        {
            if (parentIds.Count == 0)
                return new List<WorldProperty>();

            var query = new DBQuery<WorldProperty>().AddFieldSearch(nameof(WorldProperty.ParentPropertyId), parentIds.ToList());
            var count = (int)DB.SearchCount(query);
            return count <= 0 ? new List<WorldProperty>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>The skill ranks of a character, for the requirements of a choice.</summary>
        public static Dictionary<SkillType, int> GetSkillRanks(string playerId)
        {
            var player = string.IsNullOrWhiteSpace(playerId) ? null : DB.Get<Player>(playerId);
            return player == null
                ? new Dictionary<SkillType, int>()
                : player.Skills.ToDictionary(pair => pair.Key, pair => pair.Value.Rank);
        }

        // The names are EdictType members, such as "OpenGates". Only an Edict in effect counts.
        private static IReadOnlyCollection<string> GetActiveEdictNames(WorldProperty city)
        {
            return EdictChanges.GetInEffectTypes(city, DateTime.UtcNow).Select(type => type.ToString()).ToList();
        }

        #region Server start

        /// <summary>
        /// Runs at server start for each city: ends offices of citizens who left, expires situations that ran out of
        /// time, opens a recall after neglect, and brings in due follow-ups and a new situation. A failure is logged and
        /// never stops the rest of city processing.
        /// </summary>
        public static void Process(DateTime now, WorldProperty city)
        {
            try
            {
                ProcessCity(now, city);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to process City Affairs for city '{city?.CustomName}' ({city?.Id}): {ex}");
            }
        }

        private static void ProcessCity(DateTime now, WorldProperty city)
        {
            if (city.IsQueuedForDeletion)
                return;

            var changed = CityOfficers.RemoveDepartedOfficers(city);
            changed |= SituationRules.PruneModifiers(city.Modifiers, now);
            if (changed)
                DB.Set(city);

            var situations = LoadSituations(city.Id);
            foreach (var old in situations.Where(situation => SituationRules.ShouldPrune(situation, now)).ToList())
            {
                DB.Delete<CitySituation>(old.Id);
                situations.Remove(old);
            }

            foreach (var overdue in situations.Where(situation => SituationRules.IsOverdue(situation, now))
                         .OrderBy(situation => situation.ExpiresUtc)
                         .ToList())
            {
                Expire(now, city, overdue, situations);
            }

            CityRecall.OpenIfNeglected(now, city);

            var isInDisrepair = SituationRules.IsInDisrepair(city.Upkeep, city.Dates.ContainsKey(PropertyDateType.DisrepairDestruction));
            var isNeglected = city.NeglectSinceUtc.HasValue;
            ArriveDueFollowUps(now, city, situations, isInDisrepair, isNeglected);
            DrawNew(now, city, situations, isInDisrepair, isNeglected);
        }

        /// <summary>An undecided situation ran out of time: its ignored outcome applies, and it counts toward neglect.</summary>
        private static void Expire(DateTime now, WorldProperty city, CitySituation situation, List<CitySituation> situations)
        {
            var detail = SituationCatalog.Get(situation.Type);

            situation.State = CitySituationState.Expired;
            situation.ResolvedUtc = now;
            if (detail != null)
                ApplyEffects(city, situation, detail.IgnoredEffects, now, situations, string.Empty);

            city.NeglectCount = RecallRules.AddExpired(city.NeglectCount, 1);

            // The situation is saved first, so a restart between the two writes never applies its outcome twice.
            DB.Set(situation);
            DB.Set(city);

            if (detail != null && !string.IsNullOrWhiteSpace(detail.IgnoredGazetteText))
                CityGazette.Post(city.Id, CityGazetteEntryType.Decision, detail.IgnoredGazetteText, isHook: detail.IgnoredIsHook);

            Log.Write(LogGroup.Property, $"Situation '{situation.Type}' of city '{city.CustomName}' ({city.Id}) expired unhandled. Neglect count: {city.NeglectCount}.");
        }

        private static void ArriveDueFollowUps(
            DateTime now,
            WorldProperty city,
            List<CitySituation> situations,
            bool isInDisrepair,
            bool isNeglected)
        {
            var openCount = SituationRules.CountOpen(situations);
            var openTypes = SituationRules.GetOpenTypes(situations);

            foreach (var followUp in SituationRules.SelectDueFollowUps(situations, openTypes, now))
            {
                if (!SituationRules.CanArriveFollowUp(openCount, isInDisrepair, isNeglected))
                    return;

                var detail = SituationCatalog.Get(followUp.Type);
                if (detail == null || detail.Choices.Count == 0)
                {
                    DB.Delete<CitySituation>(followUp.Id);
                    situations.Remove(followUp);
                    continue;
                }

                Open(now, followUp);
                if (detail.ExposesSecret && !string.IsNullOrWhiteSpace(followUp.SecretEntryId))
                    CityGazette.Reveal(followUp.SecretEntryId);

                openCount++;
                Log.Write(LogGroup.Property, $"Follow-up situation '{followUp.Type}' arrived in city '{city.CustomName}' ({city.Id}).");
            }
        }

        private static void DrawNew(
            DateTime now,
            WorldProperty city,
            List<CitySituation> situations,
            bool isInDisrepair,
            bool isNeglected)
        {
            if (!SituationRules.CanDrawNew(
                    SituationRules.CountOpen(situations),
                    SituationRules.GetLastDrawnUtc(situations),
                    isInDisrepair,
                    isNeglected,
                    now))
                return;

            var eligible = SituationRules.SelectEligible(
                SituationCatalog.All.Values,
                BuildContext(city),
                SituationRules.GetOpenTypes(situations));
            var chosen = SituationRules.PickWeighted(eligible, Random.NextFloat());
            if (chosen == null)
                return;

            var situation = new CitySituation
            {
                CityId = city.Id,
                Type = chosen.Type
            };
            Open(now, situation);
            situations.Add(situation);

            Log.Write(LogGroup.Property, $"Situation '{chosen.Type}' was drawn for city '{city.CustomName}' ({city.Id}).");
        }

        private static void Open(DateTime now, CitySituation situation)
        {
            situation.State = CitySituationState.Open;
            situation.DrawnUtc = now;
            situation.ExpiresUtc = SituationRules.GetExpiryUtc(now);
            DB.Set(situation);
        }

        #endregion

        #region Deciding

        /// <summary>
        /// An official picks a choice. Checks that they may decide, that the situation is still open, that the choice's
        /// requirements are met and the treasury can pay for it; then applies its effects, posts it to the Gazette, resets
        /// the neglect count, and gives Leadership XP.
        /// </summary>
        public static CityDecisionResult Decide(uint player, string cityId, string situationId, int choiceIndex)
        {
            var now = DateTime.UtcNow;
            var playerId = GetObjectUUID(player);
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            if (city == null || city.PropertyType != PropertyType.City || city.IsQueuedForDeletion)
                return CityDecisionResult.Refused("This city no longer exists.");

            if (!CityOfficers.CanHandle(city, playerId))
                return CityDecisionResult.Refused("Only the mayor and the city's officers can decide situations.");

            var situation = string.IsNullOrWhiteSpace(situationId) ? null : DB.Get<CitySituation>(situationId);
            if (situation == null || situation.CityId != city.Id)
                return CityDecisionResult.Refused("That situation is no longer here.");

            if (situation.State != CitySituationState.Open)
                return CityDecisionResult.Refused("Another official has already settled that situation.");

            if (!SituationRules.CanDecide(situation, now))
                return CityDecisionResult.Refused("That situation ran out of time. It takes its ignored outcome at the next restart.");

            var detail = SituationCatalog.Get(situation.Type);
            if (detail == null || choiceIndex < 0 || choiceIndex >= detail.Choices.Count)
                return CityDecisionResult.Refused("That choice is not available.");

            var choice = detail.Choices[choiceIndex];
            var unmet = SituationRules.DescribeUnmet(choice.Requirements, BuildContext(city, GetSkillRanks(playerId)));
            if (unmet.Count > 0)
                return CityDecisionResult.Refused($"This choice needs: {string.Join(", ", unmet)}.");

            if (!SituationRules.CanAfford(choice.Cost, SituationTreasury.GetSpendable(city)))
                return CityDecisionResult.Refused($"The treasury cannot afford {SituationRules.FormatCredits(choice.Cost)}.");

            var situations = LoadSituations(city.Id);
            var title = CityOfficers.GetTitle(city, playerId);
            var entry = CityGazette.Post(
                city.Id,
                CityGazetteEntryType.Decision,
                choice.GazetteText,
                playerId,
                title,
                choice.IsSecret,
                choice.IsHook);

            situation.State = CitySituationState.Decided;
            situation.ChosenOption = choiceIndex;
            situation.DecidedByPlayerId = playerId;
            situation.ResolvedUtc = now;
            var change = ApplyEffects(city, situation, choice.Effects, now, situations, entry?.Id ?? string.Empty);
            city.NeglectCount = 0;

            // The situation is saved first, so a restart between the two writes never applies its effects twice.
            DB.Set(situation);
            DB.Set(city);

            GiveReward(player, playerId, city);
            Log.Write(LogGroup.Property, $"Player '{playerId}' ({title}) decided '{situation.Type}' for city '{city.CustomName}' ({city.Id}): '{choice.Text}'. Treasury change: {change}.");

            return CityDecisionResult.Success(BuildResultText(choice, change));
        }

        private static string BuildResultText(SituationChoice choice, int change)
        {
            var text = $"You chose: {choice.Text}.";
            if (change < 0)
                text += $" The treasury paid {SituationRules.FormatCredits(-change)}.";
            else if (change > 0)
                text += $" The treasury gained {SituationRules.FormatCredits(change)}.";

            return text;
        }

        private static void GiveReward(uint player, string playerId, WorldProperty city)
        {
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null || !dbPlayer.Skills.TryGetValue(SkillType.Leadership, out var leadership))
                return;

            var level = SituationRules.GetActivityLevel(CityOfficers.GetCityLevel(city));
            var xp = SituationRules.GetLeadershipXp(Skill.GetDeltaXP(level - leadership.Rank));
            if (xp > 0)
                Skill.GiveSkillXP(player, SkillType.Leadership, xp, false, false);
        }

        #endregion

        /// <summary>
        /// Applies an outcome's effects to the city: the treasury change within its limits, modifiers, and follow-ups.
        /// The caller saves the city and the situation. Returns the credits the treasury actually changed by.
        /// </summary>
        private static int ApplyEffects(
            WorldProperty city,
            CitySituation situation,
            IReadOnlyCollection<SituationEffect> effects,
            DateTime now,
            List<CitySituation> situations,
            string secretEntryId)
        {
            var change = SituationRules.ResolveTreasuryChange(
                SituationRules.GetRequestedChange(effects),
                SituationTreasury.GetSpendable(city),
                SituationRules.GetGainsInWindow(situations, now));
            if (change != 0)
                SituationTreasury.Apply(city, change);

            if (change > 0)
                situation.TreasuryGain = change;

            foreach (var effect in effects)
            {
                if (effect.Type == SituationEffectType.Modifier)
                {
                    SituationRules.RefreshModifier(city.Modifiers, effect.Modifier, situation.Type, now);
                }
                else if (effect.Type == SituationEffectType.FollowUp)
                {
                    QueueFollowUp(city, effect, now, situations, secretEntryId);
                }
                else if (effect.Type == SituationEffectType.StartHunt)
                {
                    LivestockHunts.Start(city, now);
                }
                else if (effect.Type == SituationEffectType.LivestockNoProducts)
                {
                    Corrals.ApplyPredatorPenalty(city, now);
                }
            }

            return change;
        }

        private static void QueueFollowUp(
            WorldProperty city,
            SituationEffect effect,
            DateTime now,
            List<CitySituation> situations,
            string secretEntryId)
        {
            if (!SituationRules.RollsFollowUp(effect.FollowUpOneIn, Random.Next(effect.FollowUpOneIn)) ||
                !SituationRules.CanQueueFollowUp(effect.FollowUp, situations))
                return;

            var followUp = new CitySituation
            {
                CityId = city.Id,
                Type = effect.FollowUp,
                State = CitySituationState.Queued,
                DueUtc = now.AddDays(effect.FollowUpDelayDays),
                SecretEntryId = effect.FollowUpCarriesSecret ? secretEntryId : string.Empty
            };
            DB.Set(followUp);
            situations.Add(followUp);
        }
    }
}

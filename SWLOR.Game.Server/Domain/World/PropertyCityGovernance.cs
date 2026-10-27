using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// City governance: the boot-time processing of every city (citizen requirement, elections, level, upkeep and
    /// citizenship fees), the Discord broadcasts about it, and the city level rules. The logic and state that used to
    /// be static in <c>Service.Property</c>, moved here unchanged.
    /// </summary>
    public class PropertyCityGovernance : IPropertyCityGovernance
    {
        private readonly IDBService _db;
        private readonly IPropertyCatalog _catalog;

        private readonly Dictionary<int, int> _citizensRequired = new()
        {
            { 1, 5 }, // Level 1 requires a minimum of 5 citizens
            { 2, 10 }, // Level 2 requires a minimum of 10 citizens
            { 3, 15 }, // Level 3 requires a minimum of 15 citizens
            { 4, 20 }, // Level 4 requires a minimum of 20 citizens
            { 5, 25 }  // Level 5 requires a minimum of 25 citizens
        };

        /// <summary>
        /// The color of a property broadcast when the caller does not name one.
        /// </summary>
        public const int PropertyBroadcastColor = 5763719;

        // The settings used to be read when the static class first ran; reading them where they are used keeps the
        // constructor free of work.
        private ApplicationSettings AppSettings => ApplicationSettings.Get();

        /// <summary>
        /// The Discord webhook that property broadcasts are posted to. A test replaces it.
        /// </summary>
        protected virtual string PropertyBroadcastWebhookUrl => AppSettings.PropertyBroadcastWebhookUrl;

        /// <summary>
        /// Determines the number of hours before the city will be destroyed due to
        /// lack of citizens. This starts at the time of city hall placement for the initial check.
        /// At boot time, if the number of citizens is below the required amount, the player will have 18 hours
        /// to rectify it. Failure to do so will result in the city being lost upon the next server reboot.
        /// Note: Due to the cleanup occurring on server boot, which occurs once every 24 hours,
        ///       it's possible the player will have more time than the value specified here.
        ///       This is expected.
        /// </summary>
        public const int MinimumCitizensGracePeriodHours = 18;

        /// <summary>
        /// Determines the number of days citizens have to register for an election.
        /// </summary>
        public const int ElectionRegistrationDays = 14;

        /// <summary>
        /// Determines the number of days all citizens have to vote for an election.
        /// </summary>
        public const int ElectionVotingDays = 7;

        public PropertyCityGovernance(IDBService db, IPropertyCatalog catalog)
        {
            _db = db;
            _catalog = catalog;
        }

        public void BroadcastPropertyEvent(WorldProperty city, string title, string details, int color = PropertyBroadcastColor)
        {
            var url = PropertyBroadcastWebhookUrl;
            if (string.IsNullOrWhiteSpace(url))
                return;

            var level = city.Upgrades.ContainsKey(PropertyUpgradeType.CityLevel)
                ? city.Upgrades[PropertyUpgradeType.CityLevel]
                : 1;
            var description =
                $"**City**: {SanitizeDiscordText(city.CustomName)}\n" +
                $"**City Level**: {GetCityLevelName(level)} (Lvl. {level})\n\n" +
                SanitizeDiscordText(details);

            try
            {
                var enqueued = BackgroundJob.EnqueueDiscordWebhook(
                        url,
                        "SWLOR Property Broadcast",
                        description,
                        color,
                        SanitizeDiscordText(title))
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                if (!enqueued)
                {
                    Log.Write(LogGroup.Error, $"Failed to queue property broadcast '{title}' for city '{city.CustomName}' ({city.Id}).");
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Failed to queue property broadcast '{title}' for city '{city.CustomName}' ({city.Id}): {ex}");
            }
        }

        public string FormatUtcDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
        }

        private string SanitizeDiscordText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text
                .Replace("@everyone", "(at)everyone")
                .Replace("@here", "(at)here")
                .Replace("@", "(at)")
                .Replace("`", "'");
        }

        public void BroadcastCityUpkeepPaid(WorldProperty city)
        {
            if (city.Upkeep > 0)
                return;

            BroadcastPropertyEvent(
                city,
                "City Upkeep Paid",
                $"The outstanding city upkeep has been paid.\n" +
                $"**Next Upkeep Check**: {FormatUtcDate(city.Dates[PropertyDateType.Upkeep])}");
        }

        /// <summary>
        /// Handles the processing of each individual city. The following will occur on each boot:
        /// 1.) Checking for the required number of citizens.
        ///     If this is the first reboot where they're under the requirement, 18 hours will be given to them for correction.
        ///     Otherwise if the timer has expired, the city will be destroyed.
        /// 2.) Processing elections. Moves the election process between stages and calculates votes at the end of the process.
        ///     If a new mayor is elected, the previous mayor's permissions are removed and given to the new one.
        /// </summary>
        public void ProcessCities()
        {
            var cityQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var queryCount = (int)_db.SearchCount(cityQuery);
            var cities = _db.Search(cityQuery.AddPaging(queryCount, 0));
            var now = DateTime.UtcNow;

            foreach (var city in cities)
            {
                ProcessCityCitizenRequirement(now, city);
                if (city.IsQueuedForDeletion)
                {
                    continue;
                }

                ProcessCityElections(now, city);

                if (now < city.Dates[PropertyDateType.Upkeep])
                {
                    Log.Write(LogGroup.Property, $"City '{city.CustomName}' ({city.Id}) upkeep isn't ready yet.");
                    continue;
                }
                else
                {
                    ProcessCityLevel(city);
                    ProcessUpkeep(now, city);
                    ProcessCitizenshipFees(city);

                    // Next upkeep check should be 7 days from the previous one.
                    city.Dates[PropertyDateType.Upkeep] = city.Dates[PropertyDateType.Upkeep].AddDays(7);
                }

                _db.Set(city);
            }
        }

        private void ProcessCityCitizenRequirement(DateTime now, WorldProperty city)
        {
            var citizenQuery = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            var citizenCount = (int)_db.SearchCount(citizenQuery);
            var citizens = _db.Search(citizenQuery.AddPaging(citizenCount, 0))
                .ToList();

            // City is below the number of citizens required to maintain the city.
            if (citizenCount < _citizensRequired[1])
            {
                if (city.Dates.ContainsKey(PropertyDateType.BelowRequiredCitizens))
                {
                    // The amount of time has elapsed. It's time to delete the city.
                    if (now >= city.Dates[PropertyDateType.BelowRequiredCitizens])
                    {
                        city.IsQueuedForDeletion = true;

                        Log.Write(LogGroup.Property, $"City '{city.CustomName}' in area '{city.ParentPropertyId}' has been queued for deletion because it fell under the required {_citizensRequired[1]} citizens needed to maintain it.");
                        BroadcastPropertyEvent(
                            city,
                            "City Expiration",
                            $"This city has been marked for removal because it remained below the required citizen count.\n" +
                            $"**Citizens Registered**: {citizenCount:N0}\n" +
                            $"**Citizens Required**: {_citizensRequired[1]:N0}",
                            15158332);
                    }
                    else
                    {
                        Log.Write(LogGroup.Property, $"City '{city.CustomName}' in area '{city.ParentPropertyId}' is below the required citizen count but time has not expired. Next check will occur on the next server reboot.");
                        BroadcastPropertyEvent(
                            city,
                            "City Citizen Warning",
                            $"This city is below the required citizen count and may expire on a future restart if the count is not restored.\n" +
                            $"**Citizens Registered**: {citizenCount:N0}\n" +
                            $"**Citizens Required**: {_citizensRequired[1]:N0}\n" +
                            $"**Grace Period Ends**: {FormatUtcDate(city.Dates[PropertyDateType.BelowRequiredCitizens])}",
                            15105570);
                    }
                }
                // This is the first restart where the city is below the required amount.
                else
                {
                    city.Dates[PropertyDateType.BelowRequiredCitizens] = now.AddHours(MinimumCitizensGracePeriodHours);

                    Log.Write(LogGroup.Property, $"City '{city.CustomName}' has fallen below the required {_citizensRequired[1]} citizens required to maintain a city. An expiration has been applied");
                    BroadcastPropertyEvent(
                        city,
                        "City Citizen Warning",
                        $"This city has fallen below the required citizen count.\n" +
                        $"**Citizens Registered**: {citizenCount:N0}\n" +
                        $"**Citizens Required**: {_citizensRequired[1]:N0}\n" +
                        $"**Grace Period Ends**: {FormatUtcDate(city.Dates[PropertyDateType.BelowRequiredCitizens])}",
                        15105570);
                }
            }
            // Otherwise they're at or above the required amount. Ensure the date is removed from the property.
            else
            {
                if (city.Dates.ContainsKey(PropertyDateType.BelowRequiredCitizens))
                {
                    city.Dates.Remove(PropertyDateType.BelowRequiredCitizens);
                }
            }

            _db.Set(city);
        }

        private void ProcessCityElections(DateTime now, WorldProperty city)
        {
            var incumbentMayorId = city.OwnerPlayerId;

            void TransferPermissions(string winnerPlayerId)
            {
                var mayorPermission = _db.Search(new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), incumbentMayorId, false)
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), city.Id, false))
                    .Single();

                _db.Delete<WorldPropertyPermission>(mayorPermission.Id);

                var winnerPermission = _db.Search(new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), winnerPlayerId, false)
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), city.Id, false))
                    .SingleOrDefault() ?? new WorldPropertyPermission
                {
                    PlayerId = winnerPlayerId,
                    PropertyId = city.Id
                };

                foreach (var permission in _catalog.GetPermissionsByPropertyType(PropertyType.City))
                {
                    winnerPermission.Permissions[permission] = true;
                    winnerPermission.GrantPermissions[permission] = true;
                }

                _db.Set(winnerPermission);
            }

            // No reason to process deleted cities. Skip.
            if (city.IsQueuedForDeletion) return;

            Log.Write(LogGroup.Property, $"Election process starting for city {city.CustomName} ({city.Id})");
            var election = _db.Search(new DBQuery<Election>()
                .AddFieldSearch(nameof(Election.PropertyId), city.Id, false))
                .SingleOrDefault();

            // Election hasn't started yet.
            if (election == null)
            {
                // It's time to start a new election.
                if (now >= city.Dates[PropertyDateType.ElectionStart])
                {
                    election = new Election
                    {
                        PropertyId = city.Id,
                        Stage = ElectionStageType.Registration
                    };

                    _db.Set(election);
                    BroadcastPropertyEvent(
                        city,
                        "City Election Open",
                        $"A new mayoral election has opened for candidate registration.\n" +
                        $"**Registration Closes**: {FormatUtcDate(city.Dates[PropertyDateType.ElectionStart].AddDays(ElectionRegistrationDays))}\n" +
                        $"**Voting Closes**: {FormatUtcDate(city.Dates[PropertyDateType.ElectionStart].AddDays(ElectionRegistrationDays + ElectionVotingDays))}");
                }
            }
            // Election has started. See if it's time to progress to the next stage.
            else
            {
                var registrationCutOff = city.Dates[PropertyDateType.ElectionStart].AddDays(ElectionRegistrationDays);
                var votingCutOff = registrationCutOff.AddDays(ElectionVotingDays);

                // We're past the voting stage.
                // Count up the votes for each candidate and determine the winner.
                // Adjust mayors if incumbent lost.
                if (now >= votingCutOff)
                {
                    var votes = new Dictionary<string, int>();
                    foreach (var voter in election.VoterSelections.Values)
                    {
                        if (!votes.ContainsKey(voter.CandidatePlayerId))
                            votes[voter.CandidatePlayerId] = 0;

                        votes[voter.CandidatePlayerId]++;
                    }

                    var orderedVotes = votes.OrderByDescending(x => x.Value).ToList();

                    // Nobody voted at all. Incumbent stays in power.
                    if (orderedVotes.Count <= 0)
                    {
                        Log.Write(LogGroup.Property, $"No one voted. Incumbent mayor '{incumbentMayorId}' stays in power.");
                        BroadcastPropertyEvent(
                            city,
                            "City Election Concluded",
                            $"The mayoral election has concluded. No votes were cast, so the incumbent mayor remains in office.\n" +
                            $"**Candidates**: {election.CandidatePlayerIds.Count:N0}\n" +
                            $"**Total Votes Cast**: 0");
                    }
                    // If top two are tied, incumbent mayor wins.
                    else if (orderedVotes.Count >= 2 && orderedVotes.ElementAt(0).Value == orderedVotes.ElementAt(1).Value)
                    {
                        Log.Write(LogGroup.Property, $"Top 2 candidates were tied. Incumbent mayor '{incumbentMayorId}' wins the election.");
                        BroadcastPropertyEvent(
                            city,
                            "City Election Concluded",
                            $"The mayoral election has concluded in a tie. The incumbent mayor remains in office.\n" +
                            $"**Candidates**: {election.CandidatePlayerIds.Count:N0}\n" +
                            $"**Total Votes Cast**: {orderedVotes.Sum(x => x.Value):N0}");
                    }
                    // Otherwise, take the person with the highest votes.
                    else
                    {
                        var winnerPlayerId = orderedVotes.ElementAt(0).Key;
                        TransferPermissions(winnerPlayerId);
                        city.OwnerPlayerId = winnerPlayerId;
                        Log.Write(LogGroup.Property, $"New mayor of {city.CustomName} is '{winnerPlayerId}'");
                        BroadcastPropertyEvent(
                            city,
                            "New Mayor Elected",
                            $"The mayoral election has concluded and a new mayor has been elected.\n" +
                            $"**Candidates**: {election.CandidatePlayerIds.Count:N0}\n" +
                            $"**Total Votes Cast**: {orderedVotes.Sum(x => x.Value):N0}");
                    }

                    Log.Write(LogGroup.Property, $"Vote Counts:");
                    foreach (var (candidatePlayerId, voteCount) in orderedVotes)
                    {
                        Log.Write(LogGroup.Property, $"{candidatePlayerId}: {voteCount} votes");
                    }

                    // The next election should occur in 3 weeks from the end of this election.
                    // To avoid timing issues between server restarts, we use the start of this election as a reference point.
                    city.Dates[PropertyDateType.ElectionStart] = city.Dates[PropertyDateType.ElectionStart]
                        .AddDays(ElectionRegistrationDays + ElectionVotingDays) // This gets us to the end of the election
                        .AddDays(21); // Then add another 3 weeks.

                    _db.Set(city);
                    _db.Delete<Election>(election.Id);
                }
                // Registration cut-off has passed.
                // If no one has registered, the incumbent mayor wins by default.
                // If only one person has registered, they become mayor without proceeding to the voting stage.
                //     -> If this was a different person from the incumbent, they receive mayor power immediately.
                // Otherwise, the election proceeds to the voting stage.
                else if (now >= registrationCutOff)
                {
                    // In the event that absolutely no one ran for election,
                    // the incumbent mayor stays in power and another election is scheduled
                    // three weeks from now.
                    if (election.CandidatePlayerIds.Count <= 0)
                    {
                        city.Dates[PropertyDateType.ElectionStart] = city.Dates[PropertyDateType.ElectionStart]
                            .AddDays(ElectionRegistrationDays) // This gets us to the end of the registration period
                            .AddDays(21); // Then add another 3 weeks.

                        _db.Set(city);

                        _db.Delete<Election>(election.Id);
                        Log.Write(LogGroup.Property, $"No one ran for this election. Existing mayor '{incumbentMayorId}' wins by default.");
                        BroadcastPropertyEvent(
                            city,
                            "City Election Concluded",
                            $"The mayoral election has concluded. No candidates registered, so the incumbent mayor remains in office.\n" +
                            $"**Next Election Opens**: {FormatUtcDate(city.Dates[PropertyDateType.ElectionStart])}");
                    }
                    // In the event only one person ran for election, they automatically win
                    // and the power shift occurs immediately. Another election is scheduled
                    // three weeks from now.
                    else if(election.CandidatePlayerIds.Count == 1)
                    {
                        var winnerPlayerId = election.CandidatePlayerIds[0];

                        // The winner was the incumbent mayor. No changes are needed.
                        if (winnerPlayerId == incumbentMayorId)
                        {
                            Log.Write(LogGroup.Property, $"Incumbent mayor '{incumbentMayorId}' ran unopposed. They retain mayor status.");
                            BroadcastPropertyEvent(
                                city,
                                "City Election Concluded",
                                $"The mayoral election has concluded. The incumbent mayor ran unopposed and remains in office.\n" +
                                $"**Next Election Opens**: {FormatUtcDate(city.Dates[PropertyDateType.ElectionStart].AddDays(ElectionRegistrationDays).AddDays(21))}");
                        }
                        // Someone new won. Transfer mayor permissions over to the new player.
                        else
                        {
                            city.OwnerPlayerId = winnerPlayerId;
                            TransferPermissions(winnerPlayerId);
                            Log.Write(LogGroup.Property, $"Only one person '{winnerPlayerId}' ran for mayor. They win by default.");
                            BroadcastPropertyEvent(
                                city,
                                "New Mayor Elected",
                                $"The mayoral election has concluded. A single candidate ran unopposed and has become mayor.\n" +
                                $"**Next Election Opens**: {FormatUtcDate(city.Dates[PropertyDateType.ElectionStart].AddDays(ElectionRegistrationDays).AddDays(21))}");
                        }

                        city.Dates[PropertyDateType.ElectionStart] = city.Dates[PropertyDateType.ElectionStart]
                            .AddDays(ElectionRegistrationDays) // This gets us to the end of the registration period
                            .AddDays(21); // Then add another 3 weeks.

                        _db.Set(city);
                        _db.Delete<Election>(election.Id);
                    }
                    // We're past the registration window. Move into the voting stage.
                    else
                    {
                        election.Stage = ElectionStageType.Voting;

                        _db.Set(election);
                        Log.Write(LogGroup.Property, $"City '{city.CustomName}' ({city.Id}) has progressed into the Voting stage of the election.");
                        BroadcastPropertyEvent(
                            city,
                            "City Election Voting Open",
                            $"Mayoral candidate registration has closed and voting is now open to citizens.\n" +
                            $"**Candidates**: {election.CandidatePlayerIds.Count:N0}\n" +
                            $"**Voting Closes**: {FormatUtcDate(votingCutOff)}");
                    }

                }
            }
        }

        private void ProcessCityLevel(WorldProperty city)
        {
            Log.Write(LogGroup.Property, $"Processing city level for '{city.CustomName}' ({city.Id})...");

            var mayor = _db.Get<Player>(city.OwnerPlayerId);
            var citizenCount = _db.SearchCount(new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false));
            var currentLevel = city.Upgrades[PropertyUpgradeType.CityLevel];
            var mayorLevel = mayor.Perks.ContainsKey(PerkType.CityManagement)
                ? mayor.Perks[PerkType.CityManagement] + 1
                : 1;

            // Mayor's perk level has fallen below the city level.
            if (mayorLevel < currentLevel)
            {
                currentLevel = mayorLevel;
                Log.Write(LogGroup.Property, $"City level reduced to {currentLevel} because mayor's perk level is {mayorLevel}");
            }

            var maxLevelThisCycle = 1;
            for (var level = 1; level <= 5; level++)
            {
                // Mayor can't support a higher city level.
                if (level > mayorLevel)
                {
                    Log.Write(LogGroup.Property, $"Mayor cannot support city level {level} or higher.");
                    break;
                }

                // The citizen count requirements are met
                if (citizenCount >= _citizensRequired[level])
                {
                    maxLevelThisCycle = level;
                    Log.Write(LogGroup.Property, $"Meets citizen requirement for level {level} (Required amount: {_citizensRequired[level]})");
                }
            }

            // Current level is higher than max level this cycle. Drop it down that value.
            if (currentLevel > maxLevelThisCycle)
            {
                currentLevel = maxLevelThisCycle;
                Log.Write(LogGroup.Property, $"City level dropped to {maxLevelThisCycle}");
            }

            // Upkeep hasn't been paid. City isn't eligible to increase in level.
            if (city.Upkeep > 0)
            {
                Log.Write(LogGroup.Property, $"Unable to upgrade city because upkeep hasn't been fully paid.");
            }
            // The city can increase in level and upkeep has been paid. Perform the upgrade now.
            else if (currentLevel < maxLevelThisCycle)
            {
                currentLevel++;
                Log.Write(LogGroup.Property, $"City increased by one level this cycle.");
            }

            Log.Write(LogGroup.Property, $"City level changed to {currentLevel} from {city.Upgrades[PropertyUpgradeType.CityLevel]}");
            city.Upgrades[PropertyUpgradeType.CityLevel] = currentLevel;
            _db.Set(city);

            Log.Write(LogGroup.Property, $"Finished processing city level for '{city.CustomName}' ({city.Id})");
        }

        private void ProcessUpkeep(DateTime now, WorldProperty city)
        {
            Log.Write(LogGroup.Property, $"Processing city '{city.CustomName}' ({city.Id}) upkeep...");

            // If upkeep wasn't fully paid for this week, process the destruction date
            if (city.Upkeep > 0)
            {
                Log.Write(LogGroup.Property, $"City upkeep was not paid for the past week.");

                // This is a consecutive week in which upkeep wasn't paid. Check if it's time to destroy the city.
                if (city.Dates.ContainsKey(PropertyDateType.DisrepairDestruction))
                {
                    if (now >= city.Dates[PropertyDateType.DisrepairDestruction])
                    {
                        Log.Write(LogGroup.Property, $"City upkeep was not paid for 30 days. City is marked for destruction.");
                        city.IsQueuedForDeletion = true;
                        BroadcastPropertyEvent(
                            city,
                            "City Upkeep Expiration",
                            $"This city has been marked for removal because upkeep remained unpaid past the disrepair deadline.\n" +
                            $"**Upkeep Status**: Unpaid past deadline\n" +
                            $"**Disrepair Deadline**: {FormatUtcDate(city.Dates[PropertyDateType.DisrepairDestruction])}",
                            15158332);
                        _db.Set(city);
                        return;
                    }
                    else
                    {
                        BroadcastPropertyEvent(
                            city,
                            "City Upkeep Warning",
                            $"This city still has unpaid upkeep and may expire if it is not paid before the disrepair deadline.\n" +
                            $"**Upkeep Status**: Unpaid\n" +
                            $"**Disrepair Deadline**: {FormatUtcDate(city.Dates[PropertyDateType.DisrepairDestruction])}",
                            15105570);
                    }
                }
                else
                {
                    city.Dates[PropertyDateType.DisrepairDestruction] = now.AddDays(30);
                    Log.Write(LogGroup.Property, $"This is the first week upkeep wasn't paid. Destruction will occur on {city.Dates[PropertyDateType.DisrepairDestruction]:yyyy-MM-dd hh:mm:ss}");
                    BroadcastPropertyEvent(
                        city,
                        "City Upkeep Warning",
                        $"This city has unpaid upkeep and has entered disrepair.\n" +
                        $"**Upkeep Status**: Unpaid\n" +
                        $"**Disrepair Deadline**: {FormatUtcDate(city.Dates[PropertyDateType.DisrepairDestruction])}",
                        15105570);
                }

            }
            // Otherwise upkeep has been paid. Remove the disrepair destruction date if it exists.
            else
            {
                if (city.Dates.ContainsKey(PropertyDateType.DisrepairDestruction))
                {
                    city.Dates.Remove(PropertyDateType.DisrepairDestruction);
                    Log.Write(LogGroup.Property, $"City upkeep was paid. Removing destruction date.");
                }
            }

            // Calculate new upkeep price for this week.
            var dbMayor = _db.Get<Player>(city.OwnerPlayerId);
            var upkeepReductionPercent = dbMayor.Perks.ContainsKey(PerkType.Upkeep)
                ? dbMayor.Perks[PerkType.Upkeep] * 0.05f
                : 0;
            var layout = _catalog.GetLayoutByType(city.Layout);
            const int UpgradeBasePrice = 10000;
            var basePrice = layout.PricePerDay * 7;
            basePrice -= (int)(basePrice * upkeepReductionPercent);

            var upgradePrice =
                (city.Upgrades[PropertyUpgradeType.MedicalCenterLevel] - 1) * UpgradeBasePrice +
                (city.Upgrades[PropertyUpgradeType.StarportLevel] - 1) * UpgradeBasePrice +
                (city.Upgrades[PropertyUpgradeType.CantinaLevel] - 1) * UpgradeBasePrice;

            Log.Write(LogGroup.Property, $"Weekly upkeep calcuated to be: {basePrice + upgradePrice} credits.");
            city.Upkeep += basePrice + upgradePrice;
            _db.Set(city);
            Log.Write(LogGroup.Property, $"Total upkeep owed: {city.Upkeep} credits.");
            BroadcastPropertyEvent(
                city,
                "City Upkeep Due",
                $"Weekly upkeep has been assessed for this city.\n" +
                $"**Upkeep Status**: Payment due\n" +
                $"**Next Upkeep Check**: {FormatUtcDate(city.Dates[PropertyDateType.Upkeep].AddDays(7))}");

            Log.Write(LogGroup.Property, $"Finished processing city upkeep for '{city.CustomName}' ({city.Id})");
        }

        private void ProcessCitizenshipFees(WorldProperty city)
        {
            Log.Write(LogGroup.Property, $"Processing citizenship fees for '{city.CustomName}' ({city.Id})");

            var citizenQuery = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            var citizenCount = (int)_db.SearchCount(citizenQuery);
            var citizens = _db.Search(citizenQuery.AddPaging(citizenCount, 0))
                .ToList();

            foreach (var citizen in citizens)
            {
                citizen.PropertyOwedTaxes += city.Taxes[PropertyTaxType.Citizenship];
                Log.Write(LogGroup.Property, $"Citizen '{citizen.Name}' owes an additional {city.Taxes[PropertyTaxType.Citizenship]} credits for a total of {citizen.PropertyOwedTaxes} credits");
                _db.Set(citizen);
            }

            Log.Write(LogGroup.Property, $"Finished processing citizenship fees for '{city.CustomName}' ({city.Id})");
        }

        /// <summary>
        /// Retrieves the name of a particular city level.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>A string representing the city level.</returns>
        public string GetCityLevelName(int level)
        {
            switch (level)
            {
                case 1:
                    return "Outpost";
                case 2:
                    return "Village";
                case 3:
                    return "Township";
                case 4:
                    return "City";
                case 5:
                    return "Metropolis";
            }

            return string.Empty;
        }

        /// <summary>
        /// Retrieves the number of citizens required for the next city level.
        /// If level isn't supported, -1 will be returned.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>The number of citizens required to level up the city.</returns>
        public int GetCitizensRequiredForNextCityLevel(int level)
        {
            return _citizensRequired.ContainsKey(level)
                ? _citizensRequired[level]
                : -1;
        }

        /// <summary>
        /// Retrieves the effective upgrade level of a city, taking into account the city's overall level
        /// into the calculation.
        /// </summary>
        /// <param name="cityId">The property Id of the city.</param>
        /// <param name="upgradeType">The type of upgrade to check</param>
        /// <returns>A value ranging between 0 and 5.</returns>
        public int GetEffectiveUpgradeLevel(string cityId, PropertyUpgradeType upgradeType)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return 0;

            var property = _db.Get<WorldProperty>(cityId);
            if (property == null)
                return 0;

            if (!property.Upgrades.ContainsKey(upgradeType))
                return 0;

            var cityLevel = property.Upgrades[PropertyUpgradeType.CityLevel];
            var upgradeLevel = property.Upgrades[upgradeType];
            var effectiveLevel = cityLevel < upgradeLevel ? cityLevel : upgradeLevel;
            var structureType = StructureType.Invalid;

            switch (upgradeType)
            {
                case PropertyUpgradeType.MedicalCenterLevel:
                    structureType = StructureType.MedicalCenter;
                    break;
                case PropertyUpgradeType.StarportLevel:
                    structureType = StructureType.Starport;
                    break;
                case PropertyUpgradeType.CantinaLevel:
                    structureType = StructureType.Cantina;
                    break;
            }

            if (structureType == StructureType.Invalid)
                return 0;

            // At least one building property of the given type must exist within the city.
            var buildingCount = _db.SearchCount(new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)structureType)
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), cityId, false));

            if (buildingCount <= 0)
                return 0;

            return effectiveLevel;
        }
    }
}

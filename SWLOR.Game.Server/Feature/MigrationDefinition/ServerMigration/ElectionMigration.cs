using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The election part of the Player City Revamp server migration (<see cref="_32_PlayerCityRevamp"/>): gives an
/// election that is already open its kind and stage dates, and gives current citizens a citizen-since time so they
/// can vote at once. An election that is running keeps the calendar it opened under (14 days of registration, 7 of
/// voting), so the shorter calendar never moves its cutoff. It adds no platforms: an election already in Voting finishes
/// without them, and its winner fills Edict slots from Manage City; one still in Registration accepts platforms, and a
/// candidate who registered earlier has none until they edit theirs (a winner with none leaves the city's Edicts alone).
/// </summary>
public static class ElectionMigration
{
    /// <summary>The registration length elections opened before the revamp ran under.</summary>
    public const int LegacyRegistrationDays = 14;

    /// <summary>The voting length elections opened before the revamp ran under.</summary>
    public const int LegacyVotingDays = 7;

    /// <summary>
    /// The citizen-since time given to citizens who joined before the revamp: long before any election opened, so they
    /// qualify to vote at once.
    /// </summary>
    public static readonly DateTime LongStandingCitizenUtc = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Gives an open election its kind and the stage dates its old calendar implies. Returns true if it changed. An
    /// election that already has its dates is left alone, so running the migration again changes nothing.
    /// </summary>
    /// <param name="election">The election.</param>
    /// <param name="cityElectionStartUtc">The day the city's election began, or null if the city is gone.</param>
    public static bool MigrateElection(Election election, DateTime? cityElectionStartUtc)
    {
        var changed = false;

        if (election.Kind == ElectionKind.Invalid)
        {
            election.Kind = ElectionKind.Regular;
            changed = true;
        }

        if (election.RegistrationOpenedUtc == default)
        {
            var opened = cityElectionStartUtc ?? election.DateCreated;
            election.RegistrationOpenedUtc = opened;
            election.RegistrationClosesUtc = opened.AddDays(LegacyRegistrationDays);
            election.VotingClosesUtc = election.RegistrationClosesUtc.AddDays(LegacyVotingDays);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Gives a current citizen a citizen-since time if they have none. Returns true if it changed.
    /// </summary>
    public static bool MigrateCitizen(Player player)
    {
        if (string.IsNullOrWhiteSpace(player.CitizenPropertyId) ||
            player.CitizenPropertyId == Guid.Empty.ToString() ||
            player.CitizenSinceUtc.HasValue)
            return false;

        player.CitizenSinceUtc = LongStandingCitizenUtc;
        return true;
    }

    public static void Run(int version)
    {
        var cityQuery = new DBQuery<WorldProperty>()
            .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City);
        var cityCount = checked((int)DB.SearchCount(cityQuery));
        var progress = new ServerMigrationProgress(
            version,
            ServerMigrationProgress.CountRecords<Election>(),
            ("WorldProperty", cityCount));

        var electionCount = progress.BeginSection<Election>();
        var cities = cityCount > 0
            ? DB.Search(cityQuery.AddPaging(cityCount, 0)).ToList()
            : new System.Collections.Generic.List<WorldProperty>();
        if (electionCount > 0)
        {
            foreach (var election in DB.Search(new DBQuery<Election>().AddPaging(electionCount, 0)).ToList())
            {
                var city = cities.FirstOrDefault(x => x.Id == election.PropertyId);
                DateTime? start = city != null && city.Dates.TryGetValue(PropertyDateType.ElectionStart, out var date)
                    ? date
                    : null;

                var changed = MigrateElection(election, start);
                if (changed)
                    DB.Set(election);

                progress.RecordProcessed(changed);
            }
        }
        progress.FinishSection();

        progress.BeginSection<WorldProperty>();
        foreach (var city in cities)
        {
            var citizenQuery = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false);
            var citizenCount = checked((int)DB.SearchCount(citizenQuery));
            var changedCitizens = 0;
            if (citizenCount > 0)
            {
                foreach (var citizen in DB.Search(citizenQuery.AddPaging(citizenCount, 0)).ToList())
                {
                    if (!MigrateCitizen(citizen))
                        continue;

                    DB.Set(citizen);
                    changedCitizens++;
                }
            }

            progress.RecordProcessed(changedCitizens > 0);
        }
        progress.FinishSection();

        Log.Write(LogGroup.Migration, $"Migration #{version}: elections and citizens updated for {cities.Count} cities.", true);
    }
}

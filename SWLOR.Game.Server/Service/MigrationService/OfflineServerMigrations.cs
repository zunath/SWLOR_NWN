using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// Applies pending server migrations to the database without the game server, in the order the
/// server would run them at boot. It runs only when every pending migration is an
/// <see cref="IOfflineServerMigration"/>, and records the new migration version only after all of
/// them succeed, so the server's next boot skips them. Saves replace whole records, so the game
/// server must be stopped while this runs.
/// </summary>
public static class OfflineServerMigrations
{
    public sealed record Result(int StartingVersion, int CompletedVersion, IReadOnlyList<string> Blockers);

    public static Result Run(OfflineMigrationOptions options)
    {
        var startingVersion = GetServerConfiguration().MigrationVersion;
        var migrations = typeof(IServerMigration).Assembly.GetTypes()
            .Where(type => typeof(IServerMigration).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(type => (IServerMigration)Activator.CreateInstance(type));
        var (pending, blockers) = Plan(migrations, startingVersion);

        if (pending.Count == 0)
        {
            options.Write($"No server migrations are pending above version {startingVersion}.");
            return new Result(startingVersion, startingVersion, blockers);
        }
        if (blockers.Count > 0)
        {
            options.Write($"Nothing was run. These pending migrations need the game server: {string.Join(", ", blockers)}.");
            return new Result(startingVersion, startingVersion, blockers);
        }

        var stopwatch = new Stopwatch();
        foreach (IOfflineServerMigration migration in pending)
        {
            options.Write($"Starting offline server migration ({migration.ExecutionType}) #{migration.Version} {migration.GetType().Name}.");
            stopwatch.Restart();
            migration.MigrateOffline(options);
            options.Write($"Offline server migration #{migration.Version} completed. (Took {stopwatch.ElapsedMilliseconds}ms)");
        }

        if (options.DryRun)
        {
            options.Write($"Dry run: nothing was saved and the migration version stays at {startingVersion}.");
            return new Result(startingVersion, startingVersion, blockers);
        }

        var completedVersion = pending.Max(migration => migration.Version);
        var config = GetServerConfiguration();
        config.MigrationVersion = completedVersion;
        DB.Set(config);
        options.Write($"Migration version advanced from {startingVersion} to {completedVersion}.");
        return new Result(startingVersion, completedVersion, blockers);
    }

    /// <summary>
    /// Orders the migrations above <paramref name="version"/> as boot runs them (every PostDatabaseLoad
    /// migration before any PostCacheLoad one) and names those that need the game server.
    /// </summary>
    public static (IReadOnlyList<IServerMigration> Pending, IReadOnlyList<string> Blockers) Plan(
        IEnumerable<IServerMigration> migrations, int version)
    {
        var pending = migrations
            .Where(migration => migration.Version > version)
            .OrderBy(migration => migration.ExecutionType)
            .ThenBy(migration => migration.Version)
            .ToList();
        var blockers = pending.Where(migration => migration is not IOfflineServerMigration)
            .Select(migration => $"#{migration.Version} {migration.GetType().Name} ({migration.ExecutionType})")
            .ToList();
        return (pending, blockers);
    }

    private static ServerConfiguration GetServerConfiguration()
        => DB.Get<ServerConfiguration>("SWLOR_CONFIG") ?? new ServerConfiguration();
}

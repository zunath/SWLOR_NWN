using System;
using System.Diagnostics;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.CLI
{
    /// <summary>
    /// Applies pending server migrations to a stopped server's Redis database, so the server does not
    /// run them at boot. Reports what would change unless asked to write. See README_OfflineMigrations.md.
    /// </summary>
    internal class OfflineMigrationRunner
    {
        public int Process(string redisHost, bool write, int parallelism)
        {
            if (!string.IsNullOrWhiteSpace(redisHost))
                Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", redisHost);
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NWNX_REDIS_HOST")))
            {
                Console.Error.WriteLine("Pass --redis <host:port> or set NWNX_REDIS_HOST.");
                return 1;
            }

            Console.WriteLine(write
                ? "Writing migrated records. The game server must be stopped: it would overwrite or lose these changes."
                : "Dry run: records are converted and counted but not saved. Add --write to save them.");
            DB.Load();

            var stopwatch = Stopwatch.StartNew();
            var result = OfflineServerMigrations.Run(new OfflineMigrationOptions
            {
                DryRun = !write,
                MaxDegreeOfParallelism = parallelism > 0 ? parallelism : Environment.ProcessorCount,
                Write = message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}"),
            });
            Console.WriteLine($"Finished in {stopwatch.Elapsed:hh\\:mm\\:ss}.");
            return result.Blockers.Count > 0 ? 2 : 0;
        }
    }
}

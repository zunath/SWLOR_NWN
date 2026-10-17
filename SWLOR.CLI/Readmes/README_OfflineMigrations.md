# Offline Migrations

`--migrateOffline` applies pending server migrations to a stopped server's Redis database, so the
server does not run them at boot. Item migrations that load every stored item into the game engine
take an hour or more at boot; run offline, migration 28's stored-item sweep takes under a minute on
a production copy.

## Running it

1. Stop the game server. Saves replace whole records, so a running server would overwrite or lose
   the migrated data.
2. Build the CLI from the same commit as the server build you are deploying.
3. Report what would change (nothing is saved):

   ```bash
   dotnet SWLOR.CLI.dll --migrateOffline --redis <host:port>
   ```

4. Apply it. This saves the changed records and advances `ServerConfiguration.MigrationVersion`:

   ```bash
   dotnet SWLOR.CLI.dll --migrateOffline --redis <host:port> --write
   ```

5. Start the server. It skips the migrations the runner completed.

`--redis` defaults to `NWNX_REDIS_HOST`. `--parallelism <n>` sets worker threads (default: processor
count). To reach a Redis container that publishes no port, run the CLI in the server image on the same
Docker network:

```bash
docker run --rm --network <network> -v <cli build dir>:/cli:ro --entrypoint /usr/share/dotnet/dotnet zunath/nwn-dotnet:<tag> /cli/SWLOR.CLI.dll --migrateOffline --redis redis:6379
```

Like a server boot, loading the database brings search indexes up to date, including during a dry run.

## What runs

The runner runs only when every pending server migration implements `IOfflineServerMigration`;
otherwise it lists the ones that need the game server and changes nothing. Migrations run in boot
order (every `PostDatabaseLoad` before any `PostCacheLoad`), and the version advances only after all
of them succeed. A failure leaves the version unchanged; migrations are idempotent, so rerun it or
let the server run them at boot.

Offline-capable today: migration 28 (stored-item sweep). How to write one: `Readmes/Migrations.md`,
"Offline migrations".

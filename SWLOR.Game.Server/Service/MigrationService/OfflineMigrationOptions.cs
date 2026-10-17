using System;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>Settings for running <see cref="IOfflineServerMigration"/>s outside the game server.</summary>
public sealed class OfflineMigrationOptions
{
    /// <summary>Worker threads that convert records; database reads and saves stay on the calling thread.</summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>Records loaded, converted and saved together.</summary>
    public int BatchSize { get; init; } = 1000;

    /// <summary>Converts and counts every record without saving anything or advancing the migration version.</summary>
    public bool DryRun { get; init; }

    public Action<string> Write { get; init; } = Console.WriteLine;
}

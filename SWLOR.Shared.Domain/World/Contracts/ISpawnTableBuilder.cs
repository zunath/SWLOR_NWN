using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.AI.ValueObjects;
using SWLOR.Shared.Domain.World.ValueObjects;

namespace SWLOR.Shared.Domain.World.Contracts;

public interface ISpawnTableBuilder
{
    /// <summary>
    /// Creates a new spawn table with the specified Id
    /// </summary>
    /// <param name="spawnTableId">The spawn table Id to create the table with</param>
    /// <param name="name">The name of the spawn table. This is purely for the programmer's benefit. Not used by the system.</param>
    /// <returns>A spawn table builder with the configured settings.</returns>
    ISpawnTableBuilder Create(string spawnTableId, string name = null);

    /// <summary>
    /// Sets the number of minutes before a respawn takes place.
    /// Values less than 1 will default to 1.
    /// </summary>
    /// <param name="minutes">The number of minutes before a respawn takes place.</param>
    ISpawnTableBuilder RespawnDelay(int minutes);

    /// <summary>
    /// Sets the number of minutes before a resource despawns naturally.
    /// This only applies to resource spawns (placeables).
    /// Values less than 1 will default to 180 minutes (3 hours).
    /// </summary>
    /// <param name="minutes">The number of minutes before a resource despawns.</param>
    ISpawnTableBuilder ResourceDespawnDelay(int minutes);

    /// <summary>
    /// Adds a new spawn object to this spawn table.
    /// </summary>
    /// <param name="type">The object type to spawn</param>
    /// <param name="resref">The resref of the object</param>
    /// <returns>A spawn table builder with the configured settings</returns>
    ISpawnTableBuilder AddSpawn(ObjectType type, string resref);

    /// <summary>
    /// Sets the frequency of a spawn. This modifies the likelihood of this particular object spawning
    /// based on the weight of all other objects in the same table.
    /// In laymen's terms, the higher this number is, the more likely it will appear.
    /// </summary>
    /// <param name="frequency">The frequency to set.</param>
    /// <returns>A spawn table builder with the configured settings</returns>
    ISpawnTableBuilder WithFrequency(int frequency);

    /// <summary>
    /// Specifies that this spawn object can only spawn on the provided days of week.
    /// If no days are specified, spawn will be unrestricted and can spawn on any day.
    /// These are real world days in UTC time.
    /// </summary>
    /// <param name="restrictedDaysOfWeek">The days which the spawn may appear.</param>
    /// <returns>A spawn table builder with the configured settings.</returns>
    ISpawnTableBuilder OnDayOfWeek(params DayOfWeek[] restrictedDaysOfWeek);

    /// <summary>
    /// Specifies that this spawn object can only spawn between the provided hours of day.
    /// The day value of these timespans should NOT be used or spawns will not be created.
    /// </summary>
    /// <param name="start">The starting timespan</param>
    /// <param name="end">The ending timespan</param>
    /// <returns>A spawn table builder with the configured settings.</returns>
    ISpawnTableBuilder BetweenRealWorldHours(TimeSpan start, TimeSpan end);

    /// <summary>
    /// Specifies that this spawn object can only spawn between the provided start and end
    /// game hours.
    /// </summary>
    /// <param name="startHour">The starting game hour</param>
    /// <param name="endHour">The ending game hour</param>
    /// <returns>A spawn table builder with the configured settings.</returns>
    ISpawnTableBuilder BetweenGameHours(int startHour, int endHour);

    /// <summary>
    /// Indicates that this spawn object will randomly walk when not in combat.
    /// </summary>
    /// <returns>A spawn table builder with the configured settings.</returns>
    ISpawnTableBuilder RandomlyWalks();

    /// <summary>
    /// Indicates that this spawn object will return home if they stray too far from their spawn point.
    /// </summary>
    /// <returns>A spawn table builder with the configured settings.</returns>
    ISpawnTableBuilder ReturnsHome();

    ISpawnTableBuilder PlayAnimation(DurationType duration, AnimationEvent animEvent, VisualEffectType vfx);

    /// <summary>
    /// Adds an action to run when this particular spawn is created.
    /// </summary>
    /// <param name="action">The action to run when the spawn is created.</param>
    /// <returns>A spawn table builder with the configured settings.</returns>
    ISpawnTableBuilder SpawnAction(OnSpawnDelegate action);

    /// <summary>
    /// Builds a dictionary of spawn tables.
    /// </summary>
    /// <returns>A dictionary of spawn tables</returns>
    Dictionary<string, SpawnTable> Build();
}
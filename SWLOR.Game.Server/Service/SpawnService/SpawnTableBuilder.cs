using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service.SpawnService
{
    public class SpawnTableBuilder
    {
        private readonly Dictionary<string, SpawnTable> _spawnTables = new();

        private SpawnTable ActiveTable { get; set; }
        private SpawnObject ActiveSpawn { get; set; }


        /// <summary>
        /// Creates a new spawn table with the specified Id
        /// </summary>
        /// <param name="spawnTableId">The spawn table Id to create the table with</param>
        /// <param name="name">The name of the spawn table. This is purely for the programmer's benefit. Not used by the system.</param>
        /// <returns>A spawn table builder with the configured settings.</returns>
        public SpawnTableBuilder Create(string spawnTableId, string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = $"Spawn Table {spawnTableId}";

            ActiveTable = new SpawnTable(name);
            _spawnTables[spawnTableId] = ActiveTable;

            return this;
        }

        /// <summary>
        /// Sets the number of minutes before a respawn takes place.
        /// Values less than 1 will default to 1.
        /// </summary>
        /// <param name="minutes">The number of minutes before a respawn takes place.</param>
        public SpawnTableBuilder RespawnDelay(int minutes)
        {
            if (minutes < 1) minutes = 1;
            ActiveTable.RespawnDelayMinutes = minutes;

            return this;
        }

        /// <summary>
        /// Adds a new spawn object to this spawn table.
        /// </summary>
        /// <param name="type">The object type to spawn</param>
        /// <param name="resref">The resref of the object</param>
        /// <returns>A spawn table builder with the configured settings</returns>
        public SpawnTableBuilder AddSpawn(ObjectType type, string resref)
        {
            ActiveSpawn = new SpawnObject
            {
                Type = type,
                Resref = resref,
                Weight = 10
            };
            ActiveTable.Spawns.Add(ActiveSpawn);

            return this;
        }

        /// <summary>
        /// Sets the frequency of a spawn. This modifies the likelihood of this particular object spawning
        /// based on the weight of all other objects in the same table.
        /// In laymen's terms, the higher this number is, the more likely it will appear.
        /// </summary>
        /// <param name="frequency">The frequency to set.</param>
        /// <returns>A spawn table builder with the configured settings</returns>
        public SpawnTableBuilder WithFrequency(int frequency)
        {
            if (frequency < 1) frequency = 1;

            ActiveSpawn.Weight = frequency;
            return this;
        }

        /// <summary>
        /// Specifies that this spawn object can only spawn on the provided days of week.
        /// If no days are specified, spawn will be unrestricted and can spawn on any day.
        /// These are real world days in UTC time.
        /// </summary>
        /// <param name="restrictedDaysOfWeek">The days which the spawn may appear.</param>
        /// <returns>A spawn table builder with the configured settings.</returns>
        public SpawnTableBuilder OnDayOfWeek(params DayOfWeek[] restrictedDaysOfWeek)
        {
            ActiveSpawn.RealWorldDayOfWeekRestriction = restrictedDaysOfWeek.ToList();
            return this;
        }

        /// <summary>
        /// Specifies that this spawn object can only spawn between the provided hours of day.
        /// The day value of these timespans should NOT be used or spawns will not be created.
        /// </summary>
        /// <param name="start">The starting timespan</param>
        /// <param name="end">The ending timespan</param>
        /// <returns>A spawn table builder with the configured settings.</returns>
        public SpawnTableBuilder BetweenRealWorldHours(TimeSpan start, TimeSpan end)
        {
            ActiveSpawn.RealWorldStartRestriction = start;
            ActiveSpawn.RealWorldEndRestriction = end;

            return this;
        }

        /// <summary>
        /// Specifies that this spawn object can only spawn between the provided start and end
        /// game hours.
        /// </summary>
        /// <param name="startHour">The starting game hour</param>
        /// <param name="endHour">The ending game hour</param>
        /// <returns>A spawn table builder with the configured settings.</returns>
        public SpawnTableBuilder BetweenGameHours(int startHour, int endHour)
        {
            ActiveSpawn.GameHourStartRestriction = startHour;
            ActiveSpawn.GameHourEndRestriction = endHour;

            return this;
        }

        /// <summary>
        /// Indicates that this spawn object will randomly walk when not in combat.
        /// </summary>
        /// <returns>A spawn table builder with the configured settings.</returns>
        public SpawnTableBuilder RandomlyWalks()
        {
            ActiveSpawn.AIFlags |= AIFlag.RandomWalk;

            return this;
        }

        /// <summary>
        /// Indicates that this spawn object will Walk Way Points when not in combat.
        /// </summary>
        /// <returns>A spawn table builder with the configured settings.</returns>
        public SpawnTableBuilder WalksWaypoints()
        {
            ActiveSpawn.AIFlags |= AIFlag.WalkWaypoints;

            return this;
        }

        /// <summary>
        /// Indicates that this spawn object will return home if they stray too far from their spawn point.
        /// </summary>
        /// <returns>A spawn table builder with the configured settings.</returns>
        public SpawnTableBuilder ReturnsHome()
        {
            ActiveSpawn.AIFlags |= AIFlag.ReturnHome;

            return this;
        }

        public SpawnTableBuilder PlayAnimation(DurationType duration, AnimationEvent animEvent, VisualEffect vfx)
        {
            var animation = new Animator()
            {
                Duration = duration,
                Event = animEvent,
                Vfx = vfx
            };
            ActiveSpawn.Animators.Add(animation);
            return this;
        }

        /// <summary>
        /// Adds an action to run when this particular spawn is created.
        /// </summary>
        /// <param name="action">The action to run when the spawn is created.</param>
        /// <returns>A spawn table builder with the configured settings.</returns>
        public SpawnTableBuilder SpawnAction(OnSpawnDelegate action)
        {
            ActiveSpawn.OnSpawnActions.Add(action);

            return this;
        }

        /// <summary>
        /// Builds a dictionary of spawn tables.
        /// </summary>
        /// <returns>A dictionary of spawn tables</returns>
        public Dictionary<string, SpawnTable> Build()
        {
            return _spawnTables;
        }
    }
}

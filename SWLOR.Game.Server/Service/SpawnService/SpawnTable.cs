using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpawnService
{
    public class SpawnTable
    {
        public string Name { get; set; }
        public int RespawnDelayMinutes { get; set; }
        public int ResourceDespawnMinutes { get; set; }
        public List<SpawnObject> Spawns { get; set; }

        public SpawnTable(string name)
        {
            Name = name;
            RespawnDelayMinutes = Spawn.DefaultRespawnMinutes;
            ResourceDespawnMinutes = 180; // Default: 3 hours for resources
            Spawns = new List<SpawnObject>();
        }

        /// <summary>
        /// Retrieves the next spawn resref, object type, and AI flags based on the rules for this specific spawn table.
        /// </summary>
        /// <returns>The detailed spawn object to spawn.</returns>
        public SpawnObject GetNextSpawn()
        {
            var selectedObject = SelectRandomSpawnObject();
            if (selectedObject == null)
                return new SpawnObject
                {
                    Type = ObjectType.All,
                    Resref = string.Empty,
                    AIFlags = AIFlag.None,
                    Animators = new List<IAnimator>()
                };

            return selectedObject;
        }

        /// <summary>
        /// Retrieves a random spawn object based on weight.
        /// </summary>
        /// <returns></returns>
        private SpawnObject SelectRandomSpawnObject()
        {
            var filteredList = FilterSpawnObjects();
            if (filteredList.Count <= 0) return null;

            var weights = filteredList.Select(s => s.Weight).ToArray();
            var index = Random.GetRandomWeightedIndex(weights);
            
            // If GetRandomWeightedIndex returns -1 (no valid weights), return null
            if (index == -1 || index >= filteredList.Count)
                return null;
                
            return filteredList.ElementAt(index);
        }

        /// <summary>
        /// Filters the list of spawn objects based on criteria such as
        /// the time of game day, the real-world day, etc.
        /// It is possible for this list to be empty so account for that accordingly.
        /// </summary>
        /// <returns>A filtered list of spawn objects.</returns>
        private List<SpawnObject> FilterSpawnObjects()
        {
            var list = Spawns.ToList();
            var now = DateTime.UtcNow;
            var dayOfWeek = now.DayOfWeek;
            var timeOfDay = now.TimeOfDay;
            var gameHour = GetTimeHour();

            for (var index = list.Count - 1; index >= 0; index--)
            {
                var obj = list.ElementAt(index);

                // Day of week restriction
                if (obj.RealWorldDayOfWeekRestriction.Count > 0 &&
                    !obj.RealWorldDayOfWeekRestriction.Contains(dayOfWeek))
                {
                    list.RemoveAt(index);
                    continue;
                }

                // Real world time range restriction
                if ((obj.RealWorldStartRestriction != null && obj.RealWorldEndRestriction != null) &&
                    !(timeOfDay >= obj.RealWorldStartRestriction && timeOfDay <= obj.RealWorldEndRestriction))
                {
                    list.RemoveAt(index);
                    continue;
                }

                // Game hour restriction
                if ((obj.GameHourStartRestriction != -1 && obj.GameHourEndRestriction != -1) &&
                    !(gameHour >= obj.GameHourStartRestriction && gameHour <= obj.GameHourEndRestriction))
                {
                    list.RemoveAt(index);
                    continue;
                }
            }

            return list;
        }
    }
}

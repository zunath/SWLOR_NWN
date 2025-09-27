using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.AI.Enums;
using SWLOR.Shared.Domain.Character.Contracts;

namespace SWLOR.Shared.Domain.World.ValueObjects
{
    public class SpawnTable
    {
        public string Name { get; set; }
        public int RespawnDelayMinutes { get; set; }
        public int ResourceDespawnMinutes { get; set; }
        public List<SpawnObject> Spawns { get; set; }

        public SpawnTable(string name, int defaultRespawnMinutes = 30)
        {
            Name = name;
            RespawnDelayMinutes = defaultRespawnMinutes;
            ResourceDespawnMinutes = 180; // Default: 3 hours for resources
            Spawns = new List<SpawnObject>();
        }

        /// <summary>
        /// Retrieves the next spawn resref, object type, and AI flags based on the rules for this specific spawn table.
        /// </summary>
        /// <param name="randomService">The random service to use for weighted selection</param>
        /// <returns>The detailed spawn object to spawn.</returns>
        public SpawnObject GetNextSpawn(IRandomService randomService)
        {
            var selectedObject = SelectRandomSpawnObject(randomService);
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
        /// <param name="randomService">The random service to use for weighted selection</param>
        /// <returns></returns>
        private SpawnObject SelectRandomSpawnObject(IRandomService randomService)
        {
            var filteredList = FilterSpawnObjects();
            if (filteredList.Count <= 0) return null;

            var weights = filteredList.Select(s => s.Weight).ToArray();
            var index = randomService.GetRandomWeightedIndex(weights);
            
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

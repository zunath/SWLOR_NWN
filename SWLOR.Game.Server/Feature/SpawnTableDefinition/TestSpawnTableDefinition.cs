using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnTableDefinition
{
    public class TestSpawnTableDefinition: ISpawnTableDefinition
    {
        public Dictionary<int, SpawnTable> BuildSpawnTables()
        {
            var builder = new SpawnTableBuilder()

                .Create(1, "Goblin Spawns")
                .AddSpawn(ObjectType.Creature, "nw_goblina")
                .WithFrequency(50)
                .OnDayOfWeek(DayOfWeek.Saturday)
                .BetweenRealWorldHours(new TimeSpan(18, 40, 0), new TimeSpan(20, 0, 0))
                
                .AddSpawn(ObjectType.Creature, "nw_hobgoblin001")
                .WithFrequency(50)
                .OnDayOfWeek(DayOfWeek.Saturday)
                .BetweenGameHours(13, 14)
                
                .Create(2, "Another spawn")
                .AddSpawn(ObjectType.Creature, "nw_goblina");

            return builder.Build();
        }
    }
}

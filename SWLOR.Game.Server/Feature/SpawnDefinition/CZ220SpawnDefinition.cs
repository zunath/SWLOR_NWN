using System.Collections.Generic;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class CZ220SpawnDefinition: ISpawnListDefinition
    {
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            var builder = new SpawnTableBuilder();
            DroidSpawns(builder);
            MynockSpawns(builder);
            ColicoidExperimentSpawn(builder);
            CZ220BreakerYard(builder);

            return builder.Build();
        }

        private void DroidSpawns(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_DROIDS", "CZ-220 Droids")
                .AddSpawn(ObjectType.Creature, "malsecdroid")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()
                .PlayAnimation(DurationType.Instant, AnimationEvent.CreatureOnDeath, VisualEffect.Fnf_Fireball)

                .AddSpawn(ObjectType.Creature, "malspiderdroid")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()
                .PlayAnimation(DurationType.Instant, AnimationEvent.CreatureOnDeath, VisualEffect.Fnf_Fireball);
        }

        private void MynockSpawns(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_MYNOCKS", "CZ-220 Mynocks")
                .AddSpawn(ObjectType.Creature, "mynock")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "czcryo_mynock")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void ColicoidExperimentSpawn(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_COLICOID_EXPERIMENT", "Colicoid Experiment")
                .AddSpawn(ObjectType.Creature, "colicoidexp")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void CZ220BreakerYard(SpawnTableBuilder builder)
        {
            builder.Create("CAPSTONE_CZ220_BREAKER_YARD", "CZ-220 Breaker Yard - General Capstone")
                .AddSpawn(ObjectType.Creature, "cp_adamguard_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_adamguard_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_adamguard_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_scraplock_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_scraplock_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_scraplock_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_worldbrk_ad")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_worldbrk_sp")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cp_worldbrk_ic")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome();

            // Named rare elite droids live in a dedicated rare table (tagged waypoint in the
            // dungeon), so the capstone lesson table stays exactly the general enemy steps.
            builder.Create("CZ220_BREAKER_YARD_RARES", "CZ-220 Breaker Yard - Rare Elites")
                .AddSpawn(ObjectType.Creature, "bulwark")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "slagborn")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "demolisherzr9")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}

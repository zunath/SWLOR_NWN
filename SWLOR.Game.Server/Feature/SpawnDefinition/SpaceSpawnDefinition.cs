﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class SpaceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            ViscaraOrbit();
            MonCalaOrbit();
            HutlarOrbit();
            TatooineOrbit();

            return _builder.Build();
        }

        private void ViscaraOrbit()
        {
            _builder.Create("SPACE_VISCARA_ORBIT", "Space - Viscara Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_sybil")
                .WithFrequency(40)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "pirate_terror")
                .WithFrequency(5)
                .RandomlyWalks()
                
                .AddSpawn(ObjectType.Creature, "pirate_night")
                .WithFrequency(1)
                .RandomlyWalks();
        }

        private void MonCalaOrbit()
        {
            _builder.Create("SPACE_MONCALA_ORBIT", "Space - Mon Cala Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_night")
                .WithFrequency(40)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "pirate_ferron")
                .WithFrequency(5)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "pirate_storm")
                .WithFrequency(1)
                .RandomlyWalks();
        }

        private void HutlarOrbit()
        {
            _builder.Create("SPACE_HUTLAR_ORBIT", "Space - Hutlar Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_storm")
                .WithFrequency(40)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "pirate_ranger")
                .WithFrequency(5)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "pirate_hammer")
                .WithFrequency(1)
                .RandomlyWalks();
        }

        private void TatooineOrbit()
        {
            _builder.Create("SPACE_TATOOINE_ORBIT", "Space - Tatooine Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_hammer")
                .WithFrequency(40)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "pirate_drake")
                .WithFrequency(5)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "pirate_borealis")
                .WithFrequency(1)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "pirate_eleyna")
                .WithFrequency(1)
                .RandomlyWalks();
        }
    }
}
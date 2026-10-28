using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class SpaceResourceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            ViscaraOrbit();
            MonCalaOrbit();
            HutlarOrbit();
            TatooineOrbit();
            KorribanOrbit();
            DathomirOrbit();
            DantooineOrbit();
            MidRimNebula();
            ViscaraAsteroidBelt();
            MidRimNebulaZone();
            KorribanDebrisField();

            return _builder.Build();
        }

        private void ViscaraOrbit()
        {
            _builder.Create("SPACE_RESOURCES_VISCARA_ORBIT", "Space Resources - Viscara Orbit")
                .ResourceDespawnDelay(180) // 3 hours for basic space asteroids
                .AddSpawn(ObjectType.Placeable, "spc_asteroid_til")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_currian")
                .WithFrequency(5);
        }

        private void MonCalaOrbit()
        {
            _builder.Create("SPACE_RESOURCES_MONCALA_ORBIT", "Space Resources - Mon Cala Orbit")
                .ResourceDespawnDelay(150) // 2.5 hours for mid-tier space asteroids
                .AddSpawn(ObjectType.Placeable, "spc_asteroid_til")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_currian")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(5);
        }

        private void HutlarOrbit()
        {
            _builder.Create("SPACE_RESOURCES_HUTLAR_ORBIT", "Space Resources - Hutlar Orbit")
                .ResourceDespawnDelay(120) // 2 hours for higher-tier space asteroids
                .AddSpawn(ObjectType.Placeable, "asteroid_currian")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(5);
        }

        private void TatooineOrbit()
        {
            _builder.Create("SPACE_RESOURCES_TATOOINE_ORBIT", "Space Resources - Tatooine Orbit")
                .ResourceDespawnDelay(90) // 1.5 hours for high-tier space asteroids
                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(5);
        }

        private void KorribanOrbit()
        {
            _builder.Create("SPACE_RESOURCES_KORRIBAN_ORBIT", "Space Resources - Korriban Orbit")
                .ResourceDespawnDelay(60) // 1 hour for rare top-tier space asteroids
                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(25)

                .AddSpawn(ObjectType.Placeable, "asteroid_arda")
                .WithFrequency(1);
        }

        /// <summary>
        /// The Mid Rim nebula on the Viscara starlane (space_midrim1): Korriban's asteroids plus Tibanna Gas clouds of
        /// tiers 3 and 4. It keeps them beside the denser Mid Rim Nebula zone (W6c, <see cref="MidRimNebulaZone"/>).
        /// </summary>
        private void MidRimNebula()
        {
            _builder.Create("SPACE_RESOURCES_MIDRIM_NEBULA", "Space Resources - Mid Rim Nebula")
                .ResourceDespawnDelay(60) // 1 hour, as Korriban Orbit
                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(25)

                .AddSpawn(ObjectType.Placeable, "asteroid_arda")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "spc_gascloud_t3")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "spc_gascloud_t4")
                .WithFrequency(10);
        }

        // The three Small/Medium-only zones (plan section 12, W6c). Each area sets RESOURCE_SPAWN_COUNT to 75, 1.5x the
        // 50 nodes of an open 32x32 orbit, and draws from a richer table than the orbit it hangs off.

        /// <summary>
        /// Viscara Asteroid Belt (space_viscbelt, T1-2): Durelium asteroids of tiers 1 and 2 make up 62% of the nodes
        /// (Viscara orbit has none), beside Viscara's Tilarium, Mon Cala's Currian and a little Idailia.
        /// </summary>
        private void ViscaraAsteroidBelt()
        {
            _builder.Create("SPACE_RESOURCES_VISCARA_BELT", "Space Resources - Viscara Asteroid Belt")
                .ResourceDespawnDelay(180) // 3 hours, as Viscara Orbit
                .AddSpawn(ObjectType.Placeable, "asteroid_durel1")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "asteroid_durel2")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "spc_asteroid_til")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "asteroid_currian")
                .WithFrequency(12)

                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(3);
        }

        /// <summary>
        /// Mid Rim Nebula (space_midrimneb, T3-4): Tibanna Gas clouds of tiers 3 and 4 make up 75% of the nodes (Mid Rim 1's
        /// nebula table has 43%), beside Hutlar's Idailia and Tatooine's Barinium.
        /// </summary>
        private void MidRimNebulaZone()
        {
            _builder.Create("SPACE_RESOURCES_MIDRIM_NEBULA_ZONE", "Space Resources - Mid Rim Nebula (zone)")
                .ResourceDespawnDelay(120) // 2 hours, as Hutlar Orbit
                .AddSpawn(ObjectType.Placeable, "spc_gascloud_t3")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "spc_gascloud_t4")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(10);
        }

        /// <summary>
        /// Korriban Debris Field (space_korrdebris, T5): Korriban's Gostian with three times its Ardanium share. The
        /// field's main prize is salvage: SALVAGE_AURODIUM_PERCENT 15 on the area (the default is 4).
        /// </summary>
        private void KorribanDebrisField()
        {
            _builder.Create("SPACE_RESOURCES_KORRIBAN_DEBRIS", "Space Resources - Korriban Debris Field")
                .ResourceDespawnDelay(60) // 1 hour, as Korriban Orbit
                // W11e (mining.py): a field of mixed rocks; Gostian alone at a zone's density paid 148% of tier 5 combat.
                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(25)

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(25)

                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(25)

                .AddSpawn(ObjectType.Placeable, "asteroid_arda")
                .WithFrequency(4);
        }

        private void DathomirOrbit()
        {
            _builder.Create("SPACE_RESOURCES_DATHOMIR_ORBIT", "Space Resources - Dathomir Orbit")
                .ResourceDespawnDelay(75) // 1.25 hours for very high-tier space asteroids

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(37)

                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(25)

                .AddSpawn(ObjectType.Placeable, "asteroid_arda")
                .WithFrequency(3);
        }
        private void DantooineOrbit()
        {
            _builder.Create("SPACE_RESOURCES_DANTOOINE_ORBIT", "Space Resources - Dantooine Orbit")
                .ResourceDespawnDelay(90) // 1.5 hours for high-tier space asteroids

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(10);
        }
    }
}

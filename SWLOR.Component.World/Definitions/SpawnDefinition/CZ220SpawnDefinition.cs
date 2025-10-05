using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Domain.World.ValueObjects;

namespace SWLOR.Component.World.Definitions.SpawnDefinition
{
    public class CZ220SpawnDefinition: ISpawnListDefinition
    {
        private readonly ISpawnTableBuilder _builder;

        public CZ220SpawnDefinition(ISpawnTableBuilder spawnTableBuilder)
        {
            _builder = spawnTableBuilder;
        }

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            DroidSpawns();
            MynockSpawns();
            ColicoidExperimentSpawn();

            return _builder.Build();
        }

        private void DroidSpawns()
        {
            _builder.Create("CZ220_DROIDS", "CZ-220 Droids")
                .AddSpawn(ObjectType.Creature, "malsecdroid")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()
                .PlayAnimation(DurationType.Instant, AnimationEvent.CreatureOnDeath, VisualEffectType.Fnf_Fireball)

                .AddSpawn(ObjectType.Creature, "malspiderdroid")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()
                .PlayAnimation(DurationType.Instant, AnimationEvent.CreatureOnDeath, VisualEffectType.Fnf_Fireball);
        }

        private void MynockSpawns()
        {
            _builder.Create("CZ220_MYNOCKS", "CZ-220 Mynocks")
                .AddSpawn(ObjectType.Creature, "mynock")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void ColicoidExperimentSpawn()
        {
            _builder.Create("CZ220_COLICOID_EXPERIMENT", "Colicoid Experiment")
                .AddSpawn(ObjectType.Creature, "colicoidexp")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}

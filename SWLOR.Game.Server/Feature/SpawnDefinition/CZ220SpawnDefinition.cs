using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.SpawnService;

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

            return builder.Build();
        }

        private void DroidSpawns(SpawnTableBuilder builder)
        {
            var localVariables = new Dictionary<string, int>();
            localVariables.Add("DEATH_VFX_ID", (int)VisualEffect.Vfx_Fnf_Electric_Explosion);
            builder.Create("CZ220_DROIDS", "CZ-220 Droids")
                .AddSpawn(ObjectType.Creature, "malsecdroid", localVariables)
                .WithFrequency(50)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "malspiderdroid", localVariables)
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void MynockSpawns(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_MYNOCKS", "CZ-220 Mynocks")
                .AddSpawn(ObjectType.Creature, "mynock")
                .WithFrequency(100)
                .RandomlyWalks();
        }

        private void ColicoidExperimentSpawn(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_COLICOID_EXPERIMENT", "Colicoid Experiment")
                .AddSpawn(ObjectType.Creature, "colicoidexp")
                .WithFrequency(100)
                .RandomlyWalks();
        }
    }
}

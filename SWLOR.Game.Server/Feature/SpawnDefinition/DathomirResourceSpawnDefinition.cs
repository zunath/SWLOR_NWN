using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class DathomirResourceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            CaveRuins();
            Desert();
            DesertWestSide();
            GrottoCaverns();
            Grottos();
            MountainCaves();
            Mountains();
            RuinBase();
            TarnishedJungles();
            TarnishedJunglesNorth();
            TribeVillage();

            return _builder.Build();
        }

        private void CaveRuins()
        {
            _builder.Create("DATHOMIR_CAVE_RUINS_RESOURCES");
        }

        private void Desert()
        {
            _builder.Create("DATHOMIR_DESERT_RESOURCES");
        }

        private void DesertWestSide()
        {
            _builder.Create("DATHOMIR_DESERT_WEST_SIDE_RESOURCES");
        }

        private void GrottoCaverns()
        {
            _builder.Create("DATHOMIR_GROTTO_CAVERNS_RESOURCES");
        }

        private void Grottos()
        {
            _builder.Create("DATHOMIR_GROTTOS_RESOURCES");
        }

        private void MountainCaves()
        {
            _builder.Create("DATHOMIR_MOUNTAIN_CAVES_RESOURCES");
        }

        private void Mountains()
        {
            _builder.Create("DATHOMIR_MOUNTAINS_RESOURCES");
        }

        private void RuinBase()
        {
            _builder.Create("DATHOMIR_RUIN_BASE_RESOURCES");
        }

        private void TarnishedJungles()
        {
            _builder.Create("DATHOMIR_TARNISHED_JUNGLES_RESOURCES");
        }

        private void TarnishedJunglesNorth()
        {
            _builder.Create("DATHOMIR_TARNISHED_JUNGLES_NORTH_RESOURCES");
        }

        private void TribeVillage()
        {
            _builder.Create("DATHOMIR_TRIBE_VILLAGE_RESOURCES");
        }

    }
}

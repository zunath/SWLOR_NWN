using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class DathomirLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Chirodactyl();
            DragonTurtle();
            KwiGuardian();
            KwiShaman();
            KwiTribal();
            Purbole();
            ShearMite();
            Sprantal();
            Squellbug();
            Ssurian();
            SwamplandBug();

            return _builder.Build();
        }

        private void Chirodactyl()
        {
            _builder.Create("DATHOMIR_CHIRODACTYL");

            _builder.Create("DATHOMIR_CHIRODACTYL_RARES");
        }

        private void DragonTurtle()
        {
            _builder.Create("DATHOMIR_DRAGON_TURTLE");

            _builder.Create("DATHOMIR_DRAGON_TURTLE_RARES");
        }

        private void KwiGuardian()
        {
            _builder.Create("DATHOMIR_KWI_GUARDIAN");

            _builder.Create("DATHOMIR_KWI_GUARDIAN_GEAR");

            _builder.Create("DATHOMIR_KWI_GUARDIAN_GEAR_RARES");

            _builder.Create("DATHOMIR_KWI_GUARDIAN_RARES");
        }

        private void KwiShaman()
        {
            _builder.Create("DATHOMIR_KWI_SHAMAN");

            _builder.Create("DATHOMIR_KWI_SHAMAN_GEAR");

            _builder.Create("DATHOMIR_KWI_SHAMAN_GEAR_RARES");

            _builder.Create("DATHOMIR_KWI_SHAMAN_RARES");
        }

        private void KwiTribal()
        {
            _builder.Create("DATHOMIR_KWI_TRIBAL");

            _builder.Create("DATHOMIR_KWI_TRIBAL_GEAR");

            _builder.Create("DATHOMIR_KWI_TRIBAL_GEAR_RARES");

            _builder.Create("DATHOMIR_KWI_TRIBAL_RARES");
        }

        private void Purbole()
        {
            _builder.Create("DATHOMIR_PURBOLE");

            _builder.Create("DATHOMIR_PURBOLE_RARES");
        }

        private void ShearMite()
        {
            _builder.Create("DATHOMIR_SHEAR_MITE");

            _builder.Create("DATHOMIR_SHEAR_MITE_RARES");
        }

        private void Sprantal()
        {
            _builder.Create("DATHOMIR_SPRANTAL");

            _builder.Create("DATHOMIR_SPRANTAL_RARES");
        }

        private void Squellbug()
        {
            _builder.Create("DATHOMIR_SQUELLBUG");

            _builder.Create("DATHOMIR_SQUELLBUG_RARES");
        }

        private void Ssurian()
        {
            _builder.Create("DATHOMIR_SSURIAN");

            _builder.Create("DATHOMIR_SSURIAN_RARES");
        }

        private void SwamplandBug()
        {
            _builder.Create("DATHOMIR_SWAMPLAND_BUG");

            _builder.Create("DATHOMIR_SWAMPLAND_BUG_RARES");
        }
    }
}

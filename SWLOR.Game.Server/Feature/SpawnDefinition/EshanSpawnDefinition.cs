using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class EshanSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            EshanSilverwoodExpanse();
            EshanWinterMarches();
            EshanBattlegrounds();
            EshanScraplands();

            return _builder.Build();
        }

        private void EshanSilverwoodExpanse()
        {
            _builder.Create("ESHAN_SILVERWOOD_EXPANSE", "Eshan - Silverwood Expanse")
                .AddSpawn(ObjectType.Creature, "esh_direwolf")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "esh_frostwolf")
                .WithFrequency(60)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "esh_wolfalpha")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "esh_gorakvesh")
                .WithFrequency(5)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void EshanWinterMarches()
        {
            _builder.Create("ESHAN_WINTER_MARCHES", "Eshan - Winter Marches")
                .AddSpawn(ObjectType.Creature, "esh_nc_scout")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "esh_nc_hunter")
                .WithFrequency(70)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "esh_nc_vanguard")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void EshanBattlegrounds()
        {
            _builder.Create("ESHAN_BATTLEGROUNDS", "Eshan - Battlegrounds")
                .AddSpawn(ObjectType.Creature, "esh_nc_heavy")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "esh_nc_medic")
                .WithFrequency(80)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "esh_nc_captain")
                .WithFrequency(40)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "esh_sunguard")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void EshanScraplands()
        {
            _builder.Create("ESHAN_SCRAPLANDS", "Eshan - Scraplands")
                .AddSpawn(ObjectType.Creature, "esh_scrap_smug")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}

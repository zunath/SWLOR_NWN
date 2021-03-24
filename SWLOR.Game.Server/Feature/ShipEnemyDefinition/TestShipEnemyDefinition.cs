using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipEnemyDefinition
{
    public class TestShipEnemyDefinition: IShipEnemyListDefinition
    {
        private readonly ShipEnemyBuilder _builder = new ShipEnemyBuilder();

        public Dictionary<string, ShipEnemyDetail> BuildShipEnemies()
        {
            DemoEnemy();

            return _builder.Build();
        }

        private void DemoEnemy()
        {
            _builder.Create("TargetDummy")
                .Capacitor(20)
                .Shield(20)
                .Hull(10)
                .Evasion(5);
        }

    }
}

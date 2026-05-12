using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _10_SPAdjustments: PlayerMigrationBase
    {
        private static readonly FeatType[] RemovedFeats =
        {
            (FeatType)1350, // ForcePush1
            (FeatType)1351, // ForcePush2
            (FeatType)1352, // ForcePush3
            (FeatType)1353, // ForcePush4
            (FeatType)1359, // ThrowLightsaber1
            (FeatType)1360, // ThrowLightsaber2
            (FeatType)1361, // ThrowLightsaber3
            (FeatType)1364, // ForceStun1
            (FeatType)1365, // ForceStun2
            (FeatType)1366, // ForceStun3
            (FeatType)1371, // BattleInsight1
            (FeatType)1372, // BattleInsight2
            (FeatType)1373, // MindTrick1
            (FeatType)1374, // MindTrick2
            (FeatType)1820, // Premonition1
            (FeatType)1821, // Premonition2
            (FeatType)1850, // ThrowRock1
            (FeatType)1851, // ThrowRock2
            (FeatType)1852, // ThrowRock3
            (FeatType)1853, // ThrowRock4
            (FeatType)1854, // ThrowRock5
            (FeatType)1866, // ForceInspiration1
            (FeatType)1867, // ForceInspiration2
            (FeatType)1868, // ForceInspiration3
            (FeatType)1734, // ForceHeal1
            (FeatType)1735, // ForceHeal2
            (FeatType)1736, // ForceHeal3
            (FeatType)1737, // ForceHeal4
            (FeatType)1738, // ForceHeal5
            (FeatType)1719, // ForceBurst1
            (FeatType)1720, // ForceBurst2
            (FeatType)1721, // ForceBurst3
            (FeatType)1722, // ForceBurst4
            (FeatType)1727, // ForceMind1
            (FeatType)1728, // ForceMind2
            (FeatType)1825, // Benevolence1
            (FeatType)1826, // Benevolence2
            (FeatType)1827, // Benevolence3
            (FeatType)1828, // ForceValor1
            (FeatType)1829, // ForceValor2
            (FeatType)1729, // ForceDrain1
            (FeatType)1730, // ForceDrain2
            (FeatType)1731, // ForceDrain3
            (FeatType)1732, // ForceDrain4
            (FeatType)1733, // ForceDrain5
            (FeatType)1723, // ForceLightning1
            (FeatType)1724, // ForceLightning2
            (FeatType)1725, // ForceLightning3
            (FeatType)1726, // ForceLightning4
            (FeatType)1717, // ForceBody1
            (FeatType)1718, // ForceBody2
            (FeatType)1833, // CreepingTerror1
            (FeatType)1834, // CreepingTerror2
            (FeatType)1835, // CreepingTerror3
            (FeatType)1836, // ForceRage1
            (FeatType)1837, // ForceRage2
        };

        public override int Version => 10;
        public override void Migrate(uint player)
        {
            foreach (var feat in RemovedFeats)
            {
                CreaturePlugin.RemoveFeat(player, feat);
            }
        }
    }
}

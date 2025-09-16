using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _19_CombatUpgrade: ServerMigrationBase, IServerMigration
    {

        private readonly Dictionary<(PerkType, int), int> _refundMap = new()
        {
            {(PerkType.ImprovedTwoWeaponFightingOneHanded, 1), 4},
            {(PerkType.ImprovedTwoWeaponFightingTwoHanded, 1), 4},
            {(PerkType.Furor, 1), 4},
            {(PerkType.ShieldMaster, 1), 4},
            {(PerkType.VibrobladeMastery, 1), 8},
            {(PerkType.VibrobladeMastery, 2), 8},
            {(PerkType.FinesseVibrobladeMastery, 1), 8},
            {(PerkType.FinesseVibrobladeMastery, 2), 8},
            {(PerkType.LightsaberMastery, 1), 8},
            {(PerkType.LightsaberMastery, 2), 8},
            {(PerkType.HeavyVibrobladeMastery, 1), 8},
            {(PerkType.HeavyVibrobladeMastery, 2), 8},
            {(PerkType.PolearmMastery, 1), 8},
            {(PerkType.PolearmMastery, 2), 8},
            {(PerkType.TwinBladeMastery, 1), 8},
            {(PerkType.TwinBladeMastery, 2), 8},
            {(PerkType.SaberstaffMastery, 1), 8},
            {(PerkType.SaberstaffMastery, 2), 8},
            {(PerkType.KatarMastery, 1), 8},
            {(PerkType.KatarMastery, 2), 8},
            {(PerkType.StaffMastery, 1), 8},
            {(PerkType.StaffMastery, 2), 8},
            {(PerkType.FlurryStyle, 2), 4},
            {(PerkType.RapidShot, 1), 3},
            {(PerkType.RapidShot, 2), 5},
            {(PerkType.PistolMastery, 1), 8},
            {(PerkType.PistolMastery, 2), 8},
            {(PerkType.ThrowingWeaponMastery, 1), 8},
            {(PerkType.ThrowingWeaponMastery, 2), 8},
            {(PerkType.RapidReload, 1), 3},
            {(PerkType.RifleMastery, 1), 8},
            {(PerkType.RifleMastery, 2), 8},

        };

        public int Version => 19;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            RefundPerksByMapping(_refundMap);
        }
    }
}

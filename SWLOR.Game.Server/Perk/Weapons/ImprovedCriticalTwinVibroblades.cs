using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class ImprovedCriticalTwinVibroblades : ImprovedCriticalBase
    {
        public override PerkType PerkType => PerkType.ImprovedCriticalTwinVibroblades;
        public override string Name => "Improved Critical - Twin Vibroblades";
        public override bool IsActive => true;
        public override string Description => "Improves the critical hit chance when using twin vibroblades.";
        public override PerkCategoryType Category => PerkCategoryType.TwinBladesTwinVibroblades;
        public override PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public override PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public override bool IsTargetSelfOnly => false;
        public override int Enmity => 0;
        public override EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public override ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class ImprovedCriticalBlasterPistols : ImprovedCriticalBase
    {
        public override PerkType PerkType => PerkType.ImprovedCriticalBlasterPistols;
        public override string Name => "Improved Critical - Blasters";
        public override bool IsActive => true;
        public override string Description => "Improves the critical hit chance when using blaster pistols.";
        public override PerkCategoryType Category => PerkCategoryType.BlastersBlasterPistols;
        public override PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public override PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public override bool IsTargetSelfOnly => false;
        public override int Enmity => 0;
        public override EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public override ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
    }
}

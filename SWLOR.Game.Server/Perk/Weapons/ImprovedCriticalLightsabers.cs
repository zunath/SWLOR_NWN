using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class ImprovedCriticalLightsabers : ImprovedCriticalBase
    {
        public override PerkType PerkType => PerkType.ImprovedCriticalLightsabers;
        public override string Name => "Improved Critical - Lightsabers";
        public override bool IsActive => true;
        public override string Description => "Improves the critical hit chance when using a lightsaber.";
        public override PerkCategoryType Category => PerkCategoryType.Lightsabers;
        public override PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public override PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public override bool IsTargetSelfOnly => false;
        public override int Enmity => 0;
        public override EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public override ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
    }
}

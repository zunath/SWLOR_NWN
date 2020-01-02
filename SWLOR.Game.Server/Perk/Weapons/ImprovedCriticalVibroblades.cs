using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class ImprovedCriticalVibroblades : ImprovedCriticalBase
    {
        public override PerkType PerkType => PerkType.ImprovedCriticalVibroblades;
        public override string Name => "Improved Critical - Vibroblades";
        public override bool IsActive => true;
        public override string Description => "Improves the critical hit chance when using vibroblades.";
        public override PerkCategoryType Category => PerkCategoryType.OneHandedVibroblades;
        public override PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public override PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public override bool IsTargetSelfOnly => false;
        public override int Enmity => 0;
        public override EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public override ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        		public override Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(3, "Grants the Improved Critical feat when equipped with a blade.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.OneHanded, 10}, 
				})
			},
		};

    }
}

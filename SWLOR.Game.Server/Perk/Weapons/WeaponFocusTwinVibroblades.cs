using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class WeaponFocusTwinVibroblades : WeaponFocusBase
    {
        public override PerkType PerkType => PerkType.WeaponFocusTwinVibroblades;
        public override string Name => "Weapon Focus - Twin Vibroblades";
        public override bool IsActive => true;
        public override string Description => "You gain a bonus to attack rolls and damage when using twin vibroblades. The first level will grant a bonus to your attack roll. The second level will grant a bonus to your damage.";
        public override PerkCategoryType Category => PerkCategoryType.TwinBladesTwinVibroblades;
        public override PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public override PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public override bool IsTargetSelfOnly => false;
        public override int Enmity => 0;
        public override EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public override ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        		public override Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(3, "You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with twin vibroblades.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 5}, 
				})
			},
			{
				2, new PerkLevel(4, "You gain the Weapon Specialization feat which grants a +2 damage when equipped with twin vibroblades.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 15}, 
				})
			},
		};

    }
}

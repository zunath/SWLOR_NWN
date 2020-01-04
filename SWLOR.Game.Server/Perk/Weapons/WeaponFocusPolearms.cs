using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class WeaponFocusPolearms : WeaponFocusBase
    {
        public override PerkType PerkType => PerkType.WeaponFocusPolearms;
        public override string Name => "Weapon Focus - Polearms";
        public override bool IsActive => true;
        public override string Description => "You gain a bonus to attack rolls and damage when using polearms. The first level will grant a bonus to your attack roll. The second level will grant a bonus to your damage.";
        public override PerkCategoryType Category => PerkCategoryType.TwoHandedPolearms;
        public override PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public override PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public override bool IsTargetSelfOnly => false;
        public override int Enmity => 0;
        public override EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public override ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        		public override Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(3, "You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with a polearm.",
				new Dictionary<Skill, int>
				{
					{ Skill.TwoHanded, 5}, 
				})
			},
			{
				2, new PerkLevel(4, "You gain the Weapon Specialization feat which grants a +2 damage when equipped with a polearm.",
				new Dictionary<Skill, int>
				{
					{ Skill.TwoHanded, 15}, 
				})
			},
		};

    }
}

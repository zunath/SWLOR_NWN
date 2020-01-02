using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class WeaponFocusFinesseVibroblades : WeaponFocusBase
    {
        public override PerkType PerkType => PerkType.WeaponFocusFinesseVibroblades;
        public override string Name => "Weapon Focus - Finesse Vibroblades";
        public override bool IsActive => true;
        public override string Description => "You gain a bonus to attack rolls and damage when using a finesse vibroblade. The first level will grant a bonus to your attack roll. The second level will grant a bonus to your damage.";
        public override PerkCategoryType Category => PerkCategoryType.OneHandedFinesseVibroblades;
        public override PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public override PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public override bool IsTargetSelfOnly => false;
        public override int Enmity => 0;
        public override EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public override ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
    }
}

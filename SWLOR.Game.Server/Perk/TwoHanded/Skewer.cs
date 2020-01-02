using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Perk.TwoHanded
{
    public class Skewer : IPerk
    {
        public PerkType PerkType => PerkType.Skewer;
        public string Name => "Skewer";
        public bool IsActive => true;
        public string Description => "Interrupts your target's concentration effect. Must be equipped with a Polearm.";
        public PerkCategoryType Category => PerkCategoryType.TwoHandedPolearms;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Skewer;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.Polearm)
                return "You must be equipped with a Polearm in order to use this ability.";

            return string.Empty;
        }

        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int chance = perkLevel * 25;

            // Failed to interrupt.
            if(RandomService.D100(1) > chance)
            {
                creature.SendMessage("You fail to interrupt your target's concentration.");
                return;
            }
            
            NWCreature targetCreature = target.Object;
            var effect = AbilityService.GetActiveConcentrationEffect(targetCreature);
            if (effect.Type != PerkType.Unknown)
            {
                targetCreature.SendMessage("Your concentration effect has been interrupted by " + creature.Name + ".");
                AbilityService.EndConcentrationEffect(target.Object);
            }
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {

        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {

        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }
        
        public bool IsHostile()
        {
            return false;
        }

        public Dictionary<int, PerkLevel> PerkLevels { get; }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class Knockdown: IPerkHandler
    {
        public PerkType PerkType => PerkType.Knockdown;
        public string Name => "Knockdown";
        public bool IsActive => true;
        public string Description => "Your next attack deals additional bludgeoning damage and knocks down the target for a short period of time. Must be equipped with a baton.";
        public PerkCategoryType Category => PerkCategoryType.OneHandedBatons;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Knockdown;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 20;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.TargetOnly;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            NWItem weapon = oPC.RightHand;
            if (weapon.CustomItemType != CustomItemType.Baton)
                return "You must be equipped with a baton weapon to use that ability.";

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

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int damage;
            float length;

            switch (perkLevel)
            {
                case 1:
                    damage = RandomService.D4(1);
                    length = 6.0f;
                    break;
                case 2:
                    damage = RandomService.D4(2);
                    length = 6.0f;
                    break;
                case 3:
                    damage = RandomService.D6(2);
                    length = 6.0f;
                    break;
                case 4:
                    damage = RandomService.D6(2);
                    length = 9.0f;
                    break;
                case 5:
                    damage = RandomService.D6(3);
                    length = 9.0f;
                    break;
                case 6:
                    damage = RandomService.D8(3);
                    length = 9.0f;
                    break;
                default: return;
            }

            _.ApplyEffectToObject(DurationType.Temporary, _.EffectKnockdown(), target.Object, length);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage, DamageType.Bludgeoning), target);
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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

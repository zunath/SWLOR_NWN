using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class ShieldBoost: IPerkHandler
    {
        public PerkType PerkType => PerkType.ShieldBoost;
        public string Name => "Shield Boost";
        public bool IsActive => true;
        public string Description => "Increases user's maximum hit points for a limited time. Increases enmity of all nearby enemies by a sharp amount. Must be equipped with Heavy Armor.";
        public PerkCategoryType Category => PerkCategoryType.Armor;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ShieldBoost;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 100;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.Chest.CustomItemType != CustomItemType.HeavyArmor)
                return "Must be equipped with heavy armor to use that ability.";

            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 3f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int duration = 60;

            var vfx = _.EffectVisualEffect(Vfx.Dur_Blur);
            vfx = _.TagEffect(vfx, "SHIELD_BOOST_VFX");

            _.ApplyEffectToObject(DurationType.Temporary, vfx, target, duration);
            CustomEffectService.ApplyCustomEffect(creature, target.Object, CustomEffectType.ShieldBoost, duration, perkLevel, null);
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

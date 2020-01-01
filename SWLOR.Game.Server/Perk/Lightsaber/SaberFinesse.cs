using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;
namespace SWLOR.Game.Server.Perk.Lightsaber
{
    public class SaberFinesse : IPerkHandler
    {
        public PerkType PerkType => PerkType.SaberFinesse;
        public string Name => "Saber Finesse";
        public bool IsActive => true;
        public string Description => "You make melee attack rolls with your DEX if it is higher than your STR. Must be equipped with a lightsaber or saberstaff.";
        public PerkCategoryType Category => PerkCategoryType.LightsabersAndSaberstaffs;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
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
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
            ApplyFeatChanges(creature, null);
        }

        public void OnRemoved(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, Feat.WeaponFinesse);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber && oItem.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalBoolean("LIGHTSABER") == false) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber && oItem.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalBoolean("LIGHTSABER") == false) return;
            if (oItem == creature.LeftHand) return;
            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem equipped = oItem ?? creature.RightHand;
            if (Equals(equipped, oItem) || (equipped.CustomItemType != CustomItemType.Lightsaber && equipped.CustomItemType != CustomItemType.Saberstaff && equipped.GetLocalBoolean("LIGHTSABER") == false))
            {
                NWNXCreature.RemoveFeat(creature, Feat.WeaponFinesse);
                return;
            }
            NWNXCreature.AddFeat(creature, Feat.WeaponFinesse);
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

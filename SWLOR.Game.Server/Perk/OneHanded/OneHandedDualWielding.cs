using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class OneHandedDualWielding : IPerk
    {
        public PerkType PerkType => PerkType.OneHandedDualWielding;
        public string Name => "One-Handed Dual Wielding";
        public bool IsActive => true;
        public string Description => "Grants bonuses while wielding two weapons. Must be equipped with a non-lightsaber one-handed weapon.";
        public PerkCategoryType Category => PerkCategoryType.OneHandedGeneral;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

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
            RemoveFeats(creature);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Vibroblade &&
                oItem.CustomItemType != CustomItemType.Baton &&
                oItem.CustomItemType != CustomItemType.FinesseVibroblade)
            {
                return;
            }

            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Vibroblade &&
                oItem.CustomItemType != CustomItemType.Baton &&
                oItem.CustomItemType != CustomItemType.FinesseVibroblade)
            {
                return;
            }
            
            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }


        private void RemoveFeats(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, Feat.Two_Weapon_Fighting);
            NWNXCreature.RemoveFeat(creature, Feat.Ambidexterity);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Two_Weapon_Fighting);
        }


        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem mainEquipped = oItem ?? creature.RightHand;
            NWItem offEquipped = oItem ?? creature.LeftHand;
            
            // oItem was unequipped.
            if (Equals(mainEquipped, oItem) || Equals(offEquipped, oItem))
            {
                RemoveFeats(creature);
                return;
            }

            // Main or offhand was invalid (i.e not equipped)
            if (!mainEquipped.IsValid || !offEquipped.IsValid)
            {
                RemoveFeats(creature);
                return;
            }

            // Main or offhand is not acceptable item type.
            if (mainEquipped.CustomItemType != CustomItemType.Vibroblade &&
                mainEquipped.CustomItemType != CustomItemType.Baton &&
                mainEquipped.CustomItemType != CustomItemType.FinesseVibroblade ||
                offEquipped.CustomItemType != CustomItemType.Vibroblade && 
                offEquipped.CustomItemType != CustomItemType.Baton && 
                offEquipped.CustomItemType != CustomItemType.FinesseVibroblade)
            {
                RemoveFeats(creature);
                return;
            }


            int perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.OneHandedDualWielding);
            NWNXCreature.AddFeat(creature, Feat.Two_Weapon_Fighting);

            if (perkLevel >= 2)
            {
                NWNXCreature.AddFeat(creature, Feat.Ambidexterity);
            }
            if (perkLevel >= 3)
            {
                NWNXCreature.AddFeat(creature, Feat.Improved_Two_Weapon_Fighting);
            }
        }

        public bool IsHostile()
        {
            return false;
        }

        		public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(3, "Grants Two-weapon Fighting feat which reduces attack penalty from -6/-10 to -4/-8 when fighting with two weapons. Must be equipped with a one-handed non-lightsaber weapon",
				new Dictionary<SkillType, int>
				{
					{ SkillType.OneHanded, 10}, 
				})
			},
			{
				2, new PerkLevel(4, "Grants Ambidexterity feat which reduces the attack penalty of your off-hand weapon by 4. Must be equipped with a one-handed non-lightsaber weapon",
				new Dictionary<SkillType, int>
				{
					{ SkillType.OneHanded, 15}, 
				})
			},
			{
				3, new PerkLevel(6, "Grants Improved two-weapon fighting which gives you a second off-hand attack at a penalty of -5 to your attack roll. Must be equipped with a one-handed non-lightsaber weapon",
				new Dictionary<SkillType, int>
				{
					{ SkillType.OneHanded, 25}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

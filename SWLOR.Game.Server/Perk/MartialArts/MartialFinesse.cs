using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class MartialFinesse: IPerk
    {
        public PerkType PerkType => PerkType.MartialFinesse;
        public string Name => "Martial Finesse";
        public bool IsActive => true;
        public string Description => "Grants the Weapon Finesse feat which enables you to make attack rolls with your dexterity modifier instead of strength, if it is higher. Requires a martial arts weapon or unarmed to use.";
        public PerkCategoryType Category => PerkCategoryType.MartialArts;
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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
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
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem unequippingItem)
        {
            NWItem mainHand = creature.RightHand;
            NWItem offHand = creature.LeftHand;
            CustomItemType mainType = mainHand.CustomItemType;
            CustomItemType offType = offHand.CustomItemType;
            bool receivesFeat = false;

            if (unequippingItem != null && Equals(unequippingItem, mainHand))
            {
                mainHand = (new NWGameObject());
            }
            else if (unequippingItem != null && Equals(unequippingItem, offHand))
            {
                offHand = (new NWGameObject());
            }

            // Main is Martial and off is invalid 
            // OR
            // Main is invalid and off is martial
            if ((mainType == CustomItemType.MartialArtWeapon && !offHand.IsValid) ||
                (offType == CustomItemType.MartialArtWeapon && !mainHand.IsValid))
            {
                receivesFeat = true;
            }
            // Both main and off are invalid
            else if (!mainHand.IsValid && !offHand.IsValid)
            {
                receivesFeat = true;
            }

            if (receivesFeat)
            {
                NWNXCreature.AddFeat(creature, Feat.WeaponFinesse);
            }
            else
            {
                NWNXCreature.RemoveFeat(creature, Feat.WeaponFinesse);
            }
        }

        public bool IsHostile()
        {
            return false;
        }

        		public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(3, "Grants the Weapon Finesse feat when equipped with a martial arts weapon or unarmed.",
				new Dictionary<Skill, int>
				{
					{ Skill.MartialArts, 1}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using Skill = SWLOR.Game.Server.Enumeration.Skill;


namespace SWLOR.Game.Server.Perk.TwinBlade
{
    public class TwinBladeMastery : IPerk
    {
        public PerkType PerkType => PerkType.TwinBladeMastery;
        public string Name => "Twin Blade Mastery";
        public bool IsActive => true;
        public string Description => "Grants bonuses and reduces penalties while wielding twin vibroblades.";
        public PerkCategoryType Category => PerkCategoryType.TwinBladesTwinVibroblades;
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
            if (oItem.CustomItemType != CustomItemType.TwinBlade) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.TwinBlade) return;
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

        private void ApplyFeatChanges(NWCreature creature, NWItem unequippedItem)
        {
            NWItem equipped = unequippedItem ?? creature.RightHand;

            if (Equals(equipped, unequippedItem) || equipped.CustomItemType != CustomItemType.TwinBlade)
            {
                RemoveFeats(creature);
                return;
            }

            int perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.TwinBladeMastery);
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
				1, new PerkLevel(3, "Grants two-weapon fighting feat which reduces attack penalty from -6/-10 to -2/-6. Must be equipped with a Twin Blade.",
				new Dictionary<Skill, int>
				{
					{ Skill.TwinBlades, 1}, 
				})
			},
			{
				2, new PerkLevel(4, "Grants Ambidexterity feat which reduces the attack penatly fo your off-hand weapon by 4. Must be equipped with a Twin Blade.",
				new Dictionary<Skill, int>
				{
					{ Skill.TwinBlades, 8}, 
				})
			},
			{
				3, new PerkLevel(6, "Grants Improved two-weapon fighting which gives you a second off-hand attack at a penalty of -5 to your attack roll. Must be equipped with a Twin Blade.",
				new Dictionary<Skill, int>
				{
					{ Skill.TwinBlades, 15}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

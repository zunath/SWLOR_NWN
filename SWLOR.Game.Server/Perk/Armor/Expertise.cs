using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using Skill = SWLOR.Game.Server.Enumeration.Skill;


namespace SWLOR.Game.Server.Perk.Armor
{
    public class Expertise : IPerk
    {
        public PerkType PerkType => PerkType.Expertise;
        public string Name => "Expertise";
        public bool IsActive => true;
        public string Description => "Gives you the ability to make defensive attacks, gaining +5 AC but receiving -5 penalty to attack rolls. Must be equipped with Heavy Armor.";
        public PerkCategoryType Category => PerkCategoryType.Armor;
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
            NWNXCreature.RemoveFeat(creature, Feat.Expertise);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.HeavyArmor) return;

            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.HeavyArmor) return;

            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem equipped = oItem ?? creature.Chest;

            if (equipped.Equals(oItem) || equipped.CustomItemType != CustomItemType.HeavyArmor)
            {
                NWNXCreature.RemoveFeat(creature, Feat.Expertise);
                return;
            }

            NWNXCreature.AddFeat(creature, Feat.Expertise);
        }

        public bool IsHostile()
        {
            return false;
        }

        		public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(2, "Grants the Expertise feat.",
				new Dictionary<Skill, int>
				{
					{ Skill.HeavyArmor, 15}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

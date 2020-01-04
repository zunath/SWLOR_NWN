using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class BluntPowerAttack : IPerk
    {
        public PerkType PerkType => PerkType.BluntPowerAttack;
        public string Name => "Blunt Power Attack";
        public bool IsActive => true;
        public string Description => "Increases damage at the cost of reduced attack rolls. Only available when equipped with a blunt.";
        public PerkCategoryType Category => PerkCategoryType.OneHandedBatons;
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
            NWNXCreature.RemoveFeat(creature, Feat.Power_Attack);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Power_Attack);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Baton) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Baton) return;
            if (oItem == creature.LeftHand) return;

            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem equipped = oItem ?? creature.RightHand;

            if (Equals(equipped, oItem) || equipped.CustomItemType != CustomItemType.Baton)
            {
                NWNXCreature.RemoveFeat(creature, Feat.Power_Attack);
                NWNXCreature.RemoveFeat(creature, Feat.Improved_Power_Attack);
                if (_.GetActionMode(creature, ActionMode.PowerAttack) == true)
                {
                    _.SetActionMode(creature, ActionMode.PowerAttack, false);
                }
                if (_.GetActionMode(creature, ActionMode.ImprovedPowerAttack) == true)
                {
                    _.SetActionMode(creature, ActionMode.ImprovedPowerAttack, false);
                }
                return;
            }

            int perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.BluntPowerAttack);
            NWNXCreature.AddFeat(creature, Feat.Power_Attack);

            if (perkLevel >= 2)
            {
                NWNXCreature.AddFeat(creature, Feat.Improved_Power_Attack);
            }
        }

        public bool IsHostile()
        {
            return false;
        }

        		public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(2, "Grants the Power Attack feat which grants a +5 bonus to damage roll at the cost of -5 to attack roll. Only available when equipped with a baton.",
				new Dictionary<Skill, int>
				{
					{ Skill.OneHanded, 2}, 
				})
			},
			{
				2, new PerkLevel(3, "Grants the Improved Power Attack feat which grants a +10 bonus to damage roll at the cost of -10 to attack roll. Does not replace Power Attack. Only available when equipped with a baton.",
				new Dictionary<Skill, int>
				{
					{ Skill.OneHanded, 15}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

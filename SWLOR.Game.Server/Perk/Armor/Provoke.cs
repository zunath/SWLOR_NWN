using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class Provoke: IPerk
    {
        public PerkType PerkType => PerkType.Provoke;
        public string Name => "Provoke";
        public bool IsActive => true;
        public string Description => "Increases the enmity of a single target towards the user. Must be equipped with Heavy Armor to use.";
        public PerkCategoryType Category => PerkCategoryType.Armor;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Provoke;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (!oTarget.IsNPC) return "Only NPCs may be targeted with Provoke.";

            float distance = _.GetDistanceBetween(oPC.Object, oTarget.Object);
            if (distance > 9.0f) return "Target is too far away.";

            if (oPC.Chest.CustomItemType != CustomItemType.HeavyArmor)
                return "You must be equipped with heavy armor to use that combat ability.";

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
            int perkRank = PerkService.GetCreaturePerkLevel(oPC, PerkType.Provoke);

            if (perkRank == 2) baseCooldownTime -= 5.0f;
            else if (perkRank == 3) baseCooldownTime -= 10.0f;
            else if (perkRank == 4) baseCooldownTime -= 15.0f;

            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            NWCreature npc = (target.Object);
            Effect vfx = _.EffectVisualEffect(Vfx.Vfx_Imp_Charm);
            _.ApplyEffectToObject(DurationType.Instant, vfx, target.Object);
            
            creature.AssignCommand(() =>
            {
                _.ActionPlayAnimation(Animation.FireForget_Taunt, 1f, 1f);
            });

            EnmityService.AdjustEnmity(npc, creature, 120);
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
            if (newLevel == 1)
            {
                ApplyFeatChanges(creature, null);
            }
        }

        public void OnRemoved(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, Feat.Provoke);
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
                NWNXCreature.RemoveFeat(creature, Feat.Provoke);
                return;
            }

            NWNXCreature.AddFeat(creature, Feat.Provoke);
        }

        public bool IsHostile()
        {
            return false;
        }

        		public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(3, "Grants the Provoke ability.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.HeavyArmor, 2}, 
				})
			},
			{
				2, new PerkLevel(2, "Reduces cooldown by 5 seconds.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.HeavyArmor, 10}, 
				})
			},
			{
				3, new PerkLevel(4, "Reduces cooldown by 10 seconds.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.HeavyArmor, 20}, 
				})
			},
			{
				4, new PerkLevel(4, "Reduces cooldown by 15 seconds.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.HeavyArmor, 30}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Perk.General
{
    public class Health: IPerk
    {
        public PerkType PerkType => PerkType.Health;
        public string Name => "Health";
        public bool IsActive => true;
        public string Description => "Improves your hit points.";
        public PerkCategoryType Category => PerkCategoryType.General;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.None;
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
            if (!creature.IsPlayer) return;
            PlayerStatService.ApplyStatChanges(creature.Object, null);
        }

        public void OnRemoved(NWCreature creature)
        {
            if (!creature.IsPlayer) return;
            PlayerStatService.ApplyStatChanges(creature.Object, null);
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

        		public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(2, "+5 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				2, new PerkLevel(2, "+10 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				3, new PerkLevel(3, "+15 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				4, new PerkLevel(3, "+20 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				5, new PerkLevel(3, "+25 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				6, new PerkLevel(4, "+30 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				7, new PerkLevel(4, "+35 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				8, new PerkLevel(5, "+40 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				9, new PerkLevel(5, "+45 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				10, new PerkLevel(6, "+50 HP",
				new Dictionary<SkillType, int>
				{

				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

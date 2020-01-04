using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class Clarity: IPerk
    {
        public PerkType PerkType => PerkType.Clarity;
        public string Name => "Clarity";
        public bool IsActive => true;
        public string Description => "Increases natural FP regeneration. Must be equipped with force armor.";
        public PerkCategoryType Category => PerkCategoryType.Armor;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.None;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature creature, NWObject oTarget, int spellTier)
        {
            return null;
        }

        public int FPCost(NWCreature creature, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature creature, int spellTier)
        {
            return 0f;
        }

        public float CooldownTime(NWCreature creature, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
        }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick)
        {
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

        public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
        {
            {
                1, new PerkLevel(2, "+2 to natural FP regen", 
                    new Dictionary<SkillType, int>
                    {
                        { SkillType.ForceArmor, 5 },
                    })
            },
            {
                2, new PerkLevel(3, "+3 to natural FP regen",
                    new Dictionary<SkillType, int>
                    {
                        { SkillType.ForceArmor, 15 }
                    })
            },
            {
                3, new PerkLevel(3, "+4 to natural FP regen",
                    new Dictionary<SkillType, int>
                    {
                        { SkillType.ForceArmor, 25 }
                    })
            },
            {
                4, new PerkLevel(3, "+5 to natural FP regen",
                    new Dictionary<SkillType, int>
                    {
                        { SkillType.ForceArmor, 40 }
                    })
            },
            {
                5, new PerkLevel(3, "+6 to natural FP regen",
                    new Dictionary<SkillType, int>
                    {
                        { SkillType.ForceArmor, 60 }
                    })
            },
        };

        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();
    }
}

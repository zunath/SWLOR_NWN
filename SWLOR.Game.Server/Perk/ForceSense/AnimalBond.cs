using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class AnimalBond : IPerk
    {
        public PerkType PerkType => PerkType.AnimalBond;
        public string Name => "Animal Bond";
        public bool IsActive => false;
        public string Description => "The caster convinces a creature to travel and fight with them.";
        public PerkCategoryType Category => PerkCategoryType.ForceSense;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.AnimalBond;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
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
                1, new PerkLevel(2, "The caster befriends an animal or beast with up to Challenge Rating 4.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceSense, 10},
                })
            },
            {
                2, new PerkLevel(2, "The caster befriends an animal or beast with up to Challenge Rating 8.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceSense, 25},
                })
            },
            {
                3, new PerkLevel(3, "The caster befriends an animal or beast with up to Challenge Rating 12.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceSense, 40},
                })
            },
            {
                4, new PerkLevel(3, "The caster befriends an animal or beast with up to Challenge Rating 16.", SpecializationType.Sentinel,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceSense, 55},
                })
            },
            {
                5, new PerkLevel(4, "The caster befriends an animal or beast with up to Challenge Rating 20.", SpecializationType.Sentinel,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceSense, 70},
                })
            },
            {
                6, new PerkLevel(5, "The caster befriends an animal or beast with any Challenge Rating.", SpecializationType.Sentinel,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceSense, 85},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AnimalBond1, BaseFPCost = 4, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AnimalBond2, BaseFPCost = 8, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AnimalBond3, BaseFPCost = 12, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                4, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AnimalBond4, BaseFPCost = 16, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                5, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AnimalBond5, BaseFPCost = 20, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                6, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AnimalBond6, BaseFPCost = 30, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

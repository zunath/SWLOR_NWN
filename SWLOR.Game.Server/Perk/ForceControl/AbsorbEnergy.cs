using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class AbsorbEnergy : IPerk
    {
        public PerkType PerkType => PerkType.AbsorbEnergy;
        public string Name => "Absorb Energy";
        public bool IsActive => true;
        public string Description => "Absorbs a percentage of damage that the caster would take, from all sources.";
        public PerkCategoryType Category => PerkCategoryType.ForceControl;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.AbsorbEnergy;
        public PerkExecutionType ExecutionType => PerkExecutionType.ForceAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 20;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
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
                1, new PerkLevel(2, "Grants 10% immunity to all damage while the caster retains concentration.",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceControl, 0},
                })
            },
            {
                2, new PerkLevel(2, "Grants 20% immunity to all damage while the caster retains concentration.",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceControl, 15},
                })
            },
            {
                3, new PerkLevel(3, "Grants 30% immunity to all damage while the caster retains concentration.",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceControl, 30},
                })
            },
            {
                4, new PerkLevel(3, "Grants 40% immunity to all damage while the caster retains concentration.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceControl, 45},
                })
            },
            {
                5, new PerkLevel(4, "Grants 50% immunity to all damage while the caster retains concentration.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceControl, 60},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AbsorbEnergy1, BaseFPCost = 2, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AbsorbEnergy2, BaseFPCost = 4, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AbsorbEnergy3, BaseFPCost = 6, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                4, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AbsorbEnergy4, BaseFPCost = 8, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                5, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.AbsorbEnergy5, BaseFPCost = 10, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

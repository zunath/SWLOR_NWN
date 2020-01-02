using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Engineering
{
    public class LightsaberBlueprints : IPerk
    {
        public PerkType PerkType => PerkType.LightsaberBlueprints;
        public string Name => "Lightsaber Blueprints";
        public bool IsActive => true;
        public string Description => "Unlocks new lightsaber blueprints on every odd level (1, 3, 5, 7, 9) and adds an enhancement slot for every even level (2, 4, 6, 8, 10) for engineering.";
        public PerkCategoryType Category => PerkCategoryType.Engineering;
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
                1, new PerkLevel(3, "Tier 1 lightsaber blueprints.",
                new Dictionary<SkillType, int>
                {

                },
                new List<int>
                {
                    30
                })
            },
            {
                2, new PerkLevel(3, "Tier 1 lightsaber blueprints. +1 enhancement slot",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 5},
                },
                new List<int>
                {
                    30
                })
            },
            {
                3, new PerkLevel(4, "Tier 2 lightsaber blueprints. +1 enhancement slot",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 10},
                },
                new List<int>
                {
                    30
                })
            },
            {
                4, new PerkLevel(5, "Tier 2 lightsaber blueprints. +2 enhancement slots",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 15},
                },
                new List<int>
                {
                    30
                })
            },
            {
                5, new PerkLevel(6, "Tier 3 lightsaber blueprints. +2 enhancement slots",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 20},
                },
                new List<int>
                {
                    30
                })
            },
            {
                6, new PerkLevel(6, "Tier 3 lightsaber blueprints. +3 enhancement slots",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 25},
                },
                new List<int>
                {
                    30
                })
            },
            {
                7, new PerkLevel(6, "Tier 4 lightsaber blueprints. +3 enhancement slots",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 30},
                },
                new List<int>
                {
                    30
                })
            },
            {
                8, new PerkLevel(7, "Tier 4 lightsaber blueprints. +4 enhancement slots",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 35},
                },
                new List<int>
                {
                    30
                })
            },
            {
                9, new PerkLevel(7, "Tier 5 lightsaber blueprints. +4 enhancement slots",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 40},
                },
                new List<int>
                {
                    30
                })
            },
            {
                10, new PerkLevel(8, "Tier 5 lightsaber blueprints. +5 enhancement slots",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Lightsaber, 45},
                },
                new List<int>
                {
                    30
                })
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

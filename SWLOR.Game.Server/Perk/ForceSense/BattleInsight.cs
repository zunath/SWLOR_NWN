using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class BattleInsight : IPerk
    {
        public PerkType PerkType => PerkType.BattleInsight;
        public string Name => "Battle insight";
        public bool IsActive => true;
        public string Description => "The caster boosts their nearby allies at the expense of their own abilities.";
        public PerkCategoryType Category => PerkCategoryType.ForceSense;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.BattleAndForceInsight;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 10;
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
                1, new PerkLevel(4, "The caster gets -5 AB & AC but their nearby party members get +5 AB & AC",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceSense, 40},
                })
            },
            {
                2, new PerkLevel(6, "The caster gets -10 AB & AC but their nearby party members get +10 AB & AC",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceSense, 60},
                })
            },
            {
                3, new PerkLevel(8, "The caster and nearby enemies get -10 AB & AC but the nearby party members get +10 AB & AC",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceSense, 80},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.BattleInsight1, BaseFPCost = 0, ConcentrationFPCost = 3, ConcentrationTickInterval = 6}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.BattleInsight2, BaseFPCost = 0, ConcentrationFPCost = 4, ConcentrationTickInterval = 6}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.BattleInsight3, BaseFPCost = 0, ConcentrationFPCost = 5, ConcentrationTickInterval = 6}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            const float MaxDistance = 5.0f;
            int nth = 1;
            int amount;

            switch (perkLevel)
            {
                case 1:
                    amount = 5;
                    break;
                case 2:
                case 3:
                    amount = 10;
                    break;
                default: return;
            }

            // Penalize the caster
            Effect effect = _.EffectACDecrease(amount);
            effect = _.EffectLinkEffects(effect, _.EffectAttackDecrease(amount));
            ApplyEffectToObject(DurationType.Temporary, effect, creature, 6.1f);


            NWCreature targetCreature = _.GetNearestCreature((int)CreatureType.IsAlive, 1, creature, nth);
            while (targetCreature.IsValid && GetDistanceBetween(creature, targetCreature) <= MaxDistance)
            {
                // Skip the caster, if they get picked up.
                if (targetCreature == creature)
                {
                    nth++;
                    targetCreature = _.GetNearestCreature((int)CreatureType.IsAlive, 1, creature, nth);
                    continue;
                }

                // Handle effects for differing spellTier values
                switch (perkLevel)
                {
                    case 1:
                        amount = 5;

                        if (_.GetIsReactionTypeHostile(targetCreature, creature) == true)
                        {
                            nth++;
                            targetCreature = _.GetNearestCreature((int)CreatureType.IsAlive, 1, creature, nth);
                            continue;
                        }
                        break;
                    case 2:
                        amount = 10;

                        if (_.GetIsReactionTypeHostile(targetCreature, creature) == true)
                        {
                            nth++;
                            targetCreature = _.GetNearestCreature((int)CreatureType.IsAlive, 1, creature, nth);
                            continue;
                        }
                        break;
                    case 3:
                        amount = 10;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(perkLevel));
                }

                if (_.GetIsReactionTypeHostile(targetCreature, creature) == true)
                {
                    effect = _.EffectACDecrease(amount);
                    effect = _.EffectLinkEffects(effect, _.EffectAttackDecrease(amount));
                }
                else
                {
                    effect = _.EffectACIncrease(amount);
                    effect = _.EffectLinkEffects(effect, _.EffectAttackIncrease(amount));
                }

                _.ApplyEffectToObject(DurationType.Temporary, effect, targetCreature, 6.1f);
                _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Dur_Magic_Resistance), targetCreature);

                nth++;
                targetCreature = _.GetNearestCreature((int)CreatureType.IsAlive, 1, creature, nth);
            }

        }
    }
}

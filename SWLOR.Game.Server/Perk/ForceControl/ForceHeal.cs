using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class ForceHeal : IPerk
    {
        public PerkType PerkType => PerkType.ForceHeal;
        public string Name => "Force Heal";
        public bool IsActive => true;
        public string Description => "Restores HP on a single target over time.";
        public PerkCategoryType Category => PerkCategoryType.ForceControl;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Light;
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
                1, new PerkLevel(2, "Heals a single target for 2 HP every three seconds.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 0},
                })
            },
            {
                2, new PerkLevel(2, "Heals a single target for 3 HP every three seconds.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 10},
                })
            },
            {
                3, new PerkLevel(3, "Heals a single target for 5 HP every three seconds.",
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 20},
                })
            },
            {
                4, new PerkLevel(3, "Heals a single target for 7 HP every three seconds.", SpecializationType.Consular,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 30},
                })
            },
            {
                5, new PerkLevel(4, "Heals a single target for 10 HP every three seconds.", SpecializationType.Consular,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceControl, 40},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceHeal1, BaseFPCost = 0, ConcentrationFPCost = 1, ConcentrationTickInterval = 6}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceHeal2, BaseFPCost = 0, ConcentrationFPCost = 2, ConcentrationTickInterval = 6}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceHeal3, BaseFPCost = 0, ConcentrationFPCost = 3, ConcentrationTickInterval = 6}
                }
            },
            {
                4, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceHeal4, BaseFPCost = 0, ConcentrationFPCost = 4, ConcentrationTickInterval = 6}
                }
            },
            {
                5, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceHeal5, BaseFPCost = 0, ConcentrationFPCost = 5, ConcentrationTickInterval = 6}
                }
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            int amount = 0;

            switch (perkLevel)
            {
                case 1: amount = 2; break;
                case 2: amount = 3; break;
                case 3: amount = 5; break;
                case 4: amount = 7; break;
                case 5: amount = 10; break;
            }

            // If target is at max HP, we do nothing else.
            int difference = target.MaxHP - target.CurrentHP;
            if (difference <= 0) return;

            // If we would heal the target for more than their max, reduce the amount healed to that number.
            if (amount > difference)
                amount = difference;

            // Apply the heal
            _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(amount), target);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Head_Holy), target);

            // Give Control XP, if player.
            if (creature.IsPlayer)
            {
                SkillService.GiveSkillXP(creature.Object, SkillType.ForceControl, amount * 10);
            }

            EnmityService.AdjustEnmityOnAllTaggedCreatures(creature, amount * 3, 2);
        }
    }
}

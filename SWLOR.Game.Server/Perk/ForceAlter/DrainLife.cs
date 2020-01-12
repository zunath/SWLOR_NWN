using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class DrainLife : IPerk
    {
        public PerkType PerkType => PerkType.DrainLife;
        public string Name => "Drain Life";
        public bool IsActive => true;
        public string Description => "Steals HP from a single target every second.";
        public PerkCategoryType Category => PerkCategoryType.ForceAlter;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.DrainLife;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Dark;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (!oTarget.IsCreature)
                return "This ability can only be used on living creatures.";
            NWCreature targetCreature = oTarget.Object;
            if (targetCreature.RacialType == RacialType.Robot)
                return "This ability cannot be used on droids.";

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
                1, new PerkLevel(4, "Steals 5 HP from a single target every second.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 30},
                })
            },
            {
                2, new PerkLevel(4, "Steals 6 HP from a single target every second.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 45},
                })
            },
            {
                3, new PerkLevel(5, "Steals 7 HP from a single target every second.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 60},
                })
            },
            {
                4, new PerkLevel(5, "Steals 8 HP from a single target every second.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 75},
                })
            },
            {
                5, new PerkLevel(6, "Steals 10 HP from a single target every second.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 90},
                })
            },
        };

        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.DrainLife1, BaseFPCost = 0, ConcentrationFPCost = 4, ConcentrationTickInterval = 1}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.DrainLife2, BaseFPCost = 0, ConcentrationFPCost = 4, ConcentrationTickInterval = 1}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.DrainLife3, BaseFPCost = 0, ConcentrationFPCost = 4, ConcentrationTickInterval = 1}
                }
            },
            {
                4, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.DrainLife4, BaseFPCost = 0, ConcentrationFPCost = 4, ConcentrationTickInterval = 1}
                }
            },
            {
                5, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.DrainLife5, BaseFPCost = 0, ConcentrationFPCost = 4, ConcentrationTickInterval = 1}
                }
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick)
        {
            int amount;

            switch (spellTier)
            {
                case 1:
                    amount = 5;
                    break;
                case 2:
                    amount = 6;
                    break;
                case 3:
                    amount = 7;
                    break;
                case 4:
                    amount = 8;
                    break;
                case 5:
                    amount = 10;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(creature, target.Object, Skill.ForceAlter, ForceBalanceType.Dark);

            // +/- percent change based on resistance
            float delta = 0.01f * result.Delta;
            amount = amount + (int)(amount * delta);

            if (target.GetLocalInt("FORCE_DRAIN_IMMUNITY") == 1)
            {
                amount = 0;
            }

            creature.AssignCommand(() =>
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(amount, DamageType.Negative), target);
            });

            // Only apply a heal if caster is not at max HP. Otherwise they'll get unnecessary spam.
            if (creature.CurrentHP < creature.MaxHP)
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(amount), creature);
            }

            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, Skill.ForceAlter);
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Com_Hit_Negative), target);
        }
    }
}

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
    public class ForceLightning : IPerk
    {
        public PerkType PerkType => PerkType.ForceLightning;
        public string Name => "Force Lightning";
        public bool IsActive => true;
        public string Description => "Deals electrical damage over time to a single target.";
        public PerkCategoryType Category => PerkCategoryType.ForceAlter;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ForceLightning;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Dark;
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
                1, new PerkLevel(4, "Damages a single target for 10 HP every three seconds.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 30},
                })
            },
            {
                2, new PerkLevel(4, "Damages a single target for 12 HP every three seconds.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 45},
                })
            },
            {
                3, new PerkLevel(5, "Damages a single target for 14 HP every three seconds.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 60},
                })
            },
            {
                4, new PerkLevel(5, "Damages a single target for 16 HP every three seconds.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 75},
                })
            },
            {
                5, new PerkLevel(6, "Damages a single target for 20 HP every three seconds.", SpecializationType.Consular,
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
                    new PerkFeat {Feat = Feat.ForceLightning1, BaseFPCost = 0, ConcentrationFPCost = 2, ConcentrationTickInterval = 3}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceLightning2, BaseFPCost = 0, ConcentrationFPCost = 2, ConcentrationTickInterval = 3}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceLightning3, BaseFPCost = 0, ConcentrationFPCost = 2, ConcentrationTickInterval = 3}
                }
            },
            {
                4, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceLightning4, BaseFPCost = 0, ConcentrationFPCost = 2, ConcentrationTickInterval = 3}
                }
            },
            {
                5, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceLightning5, BaseFPCost = 0, ConcentrationFPCost = 2, ConcentrationTickInterval = 3}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick)
        {
            int amount;

            switch (spellTier)
            {
                case 1:
                    amount = 10;
                    break;
                case 2:
                    amount = 12;
                    break;
                case 3:
                    amount = 14;
                    break;
                case 4:
                    amount = 16;
                    break;
                case 5:
                    amount = 20;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(creature, target.Object, Skill.ForceAlter, ForceBalanceType.Dark);

            // +/- percent change based on resistance
            float delta = 0.01f * result.Delta;
            amount = amount + (int)(amount * delta);

            creature.AssignCommand(() =>
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(amount, DamageType.Electrical), target);
            });

            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, Skill.ForceAlter);
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Lightning_S), target);
        }
    }
}

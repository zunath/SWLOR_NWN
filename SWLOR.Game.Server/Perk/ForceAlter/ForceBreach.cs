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
    public class ForceBreach : IPerk
    {
        public PerkType PerkType => PerkType.ForceBreach;
        public string Name => "Force Breach";
        public bool IsActive => true;
        public string Description => "Deals direct damage to a single target.";
        public PerkCategoryType Category => PerkCategoryType.ForceAlter;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ForceBreach;
        public PerkExecutionType ExecutionType => PerkExecutionType.ForceAbility;
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
            int damage;

            switch (spellTier)
            {
                case 1:
                    damage = 100;
                    break;
                case 2:
                    damage = 125;
                    break;
                case 3:
                    damage = 160;
                    break;
                case 4:
                    damage = 200;
                    break;
                case 5:
                    damage = 250;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(creature, target.Object, Skill.ForceAlter, ForceBalanceType.Dark, true);

            // +/- percent change based on resistance
            float delta = 0.01f * result.Delta;
            damage = damage + (int)(damage * delta);

            creature.AssignCommand(() =>
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage), target);
            });

            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToNPCForSkill(creature.Object, target, Skill.ForceAlter);
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Silence), target);
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
                1, new PerkLevel(4, "Deals 100 damage to a single target.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 50},
                })
            },
            {
                2, new PerkLevel(5, "Deals 125 damage to a single target.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 60},
                })
            },
            {
                3, new PerkLevel(6, "Deals 160 damage to a single target.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 70},
                })
            },
            {
                4, new PerkLevel(7, "Deals 200 damage to a single target.", SpecializationType.Consular,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceAlter, 80},
                })
            },
            {
                5, new PerkLevel(8, "Deals 250 damage to a single target.", SpecializationType.Consular,
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
                    new PerkFeat {Feat = Feat.ForceBreach1, BaseFPCost = 8, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceBreach2, BaseFPCost = 10, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceBreach3, BaseFPCost = 12, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
            {
                4, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceBreach4, BaseFPCost = 14, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
            {
                5, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ForceBreach5, BaseFPCost = 16, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

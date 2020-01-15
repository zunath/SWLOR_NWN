using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class Chi : IPerk
    {
        public PerkType PerkType => PerkType.Chi;
        public string Name => "Chi";
        public bool IsActive => true;
        public string Description => "Restores the user's HP. At rank 7, this ability also heals all party members within area of effect.";
        public PerkCategoryType Category => PerkCategoryType.MartialArts;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Chi;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => true;
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
            int wisdom = creature.WisdomModifier;
            int constitution = creature.ConstitutionModifier;
            int min = 1 + wisdom / 2 + constitution / 3;

            // Rank 7 and up: AOE heal party members
            if (perkLevel >= 7)
            {
                var members = creature.PartyMembers.Where(x => Equals(x, creature) ||
                                                             _.GetDistanceBetween(creature, x) <= 10.0f && x.CurrentHP < x.MaxHP);
                foreach (var member in members)
                {
                    DoHeal(member, perkLevel, min);
                }
            }
            else
            {
                DoHeal(target, perkLevel, min);
            }
        }

        private void DoHeal(NWObject target, int perkLevel, int minimum)
        {
            float percentage = perkLevel * 0.10f;
            int heal = (int)(target.MaxHP * percentage);

            heal = RandomService.Random(minimum, heal);

            _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(heal), target);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Healing_G), target);
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
                1, new PerkLevel(3, "Restores up to 10% of maximum HP",
                new Dictionary<Skill, int>
                {
                    { Skill.MartialArts, 5},
                })
            },
            {
                2, new PerkLevel(4, "Restores up to 20% of maximum HP",
                new Dictionary<Skill, int>
                {
                    { Skill.MartialArts, 15},
                })
            },
            {
                3, new PerkLevel(5, "Restores up to 30% of maximum HP",
                new Dictionary<Skill, int>
                {
                    { Skill.MartialArts, 30},
                })
            },
            {
                4, new PerkLevel(5, "Restores up to 40% of maximum HP",
                new Dictionary<Skill, int>
                {
                    { Skill.MartialArts, 45},
                })
            },
            {
                5, new PerkLevel(6, "Restores up to 50% of maximum HP",
                new Dictionary<Skill, int>
                {
                    { Skill.MartialArts, 60},
                })
            },
            {
                6, new PerkLevel(6, "Restores up to 60% of maximum HP",
                new Dictionary<Skill, int>
                {
                    { Skill.MartialArts, 75},
                })
            },
            {
                7, new PerkLevel(7, "Restores up to 70% of maximum HP. All party members within range also receive the healing.",
                new Dictionary<Skill, int>
                {
                    { Skill.MartialArts, 90},
                })
            },
            {
                8, new PerkLevel(7, "Restores up to 80% of maximum HP. All party members within range also receive the healing.",
                new Dictionary<Skill, int>
                {
                    { Skill.MartialArts, 100},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.Chi, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

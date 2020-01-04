using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using Skill = SWLOR.Game.Server.Enumeration.Skill;


namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class SneakAttack : IPerk
    {
        public PerkType PerkType => PerkType.SneakAttack;
        public string Name => "Sneak Attack";
        public bool IsActive => true;
        public string Description => "Deals extreme damage to a target on your next attack. Highest amount of damage is done from behind the target. Must be equipped with a finesse blade.";
        public PerkCategoryType Category => PerkCategoryType.OneHandedFinesseVibroblades;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.SneakAttack;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 120;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.TargetOnly;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            NWItem weapon = oPC.RightHand;

            if (weapon.CustomItemType != CustomItemType.FinesseVibroblade)
                return "You must be equipped with a finesse blade to use that ability";

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
            int perkRank = PerkService.GetCreaturePerkLevel(oPC, PerkType.SneakAttack);
            float cooldown = baseCooldownTime;

            if (perkRank == 2)
            {
                cooldown -= 30f;
            }
            else if (perkRank > 2)
            {
                cooldown -= 60f;
            }

            return cooldown;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            float minimum = creature.Facing - 20;
            float maximum = creature.Facing + 20;

            if (target.Facing >= minimum &&
                target.Facing <= maximum)
            {
                // Mark the player as committing a sneak attack.
                // This is later picked up in the OnApplyDamage event.
                creature.SetLocalInt("SNEAK_ATTACK_ACTIVE", 1);
            }
            else
            {
                creature.SetLocalInt("SNEAK_ATTACK_ACTIVE", 2);
            }
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
                1, new PerkLevel(4, "Deals 2x damage from behind, 1.5x damage from any other direction.",
                new Dictionary<Skill, int>
                {
                    { Skill.OneHanded, 10},
                })
            },
            {
                4, new PerkLevel(6, "Deals 3x damage from behind, 2x damage from any other direction. Reduces cooldown by 1 minute.",
                new Dictionary<Skill, int>
                {
                    { Skill.OneHanded, 40},
                })
            },
            {
                2, new PerkLevel(4, "Deals 2x damage from behind, 1.5x damage from any other direction. Reduces cooldown by 30 seconds.",
                new Dictionary<Skill, int>
                {
                    { Skill.OneHanded, 20},
                })
            },
            {
                3, new PerkLevel(5, "Deals 2x damage from behind, 1.5x damage from any other direction. Reduces cooldown by 1 minute.",
                new Dictionary<Skill, int>
                {
                    { Skill.OneHanded, 30},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.SneakAttack, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            throw new NotImplementedException();
        }
    }
}

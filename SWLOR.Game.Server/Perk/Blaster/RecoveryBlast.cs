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

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class RecoveryBlast : IPerk
    {
        public PerkType PerkType => PerkType.RecoveryBlast;
        public string Name => "Recovery Blast";
        public bool IsActive => true;
        public string Description => "Your next attack deals no damage but all party members within range receive healing. Must be equipped with a blaster rifle.";
        public PerkCategoryType Category => PerkCategoryType.BlastersBlasterRifles;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.RecoveryBlast;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.BlasterRifle)
                return "Must be equipped with a blaster rifle to use that ability.";

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
            // Mark the player as performing a recovery blast.
            // This is later picked up in the OnApplyDamage event to reduce all damage to 0.
            creature.SetLocalInt("RECOVERY_BLAST_ACTIVE", 1);

            var members = creature.PartyMembers.Where(x => _.GetDistanceBetween(x, target) <= 10.0f);
            int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky);

            foreach (var member in members)
            {
                HealTarget(member, perkLevel, luck);
            }
        }

        private void HealTarget(NWCreature member, int level, int luck)
        {
            int amount;

            switch (level)
            {
                case 1:
                    amount = RandomService.D12(1);
                    break;
                case 2:
                    amount = RandomService.D8(2);
                    break;
                case 3:
                    amount = RandomService.D8(3);
                    break;
                case 4:
                    amount = RandomService.D8(4);
                    break;
                case 5:
                    amount = RandomService.D8(5);
                    break;
                case 6:
                    amount = RandomService.D8(6);
                    break;
                default: return;
            }

            if (RandomService.D100(1) <= luck)
            {
                amount *= 2;
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(amount), member);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Healing_S), member);
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
                1, new PerkLevel(2, "1d12 HP restored",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 10},
                })
            },
            {
                2, new PerkLevel(2, "2d8 HP restored",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 20},
                })
            },
            {
                3, new PerkLevel(3, "3d8 HP restored",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 30},
                })
            },
            {
                4, new PerkLevel(3, "4d8 HP restored",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 40},
                })
            },
            {
                5, new PerkLevel(4, "5d8 HP restored",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 50},
                })
            },
            {
                6, new PerkLevel(4, "6d8 HP restored",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 60},
                })
            },
        };

        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.RecoveryBlast, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

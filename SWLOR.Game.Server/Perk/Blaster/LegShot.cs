using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class LegShot : IPerk
    {
        public PerkType PerkType => PerkType.LegShot;
        public string Name => "Leg Shot";
        public bool IsActive => true;
        public string Description => "Your next attack deals extra piercing damage and immobilizes your target for a short period of time. Must be equipped with a blaster pistol.";
        public PerkCategoryType Category => PerkCategoryType.BlastersBlasterPistols;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.LegShot;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.BlasterPistol)
                return "Must be equipped with a blaster pistol to use that ability.";

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

        public void OnImpact(NWCreature creature, NWObject target, int level, int spellTier)
        {
            int damage;
            float duration;

            switch (level)
            {
                case 1:
                    damage = RandomService.D4(1);
                    duration = 6;
                    break;
                case 2:
                    damage = RandomService.D8(1);
                    duration = 6;
                    break;
                case 3:
                    damage = RandomService.D8(2);
                    duration = 6;
                    break;
                case 4:
                    damage = RandomService.D8(2);
                    duration = 12;
                    break;
                case 5:
                    damage = RandomService.D8(3);
                    duration = 12;
                    break;
                case 6:
                    damage = RandomService.D8(4);
                    duration = 12;
                    break;
                case 7:
                    damage = RandomService.D8(5);
                    duration = 12;
                    break;
                case 8:
                    damage = RandomService.D8(5);
                    duration = 18;
                    break;
                case 9:
                    damage = RandomService.D8(6);
                    duration = 24;
                    break;
                default: return;
            }


            _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage, DamageType.Piercing), target);
            _.ApplyEffectToObject(DurationType.Temporary, _.EffectCutsceneImmobilize(), target, duration);
            _.ApplyEffectToObject(DurationType.Temporary, _.EffectVisualEffect(Vfx.Vfx_Imp_Acid_L), target, duration);
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
                1, new PerkLevel(2, "1d4 damage, immobilization lasts 6 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 10},
                })
            },
            {
                2, new PerkLevel(2, "1d8 damage, immobilization lasts 6 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 20},
                })
            },
            {
                3, new PerkLevel(3, "2d8 damage, immobilization lasts 6 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 30},
                })
            },
            {
                4, new PerkLevel(3, "2d8 damage, immobilization lasts 12 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 40},
                })
            },
            {
                5, new PerkLevel(4, "3d8 damage, immobilization lasts 12 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 50},
                })
            },
            {
                6, new PerkLevel(4, "4d8 damage, immobilization lasts 12 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 60},
                })
            },
            {
                7, new PerkLevel(5, "5d8 damage, immobilization lasts 12 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 70},
                })
            },
            {
                8, new PerkLevel(5, "5d8 damage, immobilization lasts 18 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 80},
                })
            },
            {
                9, new PerkLevel(6, "6d8 damage, immobilization lasts 24 seconds",
                new Dictionary<SkillType, int>
                {
                    { SkillType.Firearms, 90},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.LegShot, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.CustomEffect;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.General
{
    public class Rest : IPerk
    {
        public PerkType PerkType => PerkType.Rest;
        public string Name => "Rest";
        public bool IsActive => true;
        public string Description => "Restores HP quickly as long as you stay in one place. Must be out of combat to use. Moving or combat will interrupt the ability. Shares a cooldown with the Meditate perk.";
        public PerkCategoryType Category => PerkCategoryType.General;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.RestAndMeditate;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 1;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (!RestEffect.CanRest(oPC))
                return "You cannot rest while you or a party member are in combat.";

            return string.Empty;
        }

        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 1f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            int perkLevel = PerkService.GetCreaturePerkLevel(oPC, PerkType.Rest);

            switch (perkLevel)
            {
                case 1: return 300.0f;
                case 2: return 270.0f;
                case 3:
                case 4:
                    return 240.0f;
                case 5:
                    return 210.0f;
                case 6:
                case 7:
                    return 180.0f;
                default: return 300.0f;
            }
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            CustomEffectService.ApplyCustomEffect(creature, creature, CustomEffectType.Rest, -1, 0, null);
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
                1, new PerkLevel(2, "Restores 6 HP every 6 seconds. Recast time: 5 minutes",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                2, new PerkLevel(3, "Restores 6 HP every 6 seconds. Recast time: 4 minutes, 30 seconds",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                3, new PerkLevel(4, "Restores 6 HP every 6 seconds. Recast time: 4 minutes",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                4, new PerkLevel(5, "Restores 10 HP every 6 seconds. Recast time: 4 minutes",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                5, new PerkLevel(6, "Restores 10 HP every 6 seconds. Recast time: 3 minutes, 30 seconds",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                6, new PerkLevel(7, "Restores 10 HP every 6 seconds. Recast time: 3 minutes",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                7, new PerkLevel(8, "Restores 14 HP every 6 seconds. Recast time: 3 minutes",
                new Dictionary<Skill, int>
                {

                })
            },
        };

        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.Rest, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.General
{
    public class Dash : IPerk
    {
        public PerkType PerkType => PerkType.Dash;
        public string Name => "Dash";
        public bool IsActive => true;
        public string Description => "Increases your movement speed for a short period of time.";
        public PerkCategoryType Category => PerkCategoryType.General;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Dash;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.FireForget_Victory2;

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
            float duration = 60.0f;
            int speed;

            switch (perkLevel)
            {
                case 1:
                    speed = 25;
                    break;
                case 2:
                    speed = 30;
                    break;
                case 3:
                    speed = 35;
                    break;
                case 4:
                    speed = 40;
                    break;
                case 5:
                    speed = 45;
                    break;
                case 6:
                    speed = 50;
                    break;
                case 7:
                    speed = 50;
                    duration = 120.0f;
                    break;
                default: return;
            }

            if (creature.DexterityModifier > 0)
            {
                duration = duration + creature.DexterityModifier * 5;
            }

            Effect movement = _.EffectMovementSpeedIncrease(speed);
            movement = _.TagEffect(movement, "DASH");

            _.ApplyEffectToObject(DurationType.Temporary, movement, target, duration);
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
                1, new PerkLevel(2, "+25% speed, lasts 1 minute",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                2, new PerkLevel(2, "+30% speed, lasts 1 minute",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                3, new PerkLevel(3, "+35% speed, lasts 1 minute",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                4, new PerkLevel(3, "+40% speed, lasts 1 minute",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                5, new PerkLevel(3, "+45% speed, lasts 1 minute",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                6, new PerkLevel(4, "+50% speed, lasts 1 minute",
                new Dictionary<Skill, int>
                {

                })
            },
            {
                7, new PerkLevel(5, "+50% speed, lasts 2 minutes",
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
                    new PerkFeat {Feat = Feat.Dash, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

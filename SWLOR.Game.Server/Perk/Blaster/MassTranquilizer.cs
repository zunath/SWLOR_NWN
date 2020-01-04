using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class MassTranquilizer : IPerk
    {
        public PerkType PerkType => PerkType.MassTranquilizer;
        public string Name => "Mass Tranquilizer";
        public bool IsActive => true;
        public string Description => "Your next attack will tranquilize all creatures within range of your target, putting them to sleep for a short time. Damage will break the effect prematurely. Must be equipped with a Blaster Rifle to use. Length of effect depends on Tranquilizer perk.";
        public PerkCategoryType Category => PerkCategoryType.BlastersBlasterRifles;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.MassTranquilizer;
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
            int massLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.MassTranquilizer);
            int tranqLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.Tranquilizer);
            int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky);
            float duration;
            float range = 5 * massLevel;

            switch (tranqLevel)
            {
                case 0:
                    duration = 6;
                    break;
                case 1:
                    duration = 12;
                    break;
                case 2:
                    duration = 24;
                    break;
                case 3:
                    duration = 36;
                    break;
                case 4:
                    duration = 48;
                    break;
                case 5:
                    duration = 60;
                    break;
                case 6:
                    duration = 72;
                    break;
                case 7:
                    duration = 84;
                    break;
                case 8:
                    duration = 96;
                    break;
                case 9:
                    duration = 108;
                    break;
                case 10:
                    duration = 120;
                    break;
                default: return;
            }

            if (RandomService.D100(1) <= luck)
            {
                duration *= 2;
                creature.SendMessage("Lucky shot!");
            }


            // Check if Mind Shield is on target.
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);
            if (concentrationEffect.Type == PerkType.MindShield ||
                GetIsImmune(target, ImmunityType.MindSpells) == true)
            {
                creature.SendMessage("Your target is immune to tranquilization effects.");
            }
            else
            {
                // Apply to the target.
                if (!RemoveExistingEffect(target, duration))
                {
                    target.SetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN", 1);

                    Effect effect = _.EffectDazed();
                    effect = _.EffectLinkEffects(effect, _.EffectVisualEffect(Vfx.Vfx_Dur_Iounstone_Blue));
                    effect = _.TagEffect(effect, "TRANQUILIZER_EFFECT");

                    _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
                }
            }



            // Iterate over all nearby hostiles. Apply the effect to them if they meet the criteria.
            int current = 1;
            NWCreature nearest = _.GetNearestCreature((int)CreatureType.IsAlive, 1, target, current);
            while (nearest.IsValid)
            {
                float distance = _.GetDistanceBetween(nearest, target);
                // Check distance. Exit loop if we're too far.
                if (distance > range) break;

                concentrationEffect = AbilityService.GetActiveConcentrationEffect(nearest);

                // If this creature isn't hostile to the attacking player or if this creature is already tranquilized, move to the next one.
                if (_.GetIsReactionTypeHostile(nearest, creature) == false ||
                    nearest.Object == target.Object ||
                    RemoveExistingEffect(nearest, duration) ||
                    concentrationEffect.Type == PerkType.MindShield)
                {
                    current++;
                    nearest = _.GetNearestCreature((int)CreatureType.IsAlive, 1, target, current);
                    continue;
                }

                target.SetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN", 1);
                Effect effect = _.EffectDazed();
                effect = _.EffectLinkEffects(effect, _.EffectVisualEffect(Vfx.Vfx_Dur_Iounstone_Blue));
                effect = _.TagEffect(effect, "TRANQUILIZER_EFFECT");
                _.ApplyEffectToObject(DurationType.Temporary, effect, nearest, duration);

                current++;
                nearest = _.GetNearestCreature((int)CreatureType.IsAlive, 1, target, current);
            }

        }

        private bool RemoveExistingEffect(NWObject target, float duration)
        {
            Effect effect = target.Effects.FirstOrDefault(x => _.GetEffectTag(x) == "TRANQUILIZER_EFFECT");
            if (effect == null) return false;

            if (_.GetEffectDurationRemaining(effect) >= duration) return true;
            _.RemoveEffect(target, effect);
            return false;
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
                1, new PerkLevel(5, "Range: 5 meters",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 25},
                })
            },
            {
                2, new PerkLevel(5, "Range: 10 meters",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 30},
                })
            },
            {
                3, new PerkLevel(5, "Range: 15 meters",
                new Dictionary<Skill, int>
                {
                    { Skill.Blasters, 35},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.MassTranquilizer, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}

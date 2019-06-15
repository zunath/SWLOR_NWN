using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class MassTranquilizer : IPerkHandler
    {
        public PerkType PerkType => PerkType.MassTranquilizer;

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

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
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
            if (concentrationEffect.Type == PerkType.MindShield)
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
                    effect = _.EffectLinkEffects(effect, _.EffectVisualEffect(VFX_DUR_IOUNSTONE_BLUE));
                    effect = _.TagEffect(effect, "TRANQUILIZER_EFFECT");

                    _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, effect, target, duration);
                }
            }



            // Iterate over all nearby hostiles. Apply the effect to them if they meet the criteria.
            int current = 1;
            NWCreature nearest = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, target, current);
            while (nearest.IsValid)
            {
                float distance = _.GetDistanceBetween(nearest, target);
                // Check distance. Exit loop if we're too far.
                if (distance > range) break;

                concentrationEffect = AbilityService.GetActiveConcentrationEffect(nearest);

                // If this creature isn't hostile to the attacking player or if this creature is already tranquilized, move to the next one.
                if (_.GetIsReactionTypeHostile(nearest, creature) == FALSE ||
                    nearest.Object == target.Object ||
                    RemoveExistingEffect(nearest, duration) ||
                    concentrationEffect.Type == PerkType.MindShield)
                {
                    current++;
                    nearest = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, target, current);
                    continue;
                }

                target.SetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN", 1);
                Effect effect = _.EffectDazed();
                effect = _.EffectLinkEffects(effect, _.EffectVisualEffect(VFX_DUR_IOUNSTONE_BLUE));
                effect = _.TagEffect(effect, "TRANQUILIZER_EFFECT");
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, effect, nearest, duration);

                current++;
                nearest = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, target, current);
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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

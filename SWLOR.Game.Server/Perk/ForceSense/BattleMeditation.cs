using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class BattleMeditation: IPerkHandler
    {
        public PerkType PerkType => PerkType.BattleMeditation;
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWPlayer oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellTier)
        {
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
        }

        public void OnRemoved(NWPlayer oPC)
        {
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWPlayer player, NWObject target, int perkLevel, int tick)
        {
            float radiusSize = _.RADIUS_SIZE_SMALL;

            NWCreature targetCreature = targetCreature = _.GetFirstObjectInShape(_.SHAPE_SPHERE, radiusSize, player.Location, 1, _.OBJECT_TYPE_CREATURE);
            while (targetCreature.IsValid)
            {
                int amount = 0;

                // Handle effects for differing spellTier values
                switch (perkLevel)
                {
                    case 1:
                        amount = 5;

                        if (_.GetIsReactionTypeHostile(targetCreature, player) == 1)
                        {
                            continue;        
                        }                            
                        break;
                    case 2:
                        amount = 10;

                        if (_.GetIsReactionTypeHostile(targetCreature, player) == 1)
                        {
                            continue;
                        }
                        break;
                    case 3:
                        amount = 10;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(perkLevel));
                }

                Effect effect = new Effect();

                if (_.GetIsReactionTypeHostile(targetCreature, player) == 1)
                {
                    effect = _.EffectACDecrease(amount);
                    effect = _.EffectLinkEffects(effect, _.EffectAttackDecrease(amount));
                }
                else
                {
                    effect = _.EffectACIncrease(amount);
                    effect = _.EffectLinkEffects(effect, _.EffectAttackIncrease(amount));
                }

                player.AssignCommand(() =>
                {
                    _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, targetCreature, 6.1f);
                });

                targetCreature = _.GetNextObjectInShape(_.SHAPE_SPHERE, radiusSize, player.Location, 1, _.OBJECT_TYPE_CREATURE);
            }
            
        }
    }
}

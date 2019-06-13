using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class Confusion : IPerkHandler
    {
        public PerkType PerkType => PerkType.Confusion;
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            switch (spellTier)
            {                
                case 1:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    NWCreature targetCreature = oTarget.Object;
                    if (targetCreature.RacialType == (int)CustomRaceType.Robot)
                        return "This ability cannot be used on droids.";
                    break;
                case 2:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

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

        public void OnConcentrationTick(NWPlayer player, NWObject target, int spellTier, int tick)
        {
            ApplyEffect(player, target, spellTier);
        }

        private void ApplyEffect(NWPlayer player, NWObject target, int spellTier)
        {
            float radiusSize = _.RADIUS_SIZE_SMALL;            

            Effect confusionEffect = _.EffectConfused();

            // Handle effects for differing spellTier values
            switch (spellTier)
            {
                case 1:
                    if ((player.Wisdom > _.GetAbilityModifier(_.ABILITY_WISDOM, target) || player == target) && _.GetDistanceBetween(player.Object, target) <= radiusSize)
                    {
                        player.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, confusionEffect, target, 6.1f);
                        });
                        SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    }
                    break;
                case 2:
                    NWCreature targetCreature = _.GetFirstObjectInShape(_.SHAPE_SPHERE, radiusSize, player.Location, 1, _.OBJECT_TYPE_CREATURE);
                    while (targetCreature.IsValid)
                    {
                        if (targetCreature.RacialType == (int)CustomRaceType.Robot || _.GetIsReactionTypeHostile(targetCreature, player) == 0)
                        {                            
                            // Do nothing against droids or non-hostile creatures, skip object
                            continue;
                        }

                        if (player.Wisdom > targetCreature.Wisdom)
                        {
                            var creature = targetCreature;
                            player.AssignCommand(() =>
                            {
                                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, confusionEffect, creature, 6.1f);
                            });
                            SkillService.RegisterPCToNPCForSkill(player, targetCreature, SkillType.ForceAlter);
                        }

                        targetCreature = _.GetNextObjectInShape(_.SHAPE_SPHERE, radiusSize, player.Location, 1, _.OBJECT_TYPE_CREATURE);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
        }
    }
}

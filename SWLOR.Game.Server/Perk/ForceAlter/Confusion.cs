﻿using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class Confusion : IPerkHandler
    {
        public PerkType PerkType => PerkType.Confusion;
        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            switch (spellTier)
            {
                case 1:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    NWCreature targetCreature = oTarget.Object;
                    if (targetCreature.RacialType == RacialType.Robot)
                        return "This ability cannot be used on droids.";
                    break;
                case 2:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

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
            if (spellTier == 1) return 30f; // 5 minutes
            else if (spellTier == 2) return 300f; // 30 minutes

            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            ApplyEffect(creature, target, spellTier);
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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick)
        {
        }

        private void ApplyEffect(NWCreature creature, NWObject target, int spellTier)
        {
            float radiusSize = 10f;

            Effect confusionEffect = _.EffectCharmed();

            // Handle effects for differing spellTier values
            switch (spellTier)
            {
                case 1:
                    if (creature == target || (creature.Wisdom > _.GetAbilityModifier(AbilityType.Wisdom, target) && _.GetDistanceBetween(creature.Object, target) <= radiusSize))
                    {
                        creature.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(DurationType.Temporary, confusionEffect, target, 18.1f);
                            // Play VFX
                            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.Vfx_Imp_Pdk_Final_Stand), target);
                            // Success confirmation
                            creature.SendMessage("Confusion successful.");
                        });
                        if (!creature.IsPlayer)
                        {
                            SkillService.RegisterPCToNPCForSkill(creature.Object, target, SkillType.ForceAlter);
                        }
                    }
                    else
                    {
                        creature.SendMessage("Confusion failed.");
                    }
                    break;
                case 2:
                    NWCreature targetCreature = _.GetFirstObjectInShape(Shape.Sphere, radiusSize, creature.Location, true, ObjectType.Creature);
                    while (targetCreature.IsValid)
                    {
                        if (targetCreature.RacialType == RacialType.Robot || _.GetIsReactionTypeHostile(targetCreature, creature) == false)
                        {
                            // Do nothing against droids or non-hostile creatures, skip object
                            targetCreature = _.GetNextObjectInShape(Shape.Sphere, radiusSize, creature.Location, true, ObjectType.Creature);
                            continue;
                        }

                        if (creature.Wisdom > targetCreature.Wisdom)
                        {
                            var targetCreatureCopy = targetCreature; // Closure can modify the iteration variable so we copy it first.
                            creature.AssignCommand(() =>
                            {
                                _.ApplyEffectToObject(DurationType.Temporary, confusionEffect, targetCreatureCopy, 18.1f);
                                // Play VFX
                                _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.Vfx_Imp_Pdk_Final_Stand), targetCreatureCopy);
                                creature.SendMessage("Confusion successful.");
                            });

                            if (!creature.IsPlayer)
                            {
                                SkillService.RegisterPCToNPCForSkill(creature.Object, targetCreature, SkillType.ForceAlter);
                            }
                        }
                        else
                        {
                            creature.SendMessage("Confusion failed.");
                        }

                        targetCreature = _.GetNextObjectInShape(Shape.Sphere, radiusSize, creature.Location, true, ObjectType.Creature);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
        }
    }
}

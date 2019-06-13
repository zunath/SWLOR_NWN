using System;
using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ThrowSaber: IPerkHandler
    {
        public PerkType PerkType => PerkType.ThrowSaber;
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            if (_.GetDistanceBetween(oPC, oTarget) > 15)
                return "You must be within 15 meters of your target.";
            
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
            int iDamage = 20; // player.RightHand.DamageBonus + player.IntelligenceModifier;
            int iRange = 15;
            int iCount = 1;
            float fDelay = 0;
            
            NWObject oObject;

            // If player is in stealth mode, force them out of stealth mode.
            if (_.GetActionMode(player.Object, ACTION_MODE_STEALTH) == 1)
                _.SetActionMode(player.Object, ACTION_MODE_STEALTH, 0);

            // Make the player face their target.
            _.ClearAllActions();
            BiowarePosition.TurnToFaceObject(target, player);

            player.AssignCommand(() => _.ActionPlayAnimation(30, 2));

            _.SendMessageToPC(player, "Level " + spellTier);

            // Handle effects for differing spellTier values
            switch (spellTier)
            {
                case 1:
                    iDamage = (int)(iDamage * 1.6);

                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectLinkEffects(_.EffectVisualEffect(VFX_IMP_SONIC), _.EffectDamage(iDamage, _.DAMAGE_TYPE_BASE_WEAPON)), target);
                    }, fDelay);
                    SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);

                    break;
                case 2:
                    iDamage = (int)(iDamage * 1.25);

                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectLinkEffects(_.EffectVisualEffect(VFX_IMP_SONIC), _.EffectDamage(iDamage, _.DAMAGE_TYPE_BASE_WEAPON)), target);
                    }, fDelay);
                    SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);

                    break;
                case 3:
                    iDamage = (int)(iDamage * 1.6);

                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectLinkEffects(_.EffectVisualEffect(VFX_IMP_SONIC), _.EffectDamage(iDamage, _.DAMAGE_TYPE_BASE_WEAPON)), target);
                    }, fDelay);
                    SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);

                    break;
                case 4:
                    iDamage = (int)(iDamage * 2.0);

                    // apply to target
                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectLinkEffects(_.EffectVisualEffect(VFX_IMP_SONIC), _.EffectDamage(iDamage, _.DAMAGE_TYPE_BASE_WEAPON)), target);
                    }, fDelay);
                    SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    iCount += 1;

                    // apply to next nearest creature in the spellcylinder
                    oObject = _.GetFirstObjectInShape(_.SHAPE_SPELLCONE, iRange, target.Location, 1, _.OBJECT_TYPE_CREATURE, _.GetPosition(player));
                    while (oObject.IsValid && iCount < 3)
                    {
                        if (oObject != target && oObject != player)
                        {
                            fDelay = _.GetDistanceBetween(player, oObject) / 10.0f;
                            var creature = oObject;
                            player.DelayAssignCommand(() =>
                            {
                                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectLinkEffects(_.EffectVisualEffect(VFX_IMP_SONIC), _.EffectDamage(iDamage, _.DAMAGE_TYPE_BASE_WEAPON)), creature);
                            }, fDelay);
                            SkillService.RegisterPCToNPCForSkill(player, oObject, SkillType.ForceAlter);
                            iCount += 1;
                        }
                        oObject = _.GetNextObjectInShape(_.SHAPE_SPELLCONE, iRange, target.Location, 1, _.OBJECT_TYPE_CREATURE, _.GetPosition(player));
                    }
                    break;
                case 5:
                    iDamage = (int)(iDamage * 2.5);

                    // apply to target
                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectLinkEffects(_.EffectVisualEffect(VFX_IMP_SONIC), _.EffectDamage(iDamage, _.DAMAGE_TYPE_BASE_WEAPON)), target);
                    }, fDelay);
                    SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    iCount += 1;

                    // apply to next nearest creature in the spellcylinder
                    oObject = _.GetFirstObjectInShape(_.SHAPE_SPELLCYLINDER, iRange, target.Location, 1, _.OBJECT_TYPE_CREATURE, _.GetPosition(player));
                    while (oObject.IsValid  && iCount < 4)
                    {
                        if (oObject != target && oObject != player)
                        {
                            fDelay = _.GetDistanceBetween(player, oObject) / 10.0f;
                            var creature = oObject;
                            player.DelayAssignCommand(() =>
                            {
                                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectLinkEffects(_.EffectVisualEffect(VFX_IMP_SONIC), _.EffectDamage(iDamage, _.DAMAGE_TYPE_BASE_WEAPON)), creature);
                            }, fDelay);
                            SkillService.RegisterPCToNPCForSkill(player, oObject, SkillType.ForceAlter);
                            iCount += 1;
                        }
                        oObject = _.GetNextObjectInShape(_.SHAPE_SPELLCYLINDER, iRange, target.Location, 1, _.OBJECT_TYPE_CREATURE, _.GetPosition(player));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
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
            
        }
    }
}

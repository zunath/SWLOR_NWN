using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ThrowSaber : IPerkHandler
    {
        public PerkType PerkType => PerkType.ThrowSaber;
        public string CanCastSpell(NWCreature creature, NWObject oTarget, int spellTier)
        {
            NWItem weapon = creature.RightHand;
            int weaponSize = StringToInt(Get2DAString("baseitems", "WeaponSize", (int)weapon.BaseItemType));
            int strengthMod = creature.StrengthModifier;
            float distance = _.GetDistanceBetween(creature, oTarget);

            if (distance > 15)
                return "You must be within 15 meters of your target.";
            if (!weapon.IsValid)
                return "You attempt to throw your fist. Nothing of consequence happens.";

            if (weapon.CustomItemType == CustomItemType.Lightsaber ||
                weapon.CustomItemType == CustomItemType.Saberstaff)
            {

                return string.Empty;
            }
            else if
                 (
                    (weaponSize == 1 && strengthMod < 1) || // weapon size tiny
                    (weaponSize == 2 && strengthMod < 2) || // weapon size small
                    (weaponSize == 3 && strengthMod < 5) || // weapon size medium
                    (weaponSize == 4 && strengthMod < 10) || // weapon size large
                    (weaponSize > 4 && strengthMod < 20) || // weapon size huge
                    (weapon.IsRanged)
                 )
            {
                NWObject droppedWeapon = _.CopyObject(weapon, creature.Location);
                DestroyObject(weapon);
                creature.ClearAllActions();
                creature.AssignCommand(() =>
                {
                    _.ActionPickUpItem(droppedWeapon);
                });
                return "You attempt to throw your the item in your hand. Due to your lack of strength, it falls to the ground in front of you and you try to pick it up quickly.";
            }
            else
            {
                return string.Empty;
            }

        }

        public int FPCost(NWCreature creature, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature creature, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature creature, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature player, NWObject target, int perkLevel, int spellTier)
        {
            NWItem weapon = player.RightHand;
            int iDamage;
            int iRange = 15;
            int iCount = 1;
            float fDelay = 0;

            int saberDamage = player.RightHand.DamageBonus;
            if (saberDamage > 40 ) saberDamage = 40;
            
            if (weapon.CustomItemType == CustomItemType.Lightsaber ||
                weapon.CustomItemType == CustomItemType.Saberstaff)
            {
                iDamage = saberDamage + RandomService.D6(2) + player.IntelligenceModifier + player.StrengthModifier;
            }
            else
            {
                iDamage = (int)weapon.Weight + player.StrengthModifier + (saberDamage / 2);
            }

            NWObject oObject;

            // If player is in stealth mode, force them out of stealth mode.
            if (_.GetActionMode(player.Object, ActionMode.Stealth) == true)
                _.SetActionMode(player.Object, ActionMode.Stealth, false);

            // Make the player face their target.
            _.ClearAllActions();
            BiowarePosition.TurnToFaceObject(target, player);

            player.AssignCommand(() => _.ActionPlayAnimation(Animation.LoopingCustom10, 2));

            var result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Universal);
            float delta = 0.01f * result.Delta;

            /*
            // reset phenotype
                player.DelayAssignCommand(() =>
                {
                    _.SetPhenoType(4, player);
                }, 2.0f);

                player.DelayAssignCommand(() =>
                {
                    _.SetPhenoType(iPheno, player);
                }, 2.5f);
                */

            // Handle effects for differing spellTier values
            switch (spellTier)
            {
                case 1:
                    iDamage = (int)(iDamage * 1.0);
                    iDamage = iDamage + (int)(iDamage * delta);

                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DurationType.Instant, _.EffectLinkEffects(_.EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), _.EffectDamage(iDamage, DamageType.BaseWeapon)), target);
                    }, fDelay);

                    if (player.IsPlayer)
                    {
                        SkillService.RegisterPCToNPCForSkill(player.Object, target, SkillType.ForceAlter);
                    }

                    break;
                case 2:
                    iDamage = (int)(iDamage * 1.25);
                    iDamage = iDamage + (int)(iDamage * delta);

                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DurationType.Instant, _.EffectLinkEffects(_.EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), _.EffectDamage(iDamage, DamageType.BaseWeapon)), target);
                    }, fDelay);

                    if (player.IsPlayer)
                    {
                        SkillService.RegisterPCToNPCForSkill(player.Object, target, SkillType.ForceAlter);
                    }

                    break;
                case 3:
                    iDamage = (int)(iDamage * 1.6);
                    iDamage = iDamage + (int)(iDamage * delta);

                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DurationType.Instant, _.EffectLinkEffects(_.EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), _.EffectDamage(iDamage, DamageType.BaseWeapon)), target);
                    }, fDelay);

                    if (player.IsPlayer)
                    {
                        SkillService.RegisterPCToNPCForSkill(player.Object, target, SkillType.ForceAlter);
                    }

                    break;
                case 4:
                    iDamage = (int)(iDamage * 2.0);
                    iDamage = iDamage + (int)(iDamage * delta);

                    // apply to target
                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DurationType.Instant, _.EffectLinkEffects(_.EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), _.EffectDamage(iDamage, DamageType.BaseWeapon)), target);
                    }, fDelay);

                    if (player.IsPlayer)
                    {
                        SkillService.RegisterPCToNPCForSkill(player.Object, target, SkillType.ForceAlter);
                    }

                    iCount += 1;

                    // apply to next nearest creature in the spellcylinder
                    oObject = _.GetFirstObjectInShape(Shape.SpellCone, iRange, target.Location, true, ObjectType.Creature, _.GetPosition(player));
                    while (oObject.IsValid && iCount < 3)
                    {
                        if (oObject != target && oObject != player)
                        {
                            fDelay = _.GetDistanceBetween(player, oObject) / 10.0f;
                            var creature = oObject;
                            player.DelayAssignCommand(() =>
                            {
                                _.ApplyEffectToObject(DurationType.Instant, _.EffectLinkEffects(_.EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), _.EffectDamage(iDamage, DamageType.BaseWeapon)), creature);
                            }, fDelay);

                            if (player.IsPlayer)
                            {
                                SkillService.RegisterPCToNPCForSkill(player.Object, oObject, SkillType.ForceAlter);
                            }
                            iCount += 1;
                        }
                        oObject = _.GetNextObjectInShape(Shape.SpellCone, iRange, target.Location, true, ObjectType.Creature, _.GetPosition(player));
                    }
                    break;
                case 5:
                    iDamage = (int)(iDamage * 2.5);
                    iDamage = iDamage + (int)(iDamage * delta);

                    // apply to target
                    fDelay = _.GetDistanceBetween(player, target) / 10.0f;

                    player.DelayAssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DurationType.Instant, _.EffectLinkEffects(_.EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), _.EffectDamage(iDamage, DamageType.BaseWeapon)), target);
                    }, fDelay);

                    if (player.IsPlayer)
                    {
                        SkillService.RegisterPCToNPCForSkill(player.Object, target, SkillType.ForceAlter);
                    }
                    iCount += 1;

                    // apply to next nearest creature in the spellcylinder
                    oObject = _.GetFirstObjectInShape(Shape.SpellCylinder, iRange, target.Location, true, ObjectType.Creature, _.GetPosition(player));
                    while (oObject.IsValid && iCount < 4)
                    {
                        if (oObject != target && oObject != player)
                        {
                            fDelay = _.GetDistanceBetween(player, oObject) / 10.0f;
                            var creature = oObject;
                            player.DelayAssignCommand(() =>
                            {
                                _.ApplyEffectToObject(DurationType.Instant, _.EffectLinkEffects(_.EffectVisualEffect(VisualEffect.Vfx_Imp_Sonic), _.EffectDamage(iDamage, DamageType.BaseWeapon)), creature);
                            }, fDelay);

                            if (player.IsPlayer)
                            {
                                SkillService.RegisterPCToNPCForSkill(player.Object, oObject, SkillType.ForceAlter);
                            }
                            iCount += 1;
                        }
                        oObject = _.GetNextObjectInShape(Shape.SpellCylinder, iRange, target.Location, true, ObjectType.Creature, _.GetPosition(player));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
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

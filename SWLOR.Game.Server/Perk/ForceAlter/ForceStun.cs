using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

using static NWN._;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ForceStun: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceStun;
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            NWCreature targetCreature = oTarget.Object;
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(targetCreature);

            switch (spellTier)
            {
                case 1:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (targetCreature.RacialType == (int)CustomRaceType.Robot)
                        return "This ability cannot be used on droids.";
                    if (concentrationEffect.Type == PerkType.MindShield)                    
                        return "Your target is immune to tranquilization effects.";                    
                    break;
                case 2:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (targetCreature.RacialType == (int)CustomRaceType.Robot)
                        return "This ability cannot be used on droids.";
                    if (concentrationEffect.Type == PerkType.MindShield)
                        return "Your target is immune to tranquilization effects.";
                    break;
                case 3:
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

        public void OnConcentrationTick(NWPlayer player, NWObject target, int perkLevel, int tick)
        {
            ApplyEffect(player, target, perkLevel);
        }

        private void ApplyEffect(NWPlayer player, NWObject target, int spellTier)
        {
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);
            AbilityResistanceResult result = new AbilityResistanceResult();
            float radiusSize = 10;
            NWCreature targetCreature;

            int effectCount = 0;
            Effect effectTranq = _.EffectDazed();
            effectTranq = _.EffectLinkEffects(effectTranq, _.EffectVisualEffect(VFX_DUR_IOUNSTONE_BLUE));
            effectTranq = _.TagEffect(effectTranq, "TRANQUILIZER_EFFECT");

            Effect effectAbilityDecrease = _.EffectACDecrease(5);
            effectAbilityDecrease = _.EffectLinkEffects(effectTranq, _.EffectAttackDecrease(5));
            effectAbilityDecrease = _.TagEffect(effectTranq, "TRANQUILIZER_EFFECT");

            // Handle effects for differing spellTier values
            switch (spellTier)
            {
                case 1:
                    concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);
                    if (concentrationEffect.Type == PerkType.MindShield)
                    {
                        player.SendMessage("Your target is immune to tranquilization effects.");
                        return;
                    }

                    targetCreature = target.Object;

                    result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

                    if (!result.IsResisted)
                    {
                        player.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectTranq, target, 6.1f);
                        });
                        SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    }
                    else
                    {
                        player.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectAbilityDecrease, target, 6.1f);
                        });
                        SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    }

                    break;
                case 2:
                    // handle target
                    concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);
                    if (concentrationEffect.Type == PerkType.MindShield)
                    {
                        player.SendMessage("Your target is immune to tranquilization effects.");
                        return;
                    }

                    targetCreature = target.Object;

                    result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

                    if (!result.IsResisted)
                    {
                        player.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectTranq, target, 6.1f);
                        });
                        SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    }
                    else
                    {
                        player.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectAbilityDecrease, target, 6.1f);
                        });
                        SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    }

                    targetCreature = _.GetFirstObjectInShape(_.SHAPE_SPHERE, radiusSize, player.Location, 1, _.OBJECT_TYPE_CREATURE);                    
                    while (targetCreature.IsValid && effectCount < 1)
                    {
                        // handle target
                        concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);
                        if (concentrationEffect.Type == PerkType.MindShield)
                        {
                            continue;
                        }

                        targetCreature = target.Object;

                        result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

                        if (!result.IsResisted)
                        {
                            player.AssignCommand(() =>
                            {
                                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectTranq, target, 6.1f);
                            });
                            SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                        }
                        else
                        {
                            player.AssignCommand(() =>
                            {
                                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectAbilityDecrease, target, 6.1f);
                            });
                            SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                        }
                        effectCount += 1;
                    }
                    break;
                case 3:
                    // handle target
                    concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);
                    if (concentrationEffect.Type == PerkType.MindShield)
                    {
                        player.SendMessage("Your target is immune to tranquilization effects.");
                        return;
                    }

                    targetCreature = target.Object;

                    result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

                    if (!result.IsResisted)
                    {
                        player.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectTranq, target, 6.1f);
                        });
                        SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    }
                    else
                    {
                        player.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectAbilityDecrease, target, 6.1f);
                        });
                        SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                    }

                    targetCreature = _.GetFirstObjectInShape(_.SHAPE_SPHERE, radiusSize, player.Location, 1, _.OBJECT_TYPE_CREATURE);
                    while (targetCreature.IsValid)
                    {
                        // handle target
                        concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);
                        if (concentrationEffect.Type == PerkType.MindShield)
                        {
                            continue;
                        }

                        targetCreature = target.Object;

                        result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

                        if (!result.IsResisted)
                        {
                            player.AssignCommand(() =>
                            {
                                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectTranq, target, 6.1f);
                            });
                            SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                        }
                        else
                        {
                            player.AssignCommand(() =>
                            {
                                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectAbilityDecrease, target, 6.1f);
                            });
                            SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
                        }
                        effectCount += 1;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
        }
    }
}

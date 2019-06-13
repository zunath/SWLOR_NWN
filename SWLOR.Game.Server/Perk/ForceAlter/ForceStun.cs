using System;
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

        private void RunEffect(NWPlayer player, NWObject target)
        {
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);

            if (concentrationEffect.Type == PerkType.MindShield)
            {
                player.SendMessage("Your target is immune to tranquilization effects.");
                return;
            }

            AbilityResistanceResult result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);
            
            // Tranquilization effect - Daze target(s). Occurs on succeeding the DC check.
            Effect successEffect = EffectDazed();
            successEffect = EffectLinkEffects(successEffect, EffectVisualEffect(VFX_DUR_IOUNSTONE_BLUE));
            successEffect = TagEffect(successEffect, "TRANQUILIZER_EFFECT");

            // AC & AB decrease effect - Occurs on failing the DC check.
            Effect failureEffect = EffectLinkEffects(EffectAttackDecrease(5), EffectACDecrease(5));


            if (!result.IsResisted)
            {
                player.AssignCommand(() =>
                {
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, successEffect, target, 6.1f);
                });
            }
            else
            {
                player.AssignCommand(() =>
                {
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, failureEffect, target, 6.1f);
                });
            }

            SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
            EnmityService.AdjustEnmity(target.Object, player, 1);
        }

        private void ApplyEffect(NWPlayer player, NWObject target, int spellTier)
        {
            const float radiusSize = 10.0f;
            NWCreature targetCreature;
            
            switch (spellTier)
            {
                // Tier 1 - Single target is Tranquilized or, if resisted, receives -5 to AB and AC
                case 1:
                    RunEffect(player, target);
                    break;
                // Tier 2 - Target and nearest other enemy within 10m are tranquilized using tier 1 rules.
                case 2:
                    RunEffect(player, target);
                    
                    // Target the next nearest creature and do the same thing.
                    targetCreature = GetFirstObjectInShape(SHAPE_SPHERE, radiusSize, player.Location, TRUE);
                    while (targetCreature.IsValid)
                    {
                        if (targetCreature != target)
                        {
                            // Apply to nearest other creature, then exit loop.
                            RunEffect(player, target);
                            break;
                        }

                        targetCreature = GetNextObjectInShape(SHAPE_SPHERE, radiusSize, player.Location, TRUE);
                    }
                    break;
                // Tier 3 - All creatures within 10m are tranquilized using tier 1 rules.
                case 3:
                    RunEffect(player, target);
                    
                    targetCreature = GetFirstObjectInShape(SHAPE_SPHERE, radiusSize, player.Location, TRUE);
                    while (targetCreature.IsValid)
                    {
                        RunEffect(player, target);
                        targetCreature = GetNextObjectInShape(SHAPE_SPHERE, radiusSize, player.Location, TRUE);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
        }
    }
}

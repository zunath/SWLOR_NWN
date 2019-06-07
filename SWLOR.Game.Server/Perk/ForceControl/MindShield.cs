using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

using static NWN._;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class MindShield: IPerkHandler
    {
        public PerkType PerkType => PerkType.MindShield;
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

        public void OnConcentrationTick(NWPlayer player, NWObject target, int spellTier, int tick)
        {
            ApplyEffect(player, target, spellTier);
        }

        private void ApplyEffect(NWPlayer player, NWObject target, int spellTier)
        {

            Effect effectMindShield = new Effect();               

            // Handle effects for differing spellTier values
            switch (spellTier)
            {
                case 1:
                    effectMindShield =_.EffectImmunity(IMMUNITY_TYPE_DAZED);

                    player.AssignCommand(() =>
                    {
                        _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectMindShield, target, 6.1f);
                    });
                    break;
                case 2:
                    effectMindShield = _.EffectImmunity(IMMUNITY_TYPE_DAZED);
                    effectMindShield = _.EffectLinkEffects(effectMindShield, _.EffectImmunity(IMMUNITY_TYPE_CONFUSED));
                    effectMindShield = _.EffectLinkEffects(effectMindShield, _.EffectImmunity(IMMUNITY_TYPE_DOMINATE)); // Force Pursuade is DOMINATION effect

                    player.AssignCommand(() =>
                    {
                        _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectMindShield, target, 6.1f);
                    });
                    break;
                case 3:
                    effectMindShield = _.EffectImmunity(IMMUNITY_TYPE_DAZED);
                    effectMindShield = _.EffectLinkEffects(effectMindShield, _.EffectImmunity(IMMUNITY_TYPE_CONFUSED));
                    effectMindShield = _.EffectLinkEffects(effectMindShield, _.EffectImmunity(IMMUNITY_TYPE_DOMINATE)); // Force Pursuade is DOMINATION effect

                    if (target.GetLocalInt("FORCE_DRAIN_IMMUNITY") == 1)
                
                    player.SetLocalInt("FORCE_DRAIN_IMMUNITY",0);                   
                    player.DelayAssignCommand(() =>
                    {
                        player.DeleteLocalInt("FORCE_DRAIN_IMMUNITY");
                    },6.1f);

                    player.AssignCommand(() =>
                    {
                        _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effectMindShield, target, 6.1f);
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }
        }
    }
}

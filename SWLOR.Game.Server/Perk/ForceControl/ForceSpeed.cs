using System;
using NWN;
using NWN.Scripts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Effect;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;


// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class force_speed_exp
#pragma warning restore IDE1006 // Naming Styles
    {
        // This one's a little weird.
        // The Epic Dodge feat is granted to players while this effect is active.
        // We apply it at the time the effect is applied, but we need to register and subscribe to 
        // the event which fires at the time it expires, so that we can remove the feat.

        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            var data = NWNXEffect.GetEffectExpiredData();
            var creator = NWNXEffect.GetEffectExpiredCreator();
            OnEffectExpired<force_speed_exp> @event = new OnEffectExpired<force_speed_exp>(data, creator);
            MessageHub.Instance.Publish(@event);
        }
    }
}


namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class ForceSpeed: IPerkHandler
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnEffectExpired<force_speed_exp>>(message => OnEffectExpired(message.Data, message.Creator, message.AppliedTo));
        }

        public PerkType PerkType => PerkType.ForceSpeed;
        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return true;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
        }

        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
            int perkLevel = PerkService.GetPCPerkLevel(oPC, PerkType.ForceSpeed);

            switch (perkLevel)
            {
                case 1: return 2;
                case 2: return 4;
                case 3: return 6;
                case 4: return 8;
                case 5: return 20;
            }

            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            Effect effect;
            float duration;
            switch (perkLevel)
            {
                case 1:
                    effect = _.EffectMovementSpeedIncrease(10);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 2));
                    duration = 60f;
                    break;
                case 2:
                    effect = _.EffectMovementSpeedIncrease(20);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 4));
                    duration = 90f;
                    break;
                case 3:
                    effect = _.EffectMovementSpeedIncrease(30);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 6));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 120f;
                    break;
                case 4:
                    effect = _.EffectMovementSpeedIncrease(40);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 8));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 150f;
                    break;
                case 5:
                    effect = _.EffectMovementSpeedIncrease(50);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 6));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 180f;
                    break;
                default:
                    throw new ArgumentException(nameof(perkLevel) + " invalid. Value " + perkLevel + " is unhandled.");
            }

            effect = NWNXEffect.SetEffectExpiredScript(effect, "force_speed_exp");

            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, target, duration);
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(_.VFX_IMP_AC_BONUS), target);
            NWNXCreature.AddFeatByLevel(target.Object, _.FEAT_EPIC_DODGE, 1);
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

        private static void OnEffectExpired(string[] data, NWObject creator, NWObject appliedTo)
        {
            NWNXCreature.RemoveFeat(appliedTo.Object, _.FEAT_EPIC_DODGE);
        }
    }
}

﻿using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class Tranquilizer: IPerkHandler
    {
        public PerkType PerkType => PerkType.Tranquilizer;
        public string Name => "Tranquilizer";
        public bool IsActive => true;
        public string Description => "Your next attack will tranquilize your target, putting them to sleep for a short period of time. Damage will break the effect prematurely. Must be equipped with a Blaster Rifle to use.";
        public PerkCategoryType Category => PerkCategoryType.BlastersBlasterRifles;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Tranquilizer;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;

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

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0f;
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
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(target.Object);
            if (concentrationEffect.Type == PerkType.MindShield ||
                GetIsImmune(target, ImmunityType.MindSpells) == true)
            {
                creature.SendMessage("Your target is immune to tranquilization effects.");
                return;
            }

            int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky);
            float duration;

            switch (perkLevel)
            {
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

            if (RemoveExistingEffect(target, duration))
            {
                creature.SendMessage("A more powerful effect already exists on your target.");
                return;
            }

            target.SetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN", 1);

            Effect effect = _.EffectDazed();
            effect = _.EffectLinkEffects(effect, _.EffectVisualEffect(Vfx.Vfx_Dur_Iounstone_Blue));
            effect = _.TagEffect(effect, "TRANQUILIZER_EFFECT");

            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
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

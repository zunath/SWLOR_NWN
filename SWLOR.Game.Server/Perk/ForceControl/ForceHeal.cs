using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class ForceHeal: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceHeal;
        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
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
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            creature.AssignCommand(() => NWScript.ActionPlayAnimation(Animation.LoopingMeditate, 1.0f, 9999));
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
            int amount = 0;


            switch (perkLevel)
            {
                case 1: amount = 2; break;
                case 2: amount = 3; break;
                case 3: amount = 5; break;
                case 4: amount = 7; break;
                case 5: amount = 10; break;
            }

            // If target is at max HP, we do nothing else.
            int difference = target.MaxHP - target.CurrentHP;
            if (difference <= 0) return;

            // If we would heal the target for more than their max, reduce the amount healed to that number.
            if (amount > difference)
                amount = difference;

            // Apply the heal
            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(amount), target);
            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Holy), target);

            // Give Control XP, if player.
            if (creature.IsPlayer)
            {
                SkillService.GiveSkillXP(creature.Object, SkillType.ForceControl, amount * 10);
            }
            
            EnmityService.AdjustEnmityOnAllTaggedCreatures(creature, amount * 3, 2);
        }
    }
}

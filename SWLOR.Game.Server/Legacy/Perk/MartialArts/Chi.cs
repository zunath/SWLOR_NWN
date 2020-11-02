using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.Perk.MartialArts
{
    public class Chi : IPerkHandler
    {
        public PerkType PerkType => PerkType.Chi;

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
            var wisdom = creature.WisdomModifier;
            var constitution = creature.ConstitutionModifier;
            var min = 1 + wisdom / 2 + constitution / 3;

            // Rank 7 and up: AOE heal party members
            if (perkLevel >= 7)
            {
                var members = creature.PartyMembers.Where(x => Equals(x, creature) || 
                                                               NWScript.GetDistanceBetween(creature, x) <= 10.0f && x.CurrentHP < x.MaxHP);
                foreach (var member in members)
                {
                    DoHeal(member, perkLevel, min);
                }
            }
            else
            {
                DoHeal(target, perkLevel, min);
            }
        }

        private void DoHeal(NWObject target, int perkLevel, int minimum)
        {
            var percentage = perkLevel * 0.10f;
            var heal = (int)(target.MaxHP * percentage);

            heal = SWLOR.Game.Server.Service.Random.Next(minimum, heal);

            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(heal), target);
            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_G), target);
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

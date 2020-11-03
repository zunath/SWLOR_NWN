using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.Perk.TwinBlade
{
    public class CrossCut: IPerkHandler
    {
        public PerkType PerkType => PerkType.CrossCut;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.TwinBlade)
                return "Must be equipped with a twin blade to use that ability.";

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
            var damage = 0;
            var duration = 0.0f;

            switch (perkLevel)
            {
                case 1:
                    damage = SWLOR.Game.Server.Service.Random.D4(1);
                    duration = 6;   
                    break;
                case 2:
                    damage = SWLOR.Game.Server.Service.Random.D4(2);
                    duration = 6;
                    break;
                case 3:
                    damage = SWLOR.Game.Server.Service.Random.D4(2);
                    duration = 9;
                    break;
                case 4:
                    damage = SWLOR.Game.Server.Service.Random.D8(2);
                    duration = 9;
                    break;
                case 5:
                    damage = SWLOR.Game.Server.Service.Random.D8(2);
                    duration = 12;
                    break;
                case 6:
                    damage = SWLOR.Game.Server.Service.Random.D6(3);
                    duration = 15;
                    break;
                case 7:
                    damage = SWLOR.Game.Server.Service.Random.D8(3);
                    duration = 15;
                    break;
                case 8:
                    damage = SWLOR.Game.Server.Service.Random.D8(3);
                    duration = 18;
                    break;
                case 9:
                    damage = SWLOR.Game.Server.Service.Random.D8(4);
                    duration = 18;
                    break;
                case 10:
                    damage = SWLOR.Game.Server.Service.Random.D8(4);
                    duration = 21;
                    break;
            }

            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectDamage(damage, DamageType.Slashing), target);
            NWScript.ApplyEffectToObject(DurationType.Temporary, NWScript.EffectACDecrease(3), target, duration);
            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Evil), target);

            creature.SendMessage("Your target's armor has been breached.");
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

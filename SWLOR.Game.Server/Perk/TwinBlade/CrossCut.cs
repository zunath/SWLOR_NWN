using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.TwinBlade
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
            int damage = 0;
            float duration = 0.0f;

            switch (perkLevel)
            {
                case 1:
                    damage = RandomService.D4(1);
                    duration = 6;   
                    break;
                case 2:
                    damage = RandomService.D4(2);
                    duration = 6;
                    break;
                case 3:
                    damage = RandomService.D4(2);
                    duration = 9;
                    break;
                case 4:
                    damage = RandomService.D8(2);
                    duration = 9;
                    break;
                case 5:
                    damage = RandomService.D8(2);
                    duration = 12;
                    break;
                case 6:
                    damage = RandomService.D6(3);
                    duration = 15;
                    break;
                case 7:
                    damage = RandomService.D8(3);
                    duration = 15;
                    break;
                case 8:
                    damage = RandomService.D8(3);
                    duration = 18;
                    break;
                case 9:
                    damage = RandomService.D8(4);
                    duration = 18;
                    break;
                case 10:
                    damage = RandomService.D8(4);
                    duration = 21;
                    break;
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage, DamageType.Slashing), target);
            _.ApplyEffectToObject(DurationType.Temporary, _.EffectACDecrease(3), target, duration);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Evil), target);

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

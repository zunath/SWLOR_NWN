using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class LegShot: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;

        public LegShot(
            INWScript script,
            IPerkService perk,
            IRandomService random)
        {
            _perk = perk;
            _ = script;
            _random = random;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.BlasterPistol;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a blaster pistol to use that ability.";
        }

        public int FPCost(NWPlayer oPC, int baseFPCost)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWPlayer player, NWObject target, int level)
        {
            int damage;
            float duration;

            switch (level)
            {
                case 1:
                    damage = _random.D4(1);
                    duration = 6;
                    break;
                case 2:
                    damage = _random.D8(1);
                    duration = 6;
                    break;
                case 3:
                    damage = _random.D8(2);
                    duration = 6;
                    break;
                case 4:
                    damage = _random.D8(2);
                    duration = 12;
                    break;
                case 5:
                    damage = _random.D8(3);
                    duration = 12;
                    break;
                case 6:
                    damage = _random.D8(4);
                    duration = 12;
                    break;
                case 7:
                    damage = _random.D8(5);
                    duration = 12;
                    break;
                case 8:
                    damage = _random.D8(5);
                    duration = 18;
                    break;
                case 9:
                    damage = _random.D8(6);
                    duration = 24;
                    break;
                default: return;
            }


            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_PIERCING), target);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectCutsceneImmobilize(), target, duration);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_IMP_ACID_L), target, duration);
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
    }
}

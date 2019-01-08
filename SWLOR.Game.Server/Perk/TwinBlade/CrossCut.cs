using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.TwinBlade
{
    public class CrossCut: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;

        public CrossCut(INWScript script,
            IPerkService perk,
            IRandomService random)
        {
            _ = script;
            _perk = perk;
            _random = random;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.TwinBlade;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a twin blade to use that ability.";
        }

        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
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
            int damage = 0;
            float duration = 0.0f;

            switch (perkLevel)
            {
                case 1:
                    damage = _random.D4(1);
                    duration = 6;   
                    break;
                case 2:
                    damage = _random.D4(2);
                    duration = 6;
                    break;
                case 3:
                    damage = _random.D4(2);
                    duration = 9;
                    break;
                case 4:
                    damage = _random.D8(2);
                    duration = 9;
                    break;
                case 5:
                    damage = _random.D8(2);
                    duration = 12;
                    break;
                case 6:
                    damage = _random.D6(3);
                    duration = 15;
                    break;
                case 7:
                    damage = _random.D8(3);
                    duration = 15;
                    break;
                case 8:
                    damage = _random.D8(3);
                    duration = 18;
                    break;
                case 9:
                    damage = _random.D8(4);
                    duration = 18;
                    break;
                case 10:
                    damage = _random.D8(4);
                    duration = 21;
                    break;
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_SLASHING), target);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectACDecrease(3), target, duration);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_IMP_HEAD_EVIL), target);

            player.SendMessage("Your target's armor has been breached.");
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

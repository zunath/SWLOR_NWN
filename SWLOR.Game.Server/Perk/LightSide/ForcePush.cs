using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.LightSide
{
    public class ForcePush: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;

        public ForcePush(INWScript script,
            IPerkService perk,
            IRandomService random)
        {
            _ = script;
            _perk = perk;
            _random = random;
        }
        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return true;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
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

        public void OnImpact(NWPlayer oPC, NWObject oTarget)
        {
            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus;
            int level = _perk.GetPCPerkLevel(oPC, PerkType.ForcePush);
            int lightBonus = oPC.EffectiveLightAbilityBonus;
            int min = 1;
            float length;
            int damage;
            BackgroundType background = (BackgroundType)oPC.Class1;

            if (background == BackgroundType.Sage ||
                background == BackgroundType.Consular)
            {
                level++;
            }

            int wisdom = oPC.WisdomModifier;
            int intelligence = oPC.IntelligenceModifier;
            min += lightBonus / 4 + wisdom / 3 + intelligence / 4;

            switch (level)
            {
                case 1:
                    damage = _random.D4(1, min);
                    length = 3;
                    break;
                case 2:
                    damage = _random.D4(1, min);
                    length = 6;
                    break;
                case 3:
                    damage = _random.D6(1, min);
                    length = 6;
                    break;
                case 4:
                    damage = _random.D8(1, min);
                    length = 6;
                    break;
                case 5:
                    damage = _random.D8(1, min);
                    length = 9;
                    break;
                case 6: // Only available with background perk
                    damage = _random.D12(1, min);
                    length = 9;
                    break;

                default: return;
            }
            
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
                oPC.SendMessage("Lucky force push!");
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_POSITIVE), oTarget);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), oTarget, length);
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
            return true;
        }
    }
}

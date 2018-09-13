using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.DarkSide
{
    public class DarkHeal : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;

        public DarkHeal(INWScript script,
            IPerkService perk,
            IRandomService random,
            ISkillService skill)
        {
            _ = script;
            _perk = perk;
            _random = random;
            _skill = skill;
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
            int level = _perk.GetPCPerkLevel(oPC, PerkType.DarkHeal);
            int darkBonus = oPC.EffectiveDarkAbilityBonus;
            int amount;
            float length;
            int regenAmount;
            int min = 1;
            BackgroundType background = (BackgroundType) oPC.Class1;

            if (background == BackgroundType.Sorcerer ||
                background == BackgroundType.Corrupter)
            {
                level++;
            }

            int wisdom = oPC.WisdomModifier;
            int intelligence = oPC.IntelligenceModifier;
            min += darkBonus / 3 + intelligence / 2 + wisdom / 3;

            switch (level)
            {
                case 1:
                    amount = _random.D6(2, min);
                    length = 0.0f;
                    regenAmount = 0;
                    break;
                case 2:
                    amount = _random.D6(2, min);
                    length = 6.0f;
                    regenAmount = 1;
                    break;
                case 3:
                    amount = _random.D12(2, min);
                    length = 6.0f;
                    regenAmount = 1;
                    break;
                case 4:
                    amount = _random.D12(2, min);
                    length = 12.0f;
                    regenAmount = 1;
                    break;
                case 5:
                    amount = _random.D12(2, min);
                    length = 6.0f;
                    regenAmount = 2;
                    break;
                case 6:
                    amount = _random.D12(2, min);
                    length = 12.0f;
                    regenAmount = 2;
                    break;
                case 7:
                    amount = _random.D12(3, min);
                    length = 12.0f;
                    regenAmount = 2;
                    break;
                case 8:
                    amount = _random.D12(3, min);
                    length = 6.0f;
                    regenAmount = 4;
                    break;
                case 9:
                    amount = _random.D12(4, min);
                    length = 6.0f;
                    regenAmount = 4;
                    break;
                case 10:
                    amount = _random.D12(4, min);
                    length = 12.0f;
                    regenAmount = 4;
                    break;
                case 11: // Only attainable with background bonus
                    amount = _random.D12(5, min);
                    length = 12.0f;
                    regenAmount = 4;
                    break;
                default: return;
            }
            
            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus;
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
                oPC.SendMessage("Lucky heal!");
            }
            
            Effect heal = _.EffectHeal(amount);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, heal, oTarget.Object);

            if (length > 0.0f && regenAmount > 0)
            {
                Effect regen = _.EffectRegenerate(regenAmount, 1.0f);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, regen, oTarget.Object, length + 0.1f);
            }

            _skill.RegisterPCToAllCombatTargetsForSkill(oPC, SkillType.DarkSideAbilities);

            Effect vfx = _.EffectVisualEffect(NWScript.VFX_IMP_HEALING_M);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, vfx, oTarget.Object);
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

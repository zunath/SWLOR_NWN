using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.Alteration
{
    public class Recover: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;

        public Recover(INWScript script,
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
            int level = _perk.GetPCPerkLevel(oPC, PerkType.Recover);
            int alterationBonus = oPC.EffectiveAlterationBonus;
            int amountMin;
            int amountMax;
            float length;
            int regenAmount;

            switch (level)
            {
                case 1:
                    amountMin = 3;
                    amountMax = 5;
                    length = 18.0f;
                    regenAmount = 1;
                    break;
                case 2:
                    amountMin = 3;
                    amountMax = 5;
                    length = 30.0f;
                    regenAmount = 1;
                    break;
                case 3:
                    amountMin = 7;
                    amountMax = 10;
                    length = 30.0f;
                    regenAmount = 1;
                    break;
                case 4:
                    amountMin = 7;
                    amountMax = 10;
                    length = 30.0f;
                    regenAmount = 2;
                    break;
                case 5:
                    amountMin = 10;
                    amountMax = 13;
                    length = 30.0f;
                    regenAmount = 2;
                    break;
                case 6:
                    amountMin = 10;
                    amountMax = 13;
                    length = 30.0f;
                    regenAmount = 3;
                    break;
                default: return;
            }

            amountMin += alterationBonus * 2;
            amountMax += alterationBonus * 3;
            length += alterationBonus;
            regenAmount += alterationBonus / 3;

            int healAmount = _random.Random(amountMin, amountMax) + 1;

            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus;
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
            }

            int wisdom = oPC.WisdomModifier;
            int intelligence = oPC.IntelligenceModifier;
            length = length + (wisdom * 4) + (intelligence * 2);

            Effect heal =  _.EffectHeal(healAmount);
            Effect regen = _.EffectRegenerate(regenAmount, 6.0f);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, regen, oTarget.Object, length + 0.1f);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, heal, oTarget.Object);

            _skill.RegisterPCToAllCombatTargetsForSkill(oPC, SkillType.AlterationMagic);

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

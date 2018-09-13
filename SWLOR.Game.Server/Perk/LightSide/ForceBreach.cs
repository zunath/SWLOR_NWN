using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.LightSide
{
    public class ForceBreach : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;
        private readonly ICustomEffectService _customEffect;

        public ForceBreach(INWScript script,
            IPerkService perk,
            IRandomService random,
            ISkillService skill,
            ICustomEffectService customEffect)
        {
            _ = script;
            _perk = perk;
            _random = random;
            _skill = skill;
            _customEffect = customEffect;
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
            int level = _perk.GetPCPerkLevel(oPC, PerkType.ForceBreach);
            int lightBonus = oPC.EffectiveLightAbilityBonus;
            int amount;
            int length;
            int dotAmount;
            int min = 1;
            BackgroundType background = (BackgroundType)oPC.Class1;

            if (background == BackgroundType.Sage ||
                background == BackgroundType.Consular)
            {
                level++;
            }

            int wisdom = oPC.WisdomModifier;
            int intelligence = oPC.IntelligenceModifier;
            min += lightBonus / 3 + wisdom / 2 + intelligence / 3;

            switch (level)
            {
                case 1:
                    amount = _random.D6(2, min);
                    length = 0;
                    dotAmount = 0;
                    break;
                case 2:
                    amount = _random.D6(2, min);
                    length = 6;
                    dotAmount = 1;
                    break;
                case 3:
                    amount = _random.D12(2, min);
                    length = 6;
                    dotAmount = 1;
                    break;
                case 4:
                    amount = _random.D12(2, min);
                    length = 12;
                    dotAmount = 1;
                    break;
                case 5:
                    amount = _random.D12(2, min);
                    length = 6;
                    dotAmount = 2;
                    break;
                case 6:
                    amount = _random.D12(2, min);
                    length = 12;
                    dotAmount = 2;
                    break;
                case 7:
                    amount = _random.D12(3, min);
                    length = 12;
                    dotAmount = 2;
                    break;
                case 8:
                    amount = _random.D12(3, min);
                    length = 6;
                    dotAmount = 4;
                    break;
                case 9:
                    amount = _random.D12(4, min);
                    length = 6;
                    dotAmount = 4;
                    break;
                case 10:
                    amount = _random.D12(4, min);
                    length = 12;
                    dotAmount = 4;
                    break;
                case 11: // Only attainable with background bonus
                    amount = _random.D12(5, min);
                    length = 12;
                    dotAmount = 4;
                    break;
                default: return;
            }

            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus;
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
                oPC.SendMessage("Lucky force breach!");
            }

            Effect damage = _.EffectDamage(amount);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, damage, oTarget.Object);

            if (length > 0.0f && dotAmount > 0)
            {
                _customEffect.ApplyCustomEffect(oPC, (NWCreature)oTarget, CustomEffectType.ForceBreach, length, level);
            }

            _skill.RegisterPCToAllCombatTargetsForSkill(oPC, SkillType.LightSideAbilities);

            Effect vfx = _.EffectVisualEffect(VFX_IMP_DOMINATE_S);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, oTarget.Object);
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

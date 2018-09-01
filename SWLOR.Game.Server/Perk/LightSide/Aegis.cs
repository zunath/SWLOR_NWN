using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.LightSide
{
    public class Aegis: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly ICustomEffectService _customEffect;
        private readonly ISkillService _skill;
        private readonly IRandomService _random;

        public Aegis(
            INWScript script, 
            IPerkService perk, 
            ICustomEffectService customEffect,
            ISkillService skill,
            IRandomService random)
        {
            _ = script;
            _perk = perk;
            _customEffect = customEffect;
            _skill = skill;
            _random = random;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            int level = _perk.GetPCPerkLevel(oPC, PerkType.Aegis);
            int activeAegisLevel = _customEffect.GetActiveEffectLevel(oTarget, CustomEffectType.Aegis);

            return level >= activeAegisLevel;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "A more powerful effect already exists on your target.";
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
            int level = _perk.GetPCPerkLevel(oPC, PerkType.Aegis);
            int ticks;

            switch (level)
            {
                case 1:
                case 2:
                    ticks = 10;
                    break;
                case 3:
                case 4:
                case 5:
                    ticks = 50;
                    break;
                default: return;
            }
            
            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus;
            if(_random.Random(100) + 1 <= luck)
            {
                ticks = ticks * 2;
            }

            int wisdom = oPC.WisdomModifier;
            int intelligence = oPC.IntelligenceModifier;
            int lightAbilityBonus = oPC.EffectiveLightAbilityBonus / 2;
            ticks = ticks + intelligence + (wisdom * 2) + lightAbilityBonus;

            _customEffect.ApplyCustomEffect(oPC, (NWPlayer)oTarget, CustomEffectType.Aegis, ticks, level);
            _skill.ApplyStatChanges((NWPlayer)oTarget, null);
            _skill.RegisterPCToAllCombatTargetsForSkill(oPC, SkillType.LightSideAbilities);

            Effect vfx = _.EffectVisualEffect(NWScript.VFX_IMP_AC_BONUS);
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

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.Evocation
{
    public class GraspingIce : IPerk
    {
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;

        public GraspingIce(INWScript script,
            IRandomService random,
            IPerkService perk,
            ISkillService skill)
        {
            _ = script;
            _random = random;
            _perk = perk;
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
            int level = _perk.GetPCPerkLevel(oPC, PerkType.GraspingIce);
            int damage;
            float slowLength = 0.0f;
            int evocationBonus = oPC.EffectiveEvocationBonus;

            switch (level)
            {
                case 1:
                    damage = _random.Random(6 + evocationBonus) + 1;
                    break;
                case 2:
                    damage = _random.Random(6 + evocationBonus) + 1;
                    slowLength = 3.0f;
                    break;
                case 3:
                    damage = _random.Random(6 + evocationBonus) + 1;
                    damage += _random.Random(6 + evocationBonus) + 1;
                    slowLength = 3.0f;
                    break;
                case 4:
                    damage = _random.Random(4 + evocationBonus) + 1;
                    damage += _random.Random(4 + evocationBonus) + 1;
                    damage += _random.Random(4 + evocationBonus) + 1;
                    damage += _random.Random(4 + evocationBonus) + 1;
                    slowLength = 3.0f;
                    break;
                case 5:
                    damage = _random.Random(8 + evocationBonus) + 1;
                    damage += _random.Random(8 + evocationBonus) + 1;
                    damage += _random.Random(8 + evocationBonus) + 1;
                    slowLength = 3.0f;
                    break;
                default:
                    return;
            }

            int wisdom = oPC.WisdomModifier;
            int intelligence = oPC.IntelligenceModifier;

            float damageMultiplier = 1.0f + (intelligence * 0.2f) + (wisdom * 0.1f);
            damage = (int)(damage * damageMultiplier);

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectVisualEffect(NWScript.VFX_FNF_HOWL_MIND), oTarget.Object);

            if (slowLength > 0.0f)
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectSlow(), oTarget.Object, slowLength + 0.1f);
            }

            _skill.RegisterPCToNPCForSkill(oPC, (NWCreature)oTarget, SkillType.EvocationMagic);

            oPC.AssignCommand(() =>
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectDamage(damage), oTarget.Object);
            });
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

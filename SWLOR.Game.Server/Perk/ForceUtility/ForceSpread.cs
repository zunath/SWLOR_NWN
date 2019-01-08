using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.ForceUtility
{
    public class ForceSpread: IPerk
    {
        private readonly INWScript _;
        private readonly ICustomEffectService _customEffect;
        private readonly ISkillService _skill;

        public ForceSpread(
            INWScript script,
            ICustomEffectService customEffect,
            ISkillService skill)
        {
            _ = script;
            _customEffect = customEffect;
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
            NWCreature targetCreature = target.Object;

            int duration;
            int uses;
            float range;
            
            switch (perkLevel)
            {
                case 1:
                    duration = 30;
                    uses = 1;
                    range = 10f;
                    break;
                case 2:
                    duration = 30;
                    uses = 2;
                    range = 10f;
                    break;
                case 3:
                    duration = 60;
                    uses = 2;
                    range = 20f;
                    break;
                case 4:
                    duration = 60;
                    uses = 3;
                    range = 20f;
                    break;
                case 5:
                    duration = 60;
                    uses = 4;
                    range = 20f;
                    break;
                case 6:
                    duration = 60;
                    uses = 5;
                    range = 20f;
                    break;
                default: return;
            }
            _customEffect.ApplyCustomEffect(player, targetCreature, CustomEffectType.ForceSpread, duration, perkLevel, uses + "," + range);
            _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceUtility, null);
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

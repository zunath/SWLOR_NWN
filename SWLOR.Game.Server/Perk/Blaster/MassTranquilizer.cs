using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class MassTranquilizer : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;

        public MassTranquilizer(
            INWScript script,
            IPerkService perk,
            IRandomService random)
        {
            _ = script;
            _perk = perk;
            _random = random;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.BlasterRifle;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a blaster rifle to use that ability.";
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
            int massLevel = _perk.GetPCPerkLevel(player, PerkType.MassTranquilizer);
            int tranqLevel = _perk.GetPCPerkLevel(player, PerkType.Tranquilizer);
            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky);
            float duration;
            float range = 5 * massLevel;
            
            switch (tranqLevel)
            {
                case 0:
                    duration = 6;
                    break;
                case 1:
                    duration = 12;
                    break;
                case 2:
                    duration = 24;
                    break;
                case 3:
                    duration = 36;
                    break;
                case 4:
                    duration = 48;
                    break;
                case 5:
                    duration = 60;
                    break;
                case 6:
                    duration = 72;
                    break;
                case 7:
                    duration = 84;
                    break;
                case 8:
                    duration = 96;
                    break;
                case 9:
                    duration = 108;
                    break;
                case 10:
                    duration = 120;
                    break;
                default: return;
            }

            if (_random.D100(1) <= luck)
            {
                duration *= 2;
                player.SendMessage("Lucky shot!");
            }

            int current = 1;
            NWCreature nearest = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, target, current);

            while (nearest.IsValid)
            {
                if (_.GetDistanceBetween(nearest, target) > range) break;
                if (RemoveExistingEffect(target, duration)) continue;
                
                nearest.SetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN", 1);
                Effect effect = _.EffectDazed();
                effect = _.EffectLinkEffects(effect, _.EffectVisualEffect(VFX_DUR_IOUNSTONE_BLUE));
                effect = _.TagEffect(effect, "TRANQUILIZER_EFFECT");
                
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, effect, target, duration);

                current++;
                nearest = _.GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, target, current);
            }

        }

        private bool RemoveExistingEffect(NWObject target, float duration)
        {
            Effect effect = target.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "TRANQUILIZER_EFFECT");
            if (effect == null) return false;

            if (_.GetEffectDurationRemaining(effect) >= duration) return true;
            _.RemoveEffect(target, effect);
            return false;
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

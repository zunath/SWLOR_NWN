using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class ForceBody: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceBody;
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellFeatID)
        {
            if (oPC.CurrentHP <= 1)
                return "You do not have enough HP to use this ability.";

            if (oPC.IsPlayer)
            {
                var dbPlayer = DataService.Get<Player>(oPC.GlobalID);
                if (dbPlayer.CurrentFP >= dbPlayer.MaxFP)
                    return "Your FP is already at maximum.";
            }

            return string.Empty;
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
            switch ((CustomFeatType)spellFeatID)
            {
                case CustomFeatType.ForceBody1: return 900f; // 15 minutes
                case CustomFeatType.ForceBody2: return 600f; // 10 minutes
                case CustomFeatType.ForceBody3: return 420f; // 7 minutes
                case CustomFeatType.ForceBody4: return 300f; // 5 minutes
            }

            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            float percent = 0.0f;

            switch ((CustomFeatType)spellFeatID)
            {
                case CustomFeatType.ForceBody1:
                    percent = 0.10f;
                    break;
                case CustomFeatType.ForceBody2:
                    percent = 0.20f;
                    break;
                case CustomFeatType.ForceBody3:
                    percent = 0.35f;
                    break;
                case CustomFeatType.ForceBody4:
                    percent = 0.50f;
                    break;
            }

            int recovery = (int)(target.CurrentHP * percent);
            if (recovery < 1) recovery = 1;

            // Damage user.
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectDamage(recovery), player);
            
            // Check lucky chance.
            int luck = PerkService.GetPCPerkLevel(player, PerkType.Lucky);
            if (RandomService.D100(1) <= luck)
            {
                recovery *= 2;
                player.SendMessage("Lucky Force Body!");
            }
            
            // Recover FP on target.
            AbilityService.RestoreFP(target.Object, recovery);

            // Grant XP
            SkillService.GiveSkillXP(player, SkillType.ForceControl, recovery);
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

        public void OnConcentrationTick(NWPlayer player, int perkLevel, int spellFeatID)
        {
            
        }
    }
}

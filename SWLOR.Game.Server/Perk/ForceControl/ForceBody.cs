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
        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.CurrentHP <= 1)
                return "You do not have enough HP to use this ability.";

            if (oPC.IsPlayer)
            {
                var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
                if (dbPlayer.CurrentFP >= dbPlayer.MaxFP)
                    return "Your FP is already at maximum.";
            }

            return string.Empty;
        }

        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            switch (spellTier)
            {
                case 1: return 900f; // 15 minutes
                case 2: return 600f; // 10 minutes
                case 3: return 420f; // 7 minutes
                case 4: return 300f; // 5 minutes
            }

            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            float percent = 0.0f;

            switch (spellTier)
            {
                case 1:
                    percent = 0.10f;
                    break;
                case 2:
                    percent = 0.20f;
                    break;
                case 3:
                    percent = 0.35f;
                    break;
                case 4:
                    percent = 0.50f;
                    break;
            }

            int recovery = (int)(target.CurrentHP * percent);
            if (recovery < 1) recovery = 1;

            // Damage user.
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectDamage(recovery), creature);
            
            // Check lucky chance.
            int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky);
            if (RandomService.D100(1) <= luck)
            {
                recovery *= 2;
                creature.SendMessage("Lucky Force Body!");
            }
            
            // Recover FP on target.
            AbilityService.RestorePlayerFP(target.Object, recovery);

            // Play VFX
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(_.VFX_IMP_HEAD_ODD), target);

            // Grant XP, if player.
            if (creature.IsPlayer)
            {
                SkillService.GiveSkillXP(creature.Object, SkillType.ForceControl, recovery * 2);
            }
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}

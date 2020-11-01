using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.Perk.OneHanded
{
    public class BluntPowerAttack : IPerkHandler
    {
        public PerkType PerkType => PerkType.BluntPowerAttack;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
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
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
            ApplyFeatChanges(creature, null);
        }

        public void OnRemoved(NWCreature creature)
        {
            Creature.RemoveFeat(creature, Feat.PowerAttack);
            Creature.RemoveFeat(creature, Feat.ImprovedPowerAttack);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Baton) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Baton) return;
            if (oItem == creature.LeftHand) return;

            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            var equipped = oItem ?? creature.RightHand;

            if (Equals(equipped, oItem) || equipped.CustomItemType != CustomItemType.Baton)
            {
                Creature.RemoveFeat(creature, Feat.PowerAttack );
                Creature.RemoveFeat(creature, Feat.ImprovedPowerAttack);
                if (NWScript.GetActionMode(creature, ActionMode.PowerAttack) == true)
                {
                    NWScript.SetActionMode(creature, ActionMode.PowerAttack, false);
                }
                if (NWScript.GetActionMode(creature, ActionMode.ImprovedPowerAttack) == true)
                {
                    NWScript.SetActionMode(creature, ActionMode.ImprovedPowerAttack, false);
                }
                return;
            }

            var perkLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.BluntPowerAttack);
            Creature.AddFeat(creature, Feat.PowerAttack);

            if (perkLevel >= 2)
            {
                Creature.AddFeat(creature, Feat.ImprovedPowerAttack);
            }
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

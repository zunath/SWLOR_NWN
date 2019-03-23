using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class Provoke: IPerk
    {
        
        
        
        

        public Provoke(
            
            )
        {
            
            
            
            
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oTarget.IsNPC && 
                oPC.Chest.CustomItemType == CustomItemType.HeavyArmor &&
                _.GetDistanceBetween(oPC.Object, oTarget.Object) <= 9.0f;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            if (!oTarget.IsNPC) return "Only NPCs may be targeted with Provoke.";
            float distance = _.GetDistanceBetween(oPC.Object, oTarget.Object);
            if (distance > 9.0f) return "Target is too far away.";

            if (oPC.Chest.CustomItemType != CustomItemType.HeavyArmor)
                return "You must be equipped with heavy armor to use that combat ability.";
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
            int perkRank = PerkService.GetPCPerkLevel(oPC, PerkType.Provoke);

            if (perkRank == 2) baseCooldownTime -= 5.0f;
            else if (perkRank == 3) baseCooldownTime -= 10.0f;
            else if (perkRank == 4) baseCooldownTime -= 15.0f;

            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            NWCreature npc = (target.Object);
            Effect vfx = _.EffectVisualEffect(_.VFX_IMP_CHARM);
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, vfx, target.Object);
            
            player.AssignCommand(() =>
            {
                _.ActionPlayAnimation(_.ANIMATION_FIREFORGET_TAUNT, 1f, 1f);
            });

            EnmityService.AdjustEnmity(npc, player, 120);
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
            if (newLevel == 1)
            {
                ApplyFeatChanges(oPC, null);
            }
        }

        public void OnRemoved(NWPlayer oPC)
        {
            NWNXCreature.RemoveFeat(oPC, (int)CustomFeatType.Provoke);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.HeavyArmor) return;
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.HeavyArmor) return;
            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem equipped = oItem ?? oPC.Chest;
            
            if (equipped.Equals(oItem) || equipped.CustomItemType != CustomItemType.HeavyArmor)
            {
                NWNXCreature.RemoveFeat(oPC, (int)CustomFeatType.Provoke);
                return;
            }

            NWNXCreature.AddFeat(oPC, (int)CustomFeatType.Provoke);
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}

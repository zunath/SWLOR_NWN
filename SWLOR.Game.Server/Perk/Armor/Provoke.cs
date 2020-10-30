using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class Provoke: IPerkHandler
    {
        public PerkType PerkType => PerkType.Provoke;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (!oTarget.IsNPC) return "Only NPCs may be targeted with Provoke.";

            var distance = NWScript.GetDistanceBetween(oPC.Object, oTarget.Object);
            if (distance > 9.0f) return "Target is too far away.";

            if (oPC.Chest.CustomItemType != CustomItemType.HeavyArmor)
                return "You must be equipped with heavy armor to use that combat ability.";

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
            var perkRank = PerkService.GetCreaturePerkLevel(oPC, PerkType.Provoke);

            if (perkRank == 2) baseCooldownTime -= 5.0f;
            else if (perkRank == 3) baseCooldownTime -= 10.0f;
            else if (perkRank == 4) baseCooldownTime -= 15.0f;

            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            NWCreature npc = (target.Object);
            var vfx = NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Charm);
            NWScript.ApplyEffectToObject(DurationType.Instant, vfx, target.Object);
            
            creature.AssignCommand(() =>
            {
                NWScript.ActionPlayAnimation(Animation.FireForgetTaunt, 1f, 1f);
            });

            EnmityService.AdjustEnmity(npc, creature, 120);
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
            if (newLevel == 1)
            {
                ApplyFeatChanges(creature, null);
            }
        }

        public void OnRemoved(NWCreature creature)
        {
            Creature.RemoveFeat(creature, Feat.Provoke);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.HeavyArmor) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.HeavyArmor) return;
            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            var equipped = oItem ?? creature.Chest;
            
            if (equipped.Equals(oItem) || equipped.CustomItemType != CustomItemType.HeavyArmor)
            {
                Creature.RemoveFeat(creature, Feat.Provoke);
                return;
            }

            Creature.AddFeat(creature, Feat.Provoke);
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

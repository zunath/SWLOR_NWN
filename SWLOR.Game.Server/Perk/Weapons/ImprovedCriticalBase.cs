using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public abstract class ImprovedCriticalBase : IPerkHandler
    {
        public abstract PerkType PerkType { get; }

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
            ApplyFeatChanges(creature, null);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem equipped = oItem ?? creature.RightHand;
            RemoveAllFeats(creature);

            // Unarmed check
            NWItem mainHand = creature.RightHand;
            NWItem offHand = creature.LeftHand;
            if (oItem != null && Equals(oItem, mainHand))
            {
                mainHand = (new NWGameObject());
            }
            else if (oItem != null && Equals(oItem, offHand))
            {
                offHand = (new NWGameObject());
            }

            if (!mainHand.IsValid && !offHand.IsValid)
            {
                if (PerkService.GetCreaturePerkLevel(creature, PerkType.ImprovedCriticalMartialArts) > 0)
                {
                    NWNXCreature.AddFeat(creature, FEAT_IMPROVED_CRITICAL_UNARMED_STRIKE);
                }
                return;
            }

            if (oItem != null && Equals(oItem, equipped)) return;

            // All other weapon types
            PerkType perkType;
            switch (equipped.CustomItemType)
            {
                case CustomItemType.Vibroblade: perkType = PerkType.ImprovedCriticalVibroblades; break;
                case CustomItemType.FinesseVibroblade: perkType = PerkType.ImprovedCriticalFinesseVibroblades; break;
                case CustomItemType.Baton: perkType = PerkType.ImprovedCriticalBatons; break;
                case CustomItemType.HeavyVibroblade: perkType = PerkType.ImprovedCriticalHeavyVibroblades; break;
                case CustomItemType.Polearm: perkType = PerkType.ImprovedCriticalPolearms; break;
                case CustomItemType.TwinBlade: perkType = PerkType.ImprovedCriticalTwinVibroblades; break;
                case CustomItemType.MartialArtWeapon: perkType = PerkType.ImprovedCriticalMartialArts; break;
                case CustomItemType.BlasterPistol: perkType = PerkType.ImprovedCriticalBlasterPistols; break;
                case CustomItemType.BlasterRifle: perkType = PerkType.ImprovedCriticalBlasterRifles; break;
                case CustomItemType.Throwing: perkType = PerkType.ImprovedCriticalThrowing; break;
                case CustomItemType.Lightsaber: perkType = PerkType.ImprovedCriticalLightsabers; break;
                case CustomItemType.Saberstaff: perkType = PerkType.ImprovedCriticalSaberstaffs; break;
                default: return;
            }

            if (equipped.GetLocalInt("LIGHTSABER") == TRUE)
            {
                perkType = PerkType.ImprovedCriticalLightsabers;
            }
            
            int perkLevel = PerkService.GetCreaturePerkLevel(creature, perkType);
            int type = equipped.BaseItemType;
            if (perkLevel > 0)
            {
                AddCriticalFeat(creature, type);
            }
        }

        private void RemoveAllFeats(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_BASTARD_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_BATTLE_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_CLUB);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_DAGGER);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_DART);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_DIRE_MACE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_DOUBLE_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_DWAXE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_GREAT_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_GREAT_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_HALBERD);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_HAND_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_HEAVY_CROSSBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_HEAVY_FLAIL);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_KAMA);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_KATANA);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_KUKRI);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_LIGHT_CROSSBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_LIGHT_FLAIL);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_LIGHT_HAMMER);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_LIGHT_MACE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_LONGBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_LONG_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_MORNING_STAR);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_RAPIER);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_SCIMITAR);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_SCYTHE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_SHORTBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_SHORT_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_SHURIKEN);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_SICKLE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_SLING);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_SPEAR);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_STAFF);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_THROWING_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_TRIDENT);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_TWO_BLADED_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_UNARMED_STRIKE);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_WAR_HAMMER);
            NWNXCreature.RemoveFeat(creature, FEAT_IMPROVED_CRITICAL_WHIP);
        }

        private void AddCriticalFeat(NWCreature creature, int type)
        {
            int feat;

            switch (type)
            {
                case (BASE_ITEM_BASTARDSWORD): feat = FEAT_IMPROVED_CRITICAL_BASTARD_SWORD; break;
                case (BASE_ITEM_BATTLEAXE): feat = FEAT_IMPROVED_CRITICAL_BATTLE_AXE; break;
                case (BASE_ITEM_CLUB): feat = FEAT_IMPROVED_CRITICAL_CLUB; break;
                case (BASE_ITEM_DAGGER): feat = FEAT_IMPROVED_CRITICAL_DAGGER; break;
                case (BASE_ITEM_DART): feat = FEAT_IMPROVED_CRITICAL_DART; break;
                case (BASE_ITEM_DIREMACE): feat = FEAT_IMPROVED_CRITICAL_DIRE_MACE; break;
                case (BASE_ITEM_DOUBLEAXE): feat = FEAT_IMPROVED_CRITICAL_DOUBLE_AXE; break;
                case (BASE_ITEM_DWARVENWARAXE): feat = FEAT_IMPROVED_CRITICAL_DWAXE; break;
                case (BASE_ITEM_GREATAXE): feat = FEAT_IMPROVED_CRITICAL_GREAT_AXE; break;
                case (BASE_ITEM_GREATSWORD): feat = FEAT_IMPROVED_CRITICAL_GREAT_SWORD; break;
                case (BASE_ITEM_HALBERD): feat = FEAT_IMPROVED_CRITICAL_HALBERD; break;
                case (BASE_ITEM_HANDAXE): feat = FEAT_IMPROVED_CRITICAL_HAND_AXE; break;
                case (BASE_ITEM_HEAVYCROSSBOW): feat = FEAT_IMPROVED_CRITICAL_HEAVY_CROSSBOW; break;
                case (BASE_ITEM_HEAVYFLAIL): feat = FEAT_IMPROVED_CRITICAL_HEAVY_FLAIL; break;
                case (BASE_ITEM_KAMA): feat = FEAT_IMPROVED_CRITICAL_KAMA; break;
                case (BASE_ITEM_KATANA): feat = FEAT_IMPROVED_CRITICAL_KATANA; break;
                case (BASE_ITEM_KUKRI): feat = FEAT_IMPROVED_CRITICAL_KUKRI; break;
                case (BASE_ITEM_LIGHTCROSSBOW): feat = FEAT_IMPROVED_CRITICAL_LIGHT_CROSSBOW; break;
                case (BASE_ITEM_LIGHTFLAIL): feat = FEAT_IMPROVED_CRITICAL_LIGHT_FLAIL; break;
                case (BASE_ITEM_LIGHTHAMMER): feat = FEAT_IMPROVED_CRITICAL_LIGHT_HAMMER; break;
                case (BASE_ITEM_LIGHTMACE): feat = FEAT_IMPROVED_CRITICAL_LIGHT_MACE; break;
                case (BASE_ITEM_LONGBOW): feat = FEAT_IMPROVED_CRITICAL_LONGBOW; break;
                case (BASE_ITEM_LONGSWORD): feat = FEAT_IMPROVED_CRITICAL_LONG_SWORD; break;
                case (BASE_ITEM_MORNINGSTAR): feat = FEAT_IMPROVED_CRITICAL_MORNING_STAR; break;
                case (BASE_ITEM_RAPIER): feat = FEAT_IMPROVED_CRITICAL_RAPIER; break;
                case (BASE_ITEM_SCIMITAR): feat = FEAT_IMPROVED_CRITICAL_SCIMITAR; break;
                case (BASE_ITEM_SCYTHE): feat = FEAT_IMPROVED_CRITICAL_SCYTHE; break;
                case (BASE_ITEM_SHORTBOW): feat = FEAT_IMPROVED_CRITICAL_SHORTBOW; break;
                case (BASE_ITEM_SHORTSWORD): feat = FEAT_IMPROVED_CRITICAL_SHORT_SWORD; break;
                case (BASE_ITEM_SHURIKEN): feat = FEAT_IMPROVED_CRITICAL_SHURIKEN; break;
                case (BASE_ITEM_SICKLE): feat = FEAT_IMPROVED_CRITICAL_SICKLE; break;
                case (BASE_ITEM_SLING): feat = FEAT_IMPROVED_CRITICAL_SLING; break;
                case (BASE_ITEM_SHORTSPEAR): feat = FEAT_IMPROVED_CRITICAL_SPEAR; break;
                case (BASE_ITEM_QUARTERSTAFF): feat = FEAT_IMPROVED_CRITICAL_STAFF; break;
                case (BASE_ITEM_THROWINGAXE): feat = FEAT_IMPROVED_CRITICAL_THROWING_AXE; break;
                case (BASE_ITEM_TRIDENT): feat = FEAT_IMPROVED_CRITICAL_TRIDENT; break;
                case (BASE_ITEM_TWOBLADEDSWORD): feat = FEAT_IMPROVED_CRITICAL_TWO_BLADED_SWORD; break;
                case (BASE_ITEM_INVALID): feat = FEAT_IMPROVED_CRITICAL_UNARMED_STRIKE; break;
                case (BASE_ITEM_WARHAMMER): feat = FEAT_IMPROVED_CRITICAL_WAR_HAMMER; break;
                case (BASE_ITEM_WHIP): feat = FEAT_IMPROVED_CRITICAL_WHIP; break;
                case (CustomBaseItemType.Lightsaber): feat = FEAT_IMPROVED_CRITICAL_LONG_SWORD; break;
                case (CustomBaseItemType.Saberstaff): feat = FEAT_IMPROVED_CRITICAL_TWO_BLADED_SWORD; break;
                default: return;
            }

            NWNXCreature.AddFeat(creature, feat);
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

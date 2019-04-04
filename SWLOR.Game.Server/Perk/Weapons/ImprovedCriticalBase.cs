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

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
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
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnRemoved(NWPlayer oPC)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem equipped = oItem ?? oPC.RightHand;
            RemoveAllFeats(oPC);

            // Unarmed check
            NWItem mainHand = oPC.RightHand;
            NWItem offHand = oPC.LeftHand;
            if (oItem != null && Equals(oItem, mainHand))
            {
                mainHand = (new Object());
            }
            else if (oItem != null && Equals(oItem, offHand))
            {
                offHand = (new Object());
            }

            if (!mainHand.IsValid && !offHand.IsValid)
            {
                if (PerkService.GetPCPerkLevel(oPC, PerkType.ImprovedCriticalMartialArts) > 0)
                {
                    NWNXCreature.AddFeat(oPC, FEAT_IMPROVED_CRITICAL_UNARMED_STRIKE);
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
            
            int perkLevel = PerkService.GetPCPerkLevel(oPC, perkType);
            int type = equipped.BaseItemType;
            if (perkLevel > 0)
            {
                AddCriticalFeat(oPC, type);
            }
        }

        private void RemoveAllFeats(NWPlayer oPC)
        {
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_BASTARD_SWORD);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_BATTLE_AXE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_CLUB);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DAGGER);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DART);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DIRE_MACE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DOUBLE_AXE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DWAXE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_GREAT_AXE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_GREAT_SWORD);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_HALBERD);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_HAND_AXE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_HEAVY_CROSSBOW);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_HEAVY_FLAIL);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_KAMA);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_KATANA);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_KUKRI);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LIGHT_CROSSBOW);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LIGHT_FLAIL);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LIGHT_HAMMER);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LIGHT_MACE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LONGBOW);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LONG_SWORD);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_MORNING_STAR);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_RAPIER);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SCIMITAR);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SCYTHE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SHORTBOW);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SHORT_SWORD);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SHURIKEN);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SICKLE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SLING);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SPEAR);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_STAFF);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_THROWING_AXE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_TRIDENT);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_TWO_BLADED_SWORD);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_UNARMED_STRIKE);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_WAR_HAMMER);
            NWNXCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_WHIP);
        }

        private void AddCriticalFeat(NWPlayer oPC, int type)
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

            NWNXCreature.AddFeat(oPC, feat);
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}

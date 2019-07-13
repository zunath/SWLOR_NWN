using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public abstract class WeaponFocusBase : IPerkHandler
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
                int martialArtsLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.WeaponFocusMartialArts);
                if (martialArtsLevel >= 1)
                {
                    NWNXCreature.AddFeat(creature, FEAT_WEAPON_FOCUS_UNARMED_STRIKE);
                }
                if (martialArtsLevel >= 2)
                {
                    NWNXCreature.AddFeat(creature, FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE);
                }

                return;
            }

            if (oItem != null && Equals(oItem, equipped)) return;

            // All other weapon types
            PerkType perkType;
            switch (equipped.CustomItemType)
            {
                case CustomItemType.Vibroblade: perkType = PerkType.WeaponFocusVibroblades; break;
                case CustomItemType.FinesseVibroblade: perkType = PerkType.WeaponFocusFinesseVibroblades; break;
                case CustomItemType.Baton: perkType = PerkType.WeaponFocusBatons; break;
                case CustomItemType.HeavyVibroblade: perkType = PerkType.WeaponFocusHeavyVibroblades; break;
                case CustomItemType.Polearm: perkType = PerkType.WeaponFocusPolearms; break;
                case CustomItemType.TwinBlade: perkType = PerkType.WeaponFocusTwinVibroblades; break;
                case CustomItemType.MartialArtWeapon: perkType = PerkType.WeaponFocusMartialArts; break;
                case CustomItemType.BlasterPistol: perkType = PerkType.WeaponFocusBlasterPistols; break;
                case CustomItemType.BlasterRifle: perkType = PerkType.WeaponFocusBlasterRifles; break;
                case CustomItemType.Throwing: perkType = PerkType.WeaponFocusThrowing; break;
                case CustomItemType.Lightsaber: perkType = PerkType.WeaponFocusLightsaber; break;
                case CustomItemType.Saberstaff: perkType = PerkType.WeaponFocusSaberstaff; break;
                default: return;
            }

            if (equipped.GetLocalInt("LIGHTSABER") == TRUE)
            {
                perkType = PerkType.WeaponFocusLightsaber;
            }
            
            int perkLevel = PerkService.GetCreaturePerkLevel(creature, perkType);
            int type = equipped.BaseItemType;
            if (perkLevel >= 1)
            {
                AddFocusFeat(creature, type);
            }
            if (perkLevel >= 2)
            {
                AddSpecializationFeat(creature, type);
            }
        }

        private void RemoveAllFeats(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_BASTARD_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_BATTLE_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_CLUB);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_DAGGER);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_DART);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_DIRE_MACE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_DOUBLE_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_DWAXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_GREAT_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_GREAT_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_HALBERD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_HAND_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_HEAVY_CROSSBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_HEAVY_FLAIL);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_KAMA);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_KATANA);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_KUKRI);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_LIGHT_CROSSBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_LIGHT_FLAIL);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_LIGHT_HAMMER);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_LIGHT_MACE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_LONGBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_LONG_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_MORNING_STAR);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_RAPIER);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_SCIMITAR);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_SCYTHE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_SHORTBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_SHORT_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_SHURIKEN);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_SICKLE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_SLING);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_SPEAR);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_STAFF);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_THROWING_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_TRIDENT);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_UNARMED_STRIKE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_WAR_HAMMER);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_FOCUS_WHIP);


            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_BASTARD_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_BATTLE_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_CLUB);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_DAGGER);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_DART);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_DIRE_MACE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_DOUBLE_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_DWAXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_GREAT_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_GREAT_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_HALBERD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_HAND_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_HEAVY_CROSSBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_HEAVY_FLAIL);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_KAMA);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_KATANA);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_KUKRI);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_LIGHT_CROSSBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_LIGHT_FLAIL);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_LIGHT_HAMMER);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_LIGHT_MACE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_LONGBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_LONG_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_MORNING_STAR);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_RAPIER);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_SCIMITAR);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_SCYTHE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_SHORTBOW);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_SHORT_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_SHURIKEN);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_SICKLE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_SLING);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_SPEAR);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_STAFF);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_THROWING_AXE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_TRIDENT);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_WAR_HAMMER);
            NWNXCreature.RemoveFeat(creature, FEAT_WEAPON_SPECIALIZATION_WHIP);
        }

        private void AddFocusFeat(NWCreature creature, int type)
        {
            int feat;

            switch (type)
            {
                case (BASE_ITEM_BASTARDSWORD): feat = FEAT_WEAPON_FOCUS_BASTARD_SWORD; break;
                case (BASE_ITEM_BATTLEAXE): feat = FEAT_WEAPON_FOCUS_BATTLE_AXE; break;
                case (BASE_ITEM_CLUB): feat = FEAT_WEAPON_FOCUS_CLUB; break;
                case (BASE_ITEM_DAGGER): feat = FEAT_WEAPON_FOCUS_DAGGER; break;
                case (BASE_ITEM_DART): feat = FEAT_WEAPON_FOCUS_DART; break;
                case (BASE_ITEM_DIREMACE): feat = FEAT_WEAPON_FOCUS_DIRE_MACE; break;
                case (BASE_ITEM_DOUBLEAXE): feat = FEAT_WEAPON_FOCUS_DOUBLE_AXE; break;
                case (BASE_ITEM_DWARVENWARAXE): feat = FEAT_WEAPON_FOCUS_DWAXE; break;
                case (BASE_ITEM_GREATAXE): feat = FEAT_WEAPON_FOCUS_GREAT_AXE; break;
                case (BASE_ITEM_GREATSWORD): feat = FEAT_WEAPON_FOCUS_GREAT_SWORD; break;
                case (BASE_ITEM_HALBERD): feat = FEAT_WEAPON_FOCUS_HALBERD; break;
                case (BASE_ITEM_HANDAXE): feat = FEAT_WEAPON_FOCUS_HAND_AXE; break;
                case (BASE_ITEM_HEAVYCROSSBOW): feat = FEAT_WEAPON_FOCUS_HEAVY_CROSSBOW; break;
                case (BASE_ITEM_HEAVYFLAIL): feat = FEAT_WEAPON_FOCUS_HEAVY_FLAIL; break;
                case (BASE_ITEM_KAMA): feat = FEAT_WEAPON_FOCUS_KAMA; break;
                case (BASE_ITEM_KATANA): feat = FEAT_WEAPON_FOCUS_KATANA; break;
                case (BASE_ITEM_KUKRI): feat = FEAT_WEAPON_FOCUS_KUKRI; break;
                case (BASE_ITEM_LIGHTCROSSBOW): feat = FEAT_WEAPON_FOCUS_LIGHT_CROSSBOW; break;
                case (BASE_ITEM_LIGHTFLAIL): feat = FEAT_WEAPON_FOCUS_LIGHT_FLAIL; break;
                case (BASE_ITEM_LIGHTHAMMER): feat = FEAT_WEAPON_FOCUS_LIGHT_HAMMER; break;
                case (BASE_ITEM_LIGHTMACE): feat = FEAT_WEAPON_FOCUS_LIGHT_MACE; break;
                case (BASE_ITEM_LONGBOW): feat = FEAT_WEAPON_FOCUS_LONGBOW; break;
                case (BASE_ITEM_LONGSWORD): feat = FEAT_WEAPON_FOCUS_LONG_SWORD; break;
                case (BASE_ITEM_MORNINGSTAR): feat = FEAT_WEAPON_FOCUS_MORNING_STAR; break;
                case (BASE_ITEM_RAPIER): feat = FEAT_WEAPON_FOCUS_RAPIER; break;
                case (BASE_ITEM_SCIMITAR): feat = FEAT_WEAPON_FOCUS_SCIMITAR; break;
                case (BASE_ITEM_SCYTHE): feat = FEAT_WEAPON_FOCUS_SCYTHE; break;
                case (BASE_ITEM_SHORTBOW): feat = FEAT_WEAPON_FOCUS_SHORTBOW; break;
                case (BASE_ITEM_SHORTSWORD): feat = FEAT_WEAPON_FOCUS_SHORT_SWORD; break;
                case (BASE_ITEM_SHURIKEN): feat = FEAT_WEAPON_FOCUS_SHURIKEN; break;
                case (BASE_ITEM_SICKLE): feat = FEAT_WEAPON_FOCUS_SICKLE; break;
                case (BASE_ITEM_SLING): feat = FEAT_WEAPON_FOCUS_SLING; break;
                case (BASE_ITEM_SHORTSPEAR): feat = FEAT_WEAPON_FOCUS_SPEAR; break;
                case (BASE_ITEM_QUARTERSTAFF): feat = FEAT_WEAPON_FOCUS_STAFF; break;
                case (BASE_ITEM_THROWINGAXE): feat = FEAT_WEAPON_FOCUS_THROWING_AXE; break;
                case (BASE_ITEM_TRIDENT): feat = FEAT_WEAPON_FOCUS_TRIDENT; break;
                case (BASE_ITEM_TWOBLADEDSWORD): feat = FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD; break;
                case (BASE_ITEM_INVALID): feat = FEAT_WEAPON_FOCUS_UNARMED_STRIKE; break;
                case (BASE_ITEM_WARHAMMER): feat = FEAT_WEAPON_FOCUS_WAR_HAMMER; break;
                case (BASE_ITEM_WHIP): feat = FEAT_WEAPON_FOCUS_WHIP; break;
                case (CustomBaseItemType.Lightsaber): feat = FEAT_WEAPON_FOCUS_LONG_SWORD; break;
                case (CustomBaseItemType.Saberstaff): feat = FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD; break;
                default: return;
            }

            NWNXCreature.AddFeat(creature, feat);
        }


        private void AddSpecializationFeat(NWCreature creature, int type)
        {
            int feat;

            switch (type)
            {
                case (BASE_ITEM_BASTARDSWORD): feat = FEAT_WEAPON_SPECIALIZATION_BASTARD_SWORD; break;
                case (BASE_ITEM_BATTLEAXE): feat = FEAT_WEAPON_SPECIALIZATION_BATTLE_AXE; break;
                case (BASE_ITEM_CLUB): feat = FEAT_WEAPON_SPECIALIZATION_CLUB; break;
                case (BASE_ITEM_DAGGER): feat = FEAT_WEAPON_SPECIALIZATION_DAGGER; break;
                case (BASE_ITEM_DART): feat = FEAT_WEAPON_SPECIALIZATION_DART; break;
                case (BASE_ITEM_DIREMACE): feat = FEAT_WEAPON_SPECIALIZATION_DIRE_MACE; break;
                case (BASE_ITEM_DOUBLEAXE): feat = FEAT_WEAPON_SPECIALIZATION_DOUBLE_AXE; break;
                case (BASE_ITEM_DWARVENWARAXE): feat = FEAT_WEAPON_SPECIALIZATION_DWAXE; break;
                case (BASE_ITEM_GREATAXE): feat = FEAT_WEAPON_SPECIALIZATION_GREAT_AXE; break;
                case (BASE_ITEM_GREATSWORD): feat = FEAT_WEAPON_SPECIALIZATION_GREAT_SWORD; break;
                case (BASE_ITEM_HALBERD): feat = FEAT_WEAPON_SPECIALIZATION_HALBERD; break;
                case (BASE_ITEM_HANDAXE): feat = FEAT_WEAPON_SPECIALIZATION_HAND_AXE; break;
                case (BASE_ITEM_HEAVYCROSSBOW): feat = FEAT_WEAPON_SPECIALIZATION_HEAVY_CROSSBOW; break;
                case (BASE_ITEM_HEAVYFLAIL): feat = FEAT_WEAPON_SPECIALIZATION_HEAVY_FLAIL; break;
                case (BASE_ITEM_KAMA): feat = FEAT_WEAPON_SPECIALIZATION_KAMA; break;
                case (BASE_ITEM_KATANA): feat = FEAT_WEAPON_SPECIALIZATION_KATANA; break;
                case (BASE_ITEM_KUKRI): feat = FEAT_WEAPON_SPECIALIZATION_KUKRI; break;
                case (BASE_ITEM_LIGHTCROSSBOW): feat = FEAT_WEAPON_SPECIALIZATION_LIGHT_CROSSBOW; break;
                case (BASE_ITEM_LIGHTFLAIL): feat = FEAT_WEAPON_SPECIALIZATION_LIGHT_FLAIL; break;
                case (BASE_ITEM_LIGHTHAMMER): feat = FEAT_WEAPON_SPECIALIZATION_LIGHT_HAMMER; break;
                case (BASE_ITEM_LIGHTMACE): feat = FEAT_WEAPON_SPECIALIZATION_LIGHT_MACE; break;
                case (BASE_ITEM_LONGBOW): feat = FEAT_WEAPON_SPECIALIZATION_LONGBOW; break;
                case (BASE_ITEM_LONGSWORD): feat = FEAT_WEAPON_SPECIALIZATION_LONG_SWORD; break;
                case (BASE_ITEM_MORNINGSTAR): feat = FEAT_WEAPON_SPECIALIZATION_MORNING_STAR; break;
                case (BASE_ITEM_RAPIER): feat = FEAT_WEAPON_SPECIALIZATION_RAPIER; break;
                case (BASE_ITEM_SCIMITAR): feat = FEAT_WEAPON_SPECIALIZATION_SCIMITAR; break;
                case (BASE_ITEM_SCYTHE): feat = FEAT_WEAPON_SPECIALIZATION_SCYTHE; break;
                case (BASE_ITEM_SHORTBOW): feat = FEAT_WEAPON_SPECIALIZATION_SHORTBOW; break;
                case (BASE_ITEM_SHORTSWORD): feat = FEAT_WEAPON_SPECIALIZATION_SHORT_SWORD; break;
                case (BASE_ITEM_SHURIKEN): feat = FEAT_WEAPON_SPECIALIZATION_SHURIKEN; break;
                case (BASE_ITEM_SICKLE): feat = FEAT_WEAPON_SPECIALIZATION_SICKLE; break;
                case (BASE_ITEM_SLING): feat = FEAT_WEAPON_SPECIALIZATION_SLING; break;
                case (BASE_ITEM_SHORTSPEAR): feat = FEAT_WEAPON_SPECIALIZATION_SPEAR; break;
                case (BASE_ITEM_QUARTERSTAFF): feat = FEAT_WEAPON_SPECIALIZATION_STAFF; break;
                case (BASE_ITEM_THROWINGAXE): feat = FEAT_WEAPON_SPECIALIZATION_THROWING_AXE; break;
                case (BASE_ITEM_TRIDENT): feat = FEAT_WEAPON_SPECIALIZATION_TRIDENT; break;
                case (BASE_ITEM_TWOBLADEDSWORD): feat = FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD; break;
                case (BASE_ITEM_INVALID): feat = FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE; break;
                case (BASE_ITEM_WARHAMMER): feat = FEAT_WEAPON_SPECIALIZATION_WAR_HAMMER; break;
                case (BASE_ITEM_WHIP): feat = FEAT_WEAPON_SPECIALIZATION_WHIP; break;
                case (CustomBaseItemType.Lightsaber): feat = FEAT_WEAPON_SPECIALIZATION_LONG_SWORD; break;
                case (CustomBaseItemType.Saberstaff): feat = FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD; break;
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

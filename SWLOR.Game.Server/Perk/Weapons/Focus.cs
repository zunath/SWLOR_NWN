using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class Focus : IPerk
    {
        private readonly INWScript _;
        private readonly INWNXCreature _nwnxCreature;
        private readonly IPerkService _perk;

        public Focus(INWScript script,
            INWNXCreature nwnxCreature,
            IPerkService perk)
        {
            _ = script;
            _nwnxCreature = nwnxCreature;
            _perk = perk;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return false;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
        }

        public int FPCost(NWPlayer oPC, int baseFPCost)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWPlayer oPC, NWObject oTarget)
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
                mainHand = NWItem.Wrap(new Object());
            }
            else if (oItem != null && Equals(oItem, offHand))
            {
                offHand = NWItem.Wrap(new Object());
            }

            if (!mainHand.IsValid && !offHand.IsValid) 
            {
                int martialArtsLevel = _perk.GetPCPerkLevel(oPC, PerkType.WeaponFocusMartialArts);
                if (martialArtsLevel >= 1)
                {
                    _nwnxCreature.AddFeat(oPC, FEAT_WEAPON_FOCUS_UNARMED_STRIKE);
                }
                if (martialArtsLevel >= 2)
                {
                    _nwnxCreature.AddFeat(oPC, FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE);
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
                case CustomItemType.Saberstaff: perkType = PerkType.WeaponFocusSaberstaff; break;
                case CustomItemType.Polearm: perkType = PerkType.WeaponFocusPolearms; break;
                case CustomItemType.TwinBlade: perkType = PerkType.WeaponFocusTwinVibroblades; break;
                case CustomItemType.MartialArtWeapon: perkType = PerkType.WeaponFocusMartialArts; break;
                case CustomItemType.BlasterPistol: perkType = PerkType.WeaponFocusBlasterPistols; break;
                case CustomItemType.BlasterRifle: perkType = PerkType.WeaponFocusBlasterRifles; break;
                case CustomItemType.Throwing: perkType = PerkType.WeaponFocusThrowing; break;
                case CustomItemType.Lightsaber: perkType = PerkType.WeaponFocusLightsaber; break;
                default: return;
            }
            
            int perkLevel = _perk.GetPCPerkLevel(oPC, perkType);
            int type = equipped.BaseItemType;
            if (perkLevel >= 1)
            {
                AddFocusFeat(oPC, type);
            }
            if (perkLevel >= 2)
            {
                AddSpecializationFeat(oPC, type);
            }
        }

        private void RemoveAllFeats(NWPlayer oPC)
        {
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_BASTARD_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_BATTLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_CLUB);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_DAGGER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_DART);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_DIRE_MACE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_DOUBLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_DWAXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_GREAT_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_GREAT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_HALBERD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_HAND_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_HEAVY_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_HEAVY_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_KAMA);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_KATANA);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_KUKRI);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_LIGHT_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_LIGHT_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_LIGHT_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_LIGHT_MACE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_LONGBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_LONG_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_MORNING_STAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_RAPIER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_SCIMITAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_SCYTHE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_SHORTBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_SHORT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_SHURIKEN);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_SICKLE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_SLING);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_SPEAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_STAFF);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_THROWING_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_TRIDENT);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_UNARMED_STRIKE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_WAR_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_FOCUS_WHIP);


            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_BASTARD_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_BATTLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_CLUB);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_DAGGER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_DART);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_DIRE_MACE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_DOUBLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_DWAXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_GREAT_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_GREAT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_HALBERD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_HAND_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_HEAVY_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_HEAVY_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_KAMA);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_KATANA);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_KUKRI);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_LIGHT_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_LIGHT_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_LIGHT_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_LIGHT_MACE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_LONGBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_LONG_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_MORNING_STAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_RAPIER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_SCIMITAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_SCYTHE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_SHORTBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_SHORT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_SHURIKEN);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_SICKLE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_SLING);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_SPEAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_STAFF);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_THROWING_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_TRIDENT);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_WAR_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_WEAPON_SPECIALIZATION_WHIP);
        }

        private void AddFocusFeat(NWPlayer oPC, int type)
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
                default: return;
            }

            _nwnxCreature.AddFeat(oPC, feat);
        }


        private void AddSpecializationFeat(NWPlayer oPC, int type)
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
                default: return;
            }

            _nwnxCreature.AddFeat(oPC, feat);
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}

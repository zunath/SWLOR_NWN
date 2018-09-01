using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

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

        public int ManaCost(NWPlayer oPC, int baseManaCost)
        {
            return baseManaCost;
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
                    _nwnxCreature.AddFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_UNARMED_STRIKE);
                }
                if (martialArtsLevel >= 2)
                {
                    _nwnxCreature.AddFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE);
                }

                return;
            }

            if (oItem != null && Equals(oItem, equipped)) return;

            // All other weapon types
            PerkType perkType;
            switch (equipped.CustomItemType)
            {
                case CustomItemType.Blade: perkType = PerkType.WeaponFocusBlades; break;
                case CustomItemType.FinesseBlade: perkType = PerkType.WeaponFocusFinesseBlades; break;
                case CustomItemType.Blunt: perkType = PerkType.WeaponFocusBlunts; break;
                case CustomItemType.HeavyBlade: perkType = PerkType.WeaponFocusHeavyBlades; break;
                case CustomItemType.HeavyBlunt: perkType = PerkType.WeaponFocusHeavyBlunts; break;
                case CustomItemType.Polearm: perkType = PerkType.WeaponFocusPolearms; break;
                case CustomItemType.TwinBlade: perkType = PerkType.WeaponFocusTwinBlades; break;
                case CustomItemType.MartialArtWeapon: perkType = PerkType.WeaponFocusMartialArts; break;
                case CustomItemType.Blaster: perkType = PerkType.WeaponFocusBows; break;
                case CustomItemType.Rifle: perkType = PerkType.WeaponFocusCrossbows; break;
                case CustomItemType.Throwing: perkType = PerkType.WeaponFocusThrowing; break;
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
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_BASTARD_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_BATTLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_CLUB);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_DAGGER);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_DART);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_DIRE_MACE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_DOUBLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_DWAXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_GREAT_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_GREAT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_HALBERD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_HAND_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_HEAVY_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_HEAVY_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_KAMA);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_KATANA);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_KUKRI);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_LIGHT_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_LIGHT_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_LIGHT_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_LIGHT_MACE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_LONGBOW);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_LONG_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_MORNING_STAR);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_RAPIER);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_SCIMITAR);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_SCYTHE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_SHORTBOW);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_SHORT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_SHURIKEN);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_SICKLE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_SLING);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_SPEAR);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_STAFF);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_THROWING_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_TRIDENT);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_UNARMED_STRIKE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_WAR_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_FOCUS_WHIP);


            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_BASTARD_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_BATTLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_CLUB);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_DAGGER);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_DART);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_DIRE_MACE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_DOUBLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_DWAXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_GREAT_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_GREAT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_HALBERD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_HAND_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_HEAVY_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_HEAVY_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_KAMA);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_KATANA);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_KUKRI);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_MACE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_LONGBOW);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_LONG_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_MORNING_STAR);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_RAPIER);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_SCIMITAR);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_SCYTHE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_SHORTBOW);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_SHORT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_SHURIKEN);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_SICKLE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_SLING);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_SPEAR);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_STAFF);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_THROWING_AXE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_TRIDENT);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_WAR_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, NWScript.FEAT_WEAPON_SPECIALIZATION_WHIP);
        }

        private void AddFocusFeat(NWPlayer oPC, int type)
        {
            int feat;

            switch (type)
            {
                case (NWScript.BASE_ITEM_BASTARDSWORD): feat = NWScript.FEAT_WEAPON_FOCUS_BASTARD_SWORD; break;
                case (NWScript.BASE_ITEM_BATTLEAXE): feat = NWScript.FEAT_WEAPON_FOCUS_BATTLE_AXE; break;
                case (NWScript.BASE_ITEM_CLUB): feat = NWScript.FEAT_WEAPON_FOCUS_CLUB; break;
                case (NWScript.BASE_ITEM_DAGGER): feat = NWScript.FEAT_WEAPON_FOCUS_DAGGER; break;
                case (NWScript.BASE_ITEM_DART): feat = NWScript.FEAT_WEAPON_FOCUS_DART; break;
                case (NWScript.BASE_ITEM_DIREMACE): feat = NWScript.FEAT_WEAPON_FOCUS_DIRE_MACE; break;
                case (NWScript.BASE_ITEM_DOUBLEAXE): feat = NWScript.FEAT_WEAPON_FOCUS_DOUBLE_AXE; break;
                case (NWScript.BASE_ITEM_DWARVENWARAXE): feat = NWScript.FEAT_WEAPON_FOCUS_DWAXE; break;
                case (NWScript.BASE_ITEM_GREATAXE): feat = NWScript.FEAT_WEAPON_FOCUS_GREAT_AXE; break;
                case (NWScript.BASE_ITEM_GREATSWORD): feat = NWScript.FEAT_WEAPON_FOCUS_GREAT_SWORD; break;
                case (NWScript.BASE_ITEM_HALBERD): feat = NWScript.FEAT_WEAPON_FOCUS_HALBERD; break;
                case (NWScript.BASE_ITEM_HANDAXE): feat = NWScript.FEAT_WEAPON_FOCUS_HAND_AXE; break;
                case (NWScript.BASE_ITEM_HEAVYCROSSBOW): feat = NWScript.FEAT_WEAPON_FOCUS_HEAVY_CROSSBOW; break;
                case (NWScript.BASE_ITEM_HEAVYFLAIL): feat = NWScript.FEAT_WEAPON_FOCUS_HEAVY_FLAIL; break;
                case (NWScript.BASE_ITEM_KAMA): feat = NWScript.FEAT_WEAPON_FOCUS_KAMA; break;
                case (NWScript.BASE_ITEM_KATANA): feat = NWScript.FEAT_WEAPON_FOCUS_KATANA; break;
                case (NWScript.BASE_ITEM_KUKRI): feat = NWScript.FEAT_WEAPON_FOCUS_KUKRI; break;
                case (NWScript.BASE_ITEM_LIGHTCROSSBOW): feat = NWScript.FEAT_WEAPON_FOCUS_LIGHT_CROSSBOW; break;
                case (NWScript.BASE_ITEM_LIGHTFLAIL): feat = NWScript.FEAT_WEAPON_FOCUS_LIGHT_FLAIL; break;
                case (NWScript.BASE_ITEM_LIGHTHAMMER): feat = NWScript.FEAT_WEAPON_FOCUS_LIGHT_HAMMER; break;
                case (NWScript.BASE_ITEM_LIGHTMACE): feat = NWScript.FEAT_WEAPON_FOCUS_LIGHT_MACE; break;
                case (NWScript.BASE_ITEM_LONGBOW): feat = NWScript.FEAT_WEAPON_FOCUS_LONGBOW; break;
                case (NWScript.BASE_ITEM_LONGSWORD): feat = NWScript.FEAT_WEAPON_FOCUS_LONG_SWORD; break;
                case (NWScript.BASE_ITEM_MORNINGSTAR): feat = NWScript.FEAT_WEAPON_FOCUS_MORNING_STAR; break;
                case (NWScript.BASE_ITEM_RAPIER): feat = NWScript.FEAT_WEAPON_FOCUS_RAPIER; break;
                case (NWScript.BASE_ITEM_SCIMITAR): feat = NWScript.FEAT_WEAPON_FOCUS_SCIMITAR; break;
                case (NWScript.BASE_ITEM_SCYTHE): feat = NWScript.FEAT_WEAPON_FOCUS_SCYTHE; break;
                case (NWScript.BASE_ITEM_SHORTBOW): feat = NWScript.FEAT_WEAPON_FOCUS_SHORTBOW; break;
                case (NWScript.BASE_ITEM_SHORTSWORD): feat = NWScript.FEAT_WEAPON_FOCUS_SHORT_SWORD; break;
                case (NWScript.BASE_ITEM_SHURIKEN): feat = NWScript.FEAT_WEAPON_FOCUS_SHURIKEN; break;
                case (NWScript.BASE_ITEM_SICKLE): feat = NWScript.FEAT_WEAPON_FOCUS_SICKLE; break;
                case (NWScript.BASE_ITEM_SLING): feat = NWScript.FEAT_WEAPON_FOCUS_SLING; break;
                case (NWScript.BASE_ITEM_SHORTSPEAR): feat = NWScript.FEAT_WEAPON_FOCUS_SPEAR; break;
                case (NWScript.BASE_ITEM_QUARTERSTAFF): feat = NWScript.FEAT_WEAPON_FOCUS_STAFF; break;
                case (NWScript.BASE_ITEM_THROWINGAXE): feat = NWScript.FEAT_WEAPON_FOCUS_THROWING_AXE; break;
                case (NWScript.BASE_ITEM_TRIDENT): feat = NWScript.FEAT_WEAPON_FOCUS_TRIDENT; break;
                case (NWScript.BASE_ITEM_TWOBLADEDSWORD): feat = NWScript.FEAT_WEAPON_FOCUS_TWO_BLADED_SWORD; break;
                case (NWScript.BASE_ITEM_INVALID): feat = NWScript.FEAT_WEAPON_FOCUS_UNARMED_STRIKE; break;
                case (NWScript.BASE_ITEM_WARHAMMER): feat = NWScript.FEAT_WEAPON_FOCUS_WAR_HAMMER; break;
                case (NWScript.BASE_ITEM_WHIP): feat = NWScript.FEAT_WEAPON_FOCUS_WHIP; break;
                default: return;
            }

            _nwnxCreature.AddFeat(oPC, feat);
        }


        private void AddSpecializationFeat(NWPlayer oPC, int type)
        {
            int feat;

            switch (type)
            {
                case (NWScript.BASE_ITEM_BASTARDSWORD): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_BASTARD_SWORD; break;
                case (NWScript.BASE_ITEM_BATTLEAXE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_BATTLE_AXE; break;
                case (NWScript.BASE_ITEM_CLUB): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_CLUB; break;
                case (NWScript.BASE_ITEM_DAGGER): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_DAGGER; break;
                case (NWScript.BASE_ITEM_DART): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_DART; break;
                case (NWScript.BASE_ITEM_DIREMACE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_DIRE_MACE; break;
                case (NWScript.BASE_ITEM_DOUBLEAXE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_DOUBLE_AXE; break;
                case (NWScript.BASE_ITEM_DWARVENWARAXE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_DWAXE; break;
                case (NWScript.BASE_ITEM_GREATAXE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_GREAT_AXE; break;
                case (NWScript.BASE_ITEM_GREATSWORD): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_GREAT_SWORD; break;
                case (NWScript.BASE_ITEM_HALBERD): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_HALBERD; break;
                case (NWScript.BASE_ITEM_HANDAXE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_HAND_AXE; break;
                case (NWScript.BASE_ITEM_HEAVYCROSSBOW): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_HEAVY_CROSSBOW; break;
                case (NWScript.BASE_ITEM_HEAVYFLAIL): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_HEAVY_FLAIL; break;
                case (NWScript.BASE_ITEM_KAMA): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_KAMA; break;
                case (NWScript.BASE_ITEM_KATANA): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_KATANA; break;
                case (NWScript.BASE_ITEM_KUKRI): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_KUKRI; break;
                case (NWScript.BASE_ITEM_LIGHTCROSSBOW): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_CROSSBOW; break;
                case (NWScript.BASE_ITEM_LIGHTFLAIL): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_FLAIL; break;
                case (NWScript.BASE_ITEM_LIGHTHAMMER): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_HAMMER; break;
                case (NWScript.BASE_ITEM_LIGHTMACE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_LIGHT_MACE; break;
                case (NWScript.BASE_ITEM_LONGBOW): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_LONGBOW; break;
                case (NWScript.BASE_ITEM_LONGSWORD): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_LONG_SWORD; break;
                case (NWScript.BASE_ITEM_MORNINGSTAR): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_MORNING_STAR; break;
                case (NWScript.BASE_ITEM_RAPIER): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_RAPIER; break;
                case (NWScript.BASE_ITEM_SCIMITAR): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_SCIMITAR; break;
                case (NWScript.BASE_ITEM_SCYTHE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_SCYTHE; break;
                case (NWScript.BASE_ITEM_SHORTBOW): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_SHORTBOW; break;
                case (NWScript.BASE_ITEM_SHORTSWORD): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_SHORT_SWORD; break;
                case (NWScript.BASE_ITEM_SHURIKEN): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_SHURIKEN; break;
                case (NWScript.BASE_ITEM_SICKLE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_SICKLE; break;
                case (NWScript.BASE_ITEM_SLING): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_SLING; break;
                case (NWScript.BASE_ITEM_SHORTSPEAR): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_SPEAR; break;
                case (NWScript.BASE_ITEM_QUARTERSTAFF): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_STAFF; break;
                case (NWScript.BASE_ITEM_THROWINGAXE): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_THROWING_AXE; break;
                case (NWScript.BASE_ITEM_TRIDENT): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_TRIDENT; break;
                case (NWScript.BASE_ITEM_TWOBLADEDSWORD): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_TWO_BLADED_SWORD; break;
                case (NWScript.BASE_ITEM_INVALID): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_UNARMED_STRIKE; break;
                case (NWScript.BASE_ITEM_WARHAMMER): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_WAR_HAMMER; break;
                case (NWScript.BASE_ITEM_WHIP): feat = NWScript.FEAT_WEAPON_SPECIALIZATION_WHIP; break;
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

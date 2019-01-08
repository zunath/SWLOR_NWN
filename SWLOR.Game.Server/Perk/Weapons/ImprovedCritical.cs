using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public class ImprovedCritical : IPerk
    {
        private readonly INWScript _;
        private readonly INWNXCreature _nwnxCreature;
        private readonly IPerkService _perk;

        public ImprovedCritical(INWScript script,
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
                if (_perk.GetPCPerkLevel(oPC, PerkType.ImprovedCriticalMartialArts) > 0)
                {
                    _nwnxCreature.AddFeat(oPC, FEAT_IMPROVED_CRITICAL_UNARMED_STRIKE);
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
            
            int perkLevel = _perk.GetPCPerkLevel(oPC, perkType);
            int type = equipped.BaseItemType;
            if (perkLevel > 0)
            {
                AddCriticalFeat(oPC, type);
            }
        }

        private void RemoveAllFeats(NWPlayer oPC)
        {
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_BASTARD_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_BATTLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_CLUB);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DAGGER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DART);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DIRE_MACE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DOUBLE_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_DWAXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_GREAT_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_GREAT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_HALBERD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_HAND_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_HEAVY_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_HEAVY_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_KAMA);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_KATANA);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_KUKRI);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LIGHT_CROSSBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LIGHT_FLAIL);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LIGHT_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LIGHT_MACE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LONGBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_LONG_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_MORNING_STAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_RAPIER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SCIMITAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SCYTHE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SHORTBOW);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SHORT_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SHURIKEN);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SICKLE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SLING);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_SPEAR);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_STAFF);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_THROWING_AXE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_TRIDENT);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_TWO_BLADED_SWORD);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_UNARMED_STRIKE);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_WAR_HAMMER);
            _nwnxCreature.RemoveFeat(oPC, FEAT_IMPROVED_CRITICAL_WHIP);
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

            _nwnxCreature.AddFeat(oPC, feat);
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}

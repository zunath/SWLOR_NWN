using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Item;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

using static SWLOR.Game.Server.NWN._;

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
                mainHand = _.OBJECT_INVALID;
            }
            else if (oItem != null && Equals(oItem, offHand))
            {
                offHand = _.OBJECT_INVALID;
            }

            if (!mainHand.IsValid && !offHand.IsValid)
            {
                if (PerkService.GetCreaturePerkLevel(creature, PerkType.ImprovedCriticalMartialArts) > 0)
                {
                    NWNXCreature.AddFeat(creature, Feat.ImprovedCritical_UnarmedStrike);
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

            if (equipped.GetLocalBool("LIGHTSABER") == true)
            {
                perkType = PerkType.ImprovedCriticalLightsabers;
            }
            
            int perkLevel = PerkService.GetCreaturePerkLevel(creature, perkType);
            var type = equipped.BaseItemType;
            if (perkLevel > 0)
            {
                AddCriticalFeat(creature, type);
            }
        }

        private void RemoveAllFeats(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_BastardSword);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_BattleAxe);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Club);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Dagger);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Dart);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_DireMace);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_DoubleAxe);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Dwaxe);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_GreatAxe);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_GreatSword);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Halberd);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_HandAxe);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_HeavyCrossbow);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_HeavyFlail);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Kama);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Katana);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Kukri);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_LightCrossbow);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_LightFlail);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_LightHammer);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_LightMace);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Longbow);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_LongSword);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_MorningStar);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Rapier);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Scimitar);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Scythe);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Shortbow);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_ShortSword);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Shuriken);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Sickle);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Sling);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Spear);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Staff);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_ThrowingAxe);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Trident);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_TwoBladedSword);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_UnarmedStrike);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_WarHammer);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_Whip);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_LongSword);
            NWNXCreature.RemoveFeat(creature, Feat.ImprovedCritical_TwoBladedSword);
        }

        private void AddCriticalFeat(NWCreature creature, BaseItem type)
        {
            Feat feat;

            switch (type)
            {

                case (BaseItem.BastardSword): feat = Feat.ImprovedCritical_BastardSword; break;
                case (BaseItem.BattleAxe): feat = Feat.ImprovedCritical_BattleAxe; break;
                case (BaseItem.Club): feat = Feat.ImprovedCritical_Club; break;
                case (BaseItem.Dagger): feat = Feat.ImprovedCritical_Dagger; break;
                case (BaseItem.Dart): feat = Feat.ImprovedCritical_Dart; break;
                case (BaseItem.DireMace): feat = Feat.ImprovedCritical_DireMace; break;
                case (BaseItem.DoubleAxe): feat = Feat.ImprovedCritical_DoubleAxe; break;
                case (BaseItem.DwarvenWarAxe): feat = Feat.ImprovedCritical_Dwaxe; break;
                case (BaseItem.GreatAxe): feat = Feat.ImprovedCritical_GreatAxe; break;
                case (BaseItem.GreatSword): feat = Feat.ImprovedCritical_GreatSword; break;
                case (BaseItem.Halberd): feat = Feat.ImprovedCritical_Halberd; break;
                case (BaseItem.HandAxe): feat = Feat.ImprovedCritical_HandAxe; break;
                case (BaseItem.HeavyCrossbow): feat = Feat.ImprovedCritical_HeavyCrossbow; break;
                case (BaseItem.HeavyFlail): feat = Feat.ImprovedCritical_HeavyFlail; break;
                case (BaseItem.Kama): feat = Feat.ImprovedCritical_Kama; break;
                case (BaseItem.Katana): feat = Feat.ImprovedCritical_Katana; break;
                case (BaseItem.Kukri): feat = Feat.ImprovedCritical_Kukri; break;
                case (BaseItem.LightCrossbow): feat = Feat.ImprovedCritical_LightCrossbow; break;
                case (BaseItem.LightFlail): feat = Feat.ImprovedCritical_LightFlail; break;
                case (BaseItem.LightHammer): feat = Feat.ImprovedCritical_LightHammer; break;
                case (BaseItem.LightMace): feat = Feat.ImprovedCritical_LightMace; break;
                case (BaseItem.Longbow): feat = Feat.ImprovedCritical_Longbow; break;
                case (BaseItem.Longsword): feat = Feat.ImprovedCritical_LongSword; break;
                case (BaseItem.MorningStar): feat = Feat.ImprovedCritical_MorningStar; break;
                case (BaseItem.Rapier): feat = Feat.ImprovedCritical_Rapier; break;
                case (BaseItem.Scimitar): feat = Feat.ImprovedCritical_Scimitar; break;
                case (BaseItem.Scythe): feat = Feat.ImprovedCritical_Scythe; break;
                case (BaseItem.ShortBow): feat = Feat.ImprovedCritical_Shortbow; break;
                case (BaseItem.ShortSword): feat = Feat.ImprovedCritical_ShortSword; break;
                case (BaseItem.Shuriken): feat = Feat.ImprovedCritical_Shuriken; break;
                case (BaseItem.Sickle): feat = Feat.ImprovedCritical_Sickle; break;
                case (BaseItem.Sling): feat = Feat.ImprovedCritical_Sling; break;
                case (BaseItem.ShortSpear): feat = Feat.ImprovedCritical_Spear; break;
                case (BaseItem.QuarterStaff): feat = Feat.ImprovedCritical_Staff; break;
                case (BaseItem.ThrowingAxe): feat = Feat.ImprovedCritical_ThrowingAxe; break;
                case (BaseItem.Trident): feat = Feat.ImprovedCritical_Trident; break;
                case (BaseItem.TwoBladedSword): feat = Feat.ImprovedCritical_TwoBladedSword; break;
                case (BaseItem.Invalid): feat = Feat.ImprovedCritical_UnarmedStrike; break;
                case (BaseItem.WarHammer): feat = Feat.ImprovedCritical_WarHammer; break;
                case (BaseItem.Whip): feat = Feat.ImprovedCritical_Whip; break;
                case (BaseItem.Lightsaber): feat = Feat.ImprovedCritical_LongSword; break;
                case (BaseItem.Saberstaff): feat = Feat.ImprovedCritical_TwoBladedSword; break;

                default: return;
            }

            NWNXCreature.AddFeat(creature, (Feat)feat);
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

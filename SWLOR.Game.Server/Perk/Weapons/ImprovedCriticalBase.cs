using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service.Legacy;

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
            var equipped = oItem ?? creature.RightHand;
            RemoveAllFeats(creature);

            // Unarmed check
            var mainHand = creature.RightHand;
            var offHand = creature.LeftHand;
            if (oItem != null && Equals(oItem, mainHand))
            {
                mainHand = NWScript.OBJECT_INVALID;
            }
            else if (oItem != null && Equals(oItem, offHand))
            {
                offHand = NWScript.OBJECT_INVALID;
            }

            if (!mainHand.IsValid && !offHand.IsValid)
            {
                if (PerkService.GetCreaturePerkLevel(creature, PerkType.ImprovedCriticalMartialArts) > 0)
                {
                    Creature.AddFeat(creature, Feat.ImprovedCritical_UnarmedStrike);
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
            
            var perkLevel = PerkService.GetCreaturePerkLevel(creature, perkType);
            var type = equipped.BaseItemType;
            if (perkLevel > 0)
            {
                AddCriticalFeat(creature, type);
            }
        }

        private void RemoveAllFeats(NWCreature creature)
        {
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_BastardSword);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_BattleAxe);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Club);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Dagger);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Dart);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_DireMace);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_DoubleAxe);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Dwaxe);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_GreatAxe);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_GreatSword);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Halberd);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_HandAxe);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_HeavyCrossbow);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_HeavyFlail);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Kama);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Katana);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Kukri);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_LightCrossbow);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_LightFlail);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_LightHammer);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_LightMace);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Longbow);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_LongSword);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_MorningStar);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Rapier);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Scimitar);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Scythe);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Shortbow);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_ShortSword);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Shuriken);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Sickle);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Sling);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Spear);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Staff);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_ThrowingAxe);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Trident);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_TwoBladedSword);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_UnarmedStrike);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_WarHammer);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_Whip);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_LongSword);
            Creature.RemoveFeat(creature, Feat.ImprovedCritical_TwoBladedSword);
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

            Creature.AddFeat(creature, (Feat)feat);
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

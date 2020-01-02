using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public abstract class ImprovedCriticalBase : IPerk
    {
        public abstract PerkType PerkType { get; }
        public abstract string Name { get; }
        public abstract bool IsActive { get; }
        public abstract string Description { get; }
        public abstract PerkCategoryType Category { get; }
        public abstract PerkCooldownGroup CooldownGroup { get; }
        public abstract PerkExecutionType ExecutionType { get; }
        public abstract bool IsTargetSelfOnly { get; }
        public abstract int Enmity { get; }
        public abstract EnmityAdjustmentRuleType EnmityAdjustmentType { get; }
        public abstract ForceBalanceType ForceBalanceType { get; }
        public Animation CastAnimation => Animation.Invalid;


        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
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
                    NWNXCreature.AddFeat(creature, Feat.Improved_Critical_Unarmed_Strike);
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

            if (equipped.GetLocalBoolean("LIGHTSABER") == true)
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
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Bastard_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Battle_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Club);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Dagger);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Dart);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Dire_Mace);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Double_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Dwaxe);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Great_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Great_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Halberd);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Hand_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Heavy_Crossbow);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Heavy_Flail);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Kama);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Katana);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Kukri);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Light_Crossbow);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Light_Flail);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Light_Hammer);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Light_Mace);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Longbow);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Long_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Morning_Star);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Rapier);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Scimitar);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Scythe);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Shortbow);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Short_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Shuriken);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Sickle);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Sling);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Spear);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Staff);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Throwing_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Trident);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Two_Bladed_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Unarmed_Strike);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_War_Hammer);
            NWNXCreature.RemoveFeat(creature, Feat.Improved_Critical_Whip);
        }

        private void AddCriticalFeat(NWCreature creature, BaseItemType type)
        {
            Feat feat;

            switch (type)
            {
                case (BaseItemType.BastardSword): feat = Feat.Improved_Critical_Bastard_Sword; break;
                case (BaseItemType.BattleAxe): feat = Feat.Improved_Critical_Battle_Axe; break;
                case (BaseItemType.Club): feat = Feat.Improved_Critical_Club; break;
                case (BaseItemType.Dagger): feat = Feat.Improved_Critical_Dagger; break;
                case (BaseItemType.Dart): feat = Feat.Improved_Critical_Dart; break;
                case (BaseItemType.DireMace): feat = Feat.Improved_Critical_Dire_Mace; break;
                case (BaseItemType.DoubleAxe): feat = Feat.Improved_Critical_Double_Axe; break;
                case (BaseItemType.DwarvenWaraxe): feat = Feat.Improved_Critical_Dwaxe; break;
                case (BaseItemType.GreatAxe): feat = Feat.Improved_Critical_Great_Axe; break;
                case (BaseItemType.GreatSword): feat = Feat.Improved_Critical_Great_Sword; break;
                case (BaseItemType.Halberd): feat = Feat.Improved_Critical_Halberd; break;
                case (BaseItemType.HandAxe): feat = Feat.Improved_Critical_Hand_Axe; break;
                case (BaseItemType.HeavyCrossBow): feat = Feat.Improved_Critical_Heavy_Crossbow; break;
                case (BaseItemType.HeavyFlail): feat = Feat.Improved_Critical_Heavy_Flail; break;
                case (BaseItemType.Kama): feat = Feat.Improved_Critical_Kama; break;
                case (BaseItemType.Katana): feat = Feat.Improved_Critical_Katana; break;
                case (BaseItemType.Kukri): feat = Feat.Improved_Critical_Kukri; break;
                case (BaseItemType.LightCrossBow): feat = Feat.Improved_Critical_Light_Crossbow; break;
                case (BaseItemType.LightFlail): feat = Feat.Improved_Critical_Light_Flail; break;
                case (BaseItemType.LightHammer): feat = Feat.Improved_Critical_Light_Hammer; break;
                case (BaseItemType.LightMace): feat = Feat.Improved_Critical_Light_Mace; break;
                case (BaseItemType.LongBow): feat = Feat.Improved_Critical_Longbow; break;
                case (BaseItemType.LongSword): feat = Feat.Improved_Critical_Long_Sword; break;
                case (BaseItemType.Morningstar): feat = Feat.Improved_Critical_Morning_Star; break;
                case (BaseItemType.Rapier): feat = Feat.Improved_Critical_Rapier; break;
                case (BaseItemType.Scimitar): feat = Feat.Improved_Critical_Scimitar; break;
                case (BaseItemType.Scythe): feat = Feat.Improved_Critical_Scythe; break;
                case (BaseItemType.ShortBow): feat = Feat.Improved_Critical_Shortbow; break;
                case (BaseItemType.ShortSword): feat = Feat.Improved_Critical_Short_Sword; break;
                case (BaseItemType.Shuriken): feat = Feat.Improved_Critical_Shuriken; break;
                case (BaseItemType.Sickle): feat = Feat.Improved_Critical_Sickle; break;
                case (BaseItemType.Sling): feat = Feat.Improved_Critical_Sling; break;
                case (BaseItemType.ShortSpear): feat = Feat.Improved_Critical_Spear; break;
                case (BaseItemType.QuarterStaff): feat = Feat.Improved_Critical_Staff; break;
                case (BaseItemType.ThrowingAxe): feat = Feat.Improved_Critical_Throwing_Axe; break;
                case (BaseItemType.Trident): feat = Feat.Improved_Critical_Trident; break;
                case (BaseItemType.TwoBladedSword): feat = Feat.Improved_Critical_Two_Bladed_Sword; break;
                case (BaseItemType.Invalid): feat = Feat.Improved_Critical_Unarmed_Strike; break;
                case (BaseItemType.Warhammer): feat = Feat.Improved_Critical_War_Hammer; break;
                case (BaseItemType.Whip): feat = Feat.Improved_Critical_Whip; break;
                case (BaseItemType.Lightsaber): feat = Feat.Improved_Critical_Long_Sword; break;
                case (BaseItemType.Saberstaff): feat = Feat.Improved_Critical_Two_Bladed_Sword; break;
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

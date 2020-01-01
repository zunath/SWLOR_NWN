using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Weapons
{
    public abstract class WeaponFocusBase : IPerkHandler
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
                int martialArtsLevel = PerkService.GetCreaturePerkLevel(creature, PerkType.WeaponFocusMartialArts);
                if (martialArtsLevel >= 1)
                {
                    NWNXCreature.AddFeat(creature, Feat.Weapon_Focus_Unarmed_Strike);
                }
                if (martialArtsLevel >= 2)
                {
                    NWNXCreature.AddFeat(creature, Feat.Weapon_Specialization_Unarmed_Strike);
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

            if (equipped.GetLocalBoolean("LIGHTSABER") == true)
            {
                perkType = PerkType.WeaponFocusLightsaber;
            }
            
            int perkLevel = PerkService.GetCreaturePerkLevel(creature, perkType);
            var type = equipped.BaseItemType;
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
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Bastard_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Battle_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Club);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Dagger);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Dart);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Dire_Mace);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Double_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Dwaxe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Great_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Great_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Halberd);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Hand_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Heavy_Crossbow);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Heavy_Flail);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Kama);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Katana);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Kukri);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Light_Crossbow);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Light_Flail);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Light_Hammer);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Light_Mace);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Longbow);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Long_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Morning_Star);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Rapier);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Scimitar);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Scythe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Shortbow);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Short_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Shuriken);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Sickle);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Sling);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Spear);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Staff);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Throwing_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Trident);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Two_Bladed_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Unarmed_Strike);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_War_Hammer);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Focus_Whip);


            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Bastard_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Battle_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Club);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Dagger);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Dart);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Dire_Mace);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Double_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Dwaxe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Great_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Great_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Halberd);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Hand_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Heavy_Crossbow);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Heavy_Flail);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Kama);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Katana);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Kukri);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Light_Crossbow);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Light_Flail);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Light_Hammer);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Light_Mace);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Longbow);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Long_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Morning_Star);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Rapier);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Scimitar);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Scythe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Shortbow);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Short_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Shuriken);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Sickle);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Sling);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Spear);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Staff);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Throwing_Axe);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Trident);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Two_Bladed_Sword);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Unarmed_Strike);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_War_Hammer);
            NWNXCreature.RemoveFeat(creature, Feat.Weapon_Specialization_Whip);
        }

        private void AddFocusFeat(NWCreature creature, BaseItemType type)
        {
            Feat feat;

            switch (type)
            {
                case (BaseItemType.BastardSword): feat = Feat.Weapon_Focus_Bastard_Sword; break;
                case (BaseItemType.BattleAxe): feat = Feat.Weapon_Focus_Battle_Axe; break;
                case (BaseItemType.Club): feat = Feat.Weapon_Focus_Club; break;
                case (BaseItemType.Dagger): feat = Feat.Weapon_Focus_Dagger; break;
                case (BaseItemType.Dart): feat = Feat.Weapon_Focus_Dart; break;
                case (BaseItemType.DireMace): feat = Feat.Weapon_Focus_Dire_Mace; break;
                case (BaseItemType.DoubleAxe): feat = Feat.Weapon_Focus_Double_Axe; break;
                case (BaseItemType.DwarvenWaraxe): feat = Feat.Weapon_Focus_Dwaxe; break;
                case (BaseItemType.GreatAxe): feat = Feat.Weapon_Focus_Great_Axe; break;
                case (BaseItemType.GreatSword): feat = Feat.Weapon_Focus_Great_Sword; break;
                case (BaseItemType.Halberd): feat = Feat.Weapon_Focus_Halberd; break;
                case (BaseItemType.HandAxe): feat = Feat.Weapon_Focus_Hand_Axe; break;
                case (BaseItemType.HeavyCrossBow): feat = Feat.Weapon_Focus_Heavy_Crossbow; break;
                case (BaseItemType.HeavyFlail): feat = Feat.Weapon_Focus_Heavy_Flail; break;
                case (BaseItemType.Kama): feat = Feat.Weapon_Focus_Kama; break;
                case (BaseItemType.Katana): feat = Feat.Weapon_Focus_Katana; break;
                case (BaseItemType.Kukri): feat = Feat.Weapon_Focus_Kukri; break;
                case (BaseItemType.LightCrossBow): feat = Feat.Weapon_Focus_Light_Crossbow; break;
                case (BaseItemType.LightFlail): feat = Feat.Weapon_Focus_Light_Flail; break;
                case (BaseItemType.LightHammer): feat = Feat.Weapon_Focus_Light_Hammer; break;
                case (BaseItemType.LightMace): feat = Feat.Weapon_Focus_Light_Mace; break;
                case (BaseItemType.LongBow): feat = Feat.Weapon_Focus_Longbow; break;
                case (BaseItemType.LongSword): feat = Feat.Weapon_Focus_Long_Sword; break;
                case (BaseItemType.Morningstar): feat = Feat.Weapon_Focus_Morning_Star; break;
                case (BaseItemType.Rapier): feat = Feat.Weapon_Focus_Rapier; break;
                case (BaseItemType.Scimitar): feat = Feat.Weapon_Focus_Scimitar; break;
                case (BaseItemType.Scythe): feat = Feat.Weapon_Focus_Scythe; break;
                case (BaseItemType.ShortBow): feat = Feat.Weapon_Focus_Shortbow; break;
                case (BaseItemType.ShortSword): feat = Feat.Weapon_Focus_Short_Sword; break;
                case (BaseItemType.Shuriken): feat = Feat.Weapon_Focus_Shuriken; break;
                case (BaseItemType.Sickle): feat = Feat.Weapon_Focus_Sickle; break;
                case (BaseItemType.Sling): feat = Feat.Weapon_Focus_Sling; break;
                case (BaseItemType.ShortSpear): feat = Feat.Weapon_Focus_Spear; break;
                case (BaseItemType.QuarterStaff): feat = Feat.Weapon_Focus_Staff; break;
                case (BaseItemType.ThrowingAxe): feat = Feat.Weapon_Focus_Throwing_Axe; break;
                case (BaseItemType.Trident): feat = Feat.Weapon_Focus_Trident; break;
                case (BaseItemType.TwoBladedSword): feat = Feat.Weapon_Focus_Two_Bladed_Sword; break;
                case (BaseItemType.Invalid): feat = Feat.Weapon_Focus_Unarmed_Strike; break;
                case (BaseItemType.Warhammer): feat = Feat.Weapon_Focus_War_Hammer; break;
                case (BaseItemType.Whip): feat = Feat.Weapon_Focus_Whip; break;
                case (BaseItemType.Lightsaber): feat = Feat.Weapon_Focus_Long_Sword; break;
                case (BaseItemType.Saberstaff): feat = Feat.Weapon_Focus_Two_Bladed_Sword; break;
                default: return;
            }

            NWNXCreature.AddFeat(creature, feat);
        }


        private void AddSpecializationFeat(NWCreature creature, BaseItemType type)
        {
            Feat feat;

            switch (type)
            {
                case (BaseItemType.BastardSword): feat = Feat.Weapon_Specialization_Bastard_Sword; break;
                case (BaseItemType.BattleAxe): feat = Feat.Weapon_Specialization_Battle_Axe; break;
                case (BaseItemType.Club): feat = Feat.Weapon_Specialization_Club; break;
                case (BaseItemType.Dagger): feat = Feat.Weapon_Specialization_Dagger; break;
                case (BaseItemType.Dart): feat = Feat.Weapon_Specialization_Dart; break;
                case (BaseItemType.DireMace): feat = Feat.Weapon_Specialization_Dire_Mace; break;
                case (BaseItemType.DoubleAxe): feat = Feat.Weapon_Specialization_Double_Axe; break;
                case (BaseItemType.DwarvenWaraxe): feat = Feat.Weapon_Specialization_Dwaxe; break;
                case (BaseItemType.GreatAxe): feat = Feat.Weapon_Specialization_Great_Axe; break;
                case (BaseItemType.GreatSword): feat = Feat.Weapon_Specialization_Great_Sword; break;
                case (BaseItemType.Halberd): feat = Feat.Weapon_Specialization_Halberd; break;
                case (BaseItemType.HandAxe): feat = Feat.Weapon_Specialization_Hand_Axe; break;
                case (BaseItemType.HeavyCrossBow): feat = Feat.Weapon_Specialization_Heavy_Crossbow; break;
                case (BaseItemType.HeavyFlail): feat = Feat.Weapon_Specialization_Heavy_Flail; break;
                case (BaseItemType.Kama): feat = Feat.Weapon_Specialization_Kama; break;
                case (BaseItemType.Katana): feat = Feat.Weapon_Specialization_Katana; break;
                case (BaseItemType.Kukri): feat = Feat.Weapon_Specialization_Kukri; break;
                case (BaseItemType.LightCrossBow): feat = Feat.Weapon_Specialization_Light_Crossbow; break;
                case (BaseItemType.LightFlail): feat = Feat.Weapon_Specialization_Light_Flail; break;
                case (BaseItemType.LightHammer): feat = Feat.Weapon_Specialization_Light_Hammer; break;
                case (BaseItemType.LightMace): feat = Feat.Weapon_Specialization_Light_Mace; break;
                case (BaseItemType.LongBow): feat = Feat.Weapon_Specialization_Longbow; break;
                case (BaseItemType.LongSword): feat = Feat.Weapon_Specialization_Long_Sword; break;
                case (BaseItemType.Morningstar): feat = Feat.Weapon_Specialization_Morning_Star; break;
                case (BaseItemType.Rapier): feat = Feat.Weapon_Specialization_Rapier; break;
                case (BaseItemType.Scimitar): feat = Feat.Weapon_Specialization_Scimitar; break;
                case (BaseItemType.Scythe): feat = Feat.Weapon_Specialization_Scythe; break;
                case (BaseItemType.ShortBow): feat = Feat.Weapon_Specialization_Shortbow; break;
                case (BaseItemType.ShortSword): feat = Feat.Weapon_Specialization_Short_Sword; break;
                case (BaseItemType.Shuriken): feat = Feat.Weapon_Specialization_Shuriken; break;
                case (BaseItemType.Sickle): feat = Feat.Weapon_Specialization_Sickle; break;
                case (BaseItemType.Sling): feat = Feat.Weapon_Specialization_Sling; break;
                case (BaseItemType.ShortSpear): feat = Feat.Weapon_Specialization_Spear; break;
                case (BaseItemType.QuarterStaff): feat = Feat.Weapon_Specialization_Staff; break;
                case (BaseItemType.ThrowingAxe): feat = Feat.Weapon_Specialization_Throwing_Axe; break;
                case (BaseItemType.Trident): feat = Feat.Weapon_Specialization_Trident; break;
                case (BaseItemType.TwoBladedSword): feat = Feat.Weapon_Specialization_Two_Bladed_Sword; break;
                case (BaseItemType.Invalid): feat = Feat.Weapon_Specialization_Unarmed_Strike; break;
                case (BaseItemType.Warhammer): feat = Feat.Weapon_Specialization_War_Hammer; break;
                case (BaseItemType.Whip): feat = Feat.Weapon_Specialization_Whip; break;
                case (BaseItemType.Lightsaber): feat = Feat.Weapon_Specialization_Long_Sword; break;
                case (BaseItemType.Saberstaff): feat = Feat.Weapon_Specialization_Two_Bladed_Sword; break;
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

using NWN;

using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Delayed;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.NWNX;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class ItemService
    {
        public static string GetNameByResref(string resref)
        {
            NWPlaceable tempStorage = (_.GetObjectByTag("TEMP_ITEM_STORAGE"));
            if (!tempStorage.IsValid)
            {
                Console.WriteLine("Could not locate temp item storage object. Create a placeable container in a non-accessible area with the tag TEMP_ITEM_STORAGE.");
                return null;
            }
            NWItem item = (_.CreateItemOnObject(resref, tempStorage.Object));
            string name = item.Name;
            item.Destroy();
            return name;
        }

        public static CustomItemType GetCustomItemTypeByResref(string resref)
        {
            NWPlaceable tempStorage = (_.GetObjectByTag("TEMP_ITEM_STORAGE"));
            if (!tempStorage.IsValid)
            {
                Console.WriteLine("Could not locate temp item storage object. Create a placeable container in a non-accessible area with the tag TEMP_ITEM_STORAGE.");
                return CustomItemType.None;
            }
            NWItem item = (_.CreateItemOnObject(resref, tempStorage.Object));
            var itemType = item.CustomItemType;
            item.Destroy();
            return itemType;
        }

        public static void OnModuleActivatedItem()
        {
            NWPlayer user = (_.GetItemActivator());
            NWItem oItem = (_.GetItemActivated());
            NWObject target = (_.GetItemActivatedTarget());
            Location targetLocation = _.GetItemActivatedTargetLocation();

            string className = oItem.GetLocalString("JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTIVATE_JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("JAVA_ACTION_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) return;

            user.ClearAllActions();

            if (user.IsBusy)
            {
                user.SendMessage("You are busy.");
                return;
            }

            // Remove "Item." prefix if it exists.
            if (className.StartsWith("Item."))
                className = className.Substring(5);

            App.ResolveByInterface<IActionItem>("Item." + className, (item) =>
            {
                string invalidTargetMessage = item.IsValidTarget(user, oItem, target, targetLocation);
                if (!string.IsNullOrWhiteSpace(invalidTargetMessage))
                {
                    user.SendMessage(invalidTargetMessage);
                    return;
                }

                float maxDistance = item.MaxDistance(user, oItem, target, targetLocation);
                if (maxDistance > 0.0f)
                {
                    if (target.IsValid &&
                        (_.GetDistanceBetween(user.Object, target.Object) > maxDistance ||
                        user.Area.Resref != target.Area.Resref))
                    {
                        user.SendMessage("Your target is too far away.");
                        return;
                    }
                    else if (!target.IsValid &&
                             (_.GetDistanceBetweenLocations(user.Location, targetLocation) > maxDistance ||
                             user.Area.Resref != ((NWArea)_.GetAreaFromLocation(targetLocation)).Resref))
                    {
                        user.SendMessage("That location is too far away.");
                        return;
                    }
                }

                CustomData customData = item.StartUseItem(user, oItem, target, targetLocation);
                float delay = item.Seconds(user, oItem, target, targetLocation, customData);
                int animationID = item.AnimationID();
                bool faceTarget = item.FaceTarget();
                Vector userPosition = user.Position;

                user.AssignCommand(() =>
                {
                    user.IsBusy = true;
                    if (faceTarget)
                        _.SetFacingPoint(!target.IsValid ? _.GetPositionFromLocation(targetLocation) : target.Position);
                    if (animationID > 0)
                        _.ActionPlayAnimation(animationID, 1.0f, delay);
                });

                NWNXPlayer.StartGuiTimingBar(user, delay, string.Empty);
                user.DelayEvent<FinishActionItem>(
                    delay,
                    className,
                    user,
                    oItem,
                    target,
                    targetLocation,
                    userPosition,
                    customData);
            });

        }

        public static string OnModuleExamine(string existingDescription, NWPlayer examiner, NWObject examinedObject)
        {
            if (examinedObject.ObjectType != OBJECT_TYPE_ITEM) return existingDescription;

            NWItem examinedItem = (examinedObject.Object);
            string description = "";

            if (examinedItem.RecommendedLevel > 0)
            {
                description += ColorTokenService.Orange("Recommended Level: ") + examinedItem.RecommendedLevel + "\n";
            }
            if (examinedItem.LevelIncrease > 0)
            {
                description += ColorTokenService.Orange("Level Increase: ") + examinedItem.LevelIncrease + "\n";
            }
            if (examinedItem.AssociatedSkillType > 0)
            {
                Skill skill = DataService.Get<Skill>((int)examinedItem.AssociatedSkillType);
                description += ColorTokenService.Orange("Associated Skill: ") + skill.Name + "\n";
            }
            if (examinedItem.CustomAC > 0)
            {
                description += ColorTokenService.Orange("AC: ") + examinedItem.CustomAC + "\n";
            }
            if (examinedItem.HPBonus > 0)
            {
                description += ColorTokenService.Orange("HP Bonus: ") + examinedItem.HPBonus + "\n";
            }
            if (examinedItem.FPBonus > 0)
            {
                description += ColorTokenService.Orange("FP Bonus: ") + examinedItem.FPBonus + "\n";
            }
            if (examinedItem.StructureBonus > 0)
            {
                description += ColorTokenService.Orange("Structure Bonus: ") + examinedItem.StructureBonus + "\n";
            }
            if (examinedItem.StrengthBonus > 0)
            {
                description += ColorTokenService.Orange("Strength Bonus: ") + examinedItem.StrengthBonus + "\n";
            }
            if (examinedItem.DexterityBonus > 0)
            {
                description += ColorTokenService.Orange("Dexterity Bonus: ") + examinedItem.DexterityBonus + "\n";
            }
            if (examinedItem.ConstitutionBonus > 0)
            {
                description += ColorTokenService.Orange("Constitution Bonus: ") + examinedItem.ConstitutionBonus + "\n";
            }
            if (examinedItem.WisdomBonus > 0)
            {
                description += ColorTokenService.Orange("Wisdom Bonus: ") + examinedItem.WisdomBonus + "\n";
            }
            if (examinedItem.IntelligenceBonus > 0)
            {
                description += ColorTokenService.Orange("Intelligence Bonus: ") + examinedItem.IntelligenceBonus + "\n";
            }
            if (examinedItem.CharismaBonus > 0)
            {
                description += ColorTokenService.Orange("Charisma Bonus: ") + examinedItem.CharismaBonus + "\n";
            }
            if (examinedItem.CastingSpeed > 0)
            {
                description += ColorTokenService.Orange("Activation Speed: +") + examinedItem.CastingSpeed + "%\n";
            }
            else if (examinedItem.CastingSpeed < 0)
            {
                description += ColorTokenService.Orange("Activation Penalty: -") + examinedItem.CastingSpeed + "%\n";
            }
            if (examinedItem.HarvestingBonus > 0)
            {
                description += ColorTokenService.Orange("Harvesting Bonus: ") + examinedItem.HarvestingBonus + "\n";
            }
            if (examinedItem.CraftBonusArmorsmith > 0)
            {
                description += ColorTokenService.Orange("Armorsmith Bonus: ") + examinedItem.CraftBonusArmorsmith + "\n";
            }
            if (examinedItem.CraftBonusEngineering > 0)
            {
                description += ColorTokenService.Orange("Engineering Bonus: ") + examinedItem.CraftBonusEngineering + "\n";
            }
            if (examinedItem.CraftBonusFabrication > 0)
            {
                description += ColorTokenService.Orange("Fabrication Bonus: ") + examinedItem.CraftBonusFabrication + "\n";
            }
            if (examinedItem.CraftBonusWeaponsmith > 0)
            {
                description += ColorTokenService.Orange("Weaponsmith Bonus: ") + examinedItem.CraftBonusWeaponsmith + "\n";
            }
            if (examinedItem.CraftBonusCooking > 0)
            {
                description += ColorTokenService.Orange("Cooking Bonus: ") + examinedItem.CraftBonusCooking + "\n";
            }
            if (examinedItem.CraftTierLevel > 0)
            {
                description += ColorTokenService.Orange("Tool Level: ") + examinedItem.CraftTierLevel + "\n";
            }
            if (examinedItem.EnmityRate != 0)
            {
                description += ColorTokenService.Orange("Enmity: ") + examinedItem.EnmityRate + "%\n";
            }
            if (examinedItem.ForcePotencyBonus > 0)
            {
                description += ColorTokenService.Orange("Force Potency Bonus: ") + examinedItem.ForcePotencyBonus + "\n";
            }
            if (examinedItem.ForceAccuracyBonus > 0)
            {
                description += ColorTokenService.Orange("Force Accuracy Bonus: ") + examinedItem.ForceAccuracyBonus + "\n";
            }
            if (examinedItem.ForceDefenseBonus > 0)
            {
                description += ColorTokenService.Orange("Force Defense Bonus: ") + examinedItem.ForceDefenseBonus + "\n";
            }
            if (examinedItem.ElectricalPotencyBonus > 0)
            {
                description += ColorTokenService.Orange("Electrical Potency Bonus: ") + examinedItem.ElectricalPotencyBonus + "\n";
            }
            if (examinedItem.MindPotencyBonus > 0)
            {
                description += ColorTokenService.Orange("Mind Potency Bonus: ") + examinedItem.MindPotencyBonus + "\n";
            }
            if (examinedItem.LightPotencyBonus > 0)
            {
                description += ColorTokenService.Orange("Light Potency Bonus: ") + examinedItem.LightPotencyBonus + "\n";
            }
            if (examinedItem.DarkPotencyBonus > 0)
            {
                description += ColorTokenService.Orange("Dark Potency Bonus: ") + examinedItem.DarkPotencyBonus + "\n";
            }
            if (examinedItem.ElectricalDefenseBonus > 0)
            {
                description += ColorTokenService.Orange("Electrical Defense Bonus: ") + examinedItem.ElectricalDefenseBonus + "\n";
            }
            if (examinedItem.MindDefenseBonus > 0)
            {
                description += ColorTokenService.Orange("Mind Defense Bonus: ") + examinedItem.MindDefenseBonus + "\n";
            }
            if (examinedItem.LightDefenseBonus > 0)
            {
                description += ColorTokenService.Orange("Light Defense Bonus: ") + examinedItem.LightDefenseBonus + "\n";
            }
            if (examinedItem.DarkDefenseBonus > 0)
            {
                description += ColorTokenService.Orange("Dark Defense Bonus: ") + examinedItem.DarkDefenseBonus + "\n";
            }
            if (examinedItem.LuckBonus > 0)
            {
                description += ColorTokenService.Orange("Luck Bonus: ") + examinedItem.LuckBonus + "\n";
            }
            if (examinedItem.MeditateBonus > 0)
            {
                description += ColorTokenService.Orange("Meditate Bonus: ") + examinedItem.MeditateBonus + "\n";
            }
            if (examinedItem.RestBonus > 0)
            {
                description += ColorTokenService.Orange("Rest Bonus: ") + examinedItem.RestBonus + "\n";
            }
            if (examinedItem.ScanningBonus > 0)
            {
                description += ColorTokenService.Orange("Scanning Bonus: ") + examinedItem.ScanningBonus + "\n";
            }
            if (examinedItem.ScavengingBonus > 0)
            {
                description += ColorTokenService.Orange("Scavenging Bonus: ") + examinedItem.ScavengingBonus + "\n";
            }
            if (examinedItem.MedicineBonus > 0)
            {
                description += ColorTokenService.Orange("Medicine Bonus: ") + examinedItem.MedicineBonus + "\n";
            }
            if (examinedItem.HPRegenBonus > 0)
            {
                description += ColorTokenService.Orange("HP Regen Bonus: ") + examinedItem.HPRegenBonus + "\n";
            }
            if (examinedItem.FPRegenBonus > 0)
            {
                description += ColorTokenService.Orange("FP Regen Bonus: ") + examinedItem.FPRegenBonus + "\n";
            }
            if (examinedItem.BaseAttackBonus > 0)
            {
                description += ColorTokenService.Orange("Base Attack Bonus: ") + examinedItem.BaseAttackBonus + "\n";
            }
            if (examinedItem.SneakAttackBonus > 0)
            {
                description += ColorTokenService.Orange("Sneak Attack Bonus: ") + examinedItem.SneakAttackBonus + "\n";
            }
            if (examinedItem.DamageBonus > 0)
            {
                description += ColorTokenService.Orange("Damage Bonus: ") + examinedItem.DamageBonus + "\n";
            }
            if (examinedItem.CustomItemType != CustomItemType.None)
            {
                string itemTypeProper = string.Concat(examinedItem.CustomItemType.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
                description += ColorTokenService.Orange("Item Type: ") + itemTypeProper + "\n";
            }

            return existingDescription + "\n" + description;
        }


        public static HashSet<int> ArmorBaseItemTypes = new HashSet<int>()
        {
            BASE_ITEM_AMULET,
            BASE_ITEM_ARMOR,
            BASE_ITEM_BRACER,
            BASE_ITEM_BELT,
            BASE_ITEM_BOOTS,
            BASE_ITEM_CLOAK,
            BASE_ITEM_GLOVES,
            BASE_ITEM_HELMET,
            BASE_ITEM_LARGESHIELD,
            BASE_ITEM_SMALLSHIELD,
            BASE_ITEM_TOWERSHIELD
        };

        public static HashSet<int> WeaponBaseItemTypes = new HashSet<int>()
        {
            BASE_ITEM_ARROW,
            BASE_ITEM_BASTARDSWORD,
            BASE_ITEM_BATTLEAXE,
            BASE_ITEM_BOLT,
            BASE_ITEM_BRACER,
            BASE_ITEM_BULLET,
            BASE_ITEM_CLUB,
            BASE_ITEM_DAGGER,
            BASE_ITEM_DART,
            BASE_ITEM_DIREMACE,
            BASE_ITEM_DOUBLEAXE,
            BASE_ITEM_DWARVENWARAXE,
            BASE_ITEM_GLOVES,
            BASE_ITEM_GREATAXE,
            BASE_ITEM_GREATSWORD,
            BASE_ITEM_GRENADE,
            BASE_ITEM_HALBERD,
            BASE_ITEM_HANDAXE,
            BASE_ITEM_HEAVYCROSSBOW,
            BASE_ITEM_HEAVYFLAIL,
            BASE_ITEM_KAMA,
            BASE_ITEM_KATANA,
            BASE_ITEM_KUKRI,
            BASE_ITEM_LIGHTCROSSBOW,
            BASE_ITEM_LIGHTFLAIL,
            BASE_ITEM_LIGHTHAMMER,
            BASE_ITEM_LIGHTMACE,
            BASE_ITEM_LONGBOW,
            BASE_ITEM_LONGSWORD,
            BASE_ITEM_MORNINGSTAR,
            BASE_ITEM_QUARTERSTAFF,
            BASE_ITEM_RAPIER,
            BASE_ITEM_SCIMITAR,
            BASE_ITEM_SCYTHE,
            BASE_ITEM_SHORTBOW,
            BASE_ITEM_SHORTSPEAR,
            BASE_ITEM_SHORTSWORD,
            BASE_ITEM_SHURIKEN,
            BASE_ITEM_SICKLE,
            BASE_ITEM_SLING,
            BASE_ITEM_THROWINGAXE,
            BASE_ITEM_TRIDENT,
            BASE_ITEM_TWOBLADEDSWORD,
            BASE_ITEM_WARHAMMER,
            BASE_ITEM_WHIP,
            CustomBaseItemType.Saberstaff,
            CustomBaseItemType.Lightsaber
        };

        public static void OnModuleUnequipItem()
        {
            NWPlayer player = _.GetPCItemLastUnequippedBy();
            NWItem oItem = _.GetPCItemLastUnequipped();

            // Remove lightsaber hum effect.
            foreach (var effect in player.Effects.Where(x=> _.GetEffectTag(x) == "LIGHTSABER_HUM"))
            {
                _.RemoveEffect(player, effect);
            }

            // Handle lightsaber sounds
            if (oItem.CustomItemType == CustomItemType.Lightsaber ||
                oItem.CustomItemType == CustomItemType.Saberstaff)
            {
  
                player.AssignCommand(() =>
                {
                    _.PlaySound("saberoff");
                });
            }

        }

        public static void OnModuleEquipItem()
        {
            using (new Profiler("ItemService::OnModuleEquipItem()"))
            {

                int[] validItemTypes = {
                    BASE_ITEM_ARMOR,
                    BASE_ITEM_ARROW,
                    BASE_ITEM_BASTARDSWORD,
                    BASE_ITEM_BATTLEAXE,
                    BASE_ITEM_BELT,
                    BASE_ITEM_BOLT,
                    BASE_ITEM_BOOTS,
                    BASE_ITEM_BRACER,
                    BASE_ITEM_BULLET,
                    BASE_ITEM_CLOAK,
                    BASE_ITEM_CLUB,
                    BASE_ITEM_DAGGER,
                    BASE_ITEM_DART,
                    BASE_ITEM_DIREMACE,
                    BASE_ITEM_DOUBLEAXE,
                    BASE_ITEM_DWARVENWARAXE,
                    BASE_ITEM_GLOVES,
                    BASE_ITEM_GREATAXE,
                    BASE_ITEM_GREATSWORD,
                    BASE_ITEM_GRENADE,
                    BASE_ITEM_HALBERD,
                    BASE_ITEM_HANDAXE,
                    BASE_ITEM_HEAVYCROSSBOW,
                    BASE_ITEM_HEAVYFLAIL,
                    BASE_ITEM_HELMET,
                    BASE_ITEM_KAMA,
                    BASE_ITEM_KATANA,
                    BASE_ITEM_KUKRI,
                    BASE_ITEM_LARGESHIELD,
                    BASE_ITEM_LIGHTCROSSBOW,
                    BASE_ITEM_LIGHTFLAIL,
                    BASE_ITEM_LIGHTHAMMER,
                    BASE_ITEM_LIGHTMACE,
                    BASE_ITEM_LONGBOW,
                    BASE_ITEM_LONGSWORD,
                    BASE_ITEM_MORNINGSTAR,
                    BASE_ITEM_QUARTERSTAFF,
                    BASE_ITEM_RAPIER,
                    BASE_ITEM_SCIMITAR,
                    BASE_ITEM_SCYTHE,
                    BASE_ITEM_SHORTBOW,
                    BASE_ITEM_SHORTSPEAR,
                    BASE_ITEM_SHORTSWORD,
                    BASE_ITEM_SHURIKEN,
                    BASE_ITEM_SICKLE,
                    BASE_ITEM_SLING,
                    BASE_ITEM_SMALLSHIELD,
                    BASE_ITEM_THROWINGAXE,
                    BASE_ITEM_TOWERSHIELD,
                    BASE_ITEM_TRIDENT,
                    BASE_ITEM_TWOBLADEDSWORD,
                    BASE_ITEM_WARHAMMER,
                    BASE_ITEM_WHIP,
                    CustomBaseItemType.Saberstaff,
                    CustomBaseItemType.Lightsaber

            };

                NWPlayer player = _.GetPCItemLastEquippedBy();
                NWItem oItem = (_.GetPCItemLastEquipped());
                int baseItemType = oItem.BaseItemType;
                Effect eEffect = _.EffectVisualEffect(579);
                eEffect = _.TagEffect(eEffect, "LIGHTSABER_HUM");

                // Handle lightsaber sounds
                if (oItem.CustomItemType == CustomItemType.Lightsaber ||
                    oItem.CustomItemType == CustomItemType.Saberstaff)
                {
                    _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, eEffect, player);
                    player.AssignCommand(() =>  
                    {   
                        _.PlaySound("saberon");                 
                    });
                }

                if (!validItemTypes.Contains(baseItemType)) return;

                AddOnHitProperty(oItem);

                // Check ammo every time
                if (player.Arrows.IsValid)
                {
                    AddOnHitProperty(player.Arrows);
                    player.Arrows.RecommendedLevel = oItem.RecommendedLevel;
                }

                if (player.Bolts.IsValid)
                {
                    AddOnHitProperty(player.Bolts);
                    player.Bolts.RecommendedLevel = oItem.RecommendedLevel;
                }

                if (player.Bullets.IsValid)
                {
                    AddOnHitProperty(player.Bullets);
                    player.Bullets.RecommendedLevel = oItem.RecommendedLevel;
                }


                if (baseItemType == BASE_ITEM_TORCH)
                {
                    int charges = oItem.ReduceCharges();
                    if (charges <= 0)
                    {
                        oItem.Destroy();
                    }
                }
            }
        }

        private static void AddOnHitProperty(NWItem oItem)
        {
            foreach (ItemProperty ip in oItem.ItemProperties)
            {
                if (_.GetItemPropertyType(ip) == ITEM_PROPERTY_ONHITCASTSPELL)
                {
                    if (_.GetItemPropertySubType(ip) == IP_CONST_ONHIT_CASTSPELL_ONHIT_UNIQUEPOWER)
                    {
                        return;
                    }
                }
            }

            // No item property found. Add it to the item.
            BiowareXP2.IPSafeAddItemProperty(oItem, _.ItemPropertyOnHitCastSpell(IP_CONST_ONHIT_CASTSPELL_ONHIT_UNIQUEPOWER, 40), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
        }

        public static void ReturnItem(NWObject target, NWItem item)
        {
            if (_.GetHasInventory(item) == TRUE)
            {
                NWObject possessor = item.Possessor;
                possessor.AssignCommand(() =>
                {
                    _.ActionGiveItem(item, target);
                });
            }
            else
            {
                _.CopyItem(item.Object, target.Object, TRUE);
                item.Destroy();
            }
        }

        public static void StripAllItemProperties(NWItem item)
        {
            foreach (var ip in item.ItemProperties)
            {
                _.RemoveItemProperty(item.Object, ip);
            }
        }

        public static void FinishActionItem(IActionItem actionItem, NWPlayer user, NWItem item, NWObject target, Location targetLocation, Vector userStartPosition, CustomData customData)
        {
            user.IsBusy = false;

            Vector userPosition = user.Position;
            if (userPosition.m_X != userStartPosition.m_X ||
                userPosition.m_Y != userStartPosition.m_Y ||
                userPosition.m_Z != userStartPosition.m_Z)
            {
                user.SendMessage("You move and interrupt your action.");
                return;
            }

            float maxDistance = actionItem.MaxDistance(user, item, target, targetLocation);
            if (maxDistance > 0.0f)
            {
                if (target.IsValid &&
                    (_.GetDistanceBetween(user.Object, target.Object) > maxDistance ||
                    user.Area.Resref != target.Area.Resref))
                {
                    user.SendMessage("Your target is too far away.");
                    return;
                }
                else if (!target.IsValid &&
                         (_.GetDistanceBetweenLocations(user.Location, targetLocation) > maxDistance ||
                         user.Area.Resref != ((NWArea)_.GetAreaFromLocation(targetLocation)).Resref))
                {
                    user.SendMessage("That location is too far away.");
                    return;
                }
            }

            if (!target.IsValid && !actionItem.AllowLocationTarget())
            {
                user.SendMessage("Unable to locate target.");
                return;
            }

            string invalidTargetMessage = actionItem.IsValidTarget(user, item, target, targetLocation);
            if (!string.IsNullOrWhiteSpace(invalidTargetMessage))
            {
                user.SendMessage(invalidTargetMessage);
                return;
            }

            actionItem.ApplyEffects(user, item, target, targetLocation, customData);

            if (actionItem.ReducesItemCharge(user, item, target, targetLocation, customData))
            {
                if (item.Charges > 0) item.ReduceCharges();
                else item.Destroy();
            }
        }

        public static ItemProperty GetCustomItemPropertyByItemTag(string tag)
        {
            NWPlaceable container = (_.GetObjectByTag("item_props"));
            NWItem item = container.InventoryItems.SingleOrDefault(x => x.Tag == tag);
            if (item == null)
            {
                throw new Exception("Unable to find an item tagged '" + tag + "' in the item props container.");
            }

            ItemProperty prop = item.ItemProperties.FirstOrDefault();
            if (prop == null)
            {
                throw new Exception("Unable to find an item property on item tagged '" + tag + "' in the item props container.");
            }

            return prop;
        }

        public static HashSet<int> MeleeWeaponTypes = new HashSet<int>()
        {
            BASE_ITEM_BASTARDSWORD,
            BASE_ITEM_BATTLEAXE,
            BASE_ITEM_CLUB,
            BASE_ITEM_DAGGER,
            BASE_ITEM_HANDAXE,
            BASE_ITEM_KAMA,
            BASE_ITEM_KATANA,
            BASE_ITEM_KUKRI,
            BASE_ITEM_LIGHTFLAIL,
            BASE_ITEM_LIGHTHAMMER,
            BASE_ITEM_LIGHTMACE,
            BASE_ITEM_LONGSWORD,
            BASE_ITEM_RAPIER,
            BASE_ITEM_SCIMITAR,
            BASE_ITEM_SHORTSPEAR,
            BASE_ITEM_SHORTSWORD,
            BASE_ITEM_SICKLE,
            BASE_ITEM_WHIP,
            CustomBaseItemType.Lightsaber,
            BASE_ITEM_DIREMACE,
            BASE_ITEM_DWARVENWARAXE,
            BASE_ITEM_GREATAXE,
            BASE_ITEM_GREATSWORD,
            BASE_ITEM_HALBERD,
            BASE_ITEM_HEAVYFLAIL,
            BASE_ITEM_MORNINGSTAR,
            BASE_ITEM_QUARTERSTAFF,
            BASE_ITEM_SCYTHE,
            BASE_ITEM_TRIDENT,
            BASE_ITEM_WARHAMMER,
            BASE_ITEM_DOUBLEAXE,
            BASE_ITEM_TWOBLADEDSWORD,
            CustomBaseItemType.Saberstaff,
            BASE_ITEM_BRACER,
            BASE_ITEM_GLOVES

        };

        public static HashSet<int> RangedWeaponTypes = new HashSet<int>()
        {
            BASE_ITEM_HEAVYCROSSBOW,
            BASE_ITEM_LIGHTCROSSBOW,
            BASE_ITEM_LONGBOW,
            BASE_ITEM_SHORTBOW,
            BASE_ITEM_ARROW,
            BASE_ITEM_BOLT,
            BASE_ITEM_GRENADE,
            BASE_ITEM_SHURIKEN,
            BASE_ITEM_SLING,
            BASE_ITEM_THROWINGAXE,
            BASE_ITEM_BULLET,
            BASE_ITEM_DART
        };
        
        private static readonly Dictionary<int, SkillType> _skillTypeMappings = new Dictionary<int, SkillType>()
        {
            // One-Handed Skills
            {BASE_ITEM_BASTARDSWORD, SkillType.OneHanded},
            {BASE_ITEM_BATTLEAXE, SkillType.OneHanded},
            {BASE_ITEM_CLUB, SkillType.OneHanded},
            {BASE_ITEM_DAGGER, SkillType.OneHanded},
            {BASE_ITEM_HANDAXE, SkillType.OneHanded},
            {BASE_ITEM_KAMA, SkillType.OneHanded},
            {BASE_ITEM_KATANA, SkillType.OneHanded},
            {BASE_ITEM_KUKRI, SkillType.OneHanded},
            {BASE_ITEM_LIGHTFLAIL, SkillType.OneHanded},
            {BASE_ITEM_LIGHTHAMMER, SkillType.OneHanded},
            {BASE_ITEM_LIGHTMACE, SkillType.OneHanded},
            {BASE_ITEM_LONGSWORD, SkillType.OneHanded},
            {BASE_ITEM_MORNINGSTAR, SkillType.OneHanded},
            {BASE_ITEM_RAPIER, SkillType.OneHanded},
            {BASE_ITEM_SCIMITAR, SkillType.OneHanded},
            {BASE_ITEM_SHORTSWORD, SkillType.OneHanded},
            {BASE_ITEM_SICKLE, SkillType.OneHanded},
            {BASE_ITEM_WHIP, SkillType.OneHanded},
            // Two-Handed Skills
            {BASE_ITEM_DIREMACE, SkillType.TwoHanded}     ,
            {BASE_ITEM_DWARVENWARAXE, SkillType.TwoHanded},
            {BASE_ITEM_GREATAXE, SkillType.TwoHanded}     ,
            {BASE_ITEM_GREATSWORD, SkillType.TwoHanded}   ,
            {BASE_ITEM_HALBERD, SkillType.TwoHanded}      ,
            {BASE_ITEM_HEAVYFLAIL, SkillType.TwoHanded}   ,
            {BASE_ITEM_SCYTHE, SkillType.TwoHanded}       ,
            {BASE_ITEM_TRIDENT, SkillType.TwoHanded}      ,
            {BASE_ITEM_WARHAMMER, SkillType.TwoHanded}    ,
            {BASE_ITEM_SHORTSPEAR, SkillType.TwoHanded}   ,
            // Twin Blades Skills
            {BASE_ITEM_TWOBLADEDSWORD, SkillType.TwinBlades },
            {BASE_ITEM_DOUBLEAXE, SkillType.TwinBlades },
            // Martial Arts Skills
            {BASE_ITEM_BRACER, SkillType.MartialArts},
            {BASE_ITEM_GLOVES, SkillType.MartialArts},
            {BASE_ITEM_QUARTERSTAFF, SkillType.MartialArts},
            {BASE_ITEM_HEAVYCROSSBOW, SkillType.Firearms},
            {BASE_ITEM_LIGHTCROSSBOW, SkillType.Firearms},
            // Firearms Skills
            {BASE_ITEM_LONGBOW, SkillType.Firearms},
            {BASE_ITEM_SHORTBOW, SkillType.Firearms},
            {BASE_ITEM_ARROW, SkillType.Firearms},
            {BASE_ITEM_BOLT, SkillType.Firearms},
            // Throwing Skills
            {BASE_ITEM_GRENADE, SkillType.Throwing},
            {BASE_ITEM_SHURIKEN, SkillType.Throwing},
            {BASE_ITEM_SLING, SkillType.Throwing},
            {BASE_ITEM_THROWINGAXE, SkillType.Throwing},
            {BASE_ITEM_BULLET, SkillType.Throwing},
            {BASE_ITEM_DART, SkillType.Throwing},
            // Shield Skills
            {BASE_ITEM_SMALLSHIELD, SkillType.Shields },
            {BASE_ITEM_LARGESHIELD, SkillType.Shields },
            {BASE_ITEM_TOWERSHIELD, SkillType.Shields },
            // Lightsabers
            {CustomBaseItemType.Lightsaber, SkillType.Lightsaber},
            {CustomBaseItemType.Saberstaff, SkillType.Lightsaber}
        };

        public static SkillType GetSkillTypeForItem(NWItem item)
        {
            using (new Profiler("ItemService::GetSkillTypeForItem"))
            {
                int type = item.BaseItemType;

                // Check for explicit override.
                if (item.AssociatedSkillType > 0) return (SkillType)item.AssociatedSkillType;

                // Armor has to specifically be set on the item in order to count.
                // Look for an item type property first.
                if (item.CustomItemType == CustomItemType.LightArmor) return SkillType.LightArmor;
                else if (item.CustomItemType == CustomItemType.HeavyArmor) return SkillType.HeavyArmor;
                else if (item.CustomItemType == CustomItemType.ForceArmor) return SkillType.ForceArmor;
                
                // Training lightsabers are katana weapons with special local variables.
                if (item.GetLocalInt("LIGHTSABER") == TRUE)
                {
                    return SkillType.Lightsaber;
                }
                
                if (!_skillTypeMappings.TryGetValue(type, out var result))
                {
                    return SkillType.Unknown;
                }
                return result;
            }
        }
    }
}

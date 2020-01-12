using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Event.Feat;
using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.Event.Legacy;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using static SWLOR.Game.Server.NWScript._;
using AddItemPropertyPolicy = SWLOR.Game.Server.Enumeration.AddItemPropertyPolicy;
using BaseItemType = SWLOR.Game.Server.NWScript.Enumerations.BaseItemType;

namespace SWLOR.Game.Server.Service
{
    public static class ItemService
    {
        private static readonly Dictionary<string, IActionItem> _actionItemHandlers;

        static ItemService()
        {
            _actionItemHandlers = new Dictionary<string, IActionItem>();
        }

        public static void SubscribeEvents()
        {
            // Module Events
            MessageHub.Instance.Subscribe<OnItemUsed>(message => OnItemUsed());
            MessageHub.Instance.Subscribe<OnModuleEquipItem>(message => OnModuleEquipItem());
            MessageHub.Instance.Subscribe<OnModuleUnequipItem>(message => OnModuleUnequipItem());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());

            // Feat Events
            MessageHub.Instance.Subscribe<OnHitCastSpell>(message => OnHitCastSpell());
        }

        private static void OnModuleLoad()
        {
            Console.WriteLine("ItemService -> OnModuleLoad");
            RegisterActionItemHandlers();
            Console.WriteLine("ItemService -> OnModuleLoad Complete");
        }

        private static void RegisterActionItemHandlers()
        {
            // Use reflection to get all of IChatCommand handler implementations.
            var classes = Assembly.GetCallingAssembly().GetTypes()
                .Where(p => typeof(IActionItem).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();

            foreach (var type in classes)
            {
                IActionItem instance = Activator.CreateInstance(type) as IActionItem;

                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                string key = type.Name;

                // If the class has defined a custom key, use that instead.
                if (!string.IsNullOrWhiteSpace(instance.CustomKey))
                    key = instance.CustomKey;

                _actionItemHandlers.Add(key, instance);
            }
        }

        public static IActionItem GetActionItemHandler(string key)
        {
            if (!_actionItemHandlers.ContainsKey(key))
            {
                throw new KeyNotFoundException("Action item '" + key + "' is not registered.");
            }

            return _actionItemHandlers[key];
        }

        public static string GetNameByResref(string resref)
        {
            NWPlaceable tempStorage = (GetObjectByTag("TEMP_ITEM_STORAGE"));
            if (!tempStorage.IsValid)
            {
                Console.WriteLine("Could not locate temp item storage object. Create a placeable container in a non-accessible area with the tag TEMP_ITEM_STORAGE.");
                return null;
            }
            NWItem item = (CreateItemOnObject(resref, tempStorage.Object));
            string name = item.Name;
            item.Destroy();
            return name;
        }

        public static CustomItemType GetCustomItemTypeByResref(string resref)
        {
            NWPlaceable tempStorage = (GetObjectByTag("TEMP_ITEM_STORAGE"));
            if (!tempStorage.IsValid)
            {
                Console.WriteLine("Could not locate temp item storage object. Create a placeable container in a non-accessible area with the tag TEMP_ITEM_STORAGE.");
                return CustomItemType.None;
            }
            NWItem item = (CreateItemOnObject(resref, tempStorage.Object));
            var itemType = item.CustomItemType;
            item.Destroy();
            return itemType;
        }

        private static void OnItemUsed()
        {
            NWPlayer user = NWGameObject.OBJECT_SELF;
            NWItem oItem = NWNXEvents.OnItemUsed_GetItem();
            NWObject target = NWNXEvents.OnItemUsed_GetTarget();
            Location targetLocation = NWNXEvents.OnItemUsed_GetTargetLocation();

            string className = oItem.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTIVATE_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTION_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("SCRIPT");
            // Legacy events follow. We can't remove these because of backwards compatibility issues with existing items.
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTIVATE_JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("JAVA_ACTION_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) return;

            // Bypass the NWN "item use" animation.
            NWNXEvents.SkipEvent();

            user.ClearAllActions();

            if (user.IsBusy)
            {
                user.SendMessage("You are busy.");
                return;
            }

            // Remove "Item." prefix if it exists.
            if (className.StartsWith("Item."))
                className = className.Substring(5);
            IActionItem item = GetActionItemHandler(className);

            string invalidTargetMessage = item.IsValidTarget(user, oItem, target, targetLocation);
            if (!string.IsNullOrWhiteSpace(invalidTargetMessage))
            {
                user.SendMessage(invalidTargetMessage);
                return;
            }

            // NOTE - these checks are duplicated in FinishActionItem.  Keep both in sync.
            float maxDistance = item.MaxDistance(user, oItem, target, targetLocation);
            if (maxDistance > 0.0f)
            {
                NWObject owner = GetItemPossessor(target);

                if (target.IsValid && owner.IsValid)
                {
                    // We are okay - we have targeted an item in our inventory (we can't target someone
                    // else's inventory, so no need to actually check distance).
                }
                else if (target.Object == NWGameObject.OBJECT_SELF)
                {
                    // Also okay.
                }
                else if (target.IsValid && 
                         (GetDistanceBetween(user.Object, target.Object) > maxDistance ||
                          user.Area.Resref != target.Area.Resref))
                {
                    user.SendMessage("Your target is too far away.");
                    return;
                }
                else if (!target.IsValid &&
                         (GetDistanceBetweenLocations(user.Location, targetLocation) > maxDistance ||
                         user.Area.Resref != ((NWArea)GetAreaFromLocation(targetLocation)).Resref))
                {
                    user.SendMessage("That location is too far away.");
                    return;
                }
            }

            CustomData customData = item.StartUseItem(user, oItem, target, targetLocation);
            float delay = item.Seconds(user, oItem, target, targetLocation, customData);
            var animationID = item.AnimationType();
            bool faceTarget = item.FaceTarget();
            Vector userPosition = user.Position;

            user.AssignCommand(() =>
            {
                user.IsBusy = true;
                if (faceTarget)
                    SetFacingPoint(!target.IsValid ? GetPositionFromLocation(targetLocation) : target.Position);
                if (animationID > 0)
                    ActionPlayAnimation(animationID, 1.0f, delay);
            });

            if(delay > 0.0f)
            {
                NWNXPlayer.StartGuiTimingBar(user, delay, string.Empty);
            }

            var @event = new OnFinishActionItem(className, user, oItem, target, targetLocation, userPosition, customData);
            user.DelayEvent(delay, @event);
        }

        public static string OnModuleExamine(string existingDescription, NWObject examinedObject)
        {
            if (examinedObject.ObjectType != ObjectType.Item) return existingDescription;

            NWItem examinedItem = (examinedObject.Object);
            string description = "";

            if (examinedItem.RecommendedLevel > 0)
            {
                description += ColorTokenService.Orange("Recommended Level: ") + examinedItem.RecommendedLevel;

                if (examinedItem.BaseItemType == BaseItemType.Ring || examinedItem.BaseItemType == BaseItemType.Amulet)
                    description += " (Uses your highest armor skill)";

                description += "\n";
            }
            if (examinedItem.LevelIncrease > 0)
            {
                description += ColorTokenService.Orange("Level Increase: ") + examinedItem.LevelIncrease + "\n";
            }
            if (examinedItem.AssociatedSkill > 0)
            {
                var skill = SkillService.GetSkill(examinedItem.AssociatedSkill);
                description += ColorTokenService.Orange("Associated Skill: ") + skill.Name + "\n";
            }
            if (examinedItem.CustomAC > 0)
            {
                if (ShieldBaseItemTypes.Contains(examinedItem.BaseItemType))
                {
                    description += ColorTokenService.Orange("Damage Immunity: " ) + (10 + examinedItem.CustomAC / 3) + "\n";
                }
                else if (ArmorBaseItemTypes.Contains(examinedItem.BaseItemType))
                {
                    description += ColorTokenService.Orange("AC: ") + examinedItem.CustomAC + "\n";
                }
                else
                {
                    description += ColorTokenService.Red("AC (ignored due to item type): ") + examinedItem.CustomAC + "\n";
                }
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
            if (examinedItem.CooldownRecovery > 0)
            {
                description += ColorTokenService.Orange("Cooldown Recovery: +") + examinedItem.CooldownRecovery + "%\n";
            }
            else if (examinedItem.CooldownRecovery < 0)
            {
                description += ColorTokenService.Orange("Cooldown Recovery: -") + examinedItem.CooldownRecovery + "%\n";
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
            if (examinedItem.PilotingBonus > 0)
            {
                description += ColorTokenService.Orange("Piloting Bonus: ") + examinedItem.PilotingBonus + "\n";
            }
            if (examinedItem.BaseAttackBonus > 0)
            {
                if (WeaponBaseItemTypes.Contains(examinedItem.BaseItemType))
                {
                    description += ColorTokenService.Orange("Base Attack Bonus: ") + examinedItem.BaseAttackBonus + "\n";
                }
                else
                {
                    description += ColorTokenService.Red("Base Attack Bonus (ignored due to item type): ") + examinedItem.BaseAttackBonus + "\n";
                }
            }
            if (examinedItem.SneakAttackBonus > 0)
            {
                description += ColorTokenService.Orange("Sneak Attack Bonus: ") + examinedItem.SneakAttackBonus + "\n";
            }
            if (examinedItem.DamageBonus > 0)
            {
                if (WeaponBaseItemTypes.Contains(examinedItem.BaseItemType))
                {
                    description += ColorTokenService.Orange("Damage Bonus: ") + examinedItem.DamageBonus + "\n";
                }
                else
                {
                    description += ColorTokenService.Red("Damage Bonus (ignored due to item type): ") + examinedItem.DamageBonus + "\n";
                }
            }
            if (examinedItem.CustomItemType != CustomItemType.None)
            {
                string itemTypeProper = string.Concat(examinedItem.CustomItemType.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
                description += ColorTokenService.Orange("Item Type: ") + itemTypeProper + "\n";
            }

            // Check for properties that can only be applied to limited things, and flag them here.
            // Attack bonus, damage, base attack bonus: weapons only
            // AC - armor items only.
            ItemProperty ip = GetFirstItemProperty(examinedItem);
            while (GetIsItemPropertyValid(ip) == true)
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.ComponentBonus)
                {
                    switch (GetItemPropertySubType(ip))
                    {
                        case (int)ComponentBonusType.ACUp:
                        {
                            description += ColorTokenService.Cyan("AC can only be applied to Shields, Armor and Helmets.  On other items, it will be ignored.\n");
                            break;
                        }
                        case (int)ComponentBonusType.DamageUp:
                        case (int)ComponentBonusType.AttackBonusUp:
                        case (int)ComponentBonusType.BaseAttackBonusUp:
                        {
                            description += ColorTokenService.Cyan("Damage Up, Attack Bonus Up and Base Attack Bonus Up can only be applied to weapons (including gloves).  On other items, it will be ignored.\n");
                            break;
                        }
                    }
                }

                ip = GetNextItemProperty(examinedItem);
            }

            return existingDescription + "\n" + description;
        }

        public static HashSet<BaseItemType> ArmorBaseItemTypes = new HashSet<BaseItemType>()
        {
            BaseItemType.Armor,
            BaseItemType.Helmet
        };

        public static HashSet<BaseItemType> ShieldBaseItemTypes = new HashSet<BaseItemType>()
        {
            BaseItemType.LargeShield,
            BaseItemType.SmallShield,
            BaseItemType.TowerShield
        };

        public static HashSet<BaseItemType> WeaponBaseItemTypes = new HashSet<BaseItemType>()
        {
            BaseItemType.Arrow,
            BaseItemType.BastardSword,
            BaseItemType.BattleAxe,
            BaseItemType.Bolt,
            BaseItemType.Bracer,
            BaseItemType.Bullet,
            BaseItemType.Club,
            BaseItemType.Dagger,
            BaseItemType.Dart,
            BaseItemType.DireMace,
            BaseItemType.DoubleAxe,
            BaseItemType.DwarvenWaraxe,
            BaseItemType.Gloves,
            BaseItemType.GreatAxe,
            BaseItemType.GreatSword,
            BaseItemType.Grenade,
            BaseItemType.Halberd,
            BaseItemType.HandAxe,
            BaseItemType.HeavyCrossBow,
            BaseItemType.HeavyFlail,
            BaseItemType.Kama,
            BaseItemType.Katana,
            BaseItemType.Kukri,
            BaseItemType.LightCrossBow,
            BaseItemType.LightFlail,
            BaseItemType.LightHammer,
            BaseItemType.LightMace,
            BaseItemType.LongBow,
            BaseItemType.LongSword,
            BaseItemType.Morningstar,
            BaseItemType.QuarterStaff,
            BaseItemType.Rapier,
            BaseItemType.Scimitar,
            BaseItemType.Scythe,
            BaseItemType.ShortBow,
            BaseItemType.ShortSpear,
            BaseItemType.ShortSword,
            BaseItemType.Shuriken,
            BaseItemType.Sickle,
            BaseItemType.Sling,
            BaseItemType.ThrowingAxe,
            BaseItemType.Trident,
            BaseItemType.TwoBladedSword,
            BaseItemType.Warhammer,
            BaseItemType.Whip,
            BaseItemType.Saberstaff,
            BaseItemType.Lightsaber
        };

        private static void OnModuleUnequipItem()
        {
            NWPlayer player = GetPCItemLastUnequippedBy();
            if (player.GetLocalBoolean("IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.

            NWItem oItem = GetPCItemLastUnequipped();

            // Remove lightsaber hum effect.
            foreach (var effect in player.Effects.Where(x => GetEffectTag(x) == "LIGHTSABER_HUM"))
            {
                RemoveEffect(player, effect);
            }

            // Handle lightsaber sounds
            if (oItem.CustomItemType == CustomItemType.Lightsaber ||
                oItem.CustomItemType == CustomItemType.Saberstaff)
            {

                player.AssignCommand(() =>
                {
                    PlaySound("saberoff");
                });
            }

        }


        // Players abuse an exploit in NWN which allows them to gain an extra attack.
        // To work around this I force them to clear all actions.
        private static void HandleEquipmentSwappingDelay()
        {
            NWPlayer oPC = (GetPCItemLastEquippedBy());
            NWItem oItem = (GetPCItemLastEquipped());
            NWItem rightHand = oPC.RightHand;
            NWItem leftHand = oPC.LeftHand;

            if (!oPC.IsInCombat) return;
            if (Equals(oItem, rightHand) && Equals(oItem, leftHand)) return;
            if (!Equals(oItem, leftHand)) return;

            oPC.ClearAllActions();
        }

        private static void OnModuleEquipItem()
        {
            BaseItemType[] validItemTypes = {
                    BaseItemType.Armor,
                    BaseItemType.Arrow,
                    BaseItemType.BastardSword,
                    BaseItemType.BattleAxe,
                    BaseItemType.Belt,
                    BaseItemType.Bolt,
                    BaseItemType.Boots,
                    BaseItemType.Bracer,
                    BaseItemType.Bullet,
                    BaseItemType.Cloak,
                    BaseItemType.Club,
                    BaseItemType.Dagger,
                    BaseItemType.Dart,
                    BaseItemType.DireMace,
                    BaseItemType.DoubleAxe,
                    BaseItemType.DwarvenWaraxe,
                    BaseItemType.Gloves,
                    BaseItemType.GreatAxe,
                    BaseItemType.GreatSword,
                    BaseItemType.Grenade,
                    BaseItemType.Halberd,
                    BaseItemType.HandAxe,
                    BaseItemType.HeavyCrossBow,
                    BaseItemType.HeavyFlail,
                    BaseItemType.Helmet,
                    BaseItemType.Kama,
                    BaseItemType.Katana,
                    BaseItemType.Kukri,
                    BaseItemType.LargeShield,
                    BaseItemType.LightCrossBow,
                    BaseItemType.LightFlail,
                    BaseItemType.LightHammer,
                    BaseItemType.LightMace,
                    BaseItemType.LongBow,
                    BaseItemType.LongSword,
                    BaseItemType.Morningstar,
                    BaseItemType.QuarterStaff,
                    BaseItemType.Rapier,
                    BaseItemType.Scimitar,
                    BaseItemType.Scythe,
                    BaseItemType.ShortBow,
                    BaseItemType.ShortSpear,
                    BaseItemType.ShortSword,
                    BaseItemType.Shuriken,
                    BaseItemType.Sickle,
                    BaseItemType.Sling,
                    BaseItemType.SmallShield,
                    BaseItemType.ThrowingAxe,
                    BaseItemType.TowerShield,
                    BaseItemType.Trident,
                    BaseItemType.TwoBladedSword,
                    BaseItemType.Warhammer,
                    BaseItemType.Whip,
                    BaseItemType.Saberstaff,
                    BaseItemType.Lightsaber

            };

            NWPlayer player = GetPCItemLastEquippedBy();

            if (player.GetLocalBoolean("IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.

            NWItem oItem = (GetPCItemLastEquipped());
            var baseItemType = oItem.BaseItemType;
            Effect eEffect = EffectVisualEffect(Vfx.LightsaberHum);
            eEffect = TagEffect(eEffect, "LIGHTSABER_HUM");

            // Handle lightsaber sounds
            if (oItem.CustomItemType == CustomItemType.Lightsaber ||
                oItem.CustomItemType == CustomItemType.Saberstaff)
            {
                ApplyEffectToObject(DurationType.Permanent, eEffect, player);
                player.AssignCommand(() =>
                {
                    PlaySound("saberon");
                });
            }

            if (!validItemTypes.Contains(baseItemType))
            {
                HandleEquipmentSwappingDelay();
                return;
            }

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


            if (baseItemType == BaseItemType.Torch)
            {
                int charges = oItem.ReduceCharges();
                if (charges <= 0)
                {
                    oItem.Destroy();
                }
            }

            HandleEquipmentSwappingDelay();
        }

        private static void AddOnHitProperty(NWItem oItem)
        {
            foreach (ItemProperty ip in oItem.ItemProperties)
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.OnHitCastSpell)
                {
                    if (GetItemPropertySubType(ip) == (int)IPConst.Onhit_Castspell_Onhit_Uniquepower)
                    {
                        return;
                    }
                }
            }

            // No item property found. Add it to the item.
            BiowareXP2.IPSafeAddItemProperty(oItem, ItemPropertyOnHitCastSpell(IPConst.Onhit_Castspell_Onhit_Uniquepower, 40), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
        }

        public static void ReturnItem(NWObject target, NWItem item)
        {
            if (GetHasInventory(item) == true)
            {
                NWObject possessor = item.Possessor;
                possessor.AssignCommand(() =>
                {
                    ActionGiveItem(item, target);
                });
            }
            else
            {
                CopyItem(item.Object, target.Object, true);
                item.Destroy();
            }
        }

        public static void StripAllItemProperties(NWItem item)
        {
            foreach (var ip in item.ItemProperties)
            {
                RemoveItemProperty(item.Object, ip);
            }
        }

        public static ItemProperty GetCustomItemPropertyByItemTag(string tag)
        {
            NWPlaceable container = (GetObjectByTag("item_props"));
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

        public static HashSet<BaseItemType> MeleeWeaponTypes = new HashSet<BaseItemType>()
        {
            BaseItemType.BastardSword,
            BaseItemType.BattleAxe,
            BaseItemType.Club,
            BaseItemType.Dagger,
            BaseItemType.HandAxe,
            BaseItemType.Kama,
            BaseItemType.Katana,
            BaseItemType.Kukri,
            BaseItemType.LightFlail,
            BaseItemType.LightHammer,
            BaseItemType.LightMace,
            BaseItemType.LongSword,
            BaseItemType.Rapier,
            BaseItemType.Scimitar,
            BaseItemType.ShortSpear,
            BaseItemType.ShortSword,
            BaseItemType.Sickle,
            BaseItemType.Whip,
            BaseItemType.Lightsaber,
            BaseItemType.DireMace,
            BaseItemType.DwarvenWaraxe,
            BaseItemType.GreatAxe,
            BaseItemType.GreatSword,
            BaseItemType.Halberd,
            BaseItemType.HeavyFlail,
            BaseItemType.Morningstar,
            BaseItemType.QuarterStaff,
            BaseItemType.Scythe,
            BaseItemType.Trident,
            BaseItemType.Warhammer,
            BaseItemType.DoubleAxe,
            BaseItemType.TwoBladedSword,
            BaseItemType.Saberstaff,
            BaseItemType.Bracer,
            BaseItemType.Gloves

        };

        public static HashSet<BaseItemType> RangedWeaponTypes = new HashSet<BaseItemType>()
        {
            BaseItemType.HeavyCrossBow,
            BaseItemType.LightCrossBow,
            BaseItemType.LongBow,
            BaseItemType.ShortBow,
            BaseItemType.Arrow,
            BaseItemType.Bolt,
            BaseItemType.Grenade,
            BaseItemType.Shuriken,
            BaseItemType.Sling,
            BaseItemType.ThrowingAxe,
            BaseItemType.Bullet,
            BaseItemType.Dart
        };

        private static readonly Dictionary<BaseItemType, Enumeration.Skill> _skillTypeMappings = new Dictionary<BaseItemType, Enumeration.Skill>()
        {
            // One-Handed Skills
            {BaseItemType.BastardSword, Enumeration.Skill.OneHanded},
            {BaseItemType.BattleAxe, Enumeration.Skill.OneHanded},
            {BaseItemType.Club, Enumeration.Skill.OneHanded},
            {BaseItemType.Dagger, Enumeration.Skill.OneHanded},
            {BaseItemType.HandAxe, Enumeration.Skill.OneHanded},
            {BaseItemType.Kama, Enumeration.Skill.OneHanded},
            {BaseItemType.Katana, Enumeration.Skill.OneHanded},
            {BaseItemType.Kukri, Enumeration.Skill.OneHanded},
            {BaseItemType.LightFlail, Enumeration.Skill.OneHanded},
            {BaseItemType.LightHammer, Enumeration.Skill.OneHanded},
            {BaseItemType.LightMace, Enumeration.Skill.OneHanded},
            {BaseItemType.LongSword, Enumeration.Skill.OneHanded},
            {BaseItemType.Morningstar, Enumeration.Skill.OneHanded},
            {BaseItemType.Rapier, Enumeration.Skill.OneHanded},
            {BaseItemType.Scimitar, Enumeration.Skill.OneHanded},
            {BaseItemType.ShortSword, Enumeration.Skill.OneHanded},
            {BaseItemType.Sickle, Enumeration.Skill.OneHanded},
            {BaseItemType.Whip, Enumeration.Skill.OneHanded},
            // Two-Handed Skills
            {BaseItemType.DireMace, Enumeration.Skill.TwoHanded}     ,
            {BaseItemType.DwarvenWaraxe, Enumeration.Skill.TwoHanded},
            {BaseItemType.GreatAxe, Enumeration.Skill.TwoHanded}     ,
            {BaseItemType.GreatSword, Enumeration.Skill.TwoHanded}   ,
            {BaseItemType.Halberd, Enumeration.Skill.TwoHanded}      ,
            {BaseItemType.HeavyFlail, Enumeration.Skill.TwoHanded}   ,
            {BaseItemType.Scythe, Enumeration.Skill.TwoHanded}       ,
            {BaseItemType.Trident, Enumeration.Skill.TwoHanded}      ,
            {BaseItemType.Warhammer, Enumeration.Skill.TwoHanded}    ,
            {BaseItemType.ShortSpear, Enumeration.Skill.TwoHanded}   ,
            // Twin Blades Skills
            {BaseItemType.TwoBladedSword, Enumeration.Skill.TwinBlades },
            {BaseItemType.DoubleAxe, Enumeration.Skill.TwinBlades },
            // Martial Arts Skills
            {BaseItemType.Bracer, Enumeration.Skill.MartialArts},
            {BaseItemType.Gloves, Enumeration.Skill.MartialArts},
            {BaseItemType.QuarterStaff, Enumeration.Skill.MartialArts},
            {BaseItemType.HeavyCrossBow, Enumeration.Skill.Blasters},
            {BaseItemType.LightCrossBow, Enumeration.Skill.Blasters},
            // Firearms Skills
            {BaseItemType.LongBow, Enumeration.Skill.Blasters},
            {BaseItemType.ShortBow, Enumeration.Skill.Blasters},
            {BaseItemType.Arrow, Enumeration.Skill.Blasters},
            {BaseItemType.Bolt, Enumeration.Skill.Blasters},
            // Throwing Skills
            {BaseItemType.Grenade, Enumeration.Skill.Throwing},
            {BaseItemType.Shuriken, Enumeration.Skill.Throwing},
            {BaseItemType.Sling, Enumeration.Skill.Throwing},
            {BaseItemType.ThrowingAxe, Enumeration.Skill.Throwing},
            {BaseItemType.Bullet, Enumeration.Skill.Throwing},
            {BaseItemType.Dart, Enumeration.Skill.Throwing},
            // Shield Skills
            {BaseItemType.SmallShield, Enumeration.Skill.Shields },
            {BaseItemType.LargeShield, Enumeration.Skill.Shields },
            {BaseItemType.TowerShield, Enumeration.Skill.Shields },
            // Lightsabers
            {BaseItemType.Lightsaber, Enumeration.Skill.Lightsaber},
            {BaseItemType.Saberstaff, Enumeration.Skill.Lightsaber}
        };

        public static Enumeration.Skill GetSkillTypeForItem(NWItem item)
        {
            using (new Profiler("ItemService::GetSkillTypeForItem"))
            {
                var type = item.BaseItemType;

                // Check for explicit override.
                if (item.AssociatedSkill > 0) return item.AssociatedSkill;

                // Armor has to specifically be set on the item in order to count.
                // Look for an item type property first.
                if (item.CustomItemType == CustomItemType.LightArmor) return Enumeration.Skill.LightArmor;
                else if (item.CustomItemType == CustomItemType.HeavyArmor) return Enumeration.Skill.HeavyArmor;
                else if (item.CustomItemType == CustomItemType.ForceArmor) return Enumeration.Skill.ForceArmor;

                // Training lightsabers are katana weapons with special local variables.
                if (item.GetLocalBoolean("LIGHTSABER") == true)
                {
                    return Enumeration.Skill.Lightsaber;
                }

                if (!_skillTypeMappings.TryGetValue(type, out var result))
                {
                    return Enumeration.Skill.Unknown;
                }
                return result;
            }
        }

        private static void OnHitCastSpell()
        {
            NWObject target = NWGameObject.OBJECT_SELF;
            if (!target.IsValid) return;

            NWObject oSpellOrigin = (GetSpellCastItem());
            // Item specific
            string script = oSpellOrigin.GetLocalString("SCRIPT");

            if (!string.IsNullOrWhiteSpace(script))
            {
                ScriptItemEvent.Run(script);
            }
        }

        public static bool CanHandleChat(NWObject sender)
        {
            return sender.GetLocalBoolean("ITEM_RENAMING_LISTENING") == true;
        }

        private static void OnModuleNWNXChat()
        {
            NWPlayer player = NWNXChat.GetSender();

            if (!CanHandleChat(player)) return;
            string message = NWNXChat.GetMessage();
            NWNXChat.SkipMessage();

            message = message.Truncate(50);
            player.SetLocalString("RENAMED_ITEM_NEW_NAME", message);
            player.SendMessage("Please click 'Refresh' to see changes, then select 'Change Name' to confirm the changes.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Item;
using SWLOR.Game.Server.Event.Legacy;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Messaging;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using ItemProperty = SWLOR.Game.Server.Core.ItemProperty;
using OnHitCastSpell = SWLOR.Game.Server.Event.Feat.OnHitCastSpell;
using Profiler = SWLOR.Game.Server.ValueObject.Profiler;

namespace SWLOR.Game.Server.Service.Legacy
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
            RegisterActionItemHandlers();
        }

        private static void RegisterActionItemHandlers()
        {
            // Use reflection to get all of IChatCommand handler implementations.
            var classes = Assembly.GetCallingAssembly().GetTypes()
                .Where(p => typeof(IActionItem).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();

            foreach (var type in classes)
            {
                var instance = Activator.CreateInstance(type) as IActionItem;

                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                var key = type.Name;

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
            var name = item.Name;
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
            NWPlayer user = NWScript.OBJECT_SELF;
            NWItem oItem = NWScript.StringToObject(Events.GetEventData("ITEM_OBJECT_ID"));
            NWObject target = NWScript.StringToObject(Events.GetEventData("TARGET_OBJECT_ID"));
            var targetPositionX = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_X"));
            var targetPositionY = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Y"));
            var targetPositionZ = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Z"));
            var targetPosition = Vector3(targetPositionX, targetPositionY, targetPositionZ);
            var targetLocation = Location(user.Area, targetPosition, 0.0f);

            var className = oItem.GetLocalString("SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTIVATE_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTION_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("SCRIPT");
            // Legacy events follow. We can't remove these because of backwards compatibility issues with existing items.
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("ACTIVATE_JAVA_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) className = oItem.GetLocalString("JAVA_ACTION_SCRIPT");
            if (string.IsNullOrWhiteSpace(className)) return;

            // Bypass the NWN "item use" animation.
            Events.SkipEvent();

            user.ClearAllActions();

            if (user.IsBusy)
            {
                user.SendMessage("You are busy.");
                return;
            }

            // Remove "Item." prefix if it exists.
            if (className.StartsWith("Item."))
                className = className.Substring(5);
            var item = GetActionItemHandler(className);

            var invalidTargetMessage = item.IsValidTarget(user, oItem, target, targetLocation);
            if (!string.IsNullOrWhiteSpace(invalidTargetMessage))
            {
                user.SendMessage(invalidTargetMessage);
                return;
            }

            // NOTE - these checks are duplicated in FinishActionItem.  Keep both in sync.
            var maxDistance = item.MaxDistance(user, oItem, target, targetLocation);
            if (maxDistance > 0.0f)
            {
                NWObject owner = GetItemPossessor(target);

                if (target.IsValid && owner.IsValid)
                {
                    // We are okay - we have targeted an item in our inventory (we can't target someone
                    // else's inventory, so no need to actually check distance).
                }
                else if (target.Object == NWScript.OBJECT_SELF)
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

            var customData = item.StartUseItem(user, oItem, target, targetLocation);
            var delay = item.Seconds(user, oItem, target, targetLocation, customData);
            var animationID = item.AnimationID();
            var faceTarget = item.FaceTarget();
            var userPosition = user.Position;

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
                Player.StartGuiTimingBar(user, delay, string.Empty);
            }

            var @event = new OnFinishActionItem(className, user, oItem, target, targetLocation, userPosition, customData);
            user.DelayEvent(delay, @event);
        }

        public static string OnModuleExamine(string existingDescription, NWObject examinedObject)
        {
            if (examinedObject.ObjectType != ObjectType.Item) return existingDescription;

            NWItem examinedItem = (examinedObject.Object);
            var description = "";

            if (examinedItem.RecommendedLevel > 0)
            {
                description += ColorTokenService.Orange("Recommended Level: ") + examinedItem.RecommendedLevel;

                if (examinedItem.BaseItemType == BaseItem.Ring || examinedItem.BaseItemType == BaseItem.Amulet)
                    description += " (Uses your highest armor skill)";

                description += "\n";
            }
            if (examinedItem.LevelIncrease > 0)
            {
                description += ColorTokenService.Orange("Level Increase: ") + examinedItem.LevelIncrease + "\n";
            }
            if (examinedItem.AssociatedSkillType > 0)
            {
                var skill = DataService.Skill.GetByID((int)examinedItem.AssociatedSkillType);
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
                var itemTypeProper = string.Concat(examinedItem.CustomItemType.ToString().Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
                description += ColorTokenService.Orange("Item Type: ") + itemTypeProper + "\n";
            }

            // Check for properties that can only be applied to limited things, and flag them here.
            // Attack bonus, damage, base attack bonus: weapons only
            // AC - armor items only.
            var ip = GetFirstItemProperty(examinedItem);
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

        public static HashSet<BaseItem> ArmorBaseItemTypes = new HashSet<BaseItem>()
        {
            BaseItem.Armor,
            BaseItem.Helmet
        };

        public static HashSet<BaseItem> ShieldBaseItemTypes = new HashSet<BaseItem>()
        {
            BaseItem.LargeShield,
            BaseItem.SmallShield,
            BaseItem.TowerShield
        };

        public static HashSet<BaseItem> WeaponBaseItemTypes = new HashSet<BaseItem>()
        {
            BaseItem.Arrow,
            BaseItem.BastardSword,
            BaseItem.BattleAxe,
            BaseItem.Bolt,
            BaseItem.Bracer,
            BaseItem.Bullet,
            BaseItem.Club,
            BaseItem.Dagger,
            BaseItem.Dart,
            BaseItem.DireMace,
            BaseItem.DoubleAxe,
            BaseItem.DwarvenWarAxe,
            BaseItem.Gloves,
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.Grenade,
            BaseItem.Halberd,
            BaseItem.HandAxe,
            BaseItem.HeavyCrossbow,
            BaseItem.HeavyFlail,
            BaseItem.Kama,
            BaseItem.Katana,
            BaseItem.Kukri,
            BaseItem.LightCrossbow,
            BaseItem.LightFlail,
            BaseItem.LightHammer,
            BaseItem.LightMace,
            BaseItem.Longbow,
            BaseItem.Longsword,
            BaseItem.MorningStar,
            BaseItem.QuarterStaff,
            BaseItem.Rapier,
            BaseItem.Scimitar,
            BaseItem.Scythe,
            BaseItem.ShortBow,
            BaseItem.ShortSpear,
            BaseItem.ShortSword,
            BaseItem.Shuriken,
            BaseItem.Sickle,
            BaseItem.Sling,
            BaseItem.ThrowingAxe,
            BaseItem.Trident,
            BaseItem.TwoBladedSword,
            BaseItem.WarHammer,
            BaseItem.Whip,
            BaseItem.Saberstaff,
            BaseItem.Lightsaber
        };

        private static void OnModuleUnequipItem()
        {
            NWPlayer player = GetPCItemLastUnequippedBy();
            if (GetLocalBool(player, "IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.

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
            var rightHand = oPC.RightHand;
            var leftHand = oPC.LeftHand;

            if (!oPC.IsInCombat) return;
            if (Equals(oItem, rightHand) && Equals(oItem, leftHand)) return;
            if (!Equals(oItem, leftHand)) return;

            oPC.ClearAllActions();
        }

        private static void OnModuleEquipItem()
        {
            BaseItem[] validItemTypes = {
                    BaseItem.Armor,
                    BaseItem.Arrow,
                    BaseItem.BastardSword,
                    BaseItem.BattleAxe,
                    BaseItem.Belt,
                    BaseItem.Bolt,
                    BaseItem.Boots,
                    BaseItem.Bracer,
                    BaseItem.Bullet,
                    BaseItem.Cloak,
                    BaseItem.Club,
                    BaseItem.Dagger,
                    BaseItem.Dart,
                    BaseItem.DireMace,
                    BaseItem.DoubleAxe,
                    BaseItem.DwarvenWarAxe,
                    BaseItem.Gloves,
                    BaseItem.GreatAxe,
                    BaseItem.GreatSword,
                    BaseItem.Grenade,
                    BaseItem.Halberd,
                    BaseItem.HandAxe,
                    BaseItem.HeavyCrossbow,
                    BaseItem.HeavyFlail,
                    BaseItem.Helmet,
                    BaseItem.Kama,
                    BaseItem.Katana,
                    BaseItem.Kukri,
                    BaseItem.LargeShield,
                    BaseItem.LightCrossbow,
                    BaseItem.LightFlail,
                    BaseItem.LightHammer,
                    BaseItem.LightMace,
                    BaseItem.Longbow,
                    BaseItem.Longsword,
                    BaseItem.MorningStar,
                    BaseItem.QuarterStaff,
                    BaseItem.Rapier,
                    BaseItem.Scimitar,
                    BaseItem.Scythe,
                    BaseItem.ShortBow,
                    BaseItem.ShortSpear,
                    BaseItem.ShortSword,
                    BaseItem.Shuriken,
                    BaseItem.Sickle,
                    BaseItem.Sling,
                    BaseItem.SmallShield,
                    BaseItem.ThrowingAxe,
                    BaseItem.TowerShield,
                    BaseItem.Trident,
                    BaseItem.TwoBladedSword,
                    BaseItem.WarHammer,
                    BaseItem.Whip,
                    BaseItem.Saberstaff,
                    BaseItem.Lightsaber

            };

            NWPlayer player = GetPCItemLastEquippedBy();

            if (GetLocalBool(player, "IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.

            NWItem oItem = (GetPCItemLastEquipped());
            var baseItemType = oItem.BaseItemType;
            var eEffect = EffectVisualEffect(VisualEffect.LightsaberHum);
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


            if (baseItemType == BaseItem.Torch)
            {
                var charges = oItem.ReduceCharges();
                if (charges <= 0)
                {
                    oItem.Destroy();
                }
            }

            HandleEquipmentSwappingDelay();
        }

        private static void AddOnHitProperty(NWItem oItem)
        {
            foreach (var ip in oItem.ItemProperties)
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.OnHitCastSpell)
                {
                    if (GetItemPropertySubType(ip) == (int)OnHitCastSpellType.ONHIT_UNIQUEPOWER)
                    {
                        return;
                    }
                }
            }

            // No item property found. Add it to the item.
            BiowareXP2.IPSafeAddItemProperty(oItem, ItemPropertyOnHitCastSpell(OnHitCastSpellType.ONHIT_UNIQUEPOWER, 40), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
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
            var item = container.InventoryItems.SingleOrDefault(x => x.Tag == tag);
            if (item == null)
            {
                throw new Exception("Unable to find an item tagged '" + tag + "' in the item props container.");
            }

            var prop = item.ItemProperties.FirstOrDefault();
            if (prop == null)
            {
                throw new Exception("Unable to find an item property on item tagged '" + tag + "' in the item props container.");
            }

            return prop;
        }

        public static HashSet<BaseItem> MeleeWeaponTypes = new HashSet<BaseItem>()
        {
            BaseItem.BastardSword,
            BaseItem.BattleAxe,
            BaseItem.Club,
            BaseItem.Dagger,
            BaseItem.HandAxe,
            BaseItem.Kama,
            BaseItem.Katana,
            BaseItem.Kukri,
            BaseItem.LightFlail,
            BaseItem.LightHammer,
            BaseItem.LightMace,
            BaseItem.Longsword,
            BaseItem.Rapier,
            BaseItem.Scimitar,
            BaseItem.ShortSpear,
            BaseItem.ShortSword,
            BaseItem.Sickle,
            BaseItem.Whip,
            BaseItem.Lightsaber,
            BaseItem.DireMace,
            BaseItem.DwarvenWarAxe,
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.Halberd,
            BaseItem.HeavyFlail,
            BaseItem.MorningStar,
            BaseItem.QuarterStaff,
            BaseItem.Scythe,
            BaseItem.Trident,
            BaseItem.WarHammer,
            BaseItem.DoubleAxe,
            BaseItem.TwoBladedSword,
            BaseItem.Saberstaff,
            BaseItem.Bracer,
            BaseItem.Gloves

        };

        public static HashSet<BaseItem> RangedWeaponTypes = new HashSet<BaseItem>()
        {
            BaseItem.HeavyCrossbow,
            BaseItem.LightCrossbow,
            BaseItem.Longbow,
            BaseItem.ShortBow,
            BaseItem.Arrow,
            BaseItem.Bolt,
            BaseItem.Grenade,
            BaseItem.Shuriken,
            BaseItem.Sling,
            BaseItem.ThrowingAxe,
            BaseItem.Bullet,
            BaseItem.Dart
        };

        private static readonly Dictionary<BaseItem, SkillType> _skillTypeMappings = new Dictionary<BaseItem, SkillType>()
        {
            // One-Handed Skills
            {BaseItem.BastardSword, SkillType.OneHanded},
            {BaseItem.BattleAxe, SkillType.OneHanded},            
            {BaseItem.Dagger, SkillType.OneHanded},
            {BaseItem.HandAxe, SkillType.OneHanded},
            {BaseItem.Kama, SkillType.OneHanded},
            {BaseItem.Katana, SkillType.OneHanded},
            {BaseItem.Kukri, SkillType.OneHanded},
            {BaseItem.LightFlail, SkillType.OneHanded},
            {BaseItem.LightHammer, SkillType.OneHanded},
            {BaseItem.LightMace, SkillType.OneHanded},
            {BaseItem.Longsword, SkillType.OneHanded},
            {BaseItem.MorningStar, SkillType.OneHanded},
            {BaseItem.Rapier, SkillType.OneHanded},
            {BaseItem.Scimitar, SkillType.OneHanded},
            {BaseItem.ShortSword, SkillType.OneHanded},
            {BaseItem.Sickle, SkillType.OneHanded},
            {BaseItem.Whip, SkillType.OneHanded},
            // Two-Handed Skills
            {BaseItem.DireMace, SkillType.TwoHanded}     ,
            {BaseItem.DwarvenWarAxe, SkillType.TwoHanded},
            {BaseItem.GreatAxe, SkillType.TwoHanded}     ,
            {BaseItem.GreatSword, SkillType.TwoHanded}   ,
            {BaseItem.Halberd, SkillType.TwoHanded}      ,
            {BaseItem.HeavyFlail, SkillType.TwoHanded}   ,
            {BaseItem.Scythe, SkillType.TwoHanded}       ,
            {BaseItem.Trident, SkillType.TwoHanded}      ,
            {BaseItem.WarHammer, SkillType.TwoHanded}    ,
            {BaseItem.ShortSpear, SkillType.TwoHanded}   ,
            // Twin Blades Skills
            {BaseItem.TwoBladedSword, SkillType.TwinBlades },
            {BaseItem.DoubleAxe, SkillType.TwinBlades },
            // Martial Arts Skills
            {BaseItem.Club, SkillType.MartialArts},
            {BaseItem.Bracer, SkillType.MartialArts},
            {BaseItem.Gloves, SkillType.MartialArts},
            {BaseItem.QuarterStaff, SkillType.MartialArts},
            // Firearms Skills
            {BaseItem.HeavyCrossbow, SkillType.Firearms},
            {BaseItem.LightCrossbow, SkillType.Firearms},
            {BaseItem.Longbow, SkillType.Firearms},
            {BaseItem.ShortBow, SkillType.Firearms},
            {BaseItem.Arrow, SkillType.Firearms},
            {BaseItem.Bolt, SkillType.Firearms},
            {BaseItem.Bullet, SkillType.Firearms},
            {BaseItem.Sling, SkillType.Firearms},
            // Throwing Skills
            {BaseItem.Grenade, SkillType.Throwing},
            {BaseItem.Shuriken, SkillType.Throwing},            
            {BaseItem.ThrowingAxe, SkillType.Throwing},
            {BaseItem.Dart, SkillType.Throwing},
            // Shield Skills
            {BaseItem.SmallShield, SkillType.Shields },
            {BaseItem.LargeShield, SkillType.Shields },
            {BaseItem.TowerShield, SkillType.Shields },
            // Lightsabers
            {BaseItem.Lightsaber, SkillType.Lightsaber},
            {BaseItem.Saberstaff, SkillType.Lightsaber}
        };

        public static SkillType GetSkillTypeForItem(NWItem item)
        {
            using (new Profiler("ItemService::GetSkillTypeForItem"))
            {
                var type = item.BaseItemType;

                // Check for explicit override.
                if (item.AssociatedSkillType > 0) return item.AssociatedSkillType;

                // Armor has to specifically be set on the item in order to count.
                // Look for an item type property first.
                if (item.CustomItemType == CustomItemType.LightArmor) return SkillType.LightArmor;
                else if (item.CustomItemType == CustomItemType.HeavyArmor) return SkillType.HeavyArmor;
                else if (item.CustomItemType == CustomItemType.ForceArmor) return SkillType.ForceArmor;

                // Training lightsabers are katana weapons with special local variables.
                if (GetLocalBool(item, "LIGHTSABER") == true)
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

        private static void OnHitCastSpell()
        {
            NWObject target = NWScript.OBJECT_SELF;
            if (!target.IsValid) return;

            NWObject oSpellOrigin = (GetSpellCastItem());
            // Item specific
            var script = oSpellOrigin.GetLocalString("SCRIPT");

            if (!string.IsNullOrWhiteSpace(script))
            {
                ScriptItemEvent.Run(script);
            }
        }

        public static bool CanHandleChat(NWObject sender)
        {
            return GetLocalBool(sender, "ITEM_RENAMING_LISTENING") == true;
        }

        private static void OnModuleNWNXChat()
        {
            NWPlayer player = Chat.GetSender();

            if (!CanHandleChat(player)) return;
            var message = Chat.GetMessage();
            Chat.SkipMessage();

            message = message.Truncate(50);
            player.SetLocalString("RENAMED_ITEM_NEW_NAME", message);
            player.SendMessage("Please click 'Refresh' to see changes, then select 'Change Name' to confirm the changes.");
        }
    }
}

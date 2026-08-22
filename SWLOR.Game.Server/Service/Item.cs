using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    public static class Item
    {
        /// <summary>
        /// NWN local bool: set to true on crafted output so collect objectives can require player-made items.
        /// </summary>
        public const string PlayerProducedItemVariable = "PLAYER_PRODUCED_ITEM";

        private static readonly Dictionary<string, ItemDetail> _items = new();
        private static readonly Dictionary<int, int[]> _2daCache = new();
        private static readonly Dictionary<BaseItem, AbilityType> _itemToDamageAbilityMapping = new();
        private static readonly Dictionary<BaseItem, AbilityType> _itemToAccuracyAbilityMapping = new();
        private static readonly IReadOnlyList<BaseItem> _meleeStatMappedBaseItems = new[]
        {
            BaseItem.BastardSword,
            BaseItem.BattleAxe,
            BaseItem.Dagger,
            BaseItem.HandAxe,
            BaseItem.Kama,
            BaseItem.Katana,
            BaseItem.Kukri,
            BaseItem.LightFlail,
            BaseItem.LightHammer,
            BaseItem.LightMace,
            BaseItem.Longsword,
            BaseItem.MorningStar,
            BaseItem.Rapier,
            BaseItem.Scimitar,
            BaseItem.ShortSword,
            BaseItem.Sickle,
            BaseItem.Whip,
            BaseItem.Lightsaber,
            BaseItem.Electroblade,
            BaseItem.DireMace,
            BaseItem.DwarvenWarAxe,
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.Halberd,
            BaseItem.HeavyFlail,
            BaseItem.Scythe,
            BaseItem.Trident,
            BaseItem.WarHammer,
            BaseItem.ShortSpear,
            BaseItem.TwoBladedSword,
            BaseItem.DoubleAxe,
            BaseItem.Saberstaff,
            BaseItem.TwinElectroBlade,
            BaseItem.Club,
            BaseItem.Bracer,
            BaseItem.Gloves,
            BaseItem.QuarterStaff,
            BaseItem.Katar,
            BaseItem.CreatureBludgeonWeapon,
            BaseItem.CreaturePierceWeapon,
            BaseItem.CreatureSlashPierceWeapon,
            BaseItem.CreatureSlashWeapon,
        };
        private static readonly IReadOnlyList<BaseItem> _rangedStatMappedBaseItems = new[]
        {
            BaseItem.Cannon,
            BaseItem.Rifle,
            BaseItem.Longbow,
            BaseItem.Pistol,
            BaseItem.LegacyPistol,
            BaseItem.Arrow,
            BaseItem.Bolt,
            BaseItem.Bullet,
            BaseItem.Sling,
            BaseItem.Grenade,
            BaseItem.Shuriken,
            BaseItem.ThrowingAxe,
            BaseItem.Dart,
        };

        /// <summary>
        /// When the module loads, all item details are loaded into the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Load2DACache();
            LoadItemToDamageStatMapping();
            LoadItemToAccuracyStatMapping();
        }
        private static void Load2DACache()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IItemListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IItemListDefinition)Activator.CreateInstance(type);
                var items = instance.BuildItems();

                foreach (var (itemTag, itemDetail) in items)
                {
                    _items[itemTag] = itemDetail;
                }
            }

            Console.WriteLine($"Loaded {_items.Count} items.");

            // Cache 2da values that we need.  Create a new array for each row, otherwise they
            // end up pointing to the same array object (and get overwritten).
            for (var row = 0; row < Get2DARowCount("baseitems"); row++)
            {
                var threatString = Get2DAString("baseitems", "CritThreat", row);
                var multString = Get2DAString("baseitems", "CritHitMult", row);
                var sizeString = Get2DAString("baseitems", "WeaponSize", row);

                var threat = string.IsNullOrWhiteSpace(threatString) ? 1 : Convert.ToInt32(threatString);
                var mult = string.IsNullOrWhiteSpace(multString) ? 1 : Convert.ToInt32(multString);
                var size = string.IsNullOrWhiteSpace(sizeString) ? 1 : Convert.ToInt32(sizeString);

                var values = new int[3];
                values[0] = threat;
                values[1] = mult;
                values[2] = size;

                _2daCache[row] = values;
            }

            Console.WriteLine($"Loaded {_2daCache.Count} base items.");
        }

        private static void LoadItemToDamageStatMapping()
        {
            foreach (var itemType in _meleeStatMappedBaseItems)
            {
                _itemToDamageAbilityMapping[itemType] = AbilityType.Might;
            }

            foreach (var itemType in _rangedStatMappedBaseItems)
            {
                _itemToDamageAbilityMapping[itemType] = AbilityType.Perception;
            }

            Console.WriteLine($"Loaded {_itemToDamageAbilityMapping.Count} item to damage ability mappings.");
        }

        private static void LoadItemToAccuracyStatMapping()
        {
            foreach (var itemType in _meleeStatMappedBaseItems)
            {
                _itemToAccuracyAbilityMapping[itemType] = AbilityType.Perception;
            }

            foreach (var itemType in _rangedStatMappedBaseItems)
            {
                _itemToAccuracyAbilityMapping[itemType] = AbilityType.Agility;
            }

            Console.WriteLine($"Loaded {_itemToAccuracyAbilityMapping.Count} item to accuracy ability mappings.");
        }

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of damage calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="itemType">The item type</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        public static AbilityType GetWeaponDamageAbilityType(BaseItem itemType)
        {
            return !_itemToDamageAbilityMapping.ContainsKey(itemType)
                ? AbilityType.Invalid
                : _itemToDamageAbilityMapping[itemType];
        }

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of accuracy calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="itemType">The item type</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        public static AbilityType GetWeaponAccuracyAbilityType(BaseItem itemType)
        {
            return !_itemToAccuracyAbilityMapping.ContainsKey(itemType)
                ? AbilityType.Invalid
                : _itemToAccuracyAbilityMapping[itemType];
        }

        /// <summary>
        /// When an item is used, if its tag is in the item cache, run it through the action item process.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemUseBefore)]
        public static void UseItem()
        {
            var user = OBJECT_SELF;
            void CheckPosition(uint actionUser, string actionId, Vector3 originalPosition)
            {
                // Action ended, no need to continue checking.
                if (!GetLocalBool(actionUser, actionId)) return;

                var position = GetPosition(actionUser);

                if (position.X != originalPosition.X ||
                    position.Y != originalPosition.Y ||
                    position.Z != originalPosition.Z)
                {
                    Activity.ClearBusy(actionUser);
                    SendMessageToPC(actionUser, "You move and interrupt your action.");
                    PlayerPlugin.StopGuiTimingBar(actionUser, string.Empty);
                    return;
                }

                DelayCommand(0.1f, () => CheckPosition(actionUser, actionId, originalPosition));
            }

            var item = StringToObject(EventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            var itemTag = GetTag(item);

            // Not in the cache. Skip.
            if (!_items.ContainsKey(itemTag))
                return;

            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            var area = GetArea(user);
            var targetPositionX = (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_X"));
            var targetPositionY = (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Y"));
            var targetPositionZ = (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Z"));
            var targetPosition = GetIsObjectValid(target) ? GetPosition(target) : Vector3(targetPositionX, targetPositionY, targetPositionZ);
            var targetLocation = GetIsObjectValid(target) ? GetLocation(target) : Location(area, targetPosition, 0.0f);
            var userPosition = GetPosition(user);
            var propertyIndex = Convert.ToInt32(EventsPlugin.GetEventData("ITEM_PROPERTY_INDEX"));
            var itemDetail = _items[itemTag];

            // Bypass the NWN "item use" animation.
            EventsPlugin.SkipEvent();

            // Check item property requirements.
            if (!CanCreatureUseItem(user, item))
            {
                SendMessageToPC(user, "You do not meet the requirements to use this item.");
                return;
            }

            // User is busy
            if (Activity.IsBusy(user))
            {
                SendMessageToPC(user, "You are busy.");
                return;
            }

            // Check recast cooldown
            if (itemDetail.RecastGroup != null && itemDetail.RecastCooldown != null)
            {
                var (isOnRecast, timeToWait) = Recast.IsOnRecastDelay(user, (RecastGroup)itemDetail.RecastGroup);
                if (isOnRecast)
                {
                    SendMessageToPC(user, $"This item can be used in {timeToWait}.");
                    return;
                }
            }

            var validationMessage = itemDetail.ValidateAction == null ? string.Empty : itemDetail.ValidateAction(user, item, target, targetLocation, propertyIndex);

            // Failed validation.
            if(!string.IsNullOrWhiteSpace(validationMessage))
            {
                SendMessageToPC(user, validationMessage);
                return;
            }

            // Send the initialization message, if there is one.
            var initializationMessage = itemDetail.InitializationMessageAction == null
                ? string.Empty
                : itemDetail.InitializationMessageAction(user, item, target, targetLocation, propertyIndex);
            if (!string.IsNullOrWhiteSpace(initializationMessage))
            {
                SendMessageToPC(user, initializationMessage);
            }

            var maxDistance = itemDetail.CalculateDistanceAction?.Invoke(user, item, target, targetLocation, propertyIndex) ?? 3.5f;
            // Distance checks, if necessary for this item.
            if (GetItemPossessor(target) != user && maxDistance > 0.0f)
            {
                // Target is valid - check distance between objects.
                if (GetIsObjectValid(target) &&
                    (GetDistanceBetween(user, target) > maxDistance ||
                     area != GetArea(target)))
                {
                    SendMessageToPC(user, "Your target is too far away.");
                    return;
                }
                // Target is invalid - check distance between locations.
                else if (!GetIsObjectValid(target) &&
                         (GetDistanceBetweenLocations(GetLocation(user), targetLocation) > maxDistance ||
                          area != GetAreaFromLocation(targetLocation)))
                {
                    SendMessageToPC(user, "That location is too far away.");
                    return;
                }
            }

            // Make the user turn to face the target if configured.
            if (itemDetail.UserFacesTarget)
            {
                AssignCommand(user, () => SetFacingPoint(targetPosition));
            }

            var delay = itemDetail.DelayAction?.Invoke(user, item, target, targetLocation, propertyIndex) ?? 0.0f;
            // Play an animation if configured.
            if (itemDetail.ActivationAnimation != Animation.Invalid)
            {
                AssignCommand(user, () => ActionPlayAnimation(itemDetail.ActivationAnimation, 1.0f, delay));
            }

            // Play the timing bar for a player user.
            if (delay > 0.0f &&
                GetIsPC(user))
            {
                PlayerPlugin.StartGuiTimingBar(user, delay);
            }

            // Apply the item's action if specified.
            if (itemDetail.ApplyAction != null)
            {
                var actionId = Guid.NewGuid().ToString();
                Activity.SetBusy(user, ActivityStatusType.UseItem);
                SetLocalBool(user, actionId, true);
                CheckPosition(user, actionId, userPosition);

                DelayCommand(delay + 0.1f, () =>
                {
                    DeleteLocalBool(user, actionId);
                    Activity.ClearBusy(user);

                    var updatedPosition = GetPosition(user);

                    // Check if user has moved.
                    if (userPosition.X != updatedPosition.X ||
                        userPosition.Y != updatedPosition.Y ||
                        userPosition.Z != updatedPosition.Z)
                    {
                        return;
                    }

                    // Rerun validation since things may have changed since the user started the action.
                    validationMessage = itemDetail.ValidateAction == null ? string.Empty : itemDetail.ValidateAction(user, item, target, targetLocation, propertyIndex);
                    if (!string.IsNullOrWhiteSpace(validationMessage))
                    {
                        SendMessageToPC(user, validationMessage);
                        return;
                    }

                    itemDetail.ApplyAction(user, item, target, targetLocation, propertyIndex);

                    if (itemDetail.RecastGroup != null && itemDetail.RecastCooldown != null)
                    {
                        Recast.ApplyRecastDelay(user, (RecastGroup)itemDetail.RecastGroup, (float)itemDetail.RecastCooldown);
                    }

                    // Reduce item charge if specified.
                    var reducesItemCharge = itemDetail.ReducesItemChargeAction?.Invoke(user, item, target, targetLocation, propertyIndex) ?? false;
                    if (reducesItemCharge)
                    {
                        var charges = GetItemCharges(item) - 1;

                        if (charges <= 0)
                        {
                            DestroyObject(item);
                        }
                        else
                        {
                            SetItemCharges(item, charges);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Checks all item use limitation properties against a creature's effective requirements.
        /// </summary>
        public static bool CanCreatureUseItem(uint creature, uint item)
        {
            return string.IsNullOrWhiteSpace(GetCreatureItemUseError(creature, item));
        }

        public static string GetCreatureItemUseError(uint creature, uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);

                if (type == ItemPropertyType.UseLimitationPerk)
                {
                    var perkType = (PerkType)GetItemPropertySubType(ip);
                    var levelRequired = GetItemPropertyCostTableValue(ip);

                    if (perkType == PerkType.Invalid)
                        continue;

                    if (Perk.GetPerkLevel(creature, perkType) < levelRequired)
                    {
                        var perkName = Perk.GetPerkDetails(perkType).Name;
                        return $"This item requires '{perkName}' level {levelRequired} to use.";
                    }
                }
                else if (type == ItemPropertyType.RequiresSkill)
                {
                    var skillType = (SkillType)GetItemPropertySubType(ip);
                    var rankRequired = GetItemPropertyCostTableValue(ip);

                    if (Skill.GetCreatureSkillRank(creature, skillType) < rankRequired)
                    {
                        var skillName = Skill.GetSkillDetails(skillType).Name;
                        return $"This item requires {skillName} rank {rankRequired} to use.";
                    }
                }
                else if (type == ItemPropertyType.RequiresStat)
                {
                    var abilityType = (AbilityType)GetItemPropertySubType(ip);
                    var statRequired = GetItemPropertyCostTableValue(ip);

                    if (CreaturePlugin.GetRawAbilityScore(creature, abilityType) < statRequired)
                    {
                        var abilityNameStrRef = StringToInt(Get2DAString("iprp_reqstat", "Name", (int)abilityType));
                        var abilityName = abilityNameStrRef == 0
                            ? abilityType.ToString()
                            : GetStringByStrRef(abilityNameStrRef);

                        return $"This item requires {abilityName} {statRequired} to use.";
                    }
                }
            }

            return string.Empty;
        }

        public static string CanEquip(uint creature, uint item)
        {
            var isPlayer = GetIsPC(creature);
            var isDroid = Droid.IsDroid(creature);
            var itemType = GetBaseItemType(item);

            if (ForceSensitiveWeaponBaseItemTypes.Contains(itemType) &&
                !CanEquipForceSensitiveWeapon(creature))
            {
                return "Only Force Sensitive characters may equip that item.";
            }

            if ((!isPlayer && !isDroid) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return string.Empty;

            if (Gui.IsWindowOpen(creature, GuiWindowType.Craft))
                return "Items cannot be equipped while crafting.";

            var itemUseError = GetCreatureItemUseError(creature, item);
            if (!string.IsNullOrWhiteSpace(itemUseError))
                return itemUseError;

            var race = GetRacialType(creature);

            var needsDroidLimitation = race == RacialType.Droid && DroidBaseItemTypes.Contains(itemType);
            var itemHasDroidIP = false;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.UseLimitationRacialType)
                    continue;

                var limitationRace = (RacialType)GetItemPropertySubType(ip);
                if (limitationRace != RacialType.Droid)
                    continue;

                if (race != RacialType.Droid)
                    return "This item may only be equipped by Droids.";

                if (needsDroidLimitation)
                    itemHasDroidIP = true;
            }

            if (needsDroidLimitation && !itemHasDroidIP)
                return "Droids may not equip that item.";

            return string.Empty;
        }

        private static bool CanEquipForceSensitiveWeapon(uint creature)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return true;

            if (Droid.IsDroid(creature) ||
                BeastMastery.GetBeastType(creature) != BeastType.Invalid)
            {
                return false;
            }

            if (!GetIsPC(creature))
                return true;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = DB.Get<Player>(playerId);

            return dbPlayer?.CharacterType == CharacterType.ForceSensitive;
        }

        /// <summary>
        /// Returns an item to a target.
        /// </summary>
        /// <param name="target">The target receiving the item.</param>
        /// <param name="item">The item being returned.</param>
        public static void ReturnItem(uint target, uint item)
        {
            if (GetHasInventory(item))
            {
                var possessor = GetItemPossessor(item);
                AssignCommand(possessor, () =>
                {
                    ActionGiveItem(item, target);
                });
            }
            else
            {
                CopyItem(item, target, true);
                DestroyObject(item);
            }
        }

        /// <summary>
        /// Returns the number of items in an object's inventory.
        /// Returns -1 if target does not have an inventory
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>-1 if obj doesn't have an inventory, otherwise returns the number of items in the inventory</returns>
        public static int GetInventoryItemCount(uint obj)
        {
            if (!GetHasInventory(obj)) return -1;

            var count = 0;
            var item = GetFirstItemInInventory(obj);
            while (GetIsObjectValid(item))
            {
                count++;
                item = GetNextItemInInventory(obj);
            }

            return count;
        }

        /// <summary>
        /// Retrieves the list of weapon base item types.
        /// </summary>
        public static List<BaseItem> WeaponBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.BastardSword,
            BaseItem.Longsword,
            BaseItem.Katana,
            BaseItem.Scimitar,
            BaseItem.BattleAxe,
            BaseItem.Dagger,
            BaseItem.Rapier,
            BaseItem.ShortSword,
            BaseItem.Kukri,
            BaseItem.Sickle,
            BaseItem.Whip,
            BaseItem.HandAxe,
            BaseItem.Lightsaber,
            BaseItem.Electroblade,
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.DwarvenWarAxe,
            BaseItem.Halberd,
            BaseItem.Scythe,
            BaseItem.ShortSpear,
            BaseItem.Trident,
            BaseItem.DoubleAxe,
            BaseItem.TwoBladedSword,
            BaseItem.Saberstaff,
            BaseItem.TwinElectroBlade,
            BaseItem.Katar,
            BaseItem.QuarterStaff,
            BaseItem.LightMace,
            BaseItem.Pistol,
            BaseItem.LegacyPistol,
            BaseItem.Sling,
            BaseItem.ThrowingAxe,
            BaseItem.Shuriken,
            BaseItem.Dart,
            BaseItem.Cannon,
            BaseItem.Longbow,
            BaseItem.Rifle,
        };

        /// <summary>
        /// Retrieves the list of armor base item types.
        /// </summary>
        public static List<BaseItem> ArmorBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Armor,
            BaseItem.Helmet,
            BaseItem.Cloak,
            BaseItem.Belt,
            BaseItem.Amulet,
            BaseItem.Boots,
            BaseItem.LargeShield,
            BaseItem.SmallShield,
            BaseItem.TowerShield,
            BaseItem.Gloves,
            BaseItem.Bracer,
            BaseItem.Ring
        };

        /// <summary>
        /// Retrieves the list of shield base item types.
        /// </summary>
        public static List<BaseItem> ShieldBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.LargeShield,
            BaseItem.SmallShield,
            BaseItem.TowerShield
        };

        public static bool IsBaseItemType(uint item, IReadOnlyCollection<BaseItem> baseItemTypes)
        {
            return GetIsObjectValid(item) && baseItemTypes.Contains(GetBaseItemType(item));
        }

        public static bool IsBaseItemType(global::NWN.Native.API.CNWSItem item, IReadOnlyCollection<BaseItem> baseItemTypes)
        {
            return item != null && baseItemTypes.Contains((BaseItem)item.m_nBaseItem);
        }

        /// <summary>
        /// Retrieves the list of Vibroblade base item types.
        /// </summary>
        public static List<BaseItem> VibrobladeBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.BastardSword,
            BaseItem.Longsword,
            BaseItem.Katana,
            BaseItem.Scimitar,
            BaseItem.BattleAxe
        };

        /// <summary>
        /// Retrieves the list of Vibroknife base item types.
        /// </summary>
        public static List<BaseItem> VibroknifeBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Dagger,
            BaseItem.Rapier,
            BaseItem.ShortSword,
            BaseItem.Kukri,
            BaseItem.Sickle,
            BaseItem.Whip,
            BaseItem.HandAxe,
        };

        /// <summary>
        /// Retrieves the list of Lightsaber base item types.
        /// </summary>
        public static List<BaseItem> LightsaberBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Lightsaber,
            BaseItem.Electroblade
        };

        /// <summary>
        /// Retrieves the list of Heavy Vibroblade base item types.
        /// </summary>
        public static List<BaseItem> HeavyVibrobladeBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.DireMace,
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.DwarvenWarAxe,
            BaseItem.HeavyFlail,
            BaseItem.WarHammer
        };

        /// <summary>
        /// Retrieves the list of Spear base item types.
        /// </summary>
        public static List<BaseItem> SpearBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Halberd,
            BaseItem.Scythe,
            BaseItem.ShortSpear,
            BaseItem.Trident
        };

        /// <summary>
        /// Retrieves the list of Twin Blade base item types.
        /// </summary>
        public static List<BaseItem> TwinBladeBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.DoubleAxe,
            BaseItem.TwoBladedSword
        };

        /// <summary>
        /// Retrieves the list of Saberstaff base item types.
        /// </summary>
        public static List<BaseItem> SaberstaffBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Saberstaff,
            BaseItem.TwinElectroBlade
        };

        /// <summary>
        /// Retrieves the list of base item types restricted to Force Sensitive characters.
        /// </summary>
        public static List<BaseItem> ForceSensitiveWeaponBaseItemTypes { get; } = LightsaberBaseItemTypes
            .Concat(SaberstaffBaseItemTypes)
            .ToList();

        /// <summary>
        /// Retrieves the list of Katar base item types.
        /// </summary>
        public static List<BaseItem> KatarBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Katar
        };

        /// <summary>
        /// Retrieves the list of Staff base item types.
        /// </summary>
        public static List<BaseItem> StaffBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.QuarterStaff,
            BaseItem.LightMace,
            BaseItem.Club,
            BaseItem.MorningStar,
            BaseItem.LightFlail,
            BaseItem.LightHammer
        };

        /// <summary>
        /// Retrieves the list of Pistol base item types.
        /// </summary>
        public static List<BaseItem> PistolBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Pistol,
            BaseItem.LegacyPistol,
            BaseItem.Sling
        };

        /// <summary>
        /// Retrieves the list of Throwing Weapon base item types.
        /// </summary>
        public static List<BaseItem> ThrowingWeaponBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.ThrowingAxe,
            BaseItem.Shuriken,
            BaseItem.Dart
        };

        /// <summary>
        /// Retrieves the list of Rifle base item types.
        /// </summary>
        public static List<BaseItem> RifleBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.Longbow,
            BaseItem.Rifle,
            BaseItem.Cannon
        };

        /// <summary>
        /// Retrieves the list of one-hand melee weapon base item types.
        /// These are physical equip categories, not skill categories.
        /// </summary>
        public static List<BaseItem> OneHandedMeleeItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.BastardSword,
            BaseItem.Longsword,
            BaseItem.Katana,
            BaseItem.Scimitar,
            BaseItem.BattleAxe,
            BaseItem.Dagger,
            BaseItem.Rapier,
            BaseItem.ShortSword,
            BaseItem.Kukri,
            BaseItem.Sickle,
            BaseItem.Whip,
            BaseItem.HandAxe,
            BaseItem.Lightsaber,
            BaseItem.Electroblade,
            BaseItem.ShortSpear,
            BaseItem.Katar,
        };

        /// <summary>
        /// Retrieves the list of two-handed melee weapon base item types.
        /// These are physical equip categories, not skill categories.
        /// </summary>
        public static List<BaseItem> TwoHandedMeleeItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.GreatAxe,
            BaseItem.GreatSword,
            BaseItem.DwarvenWarAxe,
            BaseItem.Halberd,
            BaseItem.Scythe,
            BaseItem.Trident,
            BaseItem.DoubleAxe,
            BaseItem.TwoBladedSword,
            BaseItem.Saberstaff,
            BaseItem.QuarterStaff,
            BaseItem.LightMace
        };

        /// <summary>
        /// Retrieves the list of Creature base item types.
        /// </summary>
        public static List<BaseItem> CreatureBaseItemTypes { get; } = new List<BaseItem>
        {
            BaseItem.CreatureBludgeonWeapon,
            BaseItem.CreatureSlashWeapon,
            BaseItem.CreaturePierceWeapon,
            BaseItem.CreatureSlashPierceWeapon
        };

        /// <summary>
        /// Retrieves the list of Droid base item types.
        /// These are items which require the Use Limitation Race: Droid item property in order to be equipped by a Droid.
        /// </summary>
        public static List<BaseItem> DroidBaseItemTypes { get; } = new List<BaseItem>()
        {
            BaseItem.Armor,
            BaseItem.Helmet,
            BaseItem.Cloak,
            BaseItem.Belt,
            BaseItem.Amulet,
            BaseItem.Boots,
            BaseItem.Gloves,
            BaseItem.Bracer,
            BaseItem.Ring
        };

        /// <summary>
        /// The icon used when no valid icon resource can be resolved for an item. Prevents the
        /// red "missing texture" X from appearing in NUI item lists.
        /// </summary>
        private const string GenericItemIconResref = "iit_smlmisc_001";

        /// <summary>
        /// Determines whether an icon resource actually exists (as either a TGA or DDS texture).
        /// NUI renders a red X for missing textures, so icon resrefs must be verified before use.
        /// </summary>
        /// <param name="resref">The icon resref to check.</param>
        /// <returns>true if the resource exists, false otherwise.</returns>
        private static bool IconResourceExists(string resref)
        {
            if (string.IsNullOrWhiteSpace(resref))
                return false;

            return ResManGetAliasFor(resref, ResType.TGA) != string.Empty ||
                   ResManGetAliasFor(resref, ResType.DDS) != string.Empty;
        }

        /// <summary>
        /// Retrieves the icon used on the UIs. Every returned resref is verified to exist as a
        /// texture; unresolvable icons fall back to the base item's default icon and finally to
        /// a generic icon so NUI never renders a red missing-texture X.
        /// </summary>
        /// <param name="item">The item to retrieve the icon for.</param>
        /// <returns>A resref of the icon to use.</returns>
        public static string GetIconResref(uint item)
        {
            return ResolveIconResref(item, out _);
        }

        /// <summary>
        /// Determines whether an item has a real inventory icon, as opposed to falling back to the
        /// generic placeholder icon. Items with no real icon are almost always internal, prop, or
        /// creature items that should not appear on player-facing economy surfaces.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if a real icon resource resolved, false if the generic fallback was used.</returns>
        public static bool HasInventoryIcon(uint item)
        {
            ResolveIconResref(item, out var hasRealIcon);
            return hasRealIcon;
        }

        /// <summary>
        /// Creature-equipment base item types. Players never trade these; the "stat skins" that carry
        /// a creature's combat stats are <see cref="BaseItem.CreatureItem"/>.
        /// </summary>
        private static readonly HashSet<BaseItem> EconomyRestrictedBaseItems = new()
        {
            BaseItem.Invalid,
            BaseItem.CreatureSlashWeapon,
            BaseItem.CreaturePierceWeapon,
            BaseItem.CreatureBludgeonWeapon,
            BaseItem.CreatureSlashPierceWeapon,
            BaseItem.CreatureItem
        };

        /// <summary>
        /// Name prefixes the builders reserve for NPC-only gear, anchored to the start of the item name.
        /// </summary>
        private static readonly string[] EconomyRestrictedNamePrefixes = { "[NPC]", "(NPC" };

        /// <summary>
        /// Blueprint local variable that explicitly excludes an item from player-facing economy surfaces.
        /// Set this on NPC-only blueprints that a normal player item is otherwise indistinguishable from
        /// (a real base type, a real icon, and no [NPC] name), such as the "Specialist" NPC weapons.
        /// </summary>
        public const string NoEconomyVariable = "NO_ECONOMY";

        /// <summary>
        /// Determines whether an item should be hidden from player-facing economy surfaces (contract
        /// objective search, and any future market-style blueprint pickers). Combines creature base
        /// types, the reserved NPC name prefixes, an explicit blueprint opt-out flag, and the absence
        /// of a real inventory icon. This is the single source of truth; callers must not re-derive it.
        /// </summary>
        /// <param name="item">The item to classify.</param>
        /// <returns>true if the item is NPC/creature/internal and should not be shown to players.</returns>
        public static bool IsEconomyRestricted(uint item)
        {
            var baseItem = GetBaseItemType(item);
            var name = GetName(item);
            var noEconomy = GetLocalInt(item, NoEconomyVariable) == 1;
            if (IsEconomyRestricted(baseItem, name, noEconomy, hasInventoryIcon: true))
                return true;

            return !HasInventoryIcon(item);
        }

        /// <summary>
        /// Data-only form of the shared economy classifier. Builder tools use this overload while
        /// inspecting UTI data that has not been instantiated by the game engine.
        /// </summary>
        public static bool IsEconomyRestricted(
            BaseItem baseItem,
            string name,
            bool noEconomy,
            bool hasInventoryIcon)
        {
            return EconomyRestrictedBaseItems.Contains(baseItem) ||
                   noEconomy ||
                   IsEconomyRestrictedName(name) ||
                   !hasInventoryIcon;
        }

        /// <summary>
        /// The name-based portion of <see cref="IsEconomyRestricted"/>, split out so it can be unit
        /// tested without spawning an item. A blank name denotes an internal/unfinished blueprint.
        /// </summary>
        /// <param name="name">The item's display name.</param>
        /// <returns>true if the name marks the item as NPC-only or internal.</returns>
        public static bool IsEconomyRestrictedName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            var trimmed = name.TrimStart();

            foreach (var prefix in EconomyRestrictedNamePrefixes)
            {
                if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string ResolveIconResref(uint item, out bool hasRealIcon)
        {
            hasRealIcon = true;
            var baseItem = GetBaseItemType(item);

            if (baseItem == BaseItem.Cloak) // Cloaks use PLTs so their default icon doesn't really work
            {
                if (IconResourceExists("iit_cloak"))
                    return "iit_cloak";
            }
            else if (baseItem == BaseItem.SpellScroll || baseItem == BaseItem.EnchantedScroll)
            {// Scrolls get their icon from the cast spell property
                if (GetItemHasItemProperty(item, ItemPropertyType.CastSpell))
                {
                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.CastSpell)
                        {
                            var spellIcon = Get2DAString("iprp_spells", "Icon", GetItemPropertySubType(ip));
                            if (IconResourceExists(spellIcon))
                                return spellIcon;
                        }
                    }
                }
            }
            else if (Get2DAString("baseitems", "ModelType", (int)baseItem) == "0")
            {// Create the icon resref for simple modeltype items
                var sSimpleModelId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0).ToString();
                while (GetStringLength(sSimpleModelId) < 3)
                {
                    sSimpleModelId = "0" + sSimpleModelId;
                }

                // The engine convention for simple-model icons is i<ItemClass>_<model>. This covers
                // custom base items (e.g. DNA, essences) whose DefaultIcon is just iinvalid_2x2.
                var itemClass = Get2DAString("baseitems", "ItemClass", (int)baseItem);
                var classIcon = ("i" + itemClass + "_" + sSimpleModelId).ToLower();
                if (IconResourceExists(classIcon))
                    return classIcon;

                var sDefaultIcon = Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
                switch (baseItem)
                {
                    case BaseItem.MiscSmall:
                    case BaseItem.MiscellaneousSmallStackable:
                    case BaseItem.CraftMaterialSmall:
                        sDefaultIcon = "iit_smlmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscMedium:
                    case BaseItem.MiscMediumStackable:
                    case BaseItem.CraftMaterialMedium:
                    case BaseItem.CraftBase:
                        sDefaultIcon = "iit_midmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscLarge:
                        sDefaultIcon = "iit_talmisc_" + sSimpleModelId;
                        break;
                    case BaseItem.MiscThin:
                    case BaseItem.MiscellaneousThinStackable:
                        sDefaultIcon = "iit_thnmisc_" + sSimpleModelId;
                        break;
                }

                var nLength = GetStringLength(sDefaultIcon);
                if (GetSubString(sDefaultIcon, nLength - 4, 1) == "_")// Some items have a default icon of xx_yyy_001, we strip the last 4 symbols if that is the case
                    sDefaultIcon = GetStringLeft(sDefaultIcon, nLength - 4);
                var sIcon = sDefaultIcon + "_" + sSimpleModelId;
                if (IconResourceExists(sIcon))
                    return sIcon;
            }

            // For everything else use the item's default icon, verified to exist. The iinvalid
            // placeholder icons render as a red X, so they are never acceptable even though the
            // texture technically exists.
            var defaultIcon = Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
            if (!defaultIcon.StartsWith("iinvalid"))
            {
                if (IconResourceExists(defaultIcon))
                    return defaultIcon;

                // Some default icons are stored with a _XXX variant suffix even though the 2DA omits it.
                if (IconResourceExists(defaultIcon + "_001"))
                    return defaultIcon + "_001";
            }

            hasRealIcon = false;
            return GenericItemIconResref;
        }

        /// <summary>
        /// Builds a string containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A string containing all of the item properties.</returns>
        public static string BuildItemPropertyString(uint item)
        {
            var sb = new StringBuilder();

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                BuildSingleItemPropertyString(sb, ip);
                sb.Append("\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a list of strings containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A list containing all of the item properties.</returns>
        public static GuiBindingList<string> BuildItemPropertyList(uint item)
        {
            var list = new GuiBindingList<string>();
            var sb = new StringBuilder();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                BuildSingleItemPropertyString(sb, ip);
                list.Add(sb.ToString());
                sb.Clear();
            }

            return list;
        }

        /// <summary>
        /// Builds a list of strings containing all of the item properties on an i tem.
        /// </summary>
        /// <param name="itemProperties">The list of item properties to use.</param>
        /// <returns>A list containing all of the item properties.</returns>
        public static GuiBindingList<string> BuildItemPropertyList(List<ItemProperty> itemProperties)
        {
            var list = new GuiBindingList<string>();
            var sb = new StringBuilder();
            foreach (var ip in itemProperties)
            {
                BuildSingleItemPropertyString(sb, ip);
                list.Add(sb.ToString());
                sb.Clear();
            }

            return list;
        }

        private static void BuildSingleItemPropertyString(StringBuilder sb, ItemProperty ip)
        {
            var typeId = (int)GetItemPropertyType(ip);
            var gameStringRef = Get2DAString("itempropdef", "GameStrRef", typeId);
            if (string.IsNullOrWhiteSpace(gameStringRef))
                return;

            var name = GetStringByStrRef(Convert.ToInt32(gameStringRef));
            sb.Append(name);

            var subTypeId = GetItemPropertySubType(ip);
            if (subTypeId != -1)
            {
                var subTypeResref = Get2DAString("itempropdef", "SubTypeResRef", typeId);
                var strRefId = StringToInt(Get2DAString(subTypeResref, "Name", subTypeId));
                if (strRefId != 0)
                {
                    var text = $" {GetStringByStrRef(strRefId)}";
                    sb.Append(text);
                }
            }

            var param1 = GetItemPropertyParam1(ip);
            if (param1 != -1)
            {
                var paramResref = Get2DAString("iprp_paramtable", "TableResRef", param1);
                var strRef = StringToInt(Get2DAString(paramResref, "Name", GetItemPropertyParam1Value(ip)));
                if (strRef != 0)
                {
                    var text = $" {GetStringByStrRef(strRef)}";
                    sb.Append(text);
                }
            }

            var costTable = GetItemPropertyCostTable(ip);
            if (costTable != -1)
            {
                var costTableResref = Get2DAString("iprp_costtable", "Name", costTable);
                var strRef = StringToInt(Get2DAString(costTableResref, "Name", GetItemPropertyCostTableValue(ip)));
                if (strRef != 0)
                {
                    var text = $" {GetStringByStrRef(strRef)}";
                    sb.Append(text);
                }
            }
        }

        /// <summary>
        /// Determines whether an item can be stored persistently in the database.
        /// </summary>
        /// <param name="player">The player attempting to persistently store the item.</param>
        /// <param name="item">The item being stored.</param>
        /// <returns>An error message if validation fails, otherwise an empty string if it succeeds.</returns>
        public static string CanBePersistentlyStored(uint player, uint item)
        {
            var resref = GetResRef(item);
            string[] disallowedResrefs = { Droid.DroidControlItemResref };

            if (GetItemPossessor(item) != player)
            {
                return "Item must be in your inventory.";
            }

            if (GetHasInventory(item))
            {
                return "Containers cannot be stored.";
            }

            if (GetBaseItemType(item) == BaseItem.Gold)
            {
                return "Credits cannot be placed inside.";
            }

            if (GetItemCursedFlag(item))
            {
                return "That item cannot be stored.";
            }

            if (disallowedResrefs.Contains(resref))
            {
                return "That item cannot be stored.";
            }

            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                if (GetItemInSlot((InventorySlot)index, player) == item)
                {
                    return "Unequip the item first.";
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the cumulative DMG value on a given item.
        /// A minimum of 1 is always returned.
        /// No checks for item type are made in this method.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>The DMG rating, or 1 if not found.</returns>
        public static int GetDMG(uint item)
        {
            var dmg = 0;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.DMG)
                {
                    dmg += GetItemPropertyCostTableValue(ip);
                }
            }

            if (dmg < 1)
                dmg = 1;

            return dmg;
        }

        /// <summary>
        /// Reduces an item stack by a specific amount.
        /// If there are not enough items in the stack to reduce, false will be returned.
        /// If the stack size of the item will reach 0, the item is destroyed and true will be returned.
        /// If the stack size will reach a number greater than 0, the item's stack size will be updated and true will be returned.
        /// </summary>
        /// <param name="item">The item to adjust</param>
        /// <param name="reduceBy">The amount to reduce by. Absolute value is used to determine this value.</param>
        /// <returns>true if successfully reduced or destroyed, false otherwise</returns>
        public static bool ReduceItemStack(uint item, int reduceBy)
        {
            var amount = Math.Abs(reduceBy);
            var stackSize = GetItemStackSize(item);

            // Have to reduce by at least one.
            if (amount <= 0)
                return false;

            // Stack size cannot be smaller than the amount we're reducing by.
            if (stackSize < reduceBy)
                return false;

            var remaining = stackSize - reduceBy;
            if (remaining <= 0)
            {
                DestroyObject(item);
                return true;
            }
            else
            {
                SetItemStackSize(item, remaining);
                return true;
            }
        }

        /// <summary>
        /// Returns true if <see cref="PlayerProducedItemVariable"/> is set (e.g. crafting stamped the item).
        /// </summary>
        public static bool IsPlayerProducedItem(uint item)
        {
            return GetLocalBool(item, PlayerProducedItemVariable);
        }

        /// <summary>
        /// Determines if an item is a legacy item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if item is legacy, false otherwise</returns>
        public static bool IsLegacyItem(uint item)
        {
            return GetTag(item) == "LEGACY_ITEM";
        }

        /// <summary>
        /// Marks an item as a legacy item.
        /// </summary>
        /// <param name="item">The item to mark as legacy.</param>
        public static void MarkLegacyItem(uint item)
        {
            SetTag(item, "LEGACY_ITEM");
        }

        /// <summary>
        /// Retrieves the item slot of a specific item.
        /// If the item isn't equipped, InventorySlot.Invalid will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="item">The item to search for.</param>
        /// <returns>The inventory slot of the item or InventorySlot.Invalid if not equipped.</returns>
        public static InventorySlot GetItemSlot(uint creature, uint item)
        {
            var slot = InventorySlot.Invalid;

            if (GetItemInSlot(InventorySlot.Head, creature) == item) slot = InventorySlot.Head;
            if (GetItemInSlot(InventorySlot.Chest, creature) == item) slot = InventorySlot.Chest;
            if (GetItemInSlot(InventorySlot.Boots, creature) == item) slot = InventorySlot.Boots;
            if (GetItemInSlot(InventorySlot.Arms, creature) == item) slot = InventorySlot.Arms;
            if (GetItemInSlot(InventorySlot.RightHand, creature) == item) slot = InventorySlot.RightHand;
            if (GetItemInSlot(InventorySlot.LeftHand, creature) == item) slot = InventorySlot.LeftHand;
            if (GetItemInSlot(InventorySlot.Cloak, creature) == item) slot = InventorySlot.Cloak;
            if (GetItemInSlot(InventorySlot.LeftRing, creature) == item) slot = InventorySlot.LeftRing;
            if (GetItemInSlot(InventorySlot.RightRing, creature) == item) slot = InventorySlot.RightRing;
            if (GetItemInSlot(InventorySlot.Neck, creature) == item) slot = InventorySlot.Neck;
            if (GetItemInSlot(InventorySlot.Belt, creature) == item) slot = InventorySlot.Belt;
            if (GetItemInSlot(InventorySlot.Arrows, creature) == item) slot = InventorySlot.Arrows;
            if (GetItemInSlot(InventorySlot.Bullets, creature) == item) slot = InventorySlot.Bullets;
            if (GetItemInSlot(InventorySlot.Bolts, creature) == item) slot = InventorySlot.Bolts;
            if (GetItemInSlot(InventorySlot.CreatureLeft, creature) == item) slot = InventorySlot.CreatureLeft;
            if (GetItemInSlot(InventorySlot.CreatureRight, creature) == item) slot = InventorySlot.CreatureRight;
            if (GetItemInSlot(InventorySlot.CreatureBite, creature) == item) slot = InventorySlot.CreatureBite;
            if (GetItemInSlot(InventorySlot.CreatureArmor, creature) == item) slot = InventorySlot.CreatureArmor;

            return slot;
        }

    }
}

using System.Numerics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Inventory.Service
{
    public class ItemService : IItemService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventsPluginService _eventsPlugin;
        private readonly IPlayerPluginService _playerPlugin;
        
        // Cached data
        private IInterfaceCache<string, ItemDetail> _itemCache;
        
        // Pre-computed cache for fast retrieval
        private readonly Dictionary<string, ItemDetail> _allItems = new();
        
        // Additional caches for complex data
        private readonly Dictionary<int, int[]> _2daCache = new();
        private readonly Dictionary<BaseItemType, AbilityType> _itemToDamageAbilityMapping = new();
        private readonly Dictionary<BaseItemType, AbilityType> _itemToAccuracyAbilityMapping = new();

        public ItemService(
            ILogger logger, 
            IServiceProvider serviceProvider,
            IEventsPluginService eventsPlugin,
            IPlayerPluginService playerPlugin)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _eventsPlugin = eventsPlugin;
            _playerPlugin = playerPlugin;
        }

        // Lazy-loaded services to break circular dependencies
        private IGenericCacheService CacheService => _serviceProvider.GetRequiredService<IGenericCacheService>();
        private IActivityService ActivityService => _serviceProvider.GetRequiredService<IActivityService>();
        private IRecastService RecastService => _serviceProvider.GetRequiredService<IRecastService>();
        private IDroidService DroidService => _serviceProvider.GetRequiredService<IDroidService>();
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        

        /// <summary>
        /// When the module loads, all item details are loaded into the cache.
        /// </summary>
        public void CacheData()
        {
            Load2DACache();
            LoadItemToDamageStatMapping();
            LoadItemToAccuracyStatMapping();
        }
        public void Load2DACache()
        {
            _itemCache = CacheService.BuildInterfaceCache<IItemListDefinition, string, ItemDetail>()
                .WithDataExtractor(instance => instance.BuildItems())
                .Build();

            // Populate pre-computed cache
            foreach (var (itemTag, itemDetail) in _itemCache.AllItems)
            {
                _allItems[itemTag] = itemDetail;
            }

            Console.WriteLine($"Loaded {_itemCache.AllItems.Count} items.");

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

        public void LoadItemToDamageStatMapping()
        {
            // One-Handed Skills
            _itemToDamageAbilityMapping[BaseItemType.BastardSword] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.BattleAxe] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Dagger] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.HandAxe] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Kama] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Katana] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Kukri] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.LightFlail] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.LightHammer] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.LightMace] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Longsword] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.MorningStar] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Rapier] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Scimitar] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.ShortSword] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Sickle] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Whip] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Lightsaber] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Electroblade] = AbilityType.Perception;

            // Two-Handed Skills
            _itemToDamageAbilityMapping[BaseItemType.DireMace] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.DwarvenWarAxe] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.GreatAxe] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.GreatSword] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Halberd] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.HeavyFlail] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Scythe] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Trident] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.WarHammer] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.ShortSpear] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.TwoBladedSword] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.DoubleAxe] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Saberstaff] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.TwinElectroBlade] = AbilityType.Perception;

            // Martial Arts Skills
            _itemToDamageAbilityMapping[BaseItemType.Club] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Bracer] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Gloves] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.QuarterStaff] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Katar] = AbilityType.Perception;

            // Ranged Skills
            _itemToDamageAbilityMapping[BaseItemType.Cannon] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Rifle] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Longbow] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Pistol] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Arrow] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Bolt] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Bullet] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Sling] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Grenade] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.Shuriken] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.ThrowingAxe] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.Dart] = AbilityType.Might;

            // NPCs
            _itemToDamageAbilityMapping[BaseItemType.CreatureBludgeonWeapon] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.CreaturePierceWeapon] = AbilityType.Perception;
            _itemToDamageAbilityMapping[BaseItemType.CreatureSlashPierceWeapon] = AbilityType.Might;
            _itemToDamageAbilityMapping[BaseItemType.CreatureSlashWeapon] = AbilityType.Might;

            Console.WriteLine($"Loaded {_itemToDamageAbilityMapping.Count} item to damage ability mappings.");
        }

        public void LoadItemToAccuracyStatMapping()
        {
            // One-Handed Skills
            _itemToAccuracyAbilityMapping[BaseItemType.BastardSword] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.BattleAxe] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Dagger] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.HandAxe] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Kama] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Katana] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Kukri] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.LightFlail] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.LightHammer] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.LightMace] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Longsword] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.MorningStar] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Rapier] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Scimitar] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.ShortSword] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Sickle] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Whip] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Lightsaber] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Electroblade] = AbilityType.Agility;

            // Two-Handed Skills
            _itemToAccuracyAbilityMapping[BaseItemType.DireMace] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.DwarvenWarAxe] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.GreatAxe] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.GreatSword] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Halberd] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.HeavyFlail] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Scythe] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Trident] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.WarHammer] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.ShortSpear] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.TwoBladedSword] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.DoubleAxe] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Saberstaff] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.TwinElectroBlade] = AbilityType.Agility;

            // Martial Arts Skills
            _itemToAccuracyAbilityMapping[BaseItemType.Club] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Bracer] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Gloves] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.QuarterStaff] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.Katar] = AbilityType.Agility;

            // Ranged Skills
            _itemToAccuracyAbilityMapping[BaseItemType.Cannon] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Rifle] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Longbow] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Pistol] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Arrow] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Bolt] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Bullet] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Sling] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Grenade] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Shuriken] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.ThrowingAxe] = AbilityType.Agility;
            _itemToAccuracyAbilityMapping[BaseItemType.Dart] = AbilityType.Agility;

            // NPCs
            _itemToAccuracyAbilityMapping[BaseItemType.CreatureBludgeonWeapon] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.CreaturePierceWeapon] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.CreatureSlashPierceWeapon] = AbilityType.Perception;
            _itemToAccuracyAbilityMapping[BaseItemType.CreatureSlashWeapon] = AbilityType.Perception;

            Console.WriteLine($"Loaded {_itemToDamageAbilityMapping.Count} item to accuracy ability mappings.");
        }

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of damage calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="itemType">The item type</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        public AbilityType GetWeaponDamageAbilityType(BaseItemType itemType)
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
        public AbilityType GetWeaponAccuracyAbilityType(BaseItemType itemType)
        {
            return !_itemToAccuracyAbilityMapping.ContainsKey(itemType)
                ? AbilityType.Invalid
                : _itemToAccuracyAbilityMapping[itemType];
        }

        /// <summary>
        /// When an item is used, if its tag is in the item cache, run it through the action item process.
        /// </summary>
        public void UseItem()
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
                    ActivityService.ClearBusy(actionUser);
                    SendMessageToPC(actionUser, "You move and interrupt your action.");
                    _playerPlugin.StopGuiTimingBar(actionUser, string.Empty);
                    return;
                }

                DelayCommand(0.1f, () => CheckPosition(actionUser, actionId, originalPosition));
            }

            var item = StringToObject(_eventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            var itemTag = GetTag(item);

            // Not in the cache. Skip.
            if (!_allItems.ContainsKey(itemTag))
                return;

            var target = StringToObject(_eventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            var area = GetArea(user);
            var targetPositionX = (float)Convert.ToDouble(_eventsPlugin.GetEventData("TARGET_POSITION_X"));
            var targetPositionY = (float)Convert.ToDouble(_eventsPlugin.GetEventData("TARGET_POSITION_Y"));
            var targetPositionZ = (float)Convert.ToDouble(_eventsPlugin.GetEventData("TARGET_POSITION_Z"));
            var targetPosition = GetIsObjectValid(target) ? GetPosition(target) : Vector3(targetPositionX, targetPositionY, targetPositionZ);
            var targetLocation = GetIsObjectValid(target) ? GetLocation(target) : Location(area, targetPosition, 0.0f);
            var userPosition = GetPosition(user);
            var propertyIndex = Convert.ToInt32(_eventsPlugin.GetEventData("ITEM_PROPERTY_INDEX"));
            var itemDetail = _allItems[itemTag];

            // Bypass the NWN "item use" animation.
            _eventsPlugin.SkipEvent();

            // Check item property requirements.
            if (!CanCreatureUseItem(user, item))
            {
                SendMessageToPC(user, "You do not meet the requirements to use this item.");
                return;
            }

            // User is busy
            if (ActivityService.IsBusy(user))
            {
                SendMessageToPC(user, "You are busy.");
                return;
            }

            // Check recast cooldown
            if (itemDetail.RecastGroup != null && itemDetail.RecastCooldown != null)
            {
                var (isOnRecast, timeToWait) = RecastService.IsOnRecastDelay(user, (RecastGroupType)itemDetail.RecastGroup);
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
            if (itemDetail.ActivationAnimation != AnimationType.Invalid)
            {
                AssignCommand(user, () => ActionPlayAnimation(itemDetail.ActivationAnimation, 1.0f, delay));
            }

            // Play the timing bar for a player user.
            if (delay > 0.0f &&
                GetIsPC(user))
            {
                _playerPlugin.StartGuiTimingBar(user, delay);
            }

            // Apply the item's action if specified.
            if (itemDetail.ApplyAction != null)
            {
                var actionId = Guid.NewGuid().ToString();
                ActivityService.SetBusy(user, ActivityStatusType.UseItem);
                SetLocalBool(user, actionId, true);
                CheckPosition(user, actionId, userPosition);

                DelayCommand(delay + 0.1f, () =>
                {
                    DeleteLocalBool(user, actionId);
                    ActivityService.ClearBusy(user);

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
                        RecastService.ApplyRecastDelay(user, (RecastGroupType)itemDetail.RecastGroup, (float)itemDetail.RecastCooldown, true);
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
        /// Checks all of the "Use Limitation: Perk" item properties on an item against a creature's effective level in the required perk.
        /// If player meets or exceeds the level required for all item properties, returns true. Otherwise returns false.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="item">The item to pull requirements from.</param>
        /// <returns>true if all requirements met, false otherwise</returns>
        public bool CanCreatureUseItem(uint creature, uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.UseLimitationPerk)
                {
                    var perkType = (PerkType)GetItemPropertySubType(ip);
                    var levelRequired = GetItemPropertyCostTableValue(ip);

                    if (PerkService.GetPerkLevel(creature, perkType) < levelRequired)
                        return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Returns an item to a target.
        /// </summary>
        /// <param name="target">The target receiving the item.</param>
        /// <param name="item">The item being returned.</param>
        public void ReturnItem(uint target, uint item)
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
        public int GetInventoryItemCount(uint obj)
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
        /// Retrieves the armor type of an item.
        /// This is based on the Use Limitation: Perk property.
        /// If it's not specified, ArmorType.Invalid will be returned.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>The ArmorType value of the item. Returns ArmorType.Invalid if neither Light or Heavy are found.</returns>
        public ArmorType GetArmorType(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.UseLimitationPerk) continue;

                var perkType = (PerkType) GetItemPropertySubType(ip);
                if (PerkService.HeavyArmorPerks.Contains(perkType))
                {
                    return ArmorType.Heavy;
                }
                else if (PerkService.LightArmorPerks.Contains(perkType))
                {
                    return ArmorType.Light;
                }
            }

            return ArmorType.Invalid;
        }

        /// <summary>
        /// Retrieves the list of weapon base item types.
        /// </summary>
        public List<BaseItemType> WeaponBaseItemTypes { get; } = new()
        {
            BaseItemType.BastardSword,
            BaseItemType.Longsword,
            BaseItemType.Katana,
            BaseItemType.Scimitar,
            BaseItemType.BattleAxe,
            BaseItemType.Dagger,
            BaseItemType.Rapier,
            BaseItemType.ShortSword,
            BaseItemType.Kukri,
            BaseItemType.Sickle,
            BaseItemType.Whip,
            BaseItemType.HandAxe,
            BaseItemType.Lightsaber,
            BaseItemType.Electroblade,
            BaseItemType.GreatAxe,
            BaseItemType.GreatSword,
            BaseItemType.DwarvenWarAxe,
            BaseItemType.Halberd,
            BaseItemType.Scythe,
            BaseItemType.ShortSpear,
            BaseItemType.Trident,
            BaseItemType.DoubleAxe,
            BaseItemType.TwoBladedSword,
            BaseItemType.Saberstaff,
            BaseItemType.TwinElectroBlade,
            BaseItemType.Katar,
            BaseItemType.QuarterStaff,
            BaseItemType.LightMace,
            BaseItemType.Pistol,
            BaseItemType.ThrowingAxe,
            BaseItemType.Shuriken,
            BaseItemType.Dart,
            BaseItemType.Cannon,
            BaseItemType.Longbow,
            BaseItemType.Rifle,
        };

        /// <summary>
        /// Retrieves the list of armor base item types.
        /// </summary>
        public List<BaseItemType> ArmorBaseItemTypes { get; } = new()
        {
            BaseItemType.Armor,
            BaseItemType.Helmet,
            BaseItemType.Cloak,
            BaseItemType.Belt,
            BaseItemType.Amulet,
            BaseItemType.Boots,
            BaseItemType.LargeShield,
            BaseItemType.SmallShield,
            BaseItemType.TowerShield,
            BaseItemType.Gloves,
            BaseItemType.Bracer,
            BaseItemType.Ring
        };

        /// <summary>
        /// Retrieves the list of shield base item types.
        /// </summary>
        public List<BaseItemType> ShieldBaseItemTypes { get; } = new()
        {
            BaseItemType.LargeShield,
            BaseItemType.SmallShield,
            BaseItemType.TowerShield
        };

        /// <summary>
        /// Retrieves the list of Vibroblade base item types.
        /// </summary>
        public List<BaseItemType> VibrobladeBaseItemTypes { get; } = new()
        {
            BaseItemType.BastardSword,
            BaseItemType.Longsword,
            BaseItemType.Katana,
            BaseItemType.Scimitar,
            BaseItemType.BattleAxe
        };

        /// <summary>
        /// Retrieves the list of Finesse Vibroblade base item types.
        /// </summary>
        public List<BaseItemType> FinesseVibrobladeBaseItemTypes { get; } = new()
        {
            BaseItemType.Dagger,
            BaseItemType.Rapier,
            BaseItemType.ShortSword,
            BaseItemType.Kukri,
            BaseItemType.Sickle,
            BaseItemType.Whip,
            BaseItemType.HandAxe,
        };

        /// <summary>
        /// Retrieves the list of Lightsaber base item types.
        /// </summary>
        public List<BaseItemType> LightsaberBaseItemTypes { get; } = new()
        {
            BaseItemType.Lightsaber,
            BaseItemType.Electroblade
        };

        /// <summary>
        /// Retrieves the list of Heavy Vibroblade base item types.
        /// </summary>
        public List<BaseItemType> HeavyVibrobladeBaseItemTypes { get; } = new()
        {
            BaseItemType.GreatAxe,
            BaseItemType.GreatSword,
            BaseItemType.DwarvenWarAxe
        };

        /// <summary>
        /// Retrieves the list of Polearm base item types.
        /// </summary>
        public List<BaseItemType> PolearmBaseItemTypes { get; } = new()
        {
            BaseItemType.Halberd,
            BaseItemType.Scythe,
            BaseItemType.ShortSpear,
            BaseItemType.Trident
        };

        /// <summary>
        /// Retrieves the list of Twin Blade base item types.
        /// </summary>
        public List<BaseItemType> TwinBladeBaseItemTypes { get; } = new()
        {
            BaseItemType.DoubleAxe,
            BaseItemType.TwoBladedSword
        };

        /// <summary>
        /// Retrieves the list of Saberstaff base item types.
        /// </summary>
        public List<BaseItemType> SaberstaffBaseItemTypes { get; } = new()
        {
            BaseItemType.Saberstaff,
            BaseItemType.TwinElectroBlade
        };

        /// <summary>
        /// Retrieves the list of Katar base item types.
        /// </summary>
        public List<BaseItemType> KatarBaseItemTypes { get; } = new()
        {
            BaseItemType.Katar
        };

        /// <summary>
        /// Retrieves the list of Staff base item types.
        /// </summary>
        public List<BaseItemType> StaffBaseItemTypes { get; } = new()
        {
            BaseItemType.QuarterStaff,
            BaseItemType.LightMace,
            BaseItemType.Club,
            BaseItemType.MorningStar
        };

        /// <summary>
        /// Retrieves the list of Pistol base item types.
        /// </summary>
        public List<BaseItemType> PistolBaseItemTypes { get; } = new()
        {
            BaseItemType.Pistol
        };

        /// <summary>
        /// Retrieves the list of Throwing Weapon base item types.
        /// </summary>
        public List<BaseItemType> ThrowingWeaponBaseItemTypes { get; } = new()
        {
            BaseItemType.ThrowingAxe,
            BaseItemType.Shuriken,
            BaseItemType.Dart
        };
        
        /// <summary>
        /// Retrieves the list of Rifle base item types.
        /// </summary>
        public List<BaseItemType> RifleBaseItemTypes { get; } = new()
        {
            BaseItemType.Longbow,
            BaseItemType.Rifle,
            BaseItemType.Cannon
        };

        /// <summary>
        /// Retrieves the list of One-Handed weapon types.
        /// These are the weapons which are held in one hand and not necessarily associated with the One-Handed skill.
        /// </summary>
        public List<BaseItemType> OneHandedMeleeItemTypes { get; } = new()
        {
            BaseItemType.BastardSword,
            BaseItemType.Longsword,
            BaseItemType.Katana,
            BaseItemType.Scimitar,
            BaseItemType.BattleAxe,
            BaseItemType.Dagger,
            BaseItemType.Rapier,
            BaseItemType.ShortSword,
            BaseItemType.Kukri,
            BaseItemType.Sickle,
            BaseItemType.Whip,
            BaseItemType.HandAxe,
            BaseItemType.Lightsaber,
            BaseItemType.Electroblade,
            BaseItemType.ShortSpear,
            BaseItemType.Katar,
        };

        /// <summary>
        /// Retrieves the list of Two-Handed melee weapon types.
        /// These are the weapons which are held in two hand and not necessarily associated with the Two-Handed skill.
        /// </summary>
        public List<BaseItemType> TwoHandedMeleeItemTypes { get; } = new()
        {
            BaseItemType.GreatAxe,
            BaseItemType.GreatSword,
            BaseItemType.DwarvenWarAxe,
            BaseItemType.Halberd,
            BaseItemType.Scythe,
            BaseItemType.Trident,
            BaseItemType.DoubleAxe,
            BaseItemType.TwoBladedSword,
            BaseItemType.Saberstaff,
            BaseItemType.QuarterStaff,
            BaseItemType.LightMace
        };

        /// <summary>
        /// Retrieves the list of Creature base item types.
        /// </summary>
        public List<BaseItemType> CreatureBaseItemTypes { get; } = new()
        {
            BaseItemType.CreatureBludgeonWeapon,
            BaseItemType.CreatureSlashWeapon,
            BaseItemType.CreaturePierceWeapon,
            BaseItemType.CreatureSlashPierceWeapon
        };

        /// <summary>
        /// Retrieves the list of Droid base item types.
        /// These are items which require the Use Limitation Race: Droid item property in order to be equipped by a Droid.
        /// </summary>
        public List<BaseItemType> DroidBaseItemTypes { get; } = new()
        {
            BaseItemType.Armor,
            BaseItemType.Helmet,
            BaseItemType.Cloak,
            BaseItemType.Belt,
            BaseItemType.Amulet,
            BaseItemType.Boots,
            BaseItemType.Gloves,
            BaseItemType.Bracer,
            BaseItemType.Ring
        };

        /// <summary>
        /// Retrieves the icon used on the UIs. 
        /// </summary>
        /// <param name="item">The item to retrieve the icon for.</param>
        /// <returns>A resref of the icon to use.</returns>
        public string GetIconResref(uint item)
        {
            var baseItem = GetBaseItemType(item);

            if (baseItem == BaseItemType.Cloak) // Cloaks use PLTs so their default icon doesn't really work
                return "iit_cloak";
            else if (baseItem == BaseItemType.SpellScroll || baseItem == BaseItemType.EnchantedScroll)
            {// Scrolls get their icon from the cast spell property
                if (GetItemHasItemProperty(item, ItemPropertyType.CastSpell))
                {
                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.CastSpell)
                            return Get2DAString("iprp_spells", "Icon", GetItemPropertySubType(ip));
                    }
                }
            }
            else if (Get2DAString("baseitems", "ModelType", (int)baseItem) == "0")
            {// Create the icon resref for simple modeltype items
                var sSimpleModelId = GetItemAppearance(item, ItemModelColorType.SimpleModel, 0).ToString();
                while (GetStringLength(sSimpleModelId) < 3)
                {
                    sSimpleModelId = "0" + sSimpleModelId;
                }

                var sDefaultIcon = Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
                switch (baseItem)
                {
                    case BaseItemType.MiscSmall:
                    case BaseItemType.MiscellaneousSmallStackable:
                    case BaseItemType.CraftMaterialSmall:
                        sDefaultIcon = "iit_smlmisc_" + sSimpleModelId;
                        break;
                    case BaseItemType.MiscMedium:
                    case BaseItemType.MiscMediumStackable:
                    case BaseItemType.CraftMaterialMedium:
                    case BaseItemType.CraftBase:
                        sDefaultIcon = "iit_midmisc_" + sSimpleModelId;
                        break;
                    case BaseItemType.MiscLarge:
                        sDefaultIcon = "iit_talmisc_" + sSimpleModelId;
                        break;
                    case BaseItemType.MiscThin:
                    case BaseItemType.MiscellaneousThinStackable:
                        sDefaultIcon = "iit_thnmisc_" + sSimpleModelId;
                        break;
                }

                var nLength = GetStringLength(sDefaultIcon);
                if (GetSubString(sDefaultIcon, nLength - 4, 1) == "_")// Some items have a default icon of xx_yyy_001, we strip the last 4 symbols if that is the case
                    sDefaultIcon = GetStringLeft(sDefaultIcon, nLength - 4);
                var sIcon = sDefaultIcon + "_" + sSimpleModelId;
                if (ResManGetAliasFor(sIcon, ResType.TGA) != "")// Check if the icon actually exists, if not, we'll fall through and return the default icon
                    return sIcon;
            }

            // For everything else use the item's default icon
            return Get2DAString("baseitems", "DefaultIcon", (int)baseItem);
        }

        /// <summary>
        /// Builds a string containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A string containing all of the item properties.</returns>
        public string BuildItemPropertyString(uint item)
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
        public IGuiBindingList<string> BuildItemPropertyList(uint item)
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
        public IGuiBindingList<string> BuildItemPropertyList(List<ItemProperty> itemProperties)
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
        public string CanBePersistentlyStored(uint player, uint item)
        {
            var resref = GetResRef(item);
            string[] disallowedResrefs = { DroidService.DroidControlItemResref };

            if (GetItemPossessor(item) != player)
            {
                return "Item must be in your inventory.";
            }

            if (GetHasInventory(item))
            {
                return "Containers cannot be stored.";
            }

            if (GetBaseItemType(item) == BaseItemType.Gold)
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
                if (GetItemInSlot((InventorySlotType)index, player) == item)
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
        public int GetDMG(uint item)
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
        /// Retrieves the critical modifier for a given item type.
        /// The value returned is based on the baseitems.2da file.
        /// </summary>
        /// <param name="type">The item type to check</param>
        /// <returns>The critical modifer value.</returns>
        public int GetCriticalModifier(BaseItemType type)
        {
            var mod = _2daCache[(int)type][1];
            _logger.Write<AttackLogGroup>("Crit multiplier for item type " + type + " is " + mod);

            return mod;
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
        public bool ReduceItemStack(uint item, int reduceBy)
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
        /// Determines if an item is a legacy item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if item is legacy, false otherwise</returns>
        public bool IsLegacyItem(uint item)
        {
            return GetTag(item) == "LEGACY_ITEM";
        }

        /// <summary>
        /// Marks an item as a legacy item.
        /// </summary>
        /// <param name="item">The item to mark as legacy.</param>
        public void MarkLegacyItem(uint item)
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
        public InventorySlotType GetItemSlot(uint creature, uint item)
        {
            var slot = InventorySlotType.Invalid;

            if (GetItemInSlot(InventorySlotType.Head, creature) == item) slot = InventorySlotType.Head;
            if (GetItemInSlot(InventorySlotType.Chest, creature) == item) slot = InventorySlotType.Chest;
            if (GetItemInSlot(InventorySlotType.Boots, creature) == item) slot = InventorySlotType.Boots;
            if (GetItemInSlot(InventorySlotType.Arms, creature) == item) slot = InventorySlotType.Arms;
            if (GetItemInSlot(InventorySlotType.RightHand, creature) == item) slot = InventorySlotType.RightHand;
            if (GetItemInSlot(InventorySlotType.LeftHand, creature) == item) slot = InventorySlotType.LeftHand;
            if (GetItemInSlot(InventorySlotType.Cloak, creature) == item) slot = InventorySlotType.Cloak;
            if (GetItemInSlot(InventorySlotType.LeftRing, creature) == item) slot = InventorySlotType.LeftRing;
            if (GetItemInSlot(InventorySlotType.RightRing, creature) == item) slot = InventorySlotType.RightRing;
            if (GetItemInSlot(InventorySlotType.Neck, creature) == item) slot = InventorySlotType.Neck;
            if (GetItemInSlot(InventorySlotType.Belt, creature) == item) slot = InventorySlotType.Belt;
            if (GetItemInSlot(InventorySlotType.Arrows, creature) == item) slot = InventorySlotType.Arrows;
            if (GetItemInSlot(InventorySlotType.Bullets, creature) == item) slot = InventorySlotType.Bullets;
            if (GetItemInSlot(InventorySlotType.Bolts, creature) == item) slot = InventorySlotType.Bolts;
            if (GetItemInSlot(InventorySlotType.CreatureLeft, creature) == item) slot = InventorySlotType.CreatureLeft;
            if (GetItemInSlot(InventorySlotType.CreatureRight, creature) == item) slot = InventorySlotType.CreatureRight;
            if (GetItemInSlot(InventorySlotType.CreatureBite, creature) == item) slot = InventorySlotType.CreatureBite;
            if (GetItemInSlot(InventorySlotType.CreatureArmor, creature) == item) slot = InventorySlotType.CreatureArmor;

            return slot;
        }

    }
}

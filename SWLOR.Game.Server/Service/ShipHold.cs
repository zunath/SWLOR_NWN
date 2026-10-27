using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The ship's hold: mining, gas and salvage yields go into the ship's interior storage while it has room, and into
    /// the pilot's inventory only when it is full or the ship has none (plan section 12). The hold is a storage category
    /// of the ship's property, so its items show in the ship's storage window like anything stored by hand.
    /// </summary>
    public static class ShipHold
    {
        /// <summary>The storage category yields go to. It is created the first time the ship has a yield to store.</summary>
        public const string HoldCategoryName = "Ship Hold";

        /// <summary>The most categories a property's storage window allows (PropertyItemStorageViewModel).</summary>
        private const int MaxCategories = 20;

        private const string TemporaryStorageTag = "TEMP_ITEM_STORAGE";
        private const string HoldFullWarnedAtVariable = "SHIP_HOLD_FULL_WARNED_AT";
        private const int HoldFullWarningSeconds = 60;

        private static readonly Dictionary<string, int> _maxStackByResref = new();

        /// <summary>
        /// Delivers a cycle's yield for the ship flying as <paramref name="pilot"/>. A crew member's or droid's cycle
        /// is fired by the ship, so its yield reaches the same hold.
        /// </summary>
        /// <param name="pilot">The ship: its pilot's creature.</param>
        /// <param name="resrefs">One entry per item yielded.</param>
        /// <param name="components">Salvaged components, each with its trait; each takes its own hold entry.</param>
        public static void DeliverYield(
            uint pilot,
            IReadOnlyCollection<string> resrefs,
            IReadOnlyCollection<(string Resref, ShipComponentTraitType Trait)> components = null)
        {
            resrefs ??= Array.Empty<string>();
            components ??= Array.Empty<(string Resref, ShipComponentTraitType Trait)>();
            if (resrefs.Count == 0 && components.Count == 0)
                return;

            var byResref = resrefs
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x)
                .Select(x => (Resref: x.Key, Quantity: x.Count()))
                .ToList();

            var overflow = new List<(string Resref, int Quantity)>();
            var stored = new List<(string Resref, int Quantity)>();
            var storedComponents = new List<string>();
            var overflowComponents = new List<(string Resref, ShipComponentTraitType Trait)>();
            var holdFull = false;

            if (TryGetHold(pilot, out var property, out var categories, out var hold, out var holdIsNew))
            {
                var usedEntries = categories.Sum(x => x.Items.Count);
                foreach (var (resref, quantity) in byResref)
                {
                    var freeEntries = property.ItemStorageCount - usedEntries;
                    var stacks = hold.Items.Where(x => x.Value.Resref == resref).ToList();
                    var plan = ShipYieldRouting.Plan(
                        stacks.Select(x => x.Value.Quantity).ToList(), GetMaxStack(resref), freeEntries, quantity);

                    var kept = ApplyPlan(hold, stacks, resref, plan);
                    usedEntries += plan.NewStacks.Count;

                    var notStored = quantity - kept;
                    if (kept > 0)
                        stored.Add((resref, kept));
                    if (notStored > 0)
                    {
                        overflow.Add((resref, notStored));
                        holdFull = true;
                    }
                }

                foreach (var component in components)
                {
                    var name = property.ItemStorageCount - usedEntries > 0 ? StoreComponent(hold, component) : null;
                    if (name == null)
                    {
                        overflowComponents.Add(component);
                        holdFull = true;
                        continue;
                    }

                    usedEntries++;
                    storedComponents.Add(name);
                }

                if (stored.Count > 0 || storedComponents.Count > 0)
                {
                    if (holdIsNew)
                        SaveNewHold(pilot, property, hold);
                    else
                        DB.Set(hold);
                }
            }
            else
            {
                overflow.AddRange(byResref);
                overflowComponents.AddRange(components);
            }

            foreach (var (resref, quantity) in overflow)
            {
                for (var count = 0; count < quantity; count++)
                {
                    CreateItemOnObject(resref, pilot);
                }
            }

            foreach (var (resref, trait) in overflowComponents)
            {
                var item = CreateItemOnObject(resref, pilot);
                if (GetIsObjectValid(item))
                    ShipComponents.SetTraits(item, new[] { trait });
            }

            if (stored.Count > 0 || storedComponents.Count > 0)
            {
                var text = string.Join(", ", stored.Select(x => $"{x.Quantity}x {Cache.GetItemNameByResref(x.Resref)}")
                    .Concat(storedComponents.Select(x => $"1x {x}")));
                SendMessageToPC(pilot, ColorToken.Green($"Stored in the ship hold: {text}."));
            }

            if (holdFull)
                WarnHoldFull(pilot);
        }

        /// <summary>
        /// The pilot's active ship's property, its storage categories and its hold, when the pilot may use the hold.
        /// A hold that does not exist yet is returned unsaved (<paramref name="holdIsNew"/>).
        /// </summary>
        private static bool TryGetHold(
            uint pilot,
            out WorldProperty property,
            out List<WorldPropertyCategory> categories,
            out WorldPropertyCategory hold,
            out bool holdIsNew)
        {
            property = null;
            categories = null;
            hold = null;
            holdIsNew = false;

            if (!GetIsPC(pilot) || GetIsDM(pilot) || GetIsDMPossessed(pilot))
                return false;

            var playerId = GetObjectUUID(pilot);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null || dbPlayer.ActiveShipId == Guid.Empty.ToString())
                return false;

            var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
            if (dbShip == null || string.IsNullOrWhiteSpace(dbShip.PropertyId))
                return false;

            property = DB.Get<WorldProperty>(dbShip.PropertyId);
            if (property == null || property.ItemStorageCount <= 0)
                return false;

            var query = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), property.Id, false);
            categories = DB.Search(query).ToList();

            hold = categories.FirstOrDefault(x => x.Name == HoldCategoryName);
            var isOwner = property.OwnerPlayerId == playerId;
            if (hold == null)
            {
                // Only the owner opens a hold, the way only a player who may edit categories adds one by hand.
                if (!isOwner || categories.Count >= MaxCategories)
                    return false;

                hold = new WorldPropertyCategory
                {
                    ParentPropertyId = property.Id,
                    Name = HoldCategoryName
                };
                holdIsNew = true;
                categories.Add(hold);
                return true;
            }

            return ShipYieldRouting.CanUseHold(isOwner, HasHoldAccess(playerId, hold.Id));
        }

        private static bool HasHoldAccess(string playerId, string holdId)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), holdId, false);
            var permission = DB.Search(query).FirstOrDefault();

            return permission != null &&
                   permission.Permissions.TryGetValue(PropertyPermissionType.AccessStorage, out var access) &&
                   access;
        }

        /// <summary>Saves a new hold with every category permission for the owner, as adding a category by hand does.</summary>
        private static void SaveNewHold(uint pilot, WorldProperty property, WorldPropertyCategory hold)
        {
            var ownerPermission = new WorldPropertyPermission
            {
                PropertyId = hold.Id,
                PlayerId = property.OwnerPlayerId
            };

            foreach (var permission in Property.GetPermissionsByPropertyType(PropertyType.Category))
            {
                ownerPermission.Permissions[permission] = true;
                ownerPermission.GrantPermissions[permission] = true;
            }

            DB.Set(hold);
            DB.Set(ownerPermission);
        }

        /// <summary>Writes the plan into the hold and returns how many items it took.</summary>
        private static int ApplyPlan(
            WorldPropertyCategory hold,
            List<KeyValuePair<string, WorldPropertyItem>> stacks,
            string resref,
            ShipYieldPlan plan)
        {
            var kept = 0;
            foreach (var (index, newQuantity) in plan.TopUps)
            {
                var dbItem = stacks[index].Value;
                var item = ObjectPlugin.Deserialize(dbItem.Data);
                if (!GetIsObjectValid(item))
                    continue;

                SetItemStackSize(item, newQuantity);
                kept += newQuantity - dbItem.Quantity;
                dbItem.Quantity = newQuantity;
                dbItem.Data = ObjectPlugin.Serialize(item);
                DestroyObject(item);
            }

            var storage = GetObjectByTag(TemporaryStorageTag);
            foreach (var quantity in plan.NewStacks)
            {
                if (!GetIsObjectValid(storage))
                    break;

                var item = CreateItemOnObject(resref, storage, quantity);
                if (!GetIsObjectValid(item))
                    break;

                hold.Items[Guid.NewGuid().ToString()] = new WorldPropertyItem
                {
                    Name = GetName(item),
                    Tag = GetTag(item),
                    Resref = resref,
                    IconResref = Item.GetIconResref(item),
                    Quantity = GetItemStackSize(item),
                    Data = ObjectPlugin.Serialize(item)
                };
                kept += GetItemStackSize(item);
                DestroyObject(item);
            }

            return kept;
        }

        /// <summary>Stores one salvaged component with its trait in a new hold entry and returns its name, or null.</summary>
        private static string StoreComponent(WorldPropertyCategory hold, (string Resref, ShipComponentTraitType Trait) component)
        {
            var storage = GetObjectByTag(TemporaryStorageTag);
            if (!GetIsObjectValid(storage))
                return null;

            var item = CreateItemOnObject(component.Resref, storage);
            if (!GetIsObjectValid(item))
                return null;

            ShipComponents.SetTraits(item, new[] { component.Trait });
            var name = GetName(item);
            hold.Items[Guid.NewGuid().ToString()] = new WorldPropertyItem
            {
                Name = name,
                Tag = GetTag(item),
                Resref = component.Resref,
                IconResref = Item.GetIconResref(item),
                Quantity = 1,
                Data = ObjectPlugin.Serialize(item)
            };
            DestroyObject(item);
            return name;
        }

        /// <summary>The item's largest stack, read once per resref from a copy in the no-access storage container.</summary>
        private static int GetMaxStack(string resref)
        {
            if (_maxStackByResref.TryGetValue(resref, out var maxStack))
                return maxStack;

            var storage = GetObjectByTag(TemporaryStorageTag);
            if (!GetIsObjectValid(storage))
                return 1;

            var item = CreateItemOnObject(resref, storage);
            if (!GetIsObjectValid(item))
                return 1;

            // A stack size past the base item's limit is set to the limit.
            SetItemStackSize(item, 9999);
            maxStack = Math.Max(1, GetItemStackSize(item));
            DestroyObject(item);

            _maxStackByResref[resref] = maxStack;
            return maxStack;
        }

        /// <summary>Tells the pilot the hold is full, at most once a minute.</summary>
        private static void WarnHoldFull(uint pilot)
        {
            var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - GetLocalInt(pilot, HoldFullWarnedAtVariable) < HoldFullWarningSeconds)
                return;

            SetLocalInt(pilot, HoldFullWarnedAtVariable, now);
            SendMessageToPC(pilot, ColorToken.Red("Your ship's hold is full. Yields go to your inventory until you make room."));
        }
    }
}

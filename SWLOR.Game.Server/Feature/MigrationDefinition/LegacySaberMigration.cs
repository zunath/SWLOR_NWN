using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    /// <summary>
    /// Removes DM-built lightsabers and saberstaffs (any Lightsaber/Saberstaff
    /// base item that is not part of the craftable training saber lines or the
    /// workbench-built sabers) and refunds one Kyber Token per removed saber.
    /// Tokens are redeemed at a Lightsaber Workbench.
    /// </summary>
    internal static class LegacySaberMigration
    {
        public const string KyberTokenResref = "kyber_token";
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
        private const string StorageObjectTag = "TEMP_ITEM_STORAGE";

        private static string _serializedToken;
        private static string _tokenName;
        private static string _tokenTag;
        private static string _tokenIconResref;

        /// <summary>
        /// Sabers produced by crafting or the lightsaber workbench. These follow
        /// the established rules already and are never removed.
        /// </summary>
        private static readonly HashSet<string> CraftableSaberResrefs = new(StringComparer.OrdinalIgnoreCase)
        {
            "saber_train_1",
            "saber_train_2",
            "saber_train_3",
            "saber_train_4",
            "saber_train_5",
            "fld_trnsaber",
            "vet_trnsaber",
            "prm_trnsaber",
            "asc_trnsaber",
            "trn_saberstaff_1",
            "trn_saberstaff_2",
            "trn_saberstaff_3",
            "trn_saberstaff_4",
            "trn_saberstaff_5",
            "fld_trnsabstaff",
            "vet_trnsabstaff",
            "prm_trnsabstaff",
            "asc_trnsabstaff",
            "ls_custom",
            "ss_custom",
        };

        /// <summary>
        /// Determines whether a resref belongs to a saber players can obtain
        /// through crafting or the lightsaber workbench. These are never reclaimed.
        /// </summary>
        public static bool IsCraftableSaberResref(string resref)
        {
            return !string.IsNullOrWhiteSpace(resref) &&
                   CraftableSaberResrefs.Contains(resref);
        }

        /// <summary>
        /// Determines whether an item is a DM-built saber that must be reclaimed.
        /// </summary>
        public static bool IsLegacySaber(uint item)
        {
            if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
                return false;

            var baseItemType = GetBaseItemType(item);
            if (baseItemType != BaseItem.Lightsaber && baseItemType != BaseItem.Saberstaff)
                return false;

            return !CraftableSaberResrefs.Contains(GetResRef(item));
        }

        public static string GetSerializedToken()
        {
            return EnsureTokenTemplate() ? _serializedToken : string.Empty;
        }

        public static string GetTokenName()
        {
            return EnsureTokenTemplate() ? _tokenName : "Kyber Token";
        }

        public static string GetTokenTag()
        {
            return EnsureTokenTemplate() ? _tokenTag : KyberTokenResref;
        }

        public static string GetTokenIconResref()
        {
            return EnsureTokenTemplate() ? _tokenIconResref : string.Empty;
        }

        private static bool EnsureTokenTemplate()
        {
            if (!string.IsNullOrWhiteSpace(_serializedToken))
                return true;

            var storage = GetObjectByTag(StorageObjectTag);
            if (!GetIsObjectValid(storage))
                return false;

            var token = CreateItemOnObject(KyberTokenResref, storage);
            if (!GetIsObjectValid(token))
                return false;

            _serializedToken = ObjectPlugin.Serialize(token);
            _tokenName = GetName(token);
            _tokenTag = GetTag(token);
            _tokenIconResref = Item.GetIconResref(token);
            DestroyObject(token);

            return !string.IsNullOrWhiteSpace(_serializedToken);
        }

        /// <summary>
        /// Login sweep for the live player object. Removes legacy sabers from
        /// equipped slots, carried inventory, nested containers, and constructed
        /// droids, then grants one Kyber Token per removed saber and tells the
        /// player where the workbenches are.
        /// </summary>
        public static void MigratePlayer(uint player)
        {
            var removed = RemoveLegacySabers(player);
            if (removed <= 0)
                return;

            for (var index = 0; index < removed; index++)
            {
                CreateItemOnObject(KyberTokenResref, player);
            }

            var saberText = removed == 1 ? "lightsaber has" : "lightsabers have";
            var tokenText = removed == 1 ? "a Kyber Token" : $"{removed} Kyber Tokens";
            SendMessageToPC(player, $"Your {saberText} been reclaimed as part of the combat overhaul and {tokenText} placed in your inventory. Use each token to attune it, then visit a Lightsaber Workbench - at the Sith Academy on Korriban, the Jedi Enclave on Dantooine, or (so the rumors say) a hidden cave on Dathomir - to construct your lightsaber anew.");

            Log.Write(LogGroup.Migration, $"Reclaimed {removed} legacy saber(s) from {GetName(player)} ({GetObjectUUID(player)}) and granted {removed} Kyber Token(s).");
        }

        private static int RemoveLegacySabers(uint obj)
        {
            if (!GetIsObjectValid(obj))
                return 0;

            var removed = 0;
            var objectType = GetObjectType(obj);

            if (objectType == ObjectType.Item)
            {
                if (IsLegacySaber(obj))
                {
                    DestroyObject(obj);
                    return 1;
                }

                removed += RemoveLegacySabersFromConstructedDroid(obj, false);
            }
            else if (objectType == ObjectType.Creature)
            {
                for (var index = 0; index < NumberOfInventorySlots; index++)
                {
                    removed += RemoveLegacySabers(GetItemInSlot((InventorySlot)index, obj));
                }
            }

            if (!GetIsObjectValid(obj) || !GetHasInventory(obj))
                return removed;

            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item);)
            {
                var nextItem = GetNextItemInInventory(obj);
                removed += RemoveLegacySabers(item);
                item = nextItem;
            }

            return removed;
        }

        /// <summary>
        /// Stored-object sweep for offline surfaces. Legacy sabers nested inside
        /// containers are replaced with Kyber Tokens in place, so the owner keeps
        /// the refund wherever the saber was stored. Creature roots (DM creatures)
        /// are skipped so NPC gear stays intact. Root-level sabers are handled by
        /// the record-level swap in StoredItemDataMigration.
        /// </summary>
        public static bool MigrateStoredObject(uint obj, out int replacedCount)
        {
            replacedCount = 0;
            if (!GetIsObjectValid(obj))
                return false;

            var objectType = GetObjectType(obj);
            if (objectType == ObjectType.Creature)
                return false;

            if (objectType == ObjectType.Item)
            {
                replacedCount += RemoveLegacySabersFromConstructedDroid(obj, true);
            }

            if (GetIsObjectValid(obj) && GetHasInventory(obj))
            {
                replacedCount += ReplaceNestedSabersWithTokens(obj);
            }

            return replacedCount > 0;
        }

        private static int ReplaceNestedSabersWithTokens(uint container)
        {
            var replaced = 0;
            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item);)
            {
                var nextItem = GetNextItemInInventory(container);

                if (IsLegacySaber(item))
                {
                    DestroyObject(item);
                    CreateItemOnObject(KyberTokenResref, container);
                    replaced++;
                }
                else
                {
                    replaced += RemoveLegacySabersFromConstructedDroid(item, true);
                    if (GetHasInventory(item))
                    {
                        replaced += ReplaceNestedSabersWithTokens(item);
                    }
                }

                item = nextItem;
            }

            return replaced;
        }

        private static int RemoveLegacySabersFromConstructedDroid(uint controllerItem, bool replaceWithTokens)
        {
            var serialized = GetLocalString(controllerItem, ConstructedDroidVariable);
            if (string.IsNullOrWhiteSpace(serialized))
                return 0;

            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
            if (droid == null)
                return 0;

            var removed = 0;

            if (droid.EquippedItems != null)
            {
                foreach (var slot in droid.EquippedItems.Keys.ToList())
                {
                    if (!IsSerializedLegacySaber(droid.EquippedItems[slot]))
                        continue;

                    droid.EquippedItems.Remove(slot);
                    removed++;
                }
            }

            if (droid.Inventory != null)
            {
                foreach (var key in droid.Inventory.Keys.ToList())
                {
                    if (!IsSerializedLegacySaber(droid.Inventory[key]))
                        continue;

                    droid.Inventory.Remove(key);
                    removed++;
                }
            }

            if (removed <= 0)
                return 0;

            if (replaceWithTokens)
            {
                droid.Inventory ??= new Dictionary<string, string>();
                for (var index = 0; index < removed; index++)
                {
                    var token = GetSerializedToken();
                    if (string.IsNullOrWhiteSpace(token))
                        break;

                    droid.Inventory[Guid.NewGuid().ToString()] = token;
                }
            }

            SetLocalString(controllerItem, ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
            return removed;
        }

        private static bool IsSerializedLegacySaber(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized))
                return false;

            var obj = ObjectPlugin.Deserialize(serialized);
            if (!GetIsObjectValid(obj))
                return false;

            var isLegacySaber = IsLegacySaber(obj);
            DestroyObject(obj);
            return isLegacySaber;
        }
    }
}

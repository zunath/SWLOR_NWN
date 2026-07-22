using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.NWN.API.Engine;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class ObsoleteItemMigration
    {
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

        private static readonly HashSet<string> ObsoleteItemResRefs = new(StringComparer.OrdinalIgnoreCase)
        {
            "id_adhgren3",
            "id_alacrity",
            "id_backstab1",
            "id_backstab2",
            "id_backstab3",
            "id_bulwark",
            "id_chi1",
            "id_chi2",
            "id_chi3",
            "id_cleave",
            "id_cmoon1",
            "id_cmoon2",
            "id_cmoon3",
            "id_combenh1",
            "id_combenh2",
            "id_combenh3",
            "id_concgren3",
            "id_crippshot1",
            "id_crippshot2",
            "id_crippshot3",
            "id_crosscut1",
            "id_crosscut2",
            "id_crosscut3",
            "id_dblshot1",
            "id_dblshot2",
            "id_dblshot3",
            "id_dirtyblow",
            "id_doublehand1",
            "id_doublehand2",
            "id_doublehand3",
            "id_doublehand4",
            "id_doublehand5",
            "id_dthrust1",
            "id_dthrust2",
            "id_dthrust3",
            "id_dualwield",
            "id_elecfist1",
            "id_elecfist2",
            "id_elecfist3",
            "id_exptoss1",
            "id_exptoss2",
            "id_exptoss3",
            "id_flashgren1",
            "id_flashgren2",
            "id_flashgren3",
            "id_hardslash1",
            "id_hardslash2",
            "id_hardslash3",
            "id_innstr1",
            "id_innstr2",
            "id_iongren3",
            "id_knockdown",
            "id_kolt1",
            "id_kolt2",
            "id_kolt3",
            "id_koltgren1",
            "id_koltgren2",
            "id_koltgren3",
            "id_legsweep1",
            "id_legsweep2",
            "id_legsweep3",
            "id_medkit5",
            "id_precaim1",
            "id_precaim2",
            "id_pstab1",
            "id_pstab2",
            "id_pstab3",
            "id_ptoss1",
            "id_ptoss2",
            "id_ptoss3",
            "id_pwrattack1",
            "id_pwrattack2",
            "id_quickdraw1",
            "id_quickdraw2",
            "id_quickdraw3",
            "id_resusc3",
            "id_shbash1",
            "id_shbash2",
            "id_shbash3",
            "id_shielding4",
            "id_shmaster",
            "id_shresist1",
            "id_shresist2",
            "id_skewer1",
            "id_skewer2",
            "id_skewer3",
            "id_slam1",
            "id_slam2",
            "id_slam3",
            "id_spinwhirl1",
            "id_spinwhirl2",
            "id_spinwhirl3",
            "id_stasisf1",
            "id_stasisf2",
            "id_stasisf3",
            "id_strcobra1",
            "id_strcobra2",
            "id_strcobra3",
            "id_supweapfoc",
            "id_tranqshot1",
            "id_tranqshot2",
            "id_tranqshot3",
            // The recipes that taught the legacy single-step saber upgrade kits have
            // no equivalent in the tiered Engineering upgrade kit line (those kits are
            // never unlock-gated), so there is nothing to convert them to.
            "recipe_saberupg1",
            "recipe_staffupg1",
        };

        /// <summary>
        /// Held obsolete items that convert into their replacement instead of being
        /// destroyed outright. The legacy single-step saber/saberstaff upgrade kits
        /// were replaced by the tiered Engineering upgrade kit line, so a held kit is
        /// converted to the lowest tier of that line rather than lost.
        /// </summary>
        private static readonly Dictionary<string, string> ObsoleteItemConversions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "saber_upg1", "saber_upg2" },
            { "saberstaff_upg1", "staff_upg2" },
        };

        private static readonly Dictionary<PerkType, int> CurrentDroidInstructionMaxLevels = new()
        {
            { PerkType.AdhesiveGrenade, 2 },
            { PerkType.AdrenalStim, 3 },
            { PerkType.Antitoxin, 1 },
            { PerkType.ArcProjector, 3 },
            { PerkType.BlasterBeacon, 3 },
            { PerkType.ClusterGrenade, 1 },
            { PerkType.ConcussionGrenade, 2 },
            { PerkType.CryoSprayer, 1 },
            { PerkType.DeflectorShield, 3 },
            { PerkType.DisruptionPulse, 1 },
            { PerkType.EmergencyBunker, 1 },
            { PerkType.EmergencyCocktail, 1 },
            { PerkType.EmergencyTriage, 1 },
            { PerkType.FlashGrenade, 1 },
            { PerkType.Flamethrower, 3 },
            { PerkType.FocusStim, 2 },
            { PerkType.FragGrenade, 3 },
            { PerkType.GroupDeflector, 1 },
            { PerkType.IncendiaryField, 3 },
            { PerkType.Infusion, 2 },
            { PerkType.IonGrenade, 2 },
            { PerkType.IonLance, 3 },
            { PerkType.KillzoneBeacon, 1 },
            { PerkType.KoltoMist, 2 },
            { PerkType.MedKit, 4 },
            { PerkType.OverloadBarrage, 1 },
            { PerkType.PainSuppressant, 2 },
            { PerkType.PowerCell, 3 },
            { PerkType.Provoke, 2 },
            { PerkType.RailDart, 3 },
            { PerkType.RemoteCharge, 2 },
            { PerkType.Resuscitation, 2 },
            { PerkType.Shielding, 3 },
            { PerkType.ShockBeacon, 2 },
            { PerkType.SignalJammer, 1 },
            { PerkType.SonicBurst, 3 },
            { PerkType.ThermalDetonator, 1 },
            { PerkType.TreatmentKit, 3 },
            { PerkType.WeaponJam, 1 },
            { PerkType.WristRocket, 3 },
        };

        public static bool IsObsoleteResRef(string resref)
        {
            return !string.IsNullOrWhiteSpace(resref) &&
                   ObsoleteItemResRefs.Contains(resref);
        }

        public static bool TryGetConversionResRef(string resref, out string replacementResRef)
        {
            replacementResRef = null;
            return !string.IsNullOrWhiteSpace(resref) &&
                   ObsoleteItemConversions.TryGetValue(resref, out replacementResRef);
        }

        public static int RemoveObsoleteItemsFromObject(uint obj)
        {
            var result = new MigrationResult();
            RemoveObsoleteItemsFromObject(obj, result);
            return result.RemovedItems;
        }

        public static bool RemoveObsoleteItemsFromObject(
            uint obj,
            out int removedCount,
            out int migratedDroidPerkCount)
        {
            var result = new MigrationResult();
            RemoveObsoleteItemsFromObject(obj, result);
            removedCount = result.RemovedItems;
            migratedDroidPerkCount = result.MigratedDroidPerks;

            return result.Changed;
        }

        private static void RemoveObsoleteItemsFromObject(uint obj, MigrationResult result)
        {
            if (!GetIsObjectValid(obj))
                return;

            var objectType = GetObjectType(obj);

            if (objectType == ObjectType.Item)
            {
                var resref = GetResRef(obj);
                if (TryGetConversionResRef(resref, out var replacementResRef))
                {
                    var possessor = GetItemPossessor(obj);
                    var target = GetIsObjectValid(possessor) ? possessor : GetObjectByTag("TEMP_ITEM_STORAGE");
                    CreateItemOnObject(replacementResRef, target);
                    DestroyObject(obj);
                    result.RemovedItems++;
                    return;
                }

                if (IsObsoleteResRef(resref))
                {
                    DestroyObject(obj);
                    result.RemovedItems++;
                    return;
                }

                RemoveObsoleteItemsFromConstructedDroid(obj, result);
            }
            else if (objectType == ObjectType.Creature)
            {
                for (var index = 0; index < NumberOfInventorySlots; index++)
                {
                    RemoveObsoleteItemsFromObject(GetItemInSlot((InventorySlot)index, obj), result);
                }
            }

            if (!GetIsObjectValid(obj) || !GetHasInventory(obj))
                return;

            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item);)
            {
                var nextItem = GetNextItemInInventory(obj);
                RemoveObsoleteItemsFromObject(item, result);
                item = nextItem;
            }
        }

        public static bool RemoveObsoleteItemsFromSerializedObject(
            string serializedObject,
            out string migratedSerializedObject,
            out bool removedRoot,
            out int removedCount)
        {
            return RemoveObsoleteItemsFromSerializedObject(
                serializedObject,
                out migratedSerializedObject,
                out removedRoot,
                out removedCount,
                out _);
        }

        public static bool RemoveObsoleteItemsFromSerializedObject(
            string serializedObject,
            out string migratedSerializedObject,
            out bool removedRoot,
            out int removedCount,
            out int migratedDroidPerkCount)
        {
            migratedSerializedObject = serializedObject;
            removedRoot = false;
            removedCount = 0;
            migratedDroidPerkCount = 0;

            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            if (GetObjectType(obj) == ObjectType.Item)
            {
                var resref = GetResRef(obj);
                if (TryGetConversionResRef(resref, out var replacementResRef))
                {
                    DestroyObject(obj);
                    var tempStorage = GetObjectByTag("TEMP_ITEM_STORAGE");
                    var replacement = CreateItemOnObject(replacementResRef, tempStorage);
                    migratedSerializedObject = ObjectPlugin.Serialize(replacement);
                    DestroyObject(replacement);
                    removedCount = 1;
                    return true;
                }

                if (IsObsoleteResRef(resref))
                {
                    DestroyObject(obj);
                    removedRoot = true;
                    removedCount = 1;
                    return true;
                }
            }

            var result = new MigrationResult();
            RemoveObsoleteItemsFromObject(obj, result);
            removedCount = result.RemovedItems;
            migratedDroidPerkCount = result.MigratedDroidPerks;
            if (!result.Changed)
            {
                DestroyObject(obj);
                return false;
            }

            migratedSerializedObject = ObjectPlugin.Serialize(obj);
            DestroyObject(obj);
            return true;
        }

        private static void RemoveObsoleteItemsFromConstructedDroid(uint item, MigrationResult result)
        {
            var serialized = GetLocalString(item, ConstructedDroidVariable);
            if (string.IsNullOrWhiteSpace(serialized))
                return;

            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
            if (droid == null)
                return;

            var changed = false;
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedCPU, value => droid.SerializedCPU = value, result);
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedHead, value => droid.SerializedHead = value, result);
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedBody, value => droid.SerializedBody = value, result);
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedArms, value => droid.SerializedArms = value, result);
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedLegs, value => droid.SerializedLegs = value, result);
            changed |= RemoveObsoleteItemsFromDroidDictionary(droid.EquippedItems, result);
            changed |= RemoveObsoleteItemsFromDroidDictionary(droid.Inventory, result);

            droid.ActivePerks ??= new List<DroidPerk>();
            var activeInstructionProperties = LoadDroidInstructionProperties(item);
            var droidStateChanged = MergeDroidPerks(droid.ActivePerks, activeInstructionProperties);

            droidStateChanged |= NormalizeDroidPerks(droid);
            droidStateChanged |= SyncDroidInstructionProperties(item, droid.ActivePerks);
            changed |= droidStateChanged;

            if (!changed)
                return;

            SetLocalString(item, ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
            if (droidStateChanged)
                result.MigratedDroidPerks++;
        }

        private static bool RemoveObsoleteItemsFromDroidField(
            string serializedObject,
            Action<string> setSerializedObject,
            MigrationResult result)
        {
            if (!RemoveObsoleteItemsFromSerializedObject(
                    serializedObject,
                    out var migratedSerializedObject,
                    out var removedRoot,
                    out var serializedRemovedCount,
                    out var serializedDroidPerkCount))
                return false;

            setSerializedObject(removedRoot ? string.Empty : migratedSerializedObject);
            result.RemovedItems += serializedRemovedCount;
            result.MigratedDroidPerks += serializedDroidPerkCount;
            return true;
        }

        private static bool RemoveObsoleteItemsFromDroidDictionary<TKey>(
            Dictionary<TKey, string> serializedObjects,
            MigrationResult result)
        {
            if (serializedObjects == null || serializedObjects.Count <= 0)
                return false;

            var changed = false;
            foreach (var key in serializedObjects.Keys.ToList())
            {
                if (!RemoveObsoleteItemsFromSerializedObject(
                        serializedObjects[key],
                        out var migratedSerializedObject,
                        out var removedRoot,
                        out var serializedRemovedCount,
                        out var serializedDroidPerkCount))
                    continue;

                if (removedRoot)
                    serializedObjects.Remove(key);
                else
                    serializedObjects[key] = migratedSerializedObject;

                result.RemovedItems += serializedRemovedCount;
                result.MigratedDroidPerks += serializedDroidPerkCount;
                changed = true;
            }

            return changed;
        }

        private static List<DroidPerk> LoadDroidInstructionProperties(uint item)
        {
            var result = new List<DroidPerk>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.DroidInstruction)
                    continue;

                var perkType = (PerkType)GetItemPropertySubType(ip);
                var level = GetItemPropertyCostTableValue(ip);
                if (TryNormalizeDroidPerk(new DroidPerk(perkType, level), out var normalized))
                    result.Add(normalized);
            }

            return result;
        }

        private static bool NormalizeDroidPerks(ConstructedDroid droid)
        {
            droid.LearnedPerks ??= new List<DroidPerk>();
            droid.ActivePerks ??= new List<DroidPerk>();

            var changed = NormalizeDroidPerkList(droid.LearnedPerks, out var learnedPerks);
            changed |= NormalizeDroidPerkList(droid.ActivePerks, out var activePerks);
            changed |= MergeDroidPerks(learnedPerks, activePerks);

            droid.LearnedPerks = learnedPerks;
            droid.ActivePerks = activePerks;
            return changed;
        }

        private static bool NormalizeDroidPerkList(IReadOnlyList<DroidPerk> source, out List<DroidPerk> normalized)
        {
            normalized = new List<DroidPerk>();
            var seen = new HashSet<(PerkType Perk, int Level)>();

            foreach (var perk in source)
            {
                if (!TryNormalizeDroidPerk(perk, out var migratedPerk))
                    continue;

                if (!seen.Add((migratedPerk.Perk, migratedPerk.Level)))
                    continue;

                normalized.Add(migratedPerk);
            }

            return !AreEqualDroidPerks(source, normalized);
        }

        private static bool TryNormalizeDroidPerk(DroidPerk perk, out DroidPerk normalized)
        {
            normalized = null;
            if (perk == null ||
                perk.Perk == PerkType.Invalid ||
                perk.Level <= 0 ||
                !CurrentDroidInstructionMaxLevels.TryGetValue(perk.Perk, out var maxLevel))
            {
                return false;
            }

            normalized = new DroidPerk(perk.Perk, Math.Min(perk.Level, maxLevel));
            return true;
        }

        private static bool MergeDroidPerks(List<DroidPerk> target, IEnumerable<DroidPerk> source)
        {
            var changed = false;
            var existing = target
                .Select(x => (x.Perk, x.Level))
                .ToHashSet();

            foreach (var perk in source)
            {
                if (!existing.Add((perk.Perk, perk.Level)))
                    continue;

                target.Add(new DroidPerk(perk.Perk, perk.Level));
                changed = true;
            }

            return changed;
        }

        private static bool SyncDroidInstructionProperties(uint item, IReadOnlyList<DroidPerk> activePerks)
        {
            var existingProperties = new List<ItemProperty>();
            var existingPerks = new List<DroidPerk>();

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.DroidInstruction)
                    continue;

                existingProperties.Add(ip);
                existingPerks.Add(new DroidPerk((PerkType)GetItemPropertySubType(ip), GetItemPropertyCostTableValue(ip)));
            }

            if (AreEqualDroidPerks(existingPerks, activePerks))
                return false;

            foreach (var ip in existingProperties)
            {
                RemoveItemProperty(item, ip);
            }

            foreach (var perk in activePerks)
            {
                AddItemProperty(
                    DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.DroidInstruction, (int)perk.Perk, perk.Level),
                    item);
            }

            return true;
        }

        private static bool AreEqualDroidPerks(IReadOnlyList<DroidPerk> left, IReadOnlyList<DroidPerk> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (left[index].Perk != right[index].Perk ||
                    left[index].Level != right[index].Level)
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class MigrationResult
        {
            public int RemovedItems { get; set; }
            public int MigratedDroidPerks { get; set; }

            public bool Changed => RemovedItems > 0 || MigratedDroidPerks > 0;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using NWN.Native.API;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Runs the boot-time character migrations (<c>ICharacterMigration</c>) over a creature saved from the engine,
    /// the way the server's character upgrade runs them over a character file: the creature is serialized, the
    /// migration edits the saved data and a copy of the player record without the engine, and the result is
    /// deserialized for the test to assert on. The game types are internal to the game assembly, so they are
    /// reached by reflection.
    /// </summary>
    public static class SavedCharacterMigration
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private const string ServiceNamespace = "SWLOR.Game.Server.Service.MigrationService.";
        private const string MigrationNamespace = "SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration.";

        private static readonly Assembly Game = typeof(Migration).Assembly;

        private static Type GameType(string name) =>
            Game.GetType(name) ?? throw new InvalidOperationException($"The game assembly has no type '{name}'.");

        /// <summary>A character file after migrations, with the record changes made beside it.</summary>
        public sealed class Upgraded
        {
            internal Upgraded(byte[] file, Player record, IReadOnlyList<UpgradedOverflowItem> overflow, IReadOnlyList<string> notices)
            {
                File = file;
                Record = record;
                Overflow = overflow;
                Notices = notices;
            }

            /// <summary>The migrated character file (BIC).</summary>
            public byte[] File { get; }

            /// <summary>The migrated copy of the player record. The record stored in the database is untouched.</summary>
            public Player Record { get; }

            /// <summary>Items that had no room in the character's inventory.</summary>
            public IReadOnlyList<UpgradedOverflowItem> Overflow { get; }

            /// <summary>Messages the player is shown at their next login.</summary>
            public IReadOnlyList<string> Notices { get; }

            public string Data => Convert.ToBase64String(File);

            /// <summary>The migration checkpoint saved in the character file (its root <c>PlayerFileVer</c> field).</summary>
            public int FileVersion => ReadFileVersion(File);
        }

        /// <summary>An item the migration moved to overflow storage.</summary>
        public sealed class UpgradedOverflowItem
        {
            public string Name { get; init; }
            public string ResRef { get; init; }
            public string Tag { get; init; }
            public int Quantity { get; init; }

            /// <summary>The item as a standalone saved object, base64 as <c>ObjectPlugin.Serialize</c> writes items.</summary>
            public string Data { get; init; }
        }

        /// <summary>
        /// The save file of a creature as the server vault keeps a character (BIC V3.2): the engine's player save,
        /// which writes the level history, quickbar, hit points, inventory positions, item locals and object IDs a
        /// character file has, and no creature locals. Runs the save with the player flag set, as the engine does when
        /// it saves a connected player. (The engine's export mode drops item locals, which character files keep.)
        /// </summary>
        public static unsafe byte[] SaveFile(uint creature)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
            var wasPlayer = native.m_bPlayerCharacter;
            using var file = new CResGFF();
            using var root = new CResStruct();
            using var type = new CExoString("BIC ");
            using var version = new CExoString("V3.2");
            if (file.CreateGFFFile(root, type, version) == 0)
                throw new InvalidOperationException("The character export could not be created.");
            try
            {
                native.m_bPlayerCharacter = 1;
                if (native.SaveCreature(file, root, 0, 0, 0, 1) == 0)
                    throw new InvalidOperationException("The creature could not be exported as a character.");
            }
            finally { native.m_bPlayerCharacter = wasPlayer; }
            fixed (byte* label = Encoding.ASCII.GetBytes("IsPC\0"))
                file.WriteFieldBYTE(root, 1, label);

            void* data = null;
            var length = 0;
            if (file.WriteGFFToPointer(&data, &length) == 0)
                throw new InvalidOperationException("The character export could not be packed.");
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy((IntPtr)data, bytes, 0, length);
            // A resource that adopts the packed buffer frees it with the resource.
            using var owner = new CResGFF();
            owner.GetDataFromPointer(data, length, true);
            return bytes;
        }

        /// <summary>A deep copy of a player record, so a migration changes the copy and the database keeps the original.</summary>
        public static Player CopyRecord(Player record) =>
            JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(record));

        /// <summary>Loads the game data saved-character migrations read. Main thread only; runs inside an engine script.</summary>
        public static void EnsureDataLoaded()
        {
            var data = GameType(ServiceNamespace + "SavedCharacterData");
            if ((bool)data.GetProperty("IsLoaded", Members).GetValue(null))
                return;
            data.GetMethod("Load", Members, null, Type.EmptyTypes, null).Invoke(null, null);
        }

        /// <summary>The newest character migration version, the version a fully upgraded character holds.</summary>
        public static int LatestVersion =>
            (int)GameType(ServiceNamespace + "CharacterFileUpgrade").GetProperty("LatestVersion", Members).GetValue(null);

        /// <summary>The version of every character migration, in the order the upgrade runs them.</summary>
        public static IReadOnlyList<int> Versions =>
            ((IEnumerable<object>)GameType(ServiceNamespace + "CharacterFileUpgrade").GetProperty("Migrations", Members).GetValue(null))
            .Select(migration => (int)migration.GetType().GetProperty("Version").GetValue(migration)).ToList();

        /// <summary>
        /// Saves <paramref name="creature"/>, then runs the named character migrations over the saved file and a copy
        /// of <paramref name="record"/> in order, as the boot-time upgrade does. Names are the migration classes,
        /// for example <c>_22_ReviseLootStats</c>. Must run inside an engine script (<c>ExecuteInCreatureContextAsync</c>).
        /// </summary>
        public static Upgraded Run(uint creature, Player record, params string[] migrations) =>
            Run(SaveFile(creature), record, migrations);

        /// <summary>As <see cref="Run(uint, Player, string[])"/>, over a character file that was already saved.</summary>
        public static Upgraded Run(byte[] file, Player record, params string[] migrations) =>
            Run(file, record, true, migrations);

        /// <summary>
        /// Runs the named migrations over a saved file and a copy of the record. With
        /// <paramref name="appliesRecordEffects"/> false the record already holds what the migrations change in it, as
        /// when an earlier upgrade saved the record but not the file: the file is migrated again and the record is left alone.
        /// </summary>
        public static Upgraded Run(byte[] file, Player record, bool appliesRecordEffects, string[] migrations)
        {
            EnsureDataLoaded();
            var copy = CopyRecord(record);
            var characterType = GameType(ServiceNamespace + "SavedCharacter");
            var character = Activator.CreateInstance(characterType, Members, null, new object[] { file, copy }, null);
            GameType(MigrationNamespace + "SavedCharacterLoad").GetMethod("Apply", Members).Invoke(null, new[] { character });
            characterType.GetProperty("AppliesRecordEffects", Members).SetValue(character, appliesRecordEffects);
            foreach (var name in migrations)
            {
                var migration = Activator.CreateInstance(GameType(MigrationNamespace + name));
                migration.GetType().GetMethod("Migrate", Members).Invoke(migration, new[] { character });
                if (!appliesRecordEffects)
                    continue;
                GameType(ServiceNamespace + "ICharacterMigration").GetMethod("MigratePlayerData", Members)
                    .Invoke(migration, new object[] { copy });
            }
            var overflow = ReadOverflow((IEnumerable<object>)characterType.GetProperty("Overflow", Members).GetValue(character));
            var notices = (List<string>)characterType.GetProperty("Notices", Members).GetValue(character);
            var migrated = (byte[])characterType.GetMethod("Serialize", Members).Invoke(character, null);
            return new Upgraded(migrated, copy, overflow, notices.ToList());
        }

        /// <summary>
        /// The server's own character upgrade (<c>CharacterFileUpgrade.Upgrade</c>) over a saved file and a copy of the
        /// record: every migration the file or the record is behind on, judged by the file's saved checkpoint
        /// (<c>PLAYER_MIGRATION_VERSION</c> on the creature when it was saved) and <see cref="Player.Version"/>.
        /// Returns null when the character is current.
        /// </summary>
        public static Upgraded Upgrade(byte[] file, Player record)
        {
            EnsureDataLoaded();
            var copy = CopyRecord(record);
            var result = GameType(ServiceNamespace + "CharacterFileUpgrade").GetMethod("Upgrade", Members)
                .Invoke(null, new object[] { file, copy, null });
            if (result == null)
                return null;
            var type = result.GetType();
            var overflow = ReadOverflow((IEnumerable<object>)type.GetProperty("Overflow").GetValue(result));
            var upgradedRecord = (Player)type.GetProperty("Record").GetValue(result);
            return new Upgraded((byte[])type.GetProperty("File").GetValue(result), upgradedRecord, overflow,
                (upgradedRecord.UpgradeNotices ?? new List<string>()).ToList());
        }

        private static List<UpgradedOverflowItem> ReadOverflow(IEnumerable<object> items) => items
            .Select(item => new UpgradedOverflowItem
            {
                Name = (string)item.GetType().GetProperty("Name").GetValue(item),
                ResRef = (string)item.GetType().GetProperty("Resref").GetValue(item),
                Tag = (string)item.GetType().GetProperty("Tag").GetValue(item),
                Quantity = (int)item.GetType().GetProperty("Quantity").GetValue(item),
                Data = (string)item.GetType().GetProperty("Data").GetValue(item),
            }).ToList();

        /// <summary>Loads a migrated character file as a creature; the context destroys it when the test ends.</summary>
        public static uint Load(EngineTestContext ctx, Upgraded upgraded)
        {
            var creature = ObjectPlugin.Deserialize(upgraded.Data);
            ctx.Assert(GetIsObjectValid(creature), "The migrated character file loads");
            // The file's IsPC flag loads the creature as a player, which the engine will not destroy; tests only read it.
            NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature().m_bPlayerCharacter = 0;
            ctx.Track(creature);
            return creature;
        }

        /// <summary>
        /// The items directly in a creature's or container's inventory: not worn, and not inside a carried container
        /// (the engine's inventory loop on a creature also returns the contents of its containers).
        /// </summary>
        public static List<uint> Carried(uint holder)
        {
            var items = new List<uint>();
            for (var item = GetFirstItemInInventory(holder); GetIsObjectValid(item); item = GetNextItemInInventory(holder))
                if (GetItemPossessor(item, true) == holder)
                    items.Add(item);
            return items;
        }

        /// <summary>Every item a creature wears or carries, containers' contents included, worn items first.</summary>
        public static List<uint> AllItems(uint creature)
        {
            var items = new List<uint>();
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            {
                var worn = GetItemInSlot((InventorySlot)slot, creature);
                if (GetIsObjectValid(worn))
                    items.Add(worn);
            }
            void Expand(uint holder)
            {
                foreach (var item in Carried(holder))
                {
                    items.Add(item);
                    if (GetHasInventory(item))
                        Expand(item);
                }
            }
            Expand(creature);
            return items;
        }

        /// <summary>Every item with this resref that a creature wears or carries, containers' contents included.</summary>
        public static List<uint> FindItems(uint creature, string resref) =>
            AllItems(creature).Where(item => GetResRef(item) == resref).ToList();

        /// <summary>The first item with this resref that a creature wears or carries, or <c>OBJECT_INVALID</c>.</summary>
        public static uint FindItem(uint creature, string resref) =>
            FindItems(creature, resref).DefaultIfEmpty(OBJECT_INVALID).First();

        /// <summary>The migration checkpoint a character file holds, or 0 when it has none.</summary>
        public static int ReadFileVersion(byte[] file)
        {
            var characterType = GameType(ServiceNamespace + "SavedCharacter");
            var character = Activator.CreateInstance(characterType, Members, null, new object[] { file, null }, null);
            return (int)characterType.GetMethod("ReadInteger", Members).Invoke(character, new object[] { "PlayerFileVer" });
        }

        /// <summary>
        /// The server's boot-time pass over every character file in the server vault (<c>CharacterFileUpgrade.UpgradeServerVault</c>).
        /// Returns false when any character could not be upgraded. Must run inside an engine script.
        /// </summary>
        public static bool UpgradeServerVault() =>
            (bool)GameType(ServiceNamespace + "CharacterFileUpgrade").GetMethod("UpgradeServerVault", Members).Invoke(null, null);
    }
}

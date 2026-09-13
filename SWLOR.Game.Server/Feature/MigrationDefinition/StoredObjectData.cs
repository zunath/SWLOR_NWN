using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    /// <summary>
    /// Reads saved object metadata without instantiating native objects, and edits
    /// only the inventory lists when migrating a saved creature. Native loading
    /// can reject retired appearances and discard unrelated saved state, neither
    /// of which should prevent an item migration or rewrite the creature itself.
    /// GFF scalar payloads remain raw bytes so names, effects and unknown fields
    /// do not pass through text or gameplay-model conversions.
    /// </summary>
    internal sealed class StoredObjectData
    {
        internal const string IdentityMarkerPrefix = "MIGRATION_IDENTITY_";
        internal const string EquipmentMarkerPrefix = "MIGRATION_EQUIPMENT_";
        private readonly byte[] _header;
        private readonly Node _root;
        private string _equipmentMarker;
        private readonly Dictionary<int, Node> _savedEquipment = new();
        private string _identityMarker;
        private readonly Dictionary<int, Field> _savedIdentities = new();
        private bool IsCreature => Encoding.ASCII.GetString(_header, 0, 4) is "BIC " or "UTC ";

        private sealed class Node
        {
            public uint Type;
            public List<Field> Fields = new();
        }

        private sealed class Field
        {
            public uint Type;
            public byte[] Label;
            public byte[] Data;
            public List<Node> Children;
            public string Name => Encoding.ASCII.GetString(Label).TrimEnd('\0');
        }

        private StoredObjectData(byte[] data)
        {
            if (data.Length < 56 || Encoding.ASCII.GetString(data, 4, 4) != "V3.2")
                throw new InvalidDataException("Unsupported saved creature GFF header.");
            _header = data.AsSpan(0, 8).ToArray();
            uint Read(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
            var sections = new byte[6][];
            var recordSizes = new[] { 12, 12, 16, 1, 1, 1 };
            for (var i = 0; i < sections.Length; i++)
            {
                var offset = checked((int)Read(8 + i * 8));
                var size = checked((int)Read(12 + i * 8) * recordSizes[i]);
                sections[i] = data.AsSpan(offset, size).ToArray();
            }
            uint At(int section, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(sections[section].AsSpan(offset, 4));
            var nodes = new Dictionary<uint, Node>();
            var visiting = new HashSet<uint>();
            Node ReadNode(uint index, int depth)
            {
                if (depth > 128 || visiting.Contains(index))
                    throw new InvalidDataException("Recursive saved creature GFF structure.");
                if (nodes.TryGetValue(index, out var cached))
                    return cached;
                visiting.Add(index);
                var offset = checked((int)index * 12);
                var node = new Node { Type = At(0, offset) };
                nodes[index] = node;
                var first = At(0, offset + 4);
                var count = At(0, offset + 8);
                for (var i = 0u; i < count; i++)
                {
                    var fieldIndex = count == 1 ? first : At(4, checked((int)(first + i * 4)));
                    var fieldOffset = checked((int)fieldIndex * 12);
                    var type = At(1, fieldOffset);
                    var label = At(1, fieldOffset + 4);
                    var value = At(1, fieldOffset + 8);
                    var field = new Field
                    {
                        Type = type,
                        Label = sections[2].AsSpan(checked((int)label * 16), 16).ToArray()
                    };
                    if (type == 14)
                        field.Children = new List<Node> { ReadNode(value, depth + 1) };
                    else if (type == 15)
                    {
                        field.Children = new List<Node>();
                        var listOffset = checked((int)value);
                        var listCount = At(5, listOffset);
                        for (var j = 0u; j < listCount; j++)
                            field.Children.Add(ReadNode(At(5, checked(listOffset + 4 + (int)j * 4)), depth + 1));
                    }
                    else if (type <= 5 || type == 8)
                        field.Data = sections[1].AsSpan(fieldOffset + 8, 4).ToArray();
                    else
                    {
                        var dataOffset = checked((int)value);
                        var size = type switch
                        {
                            6 or 7 or 9 => 8,
                            10 or 12 or 13 => checked(4 + (int)At(3, dataOffset)),
                            11 => 1 + sections[3][dataOffset],
                            16 => 16,
                            17 => 12,
                            _ => throw new InvalidDataException($"Unsupported saved GFF field type {type}.")
                        };
                        field.Data = sections[3].AsSpan(dataOffset, size).ToArray();
                    }
                    node.Fields.Add(field);
                }
                visiting.Remove(index);
                return node;
            }
            _root = ReadNode(0, 0);
        }

        /// <summary>
        /// Parses BIC or UTC data without native instantiation; NPC archives can also use the BIC file type.
        /// </summary>
        public static StoredObjectData ReadCreature(string data)
        {
            // NWNX serializes NPCs as BIC too; file type is not a player flag.
            if (!IsCreatureData(data))
                return null;
            return new StoredObjectData(Convert.FromBase64String(data));
        }

        /// <summary>
        /// Parses inventory-bearing archives so nested identities can survive native loading alongside live copies.
        /// </summary>
        public static StoredObjectData ReadInventory(string data)
        {
            var document = new StoredObjectData(Convert.FromBase64String(data));
            return document.IsCreature || document._root.Fields.Any(IsInventoryList) ? document : null;
        }

        /// <summary>
        /// Identifies GFF lists that contain carried or equipped item objects.
        /// </summary>
        private static bool IsInventoryList(Field field) => field.Type == 15 &&
            (field.Name == "ItemList" || field.Name == "Equip_ItemList");

        /// <summary>
        /// Traverses nested saved inventory, optionally excluding items and subtrees intentionally retired by conversion.
        /// </summary>
        private static IEnumerable<Node> InventoryItems(Node root, bool skipRetired)
        {
            foreach (var list in root.Fields.Where(IsInventoryList))
            foreach (var item in list.Children)
            {
                var resref = item.Fields.SingleOrDefault(x => x.Name == "TemplateResRef" && x.Type == 11);
                var name = resref == null ? "" : Encoding.ASCII.GetString(resref.Data.AsSpan(1));
                if (skipRetired && (ObsoleteItemMigration.IsObsoleteResRef(name) ||
                    ObsoleteItemMigration.TryGetConversionResRef(name, out _))) continue;
                yield return item;
                foreach (var nested in InventoryItems(item, skipRetired)) yield return nested;
            }
        }

        /// <summary>
        /// Adds a temporary integer local that tracks a saved item independently of native UUID reassignment.
        /// </summary>
        private static void AddMarker(Node item, string name, int id)
        {
            var variables = item.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
            if (variables == null)
            {
                variables = MakeField(15, "VarTable", children: new List<Node>());
                item.Fields.Add(variables);
            }
            var text = Encoding.UTF8.GetBytes(name);
            var textData = new byte[4 + text.Length];
            BinaryPrimitives.WriteInt32LittleEndian(textData, text.Length);
            text.CopyTo(textData, 4);
            variables.Children.Add(new Node
            {
                Fields = new List<Field>
                {
                    MakeField(10, "Name", textData),
                    MakeField(4, "Type", BitConverter.GetBytes(1u)),
                    MakeField(5, "Value", BitConverter.GetBytes(id))
                }
            });
        }

        /// <summary>
        /// Reads a typed integer local directly from saved GFF without claiming its native object identity.
        /// </summary>
        public static int ReadRootInteger(string data, string name)
        {
            var document = new StoredObjectData(Convert.FromBase64String(data));
            var variables = document._root.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
            if (variables == null) return 0;
            foreach (var variable in variables.Children)
            {
                var variableName = variable.Fields.SingleOrDefault(x => x.Name == "Name" && x.Type == 10);
                if (variableName == null || Encoding.UTF8.GetString(variableName.Data.AsSpan(4)) != name)
                    continue;
                var variableType = variable.Fields.SingleOrDefault(x => x.Name == "Type" && x.Type == 4);
                var value = variable.Fields.SingleOrDefault(x => x.Name == "Value" && x.Type == 5);
                if (variableType != null && BinaryPrimitives.ReadUInt32LittleEndian(variableType.Data) == 1 && value != null)
                    return BinaryPrimitives.ReadInt32LittleEndian(value.Data);
            }
            return 0;
        }

        /// <summary>
        /// Restores the archived root UUID in saved bytes without modifying another live object that holds the same UUID.
        /// </summary>
        public static string PreserveRootIdentity(string original, string migrated)
        {
            var source = new StoredObjectData(Convert.FromBase64String(original));
            var identity = source._root.Fields.SingleOrDefault(x => x.Name == "UUID" && x.Type == 10);
            if (identity == null) return migrated;

            var target = new StoredObjectData(Convert.FromBase64String(migrated));
            var index = target._root.Fields.FindIndex(x => x.Name == "UUID");
            if (index >= 0)
            {
                var current = target._root.Fields[index];
                if (current.Type == identity.Type && current.Data.SequenceEqual(identity.Data))
                    return migrated;
                target._root.Fields[index] = identity;
            }
            else target._root.Fields.Add(identity);

            // A live item can hold the same identity as an archived droid copy.
            // Preserve the saved field without changing either native object's
            // UUID registration or modifying the live player's item.
            return target.Serialize();
        }

        /// <summary>
        /// Recognizes the base64 signatures emitted for saved BIC and UTC objects.
        /// </summary>
        public static bool IsCreatureData(string data) =>
            data.StartsWith("QklD", StringComparison.Ordinal) || data.StartsWith("VVRD", StringComparison.Ordinal);

        public int Appearance
        {
            get
            {
                var field = _root.Fields.SingleOrDefault(x => x.Name == "Appearance_Type");
                return field?.Type == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(field.Data) : -1;
            }
        }

        /// <summary>
        /// Produces a loadable appearance override while leaving the original saved appearance bytes intact.
        /// </summary>
        public string WithTemporaryAppearance(ushort appearance)
        {
            var field = _root.Fields.Single(x => x.Name == "Appearance_Type" && x.Type == 2);
            var original = field.Data;
            field.Data = new byte[4];
            BinaryPrimitives.WriteUInt16LittleEndian(field.Data, appearance);
            try { return Serialize(); }
            finally { field.Data = original; }
        }

        /// <summary>
        /// Marks retained inventory and equipment in a disposable GFF copy before native loading can alter identities or slots.
        /// </summary>
        public string PrepareForNativeLoad(ushort? appearance)
        {
            // Loading can unequip saved weapons because the current model cannot
            // wield them. Track individual items, including identical un-UUIDed
            // copies, without changing their permanent identity or source data.
            var copy = new StoredObjectData(Convert.FromBase64String(Serialize()));
            _savedIdentities.Clear();
            _identityMarker = IdentityMarkerPrefix + Guid.NewGuid().ToString("N");
            foreach (var item in InventoryItems(copy._root, true).Distinct())
            {
                var identity = item.Fields.SingleOrDefault(x => x.Name == "UUID" && x.Type == 10);
                if (identity == null) continue;
                var id = _savedIdentities.Count + 1;
                _savedIdentities.Add(id, identity);
                AddMarker(item, _identityMarker, id);
            }
            var equipment = copy._root.Fields.SingleOrDefault(x => x.Name == "Equip_ItemList" && x.Type == 15);
            _savedEquipment.Clear();
            _equipmentMarker = EquipmentMarkerPrefix + Guid.NewGuid().ToString("N");
            if (equipment != null)
            {
                foreach (var item in equipment.Children)
                {
                    // The ammunition migration deliberately changes these slots.
                    if (item.Type == 2048 || item.Type == 4096) continue;
                    var resref = item.Fields.SingleOrDefault(x => x.Name == "TemplateResRef" && x.Type == 11);
                    var name = resref == null ? "" : Encoding.ASCII.GetString(resref.Data.AsSpan(1));
                    if (ObsoleteItemMigration.IsObsoleteResRef(name) ||
                        ObsoleteItemMigration.TryGetConversionResRef(name, out _)) continue;

                    var id = _savedEquipment.Count + 1;
                    _savedEquipment.Add(id, item);
                    AddMarker(item, _equipmentMarker, id);
                }
            }
            return appearance.HasValue ? copy.WithTemporaryAppearance(appearance.Value) : copy.Serialize();
        }

        /// <summary>
        /// Constructs a GFF field with its fixed-width label and raw scalar payload or child list.
        /// </summary>
        private static Field MakeField(uint type, string name, byte[] data = null, List<Node> children = null)
        {
            var label = new byte[16];
            Encoding.ASCII.GetBytes(name).CopyTo(label, 0);
            return new Field { Type = type, Label = label, Data = data, Children = children };
        }

        /// <summary>
        /// Restores retained equipment to its archived slots while allowing intentional ammunition-slot conversion.
        /// </summary>
        private void RestoreSavedEquipment(StoredObjectData migrated)
        {
            if (_savedEquipment.Count == 0) return;
            var lists = migrated._root.Fields.Where(x => x.Type == 15 &&
                (x.Name == "ItemList" || x.Name == "Equip_ItemList")).ToList();
            var restored = new Dictionary<int, Node>();
            foreach (var list in lists)
            foreach (var item in list.Children.ToArray())
            {
                var variables = item.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
                if (variables == null) continue;
                var marker = variables.Children.SingleOrDefault(variable => variable.Fields.Any(field =>
                    field.Name == "Name" && field.Type == 10 && Encoding.UTF8.GetString(field.Data.AsSpan(4)) == _equipmentMarker));
                if (marker == null) continue;
                var value = marker.Fields.Single(x => x.Name == "Value" && x.Type == 5);
                var id = BinaryPrimitives.ReadInt32LittleEndian(value.Data);
                if (!_savedEquipment.TryGetValue(id, out var original) || !restored.TryAdd(id, item))
                    throw new InvalidDataException("Saved equipment identity was duplicated or lost during migration.");
                variables.Children.Remove(marker);
                if (variables.Children.Count == 0) item.Fields.Remove(variables);
                list.Children.Remove(item);
                if (list.Name != "Equip_ItemList" || item.Type != original.Type)
                {
                    // Repository coordinates only exist on stowed items. Restore
                    // their original presence and raw values along with the slot.
                    bool RepositoryField(Field field) => field.Name.StartsWith("Repos_", StringComparison.OrdinalIgnoreCase);
                    item.Fields.RemoveAll(field => RepositoryField(field));
                    item.Fields.AddRange(original.Fields.Where(RepositoryField));
                    item.Type = original.Type;
                }
            }
            if (restored.Count != _savedEquipment.Count)
                throw new InvalidDataException("A retained saved equipment item disappeared during migration.");
            var target = lists.SingleOrDefault(x => x.Name == "Equip_ItemList");
            if (target == null)
            {
                target = MakeField(15, "Equip_ItemList", children: new List<Node>());
                migrated._root.Fields.Add(target);
            }
            foreach (var item in restored.Values)
            {
                if (target.Children.Any(other => (other.Type & item.Type) != 0))
                    throw new InvalidDataException("The original saved equipment slot is occupied after migration.");
                target.Children.Add(item);
            }
        }

        /// <summary>
        /// Merges migrated inventory with original creature state and restores saved item identities before writing the result.
        /// </summary>
        public string CopyMigratedInventory(string migrated)
        {
            var source = new StoredObjectData(Convert.FromBase64String(migrated));
            if (IsCreature && !source.IsCreature)
                throw new InvalidDataException("Migrated object is not a saved creature.");
            RestoreSavedEquipment(source);
            RestoreSavedIdentities(source);
            // Item containers retain their migrated root properties; only a
            // creature needs its unrelated original root state transplanted.
            if (!IsCreature) return source.Serialize();
            foreach (var name in new[] { "ItemList", "Equip_ItemList" })
            {
                var replacement = source._root.Fields.SingleOrDefault(x => x.Name == name);
                if (replacement != null && replacement.Type != 15)
                    throw new InvalidDataException("Saved creature inventory is not a GFF list.");
                var index = _root.Fields.FindIndex(x => x.Name == name);
                if (index >= 0)
                {
                    if (replacement == null) _root.Fields.RemoveAt(index);
                    else _root.Fields[index] = replacement;
                }
                else if (replacement != null)
                    _root.Fields.Add(replacement);
            }
            return Serialize();
        }

        /// <summary>
        /// Restores each retained UUID from its unique marker and fails if an expected item was lost or duplicated.
        /// </summary>
        private void RestoreSavedIdentities(StoredObjectData migrated)
        {
            if (_savedIdentities.Count == 0) return;
            var restored = new HashSet<int>();
            foreach (var item in InventoryItems(migrated._root, false).Distinct())
            {
                var variables = item.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
                if (variables == null) continue;
                var marker = variables.Children.SingleOrDefault(variable => variable.Fields.Any(field =>
                    field.Name == "Name" && field.Type == 10 && Encoding.UTF8.GetString(field.Data.AsSpan(4)) == _identityMarker));
                if (marker == null) continue;
                var value = marker.Fields.Single(x => x.Name == "Value" && x.Type == 5);
                var id = BinaryPrimitives.ReadInt32LittleEndian(value.Data);
                if (!_savedIdentities.TryGetValue(id, out var identity) || !restored.Add(id))
                    throw new InvalidDataException("Saved inventory identity was duplicated during migration.");
                variables.Children.Remove(marker);
                if (variables.Children.Count == 0) item.Fields.Remove(variables);
                item.Fields.RemoveAll(x => x.Name == "UUID");
                item.Fields.Add(identity);
            }
            if (restored.Count != _savedIdentities.Count)
                throw new InvalidDataException("A retained saved inventory identity disappeared during migration.");
        }

        /// <summary>
        /// Writes the edited GFF graph while retaining untouched scalar payloads and shared structure references.
        /// </summary>
        private string Serialize()
        {
            var structures = new List<(uint Type, uint Offset, uint Count)>();
            var fields = new List<(uint Type, uint Label, uint Value)>();
            var labels = new List<byte[]>();
            var labelIds = new Dictionary<string, uint>();
            var nodeIds = new Dictionary<Node, uint>();
            using var payload = new MemoryStream();
            using var fieldIndices = new MemoryStream();
            using var listIndices = new MemoryStream();
            using var fieldWriter = new BinaryWriter(fieldIndices, Encoding.UTF8, true);
            using var listWriter = new BinaryWriter(listIndices, Encoding.UTF8, true);
            uint WriteNode(Node node)
            {
                if (nodeIds.TryGetValue(node, out var existing)) return existing;
                var id = checked((uint)structures.Count);
                nodeIds[node] = id;
                structures.Add(default);
                var indices = new List<uint>();
                foreach (var field in node.Fields)
                {
                    var labelKey = Convert.ToBase64String(field.Label);
                    if (!labelIds.TryGetValue(labelKey, out var label))
                    {
                        label = checked((uint)labels.Count);
                        labelIds[labelKey] = label;
                        labels.Add(field.Label);
                    }
                    uint value;
                    if (field.Type == 14)
                        value = WriteNode(field.Children.Single());
                    else if (field.Type == 15)
                    {
                        var children = field.Children.Select(WriteNode).ToArray();
                        value = checked((uint)listIndices.Position);
                        listWriter.Write(checked((uint)children.Length));
                        foreach (var child in children) listWriter.Write(child);
                    }
                    else if (field.Type <= 5 || field.Type == 8)
                        value = BinaryPrimitives.ReadUInt32LittleEndian(field.Data);
                    else
                    {
                        value = checked((uint)payload.Position);
                        payload.Write(field.Data);
                    }
                    indices.Add(checked((uint)fields.Count));
                    fields.Add((field.Type, label, value));
                }
                var offset = indices.Count == 1 ? indices[0] : checked((uint)fieldIndices.Position);
                if (indices.Count > 1)
                    foreach (var index in indices) fieldWriter.Write(index);
                structures[(int)id] = (node.Type, offset, checked((uint)indices.Count));
                return id;
            }
            WriteNode(_root);
            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output, Encoding.UTF8, true);
            writer.Write(_header);
            var sizes = new[] { structures.Count * 12, fields.Count * 12, labels.Count * 16,
                checked((int)payload.Length), checked((int)fieldIndices.Length), checked((int)listIndices.Length) };
            var counts = new[] { structures.Count, fields.Count, labels.Count, sizes[3], sizes[4], sizes[5] };
            var position = 56;
            for (var i = 0; i < sizes.Length; i++)
            {
                writer.Write(position); writer.Write(counts[i]); position = checked(position + sizes[i]);
            }
            foreach (var item in structures) { writer.Write(item.Type); writer.Write(item.Offset); writer.Write(item.Count); }
            foreach (var item in fields) { writer.Write(item.Type); writer.Write(item.Label); writer.Write(item.Value); }
            foreach (var label in labels) writer.Write(label);
            writer.Write(payload.ToArray()); writer.Write(fieldIndices.ToArray()); writer.Write(listIndices.ToArray());
            return Convert.ToBase64String(output.ToArray());
        }
    }
}

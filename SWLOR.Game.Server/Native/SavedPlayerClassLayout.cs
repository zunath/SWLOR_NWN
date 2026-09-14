using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Allows older player files with repeated, spell-free class rows to load.</summary>
    internal static unsafe class SavedPlayerClassLayout
    {
        private static readonly byte[] IsPCLabel = System.Text.Encoding.ASCII.GetBytes("IsPC\0");
        private static readonly byte[] ClassListLabel = System.Text.Encoding.ASCII.GetBytes("ClassList\0");
        private static readonly byte[] ClassLabel = System.Text.Encoding.ASCII.GetBytes("Class\0");
        private static readonly byte[] LevelLabel = System.Text.Encoding.ASCII.GetBytes("ClassLevel\0");

        internal sealed class NormalizedFile : IDisposable
        {
            public CResGFF File { get; } = new();
            public CResStruct Root { get; } = new();

            public void Dispose()
            {
                Root.Dispose();
                File.Dispose();
            }
        }

        internal static NormalizedFile Normalize(CResGFF file, CResStruct root)
        {
            var found = 0;
            fixed (byte* isPC = IsPCLabel)
                if (file.ReadFieldBYTE(root, isPC, &found, 0) != 1 || found == 0)
                    return null;

            using var list = new CResList();
            fixed (byte* label = ClassListLabel)
                if (file.GetList(list, root, label) == 0)
                    return null;
            var count = file.GetListCount(list);
            if (count < 2 || count > 3)
                return null;

            var firstRows = new Dictionary<int, int>();
            var levels = new int[count];
            var retained = new List<int>();
            var total = 0;
            for (var index = 0; index < count; index++)
            {
                using var row = new CResStruct();
                if (file.GetListElement(row, list, (uint)index) == 0 || file.GetFieldCount(row) != 2)
                    return null; // Never discard spellbooks or other per-class metadata.
                int classId;
                int level;
                fixed (byte* classLabel = ClassLabel)
                {
                    if (file.GetFieldType(row, classLabel) != 5)
                        return null;
                    classId = file.ReadFieldINT(row, classLabel, &found, -1);
                    if (found == 0 || classId < 0 || classId >= byte.MaxValue)
                        return null;
                }
                fixed (byte* levelLabel = LevelLabel)
                {
                    if (file.GetFieldType(row, levelLabel) != 3)
                        return null;
                    level = file.ReadFieldSHORT(row, levelLabel, &found, 0);
                    if (found == 0 || level < 1 || level > 40)
                        return null;
                }
                total += level;
                if (total > 40)
                    return null;
                if (firstRows.TryGetValue(classId, out var first))
                    levels[first] += level;
                else
                {
                    firstRows.Add(classId, index);
                    retained.Add(index);
                    levels[index] = level;
                }
            }
            if (retained.Count == count)
                return null;

            // Work on an owned, packed copy. A GFF still being written does not expose
            // its lists through the packed-data API, and AddList appends to existing lists.
            var normalized = new NormalizedFile();
            try
            {
                void* data = null;
                var length = 0;
                if (file.WriteGFFToPointer(&data, &length) == 0)
                    throw new InvalidOperationException("Unable to copy the saved class layout.");
                // CResGFF takes ownership of the buffer allocated by WriteGFFToPointer.
                if (normalized.File.GetDataFromPointer(data, length, true) == 0)
                    throw new InvalidOperationException("Unable to read the copied class layout.");
                normalized.File.InitializeForWriting();
                if (normalized.File.GetTopLevelStruct(normalized.Root) == 0)
                    throw new InvalidOperationException("The copied character has no root structure.");
                using var copiedList = new CResList();
                fixed (byte* label = ClassListLabel)
                    if (normalized.File.GetList(copiedList, normalized.Root, label) == 0)
                        throw new InvalidOperationException("The copied character has no class list.");
                CResGFFField listField = null;
                for (uint index = 0; index < normalized.File.GetFieldCount(normalized.Root); index++)
                    if (Marshal.PtrToStringUTF8((IntPtr)normalized.File.GetFieldLabel(normalized.Root, index)) == "ClassList")
                    {
                        listField = normalized.File.GetField(normalized.Root, index);
                        break;
                    }
                if (listField == null || listField.m_nType != 15)
                    throw new InvalidOperationException("The copied class list is invalid.");
                uint size = 0;
                var entries = (uint*)normalized.File.GetDataLayoutList(listField, &size);
                if (entries == null || size < (count + 1) * sizeof(uint) || entries[0] != count)
                    throw new InvalidOperationException("The copied class list has an invalid size.");
                var originalEntries = new uint[count];
                for (var index = 0; index < count; index++)
                    originalEntries[index] = entries[index + 1];
                foreach (var index in retained)
                {
                    using var row = new CResStruct();
                    if (normalized.File.GetListElement(row, copiedList, (uint)index) == 0)
                        throw new InvalidOperationException("A copied class row is unavailable.");
                    fixed (byte* label = LevelLabel)
                        if (normalized.File.WriteFieldSHORT(row, (short)levels[index], label) == 0)
                            throw new InvalidOperationException("Unable to preserve the saved class level total.");
                }
                for (var index = 0; index < retained.Count; index++)
                    entries[index + 1] = originalEntries[retained[index]];
                entries[0] = (uint)retained.Count;
                return normalized;
            }
            catch
            {
                normalized.Dispose();
                throw;
            }
        }
    }
}

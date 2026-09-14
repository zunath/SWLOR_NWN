// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Tlk;

/// <summary>Shared safety and addressability limits for BioWare TLK files.</summary>
public static class TlkFormatLimits
{
    /// <summary>
    /// Allocation ceiling shared by the reader and writer for decoded entry metadata and text.
    /// </summary>
    public const long MaximumDecodedAllocationBytes = 64L * 1024 * 1024;

    /// <summary>Conservative managed-allocation estimate charged for every entry record.</summary>
    public const int EstimatedManagedBytesPerEntry = 64;

    /// <summary>Conservative object/header charge for each separately decoded managed string.</summary>
    public const int EstimatedManagedStringOverheadBytes = 24;

    /// <summary>Conservative hash-table bucket/entry charge for each unique decoded text range.</summary>
    public const int EstimatedDecodedRangeDictionaryBytes = 48;

    /// <summary>
    /// Greatest number of entry records accepted by the TLK reader and writer. The remaining
    /// allocation covers one unique final-row string, its range-index entry, and 28 UTF-16 characters.
    /// </summary>
    public const int MaximumEntryCount =
        (int)((MaximumDecodedAllocationBytes - EstimatedManagedStringOverheadBytes -
               EstimatedDecodedRangeDictionaryBytes) / EstimatedManagedBytesPerEntry);

    /// <summary>Greatest zero-based entry ID accepted by the TLK reader and writer.</summary>
    public const int MaximumEntryId = MaximumEntryCount - 1;
}

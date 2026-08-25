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

    /// <summary>
    /// Greatest number of entry records accepted by the TLK reader and writer. One record's worth
    /// of the allocation budget remains for the non-empty final row that makes this count necessary.
    /// </summary>
    public const int MaximumEntryCount =
        (int)(MaximumDecodedAllocationBytes / EstimatedManagedBytesPerEntry) - 1;

    /// <summary>Greatest zero-based entry ID accepted by the TLK reader and writer.</summary>
    public const int MaximumEntryId = MaximumEntryCount - 1;
}

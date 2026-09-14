// SPDX-License-Identifier: MIT

using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Mdl;

/// <summary>Bounds cumulative decoded allocations when several models are retained together.</summary>
/// <remarks>
/// Share one instance across a model chain, its target body, and attached equipment.
/// Charges are conservative estimates, include failed parses, and are not refunded.
/// Like the readers that use it, this context is for sequential parsing.
/// </remarks>
public sealed class MdlReadBudget
{
    public const long DefaultMaximumDecodedBytes = 256L * 1024 * 1024;

    internal AllocationBudget Allocations { get; }

    public long ReservedBytes => Allocations.ReservedBytes;

    public MdlReadBudget(long maximumDecodedBytes = DefaultMaximumDecodedBytes) =>
        Allocations = new AllocationBudget("MDL model chain", maximumDecodedBytes);
}

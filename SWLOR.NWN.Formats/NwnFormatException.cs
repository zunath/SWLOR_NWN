// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats;

/// <summary>
/// A catchable failure raised when an Aurora resource is malformed, truncated, or unsupported.
/// </summary>
public sealed class NwnFormatException : FormatException
{
    public NwnFormatException(string message)
        : base(message)
    {
    }

    public NwnFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

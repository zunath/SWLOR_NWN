namespace SWLOR.Toolset.Services;

/// <summary>
/// Raised when an interrupted resource delete cannot be rolled back without risking a newer
/// filesystem generation. Opening and packing must stop until the named transaction is repaired.
/// </summary>
public sealed class ModuleResourceDeleteRecoveryException(string manifestPath, Exception innerException)
    : IOException(
        $"Could not recover interrupted resource delete '{manifestPath}': {innerException.Message}",
        innerException);

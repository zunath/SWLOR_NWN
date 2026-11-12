namespace SWLOR.Game.Host.Tests.Support;

/// <summary>One script of the game's handler manifest, and whether the manifest lists a handler for it.</summary>
internal sealed record ManifestScript(string Name, bool HasHandlers);

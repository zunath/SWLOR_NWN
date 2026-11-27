namespace SWLOR.Architecture.Tests.Debt;

/// <summary>What one type of the native sinks does besides forwarding (see <see cref="SinkWorkScanner"/>).</summary>
/// <param name="Type">The type's full name.</param>
/// <param name="Files">Its files, relative to the repository root.</param>
/// <param name="Lines">The lines of its members that do work.</param>
/// <param name="WorkingMembers">Those members, in source order (a field by its first variable).</param>
/// <param name="ForwardingMembers">Its members that only forward or hold what was injected.</param>
internal sealed record SinkWork(string Type, IReadOnlyList<string> Files, int Lines, IReadOnlyList<string> WorkingMembers, int ForwardingMembers);

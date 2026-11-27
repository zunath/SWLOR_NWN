using System.Reflection;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>One call a <see cref="RecordingProxy"/> received.</summary>
/// <param name="Method">The interface member that was called.</param>
/// <param name="Arguments">The arguments as passed.</param>
public sealed record RecordedInvocation(MethodInfo Method, IReadOnlyList<object?> Arguments);

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>One call a hook body makes.</summary>
/// <param name="Description">The callee as <c>Namespace.Type::Member</c>, or what kind of call it is.</param>
/// <param name="IsSinkCall">True when the callee is a method of a sink interface.</param>
internal sealed record HookOperation(string Description, bool IsSinkCall);

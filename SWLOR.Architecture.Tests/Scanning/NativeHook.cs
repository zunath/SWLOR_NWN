namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>One native hook entry point and every call, function pointer call and generic call its body makes.</summary>
/// <param name="Owner">The top-level type that declares the method.</param>
/// <param name="Method">The method name.</param>
/// <param name="Operations">What the body calls. Field reads and casts are not listed.</param>
internal sealed record NativeHook(TypeKey Owner, string Method, IReadOnlyList<HookOperation> Operations);

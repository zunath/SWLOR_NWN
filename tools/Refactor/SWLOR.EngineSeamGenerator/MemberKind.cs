namespace SWLOR.EngineSeamGenerator;

/// <summary>The kinds of wrapper member that can sit behind the interface.</summary>
internal enum MemberKind
{
    /// <summary>A public static method with a block body.</summary>
    Method,

    /// <summary>A public static get-only property with an expression body.</summary>
    Property
}

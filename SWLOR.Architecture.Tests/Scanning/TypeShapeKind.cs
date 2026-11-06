namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>The kinds of field type the immutability check tells apart.</summary>
internal enum TypeShapeKind
{
    /// <summary>A primitive such as <c>int</c> or <c>string</c>; <see cref="TypeShape.Name"/> is its type name.</summary>
    Primitive,

    /// <summary>A type of another assembly, or a generic instantiation of one (the arguments are in <see cref="TypeShape.Arguments"/>).</summary>
    External,

    /// <summary>A type of the scanned assembly.</summary>
    Defined,

    /// <summary>An array, pointer, by-reference type, generic parameter or anything else the check does not accept.</summary>
    Other,
}

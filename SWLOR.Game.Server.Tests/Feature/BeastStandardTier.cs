namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Where a beast sits in the mutation ladder, which decides the stat curve it should follow.
/// </summary>
public enum BeastStandardTier
{
    /// <summary>A tamable beast. Defines the role standard.</summary>
    Base,

    /// <summary>An incubation beast reached directly from a tamable beast.</summary>
    First,

    /// <summary>An incubation beast reached from a first-stage beast: about 12.5 percent above First.</summary>
    Second,

    /// <summary>A shared bad-outcome beast with no mutations of its own: about 25 percent below First.</summary>
    DeadEnd,

    /// <summary>An incubation beast no beast mutates into, so its tier cannot be read from the graph.</summary>
    Unparented,
}

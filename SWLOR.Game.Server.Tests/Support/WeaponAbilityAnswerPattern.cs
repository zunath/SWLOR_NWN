namespace SWLOR.Game.Server.Tests.Support;

/// <summary>How the recording stand-ins of a weapon ability trace answer a check, an amount or a rate they have no script for.</summary>
public enum WeaponAbilityAnswerPattern
{
    /// <summary>Every check false, every amount zero.</summary>
    Off,

    /// <summary>Every check true, every amount raised.</summary>
    On,

    /// <summary>Every check true except whether a creature has a status effect of a named type, so a status can spread to creatures that lack it.</summary>
    OnWithoutNamedStatuses,

    /// <summary>The first unscripted answer is true, then they alternate, so the second condition of a pair is the one that fails.</summary>
    AlternateStartingTrue,

    /// <summary>The first unscripted answer is false, then they alternate.</summary>
    AlternateStartingFalse,

    /// <summary>Two answers in three are true, then one false, so a check made twice in a row can pass the first time and fail the second.</summary>
    TwoTrueThenFalse,

    /// <summary>One answer in three is true, starting with the first.</summary>
    EveryThird,

    /// <summary>Two answers in three are true, starting with false.</summary>
    TwoOfThree
}

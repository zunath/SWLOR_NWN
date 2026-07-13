using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DiceService
{
    /// <summary>
    /// The per-die roll modifier applied to a dice group. At most one may be set per group.
    /// </summary>
    public enum DieModifier
    {
        None,
        Exploding,      // on max, roll again and add (depth-capped)
        Reroll          // reroll (once) any die that rolls <= threshold
    }

    /// <summary>
    /// Keep-highest / keep-lowest applied to a dice group after its dice are rolled.
    /// </summary>
    public enum KeepMode
    {
        None,
        KeepHighest,
        KeepLowest
    }

    /// <summary>
    /// A single parsed term of a dice expression: either a flat modifier or a dice group.
    /// Produced by <see cref="DiceParser"/>; contains no randomness (pure/testable).
    /// </summary>
    public class DiceTerm
    {
        public bool IsFlat { get; set; }
        public bool Negative { get; set; }      // sign comes from the +/- joining the terms

        // Flat term
        public int FlatValue { get; set; }

        // Dice group
        public int Count { get; set; }
        public int Sides { get; set; }
        public DieModifier DieModifier { get; set; } = DieModifier.None;
        public int RerollThreshold { get; set; }
        public KeepMode KeepMode { get; set; } = KeepMode.None;
        public int KeepCount { get; set; } = 1;
        public int Multiplier { get; set; } = 1;
    }

    /// <summary>
    /// The rolled outcome of a single die within a group.
    /// </summary>
    public class DieRoll
    {
        public int Value { get; set; }          // final value this die contributes
        public int[] Rolls { get; set; }        // raw rolls (exploding: chain; reroll: [old,new]; else [value])
        public DieModifier Modifier { get; set; }
        public bool Dropped { get; set; }       // dropped by keep-highest / keep-lowest
    }

    /// <summary>
    /// The rolled outcome of a whole term.
    /// </summary>
    public class TermRoll
    {
        public DiceTerm Term { get; set; }
        public List<DieRoll> Dice { get; set; } = new List<DieRoll>();
        public int Subtotal { get; set; }       // signed contribution to the grand total
    }

    /// <summary>
    /// The rolled outcome of a full dice expression.
    /// </summary>
    public class DiceRollResult
    {
        public string Expression { get; set; }
        public List<TermRoll> Terms { get; set; } = new List<TermRoll>();
        public int Total { get; set; }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWLOR.Game.Server.Service.DiceService
{
    /// <summary>
    /// Rolls parsed dice expressions and formats the result for chat. Facade over
    /// <see cref="DiceParser"/> and the shared <see cref="Random"/> RNG.
    /// NOTE: no <c>using System;</c> here on purpose, so <c>Random</c> resolves to
    /// <see cref="SWLOR.Game.Server.Service.Random"/> (the game RNG), not System.Random.
    /// </summary>
    public static class Dice
    {
        private const int MaxExplosionDepth = 100;
        private const int MaxMessageLength = 600;

        /// <summary>
        /// Parses and rolls an expression, returning a formatted, colorized message for chat.
        /// </summary>
        public static bool TryRoll(string expression, out string message, out string error)
        {
            message = null;
            if (!TryEvaluate(expression, out var result, out error))
                return false;
            message = Format(result);
            return true;
        }

        /// <summary>
        /// Parses and rolls an expression, returning the structured result (used by tests/tools).
        /// </summary>
        public static bool TryEvaluate(string expression, out DiceRollResult result, out string error)
        {
            result = null;
            if (!DiceParser.TryParse(expression, out var terms, out error))
                return false;

            var res = new DiceRollResult { Expression = Normalize(expression) };
            foreach (var term in terms)
            {
                var termRoll = new TermRoll { Term = term };
                if (term.IsFlat)
                {
                    termRoll.Subtotal = term.Negative ? -term.FlatValue : term.FlatValue;
                }
                else
                {
                    for (var d = 0; d < term.Count; d++)
                        termRoll.Dice.Add(RollDie(term));

                    ApplyKeep(term, termRoll.Dice);

                    var sum = termRoll.Dice.Where(x => !x.Dropped).Sum(x => x.Value) * term.Multiplier;
                    termRoll.Subtotal = term.Negative ? -sum : sum;
                }

                res.Terms.Add(termRoll);
                res.Total += termRoll.Subtotal;
            }

            result = res;
            return true;
        }

        private static DieRoll RollDie(DiceTerm term)
        {
            var die = new DieRoll { Modifier = term.DieModifier };
            switch (term.DieModifier)
            {
                case DieModifier.Advantage:
                {
                    var a = Roll(term.Sides);
                    var b = Roll(term.Sides);
                    die.Rolls = new[] { a, b };
                    die.Value = a >= b ? a : b;
                    break;
                }
                case DieModifier.Disadvantage:
                {
                    var a = Roll(term.Sides);
                    var b = Roll(term.Sides);
                    die.Rolls = new[] { a, b };
                    die.Value = a <= b ? a : b;
                    break;
                }
                case DieModifier.Exploding:
                {
                    var rolls = new List<int>();
                    var depth = 0;
                    var value = Roll(term.Sides);
                    rolls.Add(value);
                    var sum = value;
                    while (term.Sides >= 2 && value == term.Sides && depth < MaxExplosionDepth)
                    {
                        value = Roll(term.Sides);
                        rolls.Add(value);
                        sum += value;
                        depth++;
                    }
                    die.Rolls = rolls.ToArray();
                    die.Value = sum;
                    break;
                }
                case DieModifier.Reroll:
                {
                    var value = Roll(term.Sides);
                    if (value <= term.RerollThreshold)
                    {
                        var newValue = Roll(term.Sides);
                        die.Rolls = new[] { value, newValue };
                        die.Value = newValue;
                    }
                    else
                    {
                        die.Rolls = new[] { value };
                        die.Value = value;
                    }
                    break;
                }
                default:
                {
                    var value = Roll(term.Sides);
                    die.Rolls = new[] { value };
                    die.Value = value;
                    break;
                }
            }
            return die;
        }

        private static void ApplyKeep(DiceTerm term, List<DieRoll> dice)
        {
            if (term.KeepMode == KeepMode.None)
                return;

            var ordered = term.KeepMode == KeepMode.KeepHighest
                ? dice.OrderByDescending(x => x.Value).ToList()
                : dice.OrderBy(x => x.Value).ToList();

            for (var k = term.KeepCount; k < ordered.Count; k++)
                ordered[k].Dropped = true;
        }

        private static int Roll(int sides)
        {
            return Random.Next(1, sides + 1); // Service.Random.Next => 1..sides (max exclusive)
        }

        private static string Format(DiceRollResult res)
        {
            var label = ColorToken.SkillCheck("Dice Roll [" + res.Expression + "]: ");
            var message = label + BuildBreakdown(res, false) + " = " + res.Total;
            if (message.Length > MaxMessageLength)
                message = label + BuildBreakdown(res, true) + " = " + res.Total;
            return message;
        }

        private static string BuildBreakdown(DiceRollResult res, bool collapsed)
        {
            var sb = new StringBuilder();
            var firstAtom = true;

            foreach (var termRoll in res.Terms)
            {
                var term = termRoll.Term;

                if (collapsed)
                {
                    var abs = termRoll.Subtotal < 0 ? -termRoll.Subtotal : termRoll.Subtotal;
                    AppendAtom(sb, ref firstAtom, term.Negative, abs.ToString());
                }
                else if (term.IsFlat)
                {
                    AppendAtom(sb, ref firstAtom, term.Negative, term.FlatValue.ToString());
                }
                else if (term.Multiplier != 1)
                {
                    var inner = new StringBuilder();
                    var firstInner = true;
                    foreach (var die in termRoll.Dice)
                    {
                        if (!firstInner) inner.Append(" + ");
                        inner.Append(FormatDie(die));
                        firstInner = false;
                    }
                    AppendAtom(sb, ref firstAtom, term.Negative, "(" + inner + ") x " + term.Multiplier);
                }
                else
                {
                    foreach (var die in termRoll.Dice)
                        AppendAtom(sb, ref firstAtom, term.Negative, FormatDie(die));
                }
            }

            return sb.ToString();
        }

        private static void AppendAtom(StringBuilder sb, ref bool firstAtom, bool negative, string display)
        {
            if (firstAtom)
            {
                sb.Append(negative ? "-" + display : display);
                firstAtom = false;
            }
            else
            {
                sb.Append(negative ? " - " : " + ");
                sb.Append(display);
            }
        }

        private static string FormatDie(DieRoll die)
        {
            string s;
            switch (die.Modifier)
            {
                case DieModifier.Advantage:
                case DieModifier.Disadvantage:
                    s = "[" + die.Rolls[0] + " | " + die.Rolls[1] + "] " + die.Value;
                    break;
                case DieModifier.Exploding:
                    s = string.Join("!+", die.Rolls);
                    break;
                case DieModifier.Reroll:
                    s = die.Rolls.Length == 2 ? die.Rolls[0] + "->" + die.Rolls[1] : die.Value.ToString();
                    break;
                default:
                    s = die.Value.ToString();
                    break;
            }
            return die.Dropped ? ColorToken.Gray(s) : s;
        }

        private static string Normalize(string expression)
        {
            return new string(expression.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        }
    }
}

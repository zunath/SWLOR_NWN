using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.DiceService
{
    /// <summary>
    /// Parses text dice expressions (e.g. "1d20+3d6+2d8kh1+2") into a list of <see cref="DiceTerm"/>.
    /// Pure and deterministic (no randomness), so it can be unit-tested in isolation.
    ///
    /// Grammar (whitespace ignored, case-insensitive):
    ///   expression = ['+'|'-'] term ( ('+'|'-') term )*
    ///   term       = dice | integer | 'adv' | 'dis'        (adv = 2d20kh1, dis = 2d20kl1)
    ///   dice       = [count] 'd' sides [dieMod] [keepMod] [multMod]
    ///   dieMod     = '!' | 'r'&lt;n&gt;                    (at most one)
    ///   keepMod    = ('kh'|'kl') [n] | 'adv' | 'dis'       (at most one; default n = 1; adv/dis
    ///                                                       roll the die twice keeping the
    ///                                                       better/worse, so the group must be
    ///                                                       a single die - use khN/klN for pools)
    ///   multMod    = ('x'|'*')&lt;n&gt;                    (at most one; multiplies this term only)
    /// </summary>
    public static class DiceParser
    {
        public const int MaxCount = 100;
        public const int MaxSides = 1000;
        public const int MaxTotalDice = 300;
        public const int MaxMultiplier = 100;
        public const int MaxFlat = 1_000_000;
        public const int MaxExpressionLength = 100;

        private const string DieModConflict = "A dice group can have only one of ! or rN.";

        public static bool TryParse(string expression, out List<DiceTerm> terms, out string error)
        {
            terms = new List<DiceTerm>();
            error = null;

            if (string.IsNullOrWhiteSpace(expression))
            {
                error = "Empty dice expression. Example: /r 1d20+5 or /r adv";
                return false;
            }

            var expr = new string(expression.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
            if (expr.Length == 0)
            {
                error = "Empty dice expression. Example: /r 1d20+5 or /r adv";
                return false;
            }
            if (expr.Length > MaxExpressionLength)
            {
                error = "Dice expression is too long (max " + MaxExpressionLength + " characters).";
                return false;
            }

            var i = 0;
            var totalDice = 0;
            var first = true;

            while (i < expr.Length)
            {
                // Sign (optional on the first term, required afterwards).
                var negative = false;
                if (expr[i] == '+' || expr[i] == '-')
                {
                    negative = expr[i] == '-';
                    i++;
                    if (i >= expr.Length)
                    {
                        error = "Dice expression cannot end with '+' or '-'.";
                        return false;
                    }
                }
                else if (!first)
                {
                    error = "Expected '+' or '-' before position " + (i + 1) + ".";
                    return false;
                }
                first = false;

                // Standalone advantage/disadvantage keyword: adv = 2d20kh1, dis = 2d20kl1.
                if ((StartsWith(expr, i, "adv") || StartsWith(expr, i, "dis")) &&
                    (i + 3 >= expr.Length || expr[i + 3] == '+' || expr[i + 3] == '-'))
                {
                    var keepHigh = expr[i] == 'a';
                    i += 3;

                    totalDice += 2;
                    if (totalDice > MaxTotalDice)
                    {
                        error = "Too many dice in one roll (max " + MaxTotalDice + ").";
                        return false;
                    }

                    terms.Add(new DiceTerm
                    {
                        IsFlat = false,
                        Negative = negative,
                        Count = 2,
                        Sides = 20,
                        KeepMode = keepHigh ? KeepMode.KeepHighest : KeepMode.KeepLowest,
                        KeepCount = 1
                    });
                    continue;
                }

                // Leading digits: either the dice count or a flat modifier.
                var digitStart = i;
                while (i < expr.Length && char.IsDigit(expr[i])) i++;
                var digitStr = expr.Substring(digitStart, i - digitStart);

                if (i < expr.Length && expr[i] == 'd')
                {
                    i++; // consume 'd'
                    var term = new DiceTerm { IsFlat = false, Negative = negative };

                    if (digitStr.Length == 0)
                        term.Count = 1;
                    else if (!TryParseCapped(digitStr, 1, MaxCount, "dice count", out var count, out error))
                        return false;
                    else
                        term.Count = count;

                    var sidesStart = i;
                    while (i < expr.Length && char.IsDigit(expr[i])) i++;
                    var sidesStr = expr.Substring(sidesStart, i - sidesStart);
                    if (sidesStr.Length == 0)
                    {
                        error = "Missing number of sides after 'd' (e.g. 1d20).";
                        return false;
                    }
                    if (!TryParseCapped(sidesStr, 1, MaxSides, "die sides", out var sides, out error))
                        return false;
                    term.Sides = sides;

                    var hasDieMod = false;
                    var hasKeep = false;
                    var hasMult = false;

                    while (i < expr.Length && expr[i] != '+' && expr[i] != '-')
                    {
                        if (expr[i] == '!')
                        {
                            if (hasDieMod) { error = DieModConflict; return false; }
                            term.DieModifier = DieModifier.Exploding; hasDieMod = true; i++;
                        }
                        else if (expr[i] == 'r')
                        {
                            if (hasDieMod) { error = DieModConflict; return false; }
                            i++;
                            var rStart = i;
                            while (i < expr.Length && char.IsDigit(expr[i])) i++;
                            var rStr = expr.Substring(rStart, i - rStart);
                            if (rStr.Length == 0)
                            {
                                error = "Reroll 'r' needs a threshold, e.g. 4d6r1.";
                                return false;
                            }
                            if (!TryParseCapped(rStr, 1, term.Sides, "reroll threshold", out var threshold, out error))
                                return false;
                            term.DieModifier = DieModifier.Reroll; term.RerollThreshold = threshold; hasDieMod = true;
                        }
                        else if (StartsWith(expr, i, "adv") || StartsWith(expr, i, "dis"))
                        {
                            if (hasKeep) { error = "A dice group can have only one keep modifier (kh, kl, adv or dis)."; return false; }
                            if (term.Count != 1)
                            {
                                error = "adv/dis rolls one die twice and keeps the better/worse - write d20adv, or use khN/klN for pools (e.g. 4d6kh3).";
                                return false;
                            }
                            term.KeepMode = expr[i] == 'a' ? KeepMode.KeepHighest : KeepMode.KeepLowest;
                            term.KeepCount = 1;
                            term.Count = 2;
                            i += 3;
                            hasKeep = true;
                        }
                        else if (StartsWith(expr, i, "kh") || StartsWith(expr, i, "kl"))
                        {
                            if (hasKeep) { error = "A dice group can have only one keep modifier (kh, kl, adv or dis)."; return false; }
                            var keepHighest = expr[i + 1] == 'h';
                            i += 2;
                            var kStart = i;
                            while (i < expr.Length && char.IsDigit(expr[i])) i++;
                            var kStr = expr.Substring(kStart, i - kStart);
                            var keepCount = 1;
                            if (kStr.Length > 0 && !TryParseCapped(kStr, 1, MaxCount, "keep count", out keepCount, out error))
                                return false;
                            term.KeepMode = keepHighest ? KeepMode.KeepHighest : KeepMode.KeepLowest;
                            term.KeepCount = keepCount; hasKeep = true;
                        }
                        else if (expr[i] == 'x' || expr[i] == '*')
                        {
                            if (hasMult) { error = "A dice group can have only one multiplier (xN)."; return false; }
                            i++;
                            var mStart = i;
                            while (i < expr.Length && char.IsDigit(expr[i])) i++;
                            var mStr = expr.Substring(mStart, i - mStart);
                            if (mStr.Length == 0)
                            {
                                error = "Multiplier needs a number, e.g. 2d6x3.";
                                return false;
                            }
                            if (!TryParseCapped(mStr, 1, MaxMultiplier, "multiplier", out var mult, out error))
                                return false;
                            term.Multiplier = mult; hasMult = true;
                        }
                        else
                        {
                            error = "Unknown dice modifier near '" + expr.Substring(i) + "'.";
                            return false;
                        }
                    }

                    if (term.KeepMode != KeepMode.None && term.KeepCount > term.Count)
                    {
                        error = "Keep count cannot exceed the number of dice (" + term.Count + ").";
                        return false;
                    }

                    totalDice += term.Count;
                    if (totalDice > MaxTotalDice)
                    {
                        error = "Too many dice in one roll (max " + MaxTotalDice + ").";
                        return false;
                    }

                    terms.Add(term);
                }
                else
                {
                    // Flat modifier.
                    if (digitStr.Length == 0)
                    {
                        var ch = i < expr.Length ? expr[i].ToString() : "end of expression";
                        error = "Unexpected '" + ch + "' - expected a dice group or a number.";
                        return false;
                    }
                    if (!TryParseCapped(digitStr, 0, MaxFlat, "modifier", out var flat, out error))
                        return false;
                    terms.Add(new DiceTerm { IsFlat = true, Negative = negative, FlatValue = flat });
                }
            }

            if (terms.Count == 0)
            {
                error = "Empty dice expression. Example: /r 1d20+5 or /r adv";
                return false;
            }

            return true;
        }

        private static bool StartsWith(string s, int i, string token)
        {
            return i + token.Length <= s.Length && s.Substring(i, token.Length) == token;
        }

        private static bool TryParseCapped(string s, int min, int max, string name, out int value, out string error)
        {
            error = null;
            if (!int.TryParse(s, out value))
            {
                error = "'" + s + "' is not a valid " + name + ".";
                return false;
            }
            if (value < min || value > max)
            {
                error = char.ToUpperInvariant(name[0]) + name.Substring(1) + " must be between " + min + " and " + max + ".";
                return false;
            }
            return true;
        }
    }
}

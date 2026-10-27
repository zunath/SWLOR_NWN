namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>Builds deterministic input grids.</summary>
public static class Grid
{
    /// <summary>
    /// The cartesian product of the axes, as one int[] per combination. The first axis changes slowest, so rows
    /// group by it. A combination with an empty axis does not exist, so any empty axis yields no rows.
    /// </summary>
    public static IEnumerable<int[]> Cross(params int[][] axes)
    {
        if (axes.Any(axis => axis.Length == 0))
            yield break;

        var indexes = new int[axes.Length];
        while (true)
        {
            var combination = new int[axes.Length];
            for (var i = 0; i < axes.Length; i++)
                combination[i] = axes[i][indexes[i]];
            yield return combination;

            var position = axes.Length - 1;
            while (position >= 0 && ++indexes[position] == axes[position].Length)
            {
                indexes[position] = 0;
                position--;
            }

            if (position < 0)
                yield break;
        }
    }
}

#nullable enable
using System.Collections.Generic;

namespace SWLOR.Game.Server.EngineTests.Characterization.Support;

/// <summary>
/// The recorded result of one function (or one scenario family) over a deterministic input grid.
/// Each row holds the inputs followed by the outputs, so a reviewer can read what went in next to
/// what came out. Build one, add rows, then pass it to <see cref="ScenarioSnapshot.Verify"/>.
/// </summary>
public sealed class SnapshotTable
{
    private readonly List<object?[]> _rows = new();

    /// <param name="function">The code under test, as written in the snapshot header (for example <c>Combat.CalculateHitRate</c>).</param>
    /// <param name="description">One or two sentences: what the function computes.</param>
    /// <param name="inputs">Plain-text description of the input grid, so the snapshot can be reviewed without reading the test.</param>
    /// <param name="inputColumns">Names of the input columns, in row order.</param>
    /// <param name="outputColumns">Names of the output columns, in row order, after the inputs.</param>
    /// <param name="seed">The <c>Random.SetSeed</c> value the rows were produced with, or null when no randomness is involved.</param>
    public SnapshotTable(
        string function,
        string description,
        string inputs,
        string[] inputColumns,
        string[] outputColumns,
        int? seed = null)
    {
        if (outputColumns.Length == 0)
            throw new ArgumentException("A snapshot needs at least one output column.", nameof(outputColumns));

        Function = function;
        Description = description;
        Inputs = inputs;
        InputColumns = inputColumns;
        OutputColumns = outputColumns;
        Seed = seed;
    }

    public string Function { get; }
    public string Description { get; }
    public string Inputs { get; }
    public IReadOnlyList<string> InputColumns { get; }
    public IReadOnlyList<string> OutputColumns { get; }
    public int? Seed { get; }
    public IReadOnlyList<object?[]> Rows => _rows;

    /// <summary>
    /// By default <see cref="ScenarioSnapshot.Verify"/> rejects a table whose output column never changes, because that
    /// usually means the function was not really exercised. Set this when a constant output column is genuine.
    /// </summary>
    public bool AllowConstantOutputs { get; init; }

    /// <summary>
    /// Adds a row: the input values followed by the output values. Supported cell types are integers, floats,
    /// bool, string, enums, null and (nested) arrays or lists of those.
    /// </summary>
    public SnapshotTable Add(params object?[] values)
    {
        var expected = InputColumns.Count + OutputColumns.Count;
        if (values.Length != expected)
            throw new ArgumentException($"{Function}: expected {expected} cells but got {values.Length}.");

        _rows.Add(values);
        return this;
    }
}

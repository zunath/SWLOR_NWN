#nullable enable
using System.Globalization;
using System.Reflection;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The shared <c>Random</c> generator replaced by a seeded one that writes every draw to the transcript with its bounds
/// and its value. The values are the ones the seed yields; nothing is changed.
/// </summary>
public sealed class RecordingRandom : System.Random
{
    private readonly List<string> _lines;
    private readonly Func<uint, string> _name;
    private int _depth;

    private RecordingRandom(int seed, List<string> lines, Func<uint, string> name)
        : base(seed)
    {
        _lines = lines;
        _name = name;
    }

    public static IDisposable Install(int seed, List<string> lines, Func<uint, string> name)
    {
        var field = typeof(SWLOR.Game.Server.Domain.Foundation.Random).GetField("_random", BindingFlags.NonPublic | BindingFlags.Static)!;
        var previous = field.GetValue(null);
        field.SetValue(null, new RecordingRandom(seed, lines, name));
        return new Restore(field, previous);
    }

    public override int Next() => Draw("Next()", () => base.Next());

    public override int Next(int maxValue) => Draw($"Next({maxValue})", () => base.Next(maxValue));

    public override int Next(int minValue, int maxValue) => Draw($"Next({minValue},{maxValue})", () => base.Next(minValue, maxValue));

    public override double NextDouble() => Draw("NextDouble()", () => base.NextDouble());

    private T Draw<T>(string call, Func<T> draw)
        where T : struct, IFormattable
    {
        _depth++;
        var value = draw();
        _depth--;
        if (_depth == 0)
            _lines.Add($"Random.{call} = {value.ToString(null, CultureInfo.InvariantCulture)}");
        return value;
    }

    private sealed class Restore : IDisposable
    {
        private readonly FieldInfo _field;
        private readonly object? _previous;

        public Restore(FieldInfo field, object? previous)
        {
            _field = field;
            _previous = previous;
        }

        public void Dispose() => _field.SetValue(null, _previous);
    }
}

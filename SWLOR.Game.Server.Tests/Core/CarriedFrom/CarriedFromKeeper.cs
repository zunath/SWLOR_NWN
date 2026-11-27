using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core.CarriedFrom;

/// <summary>
/// A class that still carries its own field; in the earlier build it also carried <c>_moved</c>, which
/// <see cref="CarriedFromRightPart"/> holds now, so its own text feeds that class too.
/// </summary>
public sealed class CarriedFromKeeper
{
    [ReloadState(ReloadStateKind.Carried)]
    private readonly Dictionary<uint, int> _kept = new();

    public IReadOnlyDictionary<uint, int> Kept => _kept;
}

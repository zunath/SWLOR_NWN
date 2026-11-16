using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core.CarriedFrom;

/// <summary>One half of a class the earlier build carried whole (<c>RetiredWholeService</c>, which this build no longer has).</summary>
public sealed class CarriedFromLeftPart
{
    [ReloadState(ReloadStateKind.Carried)]
    [ReloadCarriedFrom("SWLOR.Game.Server.Tests.Core.CarriedFrom.RetiredWholeService")]
    private readonly Dictionary<uint, int> _left = new();

    public IReadOnlyDictionary<uint, int> Left => _left;
}

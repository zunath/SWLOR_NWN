using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core.CarriedFrom;

/// <summary>
/// The other half of the earlier build's <c>RetiredWholeService</c>, whose <c>_oldRight</c> it holds under a new name, plus a field
/// that moved from <see cref="CarriedFromKeeper"/>. It counts its adoptions.
/// </summary>
public sealed class CarriedFromRightPart : IReloadStateAdopter
{
    [ReloadState(ReloadStateKind.Carried)]
    [ReloadCarriedFrom("SWLOR.Game.Server.Tests.Core.CarriedFrom.RetiredWholeService", "_oldRight")]
    private readonly Dictionary<uint, int> _right = new();

    [ReloadState(ReloadStateKind.Carried)]
    [ReloadCarriedFrom("SWLOR.Game.Server.Tests.Core.CarriedFrom.CarriedFromKeeper")]
    private readonly Dictionary<uint, int> _moved = new();

    public IReadOnlyDictionary<uint, int> Right => _right;

    public IReadOnlyDictionary<uint, int> Moved => _moved;

    public int Adoptions { get; private set; }

    public void OnCarriedStateAdopted()
    {
        Adoptions++;
    }
}

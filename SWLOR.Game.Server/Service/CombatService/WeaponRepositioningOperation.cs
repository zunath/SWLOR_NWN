using NumericsVector3 = System.Numerics.Vector3;
using NumericsVector2 = System.Numerics.Vector2;

namespace SWLOR.Game.Server.Service.CombatService;

internal sealed class WeaponRepositioningOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public uint Actor { get; init; }
    public uint Area { get; init; }
    public uint MainHand { get; init; }
    public uint OffHand { get; init; }
    public NumericsVector3 Origin { get; init; }
    public NumericsVector3 Destination { get; init; }
    public NumericsVector3 LastPosition { get; set; }
    public float DistanceTravelled { get; set; }
    public long Started { get; init; }
    public ushort? ActionGroup { get; set; }
    public Action<bool, float> OnArrival { get; init; }
    /// <summary>Authored clip that replaces the run animation while this move travels, or null.</summary>
    public string TravelAnimation { get; init; }
    /// <summary>The move travels as a cutscene ghost so creatures on the path do not block it.</summary>
    public bool PassesThroughCreatures { get; init; }
}

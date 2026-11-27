#nullable enable
using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>An object the <see cref="SpawnBench"/> engine knows: an area, a creature, a placeable, a waypoint or an item.</summary>
public sealed class SpawnSim
{
    public uint Id { get; init; }
    public string Tag { get; set; } = string.Empty;
    public string ResRef { get; set; } = string.Empty;
    public ObjectType Type { get; set; }
    public bool IsArea { get; set; }
    public uint Area { get; set; } = SpawnBench.Invalid;
    public Vector3 Position { get; set; }
    public float Facing { get; set; }
    public bool IsPC { get; set; }
    public bool IsDM { get; set; }
    public bool IsDMPossessed { get; set; }
    public bool Plot { get; set; }
    public bool Immortal { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool HasInventory { get; set; }
    public List<uint> Inventory { get; } = new();
    public Dictionary<EventScript, string> EventScripts { get; } = new();
    public Dictionary<InventorySlot, uint> Slots { get; } = new();
}

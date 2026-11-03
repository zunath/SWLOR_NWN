#nullable enable
using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>An object the <see cref="AIBench"/> engine knows: an area, a creature or a player.</summary>
public sealed class AISim
{
    public uint Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string ResRef { get; set; } = string.Empty;
    public ObjectType Type { get; set; } = ObjectType.Creature;
    public bool IsArea { get; set; }
    public uint Area { get; set; } = AIBench.Invalid;
    public Vector3 Position { get; set; }
    public bool IsPC { get; set; }
    public bool IsDM { get; set; }
    public bool IsDMPossessed { get; set; }
    public bool Dead { get; set; }
    public bool Destroyed { get; set; }
    public int HitPoints { get; set; } = 100;
    public int MaxHitPoints { get; set; } = 100;
    public bool Plot { get; set; }
    public int MovementRate { get; set; } = 7;
    public AILevel AILevel { get; set; } = AILevel.Medium;
    public uint Master { get; set; } = AIBench.Invalid;
    public int Faction { get; set; } = 1;
    public bool InCombat { get; set; }
    public bool InConversation { get; set; }
    public ActionType Action { get; set; } = ActionType.Invalid;
    public float HitDistance { get; set; }
    public List<EffectTypeScript> Effects { get; } = new();
    public HashSet<FeatType> Feats { get; } = new();
}

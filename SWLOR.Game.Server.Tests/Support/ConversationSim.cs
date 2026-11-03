#nullable enable
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>An object the <see cref="ConversationBench"/> engine knows: a player, a DM, a creature or a placeable.</summary>
public sealed class ConversationSim
{
    public uint Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public ObjectType Type { get; set; } = ObjectType.Creature;
    public bool IsPC { get; set; }
    public bool IsDM { get; set; }
    public bool IsDMPossessed { get; set; }
    public uint Master { get; set; } = ConversationBench.Invalid;
    public bool Dead { get; set; }
    public bool Commandable { get; set; } = true;
    public Gender Gender { get; set; } = Gender.Male;
    public RacialType Race { get; set; } = RacialType.Human;
    public string DialogResref { get; set; } = string.Empty;
    public List<EffectTypeScript> Effects { get; } = new();
}

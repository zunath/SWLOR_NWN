using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame.Circuit;

/// <summary>Engineering recipes are crafted in the Circuit window.</summary>
public sealed class CircuitRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Engineering;

    public GuiWindowType Window => GuiWindowType.CraftCircuit;

    public CraftMinigameType Minigame => CraftMinigameType.Circuit;
}

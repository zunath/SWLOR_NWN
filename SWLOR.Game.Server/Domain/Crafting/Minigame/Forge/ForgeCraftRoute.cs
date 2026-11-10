using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame.Forge;

/// <summary>Smithery recipes are crafted in the Plasma Forge window.</summary>
public sealed class ForgeCraftRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Smithery;

    public GuiWindowType Window => GuiWindowType.Forge;

    public CraftMinigameType Minigame => CraftMinigameType.Forge;
}

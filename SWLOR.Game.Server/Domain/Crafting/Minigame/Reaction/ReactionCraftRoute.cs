using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame.Reaction;

/// <summary>Sends Espionage recipes to the Reaction window.</summary>
public sealed class ReactionCraftRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Espionage;

    public GuiWindowType Window => GuiWindowType.Reaction;

    public CraftMinigameType Minigame => CraftMinigameType.Reaction;
}

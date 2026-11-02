using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>Sends Espionage recipes to the Reaction window.</summary>
public sealed class ReactionCraftRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Espionage;

    public GuiWindowType Window => GuiWindowType.Reaction;

    public CraftMinigameType Minigame => CraftMinigameType.Reaction;
}

using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>Sends every Agriculture recipe, food, pet food and cooking enhancement alike, to the Kitchen window.</summary>
public sealed class KitchenCraftRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Agriculture;

    public GuiWindowType Window => GuiWindowType.Kitchen;

    public CraftMinigameType Minigame => CraftMinigameType.Kitchen;
}

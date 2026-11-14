using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame.Kitchen;

/// <summary>Sends every Agriculture recipe, food, pet food and cooking enhancement alike, to the Kitchen window.</summary>
public sealed class KitchenCraftRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Agriculture;

    public GuiWindowType Window => GuiWindowType.Kitchen;

    public CraftMinigameType Minigame => CraftMinigameType.Kitchen;
}

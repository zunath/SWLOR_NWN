using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame.Fabricator;

/// <summary>Fabrication recipes are crafted in the Fabricator window.</summary>
public sealed class FabricatorRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Fabrication;

    public GuiWindowType Window => GuiWindowType.CraftFabricator;

    public CraftMinigameType Minigame => CraftMinigameType.Fabricator;
}

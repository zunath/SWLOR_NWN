using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>Fabrication recipes are crafted in the Fabricator window.</summary>
public sealed class FabricatorRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Fabrication;

    public GuiWindowType Window => GuiWindowType.CraftFabricator;

    public CraftMinigameType Minigame => CraftMinigameType.Fabricator;
}

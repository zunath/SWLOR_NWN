using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>Smithery recipes are crafted in the Plasma Forge window.</summary>
public sealed class ForgeCraftRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Smithery;

    public GuiWindowType Window => GuiWindowType.Forge;

    public CraftMinigameType Minigame => CraftMinigameType.Forge;

    public double XpMultiplier => ForgeRules.XpMultiplier;
}

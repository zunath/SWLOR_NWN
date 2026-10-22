using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>Engineering recipes are crafted in the Circuit window.</summary>
public sealed class CircuitRoute : ICraftMinigameRoute
{
    public SkillType Skill => SkillType.Engineering;

    public GuiWindowType Window => GuiWindowType.CraftCircuit;

    public CraftMinigameType Minigame => CraftMinigameType.Circuit;

    public double XpMultiplier => CircuitRules.XpMultiplier;
}

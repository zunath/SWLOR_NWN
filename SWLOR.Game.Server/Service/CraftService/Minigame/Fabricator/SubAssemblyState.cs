namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>How far one sub-assembly has come: its progress toward its size, and its quality from 0 to 100.</summary>
public readonly record struct SubAssemblyState(int Progress, int Quality);

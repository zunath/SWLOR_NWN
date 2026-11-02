namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>What an action looks like in the window.</summary>
public enum ExperimentActionKind
{
    Normal,

    /// <summary>The action ends the experiment: the last trial, or Conclude.</summary>
    EndsExperiment,

    Unavailable
}

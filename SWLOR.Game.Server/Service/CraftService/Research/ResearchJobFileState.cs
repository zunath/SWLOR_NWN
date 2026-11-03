namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>How a research job stands against the character file the player just loaded.</summary>
public enum ResearchJobFileState
{
    /// <summary>The file holds every transfer the job made. Nothing to do.</summary>
    Current,

    /// <summary>The file predates the job's start: it still holds the blueprint and credits the start took, so the job is discarded.</summary>
    StartNotInFile,

    /// <summary>The file holds the blueprint the job handed back, so the job is done and is deleted.</summary>
    DeliveredInFile,

    /// <summary>The file predates the hand-back: the job keeps its blueprint copy and can be completed (or cancelled) again.</summary>
    DeliveryNotInFile,
}

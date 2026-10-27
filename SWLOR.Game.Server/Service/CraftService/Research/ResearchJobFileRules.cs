using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// Compares a research job with the character file sequence (<see cref="Minigame.CraftSessionFileSequence"/>) the player loaded.
/// Starting a job takes the blueprint and credits out of the character; completing or cancelling it puts a blueprint back. Each
/// transfer is stamped on the job before the character is saved, so a crash before that save is found at the next login.
/// </summary>
public static class ResearchJobFileRules
{
    public static ResearchJobFileState AtLogin(ResearchJob job, int loadedSequence)
    {
        if (job.StartSequence > loadedSequence)
            return ResearchJobFileState.StartNotInFile;

        if (job.DeliverySequence <= 0)
            return ResearchJobFileState.Current;

        return loadedSequence >= job.DeliverySequence
            ? ResearchJobFileState.DeliveredInFile
            : ResearchJobFileState.DeliveryNotInFile;
    }
}

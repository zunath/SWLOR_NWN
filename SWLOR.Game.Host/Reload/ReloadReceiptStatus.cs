namespace SWLOR.Game.Host.Reload;

/// <summary>Where an attempt to activate a reload candidate stands.</summary>
public enum ReloadReceiptStatus
{
    /// <summary>The host found the candidate and started the reload; a final receipt replaces this one.</summary>
    Accepted = 0,

    /// <summary>The candidate did not activate and the running build was left untouched (the detail says why).</summary>
    Rejected = 1,

    /// <summary>The candidate is the running build; the receipt carries the pause.</summary>
    Applied = 2,

    /// <summary>The reload failed after the running build was quiesced: the server runs no game build until it is restarted.</summary>
    RestartRequired = 3,

    /// <summary>The host could not run the reload at all (the detail holds the error); the running build was left untouched.</summary>
    Failed = 4
}

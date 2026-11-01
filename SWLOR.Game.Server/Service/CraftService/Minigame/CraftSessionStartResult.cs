namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>The result of starting a craft: the live session, or the reason nothing was taken.</summary>
public sealed class CraftSessionStartResult
{
    private CraftSessionStartResult(CraftSession session, string message)
    {
        Session = session;
        Message = message;
    }

    public bool Succeeded => Session != null;

    public CraftSession Session { get; }

    /// <summary>Why the craft did not start; empty on success.</summary>
    public string Message { get; }

    public static CraftSessionStartResult Started(CraftSession session) => new(session, string.Empty);

    public static CraftSessionStartResult Refused(string message) => new(null, message);
}

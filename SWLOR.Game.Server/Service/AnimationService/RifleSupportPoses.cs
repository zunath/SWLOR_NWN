namespace SWLOR.Game.Server.Service.AnimationService;

/// <summary>The independently fitted resting, ready, and firing rifle clips.</summary>
public sealed record RifleSupportPoses(string Idle, string Ready, string Shot)
{
    /// <summary>The matching full body idle used while this fitted rifle is equipped.</summary>
    public string BaseIdle => Idle[..^1] + "b";

    /// <summary>Selects the authored ability fitted to this rendered hand pair and rifle.</summary>
    public AnimationClip ResolveAuthored(AnimationClip clip) =>
        clip.Name == AuthoredAnimation.AimedShot.Name
            ? new AnimationClip(Idle[..^1] + "a", clip.Duration) : clip;
}

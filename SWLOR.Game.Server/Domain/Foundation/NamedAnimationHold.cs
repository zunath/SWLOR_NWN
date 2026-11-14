namespace SWLOR.Game.Server.Domain.Foundation;

/// <summary>How a named clip treats native item holding layers.</summary>
public enum NamedAnimationHold
{
    /// <summary>Lift a rifle, two-handed or polearm hold by playing through the one-shot Dodge Side carrier.</summary>
    Auto,
    /// <summary>Keep the native hold and play on the looping carrier, for clips authored around that hold.</summary>
    Keep,
    /// <summary>
    /// Play on the looping carrier and hold off a two-handed or polearm hold, for long loops that pose the arm
    /// themselves. A rifle hold is kept.
    /// </summary>
    LiftInPlace,
    /// <summary>Clear the existing left-hand torch pose once through the one-shot carrier.</summary>
    ClearTorch,
    /// <summary>Keep the looping carrier and suppress the left-hand torch pose after clearing it.</summary>
    LiftTorchInPlace
}

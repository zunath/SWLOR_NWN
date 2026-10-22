namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// What each action does against each move the fish can make. This is the only place the pairing is decided: the engine's
/// previews and results both read it.
/// </summary>
public static class LineEffects
{
    /// <summary>
    /// The effect of <paramref name="actionId"/> on a turn when the fish makes <paramref name="move"/>. Set Hook ignores the move,
    /// because the fish's first move is spent on the strike.
    /// </summary>
    public static LineEffect Of(LineSetup setup, string actionId, FishMove move)
    {
        var pull = setup.Pull;
        switch (actionId)
        {
            case LineActionIds.SetHook:
                return new LineEffect(0, LineRules.SetHookTension, -setup.SetHookDrain);

            case LineActionIds.Reel:
            {
                var distance = -setup.ReelDistance;
                var tension = setup.ReelTension;
                var stamina = 0;
                switch (move)
                {
                    case FishMove.Run:
                        tension += pull + LineRules.RunReelStrain;
                        break;
                    case FishMove.Dive:
                        tension += pull;
                        break;
                    case FishMove.Thrash:
                        tension += pull + LineRules.ThrashSpike;
                        break;
                    case FishMove.Tire:
                        distance -= LineRules.TireReelDistance;
                        stamina -= LineRules.TireStaminaLoss;
                        break;
                }

                return new LineEffect(distance, tension, stamina);
            }

            case LineActionIds.GiveLine:
            {
                var distance = setup.GiveLineDistance;
                var tension = -LineRules.GiveLineRelief;
                var stamina = 0;
                switch (move)
                {
                    case FishMove.Run:
                        distance += pull;
                        break;
                    case FishMove.Dive:
                        tension += Half(pull);
                        break;
                    case FishMove.Thrash:
                        tension += Half(pull + LineRules.ThrashSpike);
                        break;
                    case FishMove.Tire:
                        stamina -= LineRules.TireStaminaLoss;
                        break;
                }

                return new LineEffect(distance, tension, stamina);
            }

            case LineActionIds.Hold:
            {
                var distance = 0;
                var tension = LineRules.HoldTension;
                var stamina = -LineRules.HoldDrain;
                switch (move)
                {
                    case FishMove.Run:
                        tension += Half(pull);
                        distance += pull / 2;
                        break;
                    case FishMove.Dive:
                        tension += pull;
                        break;
                    case FishMove.Tire:
                        stamina -= LineRules.TireStaminaLoss;
                        break;
                }

                return new LineEffect(distance, tension, stamina);
            }

            default:
                throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));
        }
    }

    /// <summary>Half of a value, rounded up.</summary>
    private static int Half(int value) => (value + 1) / 2;
}

namespace SWLOR.CraftSim.Rng
{
    /// <summary>
    /// The first part of every random stream a gate takes from the seed, so two uses of the seed never share dice
    /// and a change to one gate cannot move another's results.
    /// </summary>
    public static class SimStreams
    {
        public const int Adaptive = 101;
        public const int AdaptivePolicy = 102;
        public const int SolverTraining = 103;
        public const int SolverSelection = 104;
        public const int FixedEvaluation = 105;
        public const int SolverSeedLines = 106;
        public const int Legacy = 107;
    }
}

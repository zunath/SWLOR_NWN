namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>The old minigame's mutable state; a struct so a lookahead can copy it freely.</summary>
    public struct LegacyCraftState
    {
        public int Progress;
        public int Quality;
        public int Durability;
        public int Cp;
        public bool SteadyHandActive;
        public bool MuscleMemoryActive;
        public int VenerationSteps;
        public int WasteNotSteps;
        public int Actions;
        public LegacyCraftOutcome Outcome;

        public readonly bool IsFinished => Outcome != LegacyCraftOutcome.InProgress;
    }
}

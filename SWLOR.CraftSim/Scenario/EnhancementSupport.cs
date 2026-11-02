namespace SWLOR.CraftSim.Scenario
{
    /// <summary>What a scenario does with <see cref="CraftEngineRequest.Enhancements"/>, which gate 7 (enhancement parity) needs to know.</summary>
    public enum EnhancementSupport
    {
        /// <summary>
        /// The scenario builds its engine without reading <see cref="CraftEngineRequest.Enhancements"/>, so an enhanced craft would play
        /// as easy as a plain one. Gate 7 is not run, and that blocks shipping the minigame until the scenario models enhancements.
        /// </summary>
        NotModeled,

        /// <summary>
        /// The scenario maps the request's total progress penalty onto its engine's own enhancement handling (the Forge's extra Shape,
        /// the Circuit's extra Assembly, the Kitchen's extra Doneness), so the progress meter's maximum rises with the enhancements.
        /// </summary>
        Modeled,

        /// <summary>The minigame has no enhancement slots (Reaction), so gate 7 does not apply.</summary>
        NoSlots
    }
}

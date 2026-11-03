namespace SWLOR.Game.Server.Service.AIService
{
    /// <summary>
    /// The local variable names that store a creature's AI profile. The profile decision engine lives in
    /// <c>Domain.Creatures.AIService</c>; these compile-time constants stay here because the toolset's creature editor
    /// reads them.
    /// </summary>
    public static class NPCAI
    {
        public const string ProfileLocalVariable = Domain.Creatures.AIService.ProfileLocalVariable;
        public const string ProfileIdLocalVariable = Domain.Creatures.AIService.ProfileIdLocalVariable;
    }
}

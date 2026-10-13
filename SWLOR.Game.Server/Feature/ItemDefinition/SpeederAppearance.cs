namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public static class SpeederAppearance
    {
        // Keep the vehicle's size aligned with GenerateSpeederRiderModels.py's seated skeletons.
        public static float GetBikeScale(string modelRace) => modelRace?.ToLowerInvariant() switch
        {
            "a" => 0.7f,
            "b" => 1.2f,
            "d" => 0.8f,
            "e" => 0.9f,
            "g" => 0.6f,
            "h" => 1f,
            "o" => 1.25f,
            "z" => 1f,
            _ => 1f,
        };
    }
}

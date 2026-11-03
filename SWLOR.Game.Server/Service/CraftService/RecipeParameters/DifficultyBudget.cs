namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// The difficulty range a profile allows for recipes of one level.
    /// </summary>
    public readonly record struct DifficultyBudget(double Min, double Max)
    {
        public bool Contains(double difficulty)
        {
            return difficulty >= Min && difficulty <= Max;
        }

        public override string ToString()
        {
            return $"[{Min}, {Max}]";
        }
    }
}

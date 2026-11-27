namespace SWLOR.Game.Server.Service.TownsfolkService
{
    /// <summary>How many of each group of street life a city should have right now.</summary>
    /// <param name="Wanderers">The wandering townsfolk.</param>
    /// <param name="Guards">The patrolling guards.</param>
    /// <param name="Critters">The street critters.</param>
    public readonly record struct TownsfolkTarget(int Wanderers, int Guards, int Critters)
    {
        /// <summary>Nothing: no street life at all.</summary>
        public static readonly TownsfolkTarget None = new(0, 0, 0);

        /// <summary>Everything the target asks for.</summary>
        public int Total => Wanderers + Guards + Critters;

        /// <summary>The count for one group.</summary>
        public int Get(TownsfolkGroup group)
        {
            switch (group)
            {
                case TownsfolkGroup.Wanderer:
                    return Wanderers;
                case TownsfolkGroup.Guard:
                    return Guards;
                case TownsfolkGroup.Critter:
                    return Critters;
                default:
                    return 0;
            }
        }
    }
}

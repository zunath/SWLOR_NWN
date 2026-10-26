namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Authored paired-shot payload. Costs and impacts belong to one activation.</summary>
    public sealed record TwinPistolAbilitySpec
    {
        public int FlatPower { get; init; }
        public int WeaponPowerPercent { get; init; } = 100;
        public int StaminaCost { get; init; }
        public int VolleyCount { get; init; } = 1;
        public float VolleyIntervalSeconds { get; init; }
        public float ChannelSeconds { get; init; }
        public int MomentumPowerPerCharge { get; init; }
        public bool GeneratesMomentum { get; init; }
        public bool SpendsMomentum { get; init; }
        public bool IsCrossfire { get; init; }
        public int RestoreStaminaOnHit { get; init; }
        public int OrdinaryBonusAttempts { get; init; }
        public int OrdinaryBonusDamagePercent { get; init; }
        public int OrdinaryBonusSeconds { get; init; }
        public TwinPistolMovementType Movement { get; init; }
        public float MaxRange { get; init; } = 25f;
        public float MaxMovementMeters { get; init; }
        public float MinimumMovementMeters { get; init; }
        public float ArrivalRangeMeters { get; init; } = 25f;
        public float StopDistanceMeters { get; init; }
        public int MaximumTargets { get; init; } = 1;
        public float LineWidthMeters { get; init; } = 4f;
        /// <summary>The dash ignores creature collision; walls, placeables and doors still block it.</summary>
        public bool PassesThroughCreatures { get; init; }
    }
}

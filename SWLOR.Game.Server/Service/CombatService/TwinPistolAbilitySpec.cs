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
        /// <summary>Replaces the generic per-charge Cadence bonus for this ability's spend.</summary>
        public int CadencePowerPerCharge { get; init; }
        public bool SpendsCadence { get; init; }
        /// <summary>Cadence granted once when the activation lands a hit, without needing a chain.</summary>
        public int CadenceOnHit { get; init; }
        public int RestoreStaminaOnHit { get; init; }
        public int OrdinaryBonusAttempts { get; init; }
        public int OrdinaryBonusDamagePercent { get; init; }
        public int OrdinaryBonusSeconds { get; init; }
        public float MaxRange { get; init; } = 25f;
    }
}

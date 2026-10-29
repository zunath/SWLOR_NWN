using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService;

/// <summary>Captured once at activation; one damage budget per real weapon across all its impacts.</summary>
public sealed class WeaponAbilityTraitActivation
{
    private readonly Dictionary<uint, (int Count, HashSet<int> Waves)> _claimed = new();
    public PairedRangedAbilityTraitsSnapshot Snapshot { get; private set; }
    public int DamagePerCharge { get; }
    public int PaidStamina { get; set; }
    public bool IsCommitted { get; set; }
    public bool RecoveryCompleted { get; set; }
    public bool IntrinsicCompleted { get; set; }
    /// <summary>The first landed hit has been reported; later hands, targets or waves add nothing.</summary>
    public bool CadenceHitObserved { get; set; }
    /// <summary>Paid within the chain window after a different weapon ability.</summary>
    public bool ContinuesChain { get; set; }
    /// <summary>Cadence the ability itself grants on a landed hit, without a chain.</summary>
    public int CadenceOnHit { get; init; }
    public uint PrimaryWeapon { get; set; }

    public WeaponAbilityTraitActivation(PairedRangedAbilityTraitsSnapshot snapshot, int damagePerCharge)
    {
        Snapshot = snapshot;
        DamagePerCharge = System.Math.Max(0, damagePerCharge);
    }

    /// <summary>Cadence a landed hit earns: the larger of the chain's charge and the ability's own grant.</summary>
    public int CadenceEarnedOnHit => System.Math.Max(ContinuesChain ? 1 : 0, CadenceOnHit);

    public void SetPaidStamina(int paidStamina)
    {
        PaidStamina = System.Math.Max(0, paidStamina);
        if (PaidStamina == 0)
            Snapshot = Snapshot with { SpendsCadence = false, CadenceSpent = 0, CadenceChargeIds = System.Array.Empty<long>() };
    }

    /// <summary>Returns the pre-partition bonus; a multi-wave profile partitions the resulting full damage exactly once.</summary>
    public int TryTakeDamageBonus(uint weapon, int volleyIndex = 0, int volleyCount = 1)
    {
        if (!IsCommitted || Snapshot.CadenceSpent <= 0 || volleyCount < 1 || volleyIndex < 0 || volleyIndex >= volleyCount)
            return 0;
        if (!_claimed.TryGetValue(weapon, out var budget))
            _claimed[weapon] = budget = (volleyCount, new HashSet<int>());
        return budget.Count == volleyCount && budget.Waves.Add(volleyIndex)
            ? Snapshot.CadenceSpent * DamagePerCharge : 0;
    }
}

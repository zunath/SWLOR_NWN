#nullable enable
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The damage records part with the two native reads answered by the test: the hit points a creature has apart from
/// temporary ones and the temporary pools it holds. The two dictionaries are the recorder probe's, so a test sets a
/// creature's hit points in one place.
/// </summary>
internal sealed class RecorderProbeDamage : CombatRecordDamageService
{
    private readonly Dictionary<uint, int> _hitPoints;
    private readonly Dictionary<uint, CombatRecordTemporaryHitPointPool[]> _pools;

    public RecorderProbeDamage(
        ICombatRecordSession session,
        ICombatRecorderService recorder,
        Dictionary<uint, int> hitPoints,
        Dictionary<uint, CombatRecordTemporaryHitPointPool[]> pools)
        : base(session, recorder)
    {
        _hitPoints = hitPoints;
        _pools = pools;
    }

    protected override int GetHitPointsExcludingTemporary(uint creature) => _hitPoints.GetValueOrDefault(creature, 80);

    protected override CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(uint creature) =>
        _pools.GetValueOrDefault(creature)!;
}

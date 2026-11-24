#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The ability records part with the FP and stamina reads answered by the test: the two dictionaries are the recorder
/// probe's, so a test sets a creature's resources in one place.
/// </summary>
internal sealed class RecorderProbeAbility : CombatRecordAbilityService
{
    private readonly Dictionary<uint, int> _fp;
    private readonly Dictionary<uint, int> _stamina;

    public RecorderProbeAbility(ICombatRecordSession session, ICombatRecorderService recorder, Dictionary<uint, int> fp, Dictionary<uint, int> stamina)
        : base(session, recorder, FacadeForwarder.For<IStatService>())
    {
        _fp = fp;
        _stamina = stamina;
    }

    protected override int GetCurrentFP(uint creature, Player dbPlayer = null!) => _fp.GetValueOrDefault(creature, 11);

    protected override int GetCurrentStamina(uint creature, Player dbPlayer = null!) => _stamina.GetValueOrDefault(creature, 33);
}

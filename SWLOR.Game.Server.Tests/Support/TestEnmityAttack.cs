#nullable enable
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The enmity attack part over scripted neighbours; the unconverted hot core is answered by the overrides.</summary>
public sealed class TestEnmityAttack : EnmityAttackService
{
    public TestEnmityAttack(IEnmityService table, ICombatLeash leash, IActivityService activity)
        : base(table, leash, activity, FacadeForwarder.For<IStealthService>(), FacadeForwarder.For<ICompanionTargetQuery>(),
            FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatAttackTimingService>(),
            FacadeForwarder.For<ICombatQueriesService>())
    {
    }

    public HashSet<(uint Observer, uint Target)> Hidden { get; } = new();
    public HashSet<uint> Companions { get; } = new();
    public Dictionary<uint, uint> CompanionTargets { get; } = new();
    public HashSet<(uint, uint)> DeniedCommands { get; } = new();
    public SkillType WeaponSkill { get; set; } = SkillType.Lightsaber;
    public bool RecentAttack { get; set; }
    public List<string> Logs { get; } = new();

    protected override bool IsHiddenFrom(uint observer, uint target) => Hidden.Contains((observer, target));

    protected override bool IsRegisteredCompanion(uint creature) => Companions.Contains(creature);

    protected override uint PeekAuthorizedTarget(uint companion) =>
        CompanionTargets.TryGetValue(companion, out var target) ? target : 0x7F000000;

    protected override bool CanIssueAttackCommand(uint companion, uint target) => !DeniedCommands.Contains((companion, target));

    protected override SkillType GetEquippedWeaponSkillType(uint creature) => WeaponSkill;

    protected override float GetWeaponEngagementRange(SkillType skillType) => IsRangedWeaponSkill(skillType) ? 10f : 1.5f;

    protected override bool IsRangedWeaponSkill(SkillType skillType) => skillType == SkillType.Rifle;

    protected override bool HasRecentAttackActivity(uint creature, float windowSeconds) => RecentAttack;

    protected override int CalculateAttackDelay(uint attacker) => 1500;

    protected override int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds) => attackerDelayMilliseconds;

    protected override int CalculateAttackSwingDelay(int effectiveDelayMilliseconds) => effectiveDelayMilliseconds;

    protected override void WriteAILog(string details) => Logs.Add(details);
}

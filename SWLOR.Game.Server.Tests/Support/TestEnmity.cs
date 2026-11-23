#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class TestEnmity : EnmityService
{
    public TestEnmity(
        ICombatLeash leash,
        IActivityService activity,
        IPartyService party,
        IHoloComService holoCom,
        ICompanionLookup beastMastery)
        : base(leash, activity, party, holoCom, beastMastery, FacadeForwarder.For<IStatService>(), FacadeForwarder.For<IStealthService>(),
            FacadeForwarder.For<ICompanionTargetQuery>(), FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatAttackTimingService>(), FacadeForwarder.For<ICombatQueriesService>())
    {
    }

    public Dictionary<(uint Creature, StatType Stat), int> StatAdjustments { get; } = new();
    public HashSet<(uint Observer, uint Target)> Hidden { get; } = new();
    public HashSet<uint> Droids { get; } = new();
    public HashSet<uint> Companions { get; } = new();
    public Dictionary<uint, uint> CompanionTargets { get; } = new();
    public HashSet<(uint, uint)> DeniedCommands { get; } = new();
    public Dictionary<uint, HashSet<IStatusEffect>> Effects { get; } = new();
    public SkillType WeaponSkill { get; set; } = SkillType.Lightsaber;
    public bool RecentAttack { get; set; }
    public List<string> Logs { get; } = new();

    public void RemoveEnmityAndKeepProximity(uint creature, uint enemy)
    {
        var table = (Dictionary<uint, Dictionary<uint, int>>)typeof(EnmityService)
            .GetField("_enemyEnmityTables", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(this)!;
        table[enemy].Remove(creature);
    }

    protected override int GetStatAdjustment(uint creature, StatType stat) =>
        StatAdjustments.GetValueOrDefault((creature, stat));

    protected override bool IsHiddenFrom(uint observer, uint target) => Hidden.Contains((observer, target));

    protected override bool IsDroid(uint creature) => Droids.Contains(creature);

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

    protected override HashSet<IStatusEffect> GetAllStatusEffects(uint creature) =>
        Effects.GetValueOrDefault(creature) ?? new HashSet<IStatusEffect>();

    protected override void WriteAILog(string details) => Logs.Add(details);
}

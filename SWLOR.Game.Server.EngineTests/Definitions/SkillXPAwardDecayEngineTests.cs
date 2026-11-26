using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// A rank gained at the skill cap decays one skill, picked with <c>Random.Next</c> over the candidates. These tests leave exactly one
/// unlocked candidate so the pick is not left to chance, award the rank through <c>GiveSkillXP</c>, and read what the two handlers
/// of <c>SWLOR_SKILL_LOST_BY_DECAY</c> did on the live server: the perk whose skill requirement the decay broke is refunded (its SP
/// returned, its feat removed), and a decayed Mimicry rank unequips the technique it no longer supports. Each case asserts that the
/// event was signaled once, for the decayed skill, on the player.
/// </summary>
public sealed class SkillXPAwardDecayEngineTests
{
    public SkillXPAwardDecayEngineTests(ISkillService skill, ISkillXPService skillXP, IPerkService perk, IAbilityCatalogService abilityCatalog, IDBService db)
    {
        Skill = skill;
        SkillXP = skillXP;
        Perk = perk;
        AbilityCatalog = abilityCatalog;
        DB = db;
    }

    private ISkillService Skill { get; }
    private ISkillXPService SkillXP { get; }
    private IPerkService Perk { get; }
    private IAbilityCatalogService AbilityCatalog { get; }
    private IDBService DB { get; }

    private const string Category = "SkillXPAwardDecay";
    private const SkillType GainedSkill = SkillType.Piloting;
    private const PerkType GatedPerk = PerkType.Provoke;
    private const FeatType GatedPerkFeat = FeatType.Provoke1;
    private const FeatType Technique = FeatType.BrutalBashTechnique;

    private static readonly List<(uint Player, SkillType Skill)> Decays = new();

    /// <summary>
    /// Counts the signal without a script handler: the events plugin is wrapped around the award inside the creature context (the override is per async flow, and the engine runs that context in its own), every call goes on to the
    /// real plugin, and the signal is recorded with the skill id pushed just before it.
    /// </summary>
    public class SignalSpy : DispatchProxy
    {
        public IEventsPlugin Target { get; set; }

        private string _pendingSkill;

        public static IDisposable Install()
        {
            var spy = DispatchProxy.Create<IEventsPlugin, SignalSpy>();
            ((SignalSpy)(object)spy).Target = EventsPluginRuntime.Current;
            return EventsPluginRuntime.Override(spy);
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod.Name == nameof(IEventsPlugin.PushEventData) && (string)args[0] == "SKILL_TYPE_ID")
                _pendingSkill = (string)args[1];
            else if (targetMethod.Name == nameof(IEventsPlugin.SignalEvent) && (string)args[0] == "SWLOR_SKILL_LOST_BY_DECAY")
                Decays.Add(((uint)args[1], (SkillType)int.Parse(_pendingSkill)));

            try
            {
                return targetMethod.Invoke(Target, args);
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
                throw;
            }
        }
    }

    [EngineTest("A decayed skill that gates a purchased perk refunds the perk level", Category = Category, TimeoutSeconds = 60f)]
    public async Task DecayRefundsThePerkItGates(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var requirement = GetPerkSkillRequirement(ctx, GatedPerk, 1, out var gatingSkill);
        ctx.Assert(gatingSkill != SkillType.Mimicry && gatingSkill != GainedSkill, "The gated perk depends on a skill other than the two the case uses");
        var price = Perk.GetPerkDetails(GatedPerk).PerkLevels[1].Price;
        fixture.Update(record =>
        {
            AtTheCap(record, GainedSkill, gatingSkill, requirement);
            record.Perks[GatedPerk] = 1;
            record.UnallocatedSP = 0;
        });
        await ctx.ExecuteInCreatureContextAsync(player, () => PerkGrantedFeat.SyncGrantedFeats(player, GatedPerk, 1, true));
        ctx.Assert(CreaturePlugin.GetKnowsFeat(player, GatedPerkFeat), "The purchased perk's feat is known before the decay");
        Decays.Clear();

        var awarded = 0;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            using var spy = SignalSpy.Install();
            awarded = SkillXP.GiveSkillXP(player, GainedSkill, Skill.GetRequiredXP(0), true, false);
        });
        await ctx.WaitFrameAsync();

        var record = DB.Get<Player>(fixture.Id);
        ctx.AssertEqual(Skill.GetRequiredXP(0), awarded, "The whole award is used");
        ctx.AssertEqual(1, record.Skills[GainedSkill].Rank, "The gained skill rises by a rank");
        ctx.AssertEqual(requirement - 1, record.Skills[gatingSkill].Rank, "The only unlocked skill decays by a rank");
        ctx.AssertEqual(0, record.Perks[GatedPerk], "The perk level its skill requirement no longer supports is removed");
        ctx.AssertEqual(price, record.UnallocatedSP, "The refunded level returns its price");
        ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, GatedPerkFeat), "The refunded level's feat is removed");
        AssertOneDecay(ctx, player, gatingSkill);
    }

    [EngineTest("A decayed Mimicry rank unequips the technique it no longer supports", Category = Category, TimeoutSeconds = 60f)]
    public async Task MimicryDecayUnequipsTheTechnique(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var requirement = AbilityCatalog.GetAbilityDetail(Technique).MimicrySkillRequirement;
        ctx.Assert(requirement > 15, "The technique needs a higher Mimicry rank than the analyzer's second level, so only the technique is lost");
        fixture.Update(record =>
        {
            AtTheCap(record, GainedSkill, SkillType.Mimicry, requirement);
            record.Perks[PerkType.CombatAnalyzer] = 2;
            record.UnallocatedSP = 0;
            record.LearnedTechniques[Technique] = DateTime.UtcNow;
            record.EquippedTechniques.Add(Technique);
        });
        await ctx.ExecuteInCreatureContextAsync(player, () => CreaturePlugin.AddFeat(player, Technique));
        ctx.Assert(CreaturePlugin.GetKnowsFeat(player, Technique), "The equipped technique's feat is known before the decay");
        Decays.Clear();

        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            using var spy = SignalSpy.Install();
            SkillXP.GiveSkillXP(player, GainedSkill, Skill.GetRequiredXP(0), true, false);
        });
        await ctx.WaitFrameAsync();

        var record = DB.Get<Player>(fixture.Id);
        ctx.AssertEqual(1, record.Skills[GainedSkill].Rank, "The gained skill rises by a rank");
        ctx.AssertEqual(requirement - 1, record.Skills[SkillType.Mimicry].Rank, "Mimicry is the only unlocked skill and decays by a rank");
        ctx.AssertEqual(0, record.EquippedTechniques.Count, "The technique whose Mimicry requirement is no longer met is unequipped");
        ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, Technique), "The unequipped technique's feat is removed");
        ctx.AssertEqual(2, record.Perks[PerkType.CombatAnalyzer], "The analyzer perk does not depend on the lost rank, so it keeps its level");
        ctx.AssertEqual(0, record.UnallocatedSP, "Nothing is refunded");
        AssertOneDecay(ctx, player, SkillType.Mimicry);
    }

    private int GetPerkSkillRequirement(EngineTestContext ctx, PerkType perk, int level, out SkillType skill)
    {
        var requirements = Perk.GetPerkDetails(perk).PerkLevels[level].Requirements.OfType<PerkRequirementSkill>().ToArray();
        ctx.AssertEqual(1, requirements.Length, $"{perk} level {level} has one skill requirement");
        skill = requirements[0].Type;
        return requirements[0].RequiredRank;
    }

    /// <summary>
    /// Shapes the record so a rank gained by <paramref name="gained"/> must decay <paramref name="candidate"/> and nothing else: the
    /// candidate holds the only unlocked rank, every other skill that counts toward the cap is locked at the ranks that bring the total to
    /// the cap, and the gained skill starts at rank 0. Skill and ability points are used up so the rank earns none.
    /// </summary>
    private void AtTheCap(Player record, SkillType gained, SkillType candidate, int candidateRank)
    {
        foreach (var skill in Enum.GetValues<SkillType>())
            record.Skills[skill] = new PlayerSkill();

        record.Skills[candidate].Rank = candidateRank;
        var remaining = SkillService.SkillCap - candidateRank;
        foreach (var skill in Skill.GetAllContributingSkills().Keys.Where(skill => skill != gained && skill != candidate).OrderBy(skill => (int)skill))
        {
            var rank = Math.Min(Skill.GetSkillDetails(skill).MaxRank, remaining);
            if (rank <= 0)
                break;

            record.Skills[skill].Rank = rank;
            record.Skills[skill].IsLocked = true;
            remaining -= rank;
        }

        if (remaining > 0)
            throw new InvalidOperationException($"The cap cannot be filled: {remaining} ranks short.");

        record.TotalSPAcquired = SkillService.SkillCap;
        record.TotalAPAcquired = SkillService.SkillCap / 10;
        record.XPDebt = 0;
    }

    private static void AssertOneDecay(EngineTestContext ctx, uint player, SkillType skill)
    {
        ctx.AssertEqual(1, Decays.Count, "SWLOR_SKILL_LOST_BY_DECAY is signaled once");
        ctx.AssertEqual(player, Decays[0].Player, "The event is signaled on the player");
        ctx.AssertEqual(skill, Decays[0].Skill, "The event names the decayed skill");
    }
}

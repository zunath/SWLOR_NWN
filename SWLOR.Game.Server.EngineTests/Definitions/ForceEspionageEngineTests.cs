using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Shared Espionage for Force Sensitive characters: the shared Stealth perk, Cloud Mind blinding an
/// NPC to stealth, Shared Presence granting stealth to party members, and Force Decoy on organic
/// versus droid attackers.
/// </summary>
public static class ForceEspionageEngineTests
{
    private const string OrganicResref = "nw_bandit001";
    private const string DroidResref = "malspiderdroid";
    private const float SharedPresenceWindowSeconds = 30f;

    [EngineTest("A Force Sensitive player with the Stealth perk enters stealth and pays its upkeep",
        Category = "ForceEspionage", TimeoutSeconds = 60f)]
    public static async Task ForcePlayer_UsesSharedStealth(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.CharacterType = CharacterType.ForceSensitive;
            record.Perks[PerkType.Stealth] = 1;
        });

        await SetStealthAsync(ctx, player.Creature, true);
        ctx.Assert(StatusEffect.HasStatusEffect<StealthStatusEffect>(player.Creature),
            "a Force Sensitive player in stealth carries the Stealth status");
        ctx.Assert(!Stealth.HasGrantedStealth(player.Creature), "their own Stealth perk is not granted stealth");
        await SetStealthAsync(ctx, player.Creature, false);
    }

    [EngineTest("Cloud Mind blinds an NPC to a stealthed player until the NPC takes damage",
        Category = "ForceEspionage", TimeoutSeconds = 60f)]
    public static async Task CloudMind_BlindsObserverUntilDamaged(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.Perks[PerkType.Stealth] = 1;
            record.Stealth = -1000;
        });
        await SetStealthAsync(ctx, player.Creature, true);

        // A neutral observer sees the stealthed player without forcing them out of stealth, so the
        // test can roll its verdict many times.
        var observer = ctx.SpawnCreature(OrganicResref, 3f);
        SetAILevel(observer, AILevel.VeryLow);
        ChangeToStandardFaction(observer, StandardFaction.Commoner);
        await ctx.WaitFrameAsync();

        ctx.Assert(CountDetections(observer, player.Creature, 20) > 0,
            "without Cloud Mind the observer spots a player with no Stealth rating");

        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            StatusEffect.ApplyStatusEffect<CloudedMindStatusEffect>(player.Creature, observer, 30f));
        ctx.Assert(StatusEffect.HasStatusEffect<CloudedMindStatusEffect>(observer), "the observer is clouded");
        ctx.AssertEqual(0, CountDetections(observer, player.Creature, 40),
            "a clouded NPC never spots anyone in stealth");
        ctx.Assert(GetActionMode(player.Creature, ActionMode.Stealth), "the player stays in stealth");

        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            StatusEffect.NotifyDamageStatusEffects(player.Creature, observer, 1, CombatDamageType.Physical));
        ctx.Assert(!StatusEffect.HasStatusEffect<CloudedMindStatusEffect>(observer),
            "taking damage clears the observer's mind");
        ctx.Assert(CountDetections(observer, player.Creature, 20) > 0,
            "once the cloud lifts the observer spots the player again on its next check");

        await SetStealthAsync(ctx, player.Creature, false);
    }

    [EngineTest("Cloud Mind activation clouds an organic hostile without breaking the caster's stealth or starting combat",
        Category = "ForceEspionage", TimeoutSeconds = 60f)]
    public static async Task CloudMind_ActivationKeepsCasterHidden(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.CharacterType = CharacterType.ForceSensitive;
            record.Perks[PerkType.Stealth] = 4;
            record.Perks[PerkType.CloudMind] = 1;
            record.Stealth = 1000;
        });
        ctx.SetResources(player.Creature, 50, 50);
        CreaturePlugin.AddFeat(player.Creature, FeatType.CloudMind1);
        await SetStealthAsync(ctx, player.Creature, true);

        var guard = ctx.SpawnCreature(OrganicResref, 3f);
        SetAILevel(guard, AILevel.VeryLow);
        SetIsTemporaryEnemy(player.Creature, guard);
        await ctx.WaitFrameAsync();
        ctx.Assert(Stealth.IsHiddenFrom(guard, player.Creature), "the guard has not spotted the hidden caster");

        var activated = false;
        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            activated = UsePerkFeat.TryUseAbility(player.Creature, guard, FeatType.CloudMind1, GetLocation(guard)));
        ctx.Assert(activated, $"Cloud Mind activates: {Ability.GetLastActivationDenialReason()}");

        await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect<CloudedMindStatusEffect>(guard), 10f,
            "Cloud Mind to cloud the guard after its cast");
        ctx.Assert(GetActionMode(player.Creature, ActionMode.Stealth), "Cloud Mind does not break the caster's stealth");
        ctx.Assert(!Enmity.GetEnmityTable(guard).ContainsKey(player.Creature), "Cloud Mind creates no enmity");
        ctx.Assert(!GetIsInCombat(player.Creature), "Cloud Mind does not start combat");

        await SetStealthAsync(ctx, player.Creature, false);
    }

    [EngineTest("Enemies inside aggro range attack a player who leaves stealth, clouded or not",
        Category = "ForceEspionage", TimeoutSeconds = 60f)]
    public static async Task LeavingStealth_InsideAggroRange_AcquiresAggro(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.CharacterType = CharacterType.ForceSensitive;
            record.Perks[PerkType.Stealth] = 4;
            record.Stealth = 1000;
        });
        SetPlotFlag(player.Creature, true);
        await SetStealthAsync(ctx, player.Creature, true);

        var clouded = ctx.SpawnCreature(OrganicResref, 3f);
        var unclouded = ctx.SpawnCreature(OrganicResref, 4f);
        var distant = ctx.SpawnCreature(OrganicResref, 25f);
        foreach (var creature in new[] { clouded, unclouded, distant })
        {
            SetIsTemporaryEnemy(player.Creature, creature);
        }

        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            StatusEffect.ApplyStatusEffect<CloudedMindStatusEffect>(player.Creature, clouded, 30f));
        await ctx.DelaySecondsAsync(2f);
        ctx.Assert(!Enmity.GetEnmityTable(clouded).ContainsKey(player.Creature), "the clouded enemy ignores the hidden player");
        ctx.Assert(!Enmity.GetEnmityTable(unclouded).ContainsKey(player.Creature), "the other enemy has not found the hidden player");

        await SetStealthAsync(ctx, player.Creature, false);
        await ctx.WaitUntilAsync(() => Enmity.GetEnmityTable(clouded).ContainsKey(player.Creature), 5f,
            "the clouded enemy to notice the visible player");
        await ctx.WaitUntilAsync(() => Enmity.GetEnmityTable(unclouded).ContainsKey(player.Creature), 5f,
            "the other enemy to notice the visible player");
        ctx.Assert(!Enmity.GetEnmityTable(distant).ContainsKey(player.Creature), "an enemy beyond aggro range stays idle");
    }

    [EngineTest("Force Decoy takes its percentage off the attacker's Accuracy rating, only against the caster",
        Category = "ForceEspionage", TimeoutSeconds = 60f)]
    public static async Task ForceDecoy_ReducesAccuracyRatingByPercent(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        using var other = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
        var bug = ctx.SpawnCreature("vdathswampland", 3f);

        await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            StatusEffect.ApplyStatusEffect(player.Creature, bug, new DecoyStatusEffect(8), 30f));

        ctx.AssertEqual(184, Combat.ApplyStatusSourceAccuracyModifiers(bug, player.Creature, 200), "8% off a rating of 200");
        ctx.AssertEqual(200, Combat.ApplyStatusSourceAccuracyModifiers(bug, other.Creature, 200), "no penalty against anyone else");
    }

    [EngineTest("Shared Presence hides a party member without the Stealth perk, without upkeep, for its window",
        Category = "ForceEspionage", TimeoutSeconds = 120f)]
    public static async Task SharedPresence_GrantsTimedStealthToMembers(EngineTestContext ctx)
    {
        using var caster = await CreateSharedPresenceCasterAsync(ctx);
        using var member = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
        member.Update(record => record.Perks.Remove(PerkType.Stealth));
        var baseStealth = Stat.GetStealth(member.Creature);
        var casterBonus = Stat.GetStatAdjustment(caster.Creature, StatType.ActiveStealthBonus);
        ctx.Assert(casterBonus > 0, "the caster's Stealth perk grants an active bonus");

        try
        {
            await JoinPartyAsync(ctx, caster.Creature, member.Creature);
            await CastSharedPresenceAsync(ctx, caster.Creature);
            await ctx.WaitUntilAsync(() => GetActionMode(member.Creature, ActionMode.Stealth), 5f,
                "the party member to enter granted stealth");

            ctx.Assert(GetActionMode(caster.Creature, ActionMode.Stealth), "the caster enters stealth");
            ctx.Assert(!Stealth.HasGrantedStealth(caster.Creature), "the caster uses the normal Stealth perk path");
            ctx.Assert(Stealth.HasGrantedStealth(member.Creature), "the member's stealth is granted");
            ctx.Assert(StatusEffect.HasStatusEffect<StealthStatusEffect>(member.Creature), "the member carries the Stealth status");
            ctx.AssertEqual(baseStealth + casterBonus, Stat.GetStealth(member.Creature),
                "member Stealth is their own rating plus the caster's perk bonus");

            var stamina = Stat.GetCurrentStamina(member.Creature);
            await ctx.DelaySecondsAsync(8f);
            ctx.AssertEqual(stamina, Stat.GetCurrentStamina(member.Creature), "granted stealth drains no stamina");

            await ctx.WaitUntilAsync(() => !GetActionMode(member.Creature, ActionMode.Stealth) &&
                                           !GetActionMode(caster.Creature, ActionMode.Stealth),
                SharedPresenceWindowSeconds + 5f, "the 30 second window to end both stealths");
            ctx.Assert(!Stealth.HasGrantedStealth(member.Creature), "the grant ends with its window");

            await ctx.ExecuteInCreatureContextAsync(member.Creature, () =>
                SetActionMode(member.Creature, ActionMode.Stealth, true));
            await ctx.DelaySecondsAsync(1f);
            ctx.Assert(!GetActionMode(member.Creature, ActionMode.Stealth),
                "without the Stealth perk the member cannot re-enter stealth after the window");
        }
        finally
        {
            Party.RemoveFromCurrentParty(member.Creature);
        }
    }

    [EngineTest("Shared Presence grants end when the member leaves stealth or dies",
        Category = "ForceEspionage", TimeoutSeconds = 120f)]
    public static async Task SharedPresence_GrantEndsOnExitAndDeath(EngineTestContext ctx)
    {
        using var caster = await CreateSharedPresenceCasterAsync(ctx);
        using var member = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
        member.Update(record => record.Perks.Remove(PerkType.Stealth));

        try
        {
            await JoinPartyAsync(ctx, caster.Creature, member.Creature);

            // Leaving stealth by hand releases the grant at once.
            await CastSharedPresenceAsync(ctx, caster.Creature);
            await ctx.WaitUntilAsync(() => GetActionMode(member.Creature, ActionMode.Stealth), 5f,
                "the party member to enter granted stealth");
            await SetStealthAsync(ctx, member.Creature, false);
            ctx.Assert(!Stealth.HasGrantedStealth(member.Creature), "leaving stealth ends the grant");
            await ctx.ExecuteInCreatureContextAsync(member.Creature, () =>
                SetActionMode(member.Creature, ActionMode.Stealth, true));
            await ctx.DelaySecondsAsync(1f);
            ctx.Assert(!GetActionMode(member.Creature, ActionMode.Stealth), "the member cannot re-enter without the perk");

            // Death releases a grant whose window is still open.
            await SetStealthAsync(ctx, caster.Creature, false);
            await CastSharedPresenceAsync(ctx, caster.Creature);
            await ctx.WaitUntilAsync(() => Stealth.HasGrantedStealth(member.Creature), 5f, "a second grant");
            SetPlotFlag(member.Creature, false);
            await ctx.ExecuteInCreatureContextAsync(member.Creature, () =>
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), member.Creature));
            await ctx.WaitUntilAsync(() => GetIsDead(member.Creature), 5f, "the member to die");
            await ctx.WaitFrameAsync();
            ctx.Assert(!Stealth.HasGrantedStealth(member.Creature), "death ends the grant");
            ReviveRulesEngineTests.DestroyCorpsePlaceable(member.Creature);
        }
        finally
        {
            ReviveRulesEngineTests.ReleaseBody(member.Creature);
            Party.RemoveFromCurrentParty(member.Creature);
        }
    }

    [EngineTest("Force Decoy decoys organic attackers but not droids",
        Category = "ForceEspionage", TimeoutSeconds = 60f)]
    public static async Task ForceDecoy_DecoysOnlyOrganicAttackers(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.CharacterType = CharacterType.ForceSensitive;
            record.Perks[PerkType.ForceDecoy] = 2;
        });

        var organic = ctx.SpawnCreature(OrganicResref, 3f);
        var droid = ctx.SpawnCreature(DroidResref, -3f);
        foreach (var enemy in new[] { organic, droid })
        {
            SetAILevel(enemy, AILevel.VeryLow);
            SetIsTemporaryEnemy(player.Creature, enemy);
        }

        await ctx.WaitFrameAsync();
        foreach (var enemy in new[] { organic, droid })
        {
            Enmity.ModifyEnmity(player.Creature, enemy, 100);
            ctx.AssertEqual(player.Creature, Enmity.GetHighestEnmityAttackTarget(enemy), "the enemy targets the player");
        }

        var ability = Ability.GetAbilityDetail(FeatType.ForceDecoy2);
        await ctx.ExecuteInCreatureContextAsync(player.Creature,
            () => ability.ImpactAction(player.Creature, player.Creature, 2, GetLocation(player.Creature)));

        var decoy = StatusEffect.GetStatusEffect(organic, typeof(DecoyStatusEffect), player.Creature) as DecoyStatusEffect;
        ctx.Assert(decoy != null, "the organic attacker suffers Decoy from the caster");
        ctx.AssertEqual(12, decoy!.AccuracyPenaltyPercent, "Force Decoy II accuracy penalty");
        ctx.Assert(!StatusEffect.HasStatusEffect<DecoyStatusEffect>(droid), "droids are never decoyed");
        ctx.Assert(Enmity.GetEnmityTable(droid)[player.Creature] < 100, "the droid's enmity on the caster still drops");
    }

    private static async Task<PlayerAbilityFixture> CreateSharedPresenceCasterAsync(EngineTestContext ctx)
    {
        var caster = await PlayerAbilityFixture.CreateAsync(ctx);
        caster.Update(record =>
        {
            record.CharacterType = CharacterType.ForceSensitive;
            record.Perks[PerkType.Stealth] = 2;
            record.Perks[PerkType.SharedPresence] = 1;
        });
        ctx.Assert(!GetActionMode(caster.Creature, ActionMode.Stealth), "the caster starts out of stealth");
        return caster;
    }

    private static async Task CastSharedPresenceAsync(EngineTestContext ctx, uint caster)
    {
        var ability = Ability.GetAbilityDetail(FeatType.SharedPresence);
        ctx.AssertEqual(string.Empty, ability.CustomValidation(caster, caster, 1, GetLocation(caster)),
            "Shared Presence is usable out of combat");
        await ctx.ExecuteInCreatureContextAsync(caster,
            () => ability.ImpactAction(caster, caster, 1, GetLocation(caster)));
        await ctx.WaitUntilAsync(() => GetActionMode(caster, ActionMode.Stealth), 5f,
            "Shared Presence to hide the caster");
    }

    private static async Task JoinPartyAsync(EngineTestContext ctx, uint leader, uint member)
    {
        await ctx.ExecuteInCreatureContextAsync(member, () =>
            typeof(Party).GetMethod("AddToParty", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { leader, member }));
        ctx.Assert(Party.IsInParty(leader, member), "the member joins the caster's party");
    }

    private static int CountDetections(uint observer, uint target, int rolls)
    {
        var detections = 0;
        for (var roll = 0; roll < rolls; roll++)
        {
            Stealth.ClearVerdictsForObserver(observer);
            if (!Stealth.IsHiddenFrom(observer, target))
                detections++;
        }

        return detections;
    }

    private static async Task SetStealthAsync(EngineTestContext ctx, uint creature, bool enabled)
    {
        await ctx.ExecuteInCreatureContextAsync(creature, () => SetActionMode(creature, ActionMode.Stealth, enabled));
        await ctx.WaitUntilAsync(() => GetActionMode(creature, ActionMode.Stealth) == enabled, 5f,
            enabled ? "the player to enter stealth" : "the player to leave stealth");
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using TelegraphInstance = SWLOR.Game.Server.Domain.Combat.TelegraphService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class TelegraphTests
{
    private const int PackedColorShift = 3;
    private const int PackedColorMask = 0x3;
    private const int PackedRotationShift = 21;
    private const int PackedRotationMask = 0x3ff;

    [TestCase(TelegraphColorType.Hostile, 0)]
    [TestCase(TelegraphColorType.Self, 1)]
    [TestCase(TelegraphColorType.Beneficial, 2)]
    [TestCase(TelegraphColorType.PartyMember, 3)]
    public void PackTelegraphData_UsesTheShaderColorProtocol(
        TelegraphColorType colorType,
        int expectedPackedColor)
    {
        var packed = InvokePackTelegraphData(TelegraphType.Sphere, colorType, 0f);

        ExtractPackedColor(packed).Should().Be(expectedPackedColor);
    }

    [Test]
    public void ShaderPalette_MatchesTheServerColorProtocol()
    {
        var shader = File.ReadAllText(ResolveRepositoryPath(
            "SWLOR_Haks",
            "sw_shader",
            "fsfbpostpr.shd"));

        shader.Should().Contain("const int COLOR_HOSTILE = 0;");
        shader.Should().Contain("const int COLOR_SELF = 1;");
        shader.Should().Contain("const int COLOR_BENEFICIAL = 2;");
        shader.Should().Contain("const int COLOR_PARTY_MEMBER = 3;");
        shader.Should().Contain("COLOR_HOSTILE) return vec3(1.0, 0.0, 0.0); // Red");
        shader.Should().Contain("COLOR_SELF) return vec3(0.0, 0.0, 1.0); // Blue");
        shader.Should().Contain("COLOR_BENEFICIAL) return vec3(0.0, 1.0, 0.0); // Green");
        shader.Should().Contain("COLOR_PARTY_MEMBER) return vec3(0.66, 0.66, 0.66); // Gray");
    }

    [Test]
    public void FillTiming_MatchesTheShaderProtocol()
    {
        var shader = File.ReadAllText(ResolveRepositoryPath(
            "SWLOR_Haks",
            "sw_shader",
            "fsfbpostpr.shd"));

        shader.Should().Contain($"const float FILL_CLOCK_PERIOD_MS = {GetTelegraphConstant("FillClockPeriodMilliseconds")}.0;");
        shader.Should().Contain($"const float FILL_DURATION_UNIT_MS = {GetTelegraphConstant("FillDurationUnitMilliseconds")}.0;");
        shader.Should().Contain("float fill = GetFillFraction(positionData.w);");
        shader.Should().Contain("scriptableFloat1 - startMs",
            "the server sends its fill clock in the first float uniform");
        shader.Should().NotContain("worldtimerTimeOfDay % ",
            "each client's world timer runs minutes away from the server's");
    }

    [TestCase(0, 0f)]
    [TestCase(1000, 0.25f)]
    [TestCase(2000, 0.5f)]
    [TestCase(4000, 1f)]
    [TestCase(9000, 1f)]
    public void FillTiming_FillsOverTheWindUp(int elapsedMilliseconds, float expectedFraction)
    {
        const long start = 5_000;
        var packed = InvokePackFillTiming(start, 4000);

        DecodeFillFraction(packed, InvokeFillClockValue(start + elapsedMilliseconds))
            .Should().BeApproximately(expectedFraction, 0.001f);
    }

    [Test]
    public void FillTiming_StaysContinuousWhenTheClockWraps()
    {
        var period = GetTelegraphConstant("FillClockPeriodMilliseconds");
        var start = 9L * period - 1000;
        var packed = InvokePackFillTiming(start, 4000);

        DecodeFillFraction(packed, InvokeFillClockValue(start + 1000)).Should().BeApproximately(0.25f, 0.001f);
        DecodeFillFraction(packed, InvokeFillClockValue(start + 3000)).Should().BeApproximately(0.75f, 0.001f);
    }

    [Test]
    public void FillTiming_ClockSentBeforeTheStartShowsAnEmptyShape()
    {
        const long start = 50_000;
        var packed = InvokePackFillTiming(start, 2000);

        DecodeFillFraction(packed, InvokeFillClockValue(start - 300)).Should().Be(0f);
    }

    [Test]
    public void FillTiming_LongWindUpsFillOverTheirFinalStretch()
    {
        const long start = 1_000;
        var maxFill = GetTelegraphConstant("MaxFillDurationUnits") * GetTelegraphConstant("FillDurationUnitMilliseconds");
        var packed = InvokePackFillTiming(start, maxFill + 10_000);

        DecodeFillFraction(packed, InvokeFillClockValue(start + 10_000)).Should().Be(0f);
        DecodeFillFraction(packed, InvokeFillClockValue(start + 10_000 + maxFill / 2)).Should().BeApproximately(0.5f, 0.001f);
        DecodeFillFraction(packed, InvokeFillClockValue(start + 10_000 + maxFill)).Should().Be(1f);
    }

    [Test]
    public void FillTiming_PacksToExactFloatIntegers()
    {
        var maxFill = GetTelegraphConstant("MaxFillDurationUnits") * GetTelegraphConstant("FillDurationUnitMilliseconds");
        var packed = InvokePackFillTiming(GetTelegraphConstant("FillClockPeriodMilliseconds") - 1, maxFill);

        packed.Should().BeLessThan(1 << 24, "floats hold integers exactly only below 2^24");
        packed.Should().Be(MathF.Floor(packed));
    }

    [Test]
    public void ShapesWithoutAWindUp_DoNotFill()
    {
        InvokePackFillTiming(12_345, 0).Should().Be(0f);
        DecodeFillFraction(0f, 500f).Should().Be(-1f);
    }

    [Test]
    public void PrecastTelegraphs_FillOverTheirWindUp()
    {
        InvokeFillDuration(new TelegraphData { Duration = 2.5f }).Should().Be(2500);
    }

    [Test]
    public void ImpactFlashesAndLingeringAreas_DoNotFill()
    {
        InvokeFillDuration(new TelegraphData { Duration = 0.5f, IsImpactFlash = true }).Should().Be(0);
        InvokeFillDuration(new TelegraphData { Duration = 30f, IsPersistentAreaIndicator = true }).Should().Be(0);
    }

    [Test]
    public void WindUpsTooLongForTheFillClock_DoNotFill()
    {
        // The fill of a very long wind-up would start more than half a clock period ahead, which
        // the shader cannot tell apart from a fill that already started.
        InvokeFillDuration(new TelegraphData { Duration = 90f }).Should().Be(90_000);
        InvokeFillDuration(new TelegraphData { Duration = 100f }).Should().Be(0);
    }

    [TestCase(true, true, true, TelegraphColorType.Self)]
    [TestCase(true, false, false, TelegraphColorType.Self)]
    [TestCase(false, true, true, TelegraphColorType.PartyMember)]
    [TestCase(false, true, false, TelegraphColorType.Beneficial)]
    [TestCase(false, false, true, TelegraphColorType.Hostile)]
    [TestCase(false, false, false, TelegraphColorType.Beneficial)]
    public void SelectTelegraphColorType_KeepsPartyBeneficialEffectsGreen(
        bool isOwnTelegraph,
        bool isPartyMemberTelegraph,
        bool isHostile,
        TelegraphColorType expected)
    {
        var method = typeof(TelegraphInstance)
            .GetMethod("SelectTelegraphColorType", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var actual = (TelegraphColorType)method.Invoke(
            NewInstance(),
            new object[] { isOwnTelegraph, isPartyMemberTelegraph, isHostile })!;

        actual.Should().Be(expected);
    }

    [Test]
    public void PartyMembershipChanges_RefreshAffectedTelegraphViewers()
    {
        var source = File.ReadAllText(ResolveRepositoryPath(
            "SWLOR.Game.Server",
            "Domain",
            "Social",
            "PartyService.cs"));
        var addPartyStart = source.IndexOf("private void AddToParty", StringComparison.Ordinal);
        var removePartyStart = source.IndexOf("private void RemoveCreatureFromParty", StringComparison.Ordinal);

        addPartyStart.Should().BeGreaterThan(-1);
        removePartyStart.Should().BeGreaterThan(-1);

        var addPartyBody = ExtractBlockBody(source, addPartyStart);
        var removePartyBody = ExtractBlockBody(source, removePartyStart);

        addPartyBody.Should().Contain(
            "_telegraph.UpdateShadersForPlayers(GetAllPartyMembers(requester));",
            "joining players and associates must see active party telegraphs change color immediately");
        removePartyBody.Should().Contain(
            "var affectedPartyMembers = _roster.Parties[partyId].ToList();",
            "the former party must be captured before its membership cache changes");
        removePartyBody.Should().Contain(
            "_telegraph.UpdateShadersForPlayers(affectedPartyMembers);",
            "departing and remaining players must immediately repack active telegraph colors");
    }

    [Test]
    public void PackTelegraphData_LineKeepsGameplayRotation()
    {
        var packed = InvokePackTelegraphData(TelegraphType.Line, 0f);

        ExtractPackedRotation(packed).Should().Be(0);
    }

    [Test]
    public void PackTelegraphData_ConeKeepsGameplayRotation()
    {
        var packed = InvokePackTelegraphData(TelegraphType.Cone, 0f);

        ExtractPackedRotation(packed).Should().Be(0);
    }

    [Test]
    public void ApplyTelegraphedCombatImpact_DefaultsToAVisibleImpactFlash()
    {
        // Instant-cast area abilities cannot gate damage behind a pre-cast telegraph without
        // contradicting the Bible, so the impact flash is what makes them visible at all. If this
        // default is ever zeroed, every instant area ability silently stops rendering.
        // Static callers reach the service through GameServices.Ability, typed as the interface, so the interface's
        // default is the one they get.
        var parameter = typeof(IAbilityService)
            .GetMethod(nameof(IAbilityService.ApplyTelegraphedCombatImpact), BindingFlags.Instance | BindingFlags.Public)!
            .GetParameters()
            .Single(x => x.Name == "impactFlashDuration");

        parameter.HasDefaultValue.Should().BeTrue();
        parameter.DefaultValue.Should().Be(AbilityService.DefaultImpactFlashDuration);
        AbilityService.DefaultImpactFlashDuration.Should().BeGreaterThan(0f);
    }

    /// <summary>
    /// Guards the production wiring from the immediate damage branch to geometry-aware flash suppression.
    /// </summary>
    [Test]
    public void ApplyTelegraphedCombatImpact_FlashesOnlyWhenActivationDidNotAlreadyShowTheShape()
    {
        // Guards the wiring itself: the zero-telegraph branch must still render something.
        var source = AbilitySource.Read();
        var branchStart = source.IndexOf("if (telegraphDuration <= 0f)", StringComparison.Ordinal);

        branchStart.Should().BeGreaterThan(
            -1,
            "the instant-cast branch must still be recognisable; update this test's anchor if it was reworded");

        // Scope the assertion to the branch body. Searching to end-of-file would also match the
        // ShowAreaImpactFlash method declaration further down, so deleting the call from the branch
        // would still pass.
        var branchBody = ExtractBlockBody(source, branchStart);

        branchBody.Should().Contain("trackedImpact?.ActivationAreaTelegraphs",
            "the impact must compare its current geometry with the shapes displayed during activation");
        branchBody.IndexOf("ShowAreaImpactFlash(", StringComparison.Ordinal)
            .Should().BeGreaterThan(-1, "the instant-cast path must flash the area it just struck");

        var flashStart = source.IndexOf("private void ShowAreaImpactFlash(", StringComparison.Ordinal);
        var flashBody = ExtractBlockBody(source, flashStart);
        flashBody.Should().Contain("_telegraph.ShouldShowImpactFlash(geometry, activationAreaTelegraphs)");
    }

    /// <summary>
    /// Guards snapshot capture and propagation, including impacts after a separate delay.
    /// </summary>
    [Test]
    public void AbilityActivation_CapturesDisplayedGeometryBeforeTheTelegraphExpires()
    {
        var source = File.ReadAllText(ResolveRepositoryPath(
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs"));

        source.Should().Contain("Telegraph.CaptureGeometry(activationTelegraphIds)");
        source.Should().Contain("activationAreaTelegraphs: activationAreaTelegraphs");
        var impactBody = ExtractBlockBody(source, source.IndexOf("void ResolveImpact()", StringComparison.Ordinal));
        impactBody.Should().Contain("activationAreaTelegraphs",
            "the warned footprint must survive until damage resolves");
        impactBody.Should().NotContain("ImpactDelay",
            "a separate delay must not discard the warned footprint");

        var abilitySource = AbilitySource.Read();
        abilitySource.Should().Contain("trackedImpact?.Ability.ImpactDelay > 0f ? null : trackedImpact?.ActivationAreaTelegraphs",
            "a separately delayed impact still needs a fresh flash at the original footprint");
    }

    [Test]
    public void PersistentAreaIndicators_DoNotDisplaceActivationWarnings()
    {
        var persistentIndicator = Candidate(new TelegraphData { IsPersistentAreaIndicator = true });
        var activationWarnings = Enumerable.Range(1, TelegraphService.MaxRenderCount)
            .Select(_ => Candidate(new TelegraphData()));

        var selected = SelectForRendering(new[] { persistentIndicator }.Concat(activationWarnings));

        selected.Should().HaveCount(TelegraphService.MaxRenderCount);
        selected.Should().OnlyContain(candidate => !candidate.Telegraph.Data.IsPersistentAreaIndicator);
    }

    [Test]
    public void EnemyThreats_ClaimRenderSlotsBeforeAlliedShapes()
    {
        var alliedWarnings = Enumerable.Range(1, TelegraphService.MaxRenderCount)
            .Select(_ => Candidate(new TelegraphData { IsHostile = true }, TelegraphColorType.PartyMember));
        var enemyWarning = Candidate(new TelegraphData { IsHostile = true }, TelegraphColorType.Hostile, creatorIsEnemy: true);
        var enemyZone = Candidate(
            new TelegraphData { IsHostile = true, IsPersistentAreaIndicator = true },
            TelegraphColorType.Hostile,
            creatorIsEnemy: true);

        var selected = SelectForRendering(alliedWarnings.Concat(new[] { enemyZone, enemyWarning }));

        selected.Should().HaveCount(TelegraphService.MaxRenderCount);
        selected[0].Should().Be(enemyWarning, "an enemy warning is the most urgent shape on screen");
        selected[1].Should().Be(enemyZone, "a lingering enemy area still outranks every allied shape");
    }

    [TestCase(TelegraphColorType.Self, false, true)]
    [TestCase(TelegraphColorType.Hostile, true, true)]
    [TestCase(TelegraphColorType.PartyMember, false, false)]
    [TestCase(TelegraphColorType.Beneficial, false, false)]
    [TestCase(TelegraphColorType.Hostile, false, false)]
    public void ImpactFlashes_RenderOnlyForTheirOwnerAndFromEnemies(
        TelegraphColorType colorType,
        bool creatorIsEnemy,
        bool expectedVisible)
    {
        var flash = Candidate(new TelegraphData { IsHostile = true, IsImpactFlash = true }, colorType, creatorIsEnemy);

        SelectForRendering(new[] { flash }).Should().HaveCount(expectedVisible ? 1 : 0);
    }

    [TestCase(TelegraphColorType.Self)]
    [TestCase(TelegraphColorType.Beneficial)]
    [TestCase(TelegraphColorType.PartyMember)]
    public void NonHostileColors_NeverReachTheReputationLookup(TelegraphColorType colorType)
    {
        var method = typeof(TelegraphInstance)
            .GetMethod("IsCreatorEnemyOfViewer", BindingFlags.Static | BindingFlags.NonPublic)!;

        // No engine is running here, so any lookup past the color check would throw.
        method.Invoke(null, new object[] { 1u, 2u, colorType }).Should().Be(false);
    }

    [Test]
    public void AlliedWarningsAndLingeringAreas_StayVisible()
    {
        var warning = Candidate(new TelegraphData { IsHostile = true }, TelegraphColorType.PartyMember);
        var koltoZone = Candidate(new TelegraphData { IsPersistentAreaIndicator = true }, TelegraphColorType.Beneficial);

        SelectForRendering(new[] { warning, koltoZone }).Should().Equal(warning, koltoZone);
    }

    [Test]
    public void AreaImpactFlash_IsMarkedSoAlliedViewersCanSkipIt()
    {
        var source = AbilitySource.Read();
        var flashStart = source.IndexOf("private void ShowAreaImpactFlash(", StringComparison.Ordinal);

        ExtractBlockBody(source, flashStart).Should().Contain("isImpactFlash: true");
    }

    private static TelegraphRenderCandidate Candidate(
        TelegraphData data,
        TelegraphColorType colorType = TelegraphColorType.Beneficial,
        bool creatorIsEnemy = false)
    {
        return new TelegraphRenderCandidate(new ActiveTelegraph(1, 0, 100, data), colorType, creatorIsEnemy);
    }

    private static TelegraphRenderCandidate[] SelectForRendering(IEnumerable<TelegraphRenderCandidate> candidates)
    {
        var selector = typeof(TelegraphInstance)
            .GetMethod("SelectTelegraphsForRendering", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (TelegraphRenderCandidate[])selector.Invoke(null, new object[] { candidates })!;
    }

    /// <summary>
    /// Returns the brace-delimited block that opens after <paramref name="searchFrom"/>, excluding the
    /// braces themselves.
    /// </summary>
    private static string ExtractBlockBody(string source, int searchFrom)
    {
        var open = source.IndexOf('{', searchFrom);
        open.Should().BeGreaterThan(-1, "the branch must open a block");

        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[(open + 1)..i];
            }
        }

        throw new AssertionException("Could not find the closing brace for the instant-cast branch.");
    }

    [Test]
    public void GeneratedWeaponAbilities_DoNotStackAPrecastTelegraphOnTopOfTheActivationDelay()
    {
        // UsePerkFeat already draws a pre-cast telegraph from the activation delay. TelegraphDuration
        // runs at impact, after the cast resolves, so deriving it from casting time would delay damage
        // twice and draw the shape twice. The generator must not emit it from CastingTime.
        var generator = File.ReadAllText(ResolveRepositoryPath("tools", "GenerateWeaponArchetypeImplementation.py"));
        var emitsTelegraphDuration = Regex.IsMatch(
            generator,
            @"add_profile_property\(\s*""TelegraphDuration""");

        emitsTelegraphDuration.Should().BeFalse(
            "TelegraphDuration is an impact-time delay and must stay hand-set, not derived from CastingTime");
    }

    private static string ResolveRepositoryPath(params string[] segments)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root must be locatable from the test directory");

        return Path.Combine(new[] { directory!.FullName }.Concat(segments).ToArray());
    }

    /// <summary>
    /// The shader's GetFillFraction, in single precision as the GPU computes it.
    /// </summary>
    private static float DecodeFillFraction(float packedFill, float clock)
    {
        float period = GetTelegraphConstant("FillClockPeriodMilliseconds");
        float unit = GetTelegraphConstant("FillDurationUnitMilliseconds");
        var units = MathF.Floor(packedFill / period);
        if (units < 1f)
            return -1f;

        var start = packedFill - units * period;
        var x = clock - start + period;
        var elapsed = x - period * MathF.Floor(x / period);
        if (elapsed > period * 0.5f)
            return 0f;

        return Math.Clamp(elapsed / (units * unit), 0f, 1f);
    }

    private static int GetTelegraphConstant(string name)
    {
        return (int)typeof(TelegraphInstance)
            .GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!
            .GetRawConstantValue()!;
    }

    private static float InvokePackFillTiming(long startMilliseconds, int durationMilliseconds)
    {
        var method = typeof(TelegraphInstance)
            .GetMethod("PackFillTiming", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (float)method.Invoke(null, new object[] { startMilliseconds, durationMilliseconds })!;
    }

    private static float InvokeFillClockValue(long nowMilliseconds)
    {
        var method = typeof(TelegraphInstance)
            .GetMethod("GetFillClockUniformValue", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (float)method.Invoke(null, new object[] { nowMilliseconds })!;
    }

    private static int InvokeFillDuration(TelegraphData data)
    {
        var method = typeof(TelegraphInstance)
            .GetMethod("GetFillDurationMilliseconds", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (int)method.Invoke(null, new object[] { data })!;
    }

    private static int InvokePackTelegraphData(TelegraphType type, float rotation)
    {
        return InvokePackTelegraphData(type, TelegraphColorType.Self, rotation);
    }

    private static int InvokePackTelegraphData(
        TelegraphType type,
        TelegraphColorType colorType,
        float rotation)
    {
        var method = typeof(TelegraphInstance)
            .GetMethod("PackTelegraphData", BindingFlags.Instance | BindingFlags.NonPublic)!;

        return (int)method.Invoke(
            NewInstance(),
            new object[]
            {
                type,
                colorType,
                new Vector2(8f, 2.5f),
                rotation
            })!;
    }

    // The pure helpers under test use neither the DM presence nor the party service.
    private static TelegraphInstance NewInstance() => new(null!, null!);

    private static int ExtractPackedColor(int packed)
    {
        return (packed >> PackedColorShift) & PackedColorMask;
    }

    private static int ExtractPackedRotation(int packed)
    {
        return (int)(((uint)packed >> PackedRotationShift) & PackedRotationMask);
    }
}

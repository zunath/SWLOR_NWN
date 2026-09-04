using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.TelegraphService;

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
        var method = typeof(Telegraph)
            .GetMethod("SelectTelegraphColorType", BindingFlags.Static | BindingFlags.NonPublic)!;

        var actual = (TelegraphColorType)method.Invoke(
            null,
            new object[] { isOwnTelegraph, isPartyMemberTelegraph, isHostile })!;

        actual.Should().Be(expected);
    }

    [Test]
    public void PartyMembershipChanges_RefreshAffectedTelegraphViewers()
    {
        var source = File.ReadAllText(ResolveRepositoryPath(
            "SWLOR.Game.Server",
            "Service",
            "Party.cs"));
        var addPartyStart = source.IndexOf("private static void AddToParty", StringComparison.Ordinal);
        var removePartyStart = source.IndexOf("private static void RemoveCreatureFromParty", StringComparison.Ordinal);

        addPartyStart.Should().BeGreaterThan(-1);
        removePartyStart.Should().BeGreaterThan(-1);

        var addPartyBody = ExtractBlockBody(source, addPartyStart);
        var removePartyBody = ExtractBlockBody(source, removePartyStart);

        addPartyBody.Should().Contain(
            "Telegraph.UpdateShadersForPlayers(GetAllPartyMembers(requester));",
            "joining players and associates must see active party telegraphs change color immediately");
        removePartyBody.Should().Contain(
            "var affectedPartyMembers = _parties[partyId].ToList();",
            "the former party must be captured before its membership cache changes");
        removePartyBody.Should().Contain(
            "Telegraph.UpdateShadersForPlayers(affectedPartyMembers);",
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
        var parameter = typeof(Ability)
            .GetMethod(nameof(Ability.ApplyTelegraphedCombatImpact), BindingFlags.Static | BindingFlags.Public)!
            .GetParameters()
            .Single(x => x.Name == "impactFlashDuration");

        parameter.HasDefaultValue.Should().BeTrue();
        parameter.DefaultValue.Should().Be(Ability.DefaultImpactFlashDuration);
        Ability.DefaultImpactFlashDuration.Should().BeGreaterThan(0f);
    }

    [Test]
    public void ApplyTelegraphedCombatImpact_FlashesOnlyWhenActivationDidNotAlreadyShowTheShape()
    {
        // Guards the wiring itself: the zero-telegraph branch must still render something.
        var source = File.ReadAllText(ResolveRepositoryPath("SWLOR.Game.Server", "Service", "Ability.cs"));
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

        var flashStart = source.IndexOf("private static void ShowAreaImpactFlash(", StringComparison.Ordinal);
        var flashBody = ExtractBlockBody(source, flashStart);
        flashBody.Should().Contain("Telegraph.ShouldShowImpactFlash(geometry, activationAreaTelegraphs)");
    }

    [Test]
    public void AbilityActivation_CapturesDisplayedGeometryBeforeTheTelegraphExpires()
    {
        var source = File.ReadAllText(ResolveRepositoryPath(
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs"));

        source.Should().Contain("Telegraph.CaptureGeometry(activationTelegraphIds)");
        source.Should().Contain("activationAreaTelegraphs: activationAreaTelegraphs");
        source.Should().Contain(
            "ability.ImpactDelay <= 0f ? activationAreaTelegraphs : null",
            "a delayed impact occurs after its activation telegraph has been removed and still needs an impact flash");
    }

    [Test]
    public void PersistentAreaIndicators_DoNotDisplaceActivationWarnings()
    {
        var persistentIndicator = new ActiveTelegraph(
            1,
            0,
            100,
            new TelegraphData { IsPersistentAreaIndicator = true });
        var activationWarnings = Enumerable.Range(1, Telegraph.MaxRenderCount)
            .Select(start => new ActiveTelegraph(
                1,
                start,
                100,
                new TelegraphData { IsPersistentAreaIndicator = false }))
            .ToArray();
        var candidates = new[] { persistentIndicator }
            .Concat(activationWarnings)
            .ToArray();
        var selector = typeof(Telegraph)
            .GetMethod("SelectTelegraphsForRendering", BindingFlags.Static | BindingFlags.NonPublic)!;

        var selected = (ActiveTelegraph[])selector.Invoke(null, new object[] { candidates })!;

        selected.Should().HaveCount(Telegraph.MaxRenderCount);
        selected.Should().OnlyContain(telegraph => !telegraph.Data.IsPersistentAreaIndicator);
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

    private static int InvokePackTelegraphData(TelegraphType type, float rotation)
    {
        return InvokePackTelegraphData(type, TelegraphColorType.Self, rotation);
    }

    private static int InvokePackTelegraphData(
        TelegraphType type,
        TelegraphColorType colorType,
        float rotation)
    {
        var method = typeof(Telegraph)
            .GetMethod("PackTelegraphData", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (int)method.Invoke(
            null,
            new object[]
            {
                type,
                colorType,
                new Vector2(8f, 2.5f),
                rotation
            })!;
    }

    private static int ExtractPackedColor(int packed)
    {
        return (packed >> PackedColorShift) & PackedColorMask;
    }

    private static int ExtractPackedRotation(int packed)
    {
        return (int)(((uint)packed >> PackedRotationShift) & PackedRotationMask);
    }
}

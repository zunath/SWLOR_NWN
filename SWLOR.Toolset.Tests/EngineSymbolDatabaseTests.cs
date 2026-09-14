using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script.Symbols;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The engine header is the completion database. These tests pin the counts and the two things
    /// that make parameter-aware completion possible: per-parameter documentation, and the
    /// <c>FOO_*</c> constant family named inside it.
    /// </summary>
    public class EngineSymbolDatabaseTests
    {
        private static string HeaderPath => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWN", "nwscript-8193.37.nss");

        private static string ApiDirectory => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWScript");

        private static EngineSymbolDatabase Db => _db ??= EngineSymbolDatabase.Load(HeaderPath, ApiDirectory);
        private static EngineSymbolDatabase? _db;

        [OneTimeSetUp]
        public void RequireHeader()
        {
            if (!File.Exists(HeaderPath))
                Assert.Ignore("engine header not present");
        }

        [Test]
        public void ParsesTheExpectedNumberOfFunctionsAndConstants()
        {
            // Counted from the header itself. Pinned so a parser regression that silently drops a
            // category fails loudly.
            //
            // 1,187 not 1,164: the figure in the design doc came from a grep whose name pattern was
            // [A-Za-z_]+, which excludes digits and so missed d2/d3/d6/d20/d100 and the rest of the
            // dice helpers. The parser is right and the original count was an undercount.
            Db.Functions.Should().HaveCount(1187);
            Db.Constants.Should().HaveCount(6201);
        }

        [Test]
        public void Random_HasItsSignatureAndSummary()
        {
            var fn = Db.FindFunction("Random");

            fn.Should().NotBeNull();
            fn!.ReturnType.Should().Be("int");
            fn.Parameters.Should().ContainSingle();
            fn.Parameters[0].Name.Should().Be("nMaxInteger");
            fn.Parameters[0].Type.Should().Be("int");
            fn.Summary.Should().Contain("integer between 0 and nMaxInteger-1");
        }

        [Test]
        public void GetNearestCreature_HasEightParametersSixOfThemOptional()
        {
            var fn = Db.FindFunction("GetNearestCreature");

            fn.Should().NotBeNull();
            fn!.Parameters.Should().HaveCount(8);
            fn.Parameters.Count(p => p.IsOptional).Should().Be(6);
            fn.Parameters[2].Name.Should().Be("oTarget");
            fn.Parameters[2].DefaultValue.Should().Be("OBJECT_SELF");
            fn.ReturnsOnError.Should().Contain("OBJECT_INVALID");
        }

        /// <summary>
        /// The feature Aurora never had: argument 1 of GetNearestCreature accepts CREATURE_TYPE_*,
        /// and the header says so in its own doc comment.
        /// </summary>
        [Test]
        public void ParameterDocumentation_YieldsItsConstantFamily()
        {
            var fn = Db.FindFunction("GetNearestCreature")!;

            fn.Parameters[0].ConstantFamily.Should().Be("CREATURE_TYPE_*");
            Db.ConstantsInFamily("CREATURE_TYPE_*")
                .Should().NotBeEmpty()
                .And.Contain(c => c.Name == "CREATURE_TYPE_PLAYER_CHAR");
        }

        [Test]
        public void ConstantsReferenceFamilies_UseDocumentedFamiliesBeforeStructuralPrefixes()
        {
            Db.ConstantFamilyOf(Db.FindConstant("ABILITY_CHARISMA")!).Should().Be("ABILITY_*");
            Db.ConstantFamilyOf(Db.FindConstant("APPEARANCE_TYPE_DWARF")!).Should().Be("APPEARANCE_TYPE_*");
            Db.ConstantFamilyOf(Db.FindConstant("IP_CONST_CASTSPELL_FIREBALL_5")!).Should().Be("IP_CONST_CASTSPELL_*");

            Db.Constants
                .Where(c => Db.ConstantFamilyOf(c) == "ABILITY_*")
                .Select(c => c.Name)
                .Should().BeEquivalentTo(new[]
                {
                    "ABILITY_STRENGTH",
                    "ABILITY_DEXTERITY",
                    "ABILITY_CONSTITUTION",
                    "ABILITY_INTELLIGENCE",
                    "ABILITY_WISDOM",
                    "ABILITY_CHARISMA"
                });

            Db.ConstantFamilyOf(Db.FindConstant("IP_CONST_ABILITY_STR")!).Should().Be("IP_CONST_ABILITY_*");
            Db.ConstantFamilyOf(Db.FindConstant("DISEASE_BLINDING_SICKNESS")!).Should().Be("DISEASE_*");

            Db.Constants
                .Where(c => c.Name.StartsWith("APPEARANCE_TYPE_", StringComparison.Ordinal))
                .Should().OnlyContain(c => Db.ConstantFamilyOf(c) == "APPEARANCE_TYPE_*");
        }

        [Test]
        public void ConstantsReferenceFamilies_FallBackToReadablePrefixesWhenTheHeaderHasNoFamily()
        {
            Db.ConstantFamilyOf(Db.FindConstant("AMBIENT_SOUND_CITY_SLUMS_DAY_CROWDED")!).Should().Be("AMBIENT_SOUND_*");
            Db.ConstantFamilyOf(Db.FindConstant("AMBIENT_SOUND_CRYPT_SMALL")!).Should().Be("AMBIENT_SOUND_*");
            Db.ConstantFamilyOf(Db.FindConstant("SPELLABILITY_AURA_BLINDING")!).Should().Be("SPELLABILITY_AURA_*");
            Db.ConstantFamilyOf(Db.FindConstant("SPELLABILITY_DRAGON_BREATH_ACID")!).Should().Be("SPELLABILITY_DRAGON_*");

            Db.Constants
                .Where(c => c.Name.StartsWith("AMBIENT_SOUND_", StringComparison.Ordinal))
                .Should().OnlyContain(c => Db.ConstantFamilyOf(c) == "AMBIENT_SOUND_*");
        }

        [Test]
        public void ConstantsReferenceFamilies_DoNotInventSingletonWildcardGroups()
        {
            var singletonWildcards = Db.Constants
                .GroupBy(Db.ConstantFamilyOf, StringComparer.Ordinal)
                .Where(g => g.Key.EndsWith("*", StringComparison.Ordinal) && g.Count() == 1)
                .Select(g => $"{g.Key}: {g.Single().Name}")
                .ToList();

            singletonWildcards.Should().BeEmpty();
        }

        [Test]
        public void ConstantsReferenceFamilies_UndocumentedFallbacksStayBroad()
        {
            var documented = NwScriptHeaderParser.ParseFile(HeaderPath)
                .ConstantFamilies
                .ToHashSet(StringComparer.Ordinal);

            var deepUndocumentedFamilies = Db.Constants
                .Select(Db.ConstantFamilyOf)
                .Distinct(StringComparer.Ordinal)
                .Where(f => f.EndsWith("*", StringComparison.Ordinal))
                .Where(f => !documented.Contains(f))
                .Where(f => f[..^1].TrimEnd('_').Split('_').Length > 2)
                .ToList();

            deepUndocumentedFamilies.Should().BeEmpty();
        }

        [Test]
        public void AtLeast150ParametersCarryAConstantFamily()
        {
            var withFamily = Db.Functions.SelectMany(f => f.Parameters).Count(p => p.ConstantFamily != null);

            // Measured at 150 in the shipped header; this is the fuel for context-aware completion,
            // so a parser change that halves it should fail here rather than quietly degrade ranking.
            withFamily.Should().BeGreaterThanOrEqualTo(150);
        }

        [Test]
        public void KnownConstants_ResolveWithTheirValues()
        {
            Db.FindConstant("CREATURE_TYPE_PLAYER_CHAR")!.Value.Should().Be("1");
            Db.FindConstant("OBJECT_TYPE_CREATURE").Should().NotBeNull();
            Db.FindConstant("TRUE")!.Type.Should().Be("int");
        }

        [Test]
        public void FunctionsAreCategorised_FromTheNwnApiFolder()
        {
            if (!Directory.Exists(ApiDirectory))
                Assert.Ignore("SWLOR.NWN.API/NWScript not present");

            var categorised = Db.Functions.Count(f => f.Category != "Uncategorized");

            categorised.Should().BeGreaterThan(800,
                "most engine functions have a C# wrapper whose filename names their category");
            Db.CategoryCounts().Should().Contain(c => c.Category.Contains("Creature"));
        }

        [Test]
        public void FunctionCategories_HaveNoUncategorizedBucket()
        {
            if (!Directory.Exists(ApiDirectory))
                Assert.Ignore("SWLOR.NWN.API/NWScript not present");

            Db.Functions.Should().OnlyContain(f => f.Category != "Uncategorized");
            Db.CategoryCounts().Should().NotContain(c => c.Category == "Uncategorized");
        }

        [Test]
        public void FunctionCategories_UseReadableCategoryNames()
        {
            Db.FindFunction("Get2DAString")!.Category.Should().Be("2DA");
            Db.CategoryCounts().Should().Contain(c => c.Category == "2DA");
            Db.CategoryCounts().Should().NotContain(c => c.Category == "Data2 DA");
        }

        [Test]
        public void FunctionCategories_CoverHeaderFunctionsWithoutWrappers()
        {
            Db.FindFunction("CassowaryConstrain")!.Category.Should().Be("Cassowary");
            Db.FindFunction("NWNXCall")!.Category.Should().Be("NWNX");
            Db.FindFunction("JsonArraySetInplace")!.Category.Should().Be("Json");
            Db.FindFunction("ExecuteScript")!.Category.Should().Be("Scripting");
            Db.FindFunction("SetAreaNoRestFlag")!.Category.Should().Be("Area");
            Db.FindFunction("StartAudioStream")!.Category.Should().Be("Audio");
            Db.FindFunction("GetSpellAbilityReady")!.Category.Should().Be("Spell");
            Db.FindFunction("EffectAttackIncrease")!.Category.Should().Be("Effect");
        }

        [Test]
        public void CallSkeleton_OmitsOptionalParameters()
        {
            var fn = Db.FindFunction("GetNearestCreature")!;

            // Insert-at-cursor writes a usable call, not a bare name and not all eight arguments.
            fn.CallSkeleton.Should().Be("GetNearestCreature(nFirstCriteriaType, nFirstCriteriaValue)");
        }

        [Test]
        public void Signature_RendersDefaultsInline()
        {
            Db.FindFunction("Random")!.Signature.Should().Be("int Random(int nMaxInteger)");
            Db.FindFunction("GetNearestCreature")!.Signature.Should().Contain("object oTarget=OBJECT_SELF");
        }

        [Test]
        public void NoFunctionIsAlsoAConstant()
        {
            // The two patterns must stay disjoint; overlap would mean one of them is over-matching.
            var names = Db.Functions.Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
            Db.Constants.Where(c => names.Contains(c.Name)).Should().BeEmpty();
        }
    }
}

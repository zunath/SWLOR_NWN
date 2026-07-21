using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for the WP4.3 <see cref="BlueprintModelResolver"/>: appearance-driven preview-model
    /// resolution for creatures (simple and segmented), placeables, and doors, run against real corpus
    /// blueprints plus the real appearance/placeables/doortypes 2DAs so a wrong column or field name
    /// surfaces as a failure. This is the machine-verifiable core of WP4.3; the composed segmented
    /// render itself is confirmed by the human visual gate.
    /// </summary>
    public class BlueprintModelResolverTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var hakBuilderConfig = Path.Combine(current.FullName, "Build", "hakbuilder.json");
                    var haksDirectory = Path.Combine(current.FullName, "SWLOR_Haks");
                    if (File.Exists(hakBuilderConfig) && Directory.Exists(haksDirectory))
                        return current.FullName;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        private static string Sw2DaDirectory => Path.Combine(RepoRoot, "SWLOR_Haks", "sw_2da");
        private static string SwTlkJsonPath => Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json");

        private static AppearanceService Appearances()
        {
            var twoDa = new TwoDaService(Sw2DaDirectory);
            var tlk = TlkService.Load(SwTlkJsonPath);
            return new AppearanceService(twoDa, tlk);
        }

        private static PlaceableAppearanceService Placeables()
        {
            var twoDa = new TwoDaService(Sw2DaDirectory);
            var tlk = TlkService.Load(SwTlkJsonPath);
            return new PlaceableAppearanceService(twoDa, tlk);
        }

        private static DoorTypeService Doors()
        {
            var twoDa = new TwoDaService(Sw2DaDirectory);
            var tlk = TlkService.Load(SwTlkJsonPath);
            return new DoorTypeService(twoDa, tlk);
        }

        private static Domain.Gff.JsonGffStruct BlueprintRoot(ResourceType type, string resRef)
        {
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            return workspace.LoadBlueprint(type, resRef).Document.Root;
        }

        [Test]
        public void Resolve_SimpleCreature_YieldsSingleModelResRef()
        {
            // ashwing.utc: Appearance_Type 2137 -> appearance.2da MODELTYPE S, RACE c_anurog.
            var root = BlueprintRoot(ResourceType.Utc, "ashwing");

            var result = BlueprintModelResolver.Resolve(ResourceType.Utc, root, Appearances(), null, null);

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("c_anurog");
            result.Parts.Should().BeEmpty();
        }

        [Test]
        public void Resolve_SegmentedCreature_YieldsSkeletonAndBodyParts()
        {
            // agr_guildmaster.utc: Appearance_Type 10096 -> MODELTYPE P, RACE H; Gender 0 (male),
            // Phenotype 0 -> skeleton prefix "pmh0". Part numbers read from the utc's BodyPart_*
            // fields; 0-valued/absent parts are omitted.
            var root = BlueprintRoot(ResourceType.Utc, "agr_guildmaster");

            var result = BlueprintModelResolver.Resolve(ResourceType.Utc, root, Appearances(), null, null);

            result.Kind.Should().Be(BlueprintModelKind.Segmented);
            result.SkeletonResRef.Should().Be("pmh0");

            var parts = result.Parts.ToDictionary(p => p.PartType, p => p.ModelResRef);
            parts.Should().HaveCount(15, "belt(0), both shoulders(0), and the absent right foot are omitted");
            parts["head"].Should().Be("pmh0_head220");
            parts["neck"].Should().Be("pmh0_neck001");
            parts["chest"].Should().Be("pmh0_chest001", "BodyPart_Torso maps to the 'chest' bone part");
            parts["pelvis"].Should().Be("pmh0_pelvis001");
            parts["bicepl"].Should().Be("pmh0_bicepl249");
            parts["bicepr"].Should().Be("pmh0_bicepr246");
            parts["forel"].Should().Be("pmh0_forel248", "BodyPart_LFArm maps to the 'forel' forearm part");
            parts["handr"].Should().Be("pmh0_handr246");
            parts["footl"].Should().Be("pmh0_footl001");
            parts.Should().NotContainKey("belt");
            parts.Should().NotContainKey("footr");
        }

        [Test]
        public void Resolve_Placeable_YieldsModelNameFromPlaceables2da()
        {
            // _mdrn_chair.utp: Appearance 179 -> placeables.2da Label "Chair 01", ModelName PLC_X02.
            var root = BlueprintRoot(ResourceType.Utp, "_mdrn_chair");

            var result = BlueprintModelResolver.Resolve(ResourceType.Utp, root, null, Placeables(), null);

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("PLC_X02");
            result.Status.Should().Contain("Chair 01");
        }

        [Test]
        public void Resolve_Door_YieldsModelFromDoortypes2daGenericTypeNew()
        {
            // _mdrn_dt_bars.utd: GenericType_New 47 -> doortypes.2da Model TCN_UDoor_10.
            var root = BlueprintRoot(ResourceType.Utd, "_mdrn_dt_bars");

            var result = BlueprintModelResolver.Resolve(ResourceType.Utd, root, null, null, Doors());

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("TCN_UDoor_10");
        }

        [Test]
        public void Resolve_MissingService_DegradesToNoneWithoutThrowing()
        {
            var root = BlueprintRoot(ResourceType.Utc, "ashwing");

            var result = BlueprintModelResolver.Resolve(ResourceType.Utc, root, null, null, null);

            result.Kind.Should().Be(BlueprintModelKind.None);
            result.ModelResRef.Should().BeNull();
        }

        [Test]
        public void Resolve_NonPreviewableType_YieldsNone()
        {
            var root = BlueprintRoot(ResourceType.Uti, "001"); // any struct; type gates it out

            var result = BlueprintModelResolver.Resolve(ResourceType.Uti, root, Appearances(), Placeables(), Doors());

            result.Kind.Should().Be(BlueprintModelKind.None);
        }
    }
}

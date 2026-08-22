using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.NWN.Formats.Plt;

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

        private static BaseItemIconService BaseItems() =>
            new(new TwoDaService(Sw2DaDirectory));

        private static CloakModelService CloakModels() =>
            new(new TwoDaService(Sw2DaDirectory));

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
            // fields; 0-valued/absent parts are omitted. No item loader -> naked body (no armor
            // overrides). Right foot comes from the Aurora-quirk field ArmorPart_RFoot (=1 here);
            // BodyPart_RFoot does not exist anywhere in the corpus.
            var root = BlueprintRoot(ResourceType.Utc, "agr_guildmaster");

            var result = BlueprintModelResolver.Resolve(ResourceType.Utc, root, Appearances(), null, null);

            result.Kind.Should().Be(BlueprintModelKind.Segmented);
            result.SkeletonResRef.Should().Be("pmh0");

            var parts = result.Parts.ToDictionary(p => p.PartType, p => p.ModelResRef);
            parts.Should().HaveCount(16, "belt(0) and both shoulders(0) are omitted; both feet resolve");
            parts["head"].Should().Be("pmh0_head220");
            parts["neck"].Should().Be("pmh0_neck001");
            parts["chest"].Should().Be("pmh0_chest001", "BodyPart_Torso maps to the 'chest' bone part");
            parts["pelvis"].Should().Be("pmh0_pelvis001");
            parts["bicepl"].Should().Be("pmh0_bicepl249");
            parts["bicepr"].Should().Be("pmh0_bicepr246");
            parts["forel"].Should().Be("pmh0_forel248", "BodyPart_LFArm maps to the 'forel' forearm part");
            parts["handr"].Should().Be("pmh0_handr246");
            parts["footl"].Should().Be("pmh0_footl001");
            parts["footr"].Should().Be("pmh0_footr001", "right foot is read from ArmorPart_RFoot on the utc root");
            parts.Should().NotContainKey("belt");
        }

        [Test]
        public void Resolve_SegmentedCreatureWithEquippedArmor_AppliesArmorPartOverrides()
        {
            // agr_guildmaster has 'noble_gr' equipped in the chest slot (Equip_ItemList struct id 2).
            // noble_gr.uti's ArmorPart_* values must override the creature's naked body parts
            // (Torso 27, Pelvis 50, thighs 87, shins 85, feet 52, hands 3, forearms 4, biceps 4,
            // Neck 4) — except parts the creature sets to 0 (belt 0, shoulders 0 stay invisible,
            // Quartermaster precedence) and the head, which armor never overrides.
            var root = BlueprintRoot(ResourceType.Utc, "agr_guildmaster");
            JsonGffStruct? LoadItem(string resRef) => BlueprintRoot(ResourceType.Uti, resRef);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Utc, root, Appearances(), null, null, LoadItem, _ => true);

            result.Kind.Should().Be(BlueprintModelKind.Segmented);
            var parts = result.Parts.ToDictionary(p => p.PartType, p => p.ModelResRef);
            parts.Should().HaveCount(16, "noble_gr has no robe; belt and shoulders stay 0");
            parts["head"].Should().Be("pmh0_head220", "armor never overrides the head");
            parts["chest"].Should().Be("pmh0_chest027");
            parts["pelvis"].Should().Be("pmh0_pelvis050");
            parts["neck"].Should().Be("pmh0_neck004");
            parts["bicepl"].Should().Be("pmh0_bicepl004", "armor part 4 overrides the creature's 249");
            parts["forer"].Should().Be("pmh0_forer004");
            parts["handl"].Should().Be("pmh0_handl003");
            parts["legl"].Should().Be("pmh0_legl087");
            parts["shinr"].Should().Be("pmh0_shinr085");
            parts["footl"].Should().Be("pmh0_footl052");
            parts["footr"].Should().Be("pmh0_footr052");
            parts.Should().NotContainKey("belt", "creature belt 0 wins over armor belt 25");
            parts.Should().NotContainKey("shol");

            result.LayerColorIndices[PltLayers.Skin]
                .Should().Be((int)root.Get("Color_Skin").GetInteger());
            result.LayerColorIndices[PltLayers.Hair]
                .Should().Be((int)root.Get("Color_Hair").GetInteger());

            var armor = BlueprintRoot(ResourceType.Uti, "noble_gr");
            result.LayerColorIndices[PltLayers.Metal1]
                .Should().Be((int)armor.Get("Metal1Color").GetInteger());
            result.LayerColorIndices[PltLayers.Cloth2]
                .Should().Be((int)armor.Get("Cloth2Color").GetInteger());
            result.LayerColorIndices[PltLayers.Leather1]
                .Should().Be((int)armor.Get("Leather1Color").GetInteger());
        }

        [Test]
        public void Resolve_PlacedCreature_UsesItsEmbeddedArmorPartsAndDyes()
        {
            // Placed GIT creatures contain the complete equipped UTI struct rather than the
            // EquippedRes-only entry stored in a UTC. Bruenor's embedded Czerka uniform is torso
            // 189 / Cloth1 107; ignoring that struct produced the naked part-1 body in the area view.
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            var (_, git, _) = workspace.LoadArea("czs220_hangar");
            var root = git.Creatures.First();

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Utc,
                root,
                Appearances(),
                null,
                null,
                _ => throw new InvalidOperationException("embedded armor must not reload its source UTI"),
                _ => true,
                baseItems: BaseItems().GetOrNull);

            result.Parts.Should().Contain(
                part => part.PartType == "chest" && part.ModelResRef == "pmh0_chest189");
            result.LayerColorIndices[PltLayers.Cloth1].Should().Be(107);
            BlueprintModelResolver.GetEquippedChestArmorResRef(root).Should().Be("czerkauniform");
        }

        [Test]
        public void Resolve_PlacedCreature_AttachesEveryCompositeRightHandWeaponPart()
        {
            // Find the first Anchorhead creature carrying an item in slot 16. Its embedded
            // three-part rifle retains shared item-space coordinates and every part attaches to the
            // right-hand skeleton hook.
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            var (_, git, _) = workspace.LoadArea("anchor_entreenor");
            var root = git.Creatures.First(creature => creature.GetListOrEmpty("Equip_ItemList")
                .Any(item => System.Text.Encoding.ASCII.GetString(item.RawStructId ?? []) == "16"));

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Utc,
                root,
                Appearances(),
                null,
                null,
                _ => throw new InvalidOperationException("embedded weapon must not reload its source UTI"),
                _ => true,
                baseItems: BaseItems().GetOrNull);

            var weaponParts = result.Parts.Where(part => part.PartType == "weaponr").ToList();
            weaponParts.Should().HaveCount(3);
            weaponParts.Select(part => part.ModelResRef).Should()
                .Contain(model => model.EndsWith("_b_011", StringComparison.OrdinalIgnoreCase))
                .And.Contain(model => model.EndsWith("_m_121", StringComparison.OrdinalIgnoreCase))
                .And.Contain(model => model.EndsWith("_t_011", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void Resolve_EquippedCloak_UsesTheWearersBodyPrefixAndItsOwnDyes()
        {
            // Darth Gravius is a female dynamic elf (pfe0). The cloak item preview itself uses a
            // generic pmh0 mannequin, but the equipped garment must use the wearer's skeleton and
            // retain Cloth1 45 instead of inheriting the robe's Cloth1 97.
            var root = BlueprintRoot(ResourceType.Utc, "darthgravius");
            root.SetInt("BodyPart_LShoul", GffFieldType.Byte, 1);
            root.SetInt("BodyPart_RShoul", GffFieldType.Byte, 1);
            JsonGffStruct? LoadItem(string resRef)
            {
                var item = BlueprintRoot(ResourceType.Uti, resRef);
                if (resRef == "jeweled_cloak")
                {
                    // The corpus item moved to the basic cloak (row 20: MODEL 20, TEXTURE 20,
                    // shoulders kept) when its old appearance's models were KILLed. Pin the
                    // divergent row 10 (MODEL 7, TEXTURE 14, hides both shoulders) so the split
                    // geometry/texture mapping and the shoulder-hiding flags stay covered. Both
                    // fields must move together: ItemAppearanceValues.Read prefers the larger of
                    // the byte and its xModelPart1 word companion.
                    item.SetInt("ModelPart1", GffFieldType.Byte, 10);
                    item.SetInt("xModelPart1", GffFieldType.Word, 10);
                }

                return item;
            }

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Utc, root, Appearances(), null, null,
                LoadItem, _ => true, baseItems: BaseItems().GetOrNull,
                cloakModels: CloakModels());

            result.SkeletonResRef.Should().Be("pfe0");
            var cloak = result.Parts.Single(part => part.PartType == "cloak");
            cloak.ModelResRef.Should().Be("pfe0_cloak_007",
                "cloak appearance 10 maps to geometry 7 through cloakmodel.2da");
            cloak.TextureResRef.Should().Be("pfe0_cloak_014",
                "cloak appearance 10 selects texture 14 independently of its shared geometry");
            cloak.LayerColorIndices.Should().NotBeNull();
            cloak.LayerColorIndices![PltLayers.Cloth1].Should().Be(45);
            result.LayerColorIndices[PltLayers.Cloth1].Should().Be(97,
                "the chest robe and cloak intentionally use independent palettes");
            result.Parts.Should().NotContain(
                part => part.PartType == "shol" || part.PartType == "shor",
                "cloak appearance 10 hides both wearer shoulder parts in cloakmodel.2da");
        }

        [Test]
        public void Resolve_EquippedHelmet_UsesTheItemsOwnDyes()
        {
            var root = BlueprintRoot(ResourceType.Utc, "sith_commando");

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Utc, root, Appearances(), null, null,
                resRef => BlueprintRoot(ResourceType.Uti, resRef),
                _ => true, baseItems: BaseItems().GetOrNull,
                cloakModels: CloakModels());

            var helmet = result.Parts.Single(part => part.PartType == "helmet");
            helmet.ModelResRef.Should().Be("helm_120");
            helmet.LayerColorIndices.Should().NotBeNull();
            helmet.LayerColorIndices![PltLayers.Metal1].Should().Be(17);
            helmet.LayerColorIndices[PltLayers.Metal2].Should().Be(0);
            helmet.LayerColorIndices[PltLayers.Cloth1].Should().Be(63);
        }

        [Test]
        public void VisibleEquipmentDependenciesIncludeArmorCloakAndHeldItems()
        {
            var root = BlueprintRoot(ResourceType.Utc, "darthgravius");

            BlueprintModelResolver.GetVisibleEquippedItemResRefs(root).Should().BeEquivalentTo(
                new[] { "graviusrobe001", "d_gravius_saber", "jeweled_cloak" });
        }

        [Test]
        public void Resolve_SegmentedCreatureWithRobeArmor_EmitsRobeAlongsideAllBodyParts()
        {
            // The resolver never suppresses parts for a robe — whether a robe replaces the body
            // parts it covers is a geometry question (RobeCoverage.IsFullBodyRobe) the renderer
            // answers after loading the robe model, because SWLOR's partial robes (loincloths,
            // tabards) must render alongside the full body. The robe part itself is only emitted
            // when its model resolves.
            var root = BlueprintRoot(ResourceType.Utc, "agr_guildmaster");
            var robeArmor = BlueprintRoot(ResourceType.Uti, "noble_gr");
            // This corpus item carries the EE word companion, which is authoritative over the byte.
            robeArmor.Get("ArmorPart_Robe").SetInteger(7);
            robeArmor.Get("xArmorPart_Robe").SetInteger(7);

            var withRobe = BlueprintModelResolver.Resolve(
                ResourceType.Utc, root, Appearances(), null, null, _ => robeArmor, _ => true);

            var parts = withRobe.Parts.ToDictionary(p => p.PartType, p => p.ModelResRef);
            parts.Should().ContainKey("robe").WhoseValue.Should().Be("pmh0_robe007");
            parts.Should().ContainKey("chest").And.ContainKey("pelvis")
                .And.ContainKey("bicepl").And.ContainKey("handr").And.ContainKey("shinl");
            parts.Should().ContainKey("head").And.ContainKey("footr");

            // Robe model does not resolve -> no robe part, body unchanged.
            var withoutRobeModel = BlueprintModelResolver.Resolve(
                ResourceType.Utc, root, Appearances(), null, null, _ => robeArmor, _ => false);

            withoutRobeModel.Parts.Should().NotContain(p => p.PartType == "robe");
            withoutRobeModel.Parts.Should().Contain(p => p.PartType == "chest");
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
        public void Resolve_Door_YieldsModelFromGenericdoors2daGenericTypeNew()
        {
            // _mdrn_dt_bars.utd: Appearance 0 and GenericType_New 47 select genericdoors.2da.
            var root = BlueprintRoot(ResourceType.Utd, "_mdrn_dt_bars");

            var result = BlueprintModelResolver.Resolve(ResourceType.Utd, root, null, null, Doors());

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("tn_gdoor_07");
        }

        [Test]
        public void Resolve_Door_PrefersSpecificAppearanceFromDoortypes2da()
        {
            var root = BlueprintRoot(ResourceType.Utd, "_mdrn_dt_bars");
            root.Get("Appearance").SetUnsignedInteger(47);

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

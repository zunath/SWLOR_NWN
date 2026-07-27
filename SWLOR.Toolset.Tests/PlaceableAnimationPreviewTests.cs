using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    public class PlaceableAnimationPreviewTests
    {
        private static MdlAnimation Animation(string name, float length, params MdlNode[] nodes)
        {
            var root = new MdlNode { Name = "root" };
            foreach (var node in nodes)
            {
                node.Parent = root;
                root.Children.Add(node);
            }

            return new MdlAnimation { Name = name, Length = length, GeometryRoot = root };
        }

        [Test]
        public void DefaultStateIsExplicitlyPreferredOverFileOrder()
        {
            var model = new MdlModel();
            model.Animations.Add(Animation("open", 1f));
            model.Animations.Add(Animation("on", 1f));
            model.Animations.Add(Animation("default", 1f));

            MdlAnimationPose.FindPlaceableDefault(model)!.Name.Should().Be("default");
        }

        [Test]
        public void OnIsTheFallbackForEffectsWithoutDefault()
        {
            var model = new MdlModel();
            model.Animations.Add(Animation("close", 1f));
            model.Animations.Add(Animation("on", 0.033f));

            MdlAnimationPose.FindPlaceableDefault(model)!.Name.Should().Be("on");
        }

        [Test]
        public void DeclaredStatesPreserveOrderAndIgnoreDuplicateNames()
        {
            var model = new MdlModel();
            model.Animations.Add(Animation("open", 1f));
            model.Animations.Add(Animation("OPEN", 2f));
            model.Animations.Add(Animation("closed", 0f));

            MdlAnimationPose.PlaceableAnimations(model)
                .Select(animation => animation.Name)
                .Should().Equal("open", "closed");
        }

        [Test]
        public void PosedStateCarriesTransformFramesWithoutDuplicatingGeometry()
        {
            var geometryRoot = new MdlNode { Name = "root" };
            var mesh = new MdlTrimeshNode
            {
                Name = "arm",
                Parent = geometryRoot,
                Vertices = new[] { Vector3.Zero },
                Faces = new[] { new MdlFace() },
                Bitmap = "metal"
            };
            geometryRoot.Children.Add(mesh);

            var animatedNode = new MdlNode
            {
                Name = "arm",
                PositionTimes = new[] { 0f, 1f },
                PositionValues = new[] { Vector3.Zero, new Vector3(3f, 0f, 0f) }
            };
            var model = new MdlModel { Name = "machine", GeometryRoot = geometryRoot };
            model.Animations.Add(Animation("default", 1f, animatedNode));

            var rendered = MdlMeshBuilder.BuildPlaceablePreview(model);

            rendered.Meshes.Should().ContainSingle();
            rendered.Animations.Should().ContainSingle(
                animation => animation.Name == "default" && animation.IsPlayable);
            rendered.Meshes[0].AnimationFrames["default"].Should().HaveCountGreaterThan(1);
            rendered.Meshes[0].AnimationFrames["default"][0]
                .Should().NotBe(rendered.Meshes[0].AnimationFrames["default"][^1]);
        }

        [Test]
        public void EmitterOnlyPortalGetsAPlayableDefaultState()
        {
            var root = new MdlNode { Name = "root" };
            root.Children.Add(new MdlEmitterNode
            {
                Name = "portal-stars",
                Parent = root,
                Texture = "fxpa_starbnw",
                XGrid = 4,
                YGrid = 4,
                Loop = true
            });
            var model = new MdlModel { Name = "portal", GeometryRoot = root };

            var rendered = MdlMeshBuilder.BuildPlaceablePreview(model);

            rendered.Emitters.Should().ContainSingle();
            rendered.Animations.Should().ContainSingle(
                animation =>
                    animation.Name == "default" &&
                    animation.ShowsEmitters &&
                    animation.IsPlayable);
            rendered.DefaultAnimationName.Should().Be("default");
        }

        [Test]
        public void OrdinaryAreaModelDoesNotRetainContinuousPreviewEmitters()
        {
            var root = new MdlNode { Name = "root" };
            root.Children.Add(new MdlEmitterNode
            {
                Name = "portal-stars",
                Parent = root,
                Texture = "fxpa_starbnw"
            });
            var model = new MdlModel { Name = "portal", GeometryRoot = root };

            var rendered = MdlMeshBuilder.Build(model);

            rendered.Emitters.Should().BeEmpty();
            rendered.Animations.Should().BeEmpty();
        }

        [Test]
        public void OffStateSuppressesEmitterPlayback()
        {
            var root = new MdlNode { Name = "root" };
            root.Children.Add(new MdlEmitterNode
            {
                Name = "fire",
                Parent = root,
                Texture = "fxpa_smoke",
                Loop = true
            });
            var model = new MdlModel { Name = "fire", GeometryRoot = root };
            model.Animations.Add(Animation("on", 0.033f));
            model.Animations.Add(Animation("off", 0.033f));

            var rendered = MdlMeshBuilder.BuildPlaceablePreview(model);

            rendered.Animations.Single(animation => animation.Name == "on").ShowsEmitters.Should().BeTrue();
            rendered.Animations.Single(animation => animation.Name == "off").ShowsEmitters.Should().BeFalse();
        }

        [Test]
        public void NonLoopingEmittersDoNotBecomePermanentAmbientParticles()
        {
            var root = new MdlNode { Name = "root" };
            root.Children.Add(new MdlEmitterNode
            {
                Name = "one-shot-debris",
                Parent = root,
                Texture = "fxpa_cloud02",
                Update = "Fountain",
                Loop = false
            });
            var model = new MdlModel { Name = "debris", GeometryRoot = root };

            var rendered = MdlMeshBuilder.BuildPlaceablePreview(model);

            rendered.Emitters.Should().BeEmpty();
        }

        [Test]
        public void OneShotDamageEmittersDoNotPlayAsPersistentPreviewVfx()
        {
            var root = new MdlNode { Name = "root" };
            root.Children.Add(new MdlEmitterNode
            {
                Name = "damage-debris",
                Parent = root,
                Texture = "fxpa_cloud02",
                Update = "Explosion",
                Loop = true
            });
            var model = new MdlModel { Name = "sarcophagus", GeometryRoot = root };
            model.Animations.Add(Animation("default", 0f));
            model.Animations.Add(Animation("damage", 0.1f));

            var rendered = MdlMeshBuilder.BuildPlaceablePreview(model);

            rendered.Emitters.Should().BeEmpty();
            rendered.Animations.Should().OnlyContain(animation => !animation.ShowsEmitters);
            rendered.Animations.Should().OnlyContain(animation => !animation.IsPlayable);
        }
    }
}

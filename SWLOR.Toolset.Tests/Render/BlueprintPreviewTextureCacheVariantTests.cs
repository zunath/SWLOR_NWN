using System.Numerics;
using FluentAssertions;
using Nwn.Preview.Scene;
using Nwn.Preview.Thumbnails;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests.Render;

[TestFixture]
public sealed class BlueprintPreviewTextureCacheVariantTests
{
    [Test]
    public void RenderUsesSeparateTexturesForMeshesWithDifferentArmorPartsAndSameTextureName()
    {
        var leftHandMesh = CreateMesh("left_hand", -1.2f);
        var rightHandMesh = CreateMesh("right_hand", 1.2f);
        SwlorRenderMeshMetadataStore.SetArmorPart(leftHandMesh, AppearanceArmor.LeftHand);
        SwlorRenderMeshMetadataStore.SetArmorPart(rightHandMesh, AppearanceArmor.RightHand);
        var model = new RenderModel
        {
            Name = "armor",
            Meshes = [leftHandMesh, rightHandMesh]
        };
        var resolverCalls = 0;

        var pixels = ThumbnailRenderer.Render(
            model,
            size: 96,
            resolveMeshTexture: mesh =>
            {
                resolverCalls++;
                return SwlorRenderMeshMetadataStore.GetArmorPart(mesh) switch
                {
                    AppearanceArmor.LeftHand => SolidTexture(255, 24, 24),
                    AppearanceArmor.RightHand => SolidTexture(24, 24, 255),
                    _ => null
                };
            },
            resolveCacheVariant: BlueprintPreviewRenderer.ResolveTextureCacheVariant);

        pixels.Should().NotBeNull();
        resolverCalls.Should().Be(2);
        var output = pixels!;
        var redPixels = 0;
        var bluePixels = 0;
        for (var index = 0; index < output.Length; index += ThumbnailRenderer.BytesPerPixel)
        {
            var blue = output[index];
            var green = output[index + 1];
            var red = output[index + 2];
            var alpha = output[index + 3];
            if (alpha == 0)
                continue;

            if (red > blue * 2 && red > green * 2)
                redPixels++;
            if (blue > red * 2 && blue > green * 2)
                bluePixels++;
        }

        redPixels.Should().BeGreaterThan(0);
        bluePixels.Should().BeGreaterThan(0);
    }

    private static RenderMesh CreateMesh(string nodeName, float centerX) => new()
    {
        NodeName = nodeName,
        TextureName = "shared_mtr",
        Positions =
        [
            centerX - 0.75f, 0f, -0.75f,
            centerX + 0.75f, 0f, -0.75f,
            centerX + 0.75f, 0f, 0.75f,
            centerX - 0.75f, 0f, 0.75f
        ],
        Normals = [],
        TexCoords = [0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f],
        Indices = [0, 1, 2, 0, 2, 3],
        Transform = Matrix4x4.Identity
    };

    private static ThumbnailTexture SolidTexture(byte red, byte green, byte blue) =>
        new(1, 1, [red, green, blue, 255]);
}
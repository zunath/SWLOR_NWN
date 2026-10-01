using System.Buffers.Binary;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests.Support;

public sealed class NativePreviewWorkspaceFixture : IDisposable
{
    public NativePreviewWorkspaceFixture(Action<string>? configure = null)
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), "swlor-native-preview", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(Path.Combine(DirectoryPath, "preview_fixture.mdl"), """
            newmodel preview_fixture
            setsupermodel preview_fixture NULL
            classification Tile
            setanimationscale 1
            beginmodelgeom preview_fixture
            node trimesh panel
              parent NULL
              position 0 0 0
              orientation 0 0 1 0
              bitmap fixture_map
              materialname fixture_material
              render 1
              verts 3
                0 0 0
                1 0 0
                0 1 0
              tverts 3
                0 0 0
                1 0 0
                0 1 0
              faces 1
                0 1 2 1 0 1 2 0
            endnode
            endmodelgeom preview_fixture
            donemodel preview_fixture
            """);
        WriteDds(Path.Combine(DirectoryPath, "fixture_map.dds"));
        configure?.Invoke(DirectoryPath);
        Resources = new ResourceIndex(null, [new("selected fixture", DirectoryPath)]);
        Resources.InitializationTask.GetAwaiter().GetResult();
    }

    public string DirectoryPath { get; }
    public ResourceIndex Resources { get; }

    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);

    public static void WriteDds(string path, bool reverseColors = false)
    {
        var bytes = new byte[144];
        "DDS "u8.CopyTo(bytes);
        Write(4, 124); Write(8, 0x81007); Write(12, 8); Write(16, 4); Write(20, 16); Write(28, 1);
        Write(76, 32); Write(80, 4); "DXT1"u8.CopyTo(bytes.AsSpan(84)); Write(108, 0x1000);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(128), reverseColors ? (ushort)0x001f : (ushort)0xf800);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(136), reverseColors ? (ushort)0xf800 : (ushort)0x001f);
        File.WriteAllBytes(path, bytes);
        void Write(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
    }

    public static void WriteTga(string path, byte red, byte green, byte blue)
    {
        var bytes = new byte[22];
        bytes[2] = 2; bytes[12] = 1; bytes[14] = 1; bytes[16] = 32; bytes[17] = 0x28;
        bytes[18] = blue; bytes[19] = green; bytes[20] = red; bytes[21] = 255;
        File.WriteAllBytes(path, bytes);
    }
}

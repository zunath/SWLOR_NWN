using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Byte-identity pinning for the set-piece room-supply scaling mechanism (see
/// DungeonTilesetProfile.SetPieceRoomSupplyScaling and LayoutParameterConstraints.
/// ApplySetPieceRoomSupplyScaling): only a tileset profile that explicitly declares the scaling knob
/// may generate differently than it did before the mechanism existed. Every OTHER onboarded profile
/// -- including the four interior profiles that declare SetPieceRoomCornerFloor without the scaling
/// knob (secretbase/modernfacility/labstorage/officeinteriors), which is exactly why the scaling
/// trigger is an explicit declaration rather than piggybacking on SetPieceRoomCornerFloor -- must
/// produce the IDENTICAL resolved layout for a fixed (seed, size) after the mechanism as before it.
///
/// The expected hashes below were recorded from this same test running against the pre-mechanism
/// tree (feature/procedural-areas @ 251e41cda plus the then-uncommitted fcx01 platform1 wiring,
/// July 2026), i.e. they ARE the "before" snapshot: any post-mechanism drift in any pinned
/// composition fails this test. City (fcx01) compositions are deliberately absent -- they are the
/// mechanism's intended target and are covered by CityBlockDensityTests instead.
/// </summary>
public class RoomSupplyScalingIsolationTests
{
    private sealed record PinnedComposition(string Label, string TilesetKey, string LayoutKey, int Size);

    // Spans all 5 layout styles, both tileset-profile definition classes, interior + exterior
    // solid-inversion tilesets, and every non-city profile that declares SetPieceRoomCornerFloor.
    // Size 32 is included everywhere because the scaling mechanism only activates above the 20x20
    // tuning baseline -- a regression that only manifests at large sizes must still be caught.
    private static readonly PinnedComposition[] Pinned =
    {
        new("sewers/warren/20", StandardTilesetProfiles.Sewers, StandardLayoutProfiles.Warren, 20),
        new("sewers/warren/32", StandardTilesetProfiles.Sewers, StandardLayoutProfiles.Warren, 32),
        new("facility/packed/20", StandardTilesetProfiles.Facility, StandardLayoutProfiles.Packed, 20),
        new("facility/packed/32", StandardTilesetProfiles.Facility, StandardLayoutProfiles.Packed, 32),
        new("ancientruin/halls/20", StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls, 20),
        new("ancientruin/halls/32", StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls, 32),
        new("cavern/organic/20", StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Organic, 20),
        new("cavern/organic/32", StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Organic, 32),
        new("minescaverns/complex/20", BaseGameTilesetProfiles.MinesAndCaverns, StandardLayoutProfiles.Complex, 20),
        new("minescaverns/complex/32", BaseGameTilesetProfiles.MinesAndCaverns, StandardLayoutProfiles.Complex, 32),
        new("forest/halls/20", BaseGameTilesetProfiles.Forest, StandardLayoutProfiles.Halls, 20),
        new("forest/halls/32", BaseGameTilesetProfiles.Forest, StandardLayoutProfiles.Halls, 32),
        new("crypt/labyrinth/20", BaseGameTilesetProfiles.Crypt, StandardLayoutProfiles.Labyrinth, 20),
        new("crypt/labyrinth/32", BaseGameTilesetProfiles.Crypt, StandardLayoutProfiles.Labyrinth, 32),
        new("secretbase/packed/32", BaseGameTilesetProfiles.SecretBase, StandardLayoutProfiles.Packed, 32),
        new("modernfacility/complex/32", BaseGameTilesetProfiles.ModernFacility, StandardLayoutProfiles.Complex, 32),
        new("labstorage/packed/32", BaseGameTilesetProfiles.LabStorage, StandardLayoutProfiles.Packed, 32),
        new("officeinteriors/packed/32", BaseGameTilesetProfiles.OfficeInteriors, StandardLayoutProfiles.Packed, 32),
    };

    private static readonly int[] Seeds = { 7001, 7002, 7003 };

    // label -> seed -> SHA256 hash of the fully resolved layout (tiles + rooms + transitions +
    // pinned cells -- see HashResult). Recorded from the pre-mechanism tree; see class doc comment.
    private static readonly Dictionary<string, Dictionary<int, string>> ExpectedHashes = new()
    {
        ["ancientruin/halls/20"] = new() { [7001] = "61660DA8EF891A3AA914EF3BA871F2205922F139A4FB4C893D0BF3E778E6B401", [7002] = "B8CF00FA9E3A59DCFD34933CAD780A62D2F928FE60EA257FB88D080D6D61EEBA", [7003] = "AE58BA675AC31CBAABE0C41F760A14C9CD7A805A9F5CC260501B62BC70E36208" },
        ["ancientruin/halls/32"] = new() { [7001] = "B31B58CE7FFF0A2F1BC10A3E0DA696906C6977A60CC6ED71BBC8C13BC66B677E", [7002] = "F287E71AE4941C54E6E1AE3B95AD99865B3152B1D93A275A24F592859CEC544E", [7003] = "353E29596DD6C0CC1CA9081356C5953957B72D39C44B5527073DE080C6B4381D" },
        ["cavern/organic/20"] = new() { [7001] = "4872B23D6A3CD2D87F9373CFA869C97969AC58A46937E2EB3A9EBC63C6CC3907", [7002] = "719DC8B86AEC9D731306CC33CC9FF31D59F2CF043ABACE641753490E8E96EBAA", [7003] = "B1E8C3B9FE1D9C4CE83D79B4031970D36C37BFA37906333C1F2593D5AF843B0B" },
        ["cavern/organic/32"] = new() { [7001] = "CE1CC59FCAED54B01E0E84ECB2DA1FCF8B0C2123563FA3E39CC2844F60419790", [7002] = "F82351DA0680009292255F067731FE31EA2058E771763ECE724A8A00FC17B047", [7003] = "9A7BFDE08D0186DB4B994A12C74DDF167AAF9D54B7777A5731EA4565F8353019" },
        ["crypt/labyrinth/20"] = new() { [7001] = "13AF09AD9C8EDCDD380E01B518BEC5991AE267355EEBD73823A423EDAD2B3C08", [7002] = "9D76725B285281B6887A180567D2DAB7930214302EE0D58E4308D9E33E65F2AD", [7003] = "E185E14598D2A2C20576178CFAD4FCEA6805454EDA44C0A7E0FF3D44918FD1EC" },
        ["crypt/labyrinth/32"] = new() { [7001] = "B037C76130A88BADCC32EEEC1C92539D1FBED06C8C353F4F1B91B7FC299C46E2", [7002] = "66094BB4E68A644BE8C3A21FF20255C5B25059BD7B2FFEB1BE90E1182B69D4B4", [7003] = "1E1EFF9F16B9AD3A6FD25C03239913501596E9659910E84E7E0BB0670CD432D2" },
        ["facility/packed/20"] = new() { [7001] = "A7B71E12C6260E4C3D33CD5EE54E88924F5F6778640B9BEF99A26333C94908F0", [7002] = "A148AB40B50C1F151F30E9F0F81C69300FBD09038A6686ED4D32E16285A1DC88", [7003] = "78EC50C5A38EA3AB08C01A36D35BC1224564BDBC670F0218E654E37289048F97" },
        ["facility/packed/32"] = new() { [7001] = "B9FCBAB6958335D18BAC1D9BC1EAC371D717BE2727DD45CFFC65C7F6E2F77B31", [7002] = "CE544F46DC00B001B695730D81C682C681BEB0386A8694B9B4A05A18445AC2F4", [7003] = "D326FD9D2260004FF8EA1D721CEA5B0F0695A6472AB40BB5B889166C14623238" },
        ["forest/halls/20"] = new() { [7001] = "0D1ACDA865B227B29F400BD957CDA50EADC473F74B5E5332F4E9D490F4DBD9F9", [7002] = "8019E49DA5E03A0446BDD9F258ADD07E9A97B91E2750A9C6FBF170889050716C", [7003] = "5D1E77DEC18652844D6CE104835134EF6CCDFBB1CE8463B692745C226954BE76" },
        ["forest/halls/32"] = new() { [7001] = "D8DDE3C5D837F80CAE623AA56FB7430A25653F38A348F77B7803D0D68FF9BDFC", [7002] = "DB80CBC8BA88633A398B108812302E9E36235470E5F3D3A0930C4CB971551B56", [7003] = "AA91612463C55D6EB27FC3455500162E8F94121E9F9FA87EDCB15D2C127748F7" },
        ["labstorage/packed/32"] = new() { [7001] = "8945F95A64CE245E04BB6B0457BB55D3776F4094718B6EC7CD5346EC9948488E", [7002] = "66A34F686B4C3E2BAC63D809D188F68FF9EBADCA436F7C0AC1535FCDEFFED210", [7003] = "A26414DABA66410A6093589EC3EFC563CBF30E672D3C9B1E8CCCF573DE1CE8B8" },
        ["minescaverns/complex/20"] = new() { [7001] = "AC6F60A3DB1CE63DE882D3DAB9D4D89C86AFE68DCDEBCF1534A925DBF6747A8C", [7002] = "8B4072C513B2DD7666B96F797177B0B54306CC782782FFCD4A99A9E64E2303C9", [7003] = "35636F1CC9411EE018C043FB17446C1C0323854B8190A0B80F9486E339A0ADB2" },
        ["minescaverns/complex/32"] = new() { [7001] = "144C4518B1733066609A8E1A338479568C661F75C7A8428205E38DA5CA0C8A99", [7002] = "5F674363A50F88C450FBD912C63246896DFF2EF02574544B0EB849115F793058", [7003] = "D34E8C55B2028FDBE20E65A79B1553B74B800E8B61A14CE3D02365C87CC09EDB" },
        // Refreshed (this pass): modernfacility gained a genuinely new, deliberately-wired SetPiece
        // ("elevator", see BaseGameTilesetProfiles.ModernFacility's own doc comment on
        // LayoutGroupStamper.TryClassify's mixed/open-member tolerance) -- a real content change, not a
        // SetPieceRoomSupplyScaling regression, so its pin is expected to move.
        ["modernfacility/complex/32"] = new() { [7001] = "B9D0B8CA6CB22D1A2273905B6BE3573A6EAC8237A9B2ED70658E16D34EBC6A42", [7002] = "666856ADBD2A490A55E5F86CF44BF240DA5BE9D76E6D76329F00718930C9E74D", [7003] = "7F2C8D6C99D478AC0F3F7263210394ED635319DC88D9D39DBE85889609D74405" },
        // Refreshed (this pass): officeinteriors gained "Office_Vinyl_Entry 2x1" (see
        // BaseGameTilesetProfiles.OfficeInteriors' own doc comment) -- same rationale as modernfacility
        // above.
        ["officeinteriors/packed/32"] = new() { [7001] = "08BE9FECB9B475D2C0D98A1BF7ED8660A24E223CF9DABB2155AB1BE7A67EFA8C", [7002] = "B33E5CAD70FBCCBEF3E49B7AB771EEDD1F433F0F5B53DDD0696D6A31C6B79D00", [7003] = "2F5CB04E6EF1A01A47868E722F042038F14CA2A05E15ADD41F3BB50FC5674831" },
        ["secretbase/packed/32"] = new() { [7001] = "F945C057F184CA278A3F6262FF6E516762500895D5C5A933B17739E849EE7554", [7002] = "F3BAB26FA52B5B7F0BE50CC68A748F03D19E01E589212E8ABA9C9CF09F83F178", [7003] = "7FA2073B7FD810AFE4514F69A5F15885E19A136D5C931CE202DFC9B7F7E5F433" },
        ["sewers/warren/20"] = new() { [7001] = "51D2ECD8BBD1A5F49A4F7E10F85B5AA75448741E72B23B5319758E8230F91BF3", [7002] = "D13717BFAAAC51329DD9987CC0D58656A63D82B4E3C9FCF6F6BD6D4B31455944", [7003] = "528A86F6A08674DED6CFA2FCBDE3119910C00B3298BDE7B97E852F07F696932A" },
        ["sewers/warren/32"] = new() { [7001] = "30C74BD84E2829C4E430F265AE3EEFC357D716A38C437AF3F78C83D6F6E47062", [7002] = "868C6F922ABBB957EF17AC961C6A4E2026842939A79E754EA798891722997D49", [7003] = "52552BB7DCCECAE2765BA43D3B64086A7ABF00F6368AD843D020A157357193E8" },
    };

    [Test]
    public void NonScalingProfiles_FixedSeeds_ProduceIdenticalLayouts()
    {
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        foreach (var (k, v) in new BaseGameTilesetProfiles().BuildTilesetProfiles())
            tilesetProfiles[k] = v;
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();

        var actual = new Dictionary<string, Dictionary<int, string>>();
        var failures = new List<string>();

        foreach (var pin in Pinned)
        {
            var tileset = tilesetProfiles[pin.TilesetKey];
            var layout = layoutProfiles[pin.LayoutKey];
            var model = TilesetTestSource.LoadTileset(tileset.TilesetResref);
            var perSeed = new Dictionary<int, string>();
            actual[pin.Label] = perSeed;

            foreach (var seed in Seeds)
            {
                var composition = new DungeonComposition { Tileset = tileset, Layout = layout };
                var result = LayoutSolver.Solve(
                    composition.BuildLayoutParameters(), model, pin.Size, pin.Size, seed,
                    tileset.PrimaryOpenTerrain);

                result.Success.Should().BeTrue(
                    $"pinned composition {pin.Label} seed {seed} must solve (it did when recorded): {result.FailureReason}");

                perSeed[seed] = HashResult(result);
            }
        }

        if (ExpectedHashes.Count == 0)
        {
            // Recording mode: no expectations embedded yet. Dump the actual table in paste-ready
            // form and fail so this state can never be mistaken for a passing pin.
            var sb = new StringBuilder();
            foreach (var (label, perSeed) in actual.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                sb.Append($"        [\"{label}\"] = new() {{ ");
                sb.Append(string.Join(", ", perSeed.OrderBy(kv => kv.Key).Select(kv => $"[{kv.Key}] = \"{kv.Value}\"")));
                sb.AppendLine(" },");
            }

            Assert.Fail("ExpectedHashes is empty -- paste the recorded table:\n" + sb);
        }

        foreach (var pin in Pinned)
        {
            ExpectedHashes.Should().ContainKey(pin.Label, "every pinned composition needs a recorded expectation");
            foreach (var seed in Seeds)
            {
                if (ExpectedHashes[pin.Label][seed] != actual[pin.Label][seed])
                    failures.Add($"{pin.Label} seed {seed}: expected {ExpectedHashes[pin.Label][seed]}, got {actual[pin.Label][seed]}");
            }
        }

        failures.Should().BeEmpty(
            "profiles that never declare SetPieceRoomSupplyScaling must keep byte-identical layouts");
    }

    /// <summary>
    /// Canonical hash over everything a downstream consumer can observe from a solved layout:
    /// resolved tile grid (id/orientation/height per cell), room metadata (id, role, center, tile
    /// membership), transition anchors, and pinned cells. Room metadata is included deliberately --
    /// the scaling mechanism's PackedRooms lever changes only the REPORTED room list (leaf geometry
    /// is identical), so tiles alone would under-pin.
    /// </summary>
    private static string HashResult(LayoutSolverResult result)
    {
        var sb = new StringBuilder();
        var resolved = result.Resolved;
        sb.Append(resolved.Width).Append('x').Append(resolved.Height).Append(';');

        foreach (var tile in resolved.Tiles)
            sb.Append(tile.TileId).Append('/').Append(tile.Orientation).Append('/').Append(tile.Height).Append(',');

        sb.Append(";rooms:");
        foreach (var room in resolved.Rooms.OrderBy(r => r.Id))
        {
            sb.Append(room.Id).Append('=').Append(room.Role).Append('@')
              .Append(room.CenterTile.X).Append(',').Append(room.CenterTile.Y).Append('#');
            foreach (var t in room.Tiles.OrderBy(t => t.Y).ThenBy(t => t.X))
                sb.Append(t.X).Append(',').Append(t.Y).Append('|');
        }

        sb.Append(";transitions:");
        foreach (var tr in resolved.Transitions.OrderBy(t => t.Kind).ThenBy(t => t.Tile.X).ThenBy(t => t.Tile.Y))
            sb.Append(tr.Kind).Append('@').Append(tr.Tile.X).Append(',').Append(tr.Tile.Y).Append('/').Append(tr.Style).Append('|');

        sb.Append(";pins:");
        foreach (var (cell, pin) in result.Layout.PinnedTiles.OrderBy(kv => kv.Key.Y).ThenBy(kv => kv.Key.X))
            sb.Append(cell.X).Append(',').Append(cell.Y).Append('=').Append(pin.TileId).Append('/').Append(pin.Orientation).Append('/').Append(pin.Height).Append('|');

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
